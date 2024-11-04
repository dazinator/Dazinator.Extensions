namespace Dazinator.Extensions.Pipelines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dazinator.Extensions.Pipelines.Features.Iterator;

public static class AsyncStreamExtensions
{
    /// <summary>
    /// Repeats the downstream pipeline for each item from an IAsyncEnumerable source.
    /// </summary>   
    public static IPipelineBuilder UseAsyncStream<T>(
        this IPipelineBuilder builder,
        Func<PipelineContext, IAsyncEnumerable<T>> getSource,
        string? stepId = null)
    {
        builder.Use(next => async context =>
        {
            // Create state holder once at the start
            var iteratorState = new IteratorState<T>();
            context.SetStepState<IIteratorState<T>>(iteratorState);

            try
            {
                var source = getSource(context);
                await foreach (var item in source.WithCancellation(context.CancellationToken))
                {
                    // Update the current item - no boxing for value types
                    iteratorState.CurrentItem = item;
                    await next(context);
                }
            }
            finally
            {
                // Clean up
                context.SetStepState<IIteratorState<T>>(null);
            }
        }, stepId);

        return builder;
    }

    /// <summary>
    /// Gets the current item being processed by the iterator.
    /// </summary>
    public static T GetCurrentItem<T>(this PipelineContext context)
    {
        var state = context.GetStepState<IIteratorState<T>>()
            ?? throw new InvalidOperationException("No iterator context found. Are you calling this from within an iterator pipeline?");

        return state.CurrentItem;
    }

    /// <summary>
    /// Tries to get the current item being processed by the iterator.
    /// </summary>
    public static bool TryGetCurrentItem<T>(this PipelineContext context, out T value)
    {
        var state = context.GetStepState<IIteratorState<T>>();
        if (state != null)
        {
            value = state.CurrentItem;
            return true;
        }

        value = default!;
        return false;
    }


}



