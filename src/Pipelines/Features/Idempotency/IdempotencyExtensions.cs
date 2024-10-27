#pragma warning disable IDE0130 // Namespace does not match folder structure - for discoverability
namespace Dazinator.Extensions.Pipelines;
#pragma warning restore IDE0130 // Namespace does not match folder structure
using System;
using System.Threading.Tasks;
using Dazinator.Extensions.Pipelines.Features.Idempotency;

public static class IdempotencyExtensions
{
    public static PipelineBuilder WithIdempotency(
        this PipelineBuilder builder,
        Action<IdempotencyOptions> configure)
    {
        // Using the core Configure mechanism under the hood
        return builder.Configure<IdempotencyOptions>(options =>
        {
            configure(options);
        });
    }

    // Overload for async check configuration
    public static PipelineBuilder WithIdempotency(
        this PipelineBuilder builder,
        string key,
        Func<PipelineContext, Task<bool>> checkCompleted)
    {
        return builder.Configure<IdempotencyOptions>(options =>
        {
            options.Key = key;
            options.CheckCompleted = checkCompleted;
        });
    }
}
