namespace Dazinator.Extensions.Pipelines;

using System.Diagnostics;
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

    private PipelineStep CreateInspectedStep(PipelineStep step, string stepId, string stepType, IServiceProvider sp)
    {
        return async context =>
        {
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
    }

    public void Add(Func<IServiceProvider, PipelineStep, PipelineStep> item)
    {
        _components.Add(item);
    }

    private static string GetStepId(string? stepId) => stepId ?? "Anonymous";

    public void Add(Func<PipelineStep, PipelineStep> item, string? stepId, string stepTypeName)
    {
        _components.Add((sp, next) =>
        {
            var getStep = item(next);
            var inspected = CreateInspectedStep(
                getStep,
                GetStepId(stepId),
                stepTypeName,
                sp);
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

public class Pipeline
{
    private readonly PipelineStep _pipeline;
    private readonly IServiceProvider _serviceProvider;

    internal Pipeline(PipelineStep pipeline, IServiceProvider serviceProvider)
    {
        _pipeline = pipeline;
        _serviceProvider = serviceProvider;
    }

    public Task Run(CancellationToken cancellationToken = default)
    {
        var context = new PipelineContext(_serviceProvider, cancellationToken);
        return _pipeline(context);
    }

    /// <summary>
    /// Run the pipeline with an existing context, making pipelines "composable".
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public Task RunWithContext(PipelineContext context)
    {
        return _pipeline(context);
    }
}


public delegate Task PipelineStep(PipelineContext context);


public class PipelineContext
{
    public PipelineContext(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        ServiceProvider = serviceProvider;
        CancellationToken = cancellationToken;
    }

    public IServiceProvider ServiceProvider { get; set; }
    public CancellationToken CancellationToken { get; set; }
}
