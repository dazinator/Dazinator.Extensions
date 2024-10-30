namespace Dazinator.Extensions.Pipelines.Features.Process;

using System.Runtime.CompilerServices;

/// <summary>
/// Decorates an existing pipeline builder with an item of type T, allowing the pipeline to be built with the item available to steps.
/// </summary>
/// <typeparam name="T"></typeparam>
public class ItemBranchBuilder<TItem>: IPipelineBuilder
{
    private readonly IPipelineBuilder _builder;

    internal ItemBranchBuilder(IPipelineBuilder inner, TItem item)
    {
        _builder = inner;
        Input = item;
    }
    public TItem Input { get; }     

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
