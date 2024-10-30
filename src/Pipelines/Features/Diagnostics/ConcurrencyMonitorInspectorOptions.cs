namespace Dazinator.Extensions.Pipelines.Features.Diagnostics;

using Microsoft.Extensions.DependencyInjection;

public class ConcurrencyMonitorInspectorOptions
{
    public bool IncludeExecutionTimings { get; set; } = false;

}
