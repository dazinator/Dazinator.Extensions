namespace Dazinator.Extensions.DependencyInjection.Pipelines;

public interface IPipelineInspector
{
    Task BeforeStepAsync(PipelineStepContext context);
    Task AfterStepAsync(PipelineStepContext context);
    Task OnExceptionAsync(PipelineStepContext context);
}



