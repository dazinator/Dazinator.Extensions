namespace DependencyInjection.Modules;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ModuleOptionsBindingAttribute : Attribute
{
    public ModuleOptionsBindingAttribute(string configurationKey = null, string optionsName = null)
    {
        ConfigurationKey = configurationKey;
        Name = optionsName;
    }

    /// <summary>
    /// The options for this module will be bound from an <see cref="IConfigurationSection"/> using this key if one is specified.
    /// </summary>
    public string ConfigurationKey { get; }

    /// <summary>
    /// If a Name is specified, a Named Options will be configured from the configuration key, as opposed to the Default options instance.
    /// The module should retreive its options using <see cref="IOptionsMonitor{TOptions}.Get"/> with the same name.
    /// </summary>
    public string Name { get; set; }
}
