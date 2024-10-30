namespace Dazinator.Extensions.Pipelines.Features.Process.PerItem;

using Dazinator.Extensions.Pipelines.Features.Filter;
using Dazinator.Extensions.Pipelines.Features.Filter.Utils;
using Dazinator.Extensions.Pipelines.Features.Process;

// Filters
public class BranchPerItemFilter<T> : IStepFilter
{
    private readonly IEnumerable<T> _items;
    private readonly ParallelOptions _options;
    private Lazy<Func<Action<ItemBranchBuilder<T>>, Task>>? _lazyExecutionTask;

    public BranchPerItemFilter(IEnumerable<T> items, ParallelOptions options)
    {
        _items = items;
        _options = options;
    }

    public Task BeforeStepAsync(PipelineStepContext context)
    {

        _lazyExecutionTask = new Lazy<Func<Action<ItemBranchBuilder<T>>, Task>>(() =>
        (configureBranch) =>
        {

            return Parallel.ForEachAsync(_items, _options, async (item, ct) => await ProcessItem(item, context.PipelineContext, configureBranch));

        });

        var filterCallback = new FilterCallback<Action<ItemBranchBuilder<T>>>(_lazyExecutionTask.Value);
        context.PipelineContext.SetFilterCallback(filterCallback);
        return Task.CompletedTask;
    }




    //var runner = new ItemRunner<T>();
    //  context.PipelineContext.SetStepState<IItemRunner<T>>(runner);      
    //  runner.SetExecutionTask(_items, _options, context.PipelineContext); // sets the task ready to execute but doesn't start it because we can't invokeit without also having an action to configure the branch to run for each item. This is configured in the middleware step wrapped by the filter.
    //  return Task.CompletedTask;


    private async Task ProcessItem(T item, PipelineContext pipelineContext, Action<ItemBranchBuilder<T>> configureBranch)
    {
        if (configureBranch is null)
        {
            throw new InvalidOperationException("No configureBranch action. Did you forget to configure the branch?");
        }

        await pipelineContext.ParentPipeline.RunBranch(
            pipelineContext,
            branch => configureBranch(new ItemBranchBuilder<T>(branch, item)));
    }


    public Task AfterStepAsync(PipelineStepContext context) => Task.CompletedTask;
}
