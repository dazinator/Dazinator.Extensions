#pragma warning disable IDE0130 // Namespace does not match folder structure - for discoverability
namespace Dazinator.Extensions.Pipelines;
#pragma warning restore IDE0130 // Namespace does not match folder structure

using System.Threading.Tasks;

public static class BranchExtensions
{

    public static PipelineBuilder UseBranch(this PipelineBuilder builder, Action<PipelineBuilder> configureBranch, string? stepId = null) => builder.UseBranch((ctx) => Task.FromResult(true), configureBranch, stepId);


    public static PipelineBuilder UseBranch(this PipelineBuilder builder,
        Func<PipelineContext, Task<bool>> condition,
        Action<PipelineBuilder> configureBranch, string? stepId = null)
    {
#pragma warning disable IDE0022 // Use expression body for method
        return builder.When(condition, context =>
        {
            var branchBuilder = builder.CreateBranch();        
            configureBranch(branchBuilder);
            var branchPipeline = branchBuilder.Build(context.ServiceProvider);               
            return branchPipeline.BranchFrom(context);
        }, stepId);
#pragma warning restore IDE0022 // Use expression body for method

        //builder.Add((sp, next) => async context =>
        //{
        //    if (await condition(context))
        //    {
        //        var branchBuilder = new PipelineBuilder();
        //        configureBranch(branchBuilder);
        //        var branchPipeline = branchBuilder.Build(context.ServiceProvider);
        //        await branchPipeline.RunWithContext(context);               
        //    }
        //    else
        //    {
        //        await next(context);
        //    }
        //});
        //return builder;
    }

    public static PipelineBuilder TryBranch(
       this PipelineBuilder builder,
       Func<PipelineContext, Task<bool>> condition,
       Action<PipelineBuilder> configureBranch,
       Action<Exception>? onError = null,
       string stepId = null)
    {

        builder.Add(next => async context =>
        {
            try
            {
                if (await condition(context))
                {
                    var branchBuilder = builder.CreateBranch();
                    configureBranch(branchBuilder);
                    var branchPipeline = branchBuilder.Build(context.ServiceProvider);
                    await branchPipeline.BranchFrom(context);
                }
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex);
            }
            await next(context);
        }, stepId);
        return builder;     
        
    }

    public static PipelineBuilder UseParallelBranches<T>(this PipelineBuilder builder,
    IEnumerable<T> items,
    Action<PipelineBuilder, T> configureBranch,
    string stepId = null)
    {
        builder.Add(next => async context =>
        {
            var tasks = items.Select(item =>
            {
                var branchBuilder = builder.CreateBranch();
                configureBranch(branchBuilder, item);
                // Use the context's service provider instead of the outer sp
                var branchPipeline = branchBuilder.Build(context.ServiceProvider);
                return branchPipeline.BranchFrom(context);
            });

            await Task.WhenAll(tasks);
            await next(context);
        }, stepId);
       
        return builder;
    }

    public static PipelineBuilder UseParallelBranches<T>(this PipelineBuilder builder,
       Func<PipelineContext, Task<IEnumerable<T>>> getItems,
       Action<PipelineBuilder, T> configureBranch,
       string stepId = null)
    {
        builder.Add(next => async context =>
        {
            var items = await getItems(context);
            var tasks = items.Select(item =>
            {
                var branchBuilder = builder.CreateBranch();
                configureBranch(branchBuilder, item);
                // Use the context's service provider instead of the outer sp
                var branchPipeline = branchBuilder.Build(context.ServiceProvider);
                return branchPipeline.BranchFrom(context);
            });

            await Task.WhenAll(tasks);
            await next(context);
        }, stepId);
       
        return builder;
    }

    public static PipelineBuilder UseParallelBranches<T>(this PipelineBuilder builder,
       Func<PipelineContext, IEnumerable<T>> getItems,
       Action<PipelineBuilder, T> configureBranch,
       string stepId = null)
    {
        builder.Add(next => async context =>
        {
            var items = getItems(context);

            var tasks = items.Select(item =>
            {
                var branchBuilder = builder.CreateBranch();
                configureBranch(branchBuilder, item);
                // Use the context's service provider instead of the outer sp
                var branchPipeline = branchBuilder.Build(context.ServiceProvider);
                return branchPipeline.BranchFrom(context);
            });

            await Task.WhenAll(tasks);
            await next(context);
        }, stepId, nameof(TryBranch));
       
        return builder;
    }
}
