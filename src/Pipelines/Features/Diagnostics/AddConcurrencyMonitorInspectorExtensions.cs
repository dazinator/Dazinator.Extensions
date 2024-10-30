namespace Dazinator.Extensions.Pipelines.Features.Diagnostics;

using Microsoft.Extensions.DependencyInjection;

public static class AddConcurrencyMonitorInspectorExtensions
{
    public static PipelinesServicesBuilder AddConcurrencyMonitorInspector(
        this PipelinesServicesBuilder builder)
    {       
        builder.Services.AddTransient<ConcurrencyMonitorInspector>();
        return builder;
    }
}
