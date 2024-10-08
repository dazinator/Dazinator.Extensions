namespace DependencyInjection.Modules;

using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

/// <summary>
/// The Module registry is used to register modules with a DI container. Modules represent logical groupings of services that should be registered together, and can have optional configuration controlling how they should be registered.
/// </summary>
public class ModuleRegistry : IModuleRegistry
{
    private bool _isInnerServiceProviderRequired;

    /// <summary>
    /// The Module registry is used to register modules with a DI container. Modules represent logical groupings of services that should be registered together, and can have optional configuration controlling how they should be registered.
    /// </summary>
    /// <param name="services">The application <see cref="IServiceCollection"/> that modules will contribute services to.</param>
    /// <param name="configuration"></param>
    public ModuleRegistry(IServiceCollection services, IConfiguration configuration)
    {
        Services = services;
        Configuration = configuration;
    }

    public IServiceCollection Services { get; }
    public IConfiguration Configuration { get; }
    private ServiceCollection? InternalServices { get; set; }
    private List<Action<IModuleRegistry>>? ModuleInitCallbacks { get; set; }
    private IServiceProvider? InternalServiceProvider { get; set; }

    public IModuleRegistry Register(IModule module)
    {
        AddModuleInit((r) =>
        {
            module?.Register(r);
        });
        return this;
    }

    public IModuleRegistry Register<TModule>() where TModule : IModule
    {
        AddModuleInit((r, sp) =>
        {
            var moduleInstance = ActivateModule<TModule>(sp);
            moduleInstance?.Register(r);
        });
        return this;
    }

    private static TModule ActivateModule<TModule>(IServiceProvider serviceProvider)
        //where TModule : IModule
    {
        return ActivatorUtilities.GetServiceOrCreateInstance<TModule>(serviceProvider);
    }

    private IServiceProvider GetRequiredInternalServiceProvider()
    {
        Debug.Assert(InternalServiceProvider != null, "InternalServiceProvider!=null");
        if (InternalServiceProvider == null)
        {
            throw new InvalidOperationException("InternalServiceProvider has not been built yet.");
        }
        return InternalServiceProvider;
    }

    private static IOptionsMonitor<TOptions> GetOptionsMonitor<TOptions>(IServiceProvider internalSp)
        where TOptions : class, new()
    {
        return internalSp.GetRequiredService<IOptionsMonitor<TOptions>>();
    }

    private ModuleOptionsBindingAttribute? GetModuleConfigurationAttribute(Type moduleType)
    {
        var attribute = moduleType.GetCustomAttribute<ModuleOptionsBindingAttribute>();
        return attribute;
    }

    private ServiceCollection EnsureInternalServices()
    {
        if (InternalServices == null)
        {
            InternalServices = new ServiceCollection();
        }

        return InternalServices;
    }


    private void AddOptions<TOptions>(string? optionsName, Action<TOptions>? configureOptions, string? fullConfigSectionKey)
        where TOptions : class, new()
    {
        var internalServices = EnsureInternalServices();
        if (string.IsNullOrWhiteSpace(optionsName))
        {
            optionsName = Options.DefaultName;
        }
        var optionsBuilder = internalServices.AddOptions<TOptions>(optionsName);
        if (!string.IsNullOrWhiteSpace(fullConfigSectionKey))
        {
            optionsBuilder = optionsBuilder.BindConfiguration(fullConfigSectionKey);
        }

        // OptionsServices.Configure<TOptions>(Configuration.GetSection(fullConfigSectionKey));
        if (configureOptions != null)
        {
            optionsBuilder = optionsBuilder.Configure(configureOptions);
        }

        optionsBuilder.ValidateDataAnnotations();
    }

    /// <summary>
    /// Register a module with the application. The module will be registered with the application and the options will be bound to the configuration.
    /// </summary>
    /// <param name="overrideConfigurationKey">Explicitly set the the modules configuration key used to bind its options, overriding any that the module supplies by default.</param>
    /// <param name="configureOptions"></param>
    /// <param name="moduleName">Optional name passed through to the module.
    /// This will also configure a named options of the same name.
    /// Useful so that multiple instances of the same module type, can each have individual options, which they can retreive using <see cref="IOptionsMonitor{TOptions}.Get"/></param>
    /// <typeparam name="TModule"></typeparam>
    /// <typeparam name="TOptions"></typeparam>
    /// <returns></returns>
    public IModuleRegistry Register<TModule, TOptions>(Action<TOptions>? configureOptions = null, string? overrideConfigurationKey = null, string? moduleName = null)
        where TModule : IModule<TOptions>
        where TOptions : class, new()
    {

        var attribute = GetModuleConfigurationAttribute(typeof(TModule));
        var configurationSectionKey = string.IsNullOrWhiteSpace(overrideConfigurationKey) ? attribute?.ConfigurationKey : overrideConfigurationKey;
        var name = string.IsNullOrWhiteSpace(moduleName) ? attribute?.Name : moduleName;
        var finalModuleName = name ?? string.Empty;
        var fullConfigSectionKey = configurationSectionKey?.Replace("{OptionsName}", finalModuleName);

        AddOptions(name, configureOptions, fullConfigSectionKey);
        AddModuleInit((r, sp) =>
        {
            var moduleInstance = ActivateModule<TModule>(sp);
            moduleInstance.Name = finalModuleName;
            var optionsMonitor = GetOptionsMonitor<TOptions>(sp);
            moduleInstance?.Register(r, optionsMonitor);           
        });
        return this;     
      
    }

    /// <summary>
    /// Register a module with the application. The module will be registered with the application and the options will be bound to the configuration.
    /// </summary>
    /// <param name="moduleFactory"></param>
    /// <param name="configureOptions"></param>
    /// <param name="overrideConfigurationKey"></param>
    /// <param name="moduleName">Optional name passed through to the module.
    /// This will also configure a named options of the same name.
    /// Useful so that multiple instances of the same module type, can each have individual options, which they can retreive using <see cref="IOptionsMonitor{TOptions}.Get"/></param>
    /// <typeparam name="TOptions"></typeparam>
    /// <returns></returns>
    /// <remarks>Added so that you can take control of module instantiation. Especially helpful during tests.</remarks>
    public IModuleRegistry Register<TOptions>(
        Func<IModule<TOptions>> moduleFactory,
        Action<TOptions>? configureOptions = null,
        string? overrideConfigurationKey = null,
        string? moduleName = null)
        where TOptions : class, new()
    {
        var module = moduleFactory();
        if (module is null)
        {
            throw new ArgumentException("Module factory returned null.");
        }

        var attribute = GetModuleConfigurationAttribute(module.GetType());
        var configurationSectionKey = string.IsNullOrWhiteSpace(overrideConfigurationKey) ? attribute?.ConfigurationKey : overrideConfigurationKey;
        var name = string.IsNullOrWhiteSpace(moduleName) ? attribute?.Name : moduleName;
        module.Name = name ?? string.Empty;
        // This allows a templated config section key to be used,
        // for example: "MyModule:{OptionsName}"
        // and then the OptionsName will be replaced with the actual options name before we bind it to the configuration.
        var fullConfigSectionKey = configurationSectionKey?.Replace("{OptionsName}", module.Name);
        // if (string.IsNullOrWhiteSpace(fullConfigSectionKey))
        // {
        // A module, with options, and without any configuration key - could be valid if its default Options is valid.
        // We let options validation handle this.
        //   throw new Exception("Module does not have a configuration key.");
        //}

        AddOptions(name, configureOptions, fullConfigSectionKey);
        AddModuleInit((nestedRegistry, internalSp) =>
        {
            // this is invoked only after the OptionsServiceProvider has been created.
            var optionsMonitor = GetOptionsMonitor<TOptions>(internalSp);
            module?.Register(nestedRegistry, optionsMonitor);
        });
        return this;
    }

    private void AddModuleInit(Action<IModuleRegistry, IServiceProvider> registerModule)
    {
        EnsureInternalServices();
        AddModuleInit((nestedRegistry) =>
        {
            var internalSp = GetRequiredInternalServiceProvider();
            registerModule?.Invoke(nestedRegistry, internalSp);
        });
    }

    private void AddModuleInit(Action<IModuleRegistry> registerModule)
    {
        ModuleInitCallbacks ??= new List<Action<IModuleRegistry>>();
        ModuleInitCallbacks.Add((nestedRegistry) =>
        {
            registerModule?.Invoke(nestedRegistry);
        });
    }

    internal void Build(Func<IServiceCollection, IServiceProvider> buildServiceProvider)
    {
        if (InternalServices != null)
        {
            InternalServices.TryAddSingleton(Configuration);
            InternalServiceProvider = buildServiceProvider(InternalServices);
        }

        if (ModuleInitCallbacks != null)
        {
            // Create a nested registry for each module to register its own set of modules with.
            var nestedRegistry = new ModuleRegistry(Services, Configuration);
            foreach (var moduleInit in ModuleInitCallbacks)
            {
                moduleInit(nestedRegistry);
            }

            nestedRegistry.Build(buildServiceProvider); // recursive.
        }
    }
}
