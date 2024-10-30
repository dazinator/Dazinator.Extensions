namespace Dazinator.Extensions.Pipelines.Features.Process.Chunk;
using Dazinator.Extensions.Pipelines.Features.Filter;
using Dazinator.Extensions.Pipelines.Features.Filter.Utils;
using Dazinator.Extensions.Pipelines.Features.Process;
// Define descriptive type aliases

public class BranchPerChunkFilter<T> : IStepFilter
{

    private readonly IEnumerable<T> _items;
    private readonly int _chunkSize;
    private readonly ParallelOptions _options;
    private Lazy<Func<Action<ItemBranchBuilder<T[]>>, Task>> _lazyExecutionTask;

    public BranchPerChunkFilter(IEnumerable<T> items, int chunkSize, ParallelOptions options)
    {
        _items = items;
        _chunkSize = chunkSize;
        _options = options;
    }

    public Task BeforeStepAsync(PipelineStepContext context)
    {
        _lazyExecutionTask = new Lazy<Func<Action<ItemBranchBuilder<T[]>>, Task>>(() =>
       (configureBranch) =>
       {
           var chunks = _items.Chunk(_chunkSize);
           return Parallel.ForEachAsync(chunks, _options, async (item, ct) => await ProcessItem(item, context.PipelineContext, configureBranch));
       });

        var filterCallback = new FilterCallback<Action<ItemBranchBuilder<T[] >>>(_lazyExecutionTask.Value);
        context.PipelineContext.SetFilterCallback(filterCallback);            
       
        return Task.CompletedTask;
    }

    private async Task ProcessItem(T[] items, PipelineContext pipelineContext, Action<ItemBranchBuilder<T[]>> configureBranch)
    {
        if (configureBranch is null)
        {
            throw new InvalidOperationException("No configureBranch action. Did you forget to configure the branch?");
        }

        await pipelineContext.ParentPipeline.RunBranch(
            pipelineContext,
            branch => configureBranch(new ItemBranchBuilder<T[]>(branch, items)));
    }

    public Task AfterStepAsync(PipelineStepContext context) => Task.CompletedTask;
}
