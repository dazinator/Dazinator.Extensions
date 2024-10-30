namespace Dazinator.Extensions.Pipelines.Features.Filter.Utils;

using System;

public class FilterCallback<TArg> : IFilterCallback<TArg>
{
    private readonly Func<TArg, Task> _onExecute;

    public FilterCallback(Func<TArg, Task> onExecute)
    {
        //if (_lazyExecutionTask == null)
        //{
        //    throw new InvalidOperationException("No execution task set. Did you forget to add WithItems()?");
        //}      
        _onExecute = onExecute;
    }

    public async Task ExecuteAsync(TArg arg)
    {
        await _onExecute(arg);
    }

}

