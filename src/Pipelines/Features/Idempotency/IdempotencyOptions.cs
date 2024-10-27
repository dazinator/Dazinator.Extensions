namespace Dazinator.Extensions.Pipelines.Features.Idempotency;
using System.Threading.Tasks;
using Dazinator.Extensions.Pipelines;
using Pipelines.Features.StepOptions;

public class IdempotencyOptions : IStepOptions
{
    public string? Key { get; set; }
    public Func<PipelineContext, Task<bool>>? CheckCompleted { get; set; }
}
