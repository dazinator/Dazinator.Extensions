#pragma warning disable IDE0130 // Namespace does not match folder structure - for discoverability
namespace Dazinator.Extensions.Pipelines;
#pragma warning restore IDE0130 // Namespace does not match folder structure

using System.Threading.Tasks;
using Dazinator.Extensions.Pipelines.Features.Skip;

public static class BranchExtensions
{
    public static PipelineBuilder UseBranch(
        this PipelineBuilder builder,
        Action<PipelineBuilder> configureBranch,
        string? stepId = null)
    {
        builder.Add(next => async context =>
        {
            // Use parent pipeline to create branch
            await context.ParentPipeline.RunBranch(context, configureBranch);          
            await next(context);       
        }, stepId);
        return builder;
       
    }

    // Now we can compose branching with conditions
    public static PipelineBuilder UseConditionalBranch(
        this PipelineBuilder builder,
        Func<PipelineContext, Task<bool>> shouldSkip,
        Action<PipelineBuilder> configureBranch,
        string? stepId = null)
    {
        return builder
            .UseBranch(configureBranch, stepId)
            .WithSkipConditionAsync(shouldSkip);
    }

    public static PipelineBuilder TryUseConditionalBranch(
        this PipelineBuilder builder,
        Func<PipelineContext, Task<bool>> shouldSkip,
        Action<PipelineBuilder> configureBranch,
        Action<Exception>? onError = null,
        string? stepId = null)
    {
        return builder
            .UseBranch(configureBranch, stepId)
            .WithTrySkipConditionAsync(shouldSkip, onError);  // We'd need this extension
    }

    // Parallel branches don't need conditions, they're simpler now
    public static PipelineBuilder UseParallelBranches<T>(
     this PipelineBuilder builder,
     IEnumerable<T> items,
     Action<PipelineBuilder, T> configureBranch,
     string? stepId = null)
    {
        builder.Add(next => async context =>
        {
            var tasks = items.Select(item =>
            {
                return context.ParentPipeline.RunBranch(
                    context,
                    branch => configureBranch(branch, item));
            });

            await Task.WhenAll(tasks);
            await next(context);
        }, stepId);
        return builder;
    }
}


