namespace Dazinator.Extensions.Pipelines.Features.Idempotency;
using System.Collections.Concurrent;
using System.Threading.Tasks;

public class InMemoryIdempotencyStateManager : IIdempotencyStateManager
{
    private readonly ConcurrentDictionary<string, bool> _completedOperations = new();

    public Task MarkOperationCompleted(string operationKey)
    {
        _completedOperations.TryAdd(operationKey, true);
        return Task.CompletedTask;
    }

    public Task<bool> IsOperationCompleted(string operationKey)
    {
        return Task.FromResult(_completedOperations.ContainsKey(operationKey));
    }
}
