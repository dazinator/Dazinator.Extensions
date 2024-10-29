namespace Dazinator.Extensions.Pipelines.Features.Process.Chunk;
using Dazinator.Extensions.Pipelines.Features.Filter;
using Dazinator.Extensions.Pipelines.Features.Process;

public class RunPerChunkFilter<T> : IStepFilter
{
    private readonly IEnumerable<T> _items;
    private readonly int _chunkSize;
    private readonly ParallelOptions _options;

    public RunPerChunkFilter(IEnumerable<T> items, int chunkSize, ParallelOptions options)
    {
        _items = items;
        _chunkSize = chunkSize;
        _options = options;
    }

    public async Task BeforeStepAsync(PipelineStepContext context)
    {
        var runner = context.PipelineContext.GetExtensionState<IItemsRunner<T>>();       
        var chunks = _items.Chunk(_chunkSize);

        await Parallel.ForEachAsync(
            chunks,
            _options,
            async (chunk, ct) => await runner.RunForItems(chunk, context.PipelineContext));

        context.ShouldSkip = true;
    }

    public Task AfterStepAsync(PipelineStepContext context) => Task.CompletedTask;
}
