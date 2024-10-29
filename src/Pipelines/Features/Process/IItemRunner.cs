namespace Dazinator.Extensions.Pipelines.Features.Process;

/// <summary>
/// The runner:
/// Captures the branch creation/execution logic
/// Gets invoked by the filter for each item
/// Maintains the context for branch execution
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IItemRunner<T>
{
    void SetExecutionTask(IEnumerable<T> items, ParallelOptions options, PipelineContext context);
    Task ExecuteAsync(PipelineContext context, Action<ItemBranchBuilder<T>> configureBranch);
   // Task RunForItem(T item, PipelineContext context);
}
