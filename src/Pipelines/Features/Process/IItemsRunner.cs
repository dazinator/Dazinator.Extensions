namespace Dazinator.Extensions.Pipelines.Features.Process;

/// <summary>
/// The runner:
/// Captures the branch creation/execution logic
/// Gets invoked by the filter for each set of items
/// Maintains the context for branch execution
/// </summary>
/// <typeparam name="T"></typeparam>

public interface IItemsRunner<T>
{
    Task RunForItems(IEnumerable<T> items, PipelineContext context);
}
