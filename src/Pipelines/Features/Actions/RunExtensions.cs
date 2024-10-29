#pragma warning disable IDE0130 // Namespace does not match folder structure - for discoverability
namespace Dazinator.Extensions.Pipelines;
#pragma warning restore IDE0130 // Namespace does not match folder structure
public static class RunExtensions
{

    public static IPipelineBuilder Run(this IPipelineBuilder builder, Action action, string? stepId = null)
    {
        builder.Use(next => async context =>
        {
            action();
            await next(context);
        }, stepId);
        return builder;
    }


    public static IPipelineBuilder Run(this IPipelineBuilder builder, Action<PipelineContext> action, string? stepId = null)
    {
        builder.Use(next => async context =>
        {
            action(context);
            await next(context);
        }, stepId);
        return builder;
    }

    public static IPipelineBuilder RunAsync(this IPipelineBuilder builder, Func<Task> action, string? stepId = null)
    {
        builder.Use(next => async context =>
        {
            await action();
            await next(context);
        }, stepId);
        return builder;
    }


    public static IPipelineBuilder RunAsync(this IPipelineBuilder builder, Func<PipelineContext, Task> action, string? stepId = null)
    {
        builder.Use(next => async context =>
        {
            await action(context);
            await next(context);
        }, stepId);
        return builder;
    }

    public static IPipelineBuilder TryRun(this IPipelineBuilder builder, Action action, Action<Exception>? onError = null, string? stepId = null)
    {
        builder.Use(next => async context =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex);
            }

            await next(context);
        }, stepId);
        return builder;
    }


    public static IPipelineBuilder TryRun(this IPipelineBuilder builder, Action<PipelineContext> action, Action<Exception>? onError = null, string? stepId = null)
    {
        builder.Use(next => async context =>
        {
            try
            {
                action(context);
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex);
            }
            await next(context);
        }, stepId);
        return builder;
    }

    public static IPipelineBuilder TryRunAsync(this IPipelineBuilder builder, Func<PipelineContext, Task> action, Action<Exception>? onError = null, string? stepId = null)
    {
        builder.Use(next => async context =>
        {
            try
            {
                await action(context);
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex);
            }
            await next(context);
        }, stepId);
        return builder;
    }


}
