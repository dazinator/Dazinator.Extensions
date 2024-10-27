namespace Dazinator.Extensions.Pipelines;

using System.Threading.Tasks;

public class Pipeline
{
    private readonly PipelineStep _pipeline;
    private readonly IServiceProvider _serviceProvider;

    internal Pipeline(PipelineStep pipeline, IServiceProvider serviceProvider)
    {
        _pipeline = pipeline;
        _serviceProvider = serviceProvider;
    }

    public Task Run(CancellationToken cancellationToken = default)
    {
        var context = new PipelineContext(_serviceProvider, cancellationToken);
        return _pipeline(context);
    }

    /// <summary>
    /// Run the pipeline asa a branch from an existing pipeline's context, making pipelines "composable".
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public Task BranchFrom(PipelineContext context)
    {
        var branchContext = context.CreateBranchContext(); // we give each branch a clean context, because we don't want them to share state esepcially parallel scenarios.
        return _pipeline(branchContext);
    }
}

