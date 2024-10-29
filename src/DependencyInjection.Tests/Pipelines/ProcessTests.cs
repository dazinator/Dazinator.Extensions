namespace Tests.Pipelines;

using Xunit.Abstractions;
using Xunit.Categories;
using Xunit;
using Dazinator.Extensions.Pipelines;
using System.Collections.Concurrent;
using Dazinator.Extensions.Pipelines.Features.Process.PerItem;

[UnitTest]
public class ProcessTests
{
    private IServiceProvider? _serviceProvider;

    public ProcessTests(ITestOutputHelper testOutputHelper)
    {
        TestOutputHelper = testOutputHelper;
    }

    public ITestOutputHelper TestOutputHelper { get; }

    private IPipelineBuilder CreatePipelineBuilder(IServiceCollection? configureServices = null)
    {
        configureServices ??= new ServiceCollection();
        _serviceProvider = configureServices.BuildServiceProvider();
        return new PipelineBuilder(_serviceProvider).UseFilters();
    }

    [Fact]
    public async Task ProcessItem_ExecutesForOneItem()
    {
        // Arrange
        var processedItems = new ConcurrentBag<string>();

        // Act
        var builder = CreatePipelineBuilder()
            .ProcessItem<string>(branch =>
            {
                branch.Run(() =>
                {

                    processedItems.Add(branch.Item);
                });
            })
            .WithItems(new[] { "item1" });

        var pipeline = builder.Build();
        await pipeline.Run();

        // Assert
        Assert.Equal(1, processedItems.Count);
        Assert.Contains("item1", processedItems);
        //Assert.Contains("item2", processedItems);
        //Assert.Contains("item3", processedItems);
    }

    [Fact]
    public async Task ProcessItem_ExecutesForEachItem()
    {
        // Arrange
        var processedItems = new ConcurrentBag<string>();

        // Act
        var builder = CreatePipelineBuilder()
            .ProcessItem<string>(branch =>
            {
                branch.Run(() => processedItems.Add(branch.Item));
            })
            .WithItems(new[] { "item1", "item2", "item3" });

        var pipeline = builder.Build();
        await pipeline.Run();

        // Assert
        Assert.Equal(3, processedItems.Count);
        Assert.Contains("item1", processedItems);
        Assert.Contains("item2", processedItems);
        Assert.Contains("item3", processedItems);
    }

    [Fact]
    public async Task ProcessItem_RespectsParallelOptions()
    {
        // Arrange
        var concurrentExecutions = 0;
        var maxConcurrentExecutions = 0;
        var processedItems = new ConcurrentBag<string>();
        var syncLock = new object();

        // Act
        var builder = CreatePipelineBuilder()
            .ProcessItem<string>(branch =>
            {
                branch.Run(async () =>
                {
                    lock (syncLock)
                    {
                        concurrentExecutions++;
                        maxConcurrentExecutions = Math.Max(maxConcurrentExecutions, concurrentExecutions);
                    }

                    await Task.Delay(50); // Simulate work
                    TestOutputHelper.WriteLine($"Processing item: {branch.Item}");
                    processedItems.Add(branch.Item);

                    lock (syncLock)
                    {
                        concurrentExecutions--;
                    }
                });
            })
            .WithItems(
                new[] { "item1", "item2", "item3", "item4", "item5" },
                options => options.MaxDegreeOfParallelism = 2);

        var pipeline = builder.Build();
        await pipeline.Run();

        // Assert
        TestOutputHelper.WriteLine($"Max concurrency: {maxConcurrentExecutions}");
        Assert.Equal(2, maxConcurrentExecutions);
        Assert.Equal(5, processedItems.Count);

    }

    [Fact]
    public async Task ProcessItems_ProcessesChunks()
    {
        // Arrange
        var processedChunks = new ConcurrentBag<string[]>();

        // Act
        var builder = CreatePipelineBuilder()
            .ProcessItems<string>(branch =>
            {
                branch.Run(() => processedChunks.Add(branch.Items.ToArray()));
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

    [Fact]
    public async Task ProcessItems_RespectsParallelOptions()
    {
        // Arrange
        var concurrentExecutions = 0;
        var maxConcurrentExecutions = 0;
        var processedChunks = new ConcurrentBag<string[]>();
        var syncLock = new object();

        // Act
        var builder = CreatePipelineBuilder()
            .ProcessItems<string>(branch =>
            {
                branch.Run(async () =>
                {
                    lock (syncLock)
                    {
                        concurrentExecutions++;
                        maxConcurrentExecutions = Math.Max(maxConcurrentExecutions, concurrentExecutions);
                    }

                    await Task.Delay(50); // Simulate work
                    processedChunks.Add(branch.Items.ToArray());

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
    public async Task ProcessItem_CapturesExceptions()
    {
        // Arrange
        var builder = CreatePipelineBuilder()
            .ProcessItem<string>(branch =>
            {
                branch.Run(() =>
                {
                    if (branch.Item == "item2")
                        throw new InvalidOperationException("Test exception");
                });
            })
            .WithItems(new[] { "item1", "item2", "item3" });

        var pipeline = builder.Build();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.Run());
    }

    [Fact]
    public async Task ProcessItems_HandlesEmptyInput()
    {
        // Arrange
        var executed = false;
        var builder = CreatePipelineBuilder()
            .ProcessItems<string>(branch =>
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

