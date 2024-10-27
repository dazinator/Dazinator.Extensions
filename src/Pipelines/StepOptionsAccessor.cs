namespace Dazinator.Extensions.Pipelines;

using Dazinator.Extensions.Pipelines.Features.StepOptions;

public class StepOptionsAccessor
{
    private readonly Dictionary<Type, IStepOptions> _options = new();

    public void SetOptions<T>(T options) where T : class, IStepOptions
    {
        _options[typeof(T)] = options;
    }

    public T? GetOptions<T>() where T : class, IStepOptions
    {
        return _options.TryGetValue(typeof(T), out var options) ? options as T : null;
    }
}
