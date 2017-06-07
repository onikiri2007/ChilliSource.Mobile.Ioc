using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;

namespace ChilliSource.Mobile.UI.ReactiveUI.DryIoc.Tests
{

    public interface ITest
    {
        
    }

    public class HomePageViewModel : ReactiveObject, IScreen, ITest
    {
        public RoutingState Router { get; }
    }

    public class HomePage2ViewModel : ReactiveObject, IScreen
    {
        public RoutingState Router { get; }
    }

    public class SettingsPageViewModel : ReactiveObject
    {
        private readonly HomePageViewModel _vm;

        public SettingsPageViewModel(HomePageViewModel vm)
        {
            _vm = vm;
        }

        public HomePageViewModel ViewModel => _vm;
    }

    public class HelloThisIsATest : ReactiveObject
    {
        
    }

    public class ViewModelHelloThisIsATest : ReactiveObject
    {

    }
}
