namespace Dazinator.Extensions.Pipelines;

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public class PipelineBuilder
{
    private readonly List<Func<IServiceProvider, PipelineStep, PipelineStep>> _components = new();
    private readonly List<IPipelineInspector> _inspectors = new();

    public PipelineBuilder AddInspector(IPipelineInspector inspector)
    {
        _inspectors.Add(inspector);
        return this;
    }

    private void AddComponent(Func<IServiceProvider, PipelineStep, PipelineStep> item)
    {
        _components.Add(item);
    }

    private PipelineStep CreateInspectedStep(PipelineStep step, string stepId, string stepType, IServiceProvider sp, int stepIndex)
    {       
#pragma warning disable IDE0022 // Use expression body for method
        return async context =>
        {
            context.CurrentStepIndex = stepIndex;
            var stepContext = new PipelineStepContext(stepId, stepType, sp, context);
            var sw = Stopwatch.StartNew();

            try
            {
                foreach (var inspector in _inspectors)
                {
                    await inspector.BeforeStepAsync(stepContext);
                }

                await step(context);
            }
            catch (Exception ex)
            {
                stepContext.Duration = sw.Elapsed;
                stepContext.Exception = ex;
                foreach (var inspector in _inspectors)
                {
                    await inspector.OnExceptionAsync(stepContext);
                }
                throw;
            }
            finally
            {
                sw.Stop();
                stepContext.Duration = sw.Elapsed;
                foreach (var inspector in _inspectors)
                {
                    await inspector.AfterStepAsync(stepContext);
                }
            }
        };
#pragma warning restore IDE0022 // Use expression body for method
    }
       

    private static string GetStepId(string? stepId) => stepId ?? "Anonymous";

    public void Add(Func<PipelineStep, PipelineStep> item, string? stepId, [CallerMemberName] string? stepTypeName = null)
    {
        AddComponent((sp, next) =>
        {
            var getStep = item(next);
            var inspected = CreateInspectedStep(
                getStep,
                GetStepId(stepId),
                stepTypeName ?? "Unknown",
                sp,
                 _components.Count);
            return inspected;
        });
    }

    /// <summary>
    /// Use a delegate as a middleware.
    /// </summary>
    /// <param name="middleware"></param>
    /// <param name="stepId"></param>
    /// <returns></returns>
    public PipelineBuilder Use(Func<PipelineStep, PipelineStep> middleware, string? stepId = null)
    {
        Add(middleware,
         stepId,
       nameof(Use));
        return this;
    }

    public Pipeline Build(IServiceProvider rootProvider)
    {
        PipelineStep pipeline = _ => Task.CompletedTask;

        foreach (var component in _components.AsEnumerable().Reverse())
        {
            pipeline = component(rootProvider, pipeline);
        }

        return new Pipeline(pipeline, rootProvider);
    }

    public PipelineBuilder WrapLastComponent(
       Func<Func<IServiceProvider, PipelineStep, PipelineStep>,
           Func<IServiceProvider, PipelineStep, PipelineStep>> wrapper)
    {
        if (_components.Count == 0)
        {
            throw new InvalidOperationException("No component to wrap");
        }

        _components[^1] = wrapper(_components[^1]);
        return this;
    }


    internal PipelineBuilder CreateBranch()
    {
        var branchBuilder = new PipelineBuilder();
        // Transfer inspectors
        foreach (var inspector in _inspectors)
        {
            branchBuilder.AddInspector(inspector);
        }
        
        return branchBuilder;
    }  

   
    // public PipelineStep Build(IServiceProvider rootProvider) => Build(rootProvider, _ => Task.CompletedTask);

    //private PipelineStep Build(IServiceProvider provider, PipelineStep final)
    //{

    //    var pipeline = final;
    //    var componentsToProcess = _components.AsEnumerable().Reverse().ToList();

    //    foreach (var component in componentsToProcess)
    //    {
    //        var currentComponent = component;
    //        var previousPipeline = pipeline;
    //        pipeline = ct => currentComponent(provider, previousPipeline)(ct);
    //    }

    //    return pipeline;
    //}

}


public delegate Task PipelineStep(PipelineContext context);
