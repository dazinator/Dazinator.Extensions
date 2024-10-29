namespace Tests.Pipelines;

using Dazinator.Extensions.Pipelines;
using Tests.Pipelines.Utils;
using Xunit.Abstractions;
using Xunit.Categories;

[UnitTest]
public class RunActionsTests
{
    private IServiceProvider? _serviceProvider;   


    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0021:Use expression body for constructor", Justification = "<Pending>")]
    public RunActionsTests(ITestOutputHelper testOutputHelper)
    {
        TestOutputHelper = testOutputHelper;
        TestExecutionLogger = new TestExecutionLogger(testOutputHelper);

    }

    public ITestOutputHelper TestOutputHelper { get; }
    public TestExecutionLogger TestExecutionLogger { get; }

    private PipelineBuilder CreatePipelineBuilder(IServiceCollection? configureServices = null)
    {
        configureServices ??= new ServiceCollection();
        //var services = new ServiceCollection();
        //  configureServices?.Invoke(services);
        _serviceProvider = configureServices.BuildServiceProvider();
        return new PipelineBuilder(_serviceProvider);
    }

    [Fact]
    public async Task Run_ExecutesAction()
    {       

        // Act
        var builder = CreatePipelineBuilder()
            .Run((ctx) => TestExecutionLogger.WriteToLog("Ran"));

        var pipeline = builder.Build();
        await pipeline.Run(default);

        // Assert
        TestExecutionLogger.AssertWasLogged("Ran");      
    }

    [Fact]
    public async Task RunAsync_ExecutesAsyncAction()
    {
        // Arrange    

        // Act
        var builder = CreatePipelineBuilder()
            .RunAsync(async () =>
            {              
                await TestExecutionLogger.WriteToLogAsync("Ran");              
            });

        var pipeline = builder.Build();
        await pipeline.Run(default);

        // Assert
        TestExecutionLogger.AssertWasLogged("Ran");      
    }

    [Fact]
    public async Task TryRun_HandlesException()
    {
        // Arrange
        var exceptionHandled = false;

        // Act
        var builder = CreatePipelineBuilder()
            .TryRun(
                () => throw new Exception("Test exception"),
                ex => exceptionHandled = true
            );

        var pipeline = builder.Build();
        await pipeline.Run(default);

        // Assert
        Assert.True(exceptionHandled);
    }

    [Fact]
    public async Task TryRunAsync_HandlesException_AndContinuesPipeline()
    {
        // Arrange
        var exceptionHandled = false;
        var pipelineContinued = false;

        // Act
        var builder = CreatePipelineBuilder()
            .TryRunAsync(
                async ctx =>
                {
                    await Task.Delay(1, ctx.CancellationToken);
                    throw new Exception("Test exception");
                },
                ex => exceptionHandled = true
            )
            .Run(() => pipelineContinued = true);

        var pipeline = builder.Build();
        await pipeline.Run(default);

        // Assert
        Assert.True(exceptionHandled);
        Assert.True(pipelineContinued);
    }

    [Fact]
    public async Task RunAsync_CancellationToken_IsRespected()
    {
        // Arrange
        var services = new ServiceCollection();
        using var cts = new CancellationTokenSource();
        var taskWasCancelled = false;

        // Act
        var builder = CreatePipelineBuilder(services)
            .RunAsync(async context =>
            {
                try
                {
                    cts.Cancel();
                    await Task.Delay(5000, context.CancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // ExecutionLogs.WriteToExecutionLog("Test was cancelled");
                    taskWasCancelled = true;
                }
            });

        var pipeline = builder.Build();
        await pipeline.Run(cts.Token);

        // Assert
        Assert.True(taskWasCancelled);
    }
}
