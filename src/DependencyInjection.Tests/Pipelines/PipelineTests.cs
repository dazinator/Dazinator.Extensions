namespace Tests.Pipelines;

using Dazinator.Extensions.Pipelines;
using global::Tests.Pipelines.Utils;
using Xunit;
using Xunit.Abstractions;
using Xunit.Categories;

[UnitTest]
public class PipelineBuilderTests
{
    private IServiceProvider? _serviceProvider; 

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0021:Use expression body for constructor", Justification = "<Pending>")]
    public PipelineBuilderTests(ITestOutputHelper testOutputHelper)
    {
        TestOutputHelper = testOutputHelper;       

    }

    public ITestOutputHelper TestOutputHelper { get; }
    public TestExecutionLogger? ExecutionLogs { get; }

  

    private PipelineBuilder CreatePipelineBuilder(IServiceCollection? configureServices = null)
    {
        configureServices ??= new ServiceCollection();
        //var services = new ServiceCollection();
        //  configureServices?.Invoke(services);
        _serviceProvider = configureServices.BuildServiceProvider();
        return new PipelineBuilder(_serviceProvider);
    }
         

    [Fact]
    public async Task StepIndicesAreAssignedInOrderOfAddition()
    {
        // Arrange
        var stepIndices = new List<int>();
        var inspector = new TestCallbackInspector(context =>
        {
            stepIndices.Add(context.PipelineContext.CurrentStepIndex);
        });

        var builder = CreatePipelineBuilder()
         .AddInspector(inspector)
         .Use(next => async ctx => await next(ctx), "Step1")
         .Use(next => async ctx => await next(ctx), "Step2");

        // Act
        var pipeline = builder.Build();
        await pipeline.Run();

        // Assert
        Assert.Equal(new[] { 0, 1 }, stepIndices);
    }
}
