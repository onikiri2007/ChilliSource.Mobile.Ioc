using System;
using System.Collections.Generic;
using DryIoc;
using Splat;

namespace ChilliSource.Mobile.Ioc.DryIoc
{
    public class DryIocDependencyResolver : IMutableDependencyResolver
    {
        class EmptyDisposable : IDisposable
        {
            public void Dispose()
            {
                
            }
        }
        private readonly IContainer _container;

        public DryIocDependencyResolver(IContainer container)
        {
            _container = container;
        }

        public void Dispose()
        {
            _container.Dispose();
        }

        public object GetService(Type serviceType, string contract = null)
        {
            return string.IsNullOrEmpty(contract)
                    ? _container.Resolve(serviceType)
                    : _container.Resolve(serviceType, contract);
        }

        public IEnumerable<object> GetServices(Type serviceType, string contract = null)
        {
            return _container.ResolveMany(serviceType, serviceKey: contract);
        }

        public void Register(System.Func<object> factory, Type serviceType, string contract = null)
        {
            _container.RegisterDelegate(serviceType, r => factory(), serviceKey: contract);
        }

      
        public IDisposable ServiceRegistrationCallback(Type serviceType, string contract, System.Action<IDisposable> callback)
        {
            return new EmptyDisposable();
        }

    }
}
