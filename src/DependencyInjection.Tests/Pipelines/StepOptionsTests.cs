namespace Tests.Pipelines;

using System.Collections.Concurrent;
using Dazinator.Extensions.Pipelines;
using Dazinator.Extensions.Pipelines.Features.StepOptions;
using Xunit;

public partial class StepOptionsTests
{


    [Fact]
    public async Task CanConfigureAndReadStepOptions()
    {
        // Arrange
        var testOptions = new TestStepOptions { Value = "test" };
        var optionsVerified = false;

        var inspector = new TestCallbackInspector(context =>
        {
            var options = context.PipelineContext.StepState.Get<TestStepOptions>();
            Assert.Equal(testOptions.Value, options?.Value);
            optionsVerified = true;
        });

        var builder = new PipelineBuilder()
            .AddInspector(inspector)
            .RunAsync(async ctx => await Task.CompletedTask)
            .Configure<TestStepOptions>(options =>
            {
                options.Value = testOptions.Value;
            });

        // Act
        var pipeline = builder.Build(new ServiceCollection().BuildServiceProvider());
        await pipeline.Run();

        // Assert
        Assert.True(optionsVerified);
    }

    [Fact]
    public async Task OptionsAreScopedToStep()
    {
        // Arrange
        var optionsChecks = new List<string>();
        var wrappingChecks = new List<string>();

        var builder = new PipelineBuilder()
            .Use(next => async ctx =>
            {
                var options = ctx.StepState.Get<TestStepOptions>();
                optionsChecks.Add($"Step0:{options?.Value ?? "null"}");
                await next(ctx);
            }, "Step1")
            .Configure<TestStepOptions>(options =>
            {
                wrappingChecks.Add("Wrapping Step1");
                options.Value = "Step1Options";
            })
            .Use(next => async ctx =>
            {
                var options = ctx.StepState.Get<TestStepOptions>();
                optionsChecks.Add($"Step1:{options?.Value ?? "null"}");
                await next(ctx);
            }, "Step2")
            .Configure<TestStepOptions>(options =>
            {
                wrappingChecks.Add("Wrapping Step2");
                options.Value = "Step2Options";
            });

        // Act
        var pipeline = builder.Build(new ServiceCollection().BuildServiceProvider());
        await pipeline.Run();

        // Assert
        Assert.Equal(new[] { "Wrapping Step1", "Wrapping Step2" }, wrappingChecks);
        Assert.Equal(new[]
        {
        "Step0:Step1Options",
        "Step1:Step2Options"
    }, optionsChecks);
    }

    [Fact]
    public async Task MultipleOptionTypesCanCoexist()
    {
        // Arrange
        var optionsVerified = false;
        var inspector = new TestCallbackInspector(context =>
        {
            if (context.PipelineContext.CurrentStepIndex == 0) // Check first step
            {
                var options1 = context.PipelineContext.StepState.Get<TestStepOptions>();
                var options2 = context.PipelineContext.StepState.Get<AnotherTestOptions>();
                Assert.Equal("test1", options1?.Value);
                Assert.Equal("test2", options2?.OtherValue);
                optionsVerified = true;
            }
        });

        var builder = new PipelineBuilder()
            .AddInspector(inspector)
            .Use(next => async ctx => await next(ctx))
            .Configure<TestStepOptions>(options =>
            {
                options.Value = "test1";
            })
            .Configure<AnotherTestOptions>(options =>
            {
                options.OtherValue = "test2";
            });

        // Act
        var pipeline = builder.Build(new ServiceCollection().BuildServiceProvider());
        await pipeline.Run();

        // Assert
        Assert.True(optionsVerified);
    }

    [Fact]
    public async Task OptionsShouldBeIsolatedInParallelBranches()
    {
        // Arrange
        var optionsChecks = new ConcurrentBag<string>();
        var items = new[] { "1", "2" };

        var inspector = new TestCallbackInspector(context =>
        {
            var options = context.PipelineContext.StepState.Get<TestStepOptions>();
            if (options != null) // Only record when options are present
            {
                optionsChecks.Add($"Branch{options.Value}");
            }
        });

        var builder = new PipelineBuilder()
            .UseParallelBranches(items, (branch, item) =>
            {
                branch
                    .AddInspector(inspector)
                    .Use(next => async ctx => await next(ctx))
                    .Configure<TestStepOptions>(options =>
                    {
                        options.Value = $"{item}";
                    });
            });

        // Act
        var pipeline = builder.Build(new ServiceCollection().BuildServiceProvider());
        await pipeline.Run();

        // Assert
        Assert.Contains("Branch1", optionsChecks);
        Assert.Contains("Branch2", optionsChecks);
    }

    private class TestStepOptions : IStepOptions
    {
        public string? Value { get; set; }
    }

    private class AnotherTestOptions : IStepOptions
    {
        public string? OtherValue { get; set; }
    }
   
}
