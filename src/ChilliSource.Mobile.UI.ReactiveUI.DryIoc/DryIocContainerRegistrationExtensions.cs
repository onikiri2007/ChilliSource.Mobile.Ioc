using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DryIoc;
using ReactiveUI;

namespace ChilliSource.Mobile.UI.ReactiveUI.DryIoc
{
    public static class DryIocContainerRegistrationExtensions
    {
        /// <summary>
        /// Register all types that implement generic interface IViewFor{T}
        /// </summary>
        /// <param name="container">Container builder</param>
        /// <param name="assemblies">Assemblies to be scanned</param>
        public static void RegisterViews(this IContainer container, Assembly[] assemblies)
        {
            foreach (var assembly in assemblies)
            {
                RegisterViews(container, assembly);
            }
        }

        /// <summary>
        /// Register all types that implement generic interface IViewFor{T}
        /// </summary>
        /// <param name="container">Container builder</param>
        /// <param name="assembly">Assembly to be scanned</param>
        public static void RegisterViews(this IContainer container, Assembly assembly)
        {
            container.RegisterMany(assembly
                                    .ExportedTypes
                                    .Where(type => type.GetImplementedInterfaces()
                                                        .Any(@interface => @interface.IsGeneric() 
                                                                           && @interface.GetGenericDefinitionOrNull() == typeof(global::ReactiveUI.IViewFor<>))));
        }

        /// <summary>
        /// Register all types that end with "ViewModel"
        /// IScreen implementation is ignored since it must be registered as a singleton
        /// </summary>
        /// <param name="container">Container builder</param>
        /// <param name="assemblies">Assemblies to be scanned</param>
        public static void RegisterViewModels(this IContainer container, Assembly[] assemblies)
        {
            foreach (var assembly in assemblies)
            {
                RegisterViewModels(container, assembly);
            }
        }

        /// <summary>
        /// Register all types that end with "ViewModel"
        /// <seealso cref="RegisterScreen"/>
        /// </summary>
        /// <param name="container">Container builder</param>
        /// <param name="assembly">Assembly to be scanned</param>
        public static void RegisterViewModels(this IContainer container, Assembly assembly) => RegisterViewModels(container, assembly, new List<Type>());

        /// <summary>
        /// Register all types that have "ViewModel" in their name
        /// </summary>
        /// <param name="container"></param>
        /// <param name="excludeTheseViewModels">viewModels to exclude from auto registration</param>
        /// <param name="assembly"></param>
        public static void RegisterViewModels(this IContainer container, Assembly assembly, IEnumerable<Type> excludeTheseViewModels)
        {
            container.RegisterMany(assembly.ExportedTypes.Where(type => type.Name.EndsWith("ViewModel") 
                && !excludeTheseViewModels.Contains(type) 
                && !type.IsInterface() 
                && !type.IsAbstract()), 
                action: (registrator, serviceTypes, implType) =>
                {
                    var s = serviceTypes.Where(m => m != typeof(IScreen)).ToArray();
                    registrator.RegisterMany(s, implType);
                }
            );
        }

        /// <summary>
        /// Performs assembly scanning for views, view models and the screen.
        /// View models are registered by convention and must have the "ViewName" in their class names.
        /// You can call this method only to register all required components.
        /// </summary>
        /// <param name="container">Container builder</param>
        /// <param name="assemblies">Assemblies to be scanned</param>
        public static void RegisterForReactiveUI(this IContainer container, Assembly[] assemblies)
        {
            foreach (var assembly in assemblies)
            {
                RegisterForReactiveUI(container, assembly);
            }
        }

        /// <summary>
        /// Performs assembly scanning for views, view models and the screen.
        /// View models are registered by convention and must have the "ViewName" in their class names.
        /// You can call this method only to register all required components.
        /// </summary>
        /// <param name="container">Container builder</param>
        /// <param name="assembly">Assemblies to be scanned</param>
        public static void RegisterForReactiveUI(this IContainer container, Assembly assembly)
        {
            container.RegisterViews(assembly);
            container.RegisterViewModels(assembly);
        }
    }
}

