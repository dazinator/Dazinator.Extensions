| Branch  | DevOps |
| ------------- | ------------- |
| Master  | [![Build Status](https://darrelltunnell.visualstudio.com/Public%20Projects/_apis/build/status/dazinator.Dazinator.Extensions.DependencyInjection?branchName=master)](https://darrelltunnell.visualstudio.com/Public%20Projects/_build/latest?definitionId=12&branchName=master) |
| Develop | [![Build Status](https://darrelltunnell.visualstudio.com/Public%20Projects/_apis/build/status/dazinator.Dazinator.Extensions.DependencyInjection?branchName=develop)](https://darrelltunnell.visualstudio.com/Public%20Projects/_build/latest?definitionId=12&branchName=develop) |

| Package  | Stable | Pre-release |
| ------------- | --- | --- |
| Dazinator.Extensions.DependencyInjection.Modules  | [![Dazinator.Extensions.DependencyInjection.Modules](https://img.shields.io/nuget/v/Dazinator.Extensions.DependencyInjection.Modules.svg)](https://www.nuget.org/packages/Dazinator.Extensions.DependencyInjection.Modules/) | [![Dazinator.Extensions.DependencyInjection.Modules](https://img.shields.io/nuget/vpre/Dazinator.Extensions.DependencyInjection.Modules.svg)](https://www.nuget.org/packages/Dazinator.Extensions.DependencyInjection.Modules/) | 
| Dazinator.Extensions.DependencyInjection.NamedServices  | [![Dazinator.Extensions.DependencyInjection.NamedServices](https://img.shields.io/nuget/v/Dazinator.Extensions.DependencyInjection.NamedServices.svg)](https://www.nuget.org/packages/Dazinator.Extensions.DependencyInjection.NamedServices/) | [![Dazinator.Extensions.DependencyInjection.NamedServices](https://img.shields.io/nuget/vpre/Dazinator.Extensions.DependencyInjection.NamedServices.svg)](https://www.nuget.org/packages/Dazinator.Extensions.DependencyInjection.NamedServices/) | 
| Dazinator.Extensions.DependencyInjection.ChildContainers  | [![Dazinator.Extensions.DependencyInjection.ChildContainers](https://img.shields.io/nuget/v/Dazinator.Extensions.DependencyInjection.ChildContainers.svg)](https://www.nuget.org/packages/Dazinator.Extensions.DependencyInjection.ChildContainers/) | [![Dazinator.Extensions.DependencyInjection.ChildContainers](https://img.shields.io/nuget/vpre/Dazinator.Extensions.DependencyInjection.ChildContainers.svg)](https://www.nuget.org/packages/Dazinator.Extensions.DependencyInjection.ChildContainers/) | 
| Dazinator.Extensions.DependencyInjection.Microsoft | [![Dazinator.Extensions.DependencyInjection.Microsoft](https://img.shields.io/nuget/v/Dazinator.Extensions.DependencyInjection.Microsoft.svg)](https://www.nuget.org/packages/Dazinator.Extensions.DependencyInjection.Microsoft/) | [![Dazinator.Extensions.DependencyInjection.Microsoft](https://img.shields.io/nuget/vpre/Dazinator.Extensions.DependencyInjection.Microsoft.svg)](https://www.nuget.org/packages/Dazinator.Extensions.DependencyInjection.Microsoft/) | 

## Intro

This repository builds on `Microsoft.Extensions.DependencyInjection.Abstractions` to provide additional features, which currently are:

- Named Services
- Child Containers

It also provides a seperate (and optional) nuget package called `Dazinator.Extensions.DependencyInjection.Microsoft` which basically contains a copy of the native MS `ServiceProvider` but with some changes as published here: https://github.com/dotnet/runtime/issues/45497

## Named Services

Allows you to register services that can be resolved by name.
Note: In dotnet 8, microsoft have now added a feature to allow you to resolve services by name. However if you are using dotnet 6 or below, you can use this package to achieve the same thing.

For more detailed docs [see here](./src/DependencyInjection.NamedServices/README.md)

## Modules

Allows you to define modules that can be used to configure services in a modular way.
For more detailed docs [see here](./src/DependencyInjection.Modules/README.md)

## Child Containers

For more detailed docs [see here](./src/DependencyInjection.ChildContainers/README.md)

Allows you to configure "child containers" using the normal `IServiceCollection` interface.
The child service provider can be implemented by your `conforming container` of choice i.e Autofac, Structuremap, Microsoft DI - any DI package that supports IServiceProvider.

It means, thanks to a standard interface for building / configuring child containers, you can take a DI container library that doesn't have a child container feature,
(like I did with Microsofts) and create "child containers" with it! The caveat is that:

    - Your DI container of choice must support building a container from an `IServiceCollection` or IEnumerable<ServiceDescriptor>`
    
If you are interested in that, look at the tests for `ChildServiceCollection`

For docs, [see here](./src/DependencyInjection.ChildContainers/README.md)


## Pipelines

Build composable, inspectable execution pipelines with dependency injection support.
For more detailed docs [see here](./src/DependencyInjection.Pipelines/README.md)