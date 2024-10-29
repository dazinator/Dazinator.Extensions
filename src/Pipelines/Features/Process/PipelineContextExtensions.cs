namespace Dazinator.Extensions.Pipelines.Features.Process;

using System;

public static class PipelineContextExtensions
{
    public static IItemRunner<T> GetItemRunner<T>(this PipelineContext context)
    {
        return context.GetStepState<IItemRunner<T>>()
            ?? throw new InvalidOperationException(
                $"No item runner found. Did you forget to use {nameof(ProcessExtensions.ProcessItem)}?");
    }
}

