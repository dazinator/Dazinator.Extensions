#pragma warning disable IDE0130 // Namespace does not match folder structure - for discoverability
namespace Dazinator.Extensions.Pipelines;
#pragma warning restore IDE0130 // Namespace does not match folder structure

using System.Threading.Tasks;

public static class BranchExtensions
{
    public static IPipelineBuilder UseBranch(
        this IPipelineBuilder builder,
        Action<IPipelineBuilder> configureBranch,
        string? stepId = null)
    {
        builder.Use(next => async context =>
        {
            // Use parent pipeline to create branch
            await context.ParentPipeline.RunBranch(context, configureBranch);          
            await next(context);       
        }, stepId);
        return builder;
       
    }
   

    // Parallel branches don't need conditions, they're simpler now
    public static IPipelineBuilder UseParallelBranches<T>(
     this IPipelineBuilder builder,
     IEnumerable<T> items,
     Action<IPipelineBuilder, T> configureBranch,
     string? stepId = null)
    {
        builder.Use(next => async context =>
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


