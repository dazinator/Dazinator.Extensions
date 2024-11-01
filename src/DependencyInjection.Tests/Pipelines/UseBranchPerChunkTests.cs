namespace Tests.Pipelines;

using Xunit.Abstractions;
using Xunit.Categories;
using Xunit;
using Dazinator.Extensions.Pipelines;
using System.Collections.Concurrent;
using Dazinator.Extensions.DependencyInjection;
using Dazinator.Extensions.Pipelines.Features.Diagnostics;
using Dazinator.Extensions.Pipelines.Features.Branching;
using Dazinator.Extensions.Pipelines.Features.Branching.Chunk;

[UnitTest]
public class UseBranchPerChunkTests
{
    private IServiceProvider? _serviceProvider;

    public ConcurrencyMonitorInspector? ConcurrencyInspector { get; private set; }

    public UseBranchPerChunkTests(ITestOutputHelper testOutputHelper)
    {
        TestOutputHelper = testOutputHelper;
    }

    public ITestOutputHelper TestOutputHelper { get; }

    private IPipelineBuilder CreatePipelineBuilder(IServiceCollection? services = null)
    {
        services ??= new ServiceCollection();
        services.AddPipelines((builder) =>
        {           
            builder.AddConcurrencyMonitorInspector();           
        });

        _serviceProvider = services.BuildServiceProvider();
        this.ConcurrencyInspector = _serviceProvider.GetRequiredService<ConcurrencyMonitorInspector>();
        return new PipelineBuilder(_serviceProvider)
            .UseFilters()
            .AddInspector(this.ConcurrencyInspector);
    }
     

    [Fact]
    public async Task WithChunks_ProcessesChunks()
    {
        // Arrange
        var processedChunks = new ConcurrentBag<string[]>();

        // Act
        var builder = CreatePipelineBuilder()
            .UseBranchPerInputs<string>(branch =>
            {
                branch.Run(() => processedChunks.Add(branch.Input.ToArray()));
            })
            .WithChunks(
                new[] { "1", "2", "3", "4", "5" },
                chunkSize: 2);

        var pipeline = builder.Build();
        await pipeline.Run();

        // Assert
        Assert.Equal(3, processedChunks.Count); // 2 chunks of 2, 1 chunk of 1
        Assert.Contains(processedChunks, chunk => chunk.SequenceEqual(new[] { "1", "2" }));
        Assert.Contains(processedChunks, chunk => chunk.SequenceEqual(new[] { "3", "4" }));
        Assert.Contains(processedChunks, chunk => chunk.SequenceEqual(new[] { "5" }));
    }

    [Documentation]
    [Fact]
    public async Task WithChunks_ProcessesChunks_WithConcurrencyControl()
    {
        // Arrange
        var processedChunks = new ConcurrentBag<string[]>();

        // Act
        var builder = CreatePipelineBuilder()
            .UseBranchPerInputs<string>(branch =>
            {
                branch.Run(() => processedChunks.Add(branch.Input.ToArray()));
            })
            .WithChunks(
                new[] { "1", "2", "3", "4", "5" },
                chunkSize: 2, (options) => {
                    options.MaxDegreeOfParallelism = 2;                  
                });

        var pipeline = builder.Build();
        await pipeline.Run();

        // Assert
        Assert.Equal(3, processedChunks.Count); // 2 chunks of 2, 1 chunk of 1
        Assert.Contains(processedChunks, chunk => chunk.SequenceEqual(new[] { "1", "2" }));
        Assert.Contains(processedChunks, chunk => chunk.SequenceEqual(new[] { "3", "4" }));
        Assert.Contains(processedChunks, chunk => chunk.SequenceEqual(new[] { "5" }));
    }
   

    [Fact]
    public async Task WithChunks_RespectsParallelOptions()
    {
        // Arrange
        var concurrentExecutions = 0;
        var maxConcurrentExecutions = 0;
        var processedChunks = new ConcurrentBag<string[]>();
        var syncLock = new object();

        // Act
        var builder = CreatePipelineBuilder()
            .UseBranchPerInputs<string>(branch =>
            {
                branch.Run(async () =>
                {
                    lock (syncLock)
                    {
                        concurrentExecutions++;
                        maxConcurrentExecutions = Math.Max(maxConcurrentExecutions, concurrentExecutions);
                    }

                    await Task.Delay(50); // Simulate work
                    processedChunks.Add(branch.Input.ToArray());

                    lock (syncLock)
                    {
                        concurrentExecutions--;
                    }
                });
            })
            .WithChunks(
                new[] { "1", "2", "3", "4", "5", "6" },
                chunkSize: 2,
                options => options.MaxDegreeOfParallelism = 2);

        var pipeline = builder.Build();
        await pipeline.Run();

        // Assert
        Assert.Equal(3, processedChunks.Count); // 3 chunks of 2
        Assert.Equal(2, maxConcurrentExecutions);
    }

    [Fact]
    public async Task WithChunks_CapturesExceptions()
    {
        // Arrange
        var builder = CreatePipelineBuilder()
            .UseBranchPerInputs<string>(branch =>
            {
                branch.Run(() =>
                {
                    if (branch.Input.Contains("3"))
                    {
                        throw new InvalidOperationException("Test exception");
                    }
                });
            })
            .WithChunks(
                new[] { "1", "2", "3", "4", "5" },
                chunkSize: 2, (options) => {
                    options.MaxDegreeOfParallelism = 2;
                });

        var pipeline = builder.Build();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.Run());
    }

    [Fact]
    public async Task WithChunks_HandlesEmptyInput()
    {
        // Arrange
        var executed = false;
        var builder = CreatePipelineBuilder()
            .UseBranchPerInputs<string>(branch =>
            {
                branch.Run(() => executed = true);
            })
            .WithChunks(
                Array.Empty<string>(),
                chunkSize: 2);

        var pipeline = builder.Build();
        await pipeline.Run();

        // Assert
        Assert.False(executed);
    }

}
