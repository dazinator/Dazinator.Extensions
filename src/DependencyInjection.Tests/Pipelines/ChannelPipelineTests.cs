namespace Tests.Pipelines;

using Xunit.Abstractions;
using Xunit.Categories;
using Xunit;
using Dazinator.Extensions.Pipelines;
using System.Collections.Concurrent;
using Dazinator.Extensions.DependencyInjection;
using Dazinator.Extensions.Pipelines.Features.Diagnostics;
using System.Threading.Channels;

[UnitTest]
public class ChannelPipelineTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private IServiceProvider? _serviceProvider;
    public ConcurrencyMonitorInspector? ConcurrencyInspector { get; private set; }

    public ChannelPipelineTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    private IPipelineBuilder CreatePipelineBuilder(IServiceCollection? services = null)
    {
        services ??= new ServiceCollection();
        services.AddPipelines((builder) =>
        {
            builder.AddConcurrencyMonitorInspector();
        });

        _serviceProvider = services.BuildServiceProvider();
        ConcurrencyInspector = _serviceProvider.GetRequiredService<ConcurrencyMonitorInspector>();
        return new PipelineBuilder(_serviceProvider)
            .UseFilters()
            .AddInspector(ConcurrencyInspector);
    }

    //[Fact]
    //public async Task SingleReaderWriter_ProcessesAllItems()
    //{
    //    // Arrange
    //    var processedItems = new ConcurrentDictionary<int, string>();
    //    const int itemCount = 100;

    //    // Act
    //    var builder = CreatePipelineBuilder()
    //        .UseChannel<string>(
    //            reader => {
    //                reader.Run((ctx) =>
    //                {
    //                    var item = ctx.Input;

    //                    _testOutputHelper.WriteLine($"Processing item: {item}");
    //                    var itemNumber = int.Parse(reader.Input.Replace("item", ""));
    //                    processedItems.TryAdd(itemNumber, reader.Input);
    //                });
    //                _testOutputHelper.WriteLine($"Processing item: {reader.Input}");
    //                var itemNumber = int.Parse(reader.Input.Replace("item", ""));
    //                processedItems.TryAdd(itemNumber, reader.Input);
    //            },
    //            writer => {
    //                writer.Run(async () => {
    //                    for (int i = 0; i < itemCount; i++)
    //                    {
    //                        var item = $"item{i}";
    //                        _testOutputHelper.WriteLine($"Writing item: {item}");
    //                        await writer.Writer.WriteAsync(item);
    //                    }
    //                    _testOutputHelper.WriteLine("Completing channel");
    //                    writer.Writer.Complete();
    //                });
    //            },
    //            options => {
    //                options.ReaderCount = 1;
    //                options.WriterCount = 1;
    //                options.MaxCapacity = 10;
    //            });

    //    await builder.Build().Run();

    //    // Assert
    //    Assert.Equal(itemCount, processedItems.Count);
    //    for (int i = 0; i < itemCount; i++)
    //    {
    //        Assert.Contains($"item{i}", processedItems.Values);
    //    }
    //}

    //[Fact]
    //public async Task MultipleReaders_ProcessAllItemsOnce()
    //{
    //    // Arrange
    //    var processedItems = new ConcurrentDictionary<int, string>();
    //    var concurrentReaders = 0;
    //    var maxConcurrentReaders = 0;
    //    var semaphore = new SemaphoreSlim(1, 1);
    //    const int itemCount = 100;
    //    const int readerCount = 3;

    //    // Act
    //    var builder = CreatePipelineBuilder()
    //        .UseChannel<string>(
    //            reader => {
    //                reader.Run(async () => {
    //                    try
    //                    {
    //                        await semaphore.WaitAsync();
    //                        var current = Interlocked.Increment(ref concurrentReaders);
    //                        maxConcurrentReaders = Math.Max(maxConcurrentReaders, current);
    //                        semaphore.Release();

    //                        await Task.Delay(10); // Simulate processing time
    //                        var itemNumber = int.Parse(reader.Input.Replace("item", ""));
    //                        processedItems.TryAdd(itemNumber, reader.Input);
    //                        _testOutputHelper.WriteLine($"Processing item: {reader.Input}");

    //                        await semaphore.WaitAsync();
    //                        Interlocked.Decrement(ref concurrentReaders);
    //                        semaphore.Release();
    //                    }
    //                    catch (Exception ex)
    //                    {
    //                        _testOutputHelper.WriteLine($"Error processing item: {ex}");
    //                        throw;
    //                    }
    //                });
    //            },
    //            writer => {
    //                writer.Run(async () => {
    //                    for (int i = 0; i < itemCount; i++)
    //                    {
    //                        await writer.Writer.WriteAsync($"item{i}");
    //                        await Task.Delay(1); // Small delay to ensure readers can catch up
    //                    }
    //                    writer.Writer.Complete();
    //                });
    //            },
    //            options => {
    //                options.ReaderCount = readerCount;
    //                options.WriterCount = 1;
    //                options.MaxCapacity = 20;
    //            });

    //    await builder.Build().Run();

    //    // Assert
    //    Assert.Equal(readerCount, maxConcurrentReaders);
    //    Assert.Equal(itemCount, processedItems.Count);
    //    Assert.Equal(itemCount, processedItems.Values.Distinct().Count()); // Each item processed exactly once
    //}

    //[Fact]
    //public async Task BoundedChannel_DropsItemsWhenFull()
    //{
    //    // Arrange
    //    var processedItems = new HashSet<int>();
    //    const int channelCapacity = 5;
    //    const int totalItems = 100;

    //    // Act
    //    var builder = CreatePipelineBuilder()
    //        .UseChannel<string>(
    //            reader => {
    //                var itemNumber = int.Parse(reader.Input.Replace("item", ""));
    //                processedItems.Add(itemNumber);
    //                _testOutputHelper.WriteLine($"Processing item: {reader.Input}");
    //            },
    //            writer => {
    //                writer.Run(async () => {
    //                    await Task.Delay(500); // Delay writer to ensure channel fills
    //                    for (int i = 0; i < totalItems; i++)
    //                    {
    //                        var item = $"item{i}";
    //                        await writer.Writer.WriteAsync(item);
    //                        _testOutputHelper.WriteLine($"Wrote item: {item}");
    //                    }
    //                    writer.Writer.Complete();
    //                });
    //            },
    //            options => {
    //                options.ReaderCount = 1;
    //                options.WriterCount = 1;
    //                options.MaxCapacity = channelCapacity;
    //                options.FullMode = BoundedChannelFullMode.DropWrite;
    //            });

    //    await builder.Build().Run();

    //    // Assert
    //    _testOutputHelper.WriteLine($"Processed {processedItems.Count} items");
    //    Assert.True(processedItems.Count < totalItems, "Some items should have been dropped");
    //    Assert.True(processedItems.Count >= channelCapacity,
    //        $"Should have processed at least {channelCapacity} items");
    //}

    //[Fact]
    //public async Task BoundedChannel_BuffersAndBlocksWhenFull()
    //{
    //    // Arrange
    //    var processedItems = new ConcurrentDictionary<int, string>();
    //    const int channelCapacity = 5;
    //    const int totalItems = 100;
    //    var writerProgress = new ConcurrentDictionary<int, DateTime>();

    //    // Act
    //    var builder = CreatePipelineBuilder()
    //        .UseChannel<string>(
    //            reader => {
    //                // Don't try to read from channel - middleware handles that
    //                // Just process the current item
    //                reader.Run(async () => {
    //                    var itemNumber = int.Parse(reader.Input.Replace("item", ""));
    //                    processedItems.TryAdd(itemNumber, reader.Input);
    //                    _testOutputHelper.WriteLine($"Processing item: {reader.Input}");
    //                    await Task.Delay(50); // Simulate slow processing
    //                });
    //            },
    //            writer => {
    //                writer.Run(async () => {
    //                    try
    //                    {
    //                        for (int i = 0; i < totalItems; i++)
    //                        {
    //                            var item = $"item{i}";
    //                            var beforeWrite = DateTime.UtcNow;
    //                            _testOutputHelper.WriteLine($"Writing item: {item}");
    //                            await writer.Writer.WriteAsync(item);
    //                            writerProgress[i] = DateTime.UtcNow;
    //                            var writeTime = DateTime.UtcNow - beforeWrite;
    //                            _testOutputHelper.WriteLine($"Wrote item: {item} (took: {writeTime.TotalMilliseconds:F1}ms)");
    //                        }
    //                    }
    //                    finally
    //                    {
    //                        _testOutputHelper.WriteLine("Completing channel");
    //                        writer.Writer.Complete();
    //                    }
    //                });
    //            },
    //            options => {
    //                options.ReaderCount = 1;
    //                options.WriterCount = 1;
    //                options.MaxCapacity = channelCapacity;
    //            });

    //    await builder.Build().Run();

    //    // Assert
    //    _testOutputHelper.WriteLine($"Total processed items: {processedItems.Count}");
    //    Assert.Equal(totalItems, processedItems.Count);

    //    // Verify all items were processed
    //    for (int i = 0; i < totalItems; i++)
    //    {
    //        Assert.True(processedItems.ContainsKey(i), $"Missing item{i}");
    //    }

    //    // Verify backpressure by checking time gaps between writes
    //    var progressTimes = writerProgress.OrderBy(kvp => kvp.Key)
    //        .Select(kvp => kvp.Value)
    //        .ToList();

    //    // Once buffer is full, writes should take longer
    //    for (int i = channelCapacity; i < progressTimes.Count - 1; i++)
    //    {
    //        var gap = progressTimes[i + 1] - progressTimes[i];
    //        _testOutputHelper.WriteLine($"Gap between writes {i} and {i + 1}: {gap.TotalMilliseconds:F1}ms");
    //        Assert.True(gap.TotalMilliseconds >= 40, $"Write {i} to {i + 1} should show backpressure");
    //    }
    //}
}





