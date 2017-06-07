using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DryIoc;
using Splat;
using Xunit;

namespace ChilliSource.Mobile.Ioc.DryIoc.Tests
{
    public interface ITestInterface
    {
        
    }

    public class ImpTestClassObject : ITestInterface
    {
        
    }
    public class ParentObject
    {
        private readonly ITestInterface _tester;

        public ParentObject(ITestInterface tester)
        {
            _tester = tester;
        }

        public ITestInterface Tester => _tester;
    }

    public class ParentObject2
    {
        private readonly Func<ITestInterface> _factory;

        public ParentObject2(Func<ITestInterface> factory)
        {
            _factory = factory;
        }

        public ITestInterface Tester => _factory();
    }

    public class ChildObject
    {
        
    }

    public class DryIocDependencyResolverTests
    {
        [Fact]
        public void ShouldBeAbleToResolve()
        {
            var container = new Container();
            container.Register<ITestInterface, ImpTestClassObject>();
            container.Register<ParentObject, ParentObject>();
            container.Register<ChildObject, ChildObject>();
            container.UseDryIocDependencyResolver();

            var parent = Locator.Current.GetService<ParentObject>();

            Assert.NotNull(parent);
            Assert.NotNull(parent.Tester);
        }

        [Fact]
        public void ShouldBeAbleToRegister()
        {
            var container = new Container();
            container.UseDryIocDependencyResolver();
            Locator.CurrentMutable.Register(() => new ChildObject(), typeof(ChildObject));
            var child = Locator.Current.GetService<ChildObject>();
            Assert.NotNull(child);
        }

        [Fact]
        public void ShouldBeAbleToResolveFactory()
        {
            var container = new Container();
            container.Register<ITestInterface, ImpTestClassObject>();
            container.Register<ParentObject2, ParentObject2>();
            container.Register<ChildObject, ChildObject>();
            container.UseDryIocDependencyResolver();

            var parent = Locator.Current.GetService<ParentObject2>();

            Assert.NotNull(parent);
            Assert.NotNull(parent.Tester);
        }


    }

}
