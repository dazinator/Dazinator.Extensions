namespace Tests.Pipelines;

using Dazinator.Extensions.Pipelines;

public class TestExecutionCollector : IPipelineInspector
{
    public List<StepExecutionInfo> Steps { get; } = new();

    public void RecordExecution(string message)
    {
        Steps[^1].ExecutionMessage = message;
    }

    public Task BeforeStepAsync(PipelineStepContext context)
    {
        Steps.Add(new StepExecutionInfo
        {
            StepId = context.StepId,
            WasSkipped = context.ShouldSkip,
            ExecutionOrder = Steps.Count,
            ExecutionMessage = null,
            BeforeStepAsyncContext = new ContextInfo() {
                Exception = context.Exception,
                ShouldSkip = context.ShouldSkip
            },            
        });
        return Task.CompletedTask;
    }

    public Task AfterStepAsync(PipelineStepContext context)
    {
        Steps[^1].AfterStepAsyncFired = true;
        return Task.CompletedTask;
    }
    public Task OnExceptionAsync(PipelineStepContext context)
    {
        Steps[^1].AfterStepAsyncFired = true;
        Steps[^1].AfterStepAsyncContext = new ContextInfo()
        {
            Exception = context.Exception,
            ShouldSkip = context.ShouldSkip
        };
        return Task.CompletedTask;
    }

   
}


