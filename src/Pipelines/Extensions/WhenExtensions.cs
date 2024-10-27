#pragma warning disable IDE0130 // Namespace does not match folder structure - for discoverability
namespace Dazinator.Extensions.Pipelines;
#pragma warning restore IDE0130 // Namespace does not match folder structure

using System.Threading.Tasks;

public static class WhenExtensions
{
    public static PipelineBuilder When(this PipelineBuilder builder,
    Func<PipelineContext, bool> predicate,
    Func<PipelineContext, Task> action)
    {
        builder.Add((sp, next) => async context =>
        {
            if (predicate(context))
            {
                await action(context);
            }
            await next(context);
        });
        return builder;
    }

    // Async predicate
    public static PipelineBuilder When(this PipelineBuilder builder,
        Func<PipelineContext, Task<bool>> predicate,
        Func<PipelineContext, Task> action)
    {
        builder.Add((sp, next) => async context =>
        {
            if (await predicate(context))
            {
                await action(context);
            }
            await next(context);
        });
        return builder;
    }

    public static PipelineBuilder TryWhen(
    this PipelineBuilder builder,
    Func<PipelineContext, bool> predicate,
    Func<PipelineContext, Task> action,
    Action<Exception>? onError = null)
    {
        builder.Add((sp, next) => async context =>
        {
            try
            {
                if (predicate(context))
                {
                    await action(context);
                }
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex);
            }
            await next(context);
        });
        return builder;
    }

    public static PipelineBuilder TryWhen(
    this PipelineBuilder builder,
    Func<PipelineContext, Task<bool>> predicate,
    Func<PipelineContext, Task> action,
    Action<Exception>? onError = null)
    {
        builder.Add((sp, next) => async context =>
        {
            try
            {
                if (await predicate(context))
                {
                    await action(context);
                }
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex);
            }
            await next(context);
        });
        return builder;
    }

}
