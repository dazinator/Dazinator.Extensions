#pragma warning disable IDE0130 // for discoverability
namespace Dazinator.Extensions.Pipelines.Features.Branching.Chunk;

using Dazinator.Extensions.Pipelines.Features.Branching;
#pragma warning restore IDE0130 // for discoverability

public static class BranchPerChunkExtensions
{
    public static IPipelineBuilder WithChunks<T>(
        this IAwaitingItemsSource<T> builder,
        IEnumerable<T> items,
        int chunkSize,
        Action<ParallelOptions>? configureOptions = null)
    {
        var options = new ParallelOptions();
        configureOptions?.Invoke(options);

        return builder.AddFilters(registry =>
        {
            registry.AddFilter(sp => new BranchPerChunkFilter<T>(items, chunkSize, options));
        });
    }
}
