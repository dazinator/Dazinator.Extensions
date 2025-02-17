#pragma warning disable IDE0130 // Namespace does not match folder structure - for discoverability
namespace Dazinator.Extensions.Pipelines;
#pragma warning restore IDE0130 // Namespace does not match folder structure

using Microsoft.Extensions.DependencyInjection;

public static class NewScopeExtensions
{
    public static IPipelineBuilder UseNewScope(this IPipelineBuilder builder, string? stepId = null)
    {
        builder.Use(next => async context =>
        {
            await using var scope = context.ServiceProvider.CreateAsyncScope();
            var original = context.ServiceProvider;
            // Create new context with scoped provider
            try
            {
                context.ServiceProvider = scope.ServiceProvider;
                //var scopedContext = new PipelineContext
                //{
                //    ServiceProvider = scope.ServiceProvider,
                //    CancellationToken = context.CancellationToken
                //};
                await next(context);
            }
            finally
            {
                context.ServiceProvider = original;
            }
        }, stepId);
        return builder;
    }
}
