namespace Tests.Pipelines;

using Dazinator.Extensions.Pipelines;
using Dazinator.Extensions.Pipelines.Features.Skip;
using Tests.Pipelines.Utils;
using Xunit.Abstractions;
using Xunit.Categories;

[UnitTest]
public class SkipConditionTests
{
    private IServiceProvider? _serviceProvider;
    private readonly TestExecutionLogger _testExecutionLogger;


    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0021:Use expression body for constructor", Justification = "<Pending>")]
    public SkipConditionTests(ITestOutputHelper testOutputHelper)
    {
        TestOutputHelper = testOutputHelper;
        _testExecutionLogger = new TestExecutionLogger(testOutputHelper);

    }

    public ITestOutputHelper TestOutputHelper { get; }


    private IPipelineBuilder CreatePipelineBuilder(IServiceCollection? configureServices = null)
    {
        configureServices ??= new ServiceCollection();
        //var services = new ServiceCollection();
        //  configureServices?.Invoke(services);
        _serviceProvider = configureServices.BuildServiceProvider();
        return new PipelineBuilder(_serviceProvider).UseFilters();
    }

    [Fact]
    public async Task SkipCondition_ContinuesPipelineWhenConditionNotMet()
    {
        // Arrange
        var executionOrder = new List<string>();

        // Act
        var builder = CreatePipelineBuilder()
            .Use(next => async context =>
            {
                executionOrder.Add("Before When");
                await next(context);
                executionOrder.Add("After When");
            })
            .Run(() => executionOrder.Add("When Action"))
                .WithSkipCondition(true)
            .Use(next => async context =>
            {
                executionOrder.Add("After When Middleware");
                await next(context);
            });

        var pipeline = builder.Build();
        await pipeline.Run(default);

        // Assert
        Assert.Equal(new[]
        {
        "Before When",
        "After When Middleware",
        "After When"
    }, executionOrder);
    }

    [Fact]
    public async Task SkipCondition_ExecutesActionAndContinuesPipelineWhenConditionMet()
    {
        // Arrange
        var executionOrder = new List<string>();

        // Act
        var builder = CreatePipelineBuilder()
            .Use(next => async context =>
            {
                executionOrder.Add("Before When");
                await next(context);
                executionOrder.Add("After When");
            })
             .Run(() => executionOrder.Add("When Action"))
                .WithSkipCondition(false)
            .Use(next => async context =>
            {
                executionOrder.Add("After When Middleware");
                await next(context);
            });

        var pipeline = builder.Build();
        await pipeline.Run(default);

        // Assert
        Assert.Equal(new[]
        {
        "Before When",
        "When Action",
        "After When Middleware",
        "After When"
    }, executionOrder);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SkipCondition_SupportsBranch(bool shouldSkip)
    {
        // Arrange
        var executionOrder = new List<string>();

        // Act
        var builder = CreatePipelineBuilder()
            .Use(next => async context =>
            {
                executionOrder.Add("Main Pipeline Start");
                await next(context);
                executionOrder.Add("Main Pipeline End");
            })
            .UseBranch(branch =>
            {
                branch.Use(next => async ctx =>
                {
                    executionOrder.Add("Nested Pipeline Start");
                    await next(ctx);
                    executionOrder.Add("Nested Pipeline End");
                });
            })
              .WithSkipCondition(shouldSkip)
            .Use(next => async context =>
            {
                executionOrder.Add("After Nested Pipeline");
                await next(context);
            });

        var pipeline = builder.Build();
        await pipeline.Run(default);

        // Assert
        var expectedOrder = shouldSkip
            ? new[]
            {
            "Main Pipeline Start",
            "After Nested Pipeline",
            "Main Pipeline End"
            }
            : new[]
            {
            "Main Pipeline Start",
            "Nested Pipeline Start",
            "Nested Pipeline End",
            "After Nested Pipeline",
            "Main Pipeline End"
            };

        Assert.Equal(expectedOrder, executionOrder);
    }


    [Fact]
    public async Task SkipCondition_SupportsChainingMultipleConditions()
    {
        // Act
        var builder = CreatePipelineBuilder()
            .Run(_testExecutionLogger.WriteCurrentStepIdToLog, "A")
                .WithSkipCondition(() => false)

            .Run(_testExecutionLogger.WriteCurrentStepIdToLog, "B")
                .WithSkipCondition(() => false)
                .WithSkipConditionAsync(() => Task.FromResult(false))
                //.WithSkipConditionAsync((ctx) =>CheckShouldBeSkippedAsync())

            .Run(_testExecutionLogger.WriteCurrentStepIdToLog, "C")
                .WithSkipCondition(() => true)

            .Run(_testExecutionLogger.WriteCurrentStepIdToLog, "D")
                .WithSkipCondition(() => false)
                .WithSkipCondition(() => true)

            .UseBranch((branch) =>
            {
                branch.Run(_testExecutionLogger.WriteCurrentStepIdToLog, "E.1")
                    .WithSkipCondition(() => false)
                    .WithSkipCondition(() => true); // THIS NEVER RUNS BECAUSE THIS WHOLE BRANCH IS SKIPPED AS PER THE SKIP ON UseBranch ITSELF

            }, "E")
                .WithSkipCondition(() => false)
                .WithSkipCondition(() => true)

            .UseBranch((branch) =>
            {
                branch.Run(_testExecutionLogger.WriteCurrentStepIdToLog, "F.1")
                    .WithSkipCondition(() => false)
                    .WithSkipCondition(() => true);

                branch.Run(_testExecutionLogger.WriteCurrentStepIdToLog, "F.2")
                   .WithSkipCondition(() => false)
                   .WithSkipCondition(() => false);

                branch.Run(_testExecutionLogger.WriteCurrentStepIdToLog, "F.3");   // no skip condition.             

            }, "F")
                .WithSkipCondition(() => false)
                .WithSkipCondition(() => false);

        var pipeline = builder.Build();
        await pipeline.Run(default);

        _testExecutionLogger.AssertLogsEqual(["A", "B", "F.2", "F.3"]);
    }

}
