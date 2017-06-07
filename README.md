[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT) ![Built With C#](https://img.shields.io/badge/Built_with-C%23-green.svg)

# ChilliSource.Mobile.Ioc #

This project is part of the ChilliSource framework developed by [BlueChilli](https://github.com/BlueChilli).

## Summary ##

```ChilliSource.Mobile.Ioc``` provides a [DryIoc](https://bitbucket.org/dadhi/dryioc) based dependency resolver for the [Splat](https://github.com/paulcbetts/splat)  Service Locator. 

## Usage ##

### Registering dependencies with the IoC Container ###

```csharp
var container = new Container();
container.Register<ITestInterface, ImpTestClassObject>();
container.Register<ParentObject, ParentObject>();
container.Register<ChildObject, ChildObject>();
```
For more information please read the [DryIoc Documentation](https://bitbucket.org/dadhi/dryioc/wiki/Home)

### Registering the resolver with the Splat Service Locator ###

```csharp
container.UseDryIocDependencyResolver();
```

### Registering dependencies via the Splat Service Locator ###

```csharp
Locator.CurrentMutable.Register(() => new ChildObject(), typeof(ChildObject));
```

For more information please read the [Splat Documentation](https://github.com/paulcbetts/splat)

### ReactiveUI Integration ###

### Register all views in the assembly ###

The following code will register all views in the assembly that implement ReactiveUI's ```IViewFor<T>``` interface:

```csharp
var assembly = typeof(HomePageViewModel).Assembly;
container.RegisterViews(assembly);
container.UseDryIocDependencyResolver();
```

### Register all view models in the assembly ###

Based on the MVVM convention of naming view models with the "ViewModel" suffix, the following code will search for all types suffixed with "ViewModel" in the assembly and register them in the container in order to automatically link them to their corresponding views.

```csharp
var assembly = typeof(HomePageViewModel).Assembly;
container.RegisterViewModels(assembly);
container.UseDryIocDependencyResolver();
```

### Resolve dependencies via the Splat Service Locator ###

```csharp
var child = Locator.Current.GetService<ChildObject>();
```

## Installation ##

The library is available via NuGet [here](https://www.nuget.org/packages/ChilliSource.Mobile.Ioc).

## Releases ##

See the [releases](https://github.com/BlueChilli/ChilliSource.Mobile.Ioc/releases).

## Contribution ##

Please see the [Contribution Guide](.github/CONTRIBUTING.md).

## License ##

ChilliSource.Mobile is licensed under the [MIT license](LICENSE).

## Feedback and Contact ##

For questions or feedback, please contact [chillisource@bluechilli.com](mailto:chillisource@bluechilli.com).


