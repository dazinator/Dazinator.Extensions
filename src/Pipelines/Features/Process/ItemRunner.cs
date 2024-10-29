namespace Dazinator.Extensions.Pipelines.Features.Process;

using System;
using Microsoft.Extensions.Options;

public class ItemRunner<T> : IItemRunner<T>
{
    private Action<ItemBranchBuilder<T>>? _configureBranch;
    private Lazy<Task> _lazyExecutionTask;

    public ItemRunner()
    {

    }

    public void SetExecutionTask(IEnumerable<T> items, ParallelOptions options, PipelineContext context)
    {
        _lazyExecutionTask = new Lazy<Task>(() =>
        Parallel.ForEachAsync(
            items,
            options,
            async (item, ct) => await ProcessItem(item, context))
    );

    }

    public async Task ExecuteAsync(PipelineContext context, Action<ItemBranchBuilder<T>> configureBranch)
    {
        _configureBranch = configureBranch ?? throw new InvalidOperationException("No configureBranch action. Did you forget to configure the branch?");

        if (_lazyExecutionTask == null)
        {
            throw new InvalidOperationException("No execution task set. Did you forget to add WithItems()?");
        }

        await _lazyExecutionTask.Value;
    }

    private async Task ProcessItem(T item, PipelineContext pipelineContext)
    {
        if (_configureBranch is null)
        {
            throw new InvalidOperationException("No configureBranch action. Did you forget to configure the branch?");
        }

        await pipelineContext.ParentPipeline.RunBranch(
            pipelineContext,
            branch => _configureBranch(new ItemBranchBuilder<T>(branch, item)));
    }
}

