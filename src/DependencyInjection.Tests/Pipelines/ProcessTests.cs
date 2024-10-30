namespace Tests.Pipelines;

using Xunit.Abstractions;
using Xunit.Categories;
using Xunit;
using Dazinator.Extensions.Pipelines;
using System.Collections.Concurrent;
using Dazinator.Extensions.Pipelines.Features.Process.PerItem;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Dazinator.Extensions.Pipelines.Features.Inspector;
using Microsoft.Extensions.Logging;
using Dazinator.Extensions.DependencyInjection;
using Dazinator.Extensions.Pipelines.Features.Diagnostics;
using Autofac.Core;

[UnitTest]
public class ProcessTests
{
    private IServiceProvider? _serviceProvider;

    public ProcessTests(ITestOutputHelper testOutputHelper)
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
        return new PipelineBuilder(_serviceProvider)
            .UseFilters()
            .AddInspector<ConcurrencyMonitorInspector>();
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
        var semaphore = new SemaphoreSlim(1, 1);
        var services = new ServiceCollection().AddLogging((b) =>
        b.AddXUnit(TestOutputHelper));      

        var executionTimeline = new ConcurrentDictionary<string, List<string>>();      
        // var concurrencyMonitor = new ConcurrencyMonitorInspector(_serviceProvider.GetRequiredService);

        // Act
        var builder = CreatePipelineBuilder(services);     
        builder
            .ProcessItem<string>(branch =>
            {
                branch.Run(async () =>
                {
                    var item = branch.Item;
                    var threadId = Environment.CurrentManagedThreadId;
                    var timeline = new List<string>();
                    executionTimeline[item] = timeline;

                    try
                    {
                        await semaphore.WaitAsync();
                        concurrentExecutions = Interlocked.Increment(ref concurrentExecutions);
                        maxConcurrentExecutions = Math.Max(maxConcurrentExecutions, concurrentExecutions);
                        timeline.Add($"Start {item} - Time: {DateTime.Now:mm:ss.fff} - Concurrent: {concurrentExecutions}");
                        TestOutputHelper.WriteLine($"Start {item} - Time: {DateTime.Now:mm:ss.fff} - Thread: {threadId} - Concurrent: {concurrentExecutions}");
                        semaphore.Release();

                        // Track that we're still executing
                        timeline.Add($"Delay start {item} - Time: {DateTime.Now:mm:ss.fff}");
                        await Task.Delay(100); // Simulate work
                        timeline.Add($"Delay end {item} - Time: {DateTime.Now:mm:ss.fff}");

                        await semaphore.WaitAsync();
                        processedItems.Add(item);
                        concurrentExecutions = Interlocked.Decrement(ref concurrentExecutions);
                        timeline.Add($"End {item} - Time: {DateTime.Now:mm:ss.fff} - Concurrent: {concurrentExecutions}");
                        TestOutputHelper.WriteLine($"End {item} - Time: {DateTime.Now:mm:ss.fff} - Thread: {threadId} - Concurrent: {concurrentExecutions}");
                        semaphore.Release();
                    }
                    catch (Exception ex)
                    {
                        timeline.Add($"Error {item}: {ex.Message}");
                        throw;
                    }
                }, "B");
            }, "A")
            .WithItems(
                new[] { "item1", "item2", "item3", "item4", "item5" },
                options => options.MaxDegreeOfParallelism = 2);

        var pipeline = builder.Build();
        await pipeline.Run();

        var concurrencyMonitorInspector = pipeline.FindInspector<ConcurrencyMonitorInspector>();
        // Get the concurrency report
        var report = concurrencyMonitorInspector?.GenerateReport();
        TestOutputHelper.WriteLine(report?.ToString());


        // Print timeline
        TestOutputHelper.WriteLine("\nExecution Timeline:");
        foreach (var entry in executionTimeline.OrderBy(e => e.Key))
        {
            TestOutputHelper.WriteLine($"\n{entry.Key}:");
            foreach (var event_ in entry.Value)
            {
                TestOutputHelper.WriteLine($"  {event_}");
            }
        }

        // Assert
        TestOutputHelper.WriteLine($"\nMax concurrency: {maxConcurrentExecutions}");
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


    [Exploratory]
    [Fact]
    public async Task ParallelForEachAsync_ShouldRespectMaxConcurrency()
    {
        // Arrange
        const int maxConcurrency = 2;
        const int totalItems = 10;
        const int delayMs = 100;

        var items = Enumerable.Range(1, totalItems).ToList();
        var processedItems = new ConcurrentBag<int>();

        var currentConcurrency = 0;
        var maxObservedConcurrency = 0;
        var concurrencyLock = new SemaphoreSlim(1, 1);

        // Act
        await Parallel.ForEachAsync(
            items,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = maxConcurrency
            },
            async (item, ct) =>
            {
                try
                {
                    // Track concurrency at the start
                    await concurrencyLock.WaitAsync();
                    currentConcurrency++;
                    maxObservedConcurrency = Math.Max(maxObservedConcurrency, currentConcurrency);

                    var threadId = Environment.CurrentManagedThreadId;
                    TestOutputHelper.WriteLine($"Starting item {item} on thread {threadId}. Current concurrency: {currentConcurrency}");

                    concurrencyLock.Release();

                    // Simulate some async work
                    await Task.Delay(delayMs, ct);
                    processedItems.Add(item);

                    // Track concurrency at the end
                    await concurrencyLock.WaitAsync();
                    currentConcurrency--;
                    TestOutputHelper.WriteLine($"Completed item {item} on thread {threadId}. Current concurrency: {currentConcurrency}");
                    concurrencyLock.Release();
                }
                catch (Exception ex)
                {
                    TestOutputHelper.WriteLine($"Error processing item {item}: {ex}");
                    throw;
                }
            });

        // Assert
        Assert.Equal(totalItems, processedItems.Count);
        Assert.Equal(maxConcurrency, maxObservedConcurrency);

        TestOutputHelper.WriteLine($"Maximum observed concurrency: {maxObservedConcurrency}");
        TestOutputHelper.WriteLine($"Total items processed: {processedItems.Count}");

        // Verify all items were processed exactly once
        var processedSet = processedItems.ToHashSet();
        Assert.Equal(totalItems, processedSet.Count);
        Assert.All(items, item => Assert.Contains(item, processedSet));
    }

    [Exploratory]
    [Fact]
    public async Task ParallelForEachAsync_ShouldRespectMaxConcurrency_NoSemaphore()
    {
        // Arrange
        const int maxConcurrency = 2;
        const int totalItems = 10;
        const int delayMs = 100;

        var items = Enumerable.Range(1, totalItems).ToList();
        var processedItems = new ConcurrentBag<int>();

        var currentConcurrency = 0;
        var maxObservedConcurrency = 0;
        //var concurrencyLock = new SemaphoreSlim(1, 1);

        var syncLock = new object();

        // Act
        await Parallel.ForEachAsync(
            items,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = maxConcurrency
            },
            async (item, ct) =>
            {
                try
                {
                    // Track concurrency at the start

                    lock (syncLock)
                    {
                        currentConcurrency++;
                        maxObservedConcurrency = Math.Max(maxObservedConcurrency, currentConcurrency);
                    }

                    var threadId = Environment.CurrentManagedThreadId;
                    TestOutputHelper.WriteLine($"Starting item {item} on thread {threadId}. Current concurrency: {currentConcurrency}");

                    await Task.Delay(50); // Simulate work
                    processedItems.Add(item);

                    TestOutputHelper.WriteLine($"Completed item {item} on thread {threadId}. Current concurrency: {currentConcurrency}");

                    lock (syncLock)
                    {
                        currentConcurrency--;
                    }



                }
                catch (Exception ex)
                {
                    TestOutputHelper.WriteLine($"Error processing item {item}: {ex}");
                    throw;
                }
            });

        // Assert
        Assert.Equal(totalItems, processedItems.Count);
        Assert.Equal(maxConcurrency, maxObservedConcurrency);

        TestOutputHelper.WriteLine($"Maximum observed concurrency: {maxObservedConcurrency}");
        TestOutputHelper.WriteLine($"Total items processed: {processedItems.Count}");

        // Verify all items were processed exactly once
        var processedSet = processedItems.ToHashSet();
        Assert.Equal(totalItems, processedSet.Count);
        Assert.All(items, item => Assert.Contains(item, processedSet));
    }

}





