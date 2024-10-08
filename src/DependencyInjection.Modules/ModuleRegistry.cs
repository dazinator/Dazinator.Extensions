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

    private static IModule<TOptions> ActivateModule<TOptions>(IServiceProvider serviceProvider, Type moduleType)
      where TOptions : class, new()
    {
        return ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, moduleType) as IModule<TOptions>;
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

    private ServiceCollection EnsureInternalServices()
    {
        if (InternalServices == null)
        {
            InternalServices = new ServiceCollection();
        }

        return InternalServices;
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
        where TOptions : class, new() => RegisterWithOptions<TModule, TOptions>((o) =>
        {
            if (configureOptions is not null)
            {
                o.Configure(configureOptions);
            }
        }, overrideConfigurationKey, moduleName);

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
    public IModuleRegistry RegisterWithOptions<TModule, TOptions>(Action<OptionsBuilder<TOptions>>? configureOptions = null, string? overrideConfigurationKey = null, string? moduleName = null)
        where TModule : IModule<TOptions>
        where TOptions : class, new() => RegisterWithOptions(typeof(TModule), (internalSp) => ActivateModule<TModule>(internalSp), configureOptions, overrideConfigurationKey, moduleName);

    /// <summary>
    /// Registers a module that requires <see cref="IOptionsMonitor{TOptions}"/> in order to participate in adding services to the application DI container.
    /// </summary>
    /// <param name="overrideConfigurationKey">Explicitly set the the modules configuration key used to bind its options, overriding any that the module supplies by default.</param>
    /// <param name="configureOptions"></param>
    /// <param name="moduleName">Optional name passed through to the module.
    /// This will also configure a named options of the same name.
    /// Useful so that multiple instances of the same module type, can each have individual options, which they can retreive using <see cref="IOptionsMonitor{TOptions}.Get"/></param>
    /// <typeparam name="TModule"></typeparam>
    /// <typeparam name="TOptions"></typeparam>
    /// <returns></returns>
    public IModuleRegistry RegisterWithOptions<TOptions>(Type moduleType,
        Func<IServiceProvider, IModule<TOptions>> moduleInstanceFactory,
        Action<OptionsBuilder<TOptions>>? configureOptions = null,
        string? overrideConfigurationKey = null,
        string? moduleName = null)
        where TOptions : class, new()
    {
        var optionsReg = new ModuleOptionsRegistry<TOptions>(moduleName);
        optionsReg.UseModuleOptionsBindingAttributeConvention(moduleType, overrideConfigurationKey);
        if (configureOptions != null)
        {
            optionsReg.UseOptionsBuilder(configureOptions);
        }
        optionsReg.Build(EnsureInternalServices());
        var finalModuleName = optionsReg.Name;
        AddModuleInit((nestedRegistry, internalSp) =>
        {
            var moduleInstance = moduleInstanceFactory(internalSp);
            if (moduleInstance is null)
            {
                throw new ArgumentException("Module factory returned null.");
            }
            // ActivateModule<TOptions>(internalSp, moduleType);
            moduleInstance.Name = finalModuleName ?? Options.DefaultName;
            var optionsMonitor = GetOptionsMonitor<TOptions>(internalSp);
            moduleInstance?.Register(nestedRegistry, optionsMonitor);
        });
        return this;
    }


    /// <summary>
    /// Register a module with the application. The module will be registered with the application and the options will be bound to the configuration.
    /// </summary>
    /// <param name="moduleFactory"></param>
    /// <param name="configureModuleOptions"></param>
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
        var moduleInstance = moduleFactory();
        if (moduleInstance is null)
        {
            throw new ArgumentException("Module factory returned null.");
        }
        return RegisterWithOptions(moduleInstance.GetType(), (internalSp) => moduleInstance, (b) =>
        {
            if (configureOptions is not null)
            {
                b.Configure(configureOptions);
            }
        }, overrideConfigurationKey, moduleName);

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
