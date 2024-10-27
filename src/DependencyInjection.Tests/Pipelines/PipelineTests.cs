namespace DependencyInjection.Tests.Pipelines;

using Dazinator.Extensions.Pipelines;
using global::Tests.Pipelines;
using Xunit;
using Xunit.Abstractions;
using Xunit.Categories;

[UnitTest]
public class PipelineBuilderTests
{

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0021:Use expression body for constructor", Justification = "<Pending>")]
    public PipelineBuilderTests(ITestOutputHelper testOutputHelper)
    {
        TestOutputHelper = testOutputHelper;
    }

    public ITestOutputHelper TestOutputHelper { get; }

    #region Middleware

    [Fact]
    public async Task ExecutesMiddlewareInOrder()
    {
        // Arrange
        var executionOrder = new List<string>();
        var services = new ServiceCollection();

        services.AddTransient<TestMiddleware>(_ =>
            new TestMiddleware(() => executionOrder.Add("First")));
        services.AddTransient<AnotherTestMiddleware>(_ =>
            new AnotherTestMiddleware(() => executionOrder.Add("Second")));

        var sp = services.BuildServiceProvider();

        // Act
        var builder = new PipelineBuilder()
            .UseMiddleware<TestMiddleware>("First")
            .UseMiddleware<AnotherTestMiddleware>("Second");

        var pipeline = builder.Build(sp);
        await pipeline.Run(default);

        // Assert
        Assert.Equal(new[] { "First", "Second" }, executionOrder);
    }

#if NET8_0
    [Fact]
    public async Task ExecutesKeyedMiddlewareInOrder()
    {
        // Arrange
        var executionOrder = new List<string>();
        var services = new ServiceCollection();

        services.AddKeyedTransient<TestMiddleware>("first", (sp, key) =>
            new TestMiddleware(() => executionOrder.Add("First")));
        services.AddKeyedTransient<TestMiddleware>("second", (sp, key) =>
            new TestMiddleware(() => executionOrder.Add("Second")));

        var sp = services.BuildServiceProvider();

        // Act
        var builder = new PipelineBuilder()
            .UseMiddleware<TestMiddleware>("first", "First")
            .UseMiddleware<TestMiddleware>("second", "Second");

        var pipeline = builder.Build(sp);
        await pipeline.Run(default);

        // Assert
        Assert.Equal(new[] { "First", "Second" }, executionOrder);
    }
#endif

    // Test middleware implementation
    private class TestMiddleware : IPipelineMiddleware
    {
        private readonly Action _action;

        public TestMiddleware(Action action)
        {
            _action = action;
        }

        public Task ExecuteAsync(PipelineStep next, PipelineContext context)
        {
            _action();
            return next(context);
        }
    }

    private class AnotherTestMiddleware : IPipelineMiddleware
    {
        private readonly Action _action;

        public AnotherTestMiddleware(Action action)
        {
            _action = action;
        }

        public Task ExecuteAsync(PipelineStep next, PipelineContext context)
        {
            _action();
            return next(context);
        }
    }
    // Test inspector implementation

    #endregion

    #region DI Scope
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
        var builder = new PipelineBuilder()
            .Use(next => async ct =>
            {
                executionOrder.Add("Before root scope");
                await next(ct);
                executionOrder.Add("After root scope");
            })
            .UseNewScope()
            .UseParallelBranches(
                new[] { "Branch1", "Branch2" },
                (branch, branchName) =>
                {
                    executionOrder.Add($"Configuring {branchName}");
                    branch
                        .Use(next => async ct =>
                        {
                            executionOrder.Add($"Before {branchName} scope");
                            await next(ct);
                            executionOrder.Add($"After {branchName} scope");
                        })
                        .UseNewScope()
                        .UseMiddleware<ScopedTestMiddleware>();
                }
            );

        var pipeline = builder.Build(sp);
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
    #endregion

    #region Cancellation

    [Fact]
    public async Task CancellationToken_IsRespectedInRunAsync()
    {
        // Arrange
        var services = new ServiceCollection().BuildServiceProvider();
        using var cts = new CancellationTokenSource();
        var taskWasCancelled = false;

        // Act
        var builder = new PipelineBuilder()
            .RunAsync(async context =>
            {
                try
                {
                    cts.Cancel();
                    await Task.Delay(5000, context.CancellationToken);
                }
                catch (OperationCanceledException)
                {
                    taskWasCancelled = true;
                }
            });

        var pipeline = builder.Build(services);
        await pipeline.Run(cts.Token);

        // Assert
        Assert.True(taskWasCancelled);
    }

    #endregion

    #region Inspector
    private class TestInspector : IPipelineInspector
    {
        public List<string> StepOrder { get; } = new();
        public List<Exception> Exceptions { get; } = new();

        public Task BeforeStepAsync(PipelineStepContext context)
        {
            StepOrder.Add($"Before {context.StepId}");
            return Task.CompletedTask;
        }

        public Task AfterStepAsync(PipelineStepContext context)
        {
            StepOrder.Add($"After {context.StepId}");
            return Task.CompletedTask;
        }

        public Task OnExceptionAsync(PipelineStepContext context)
        {
            if (context.Exception != null)
            {
                Exceptions.Add(context.Exception);
            }
            return Task.CompletedTask;
        }
    }

    private class TestInspectorEvents : IPipelineInspector
    {
        public event Action<PipelineStepContext>? BeforeStep;
        public event Action<PipelineStepContext>? AfterStep;
        public event Action<PipelineStepContext>? OnError;

        public Task BeforeStepAsync(PipelineStepContext context)
        {
            BeforeStep?.Invoke(context);
            return Task.CompletedTask;
        }

        public Task AfterStepAsync(PipelineStepContext context)
        {
            AfterStep?.Invoke(context);
            return Task.CompletedTask;
        }

        public Task OnExceptionAsync(PipelineStepContext context)
        {
            OnError?.Invoke(context);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task InspectorTracksExecution()
    {
        // Arrange
        var inspector = new TestInspector();
        var services = new ServiceCollection()
            .BuildServiceProvider();

        // Act
        var builder = new PipelineBuilder()
            .AddInspector(inspector)
            .Use(next => async ct => await next(ct), "Step1")
            .Use(next => async ct => await next(ct), "Step2");

        var pipeline = builder.Build(services);
        await pipeline.Run(default);

        // Assert
        Assert.Equal(new[]
        {
        "Before Step1",  // Step1 starts
        "Before Step2",  // Step1 calls next, Step2 starts
        "After Step2",   // Step2 completes
        "After Step1"    // Step1 completes
        }, inspector.StepOrder);
    }

    [Fact]
    public async Task InspectorTracksMiddlewareActionsCorrectly()
    {
        // Arrange
        var allEvents = new List<string>();
        var services = new ServiceCollection()
            .BuildServiceProvider();

        var inspector = new TestInspectorEvents();
        inspector.BeforeStep += (ctx) => allEvents.Add($"Inspector Before {ctx.StepId}");
        inspector.AfterStep += (ctx) => allEvents.Add($"Inspector After {ctx.StepId}");

        // Act
        var builder = new PipelineBuilder()
            .AddInspector(inspector)
            .Use(next => async ct =>
            {
                allEvents.Add("Middleware1 Start");
                await next(ct);
                allEvents.Add("Middleware1 End");
            }, "Step1")
            .Use(next => async ct =>
            {
                allEvents.Add("Middleware2 Start");
                await next(ct);
                allEvents.Add("Middleware2 End");
            }, "Step2");

        var pipeline = builder.Build(services);
        await pipeline.Run(default);

        // Assert
        var expected = new[]
        {
        "Inspector Before Step1",
        "Middleware1 Start",
        "Inspector Before Step2",
        "Middleware2 Start",
        "Middleware2 End",
        "Inspector After Step2",
        "Middleware1 End",
        "Inspector After Step1"
    };

        Assert.Equal(expected, allEvents);
    }

    [Fact]
    public async Task InspectorCatchesExceptions()
    {
        // Arrange
        var inspector = new TestInspector();
        var services = new ServiceCollection()
            .BuildServiceProvider();

        var expectedException = new Exception("Test Exception");

        // Act
        var builder = new PipelineBuilder()
            .AddInspector(inspector)
            .Use(next => ct => throw expectedException, "FailingStep");

        var pipeline = builder.Build(services);
        var exception = await Assert.ThrowsAsync<Exception>(() => pipeline.Run(default));

        // Assert
        Assert.Single(inspector.Exceptions);
        Assert.Equal(expectedException, inspector.Exceptions[0]);
    }

    [Fact]
    public async Task InspectorTracksBranchSteps()
    {
        // Arrange
        var allEvents = new List<string>();
        var services = new ServiceCollection()
            .BuildServiceProvider();

        var inspector = new TestInspectorEvents();
        inspector.BeforeStep += (ctx) => allEvents.Add($"Inspector Before {ctx.StepId}");
        inspector.AfterStep += (ctx) => allEvents.Add($"Inspector After {ctx.StepId}");

        // Act
        var builder = new PipelineBuilder()
            .AddInspector(inspector)
            .Use(next => async ct =>
            {
                allEvents.Add("Main Pipeline Start");
                await next(ct);
                allEvents.Add("Main Pipeline End");
            }, "MainStep")
            .UseBranch(
                ctx => Task.FromResult(true), // Always execute branch
                branch => branch
                    .Use(next => async ct =>
                    {
                        allEvents.Add("Branch Step1");
                        await next(ct);
                    }, "BranchStep1")
                    .Use(next => async ct =>
                    {
                        allEvents.Add("Branch Step2");
                        await next(ct);
                    }, "BranchStep2"),
                "BranchOperation"
            );

        var pipeline = builder.Build(services);
        await pipeline.Run(default);

        // Assert
        var expected = new[]
        {
        "Inspector Before MainStep",
        "Main Pipeline Start",
        "Inspector Before BranchOperation",
        "Inspector Before BranchStep1",
        "Branch Step1",
        "Inspector Before BranchStep2",
        "Branch Step2",
        "Inspector After BranchStep2",
        "Inspector After BranchStep1",
        "Inspector After BranchOperation",
        "Main Pipeline End",
        "Inspector After MainStep"
    };

        Assert.Equal(expected, allEvents);
    }

    #endregion

    #region When

    [Fact]
    public async Task When_ContinuesPipelineWhenConditionNotMet()
    {
        // Arrange
        var executionOrder = new List<string>();
        var services = new ServiceCollection()
            .BuildServiceProvider();

        // Act
        var builder = new PipelineBuilder()
            .Use(next => async context =>
            {
                executionOrder.Add("Before When");
                await next(context);
                executionOrder.Add("After When");
            })
            .When(
                context => false,
                context =>
                {
                    executionOrder.Add("When Action"); // Should not execute;
                    return Task.CompletedTask;
                })
            .Use(next => async context =>
            {
                executionOrder.Add("After When Middleware");
                await next(context);
            });

        var pipeline = builder.Build(services);
        await pipeline.Run(default);

        // Assert
        Assert.Equal(new[]
        {
        "Before When",
        "After When Middleware",
        "After When"
    }, executionOrder);
    }

    [Fact]
    public async Task When_ExecutesActionAndContinuesPipelineWhenConditionMet()
    {
        // Arrange
        var executionOrder = new List<string>();
        var services = new ServiceCollection()
            .BuildServiceProvider();

        // Act
        var builder = new PipelineBuilder()
            .Use(next => async context =>
            {
                executionOrder.Add("Before When");
                await next(context);
                executionOrder.Add("After When");
            })
            .When(
                context => true,
                context => { executionOrder.Add("When Action"); return Task.CompletedTask; }
            )
            .Use(next => async context =>
            {
                executionOrder.Add("After When Middleware");
                await next(context);
            });

        var pipeline = builder.Build(services);
        await pipeline.Run(default);

        // Assert
        Assert.Equal(new[]
        {
        "Before When",
        "When Action",
        "After When Middleware",
        "After When"
    }, executionOrder);
    }

    [Fact]
    public async Task When_SupportsNestedPipelines()
    {
        // Arrange
        var executionOrder = new List<string>();
        var services = new ServiceCollection()
            .BuildServiceProvider();

        // Act
        var builder = new PipelineBuilder()
            .Use(next => async context =>
            {
                executionOrder.Add("Main Pipeline Start");
                await next(context);
                executionOrder.Add("Main Pipeline End");
            })
            .When(
                context => true,
                async context =>
                {
                    var nestedPipeline = new PipelineBuilder()
                        .Use(next => async ctx =>
                        {
                            executionOrder.Add("Nested Pipeline Start");
                            await next(ctx);
                            executionOrder.Add("Nested Pipeline End");
                        })
                        .Build(context.ServiceProvider);

                    await nestedPipeline.BranchFrom(context);
                }
            )
            .Use(next => async context =>
            {
                executionOrder.Add("After Nested Pipeline");
                await next(context);
            });

        var pipeline = builder.Build(services);
        await pipeline.Run(default);

        // Assert
        Assert.Equal(new[]
        {
        "Main Pipeline Start",
        "Nested Pipeline Start",
        "Nested Pipeline End",
        "After Nested Pipeline",
        "Main Pipeline End"
    }, executionOrder);
    }

    [Fact]
    public async Task When_SupportsChainingMultipleConditions()
    {
        // Arrange
        var executionOrder = new List<string>();
        var services = new ServiceCollection()
            .BuildServiceProvider();

        // Act
        var builder = new PipelineBuilder()
            .When(
                context => Task.FromResult(true),
                context => { executionOrder.Add("First When"); return Task.CompletedTask; }
            )
            .When(
                context => Task.FromResult(false),
                context => { executionOrder.Add("Second When - Should Skip"); return Task.CompletedTask; }
            )
            .When(
                context => Task.FromResult(true),
                context => { executionOrder.Add("Third When"); return Task.CompletedTask; }
            );

        var pipeline = builder.Build(services);
        await pipeline.Run(default);

        // Assert
        Assert.Equal(new[]
        {
        "First When",
        "Third When"
    }, executionOrder);
    }

    [Fact]
    public async Task When_PreservesContextAcrossPipelines()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ITestService>(new TestService());
        var sp = services.BuildServiceProvider();
        var serviceAccessCount = 0;

        // Act
        var builder = new PipelineBuilder()
            .When(
                context =>
                {
                    var service = context.ServiceProvider.GetRequiredService<ITestService>();
                    serviceAccessCount++;
                    return Task.FromResult(true);
                },
                async context =>
                {
                    var nestedPipeline = new PipelineBuilder()
                        .Use(next => async ctx =>
                        {
                            var service = ctx.ServiceProvider.GetRequiredService<ITestService>();
                            serviceAccessCount++;
                            await next(ctx);
                        })
                        .Build(context.ServiceProvider);

                    await nestedPipeline.BranchFrom(context);
                }
            );

        var pipeline = builder.Build(sp);
        await pipeline.Run(default);

        // Assert
        Assert.Equal(2, serviceAccessCount);
    }

    #endregion

    #region Branch
    [Fact]
    public async Task Branch_ExecutesWhenConditionMet()
    {
        // Arrange
        var branchExecuted = false;
        var services = new ServiceCollection()
            .BuildServiceProvider();

        // Act
        var builder = new PipelineBuilder()
            .UseBranch(
                (context) => Task.FromResult(true),
                branch => branch.Use(next => async ct =>
                {
                    branchExecuted = true;
                    await next(ct);
                })
            );

        var pipeline = builder.Build(services);
        await pipeline.Run(default);

        // Assert
        Assert.True(branchExecuted);
    }

    [Fact]
    public async Task Branch_SkipsWhenConditionNotMet()
    {
        // Arrange
        var branchExecuted = false;
        var services = new ServiceCollection()
            .BuildServiceProvider();

        // Act
        var builder = new PipelineBuilder()
            .UseBranch(
                (ctx) => Task.FromResult(false),
                branch => branch.Use(next => async ctx =>
                {
                    branchExecuted = true;
                    await next(ctx);
                })
            );

        var pipeline = builder.Build(services);
        await pipeline.Run(default);

        // Assert
        Assert.False(branchExecuted);
    }

    [Fact]
    public async Task Branch_ContinuesPipelineWhenConditionNotMet()
    {
        // Arrange
        var executionOrder = new List<string>();
        var services = new ServiceCollection()
            .BuildServiceProvider();

        // Act
        var builder = new PipelineBuilder()
            .Use(next => async context =>
            {
                executionOrder.Add("Before Branch");
                await next(context);
                executionOrder.Add("After Branch");
            })
            .UseBranch(
                context => Task.FromResult(false),
                branch => branch.Use(next => async context =>
                {
                    executionOrder.Add("Branch Executed"); // Should not execute
                    await next(context);
                })
            )
            .Use(next => async context =>
            {
                executionOrder.Add("After Branch Middleware");
                await next(context);
            });

        var pipeline = builder.Build(services);
        await pipeline.Run(default);

        // Assert
        Assert.Equal(new[]
        {
        "Before Branch",
        "After Branch Middleware",
        "After Branch"
        }, executionOrder);
    }

    [Fact]
    public async Task ParallelBranchesExecuteAllBranches()
    {
        // Arrange
        var executedBranches = new HashSet<string>();
        var services = new ServiceCollection()
            .BuildServiceProvider();

        var items = new[] { "Item1", "Item2", "Item3" };
        var results = Task.FromResult<IEnumerable<string>>(items);
        // Act
        var builder = new PipelineBuilder()
            .UseParallelBranches(
                async (ctx) => await results,
                (branch, item) => branch.Use(next => async ctx =>
                {
                    lock (executedBranches)
                    {
                        executedBranches.Add(item);
                    }
                    await next(ctx);
                })
            );

        var pipeline = builder.Build(services);
        await pipeline.Run(default);

        // Assert
        Assert.Equal(items.Length, executedBranches.Count);
        Assert.All(items, item => Assert.Contains(item, executedBranches));
    }

    [Fact]
    public async Task ServiceProviderIsAccessibleInBranchCondition()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ITestService>(new TestService());
        var sp = services.BuildServiceProvider();

        var serviceWasAccessed = false;

        // Act
        var builder = new PipelineBuilder()
            .UseBranch(
                (ctx) =>
                {
                    var service = ctx.ServiceProvider.GetRequiredService<ITestService>();
                    serviceWasAccessed = true;
                    return Task.FromResult(true);
                },
                branch => branch.Run(() => { })
            );

        var pipeline = builder.Build(sp);
        await pipeline.Run(default);

        // Assert
        Assert.True(serviceWasAccessed);
    }

    #endregion

    #region Run
    [Fact]
    public async Task Run_ExecutesAction()
    {
        // Arrange
        var executed = false;
        var services = new ServiceCollection().BuildServiceProvider();

        // Act
        var builder = new PipelineBuilder()
            .Run(() => executed = true);

        var pipeline = builder.Build(services);
        await pipeline.Run(default);

        // Assert
        Assert.True(executed);
    }

    [Fact]
    public async Task RunAsync_ExecutesAsyncAction()
    {
        // Arrange
        var executed = false;
        var services = new ServiceCollection().BuildServiceProvider();

        // Act
        var builder = new PipelineBuilder()
            .RunAsync(async ctx =>
            {
                await Task.Delay(1, ctx.CancellationToken);
                executed = true;
            });

        var pipeline = builder.Build(services);
        await pipeline.Run(default);

        // Assert
        Assert.True(executed);
    }

    [Fact]
    public async Task TryRun_HandlesException()
    {
        // Arrange
        var exceptionHandled = false;
        var services = new ServiceCollection().BuildServiceProvider();

        // Act
        var builder = new PipelineBuilder()
            .TryRun(
                () => throw new Exception("Test exception"),
                ex => exceptionHandled = true
            );

        var pipeline = builder.Build(services);
        await pipeline.Run(default);

        // Assert
        Assert.True(exceptionHandled);
    }

    [Fact]
    public async Task TryRunAsync_HandlesException_AndContinuesPipeline()
    {
        // Arrange
        var exceptionHandled = false;
        var pipelineContinued = false;
        var services = new ServiceCollection().BuildServiceProvider();

        // Act
        var builder = new PipelineBuilder()
            .TryRunAsync(
                async ctx =>
                {
                    await Task.Delay(1, ctx.CancellationToken);
                    throw new Exception("Test exception");
                },
                ex => exceptionHandled = true
            )
            .Run(() => pipelineContinued = true);

        var pipeline = builder.Build(services);
        await pipeline.Run(default);

        // Assert
        Assert.True(exceptionHandled);
        Assert.True(pipelineContinued);
    }

    #endregion

    #region Builder

    [Fact]
    public async Task StepIndicesAreAssignedInOrderOfAddition()
    {
        // Arrange
        var stepIndices = new List<int>();
        var inspector = new TestCallbackInspector(context =>
        {
            stepIndices.Add(context.PipelineContext.CurrentStepIndex);
        });

        var builder = new PipelineBuilder()
         .AddInspector(inspector)
         .Use(next => async ctx => await next(ctx), "Step1")
         .Use(next => async ctx => await next(ctx), "Step2");

        // Act
        var pipeline = builder.Build(new ServiceCollection().BuildServiceProvider());
        await pipeline.Run();

        // Assert
        Assert.Equal(new[] { 0, 1 }, stepIndices);
    }

    [Fact]
    public async Task StepIndexStaysConstantWhenWrapped()
    {
        // Arrange
        var stepIndices = new Dictionary<string, int>();
        var inspector = new TestCallbackInspector(context =>
        {
            stepIndices[context.StepId] = context.PipelineContext.CurrentStepIndex;
        });

        var builder = new PipelineBuilder()
            .AddInspector(inspector)
            .Use(next => async ctx => await next(ctx), "Step1");

        // Wrap the step multiple times
        builder.WrapLastComponent(component => (sp, next) => async context =>
        {
            await component(sp, next)(context);
        });

        builder.WrapLastComponent(component => (sp, next) => async context =>
        {
            await component(sp, next)(context);
        });

        builder.Use(next => async ctx => await next(ctx), "Step2");

        // Act
        var pipeline = builder.Build(new ServiceCollection().BuildServiceProvider());
        await pipeline.Run();

        // Assert
        Assert.Equal(0, stepIndices["Step1"]); // First step should always be index 0
        Assert.Equal(1, stepIndices["Step2"]); // Second step should be index 1
    }

    #endregion


    // Support types for tests
    private interface IScopedService { }
    private class ScopedService : IScopedService { }
    private interface ITestService { }
    private class TestService : ITestService { }

}
