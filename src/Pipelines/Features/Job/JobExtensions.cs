namespace Dazinator.Extensions.Pipelines;

using Dazinator.Extensions.Pipelines.Features.Job;
using Microsoft.Extensions.DependencyInjection;

public static class JobExtensions
{
    public static IPipelineBuilder RunJob<TJob>(this IPipelineBuilder builder,
         string? stepId = null)
        where TJob : IJob
    {
        return builder.Use(next => async context =>
        {
            // Resolve from execution scope
            var job = context.ServiceProvider.GetRequiredService<TJob>();
            await job.ExecuteAsync(context.CancellationToken);
            await next(context);
        }, stepId);
    }

    public static IPipelineBuilder TryRunJob<TJob>(
       this IPipelineBuilder builder,
       Action<Exception>? onError = null,
       string? stepId = null)
       where TJob : IJob
    {
        return builder.Use(next => async context =>
        {
            try
            {
                var job = context.ServiceProvider.GetRequiredService<TJob>();
                await job.ExecuteAsync(context.CancellationToken);
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex);
            }
            await next(context);
        }, stepId);
    }
}
