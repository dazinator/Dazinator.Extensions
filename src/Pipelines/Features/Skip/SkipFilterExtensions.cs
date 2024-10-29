#pragma warning disable IDE0130 // Namespace does not match folder structure - for discoverability
namespace Dazinator.Extensions.Pipelines.Features.Skip;
#pragma warning restore IDE0130 // Namespace does not match folder structure

using System.Threading.Tasks;

// Or perhaps more descriptive:
public static class SkipFilterExtensions
{
    public static IPipelineBuilder WithSkipCondition(
      this IPipelineBuilder builder,
      bool shouldSkip)
    {
        return builder.AddFilters(registry =>
        {
            registry.AddFilter(sp => new SkipConditionFilter(ctx =>
                Task.FromResult(shouldSkip)));
        });
    }

    public static IPipelineBuilder WithSkipCondition(
       this IPipelineBuilder builder,
       Func<bool> shouldSkip)
    {
        return builder.AddFilters(registry =>
        {
            registry.AddFilter(sp => new SkipConditionFilter(ctx =>
                Task.FromResult(shouldSkip())));
        });
    }


    public static IPipelineBuilder WithSkipCondition(
        this IPipelineBuilder builder,
        Func<PipelineContext, bool> shouldSkip)
    {
        return builder.AddFilters(registry =>
        {
            registry.AddFilter(sp => new SkipConditionFilter(ctx =>
                Task.FromResult(shouldSkip(ctx))));
        });
    }

    public static IPipelineBuilder WithSkipConditionAsync(
        this IPipelineBuilder builder,
        Func<Task<bool>> shouldSkip)
    {
        return builder.AddFilters(registry =>
        {
            registry.AddFilter(sp => new SkipConditionFilter((ctx) => shouldSkip()));
        });
    }

    public static IPipelineBuilder WithSkipConditionAsync(
        this IPipelineBuilder builder,
        Func<PipelineContext, Task<bool>> shouldSkip)
    {
        return builder.AddFilters(registry =>
        {
            registry.AddFilter(sp => new SkipConditionFilter(shouldSkip));
        });
    }

    public static IPipelineBuilder WithTrySkipConditionAsync(
       this IPipelineBuilder builder,
       Func<PipelineContext, Task<bool>> condition,
       Action<Exception>? onError = null)
    {
        return builder.AddFilters(registry =>
        {
            registry.AddFilter(sp => new TrySkipConditionFilter(condition, onError));
        });
    }

    public static IPipelineBuilder WithSkipCondition(
        this IPipelineBuilder builder,
        Func<PipelineContext, bool> shouldSkip,
        Action<Exception>? onError = null)
    {
        return builder.WithTrySkipConditionAsync(
            ctx => Task.FromResult(shouldSkip(ctx)),
            onError);
    }

}
