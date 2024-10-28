#pragma warning disable IDE0130 // Namespace does not match folder structure - for discoverability
namespace Dazinator.Extensions.Pipelines;
#pragma warning restore IDE0130 // Namespace does not match folder structure
using System;
using System.Threading.Tasks;
using Dazinator.Extensions.Pipelines.Features.Filter;
using Dazinator.Extensions.Pipelines.Features.Idempotency;

// Example idempotency implementation as a filter
public class IdempotencyFilter : IStepFilter
{
    private readonly IIdempotencyStateManager _stateManager;
    private readonly string _key;
    private readonly Func<PipelineContext, Task<bool>>? _checkCompleted;

    public IdempotencyFilter(IIdempotencyStateManager stateManager, string key, Func<PipelineContext, Task<bool>>? checkCompleted = null)
    {
        _stateManager = stateManager;
        _key = key;
        _checkCompleted = checkCompleted;
    }

    public async Task BeforeStepAsync(PipelineStepContext context)
    {
        if (await _stateManager.IsOperationCompleted(_key) ||
            (_checkCompleted != null && await _checkCompleted(context.PipelineContext)))
        {
            context.ShouldSkip = true;
        }
    }

    public async Task AfterStepAsync(PipelineStepContext context)
    {
        if (!context.ShouldSkip)
        {
            await _stateManager.MarkOperationCompleted(_key);
        }
    }
}
