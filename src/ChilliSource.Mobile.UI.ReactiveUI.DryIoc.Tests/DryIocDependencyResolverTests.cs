using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChilliSource.Mobile.Ioc.DryIoc;
using DryIoc;
using ReactiveUI;
using Xunit;
using Splat;

namespace ChilliSource.Mobile.UI.ReactiveUI.DryIoc.Tests
{
    public class DryIocDependencyResolverTests
    {
        [Fact]
        public void CanRegisterViews()
        {
            var container = new Container();
            var assembly = typeof(HomePageViewModel).Assembly;
            container.RegisterViews(assembly);
            container.RegisterViewModels(assembly);
            container.UseDryIocDependencyResolver();

            var view = Locator.Current.GetService<IViewFor<HomePageViewModel>>();
            var vm = Locator.Current.GetService<HomePageViewModel>();
            Assert.NotNull(view);
            Assert.NotNull(vm);
        }

        [Fact]
        public void CanRegisterViewModels()
        {
            var container = new Container();
            var assembly = typeof(HomePageViewModel).Assembly;
            container.RegisterViews(assembly);
            container.RegisterViewModels(assembly);
            container.UseDryIocDependencyResolver();
            var view = Locator.Current.GetService<IViewFor<SettingsPageViewModel>>();
            var vm = Locator.Current.GetService<SettingsPageViewModel>();
            Assert.NotNull(view);
            Assert.NotNull(vm);
            Assert.NotNull(vm.ViewModel);
        }

        [Fact]
        public void ShouldNotRegisterObjectNotEndWithViewModels()
        {
            var container = new Container();
            var assembly = typeof(HomePageViewModel).Assembly;
            container.RegisterForReactiveUI(assembly);
            container.UseDryIocDependencyResolver();
            Assert.ThrowsAny<Exception>(() => Locator.Current.GetService<HelloThisIsATest>());
            Assert.ThrowsAny<Exception>(() => Locator.Current.GetService<ViewModelHelloThisIsATest>());
        }


        [Fact]
        public void ShouldNotRegisterIScreenServiceTypWhenViewModelImplementsIScreen()
        {
            var container = new Container();
            var assembly = typeof(HomePageViewModel).Assembly;
            container.RegisterViewModels(assembly);
            container.UseDryIocDependencyResolver();
            Assert.ThrowsAny<Exception>(() => Locator.Current.GetService<IScreen>());
        }

        [Fact]
        public void ShouldNotRegisterViewModelIfItContainsInExcludeViewModelList()
        {
            var container = new Container();
            var assembly = typeof(HomePageViewModel).Assembly;
            container.RegisterViewModels(assembly, new List<Type>() { typeof(HomePageViewModel)});
            container.UseDryIocDependencyResolver();
            Assert.ThrowsAny<Exception>(() => Locator.Current.GetService<HomePageViewModel>());
        }

    }
}
