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
}


