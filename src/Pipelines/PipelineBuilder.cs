namespace Dazinator.Extensions.Pipelines;

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

public class PipelineBuilder
{
    private readonly List<Func<IServiceProvider, PipelineStep, PipelineStep>> _components = new();

    private readonly List<IPipelineInspector> _inspectors = new();
    private readonly List<Type> _inspectorTypes = new();
    private readonly List<Func<IServiceProvider, IPipelineInspector>> _inspectorFactories = new();

    private readonly Dictionary<Type, object> _extensionState = new();

    private bool _isBuilt = false;

    public IServiceProvider Services { get; init; }
    public PipelineBuilder(IServiceProvider rootProvider)
    {
        Services = rootProvider;
    }

    public PipelineBuilder AddInspector(IPipelineInspector inspector)
    {
        _inspectors.Add(inspector);
        // Handle initialization if supported
        if (inspector is IInspectorInitialization init)
        {
            init.Initialize(this);
        }
        return this;
    }

    public PipelineBuilder AddInspector<T>() where T : IPipelineInspector
    {
        _inspectorTypes.Add(typeof(T));
        return this;
    }

    public PipelineBuilder AddInspector(Func<IServiceProvider, IPipelineInspector> factory)
    {
        _inspectorFactories.Add(factory);
        return this;
    }


    private void AddComponent(Func<IServiceProvider, PipelineStep, PipelineStep> item)
    {
        _components.Add(item);
    }

    private PipelineStep CreateInspectedStep(PipelineStep step, string stepId, string stepType, IServiceProvider sp, int stepIndex, PipelineStep next)
    {
#pragma warning disable IDE0022 // Use expression body for method
        return async context =>
        {
            context.CurrentStepIndex = stepIndex;
            context.CurrentStepId = stepId;

            var stepContext = new PipelineStepContext(stepId, stepType, sp, context);
            var sw = Stopwatch.StartNew();

            try
            {
                foreach (var inspector in _inspectors)
                {
                    await inspector.BeforeStepAsync(stepContext);
                    if (stepContext.ShouldSkip)
                    {
                        // Even if we skip this step, we should continue the pipeline
                        await next(context);
                        return; // Skip step execution if any inspector requests it
                    }
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

    internal int CurrentStepIndex => _components.Count - 1;

    public void Add(Func<PipelineStep, PipelineStep> item, string? stepId, [CallerMemberName] string? stepTypeName = null)
    {
        var index = CurrentStepIndex + 1;  // Gets the next index which is captured below, as the index of the step after its added.
        AddComponent((sp, next) =>
        {
            var getStep = item(next);
            var inspected = CreateInspectedStep(
                getStep,
                GetStepId(stepId),
                stepTypeName ?? "Unknown",
                sp,
                 index,
                 next);
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

    public Pipeline Build()
    {

        if (_isBuilt)
        {
            throw new InvalidOperationException("Pipeline has already been built. PipelineBuilder instances should only be built once.");
        }

        _isBuilt = true;
        ResolveInspectors(Services);

        PipelineStep pipeline = _ => Task.CompletedTask;

        foreach (var component in _components.AsEnumerable().Reverse())
        {
            pipeline = component(Services, pipeline);
        }

        return new Pipeline(pipeline, Services, _inspectors.ToList(), _extensionState);

    }

    private void ResolveInspectors(IServiceProvider rootProvider)
    {
        // var allInspectors = new List<IPipelineInspector>();

        // Add directly registered inspectors
        // allInspectors.AddRange(_inspectors);

        // Resolve inspector types
        foreach (var type in _inspectorTypes)
        {
            var inspector = (IPipelineInspector)ActivatorUtilities.CreateInstance(rootProvider, type);
            _inspectors.Add(inspector);
        }

        // Use inspector factories
        foreach (var factory in _inspectorFactories)
        {
            _inspectors.Add(factory(rootProvider));
        }
    }

    #region Extension State
    internal void SetExtensionState<T>(T state) where T : class
    {
        _extensionState[typeof(T)] = state;
    }

    internal T? GetExtensionState<T>() where T : class
    {
        return _extensionState.TryGetValue(typeof(T), out var state) ? state as T : null;
    }

    // Optional: method to check if state exists
    internal bool HasExtensionState<T>() where T : class
    {
        return _extensionState.ContainsKey(typeof(T));
    }
    #endregion   

}


public delegate Task PipelineStep(PipelineContext context);
