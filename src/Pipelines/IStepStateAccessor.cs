namespace Dazinator.Extensions.Pipelines;

// Core interfaces in the pipeline system
public interface IStepStateAccessor
{
    void Set<T>(T state) where T : class;
    T? Get<T>() where T : class;
}

