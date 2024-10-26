namespace Dazinator.Extensions.DependencyInjection.Pipelines;

public class PipelineStepContext
{
    public PipelineStepContext(string stepId, string stepType, IServiceProvider serviceProvider, PipelineContext pipelineContext)
    {
        StepId = stepId;
        StepType = stepType;
        ServiceProvider = serviceProvider;
        PipelineContext = pipelineContext;
    }

    public string StepId { get; }
    public string StepType { get; }
    public IServiceProvider ServiceProvider { get; }
    public TimeSpan Duration { get; set; }
    public Exception? Exception { get; set; }
    public PipelineContext PipelineContext { get; }   
}
