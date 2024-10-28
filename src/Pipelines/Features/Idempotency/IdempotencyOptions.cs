namespace Dazinator.Extensions.Pipelines.Features.Idempotency;
using System.Threading.Tasks;
using Dazinator.Extensions.Pipelines;

public class IdempotencyOptions 
{
    public string? Key { get; set; }
    public Func<PipelineContext, Task<bool>>? CheckCompleted { get; set; }
}
