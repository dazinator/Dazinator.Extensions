namespace Tests.Pipelines;

using Dazinator.Extensions.Pipelines;
using Xunit;
using Xunit.Abstractions;
using Xunit.Categories;

[UnitTest]
public class AsyncStreamTests
{
    private IServiceProvider? _serviceProvider;
    private readonly ITestOutputHelper _testOutput;
    private static List<int> _processedItems = new();
    private static List<string> _executionOrder = new();

    public AsyncStreamTests(ITestOutputHelper testOutput)
    {
        _testOutput = testOutput;
        _processedItems.Clear();
        _executionOrder.Clear();
    }

    private IPipelineBuilder CreatePipelineBuilder(IServiceCollection? services = null)
    {
        services ??= new ServiceCollection();
        _serviceProvider = services.BuildServiceProvider();
        return new PipelineBuilder(_serviceProvider);
    }

    [Fact]
    public async Task UseAsyncIterator_ProcessesAllItems()
    {
        // Arrange
        var items = new[] { 1, 2, 3 };
        var builder = CreatePipelineBuilder()
            .UseAsyncStream<int>(ctx => GetItemsAsync(items))
            .Use(next => async ctx =>
            {
                var item = ctx.GetCurrentItem<int>();
                _processedItems.Add(item);
                await next(ctx);
            });

        // Act
        await builder.Build().Run();

        // Assert
        Assert.Equal(items, _processedItems);
    }

    [Fact]
    public async Task UseAsyncIterator_ValueTypes_NoBoxing()
    {
        // Arrange
        var items = new[] { 1, 2, 3 };
        var processedSum = 0;

        var builder = CreatePipelineBuilder()
            .UseAsyncStream<int>(ctx => GetItemsAsync(items))
            .Use(next => async ctx =>
            {
                var item = ctx.GetCurrentItem<int>();
                processedSum += item;  // Working directly with value type
                await next(ctx);
            });

        // Act
        await builder.Build().Run();

        // Assert
        Assert.Equal(items.Sum(), processedSum);
    }

    [Fact]
    public async Task UseAsyncIterator_ExecutionOrder_IsCorrect()
    {
        // Arrange
        var items = new[] { 1, 2 };
        var builder = CreatePipelineBuilder()
            .Run(() => _executionOrder.Add("Before"))
            .UseAsyncStream<int>(ctx => GetItemsAsync(items))
            .Use(next => async ctx =>
            {
                var item = ctx.GetCurrentItem<int>();
                _executionOrder.Add($"Item{item}");
                await next(ctx);
            })
            .Run(() => _executionOrder.Add("After"));

        // Act
        await builder.Build().Run();

        // Assert
        Assert.Equal(
            new[] { "Before", "Item1", "Item2", "After" },
            _executionOrder
        );
    }

    [Fact]
    public async Task UseAsyncIterator_EmptySource_ContinuesPipeline()
    {
        // Arrange
        var executed = false;
        var builder = CreatePipelineBuilder()
            .UseAsyncStream<int>(ctx => GetItemsAsync(Array.Empty<int>()))
            .Run(() => executed = true);

        // Act
        await builder.Build().Run();

        // Assert
        Assert.True(executed, "Pipeline should continue after empty iterator");
    }

    [Fact]
    public async Task UseAsyncIterator_SourceThrows_PropagatesException()
    {
        // Arrange
        var builder = CreatePipelineBuilder()
            .UseAsyncStream<int>(ctx => ThrowingSourceAsync());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await builder.Build().Run();
        });
    }

    [Fact]
    public async Task UseAsyncIterator_Cancellation_StopsIteration()
    {
        // Arrange
        var items = Enumerable.Range(1, 1000);  // Large range
        var processedCount = 0;
        using var cts = new CancellationTokenSource();

        var builder = CreatePipelineBuilder()
            .UseAsyncStream<int>(ctx => GetItemsAsync(items))
            .Use(next => async ctx =>
            {
                processedCount++;
                if (processedCount == 5)  // Cancel after 5 items
                {
                    cts.Cancel();
                }
                await next(ctx);
            });

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await builder.Build().Run(cts.Token);
        });
        Assert.Equal(5, processedCount);
    }

    [Fact]
    public async Task UseAsyncIterator_AccessingCurrentItemOutsideIterator_ThrowsException()
    {
        // Arrange
        var builder = CreatePipelineBuilder()
            .Run(ctx =>
            {
                // Act & Assert
                Assert.Throws<InvalidOperationException>(() =>
                {
                    ctx.GetCurrentItem<int>();
                });
            });

        await builder.Build().Run();
    }

    [Fact]
    public async Task UseAsyncIterator_TryGetCurrentItem_ReturnsExpectedResults()
    {
        // Arrange
        var items = new[] { 42 };
        var outsideIterator = false;
        var insideIterator = false;

        var builder = CreatePipelineBuilder()
            .Run(ctx =>
            {
                outsideIterator = ctx.TryGetCurrentItem<int>(out var _);
            })
            .UseAsyncStream<int>(ctx => GetItemsAsync(items))
            .Use(next => async ctx =>
            {
                insideIterator = ctx.TryGetCurrentItem<int>(out var item);
                Assert.Equal(42, item);
                await next(ctx);
            });

        // Act
        await builder.Build().Run();

        // Assert
        Assert.False(outsideIterator);
        Assert.True(insideIterator);
    }

    private static async IAsyncEnumerable<T> GetItemsAsync<T>(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            await Task.Delay(1);  // Simulate async work
            yield return item;
        }
    }

    private static async IAsyncEnumerable<int> ThrowingSourceAsync()
    {
        await Task.Delay(1);
        throw new InvalidOperationException("Test exception");
        yield break;
    }
}
