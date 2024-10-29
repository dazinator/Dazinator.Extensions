namespace Dazinator.Extensions.Pipelines;

using System.Runtime.CompilerServices;

public interface IPipelineBuilder
{
    // Core builder functionality
    Pipeline Build();   
    /// <summary>
    /// Adds a middleware step to the pipeline.
    /// </summary>
    /// <param name="middleware"></param>
    /// <param name="stepId"></param>
    /// <returns></returns>
    IPipelineBuilder Use(Func<PipelineStep, PipelineStep> middleware, string? stepId = null, [CallerMemberName] string? stepTypeName = null);
    // Extension state management
    T? GetExtensionState<T>() where T : class;
    void SetExtensionState<T>(T state) where T : class;
    bool HasExtensionState<T>() where T : class;
    // Inspector management
    IPipelineBuilder AddInspector(IPipelineInspector inspector);
    IPipelineBuilder AddInspector<T>() where T : IPipelineInspector;
    int CurrentStepIndex { get; }
    // Access to services
    IServiceProvider ServiceProvider { get; }
}
