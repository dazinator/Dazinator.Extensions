namespace DependencyInjection.Modules;

using Microsoft.Extensions.Options;

/// <summary>
/// A module.
/// </summary>
public interface IModule<in TOptions>
    where TOptions : class, new()
{
    //string DefaultConfigurationKey => GetType().Name.Replace("Module", "");
    void Register(IModuleRegistry moduleRegistry, IOptionsMonitor<TOptions> optionsMonitor);

    /// <summary>
    /// Set by the module registry if a name is provided when the module is registered.
    /// </summary>
    public string Name { get; set; }
}

/// <summary>
/// A module.
/// </summary>
public interface IModule : IModule<EmptyModuleOptions>
{
    void Register(IModuleRegistry moduleRegistry);

    // Explicit interface implementation to fulfill the generic interface contract.
    void IModule<EmptyModuleOptions>.Register(IModuleRegistry moduleRegistry, IOptionsMonitor<EmptyModuleOptions> optionsMonitor)
    {
        Register(moduleRegistry);
    }
}

public sealed class EmptyModuleOptions
{
}

public abstract class BaseModule<TOptions> : IModule<TOptions>
    where TOptions : class, new()
{
    //string DefaultConfigurationKey => GetType().Name.Replace("Module", "");
    public abstract void Register(IModuleRegistry moduleRegistry, IOptionsMonitor<TOptions> optionsMonitor);

    /// <summary>
    /// Set by the module registry if a name is provided when the module is registered.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}

public abstract class BaseModule : IModule
{
    //string DefaultConfigurationKey => GetType().Name.Replace("Module", "");
    public abstract void Register(IModuleRegistry moduleRegistry);

    /// <summary>
    /// Set by the module registry if a name is provided when the module is registered.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
