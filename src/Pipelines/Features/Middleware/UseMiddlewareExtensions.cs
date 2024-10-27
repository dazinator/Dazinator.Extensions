
#pragma warning disable IDE0130 // Namespace does not match folder structure - for discoverability
namespace Dazinator.Extensions.Pipelines;
#pragma warning restore IDE0130 // Namespace does not match folder structure
using Microsoft.Extensions.DependencyInjection;

public static class UseMiddlewareExtensions
{
    /// <summary>
    /// Use a service registered in the service provider as a middleware. It will be resolved from whatever the current context DI scope is.
    /// </summary>
    /// <typeparam name="TMiddleware"></typeparam>
    /// <param name="builder"></param>
    /// <param name="stepId"></param>
    /// <returns></returns>
    public static PipelineBuilder UseMiddleware<TMiddleware>(this PipelineBuilder builder, string? stepId = null)
        where TMiddleware : IPipelineMiddleware
    {
        builder.Add(next => async context =>
        {
            var middleware = context.ServiceProvider.GetRequiredService<TMiddleware>();
            await middleware.ExecuteAsync(next, context);
        },
        stepId ?? typeof(TMiddleware).Name,
        typeof(TMiddleware).Name);

        return builder;
    }


#if NET8_0
    // New method that supports keyed middleware
    public static PipelineBuilder UseMiddleware<TMiddleware>(this PipelineBuilder builder, string key, string? stepId = null)
        where TMiddleware : IPipelineMiddleware
    {
        builder.Add(next => async context =>
        {
            var middleware = context.ServiceProvider.GetRequiredKeyedService<TMiddleware>(key);
            await middleware.ExecuteAsync(next, context);
        },
       stepId ?? typeof(TMiddleware).Name,
       typeof(TMiddleware).Name);

        return builder;
    }
#endif

}
