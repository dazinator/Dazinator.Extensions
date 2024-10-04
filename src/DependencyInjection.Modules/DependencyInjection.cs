namespace DependencyInjection.Modules;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

public static class DependencyInjection
{
    /// <summary>
    /// Add modules to the service collection. A module is a collection of services that are registered together.
    /// </summary>
    /// <param name="services">Your applicaation services for which modules will add services to.</param>
    /// <param name="configuration">Supply configuration from which any modules that require configuration can find it.</param>
    /// <param name="configure">Optionally supply a callback to configure this modules options.</param>
    /// <param name="buildServiceProvider">Build the seperte isolated service provider that is used by the module registry, to build <see cref="IOptionsMonitor{TOptions}"/> classes for your modules.</param>
    /// <returns></returns>
    public static IServiceCollection AddModules(this IServiceCollection services, IConfiguration configuration, Action<IModuleRegistry> configure, Func<IServiceCollection, IServiceProvider> buildServiceProvider)
    {
        // Register modules
        var app = new ModuleRegistry(services, configuration);
        // allow for other modules to be registered.
        configure(app);
        app.Build(buildServiceProvider);
        return services;
    }
}
