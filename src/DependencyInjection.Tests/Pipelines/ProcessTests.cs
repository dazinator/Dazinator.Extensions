namespace Tests.Pipelines;

using Xunit.Abstractions;
using Xunit.Categories;
using Xunit;
using Dazinator.Extensions.Pipelines;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Dazinator.Extensions.DependencyInjection;
using Dazinator.Extensions.Pipelines.Features.Diagnostics;
using Dazinator.Extensions.Pipelines.Features.Skip;

[UnitTest]
public class ProcessTests
{
    private IServiceProvider? _serviceProvider;

    public ConcurrencyMonitorInspector? ConcurrencyInspector { get; private set; }

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
        this.ConcurrencyInspector = _serviceProvider.GetRequiredService<ConcurrencyMonitorInspector>();
        return new PipelineBuilder(_serviceProvider)
            .UseFilters()
            .AddInspector(this.ConcurrencyInspector);
    }

    [Fact]
    public async Task ProcessItem_ExecutesForOneItem()
    {
        // Arrange
        var processedItems = new ConcurrentBag<string>();

        // Act
        var builder = CreatePipelineBuilder()
            .UseBranchPerInput<string>(branch =>
            {
                branch.Run(() =>
                {

                    processedItems.Add(branch.Input);
                });
            })
            .WithInputs(new[] { "item1" });

        var pipeline = builder.Build();
        await pipeline.Run();

        // Assert
        Assert.Single(processedItems);
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
            .UseBranchPerInput<string>(branch =>
            {
                branch.Run(() => processedItems.Add(branch.Input));
            })
            .WithInputs(new[] { "item1", "item2", "item3" });

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
        var enableDiagnostics = false; // Flag to enable/disable diagnostic logging

        var services = new ServiceCollection().AddLogging(b =>
            b.AddXUnit(TestOutputHelper));

        // Act
        var builder = CreatePipelineBuilder(services);
        builder
            .UseBranchPerInput<string>(branch =>
            {
                branch.Run(async () =>
                {
                    try
                    {
                        // Track concurrent executions
                        await semaphore.WaitAsync();
                        var current = Interlocked.Increment(ref concurrentExecutions);
                        maxConcurrentExecutions = Math.Max(maxConcurrentExecutions, current);

                        if (enableDiagnostics)
                        {
                            TestOutputHelper.WriteLine($"Start {branch.Input} - Concurrent: {current}");
                        }

                        semaphore.Release();

                        await Task.Delay(100); // Simulate work

                        await semaphore.WaitAsync();
                        processedItems.Add(branch.Input);
                        Interlocked.Decrement(ref concurrentExecutions);

                        if (enableDiagnostics)
                        {
                            TestOutputHelper.WriteLine($"End {branch.Input} - Concurrent: {current - 1}");
                        }

                        semaphore.Release();
                    }
                    catch when (enableDiagnostics)
                    {
                        throw;
                    }
                }, "ProcessItem");
            })
            .WithInputs(
                new[] { "item1", "item2", "item3", "item4", "item5" },
                options => options.MaxDegreeOfParallelism = 2);

        await builder.Build().Run();

        // Assert
        if (enableDiagnostics)
        {
            TestOutputHelper.WriteLine($"Max concurrency: {maxConcurrentExecutions}");

            var report = ConcurrencyInspector?.GenerateReport();
            TestOutputHelper.WriteLine(report?.ToString());
        }

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
    public async Task ProcessItems_ProcessesChunks_WithConcurrencyControl()
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

    [Documentation]
    [Fact]
    public async Task ProcessItems_ProcessesChunks_WithConcurrencyControlAndFilters_Docs()
    {       

        // Act
        var builder = CreatePipelineBuilder()
            .UseBranchPerInputs<string>(branch =>
            {
                branch.Run(async () => TestOutputHelper.WriteLine($"Processing orders {string.Join(",", branch.Input.ToArray())}")); // got the chunk of items
                branch.Run(async () => TestOutputHelper.WriteLine($"Some other task"));
            }, "process-order-ids")
             .WithChunks(
                new[] { "1", "2", "3", "4", "5" },
                chunkSize: 2, (options) =>
                {
                    options.MaxDegreeOfParallelism = 2;
                })
             .WithSkipConditionAsync((ctx) => Task.FromResult(true)) // skip process-order-ids as feature disabled.
           .Run(()=>TestOutputHelper.WriteLine("About to run notifications"))
           .UseBranchPerInput<string>(branch =>
           {
               branch.Run(() => TestOutputHelper.WriteLine($"Logging email addresses for {branch.Input} notifications.."))
                       .WithSkipCondition(() => branch.Input != "Email") // we skip logging email addresses for non email notifications.

                     .Run(() => TestOutputHelper.WriteLine($"Send {branch.Input} notifications.."))
                      
           })
            .WithInputs(
                new[] { "Email", "Web" },
                chunkSize: 2, (options) =>
                {
                    options.MaxDegreeOfParallelism = 2;
                })           
            .Run(() => TestOutputHelper.WriteLine("Finished"));




        var pipeline = builder.Build();
        await pipeline.Run();
      
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
    public async Task ProcessItem_CapturesExceptions()
    {
        // Arrange
        var builder = CreatePipelineBuilder()
            .UseBranchPerInput<string>(branch =>
            {
                branch.Run(() =>
                {
                    if (branch.Input == "item2")
                    {
                        throw new InvalidOperationException("Test exception");
                    }
                });
            })
            .WithInputs(new[] { "item1", "item2", "item3" });

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





