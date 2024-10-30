namespace Dazinator.Extensions.Pipelines.Features.Filter.Utils;

public static class FilterCallbackExtensions
{
    public static IFilterCallback<TArg>? GetFilterCallback<TArg>(this PipelineContext context)
    {
        return context.GetStepState<IFilterCallback<TArg>>();
    }

    public static void SetFilterCallback<TArg>(this PipelineContext context, IFilterCallback<TArg> filterCallback)       
    {
        context.SetStepState<IFilterCallback<TArg>>(filterCallback);
    }

    public static async Task ExecuteFilterCallback<TArg>(this PipelineContext context, TArg argument)
    {
        var filterCallback = context.GetStepState<IFilterCallback<TArg>>()
            ?? throw new InvalidOperationException("No filter callback found. Did you forget to add a filter that provides a callback?");
        await filterCallback.ExecuteAsync(argument);
    }
    public static async Task<bool> TryExecuteFilterCallback<TArg>(this PipelineContext context, TArg argument)
    {
        var filterCallback = context.GetStepState<IFilterCallback<TArg>>();
        if (filterCallback is null)
        {
            return false;
        }
        await filterCallback.ExecuteAsync(argument);
        return true;
    }
}
