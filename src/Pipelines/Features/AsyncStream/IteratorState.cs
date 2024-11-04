namespace Dazinator.Extensions.Pipelines.Features.Iterator;

public class IteratorState<T> : IIteratorState<T>
{
    public T CurrentItem { get; internal set; }
}


