#pragma warning disable IDE0130 // Extensions placed in this namespace for ease of discoverability
namespace Dazinator.Extensions.Pipelines;
#pragma warning restore IDE0130 // Extensions placed in this namespace for ease of discoverability

using System;
using Dazinator.Extensions.Pipelines.Features.Branching;
using Dazinator.Extensions.Pipelines.Features.Filter.Utils;


// Extension methods
public static class BranchPerInputExtensions
{
    /// <summary>
    /// Configures a step to process individual items using a branch per item.
    /// </summary>
    public static IAwaitingItemSource<T> UseBranchPerInput<T>(
        this IPipelineBuilder builder,
        Action<ItemBranchBuilder<T>> configureBranch,
        string? stepId = null)
    {
        // builder.UseMiddleware(new ProcessItemMiddleware<T>(configureBranch), stepId);
        builder.Use(next => async context =>
        {
            var filterCallback = context.GetFilterCallback<Action<ItemBranchBuilder<T>>>();
            if (filterCallback is null)
            {
                throw new InvalidOperationException("No filter callback set on context. Did you forget to add WithItems() etc?");
            }          

            await filterCallback.ExecuteAsync(configureBranch);
            await next(context);

        }, stepId);

        // We return a class purely to guide the fluent API, so that the user can call WithItem() on the result to add the filter that supplies an Item to the runner at a time,
        // and not incompatible Filter that expects to supply a collection of items at a time. See below methods for Collection variants.
        return new AwaitingItemSource<T>(builder);
    }



    /// <summary>
    /// Configures a step to process multiple items using a branch per set of items.
    /// </summary>
    /// <remarks>You must now chain an appropriate With Filter to supply the items and concurrency options.</remarks>
    public static IAwaitingItemsSource<T> UseBranchPerInputs<T>(
        this IPipelineBuilder builder,
        Action<ItemBranchBuilder<T[]>> configureBranch,
        string? stepId = null)
    {
        builder.Use(next => async context =>
        {
            var filterCallback = context.GetStepState<IFilterCallback<Action<ItemBranchBuilder<T[]>>>>()
             ?? throw new InvalidOperationException("No filter callback set on context. Did you forget to add WithItems() etc?");

            await filterCallback.ExecuteAsync(configureBranch);
            await next(context);

        }, stepId);      

        return new AwaitingItemsSource<T>(builder);
    }
}
