namespace Dazinator.Extensions.Pipelines.Features.Idempotency;
using System.Threading.Tasks;

public interface IIdempotencyStateManager
{
    Task MarkOperationCompleted(string operationKey);
    Task<bool> IsOperationCompleted(string operationKey);
}
