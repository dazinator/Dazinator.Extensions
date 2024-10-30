namespace Dazinator.Extensions.Pipelines.Features.Filter.Utils;

/// <summary>
/// A callback that can be used to execute a task that is supplied by a filter,and that requires an argument to be supplied when it is executed.
/// </summary>
/// <typeparam name="TArg"></typeparam>
public interface IFilterCallback<TArg>
{
    Task ExecuteAsync(TArg argument);
}
