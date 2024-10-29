#pragma warning disable IDE0130 // Namespace does not match folder structure - for discoverability
namespace Dazinator.Extensions.Pipelines;
#pragma warning restore IDE0130 // Namespace does not match folder structure
using System;
using System.Threading.Tasks;
using Dazinator.Extensions.Pipelines.Features.Idempotency;
using Microsoft.Extensions.DependencyInjection;

public static class IdempotencyExtensions
{
    public static IPipelineBuilder WithIdempotency(
         this IPipelineBuilder builder,
         string key,
         Func<PipelineContext, Task<bool>>? checkCompleted = null)
    {
        return builder.AddFilters((registry) =>
        {
            registry.AddFilter(
                    sp => new IdempotencyFilter(
                    sp.GetRequiredService<IIdempotencyStateManager>(),
                    key,
                     checkCompleted));
        });
    }


    public static IPipelineBuilder WithIdempotency(
        this IPipelineBuilder builder,
        Action<IdempotencyOptions> configure)
    {
        var options = new IdempotencyOptions();
        configure(options);

        return builder.AddFilters((registry) =>
        {
            registry.AddFilter(
                sp => new IdempotencyFilter(
                    sp.GetRequiredService<IIdempotencyStateManager>(),
                    options.Key ?? string.Empty,
                     options.CheckCompleted));
        });

    }

}
