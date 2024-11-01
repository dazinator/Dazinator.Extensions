namespace Tests.Pipelines;

using Dazinator.Extensions.Pipelines;
using Dazinator.Extensions.Pipelines.Features.Branching;
using Dazinator.Extensions.Pipelines.Features.Branching.PerItem;
using Xunit.Abstractions;
using Xunit.Categories;

[UnitTest]
public class ScopeTests
{
    private IServiceProvider? _serviceProvider;
    private int _currentId = 0;   

    private readonly List<string> _executionOrder = new();


    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0021:Use expression body for constructor", Justification = "<Pending>")]
    public ScopeTests(ITestOutputHelper testOutputHelper)
    {
        TestOutputHelper = testOutputHelper;
        this.GetNextId = () => Interlocked.Increment(ref _currentId).ToString();
        this.WriteNextIdToOutput = () => TestOutputHelper.WriteLine(GetNextId().ToString());      

    }

    public ITestOutputHelper TestOutputHelper { get; }

    public Func<object> GetNextId { get; }

    public Action WriteNextIdToOutput { get; set; }

    private IPipelineBuilder CreatePipelineBuilder(IServiceCollection? configureServices = null)
    {
        configureServices ??= new ServiceCollection();
        //var services = new ServiceCollection();
        //  configureServices?.Invoke(services);
        _serviceProvider = configureServices.BuildServiceProvider();
        return new PipelineBuilder(_serviceProvider).UseFilters();
    }

    private class ScopedTestMiddleware : IPipelineMiddleware
    {
        private readonly IScopedService _service;
        private readonly List<object> _instances;

        public ScopedTestMiddleware(IScopedService service, List<object> instances)
        {
            _service = service;
            _instances = instances;
        }

        public async Task ExecuteAsync(PipelineStep next, PipelineContext context)
        {
            _instances.Add(_service);
            await next(context);
        }
    }

    [Fact]
    public async Task ScopedServicesAreIndependent()
    {
        // Arrange
        var instances = new List<object>();
        var executionOrder = new List<string>();
        var services = new ServiceCollection();
        services.AddScoped<IScopedService, ScopedService>();
        services.AddSingleton(instances);
        services.AddScoped<ScopedTestMiddleware>();
        var sp = services.BuildServiceProvider();

        // Act
        var builder = CreatePipelineBuilder(services)
            .Use(next => async ct =>
            {
                executionOrder.Add("Before root scope");
                await next(ct);
                executionOrder.Add("After root scope");
            })
            .UseNewScope()
            .UseBranchPerInput<string>(
                (branch) =>
                {
                    executionOrder.Add($"Configuring {branch.Input}");
                    branch
                        .Use(next => async ct =>
                        {
                            executionOrder.Add($"Before {branch.Input} scope");
                            await next(ct);
                            executionOrder.Add($"After {branch.Input} scope");
                        })
                        .UseNewScope()
                        .UseMiddleware<ScopedTestMiddleware>();
                }
            ).WithInputs(new[] { "Branch1", "Branch2" });

        var pipeline = builder.Build();
        await pipeline.Run(default);

        // Output execution order for debugging
        foreach (var step in executionOrder)
        {
            TestOutputHelper.WriteLine(step);
        }

        // Assert
        Assert.Equal(2, instances.Count);
        Assert.NotEqual(instances[0], instances[1]);
    }


    // Support types for tests
    private interface IScopedService { }
    private class ScopedService : IScopedService { }
    private interface ITestService { }
    private class TestService : ITestService { }
}
