namespace Dazinator.Extensions.Pipelines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public static class PipelineServiceExtensions
{   

    public static IServiceCollection AddPipelines(
        this IServiceCollection services,        
        Action<PipelinesServicesBuilder> configure)
    {
        services.AddOptions();       
        services.TryAddSingleton<PipelineRegistry>();

        var builder = new PipelinesServicesBuilder(services);
        configure?.Invoke(builder);
        return services;
    }


}

public class PipelinesServicesBuilder
{
    public PipelinesServicesBuilder(IServiceCollection services)
    {
        Services = services;
    }

    public IServiceCollection Services { get; }

    public PipelinesServicesBuilder AddPipeline(        
         string name,
         Action<PipelineBuilder> configure)
    {
        Services.AddOptions();
        Services.Configure<PipelineDefinition>(name, def =>
        {
            def.Configure = configure;
        });
        Services.TryAddSingleton<PipelineRegistry>();
        return this;
    }
}



public class PipelineDefinition
{
    public Action<PipelineBuilder>? Configure { get; set; }
}


