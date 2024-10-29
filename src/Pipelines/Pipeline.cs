namespace Dazinator.Extensions.Pipelines;

using System.Threading.Tasks;

public class Pipeline
{
    private readonly PipelineStep _pipeline;
    private readonly IServiceProvider _serviceProvider;
    private readonly IReadOnlyList<IPipelineInspector> _inspectors;

    internal Dictionary<Type, object> ExtensionState { get; }

    internal Pipeline(PipelineStep pipeline, IServiceProvider serviceProvider, IReadOnlyList<IPipelineInspector> inspectors, Dictionary<Type, object> extensionState)
    {
        _pipeline = pipeline;
        _serviceProvider = serviceProvider;
        _inspectors = inspectors;
        ExtensionState = extensionState;
    }

    public Task Run(CancellationToken cancellationToken = default)
    {
        var context = new PipelineContext(_serviceProvider, cancellationToken, this);       
        return _pipeline(context);
    }

    /// <summary>
    /// Run the pipelinepassing an existing context.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    private Task Run(PipelineContext context)
    {   
        return _pipeline(context);
    }

    internal Pipeline CreateBranch(Action<PipelineBuilder> configure)
    {
        var branchBuilder = new PipelineBuilder(_serviceProvider);

        // Transfer inspectors from parent pipeline
        foreach (var inspector in _inspectors)
        {
            branchBuilder.AddInspector(inspector);           
        }

        configure(branchBuilder);
        return branchBuilder.Build();
    }

    /// <summary>
    /// Configure and execute a branch of the pipeline.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task RunBranch(PipelineContext parentContext, Action<PipelineBuilder> configure)
    {     

        var branch = CreateBranch(configure);
        var branchContext = parentContext.CreateBranchContext(branch); // we give each branch a clean context, because we don't want them to share state esepcially parallel scenarios.
        await branch.Run(branchContext);
    }
}

