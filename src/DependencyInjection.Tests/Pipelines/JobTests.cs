namespace Tests.Pipelines;

using Dazinator.Extensions.Pipelines;
using Dazinator.Extensions.Pipelines.Features.Job;
using Xunit.Abstractions;
using Xunit.Categories;

[UnitTest]
public class JobTests
{
    private IServiceProvider? _serviceProvider;
    private readonly ITestOutputHelper _testOutput;
    private static bool _executed;
    private static List<string> _executionOrder = new();
    private static Guid _scopeFound;

    public JobTests(ITestOutputHelper testOutput)
    {
        _testOutput = testOutput;
        _executed = false;
        _executionOrder.Clear();
    }

    private IPipelineBuilder CreatePipelineBuilder(IServiceCollection? services = null)
    {
        services ??= new ServiceCollection();
        _serviceProvider = services.BuildServiceProvider();
        return new PipelineBuilder(_serviceProvider);
    }

    private class TestJob : IJob
    {
        public Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _executed = true;
            return Task.CompletedTask;
        }
    }

    private class OrderTrackingJob : IJob
    {
        public Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _executionOrder.Add("Job");
            return Task.CompletedTask;
        }
    }

    private class ThrowingJob : IJob
    {
        public Task ExecuteAsync(CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Test exception");
        }
    }

    private class TokenCheckingJob : IJob
    {
        private readonly Action<CancellationToken> _tokenCallback;

        public TokenCheckingJob(Action<CancellationToken> tokenCallback)
        {
            _tokenCallback = tokenCallback;
        }

        public Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _tokenCallback(cancellationToken);
            return Task.CompletedTask;
        }
    }

    private class ScopedDependency
    {
        public Guid Id { get; set; }
    }

    private class ScopeTestJob : IJob
    {
        private readonly ScopedDependency _dependency;

        public ScopeTestJob(ScopedDependency dependency)
        {
            _dependency = dependency;
        }

        public Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _scopeFound = _dependency.Id;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task RunJob_ExecutesJob()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<TestJob>();

        var builder = CreatePipelineBuilder(services);

        // Act
        builder.RunJob<TestJob>();
        var pipeline = builder.Build();
        await pipeline.Run();

        // Assert
        Assert.True(_executed);
    }

    [Fact]
    public async Task RunJob_ExecutesJobAndContinuesPipeline()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<OrderTrackingJob>();

        var builder = CreatePipelineBuilder(services)
            .Run(() => _executionOrder.Add("Before"))
            .RunJob<OrderTrackingJob>()
            .Run(() => _executionOrder.Add("After"));

        // Act
        await builder.Build().Run();

        // Assert
        Assert.Equal(new[] { "Before", "Job", "After" }, _executionOrder);
    }

    [Fact]
    public async Task RunJob_JobThrowsException_PipelineStops()
    {
        // Arrange
        _executed = false;
        var services = new ServiceCollection();
        services.AddTransient<ThrowingJob>();

        var builder = CreatePipelineBuilder(services)
            .RunJob<ThrowingJob>()
            .Run(() => _executed = true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await builder.Build().Run();
        });

        Assert.False(_executed, "Pipeline should have stopped at throwing job");
    }

    [Fact]
    public async Task TryRunJob_JobThrowsException_ContinuesPipeline()
    {
        // Arrange
        _executed = false;
        Exception? caughtException = null;
        var services = new ServiceCollection();
        services.AddTransient<ThrowingJob>();

        var builder = CreatePipelineBuilder(services)
            .TryRunJob<ThrowingJob>(ex => caughtException = ex)
            .Run(() => _executed = true);

        // Act
        await builder.Build().Run();

        // Assert
        Assert.True(_executed, "Pipeline should have continued after job exception");
        Assert.NotNull(caughtException);
        Assert.IsType<InvalidOperationException>(caughtException);
    }

    [Fact]
    public async Task RunJob_CancellationTokenPropagated()
    {
        // Arrange
        var receivedToken = false;
        var services = new ServiceCollection();
        using var cts = new CancellationTokenSource();

        services.AddTransient<IJob>(sp => new TokenCheckingJob(token =>
        {
            receivedToken = token == cts.Token;
        }));

        var builder = CreatePipelineBuilder(services)
            .RunJob<IJob>();

        // Act
        await builder.Build().Run(cts.Token);

        // Assert
        Assert.True(receivedToken, "Job should receive the correct cancellation token");
    }

    [Fact]
    public async Task RunJob_JobResolvesFromCorrectScope()
    {
        // Arrange
        var executionScope = Guid.NewGuid();
        var services = new ServiceCollection();
        services.AddScoped<ScopedDependency>();
        services.AddTransient<ScopeTestJob>();

        var builder = CreatePipelineBuilder(services)
            .UseNewScope()
            .Run(context =>
            {
                var dependency = context.ServiceProvider.GetRequiredService<ScopedDependency>();
                dependency.Id = executionScope;
            })
            .RunJob<ScopeTestJob>();

        // Act
        await builder.Build().Run();

        // Assert
        Assert.Equal(executionScope, _scopeFound);
    }
}
