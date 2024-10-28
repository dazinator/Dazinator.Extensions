namespace Dazinator.Extensions.Pipelines;

using Microsoft.Extensions.Options;

public class PipelineRegistry
{
    private readonly IOptionsMonitor<PipelineDefinition> _definitions;
    private readonly Dictionary<string, Pipeline> _pipelines = new();
    private readonly IServiceProvider _serviceProvider;

    public PipelineRegistry(
        IOptionsMonitor<PipelineDefinition> definitions,
        IServiceProvider serviceProvider)
    {
        _definitions = definitions;
        _serviceProvider = serviceProvider;
    }

    public Pipeline GetPipeline(string name)
    {
        if (!_pipelines.TryGetValue(name, out var pipeline))
        {
            var definition = _definitions.Get(name);
            if (definition.Configure == null)
            {
                throw new InvalidOperationException($"Pipeline '{name}' not configured");
            }

            var builder = new PipelineBuilder(_serviceProvider);
            definition.Configure(builder);
            pipeline = builder.Build();
            _pipelines[name] = pipeline;
        }
        return pipeline;
    }
}
