namespace Tests.Pipelines;

using Dazinator.Extensions.Pipelines;
using Xunit.Abstractions;
using Xunit.Categories;

[UnitTest]
public class BranchTests
{
    private IServiceProvider? _serviceProvider;
    private int _currentId = 0;
  
    private readonly List<string> _executionOrder = new();


    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0021:Use expression body for constructor", Justification = "<Pending>")]
    public BranchTests(ITestOutputHelper testOutputHelper)
    {
        TestOutputHelper = testOutputHelper;
        this.GetNextId = () => Interlocked.Increment(ref _currentId).ToString();
        this.WriteNextIdToOutput = () => TestOutputHelper.WriteLine(GetNextId().ToString());     

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
    public async Task Branch_ExecutesNormally()
    {
        // Arrange & Act
        var builder = CreatePipelineBuilder()
            .UseBranch(branch => branch
                .Run(() => _executionOrder.Add("BranchStep1"))
                .Run(() => _executionOrder.Add("BranchStep2")));

        var pipeline = builder.Build();
        await pipeline.Run();

        // Assert
        Assert.Equal(new[]
        {
            "BranchStep1",
            "BranchStep2"
        }, _executionOrder);
    }


    [Fact]
    public async Task Branch_ExecutesInPipelineOrder()
    {
        // Arrange & Act
        var builder = CreatePipelineBuilder()
            .Run(() => _executionOrder.Add("MainStep1"))
            .UseBranch(branch => branch
                .Run(() => _executionOrder.Add("BranchStep1"))
                .Run(() => _executionOrder.Add("BranchStep2")))
            .Run(() => _executionOrder.Add("MainStep2"));

        var pipeline = builder.Build();
        await pipeline.Run();

        // Assert
        Assert.Equal(new[]
        {
            "MainStep1",
            "BranchStep1",
            "BranchStep2",
            "MainStep2"
        }, _executionOrder);
    } 

}
