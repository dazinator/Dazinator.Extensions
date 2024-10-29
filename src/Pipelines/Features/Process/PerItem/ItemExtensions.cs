#pragma warning disable IDE0130 // here for discoverability
namespace Dazinator.Extensions.Pipelines;
#pragma warning restore IDE0130 // here for discoverability
using Dazinator.Extensions.Pipelines.Features.Process;
using Dazinator.Extensions.Pipelines.Features.Process.PerItem;

public static class ItemExtensions
{
    public static IPipelineBuilder WithItems<T>(
      this IAwaitingItemSource<T> builder,
      IEnumerable<T> items,
      Action<ParallelOptions>? configureOptions = null)
    {
        var options = new ParallelOptions();
        configureOptions?.Invoke(options);

        return (builder).AddFilters(registry =>
        {
            registry.AddFilter(sp => new RunPerItemFilter<T>(items, options));
        });
    }
}
