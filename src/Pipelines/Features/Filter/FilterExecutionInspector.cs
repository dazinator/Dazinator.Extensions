namespace Dazinator.Extensions.Pipelines.Features.Filter;
using System.Threading.Tasks;

public class FilterExecutionInspector : IPipelineInspector
{
    public FilterRegistry FilterRegistry { get; }

    internal FilterExecutionInspector(FilterRegistry filterRegistry)
    {
       FilterRegistry = filterRegistry;
    }

    public async Task BeforeStepAsync(PipelineStepContext context)
    {     

        var filters = FilterRegistry.GetFilters(context.ServiceProvider, context.PipelineContext.CurrentStepIndex);
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
        var filters = FilterRegistry.GetFilters(context.ServiceProvider, context.PipelineContext.CurrentStepIndex);
        // Execute After steps in reverse order
        // this models the behaviour of middleware in terms of the first filter to run, is the "outer" one, meaning it should also be the last to complete.
        foreach (var filter in filters.Reverse())
        {
            await filter.AfterStepAsync(context);
        }      
    }

    public Task OnExceptionAsync(PipelineStepContext context) => Task.CompletedTask;
}
