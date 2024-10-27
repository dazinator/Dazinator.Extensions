#pragma warning disable IDE0130 // Namespace does not match folder structure - for discoverability
namespace Dazinator.Extensions.Pipelines;
#pragma warning restore IDE0130 // Namespace does not match folder structure

using System.Threading.Tasks;

public static class WhenExtensions
{

    // Single async implementation
    public static PipelineBuilder When(
        this PipelineBuilder builder,
        Func<PipelineContext, Task<bool>> predicate,
        Func<PipelineContext, Task> action,
        string? stepId = null)
    {
        builder.Add(next => async context =>
        {
            if (await predicate(context))
            {
                await action(context);
            }
            await next(context);
        }, stepId);
        return builder;
    }

    // Sync helpers
    public static PipelineBuilder When(
        this PipelineBuilder builder,
        Func<PipelineContext, bool> predicate,
        Func<PipelineContext, Task> action,
        string? stepId = null) =>
        builder.When(
            ctx => Task.FromResult(predicate(ctx)),
            action,
            stepId);


    // Single async implementation
    public static PipelineBuilder TryWhen(
        this PipelineBuilder builder,
        Func<PipelineContext, Task<bool>> predicate,
        Func<PipelineContext, Task> action,
        Action<Exception>? onError = null,
        string? stepId = null)
    {
        builder.Add(next => async context =>
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
        }, stepId);
        return builder;
    }

    // Sync helper
    public static PipelineBuilder TryWhen(
        this PipelineBuilder builder,
        Func<PipelineContext, bool> predicate,
        Func<PipelineContext, Task> action,
        Action<Exception>? onError = null,
        string? stepId = null) =>
        builder.TryWhen(
            ctx => Task.FromResult(predicate(ctx)),
            action,
            onError,
            stepId);
}


