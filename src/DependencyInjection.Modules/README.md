# Module Registry

The Module Registry is a flexible system for registering and managing modules in a .NET application. It provides a structured way to organize services, configure options, and manage dependencies between different parts of your application.

## Introduction

The Module Registry allows you to break down your application into logical modules, each responsible for registering its own services and configuration. This promotes a modular architecture, making your application easier to maintain and extend.

## Key Features

- **Module Registration**: Easily register modules with or without options.
- **Configuration Binding**: Automatically bind configuration sections to module options.
- **Nested Modules**: Support for modules registering other modules, allowing for complex hierarchies.
- **Integration with Dependency Injection**: Seamlessly works with .NET's built-in dependency injection system.

## Getting Started

To use the Module Registry in your project:

1. Install the necessary NuGet package (details to be added).

2. In your application's startup code (e.g., `Program.cs` or `Startup.cs`), use the `AddModules` extension method to set up the Module Registry:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using YourNamespace.ModuleRegistry; // Adjust this to your actual namespace

public class Startup
{
    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddModules(Configuration, ConfigureModules);
    }

    private void ConfigureModules(IModuleRegistry registry)
    {
        // Register your modules here
        registry.Register<Module1>();
        registry.Register<Module2, Module2Options>();
        // ... register other modules as needed
    }
}
```

3. Define your modules by implementing the `IModule` or `IModule<TOptions>` interface:

```csharp
public class Module1 : IModule
{
    public void Register(IModuleRegistry registry)
    {
        // Register services for this module
    }
}

[ModuleOptionsBinding("Module2")] // Optional, if you want TOptions to be automatically bound to a specific configuration section.
public class Module2 : IModule<Module2Options>
{
    public void Register(IModuleRegistry registry, IOptionsMonitor<Module2Options> optionsMonitor)
    {
        var options = optionsMonitor.Get(this.Name);  // Use this.Name to get the correct options instance - it will be the name from the attribute or the one supplied at registration time if overriden.
        // Use options to configure and register services for this module
        
        // Can also use the registr to register other modules.
        // registry.Register<ChildModule>();
    }
}

[ModuleOptionsBinding(configurationKey: "Module3:Foo", optionsName: "Foo")] // Optional, bind options named "Foo". This is only a default name, the final name can be overridden by the caller at registration time.
public class Module3 : IModule<Module3Options>
{
    public void Register(IModuleRegistry registry, IOptionsMonitor<Module3Options> optionsMonitor)
    {
         var options = optionsMonitor.Get(this.Name);  // this.Name will be "Foo" as per attribute, unless override at registration time by the caller in which case it will be that value. Using this.Name ensures we use the correct value.
          
    }
}
```

4. Configure your modules in your application's configuration file (e.g., `appsettings.json`):

```json
{
   "Module2": {
      "Setting1": "Value1",
      "Setting2": "Value2"
   },
   "Module3": {
      "Foo": {
         "Setting1": "FooValue1",
         "Setting2": "FooValue2"
      }
   }
}
```

The `AddModules` extension method takes care of creating the `ModuleRegistry`, registering your modules, and building the registry. It provides a clean and convenient way to set up your module system.

### Advanced Usage

#### Dependency Injection

If you want to inject dependencies into modules, you can add them to the module registry's internal services.
These are only used for module injection, they won't be included in your applications DI container, unless a module chooses to register one as a service.

```csharp
services.AddModules(
    Configuration, 
    ConfigureModules,
    serviceCollection => {
        serviceCollection.AddSingleton<IService>(new Service());
        return serviceCollection.BuildServiceProvider(); // service provider used for module activation.
    })
);
```

and

```csharp
public class ModuleIsInjected : IModule
{
    public Module1(IService service)
    {
        // Use the injected service
    }
    ...
}
```

This hook can also be useful if you want to return a different DI container (IServiceProvider) implementation that the registry should use for resolving module dependencies.


#### ModuleOptionsBinding attribute

This attribute is placed above modules that derive from `IModule<MyOptions>` and is used to specify:
- the configuration key to bind the options to.
- the name of the options to bind to. This is useful when you have multiple instances of the same module, each with its own configuration.
However these values are only "defaults". They can be overridden at registration time.
```csharp
moduleRegistry.Register<DefaultOptionsModule, MyOptions>(configurationKey: "CustomKey", moduleName: "CustomName");
```

The final configuration key and options / module name (they are the same) is taken from the following order of precedence:
- Explicit provided at registration time by the caller.
- Values specified by the `ModuleOptionsBinding` attribute.

What this means is that in a module, if it wants to get its options,
it should use `optionsMonitor.Get(this.Name)` instead of `optionsMonitor.CurrentValue` or  `optionsMonitor.Get("HardCoded")` to ensure it gets the right options no matter the use case - this will either be the name from the attribute (if present) or the one supplied by the caller (if they specified one). 
If neither is present, the module will use the default options name and so this will still work - you'll get the default options instance.

```csharp
[ModuleOptionsBinding(configurationKey: "NamedOptionsModule:{OptionsName}", optionsName: "Default")]
public class NamedOptionsModule : IModule<MyOptions>
{
    public void Register(IModuleRegistry registry, IOptionsMonitor<MyOptions> optionsMonitor)
    {       
        var options = optionsMonitor.Get(this.Name); // IModule has a Name property that will be set by the registry, so you'll end up with the right options no matter the use case. 
        // Use options to configure services
    }    
}

// In your startup code:
moduleRegistry.Register<NamedOptionsModule, MyOptions>(); // config section: "NamedOptionsModule:Default" > bound to named options "Default".
moduleRegistry.Register<NamedOptionsModule, MyOptions>(moduleName: "MyNamedModule");  // config section: "NamedOptionsModule:MyNamedModule" > bound to named options "MyNamedModule".
moduleRegistry.Register<NamedOptionsModule, MyOptions>(configurationSectionKey: "Custom", moduleName: "Custom"); // config section: "Custom" > bound to named options "Custom".
var myVariable = GetSomeName();
moduleRegistry.Register<NamedOptionsModule, MyOptions>(configurationSectionKey: "Mods:{OptionsName}", moduleName: myVariable); // config section: "Mods:xyzb22" > bound to named options "xyzb22" i.e whatever myVariable is.
```

##### Why Use Named Options?

Named options allow multiple modules to share the same options type while having different configurations. This is particularly useful in scenarios such as:

1. **Multi-tenant applications**: You might have a `TenantModule<TenantOptions>` for each tenant, where each instance is configured differently:

   ```csharp
   public class TenantModule : IModule<TenantOptions>
   {
       private readonly string tenantId;

       public TenantModule(string tenantId)
       {
           this.tenantId = tenantId;
       }

       public void Register(IModuleRegistry registry, IOptionsMonitor<TenantOptions> optionsMonitor)
       {
           var options = optionsMonitor.Get(this.Name);
           // Configure tenant-specific services based on options
       }       
   }

   // In your startup code:
   moduleRegistry.Register(() => new TenantModule("Tenant1"));
   moduleRegistry.Register(() => new TenantModule("Tenant2"));
   ```

   This allows each tenant to have its own configuration (e.g., feature flags, database connections) while using the same `TenantOptions` class.

2. **Environment-specific configurations**: You could have different named options for development, staging, and production environments.

3. **Feature variations**: If you have a feature that needs different configurations in different parts of your application, you can use named options to manage these variations.

Using named options provides flexibility and reusability in your module system, allowing for more granular control over configuration without needing to create separate options classes for every variation.

#### Nested Modules

Modules can register other modules:

```csharp
public class ParentModule : IModule
{
    public void Register(IModuleRegistry registry)
    {
        registry.Register<ChildModule>();
    }
}
```

## Best Practices

1. Keep modules focused on a specific feature or area of functionality.
2. Use meaningful names for your modules and option classes.
3. Leverage configuration to make your modules more flexible and reusable.
4. Use nested modules to create a clear hierarchy of features in your application.
