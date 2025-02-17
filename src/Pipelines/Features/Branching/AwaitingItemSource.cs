namespace Dazinator.Extensions.Pipelines.Features.Branching;

using System;
using System.Runtime.CompilerServices;

/// <summary>
/// Decorates the builder for fluent API where are awaiting the caller to provide a source for items to process one per step.
/// </summary>
/// <typeparam name="T"></typeparam>
public class AwaitingItemSource<TItem> : IPipelineBuilder, IAwaitingItemSource<TItem>
{
    private readonly IPipelineBuilder _builder;

    public AwaitingItemSource(IPipelineBuilder builder)
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

    public int CurrentStepIndex => _builder.CurrentStepIndex;
}
