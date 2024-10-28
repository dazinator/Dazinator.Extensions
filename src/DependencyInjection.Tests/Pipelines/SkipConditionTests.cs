namespace Tests.Pipelines;

using Dazinator.Extensions.Pipelines;
using Dazinator.Extensions.Pipelines.Features.Skip;
using Xunit.Abstractions;
using Xunit.Categories;

[UnitTest]
public class SkipConditionTests
{
    private IServiceProvider? _serviceProvider;
    private int _currentId = 0;
    private readonly TestExecutionCollector _collector;

    private readonly List<string> _executionOrder = new();


    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0021:Use expression body for constructor", Justification = "<Pending>")]
    public SkipConditionTests(ITestOutputHelper testOutputHelper)
    {
        TestOutputHelper = testOutputHelper;
        this.GetNextId = () => Interlocked.Increment(ref _currentId).ToString();
        this.WriteNextIdToOutput = () => TestOutputHelper.WriteLine(GetNextId().ToString());
        _collector = new TestExecutionCollector();

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
        // Arrange
        var collector = new TestExecutionCollector();    


        // Act
        var builder = CreatePipelineBuilder()
            .AddInspector(collector)
            .Run(WriteNextIdToOutput)
                .WithSkipCondition(() => false)
            .Run(WriteNextIdToOutput)
                .WithSkipCondition(() => false)
                .WithSkipConditionAsync(() => Task.FromResult(false))
            .Run(WriteNextIdToOutput)
                .WithSkipCondition(() => true)
            .Run(WriteNextIdToOutput)
                .WithSkipCondition(() => false)
                .WithSkipCondition(() => true);

        var pipeline = builder.Build();
        await pipeline.Run(default);

        // Assert
        await Verify(collector.Steps);
    }

}
