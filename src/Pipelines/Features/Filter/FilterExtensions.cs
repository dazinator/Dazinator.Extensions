namespace Dazinator.Extensions.Pipelines;

using Dazinator.Extensions.Pipelines.Features.Filter;

public static class FilterExtensions
{ 

    /// <summary>
    /// Adds filter support to the pipeline. This extension must be called exactly once in the root pipeline,
    /// before any filter-specific extensions like WithIdempotency().
    /// </summary>
    /// <remarks>
    /// Filters provide step-level behaviors in your pipeline.
    /// <example>
    /// <code>
    /// // Correct usage:
    /// builder
    ///     .UseFilters()
    ///     .Use(...)
    ///     .WithIdempotency(...) // this is an extensions method that gets, and adds a filter to FilterRegistry.
    /// </code>
    /// </example>   
    /// <param name="builder">The pipeline builder</param>
    /// <returns>The pipeline builder for chaining</returns>
    public static IPipelineBuilder UseFilters(this IPipelineBuilder builder)
    {      
        var inspector = new FilterExecutionInspector();      
        builder.AddInspector(inspector);    
        return builder;
    }

    /// <summary>
    /// Called by extensions to configure the filters for the current step.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="addFilter"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static IPipelineBuilder AddFilters(
      this IPipelineBuilder builder,
      Action<FilterRegistry> addFilter)
    {
        var registry = builder.GetExtensionState<FilterRegistry>() ??
            throw new InvalidOperationException(
                "UseFilters() must be called before adding filters to enable the filter system.");

        registry.CurrentStepIndex = builder.CurrentStepIndex;
        addFilter(registry);
        return builder;
    }

}
