namespace Tests.Pipelines;

using Dazinator.Extensions.Pipelines;


public class TestCallbackInspector : IPipelineInspector
{
    private readonly Action<PipelineStepContext> _beforeStep;

    public TestCallbackInspector(Action<PipelineStepContext> beforeStep)
    {
        _beforeStep = beforeStep;
    }

    public Task BeforeStepAsync(PipelineStepContext context)
    {
        _beforeStep(context);
        return Task.CompletedTask;
    }

    public Task AfterStepAsync(PipelineStepContext context) => Task.CompletedTask;
    public Task OnExceptionAsync(PipelineStepContext context) => Task.CompletedTask;
}


