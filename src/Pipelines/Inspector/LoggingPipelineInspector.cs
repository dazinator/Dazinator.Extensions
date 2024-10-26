namespace Dazinator.Extensions.Pipelines.Inspector;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public class LoggingPipelineInspector : IPipelineInspector
{
    private readonly ILogger<LoggingPipelineInspector> _logger;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0021:Use expression body for constructor", Justification = "<Pending>")]
    public LoggingPipelineInspector(ILogger<LoggingPipelineInspector> logger)
    {
        _logger = logger;
    }

    public Task BeforeStepAsync(PipelineStepContext context)
    {
        _logger.LogInformation(
            "Starting pipeline step {StepId} of type {StepType}",
            context.StepId,
            context.StepType);
        return Task.CompletedTask;
    }

    public Task AfterStepAsync(PipelineStepContext context)
    {
        _logger.LogInformation(
            "Completed pipeline step {StepId} after {Duration}ms",
            context.StepId,
            context.Duration.TotalMilliseconds);
        return Task.CompletedTask;
    }

    public Task OnExceptionAsync(PipelineStepContext context)
    {
        _logger.LogError(
            context.Exception,
            "Pipeline step {StepId} failed after {Duration}ms: {Error}",
            context.StepId,
            context.Duration.TotalMilliseconds,
            context.Exception?.Message);
        return Task.CompletedTask;
    }
}


