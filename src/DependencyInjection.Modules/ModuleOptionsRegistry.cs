namespace DependencyInjection.Modules;

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

public class ModuleOptionsRegistry<TOptions>
    where TOptions : class
{

    public List<Action<IServiceCollection, OptionsBuilder<TOptions>>> _buildCallbacks { get; set; }

    public ModuleOptionsRegistry(string? name)
    {
        Name = name;
    }
    public string? Name { get; private set; }

    public List<Action<IServiceCollection, OptionsBuilder<TOptions>>> GetCallacksList()
    {
        if (_buildCallbacks is null)
        {
            _buildCallbacks = new();
        }
        return _buildCallbacks;
    }

    internal bool HasBuildActions => _buildCallbacks?.Any() ?? false;

    /// <summary>
    /// Binds the options for the module to the configuration section with the key specified by the <see cref="ModuleOptionsBindingAttribute"/> on the module type, or the key specified by the overrideConfigurationKey parameter if provided.
    /// </summary>
    /// <param name="overrideConfigurationKey"></param>
    public void UseModuleOptionsBindingAttributeConvention(Type moduleType, string? overrideConfigurationKey = null)
    {
        var attribute = GetModuleConfigurationAttribute(moduleType);
        var configurationSectionKey = string.IsNullOrWhiteSpace(overrideConfigurationKey) ? attribute?.ConfigurationKey : overrideConfigurationKey;
        Name ??= attribute?.Name ?? Options.DefaultName;
        var fullConfigSectionKey = configurationSectionKey?.Replace("{OptionsName}", Name);

        if (!string.IsNullOrWhiteSpace(fullConfigSectionKey))
        {
            var callbacks = GetCallacksList();
            callbacks.Add((services, optionsBuilder) => optionsBuilder.BindConfiguration(fullConfigSectionKey));
        }
    }

    public void UseOptionsBuilder(Action<OptionsBuilder<TOptions>>? configureOptions)
    {
        var callbacks = GetCallacksList();
        callbacks.Add((services, optionsBuilder) => configureOptions?.Invoke(optionsBuilder));
    }

    private ModuleOptionsBindingAttribute? GetModuleConfigurationAttribute(Type moduleType)
    {
        var attribute = moduleType.GetCustomAttribute<ModuleOptionsBindingAttribute>();
        return attribute;
    }

    public void Build(IServiceCollection serviceCollection)
    {
        var optionsBuilder = serviceCollection.AddOptions<TOptions>(Name ?? Options.DefaultName);
        if (HasBuildActions)
        {
            foreach (var callback in GetCallacksList())
            {
                callback(serviceCollection, optionsBuilder);
            }
        }
        optionsBuilder.ValidateDataAnnotations();

    }
}
