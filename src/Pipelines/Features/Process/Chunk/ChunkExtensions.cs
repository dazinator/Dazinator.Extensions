#pragma warning disable IDE0130 // for discoverability
namespace Dazinator.Extensions.Pipelines;
#pragma warning restore IDE0130 // for discoverability
using Dazinator.Extensions.Pipelines.Features.Process;
using Dazinator.Extensions.Pipelines.Features.Process.Chunk;

public static class ChunkExtensions
{   
    public static IPipelineBuilder WithChunks<T>(
        this IAwaitingItemsSource<T> builder,
        IEnumerable<T> items,
        int chunkSize,
        Action<ParallelOptions>? configureOptions = null)
    {
        var options = new ParallelOptions();
        configureOptions?.Invoke(options);

        return ((PipelineBuilder)builder).AddFilters(registry =>
        {
            registry.AddFilter(sp => new RunPerChunkFilter<T>(items, chunkSize, options));
        });
    }
}
