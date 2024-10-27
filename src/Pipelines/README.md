# Pipeline Builder for .NET

[![Licence: AGPL-3.0 with Commercial Option](https://img.shields.io/badge/License-AGPL%20with%20Commercial%20Option-blue.svg)](LICENCE)

Build composable, inspectable execution pipelines with dependency injection support.

## Table of Contents
- [Why Use This?](#why-use-this)
- [Quick Start](#quick-start)
- [Core Concepts](#core-concepts)
 - [Pipeline Context](#pipeline-context)
 - [Dependency Injection Scopes](#dependency-injection-scopes)
 - [Middleware vs Actions](#middleware-vs-actions)
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
Think of this as configuring a middleware pipeline like in ASP.NET Core, but for any processing scenario, with added support for branching and parallel execution.

```csharp
var builder = new PipelineBuilder()
   // Create a new DI scope
   .UseNewScope()  
   
   // Basic middleware - delegate
   .Use(next => async ctx => {
       var logger = ctx.ServiceProvider.GetRequiredService();
       logger.LogInformation("Starting...");
       await next(ctx);
   })
   
   // Simple synchronous action
   .Run(() => Console.WriteLine("Performing initialization..."))
   
   // Conditional branch
   .UseBranch(
       async ctx => await NeedsMigration(ctx), // the condition to enable the branch
       branch => branch // this is like a whole other nested pipeline you can configure here
           // Async action within branch
           .RunAsync(async ctx => 
           {
               await Task.Delay(100, ctx.CancellationToken); // Simulate work
               Console.WriteLine("Migration complete");
           })
   )
   
   // Parallel execution
   .UseParallelBranches(
       new[] { "Tenant1", "Tenant2" },
       (branch, tenant) => branch
           .UseNewScope()
           .Run(() => Console.WriteLine($"Initializing {tenant}"))
   );

var pipeline = builder.Build(services);
await pipeline.Run();

// Output:
// Starting...
// Performing initialization...
// Migration complete
// Initializing Tenant1
// Initializing Tenant2
```

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

### Parallel Execution
Execute multiple pipelines concurrently:

```csharp
builder.UseParallelBranches(
   new[] { "Region1", "Region2", "Region3" },
   (branch, region) => branch
       .UseNewScope()  // Each branch gets its own scope
       .Run(() => Console.WriteLine($"Starting {region}"))
       .TryRunAsync(
           async ctx => 
           {
               await Task.Delay(100, ctx.CancellationToken);
               if (region == "Region2") throw new Exception("Region2 failed");
               Console.WriteLine($"Completed {region}");
           },
           ex => Console.WriteLine($"Region failed: {ex.Message}")
       )
);

// Output might be:
// Starting Region1
// Starting Region2
// Starting Region3
// Region failed: Region2 failed
// Completed Region1
// Completed Region3
```

### Pipeline Inspection
Monitor execution, track timing, and handle errors throughout your pipeline using inspectors.

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

#### Example: Logging Inspector
```csharp
public class LoggingInspector : IPipelineInspector
{
   private readonly ILogger _logger;

   public LoggingInspector(ILogger logger)
   {
       _logger = logger;
   }

   public Task BeforeStepAsync(PipelineStepContext context)
   {
       _logger.LogInformation("Starting step: {StepId}", context.StepId);
       return Task.CompletedTask;
   }

   public Task AfterStepAsync(PipelineStepContext context)
   {
       _logger.LogInformation(
           "Completed step: {StepId} in {Duration}ms", 
           context.StepId, 
           context.Duration.TotalMilliseconds);
       return Task.CompletedTask;
   }

   public Task OnExceptionAsync(PipelineStepContext context)
   {
       _logger.LogError(
           context.Exception,
           "Error in step: {StepId}",
           context.StepId);
       return Task.CompletedTask;
   }
}
```

#### Example: Performance Tracking Inspector
```csharp
public class PerformanceInspector : IPipelineInspector
{
   private readonly List _timings = new();

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
```csharp
// Create and add inspectors
var performanceInspector = new PerformanceInspector();
var builder = new PipelineBuilder()
   .AddInspector<LoggingInspector>())
   .AddInspector(performanceInspector);

// Build your pipeline
builder
   .UseMiddleware("DB Migration")
   .UseMiddleware("Cache Warmup")
   .Run(() => Console.WriteLine("Done"), "Final Step");

// Run the pipeline
await pipeline.Run();

// Check performance metrics
performanceInspector.PrintReport();

// Output might be:
// DB Migration: 1234.56ms
// Cache Warmup: 567.89ms
// Final Step: 1.23ms
```

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
- 🚀 Run independent operations in parallel using `UseParallelBranches`
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
