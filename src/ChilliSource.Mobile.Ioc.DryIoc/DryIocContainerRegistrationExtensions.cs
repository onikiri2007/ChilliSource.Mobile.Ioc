using DryIoc;
using Splat;

namespace ChilliSource.Mobile.Ioc.DryIoc
{
    public static class DryIocContainerRegistrationExtensions
    {
        /// <summary>
        /// Registers DryIoc Dependency resolver as Splat dependency resolver
        /// </summary>
        /// <param name="container"></param>
        public static void UseDryIocDependencyResolver(this IContainer container)
        {
            var resolver = new DryIocDependencyResolver(container);
            Locator.Current = resolver;
        }
    }
}
