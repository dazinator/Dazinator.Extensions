# Pipeline Builder for .NET

[![Licence: AGPL-3.0 with Commercial Option](https://img.shields.io/badge/License-AGPL%20with%20Commercial%20Option-blue.svg)](LICENCE)

Build composable, inspectable execution pipelines with dependency injection support.

## Table of Contents
- [Why Use This?](#why-use-this)
- [Quick Start](#quick-start)
- [How Does This Compare To Other Solutions?](#how-does-this-compare-to-other-solutions)
  - [Key Differentiators](#key-differentiators)
  - [Perfect For](#perfect-for)
  - [Not Designed For](#not-designed-for)
- [Core Concepts](#core-concepts)
  - [Pipeline Context](#pipeline-context)
  - [Dependency Injection Scopes](#dependency-injection-scopes)
  - [Middleware vs Actions](#middleware-vs-actions)
  - [Filters](#filters)   
- [API Reference](#api-reference)
  - [Middleware](#middleware)
  - [Actions (Run/RunAsync)](#actions)
  - [Conditional Execution (When/TryWhen)](#conditional-execution)
  - [Branching (UseBranch/TryBranch)](#branching)
  - [Parallel Execution](#parallel-execution)
  - [Pipeline Inspection](#pipeline-inspection)
- [Best Practices](#best-practices)
- [Contributing](#contributing)

## Why Use This?
✨ Perfect for when you need to:
- Execute startup tasks in order/parallel with proper dependency injection
- Build multi-tenant initialization workflows
- Create database migration orchestration
- Set up conditional feature toggles
- Build diagnostic/telemetry pipelines
- Orchestrate complex business processes, or processing flows, including batch processing flows

## Quick Start
Think of this as building a middleware pipeline like in ASP.NET Core, but here we are building a general purpose processing pipeline not coupled to web requests.
There is added support for things such as branching and parallel execution, and more features discussed below.

```csharp
// Service configuration
services.AddPipelines(pipelines => 
{
    pipelines.Add("order-processing", builder =>
    {
        builder
            .UseFilters() // this call may not be necessary in future versions.
           
            // "Use" = "Middleware" (runs before and after next steps)
            .Use(next => async ctx => {
                Console.WriteLine("Starting order processing...");              
                await next(ctx);              
                Console.WriteLine("Order processing complete.");
            })

            // "Run" == "Action" - the business end of some logic you want to execute as a step.
            .RunAsync(async ctx => {
                await Task.Delay(100); // Simulate work
                Console.WriteLine("Processing order...");
            })         
                // "With" = "Filter" - this one is an out of the box filter. Runs before and after the step above and can optionally skip it (see below).
               .WithSkipCondition(() => false)
               .WithSkipConditionAsync(() => Task.FromResult(false))
               .WithSkipConditionAsync((ctx) =>CheckIsFeatureDisabledAsync()) //If any skip conditions return true, step will be skipped.

               .WithIdempotency(opt => { // Another out of the box filter. Skips the step if something causes the step to re-execute and CheckCompleted returns true. Useful if add a retry middleware or Filter that causes re-execution.
                   opt.Key = "order-123";
                   opt.CheckCompleted = async ctx => {
                       await Task.Delay(10); // Simulate check
                       return false; // Not yet processed
                   };
               })
            // Conditional branch for a more complex payment processing flow fetaure.
            .UseBranch((branch) =>
            {
                // We are building a sub-pipeline here.
                branch.RunAsync(async ctx => {
                        await Task.Delay(50);
                        Console.WriteLine("Processing payment...");
                       })
                          .WithSkipCondition(() => false);                      

            })
                // We can still use Filters on the branch step itself.
               .WithSkipConditionAsync(async ctx => {
                   Console.WriteLine("Checking if payment processing is disabled...");
                   return IsPaymentProcessingDisabled(ctx);                  
               }) 
           
            // Parallel branches - to spawn off multiple parallel processing branches.
            .UseParallelBranches(
                new[] { "Customer", "Warehouse", "Shipping" },
                (branch, recipient) => branch // We are building a sub-pipeline here.. per branch.
                    .Run(() => Console.WriteLine($"Sending notification to {recipient}"))
            )
               .WithSkipConditionAsync(IsNotificationsFeatureDisabled);
    });
});

// Usage
var registry = serviceProvider.GetRequiredService();
var pipeline = registry.GetPipeline("order-processing");
await pipeline.Run(); // pass optional cancellation token.

// Output:
// Starting order processing...
// Processing order...
// Checking if payment processing is disabled...
// Processing payment...
// Sending notification to Customer
// Sending notification to Warehouse
// Sending notification to Shipping
// Order processing complete.
```

Once you have built your pipeline, you can execute it as many times as you like.
The pipeline is immutable and can be reused across multiple executions.
Pipelines can be joined together, branched, and run in parallel.

Key goals of this library are to:

1. Allow you write a processing pipeline where its easier to visualise the steps, like a workflow.  
1. Allow you to capture cross cutting concerns as `Inspector`'s (see below), `Middleware` and `Filter`'s that can be added on the fly, or written as re-usable classes if they are more broadly useful.
1. Provide a set of broadly useful `Inspector` `Middleware` and `Filter`'s that can be used out of the box.` to add standard and tested behviours for things like:-
    - Timeouts
    - Idempotency
    - Retries (using Polly Policies)
    - Logging
    - Parallel Processing - via Branching etc.
    - DI Scope management.

## How Does This Compare To Other Solutions?

This library fills a specific gap between simple pipeline patterns and complex workflow engines:

| Solution | Focus | Strengths | Limitations | When To Use |
|----------|-------|-----------|-------------|-------------|
| **Pipeline Builder (this)** | General purpose processing pipelines | • Rich branching & parallel execution<br>• Explicit scope control<br>• Step-level filters<br>• DI friendly<br>• Simple but powerful API<br>• Built-in inspection | • Single machine execution<br>• In-memory only | • Application startup orchestration<br>• Multi-tenant operations<br>• Complex initialization flows<br>• Batch processing with branches |
| MediatR Pipeline | CQRS and commands | • Simple to use<br>• Well established<br>• Good for CQRS | • No branching<br>• No parallel execution<br>• Limited scope control | • Request/response pipelines<br>• Command validation<br>• Simple cross cutting concerns |
| ASP.NET Middleware | Web request pipeline | • HTTP specific features<br>• Well documented<br>• Standard approach | • Fixed scope per request<br>• No branching<br>• Web focused | • HTTP request handling<br>• Web specific middleware |
| TPL Dataflow | Data processing pipelines | • High performance<br>• Good for data flows<br>• Mature | • Complex setup<br>• Less DI friendly<br>• Steeper learning curve | • Data processing<br>• Producer/consumer scenarios<br>• Parallel data processing |

### Key Differentiators

- **Explicit Scope Control**: Unlike ASP.NET Core middleware, you control exactly when and where new DI scopes are created, making it perfect for complex initialization flows and multi-tenant scenarios.

- **Rich Branching**: Both conditional and parallel execution paths with proper scope isolation - something not commonly found in other pipeline implementations.

- **Error Handling Models**: Provides both strict (`Use`, `When`, `Branch`) and tolerant (`TryRun`, `TryWhen`, `TryBranch`) error handling approaches, letting you control exactly how failures affect the pipeline.

- **Inspection System**: Built-in support for monitoring execution, timing, and errors across all pipeline operations including branches.

### Perfect For

✅ Application startup orchestration that requires careful ordering and conditional execution  
✅ Multi-tenant scenarios where operations need to run in parallel with proper isolation  
✅ Complex initialization flows with branching logic and dependency injection  
✅ Batch processing where some steps can run in parallel and others must be sequential  

### Not Designed For

❌ Distributed workflow processing across multiple machines  
❌ Long-running persistent workflows  
❌ Event-driven architectures (though could be used within event handlers)  
❌ Web request processing (use ASP.NET middleware instead)

## Core Concepts

### Pipeline Context
The pipeline context flows through your entire pipeline, providing access to services and cancellation:

```csharp
public class PipelineContext
{
   public IServiceProvider ServiceProvider { get; }  // Current service scope
   public CancellationToken CancellationToken { get; }
}
```

### Dependency Injection Scopes
Unlike ASP.NET Core middleware:
- No automatic scope creation
- Services are resolved from the current scope at execution time
- Explicit scope control using `UseNewScope()`

```csharp
builder
   .UseNewScope() // Creates new scope
   .UseMiddleware<DbMiddleware>() // Uses that scope
   .UseParallelBranches(tenants, (branch, tenant) =>
       branch
           .UseNewScope() // Each branch now has its own scope
           .UseMiddleware<TenantMiddleware>()
   );
```

### Try Semantics
The pipeline provides both strict and error-tolerant versions of many operations. Methods prefixed with "Try" will:
- Catch any exceptions that occur
- Continue pipeline execution even if an exception has occurred.
- Optionally allow you to intercept the exception via an optional callback. Howver more general error handling can be done using Interceptors.

```csharp
// These will stop the pipeline if they throw
builder
    .Run(() => throw new Exception("Stops here"))
    .Run(() => Console.WriteLine("Never runs"));

// These will continue despite errors
builder
    .TryRun(
        () => throw new Exception("Handled gracefully"),
        ex => Console.WriteLine("Optional error handler")
    )
    .Run(() => Console.WriteLine("Still runs"));

// Try semantics are available on many operations:
TryRun() / TryRunAsync()  // For simple actions
TryWhen()                 // For conditional logic
TryBranch()              // For entire sub-pipelines
```

### Middleware vs Actions
Choose the right tool for your needs:

#### Middleware
- Complex operations
- Dependency injection
- Reusable components
- Before/after execution control

#### Actions
- Simple operations
- Inline code
- One-off tasks
- When DI isn't needed

#### Step IDs
Every step in your pipeline can have an optional ID for tracking:
```csharp
builder
   .UseMiddleware("Step 1")         // Class middleware
   .Use(next => next(context), "Step 2")         // Delegate middleware
   .Run(() => DoSomething(), "Step 3")           // Run action
   .RunAsync(async ctx => await Task.Delay(1), "Step 4")  // RunAsync action
   .UseBranch(
       ctx => Task.FromResult(true),
       branch => branch.Use(next => next(ctx)),
       "Conditional Branch"  // Branch ID
   );
```

These IDs appear in your inspector contexts (see section on Inspectors below), making it easy to:
- Track execution flow
- Measure performance
- Debug issues
- Create audit trails

### Filters
Filters provide step-level behaviors that can be configured for specific pipeline steps. Unlike middleware which affects the entire pipeline, filters are scoped to individual steps:

```csharp
// Basic logging filter
public class LoggingFilter : IStepFilter
{
    private readonly string _category;
    
    public LoggingFilter(string category)
    {
        _category = category;
    }

    public Task BeforeStepAsync(PipelineStepContext context)
    {
        Console.WriteLine($"[{_category}] Starting step: {context.StepId}");
        return Task.CompletedTask;
    }

    public Task AfterStepAsync(PipelineStepContext context)
    {
        Console.WriteLine($"[{_category}] Completed step: {context.StepId}");
        return Task.CompletedTask;
    }
}

// Filter that can conditionally skip step execution
public class ConditionalFilter : IStepFilter
{
    private readonly Func<PipelineContext, Task<bool>> _shouldProcess;

    public ConditionalFilter(Func<PipelineContext, Task<bool>> shouldProcess)
    {
        _shouldProcess = shouldProcess;
    }

    public async Task BeforeStepAsync(PipelineStepContext context)
    {
        if (!await _shouldProcess(context.PipelineContext))
        {
            // Setting ShouldSkip prevents the step from executing
            context.ShouldSkip = true;
        }
    }

    public Task AfterStepAsync(PipelineStepContext context) => Task.CompletedTask;
}

// Filters instances can be added directly
builder
    .UseFilters()
    .Run(async ctx => await ProcessOrder())
    .AddFilters(registry =>
    {
        // Creates new instance for this step
        registry.AddFilter(sp => new LoggingFilter("Orders"));
        // Skip step if condition not met
        registry.AddFilter(sp => new ConditionalFilter(
            async ctx => await ShouldProcessOrder(ctx)));
    });

// Or resolved from DI
public class TransactionFilter : IStepFilter
{
    private readonly ITransactionService _transactionService;

    public TransactionFilter(ITransactionService transactionService) 
    {
        _transactionService = transactionService;
    }

    public async Task BeforeStepAsync(PipelineStepContext context)
    {
        await _transactionService.BeginAsync();
    }

    public async Task AfterStepAsync(PipelineStepContext context)
    {
        await _transactionService.CommitAsync();
    }
}

// Register with appropriate lifecycle
services.AddScoped<TransactionFilter>();

// Use from DI
builder
    .UseFilters()
    .Run(async ctx => await ProcessOrder())
    .AddFilters(registry =>
    {
        // Will be resolved from current execution scope
        registry.AddFilterFromServices<TransactionFilter>();
    });
```

Filter Lifetime Considerations:
- Filters resolved from DI follow standard DI lifecycle rules
- Singleton filters will share state across all pipeline executions
- Scoped filters get new instances per DI scope.
  - The service provider used to resolve filters is the current execution scope.
  - When using `UseNewScope()`, filters resolved from DI will get new instances per that scope

Filter Execution Order:
- BeforeStepAsync executes in registration order (1, 2, 3)
- The step executes (if not skipped)
- AfterStepAsync executes in reverse order (3, 2, 1)
- Any filter can prevent step execution by setting `context.ShouldSkip = true`
- If any filter sets `ShouldSkip` in it's `BeforeStepAsync`, then the step and remaining filters' BeforeStepAsync and AfterStepAsync are skipped

## API Reference

### Middleware
The core building block of your pipeline, supporting dependency injection and complex logic.

```csharp
// 1. Using Delegates (Simple Cases)
builder.Use(next => async context => {
   Console.WriteLine("Before next middleware");
   await next(context);
   Console.WriteLine("After next middleware");
});

// 2. Using Classes (Recommended for DI)
public class LoggingMiddleware : IPipelineMiddleware
{
   private readonly ILogger _logger;

   public LoggingMiddleware(ILogger logger) // Automatically injected
   {
       _logger = logger;
   }

   public async Task ExecuteAsync(PipelineStep next, PipelineContext context)
   {
       _logger.LogInformation("Before next middleware");
       await next(context);
       _logger.LogInformation("After next middleware");
   }
}

// Usage:
builder.UseMiddleware<LoggingMiddleware>("Optional Step ID");
```

### Actions
Simpler alternatives to middleware for basic operations.

```csharp
// Basic synchronous action
builder.Run(() => Console.WriteLine("Simple action"));

// Async action with context access
builder.RunAsync(async ctx => 
{
   var service = ctx.ServiceProvider.GetRequiredService();
   await service.DoSomethingAsync(ctx.CancellationToken);
});

// Error-tolerant versions
builder.TryRun(
   () => throw new Exception("Oops"),
   ex => Console.WriteLine($"Caught: {ex.Message}")  // Optional handler
);

builder.TryRunAsync(
   async ctx => 
   {
       await Task.Delay(100, ctx.CancellationToken);
       throw new Exception("Async Oops");
   },
   ex => Console.WriteLine($"Caught async: {ex.Message}")  // Optional handler
);

// Example showing pipeline continuation
builder
   .Run(() => Console.WriteLine("First action"))
   .TryRun(
       () => throw new Exception("This error is handled"),
       ex => Console.WriteLine("Logged the error")
   )
   .Run(() => Console.WriteLine("Still runs"));
```

Key differences:
- `Run`/`RunAsync`: Exceptions stop the pipeline
- `TryRun`/`TryRunAsync`: Pipeline continues despite exceptions

### Conditional Execution
Two approaches to conditional logic in your pipeline:

```csharp
// 1. When - Stops pipeline if condition check or action throws
builder
   .When(
       ctx => ctx.ServiceProvider.GetService().IsEnabled("MyFeature"),
       async ctx => await InitializeFeature(ctx)
   )
   .Run(() => Console.WriteLine("Only runs if condition succeeds"));

// 2. TryWhen - Continues pipeline even if condition or action throws
builder
   .TryWhen(
       ctx => CheckFeature(ctx),  // Exception here won't stop pipeline
       async ctx => await InitializeFeature(ctx),  // Exception here won't stop pipeline
       ex => Console.WriteLine($"Feature init failed: {ex.Message}")  // Optional handler
   )
   .Run(() => Console.WriteLine("Always runs"));
```

### Branching

#### Single Branch
Create sub-pipelines for complex workflows:

```csharp
// Standard branch - stops pipeline on error
builder
   .UseBranch(
       async ctx => await ShouldRunMigration(ctx),
       branch => branch
           .UseNewScope()
           .Run(() => Console.WriteLine("Running migration"))
           .RunAsync(async ctx => await PerformMigration(ctx))
   )
   .Run(() => Console.WriteLine("Only runs if branch succeeds"));

// Error-tolerant branch - continues pipeline on error
builder
   .TryBranch(
       async ctx => await ShouldRunOptionalTask(ctx),
       branch => branch
           .UseNewScope()
           .RunAsync(async ctx => await PerformOptionalTask(ctx)),
       ex => Console.WriteLine($"Optional task failed: {ex.Message}")  // Optional handler
   )
   .Run(() => Console.WriteLine("Always runs"));

// Nested branches
builder
   .UseBranch(
       async ctx => await IsMultiTenant(ctx),
       branch => branch
           .UseNewScope()
           .UseBranch(
               async ctx => await NeedsMigration(ctx),
               migrationBranch => migrationBranch
                   .UseMiddleware()
           )
   );
```

#### Multiple Branches

You can spawn multiple branches, with concurrency controls.

Branch per item:

```csharp
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
```

Branch per chunk of items:

```csharp
 // Arrange
 var processedChunks = new ConcurrentBag<string[]>();

 // Act
 var builder = CreatePipelineBuilder()
     .UseBranchPerInputs<string>(branch =>
     {
         branch.Run(() => processedChunks.Add(branch.Input.ToArray())); // branch.Input here is the "chunk" of items assigned to this branch.
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

```

Controlling concurrency:

```csharp
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

```

Note: all api's that can spawn multiple branches (not just WithChunks), allow you to control the degree of parallelism in the same way. 
The default is 1, so no concurrency unless exlicitly setting the options in this way.


Exmple of using a skip condition on a parallel branch and within a parallel branch 

```

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

```

### Pipeline Inspection
Inspectors are notified before and after every step in the pipeline as well as if there is an exception.
They can be used to do things like monitor execution, track timing, handle errors - or other cross cutting concerns.

#### Basic Inspector Interface
```csharp
public interface IPipelineInspector
{
   Task BeforeStepAsync(PipelineStepContext context);
   Task AfterStepAsync(PipelineStepContext context);
   Task OnExceptionAsync(PipelineStepContext context);
}

public class PipelineStepContext
{
   public string StepId { get; }      // Identifier for the step
   public string StepType { get; }    // Type of step (e.g., "Middleware", "Action")
   public TimeSpan Duration { get; }   // How long the step took
   public Exception? Exception { get; } // If step threw an exception
   public PipelineContext PipelineContext { get; } // Access to the pipeline context
}
```

#### Built-in Inspectors
Pipeline Builder comes with several built-in inspectors that provide immediate visibility into your pipeline's execution.

##### LoggingPipelineInspector
Provides a log before and after each step as well as on exceptions, using `Microsoft.Extensions.Logging`.

```csharp
// Add to your pipeline
builder.AddInspector<LoggingPipelineInspector>();

// Or with custom logger
var logger = serviceProvider.GetRequiredService<ILogger<LoggingPipelineInspector>>();
builder.AddInspector(new LoggingPipelineInspector(logger));
```

Output example:
```
info: Starting pipeline step "ProcessOrder" of type Run
info: Completed pipeline step "ProcessOrder" after 123.45ms
error: Pipeline step "ProcessOrder" failed: Operation timed out
```

Perfect for:
- Development debugging
- Production monitoring
- Understanding pipeline flow
- Tracking execution times

##### ConcurrencyMonitorInspector
Monitors and analyzes concurrent execution patterns in your pipeline, especially useful for parallel processing scenarios.

```csharp
// Create the inspector
var concurrencyMonitor = new ConcurrencyMonitorInspector(logger); // Logger is optional

// Add to your pipeline
builder.AddInspector(concurrencyMonitor);

// After pipeline execution, get the report
var report = concurrencyMonitor.GenerateReport();
Console.WriteLine(report.ToString());
```

The report provides:
- Maximum concurrent executions per step
- Current active executions
- Detailed execution timeline
- Thread usage patterns

Example output:
```
    Concurrency Analysis Report
    ==========================
    
    Step: A
    Max Concurrent Executions: 1
    Current Active Executions: 0
    Total Executions: 1
    
    Execution Timeline:
      Thread 9: Start=23:24:52.497 End=PENDING Concurrent=(Start: 1, End: 0)
    
    Step: B
    Max Concurrent Executions: 2
    Current Active Executions: 0
    Total Executions: 2
    
    Execution Timeline:
      Thread 15: Start=23:24:52.506 End=23:24:52.506 Concurrent=(Start: 2, End: 0)
      Thread 12: Start=23:24:52.506 End=23:24:52.506 Concurrent=(Start: 1, End: 0)
```

The above report shows a pipeline with a single step `ProcessItems` it reached a max concurrency of `2`, and it ran 5 times in total. The timeline shows each execution a problem because, 

Perfect for:
- Debugging concurrency issues
- Verifying parallel execution behavior
- Performance optimization
- Understanding thread utilization

#### Example: Writing Custom Inspectors

##### Performance Tracking Inspector
```csharp
public class PerformanceInspector : IPipelineInspector
{
   private readonly List<(string StepId, TimeSpan Duration)> _timings = new();

   public Task BeforeStepAsync(PipelineStepContext context) => Task.CompletedTask;

   public Task AfterStepAsync(PipelineStepContext context)
   {
       _timings.Add((context.StepId, context.Duration));
       return Task.CompletedTask;
   }

   public Task OnExceptionAsync(PipelineStepContext context) => Task.CompletedTask;

   public void PrintReport()
   {
       foreach (var (stepId, duration) in _timings)
       {
           Console.WriteLine($"{stepId}: {duration.TotalMilliseconds}ms");
       }
   }
}
```

#### Using Inspectors
Inspectors can be combined to provide comprehensive monitoring:

```csharp
// Create and add inspectors
var performanceInspector = new PerformanceInspector();
var concurrencyMonitor = new ConcurrencyMonitorInspector();

var builder = new PipelineBuilder()
   .AddInspector<LoggingPipelineInspector>()
   .AddInspector(performanceInspector)
   .AddInspector(concurrencyMonitor);

// Build your pipeline
builder
   .UseMiddleware("DB Migration")
   .UseMiddleware("Cache Warmup")
   .Run(() => Console.WriteLine("Done"), "Final Step");

// Run the pipeline
await pipeline.Run();

// Check metrics
performanceInspector.PrintReport();
var concurrencyReport = concurrencyMonitor.GenerateReport();

// Output might be:
// DB Migration: 1234.56ms
// Cache Warmup: 567.89ms
// Final Step: 1.23ms
```

#### Best Practices for Inspectors

1. **Development vs Production**
   - Use detailed logging in development
   - Consider performance impact in production
   - Use step IDs for better tracking

2. **Performance Considerations**
   - Inspectors run for every step
   - Consider using conditional logging
   - Be mindful of memory usage in long-running pipelines

3. **Debugging Tips**
   - Use meaningful step IDs
   - Combine multiple inspectors for full visibility
   - Save inspector reports for post-execution analysis

#### Step IDs
Every step in your pipeline can have an optional ID for tracking:
```csharp
builder
   .UseMiddleware("Step 1")         // Class middleware
   .Use(next => next(context), "Step 2")         // Delegate middleware
   .Run(() => DoSomething(), "Step 3")           // Run action
   .RunAsync(async ctx => await Task.Delay(1), "Step 4")  // RunAsync action
   .UseBranch(
       ctx => Task.FromResult(true),
       branch => branch.Use(next => next(ctx)),
       "Conditional Branch"  // Branch ID
   );
```

These IDs appear in your inspector contexts, making it easy to:
- Track execution flow
- Measure performance
- Debug issues
- Create audit trails


## Best Practices

### Pipeline Design
- 🎯 Keep pipelines focused on a single responsibility
- 📦 Use class-based middleware for complex or reusable operations
- 🔄 Use actions (`Run`/`RunAsync`) for simple, one-off tasks
- ⚡ Use `TryRun`/`TryWhen`/`TryBranch` for non-critical operations
- 🔍 Add inspectors early in development for debugging

### Dependency Injection
- 💉 Prefer class-based middleware over closures when you need DI
- 🔒 Use `UseNewScope()` to isolate service lifetimes
- 🌳 Create new scopes for parallel branches to prevent concurrency issues
- ⚠️ Don't capture scoped services in closures

### Error Handling
- 🛡️ Use Try* methods for operations that shouldn't stop the pipeline
- 🎯 Use standard methods when pipeline should stop on failure
- 🔍 Use inspectors for centralized error tracking

### Performance
- 🚀 Run independent operations in parallel using branches per input and driving input from Chunks or Individual items etc - setting concurrency options as needed.
- ⏱️ Use performance inspectors to identify bottlenecks
- 🎯 Keep middleware focused and lightweight

### Debugging
- 📝 Assign meaningful step IDs to important pipeline steps
- 🔍 Use logging inspectors in development
- 📊 Track execution times with performance inspectors
- 🐛 Use `TryWhen` with logging during development to debug conditions

## Contributing
PRs welcome! Check out the [contribution guidelines](CONTRIBUTING.md).

## License
This project is available under a dual license:
- Free for non-commercial use under AGPL-3.0
- Commercial use requires a separate license. Contact [details in LICENCE.md]

## Need Help?
- 📖 [Documentation](link-to-docs)
- 🐛 [Issue Tracker](link-to-issues)
- 💬 [Discussions](link-to-discussions)
