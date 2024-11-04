namespace Dazinator.Extensions.Pipelines.Features.Iterator;

// Option 2: Generic Iterator State Holder
public interface IIteratorState<T>
{
    T CurrentItem { get; }
}


