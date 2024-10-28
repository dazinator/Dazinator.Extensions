namespace Tests.Pipelines;

using Xunit.Abstractions;
using Xunit.Categories;
using Xunit;
using Dazinator.Extensions.Pipelines;
using Dazinator.Extensions.Pipelines.Features.Filter;

[UnitTest]
public class FilterTests
{
    private IServiceProvider? _serviceProvider;

    public FilterTests(ITestOutputHelper testOutputHelper)
    {
        TestOutputHelper = testOutputHelper;
    }

    public ITestOutputHelper TestOutputHelper { get; }

    private PipelineBuilder CreatePipelineBuilder(IServiceCollection? configureServices = null)
    {
        configureServices ??= new ServiceCollection();
        _serviceProvider = configureServices.BuildServiceProvider();
        return new PipelineBuilder(_serviceProvider);
    }

    [Fact]
    public async Task Filter_ExecutesInOrder()
    {
        // Arrange
        var executionOrder = new List<string>();
        var builder = CreatePipelineBuilder();

        // Act
        builder
            .UseFilters()
            .Use(next => async ctx =>
            {
                executionOrder.Add("Step");
                await next(ctx);
            }, "TestStep")
            .AddFilters(registry =>
            {
                registry.AddFilter(sp => new TestFilter(
                    "Filter1",
                    beforeStep: () => executionOrder.Add("Before Filter1"),
                    afterStep: () => executionOrder.Add("After Filter1")));
                registry.AddFilter(sp => new TestFilter(
                    "Filter2",
                    beforeStep: () => executionOrder.Add("Before Filter2"),
                    afterStep: () => executionOrder.Add("After Filter2")));
            });

        var pipeline = builder.Build();
        await pipeline.Run();

        // Assert
        Assert.Equal(new[]
        {
            "Before Filter1",
            "Before Filter2",
            "Step",
            "After Filter2",
            "After Filter1"
        }, executionOrder);
    }

    private class SkippingFilter : IStepFilter
    {
        public Task BeforeStepAsync(PipelineStepContext context)
        {
            // Setting ShouldSkip will prevent the step from executing
            context.ShouldSkip = true;
            return Task.CompletedTask;
        }

        public Task AfterStepAsync(PipelineStepContext context)
        {
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Filter_CanSkipStep()
    {
        // Arrange
        var stepExecuted = false;
        var afterStepExecuted = false;
        var builder = CreatePipelineBuilder();

        // Act
        builder
            .UseFilters()
            .Use(next => async ctx =>
            {
                stepExecuted = true;
                await next(ctx);
                afterStepExecuted = true;
            })
            .AddFilters(registry =>
            {
                registry.AddFilter(sp => new SkippingFilter());
            });

        var pipeline = builder.Build();
        await pipeline.Run();

        // Assert
        Assert.False(stepExecuted);
        Assert.False(afterStepExecuted);
    }

    [Fact]
    public async Task Filter_CanResolveFromServices()
    {
        // Arrange
        var services = new ServiceCollection();      
        services.AddScoped<TestServiceFilter>();

        var builder = CreatePipelineBuilder(services);

        // Act
        builder
            .UseFilters()
            .Use(next => async ctx => await next(ctx))
            .AddFilters(registry =>
            {
                registry.AddFilterFromServices<TestServiceFilter>();
            });

        var pipeline = builder.Build();
        await pipeline.Run();

        // Assert
        var filter = _serviceProvider!.GetRequiredService<TestServiceFilter>();
        Assert.True(filter.WasExecuted);
    }

    [Fact]
    public async Task Filter_ExecutesForCorrectStep()
    {
        // Arrange
        var executedStepIndices = new List<int>();
        var builder = CreatePipelineBuilder();

        // Act
        builder
            .UseFilters()
            .Use(next => async ctx => await next(ctx), "Step1")
            .AddFilters(registry =>
            {
                registry.AddFilter(sp => new TestFilter(
                    "Filter1",
                    beforeStep: () => executedStepIndices.Add(0)));
            })
            .Use(next => async ctx => await next(ctx), "Step2")
            .AddFilters(registry =>
            {
                registry.AddFilter(sp => new TestFilter(
                    "Filter2",
                    beforeStep: () => executedStepIndices.Add(1)));
            });

        var pipeline = builder.Build();
        await pipeline.Run();

        // Assert
        Assert.Equal(new[] { 0, 1 }, executedStepIndices);
    }

    // Test helper classes
    private class TestFilter : IStepFilter
    {
        private readonly string _name;
        private readonly Action<PipelineStepContext>? _beforeStep;
        private readonly Action<PipelineStepContext>? _afterStep;

        public TestFilter(
            string name,
            Action<PipelineStepContext>? beforeStep = null,
            Action<PipelineStepContext>? afterStep = null)
        {
            _name = name;
            _beforeStep = beforeStep ?? (_ => { });
            _afterStep = afterStep ?? (_ => { });
        }

        public TestFilter(
            string name,
            Action? beforeStep = null,
            Action? afterStep = null)
            : this(
                name,
                beforeStep != null ? _ => beforeStep() : null,
                afterStep != null ? _ => afterStep() : null)
        {
        }

        public Task BeforeStepAsync(PipelineStepContext context)
        {
            _beforeStep?.Invoke(context);
            return Task.CompletedTask;
        }

        public Task AfterStepAsync(PipelineStepContext context)
        {
            _afterStep?.Invoke(context);
            return Task.CompletedTask;
        }
    }

    private class TestServiceFilter : IStepFilter
    {
        public bool WasExecuted { get; private set; }

        public Task BeforeStepAsync(PipelineStepContext context)
        {
            WasExecuted = true;
            return Task.CompletedTask;
        }

        public Task AfterStepAsync(PipelineStepContext context) => Task.CompletedTask;
    }
}
