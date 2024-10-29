namespace Dazinator.Extensions.Pipelines.Features.Filter;
using System.Threading.Tasks;

public class FilterExecutionInspector : IPipelineInspector, IInspectorInitialization
{
    public async Task BeforeStepAsync(PipelineStepContext context)
    {
        // When we execute, we always gran the registry from the context and work with it.
        // We've ensure in Initialize() that we set a new Reigstry instance per pipeline.
        // This means we can be executing right now in multiple pipelines / branches, and running concurrently
        // - we always use the right registry for the current pipeline based on the context argument.

        var registry = context.PipelineContext.GetExtensionState<FilterRegistry>();
        var filters = registry.GetFilters(context.ServiceProvider, context.PipelineContext.CurrentStepIndex);
        foreach (var filter in filters)
        {
            await filter.BeforeStepAsync(context);
            if (context.ShouldSkip)
            {
                break;
            }
        }
    }

    public async Task AfterStepAsync(PipelineStepContext context)
    {
        // When we execute, we always gran the registry from the context and work with it.
        // We've ensure in Initialize() that we set a new Reigstry instance per pipeline.
        // This means we can be executing right now in multiple pipelines / branches, and running concurrently
        // - we always use the right registry for the current pipeline based on the context argument.

        var registry = context.PipelineContext.GetExtensionState<FilterRegistry>();
        var filters = registry.GetFilters(context.ServiceProvider, context.PipelineContext.CurrentStepIndex);
        // Execute After steps in reverse order
        // this models the behaviour of middleware in terms of the first filter to run, is the "outer" one, meaning it should also be the last to complete.
        foreach (var filter in filters.Reverse())
        {
            await filter.AfterStepAsync(context);
        }      
    }

    public Task OnExceptionAsync(PipelineStepContext context) => Task.CompletedTask;

    /// <summary>
    /// Called when the inspector instance is first added to a new Pipeline Builder. This is called for a root pipeline, but also for each branch pipeline created.
    /// </summary>
    /// <param name="builder"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public void Initialize(PipelineBuilder builder)
    {
        if (builder.HasExtensionState<FilterRegistry>())
        {
            throw new InvalidOperationException("FilterExecutionInspector has already been initialized for this pipeline.");
        }
        // We ensure each new pipeline gets a new FilterRegistry
        // When we execute, we always gran the registry from the context and work with it. This means we can be part of multiple pipeline and run concurrently.
        var registry = new FilterRegistry();
        builder.SetExtensionState(registry);     
    }
}
