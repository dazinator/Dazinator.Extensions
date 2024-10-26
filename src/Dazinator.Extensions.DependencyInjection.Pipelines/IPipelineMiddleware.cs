namespace Dazinator.Extensions.DependencyInjection.Pipelines;

public interface IPipelineMiddleware
{
    Task ExecuteAsync(PipelineStep next, PipelineContext context);
}
