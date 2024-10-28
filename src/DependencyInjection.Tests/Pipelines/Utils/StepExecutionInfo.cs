namespace Tests.Pipelines;

public class StepExecutionInfo
{
    public string StepId { get; set; } = "";
    public bool WasSkipped { get; set; }
    public int ExecutionOrder { get; set; }
    public string? ExecutionMessage { get; set; }
    public bool BeforeStepAsyncFired { get; internal set; }

    public bool AfterStepAsyncFired { get; internal set; }
    public bool OnExceptionAsyncFired { get; internal set; }
    public ContextInfo? BeforeStepAsyncContext { get; set; }
    public ContextInfo? AfterStepAsyncContext { get; internal set; }
}

public class ContextInfo
{
    public bool ShouldSkip { get; set; }
    public Exception? Exception { get; set; }
}


