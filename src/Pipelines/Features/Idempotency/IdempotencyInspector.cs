namespace Dazinator.Extensions.Pipelines.Features.Idempotency;
using System.Threading.Tasks;
using Dazinator.Extensions.Pipelines;

public class IdempotencyInspector : IPipelineInspector
{
    private readonly IIdempotencyStateManager _stateManager;

    public IdempotencyInspector(IIdempotencyStateManager stateManager)
    {
        _stateManager = stateManager;
    }

    public async Task BeforeStepAsync(PipelineStepContext context)
    {
        var options = context.PipelineContext.GetStepState<IdempotencyOptions>();
        if (options?.Key != null)
        {
            if (await _stateManager.IsOperationCompleted(options.Key) ||
                (options.CheckCompleted != null &&
                 await options.CheckCompleted(context.PipelineContext)))
            {
                context.ShouldSkip = true;
            }
        }
    }

    public async Task AfterStepAsync(PipelineStepContext context)
    {
        var options = context.PipelineContext.GetStepState<IdempotencyOptions>();
        if (options?.Key != null && !context.ShouldSkip)
        {
            await _stateManager.MarkOperationCompleted(options.Key);
        }
    }

    public Task OnExceptionAsync(PipelineStepContext context) => Task.CompletedTask;
}
