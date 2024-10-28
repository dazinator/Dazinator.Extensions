namespace Tests.Pipelines;

using Dazinator.Extensions.Pipelines;
using Xunit.Abstractions;
using Xunit.Categories;

[UnitTest]
public class MiddlewareTests
{
    private IServiceProvider? _serviceProvider;
    private int _currentId = 0;
    private readonly TestExecutionCollector _collector;

    private readonly List<string> _executionOrder = new();


    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0021:Use expression body for constructor", Justification = "<Pending>")]
    public MiddlewareTests(ITestOutputHelper testOutputHelper)
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
    public async Task ExecutesMiddlewareInOrder()
    {
        // Arrange
        var executionOrder = new List<string>();
        var services = new ServiceCollection();

        services.AddTransient<TestMiddleware>(_ =>
            new TestMiddleware(() => executionOrder.Add("First")));
        services.AddTransient<AnotherTestMiddleware>(_ =>
            new AnotherTestMiddleware(() => executionOrder.Add("Second")));

        var sp = services.BuildServiceProvider();

        // Act
        var builder = CreatePipelineBuilder(services)
            .UseMiddleware<TestMiddleware>("First")
            .UseMiddleware<AnotherTestMiddleware>("Second");

        var pipeline = builder.Build();
        await pipeline.Run(default);

        // Assert
        Assert.Equal(new[] { "First", "Second" }, executionOrder);
    }

#if NET8_0
    [Fact]
    public async Task ExecutesKeyedMiddlewareInOrder()
    {
        // Arrange
        var executionOrder = new List<string>();
        var services = new ServiceCollection();

        services.AddKeyedTransient<TestMiddleware>("first", (sp, key) =>
            new TestMiddleware(() => executionOrder.Add("First")));
        services.AddKeyedTransient<TestMiddleware>("second", (sp, key) =>
            new TestMiddleware(() => executionOrder.Add("Second")));

        var sp = services.BuildServiceProvider();

        // Act
        var builder = CreatePipelineBuilder(services)
            .UseMiddleware<TestMiddleware>("first", "First")
            .UseMiddleware<TestMiddleware>("second", "Second");

        var pipeline = builder.Build();
        await pipeline.Run(default);

        // Assert
        Assert.Equal(new[] { "First", "Second" }, executionOrder);
    }
#endif

    // Test middleware implementation
    private class TestMiddleware : IPipelineMiddleware
    {
        private readonly Action _action;

        public TestMiddleware(Action action)
        {
            _action = action;
        }

        public Task ExecuteAsync(PipelineStep next, PipelineContext context)
        {
            _action();
            return next(context);
        }
    }

    private class AnotherTestMiddleware : IPipelineMiddleware
    {
        private readonly Action _action;

        public AnotherTestMiddleware(Action action)
        {
            _action = action;
        }

        public Task ExecuteAsync(PipelineStep next, PipelineContext context)
        {
            _action();
            return next(context);
        }
    }
    // Test inspector implementation

}
