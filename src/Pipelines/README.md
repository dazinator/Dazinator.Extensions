# Pipeline Builder for .NET

[![Licence: AGPL-3.0 with Commercial Option](https://img.shields.io/badge/License-AGPL%20with%20Commercial%20Option-blue.svg)](LICENSE)

Build composable, inspectable execution pipelines with dependency injection support.

## Why Use This?
✨ Perfect for when you need to:
- Execute startup tasks in order/parallel with proper dependency injection
- Build multi-tenant initialization workflows
- Create database migration orchestration
- Set up conditional feature toggles
- Build diagnostic/telemetry pipelines
- Orchestrate complex business processes

## Pipeline Context
The pipeline context (`ctx` in examples) flows through your entire pipeline and provides:
```csharp
public class PipelineContext
{
    public IServiceProvider ServiceProvider { get; }  // Current service scope
    public CancellationToken CancellationToken { get; }
}
```

## Quick Example
```csharp
var builder = new PipelineBuilder()
    .UseScope()  // Create a scope for database work
    .Use(next => async ctx => {
        var logger = ctx.ServiceProvider.GetRequiredService<ILogger>();
        logger.LogInformation("Starting initialization...");
        await next(ctx);
    })
    // Conditional branch
    .UseBranch(
        async ctx => {
            var db = ctx.ServiceProvider.GetRequiredService<DbContext>();
            return await db.Migrations.Any();
        },
        branch => branch
            .UseMiddleware<DatabaseMigrationMiddleware>()
            .UseMiddleware<DataSeedingMiddleware>()
    )
    // Parallel branches for each tenant
    .UseParallelBranches(
        await GetTenants(),
        (branch, tenant) => branch
            .UseScope()  // Each tenant gets its own scope
            .UseMiddleware<TenantInitializer>()
    )
    // Conditional feature initialization
    .When(
        ctx => ctx.ServiceProvider.GetService<IFeatureFlags>().IsEnabled("NewFeature"),
        async ctx => await InitializeNewFeature(ctx)
    );

var pipeline = builder.Build(services);
await pipeline.Run(cancellationToken); // Optional cancellation token
```

## Installation
```bash
dotnet add package Dazinator.Extensions.Pipelines
```

## Middleware
Middlewares are the building blocks of your pipeline. You have two ways to create them:

### 1. Using Delegates (Simple Cases)
```csharp
builder.Use(next => async context => {
    var service = context.ServiceProvider.GetRequiredService<MyService>();
    // Do something before
    await next(context);
    // Do something after
});
```

### 2. Using Classes (Recommended for DI)
```csharp
public class LoggingMiddleware : IPipelineMiddleware
{
    private readonly ILogger _logger;

    public LoggingMiddleware(ILogger logger) // Injected automatically
    {
        _logger = logger;
    }

    public async Task ExecuteAsync(PipelineStep next, PipelineContext context)
    {
        _logger.LogInformation("Before execution");
        await next(context);
        _logger.LogInformation("After execution");
    }
}

// Usage:
builder.UseMiddleware<LoggingMiddleware>();
```

Always prefer class-based middleware when you need:
- Dependency injection
- Reusable components
- Testable code
- Complex logic

## DI Scopes
Scopes control the lifetime of your services and ensure proper resource management:

```csharp
builder
    .UseScope() // Creates new scope - new PipelineContext with scoped ServiceProvider
    .UseMiddleware<DbMiddleware>() // Gets scoped DbContext
    .UseParallelBranches(tenants, (branch, tenant) =>
        branch
            .UseScope() // Each branch gets its own scope
            .UseMiddleware<TenantMiddleware>() // Gets new scoped services
    );
```

Key benefits:
- Each scope gets fresh instances of scoped services
- Resources are properly disposed
- Isolated contexts for parallel operations
- Perfect for database contexts and other disposable resources

## Key Features
- 🔄 **Middleware Pipeline**: Similar to ASP.NET Core middleware but for any scenario
- 🌲 **Branching**: Conditional and parallel execution paths
- 💉 **DI Support**: Full dependency injection with scoping control
- 🔍 **Inspection**: Monitor execution, timing, and errors
- 🔀 **Composable**: Build and combine pipelines flexibly

## Best Practices
- Use `IPipelineMiddleware` for dependency injection rather than closures
- Use inspectors for logging, timing, and diagnostics
- Leverage scopes for proper resource management
- Consider using `When` for flexible conditional logic

## More Examples & Documentation
[Link to full documentation]

## Contributing
PRs welcome! Check out our [contribution guidelines](CONTRIBUTING.md).
