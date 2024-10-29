namespace Dazinator.Extensions.Pipelines.Features.Process.PerItem;
using Dazinator.Extensions.Pipelines.Features.Filter;
using Dazinator.Extensions.Pipelines.Features.Process;

// Filters
public class RunPerItemFilter<T> : IStepFilter
{
    private readonly IEnumerable<T> _items;
    private readonly ParallelOptions _options;

    public RunPerItemFilter(IEnumerable<T> items, ParallelOptions options)
    {
        _items = items;
        _options = options;
    }

    public Task BeforeStepAsync(PipelineStepContext context)
    {
        var runner = new ItemRunner<T>();
        context.PipelineContext.SetStepState<IItemRunner<T>>(runner);      
        runner.SetExecutionTask(_items, _options, context.PipelineContext); // sets the task ready to execute but doesn't start it because we can't invokeit without also having an action to configure the branch to run for each item. This is configured in the middleware step wrapped by the filter.
        return Task.CompletedTask;
    }  

    public Task AfterStepAsync(PipelineStepContext context) => Task.CompletedTask;
}
