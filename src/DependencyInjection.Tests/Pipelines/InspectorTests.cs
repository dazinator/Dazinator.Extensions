namespace Tests.Pipelines;

using Dazinator.Extensions.Pipelines;
using Xunit.Abstractions;
using Xunit.Categories;

[UnitTest]
public class InspectorTests
{
    private IServiceProvider? _serviceProvider;
    private int _currentId = 0;
  //  private readonly TestExecutionCollector _collector;

    private readonly List<string> _executionOrder = new();


    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0021:Use expression body for constructor", Justification = "<Pending>")]
    public InspectorTests(ITestOutputHelper testOutputHelper)
    {
        TestOutputHelper = testOutputHelper;
        this.GetNextId = () => Interlocked.Increment(ref _currentId).ToString();
        this.WriteNextIdToOutput = () => TestOutputHelper.WriteLine(GetNextId().ToString());
       // _collector = new TestExecutionCollector();

    }

    public ITestOutputHelper TestOutputHelper { get; }

    public Func<object> GetNextId { get; }

    public Action WriteNextIdToOutput { get; set; }

    private PipelineBuilder CreatePipelineBuilder(IServiceCollection? configureServices = null)
    {
        configureServices ??= new ServiceCollection();
        //var services = new ServiceCollection();
        //  configureServices?.Invoke(services);
        _serviceProvider = configureServices.BuildServiceProvider();
        return new PipelineBuilder(_serviceProvider);
    }

    private class TestInspector : IPipelineInspector
    {
        public List<string> StepOrder { get; } = new();
        public List<Exception> Exceptions { get; } = new();

        public Task BeforeStepAsync(PipelineStepContext context)
        {
            StepOrder.Add($"Before {context.StepId}");
            return Task.CompletedTask;
        }

        public Task AfterStepAsync(PipelineStepContext context)
        {
            StepOrder.Add($"After {context.StepId}");
            return Task.CompletedTask;
        }

        public Task OnExceptionAsync(PipelineStepContext context)
        {
            if (context.Exception != null)
            {
                Exceptions.Add(context.Exception);
            }
            return Task.CompletedTask;
        }
    }

    private class TestInspectorEvents : IPipelineInspector
    {
        public event Action<PipelineStepContext>? BeforeStep;
        public event Action<PipelineStepContext>? AfterStep;
        public event Action<PipelineStepContext>? OnError;

        public Task BeforeStepAsync(PipelineStepContext context)
        {
            BeforeStep?.Invoke(context);
            return Task.CompletedTask;
        }

        public Task AfterStepAsync(PipelineStepContext context)
        {
            AfterStep?.Invoke(context);
            return Task.CompletedTask;
        }

        public Task OnExceptionAsync(PipelineStepContext context)
        {
            OnError?.Invoke(context);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task InspectorTracksExecution()
    {
        // Arrange
        var inspector = new TestInspector();
        //.BuildServiceProvider();

        // Act
        var builder = CreatePipelineBuilder()
            .AddInspector(inspector)
            .Use(next => async ct => await next(ct), "Step1")
            .Use(next => async ct => await next(ct), "Step2");

        var pipeline = builder.Build();
        await pipeline.Run(default);

        // Assert
        Assert.Equal(new[]
        {
        "Before Step1",  // Step1 starts
        "Before Step2",  // Step1 calls next, Step2 starts
        "After Step2",   // Step2 completes
        "After Step1"    // Step1 completes
        }, inspector.StepOrder);
    }

    [Fact]
    public async Task InspectorTracksMiddlewareActionsCorrectly()
    {
        // Arrange
        var allEvents = new List<string>();
        //.BuildServiceProvider();

        var inspector = new TestInspectorEvents();
        inspector.BeforeStep += (ctx) => allEvents.Add($"Inspector Before {ctx.StepId}");
        inspector.AfterStep += (ctx) => allEvents.Add($"Inspector After {ctx.StepId}");

        // Act
        var builder = CreatePipelineBuilder()
            .AddInspector(inspector)
            .Use(next => async ct =>
            {
                allEvents.Add("Middleware1 Start");
                await next(ct);
                allEvents.Add("Middleware1 End");
            }, "Step1")
            .Use(next => async ct =>
            {
                allEvents.Add("Middleware2 Start");
                await next(ct);
                allEvents.Add("Middleware2 End");
            }, "Step2");

        var pipeline = builder.Build();
        await pipeline.Run(default);

        // Assert
        var expected = new[]
        {
        "Inspector Before Step1",
        "Middleware1 Start",
        "Inspector Before Step2",
        "Middleware2 Start",
        "Middleware2 End",
        "Inspector After Step2",
        "Middleware1 End",
        "Inspector After Step1"
    };

        Assert.Equal(expected, allEvents);
    }

    [Fact]
    public async Task InspectorCatchesExceptions()
    {
        // Arrange
        var inspector = new TestInspector();


        var expectedException = new Exception("Test Exception");

        // Act
        var builder = CreatePipelineBuilder()
            .AddInspector(inspector)
            .Use(next => ct => throw expectedException, "FailingStep");

        var pipeline = builder.Build();
        var exception = await Assert.ThrowsAsync<Exception>(() => pipeline.Run(default));

        // Assert
        Assert.Single(inspector.Exceptions);
        Assert.Equal(expectedException, inspector.Exceptions[0]);
    }

    [Fact]
    public async Task InspectorTracksBranchSteps()
    {
        // Arrange
        var allEvents = new List<string>();

        var inspector = new TestInspectorEvents();
        inspector.BeforeStep += (ctx) => allEvents.Add($"Inspector Before {ctx.StepId}");
        inspector.AfterStep += (ctx) => allEvents.Add($"Inspector After {ctx.StepId}");

        // Act
        var builder = CreatePipelineBuilder()
            .AddInspector(inspector)
            .Use(next => async ct =>
            {
                allEvents.Add("Main Pipeline Start");
                await next(ct);
                allEvents.Add("Main Pipeline End");
            }, "MainStep")
            .UseBranch(
                branch => branch
                    .Use(next => async ct =>
                    {
                        allEvents.Add("Branch Step1");
                        await next(ct);
                    }, "BranchStep1")
                    .Use(next => async ct =>
                    {
                        allEvents.Add("Branch Step2");
                        await next(ct);
                    }, "BranchStep2"),
                "BranchOperation"
            );

        var pipeline = builder.Build();
        await pipeline.Run(default);

        // Assert
        var expected = new[]
        {
        "Inspector Before MainStep",
        "Main Pipeline Start",
        "Inspector Before BranchOperation",
        "Inspector Before BranchStep1",
        "Branch Step1",
        "Inspector Before BranchStep2",
        "Branch Step2",
        "Inspector After BranchStep2",
        "Inspector After BranchStep1",
        "Inspector After BranchOperation",
        "Main Pipeline End",
        "Inspector After MainStep"
        };

        Assert.Equal(expected, allEvents);
    }
}
