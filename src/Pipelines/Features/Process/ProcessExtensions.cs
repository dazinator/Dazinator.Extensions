#pragma warning disable IDE0130 // Extensions placed in this namespace for ease of discoverability
namespace Dazinator.Extensions.Pipelines;
#pragma warning restore IDE0130 // Extensions placed in this namespace for ease of discoverability

using System;
using Dazinator.Extensions.Pipelines.Features.Process;

// Extension methods
public static class ProcessExtensions
{
    /// <summary>
    /// Configures a step to process individual items using a branch per item.
    /// </summary>
    public static IAwaitingItemSource<T> ProcessItem<T>(
        this IPipelineBuilder builder,
        Action<ItemBranchBuilder<T>> configureBranch,
        string? stepId = null)
    {
        builder.Use(next => async context =>
        {
            // We get the runner set by the our companion surrounding filter.
            // We execute the task it has prepared for us based on its settings like level of concurrency,
            // and we give it the configureBranch action so it can create concrrent branches for each item as needed.
            var itemsRunner = context.GetItemRunner<T>();       
            await itemsRunner.ExecuteAsync(context, configureBranch);
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
    public static IAwaitingItemsSource<T> ProcessItems<T>(
        this IPipelineBuilder builder,
        Action<ItemsBranchBuilder<T>> configureBranch,
        string? stepId = null)
    {
        builder.Use(next => async context =>
        {
            var runner = new ItemsRunner<T>(
                context,
                (items, parentContext) => parentContext.ParentPipeline.RunBranch(
                    parentContext,
                    branch => configureBranch(new ItemsBranchBuilder<T>(branch, items))));

            context.SetStepState(runner);
            await next(context);
        }, stepId);

        return new AwaitingItemsSource<T>(builder);
    }


}
