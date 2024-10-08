namespace DependencyInjection.Modules;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

public interface IModuleRegistry
{
    IServiceCollection Services { get; }
    IConfiguration Configuration { get; }

    /// <summary>
    /// Register an instance of a module you constructed yourself.
    /// </summary>
    /// <param name="module"></param>
    /// <returns></returns>
    IModuleRegistry Register(IModule module);

    /// <summary>
    /// Register a module. The module will be activated with dependency injection using <see cref="ActivatorUtilities.GetServiceOrCreateInstance{T}"/>.
    /// </summary>
    /// <typeparam name="TModule"></typeparam>
    /// <returns></returns>
    IModuleRegistry Register<TModule>()
        where TModule : IModule;

    /// <summary>
    /// Register a module with auto configuration of options. The options will be bound to the configuration section with the key provided, or using a default convention if the key is not specified.
    /// </summary>
    /// <param name="overrideRootConfigurationKey"></param>
    /// <param name="configureOptions"></param>
    /// <typeparam name="TModule"></typeparam>
    /// <typeparam name="TOptions"></typeparam>
    /// <returns></returns>
    IModuleRegistry Register<TModule, TOptions>(Action<TOptions>? configureOptions = null, string? overrideRootConfigurationKey = null, string? moduleName = null)
        where TModule : IModule<TOptions>
        where TOptions : class, new();

    /// <summary>
    /// Register a module with auto configuration of options. This overload allows you to provide a factory method for the module instance.
    /// </summary>
    /// <param name="moduleFactory"></param>
    /// <param name="configureOptions"></param>
    /// <param name="overrideConfigurationKey"></param>
    /// <param name="moduleName">Optional name passed through to the module.
    /// This will also configure a named options of the same name.
    /// Useful so that multiple instances of the same module type, can each have individual options, which they can retreive using <see cref="IOptionsMonitor{TOptions}.Get"/></param>
    /// <typeparam name="TOptions"></typeparam>
    /// <returns></returns>
    IModuleRegistry Register<TOptions>(
        Func<IModule<TOptions>> moduleFactory,
        Action<TOptions>? configureOptions = null,
        string? overrideConfigurationKey = null,
        string? moduleName = null
    )
        where TOptions : class, new();


    /// <summary>
    /// Register a class that can participate in the configuration of a modules options class, by implemening any of:
    /// <see cref="IConfigureOptions{TOptions}"/>
    /// <see cref="IPostConfigureOptions{TOptions}"/>
    /// <see cref="IValidateOptions{TOptions}"/> 
    /// </summary>
    /// <typeparam name="TConfigureOptions"></typeparam>
    void ConfigureOptions<TConfigureOptions>()
        where TConfigureOptions : class;   
}
