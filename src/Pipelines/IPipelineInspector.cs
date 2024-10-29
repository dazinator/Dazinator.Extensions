namespace Dazinator.Extensions.Pipelines;

public interface IPipelineInspector
{
    Task BeforeStepAsync(PipelineStepContext context);
    Task AfterStepAsync(PipelineStepContext context);
    Task OnExceptionAsync(PipelineStepContext context);
}

public interface IInspectorInitialization
{
    void Initialize(PipelineBuilder builder);
}



