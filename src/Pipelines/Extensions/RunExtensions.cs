#pragma warning disable IDE0130 // Namespace does not match folder structure - for discoverability
namespace Dazinator.Extensions.Pipelines;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class RunExtensions
{

    public static PipelineBuilder Run(this PipelineBuilder builder, Action action, string stepId = null)
    {
        builder.Add(next => async context =>
        {
            action();
            await next(context);
        }, stepId, nameof(Run));
        return builder;
    }


    public static PipelineBuilder Run(this PipelineBuilder builder, Action<PipelineContext> action, string? stepId = null)
    {
        builder.Add(next => async context =>
        {
            action(context);
            await next(context);
        }, stepId, nameof(Run));
        return builder;
    }

    public static PipelineBuilder RunAsync(this PipelineBuilder builder, Func<Task> action, string? stepId = null)
    {
        builder.Add(next => async context =>
        {
            await action();
            await next(context);
        }, stepId, nameof(RunAsync));
        return builder;
    }


    public static PipelineBuilder RunAsync(this PipelineBuilder builder, Func<PipelineContext, Task> action, string? stepId = null)
    {
        builder.Add(next => async context =>
        {
            await action(context);
            await next(context);
        }, stepId, nameof(RunAsync));
        return builder;
    }

    public static PipelineBuilder TryRun(this PipelineBuilder builder, Action action, Action<Exception>? onError = null, string? stepId = null)
    {
        builder.Add(next => async context =>
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
        }, stepId, nameof(TryRun));
        return builder;
    }


    public static PipelineBuilder TryRun(this PipelineBuilder builder, Action<PipelineContext> action, Action<Exception>? onError = null, string? stepId = null)
    {
        builder.Add(next => async context =>
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
        }, stepId, nameof(TryRun));
        return builder;      
    }

    public static PipelineBuilder TryRunAsync(this PipelineBuilder builder, Func<PipelineContext, Task> action, Action<Exception>? onError = null, string? stepId = null)
    {
        builder.Add(next => async context =>
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
        }, stepId, nameof(TryRunAsync));
        return builder;
    }


}
