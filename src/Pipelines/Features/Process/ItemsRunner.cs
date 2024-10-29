namespace Dazinator.Extensions.Pipelines.Features.Process;

using System;

internal class ItemsRunner<T> : IItemsRunner<T>
{
    private readonly PipelineContext _originalContext;
    private readonly Func<IEnumerable<T>, PipelineContext, Task> _runBranch;

    public ItemsRunner(
        PipelineContext originalContext,
        Func<IEnumerable<T>, PipelineContext, Task> runBranch)
    {
        _originalContext = originalContext;
        _runBranch = runBranch;
    }

    public Task RunForItems(IEnumerable<T> items, PipelineContext context)
    {
        return _runBranch(items, context);
    }
}
