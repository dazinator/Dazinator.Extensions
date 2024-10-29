namespace Dazinator.Extensions.Pipelines.Features.Process;

using System;
using System.Runtime.CompilerServices;

/// <summary>
/// Decorates the builder for fluent API where are awaiting the caller to provide a source for multiple items to process per step.
/// </summary>
/// <typeparam name="T"></typeparam>
public class AwaitingItemsSource<TItem> : IPipelineBuilder, IAwaitingItemsSource<TItem>
{
    private readonly IPipelineBuilder _builder;
    public AwaitingItemsSource(IPipelineBuilder builder)
    {
        _builder = builder;
    }

    // Implement IPipelineBuilder by delegating to inner builder
    public Pipeline Build() => _builder.Build();
    public T? GetExtensionState<T>() where T : class
        => _builder.GetExtensionState<T>();
    public void SetExtensionState<T>(T state) where T : class
        => _builder.SetExtensionState(state);
    public bool HasExtensionState<T>() where T : class
        => _builder.HasExtensionState<T>();
    public IPipelineBuilder AddInspector(IPipelineInspector inspector)
        => _builder.AddInspector(inspector);
    public IPipelineBuilder AddInspector<T>() where T : IPipelineInspector
        => _builder.AddInspector<T>();
    public IPipelineBuilder Use(Func<PipelineStep, PipelineStep> middleware, string? stepId = null, [CallerMemberName] string? stepTypeName = null) => _builder.Use(middleware, stepId, stepTypeName);

    public IServiceProvider ServiceProvider => _builder.ServiceProvider;

    public int CurrentStepIndex { get; }
}
