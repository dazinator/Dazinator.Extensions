namespace Dazinator.Extensions.Pipelines;

public interface IPipelineMiddleware
{
    Task ExecuteAsync(PipelineStep next, PipelineContext context);
}
