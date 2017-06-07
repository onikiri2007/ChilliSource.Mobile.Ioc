using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;

namespace ChilliSource.Mobile.UI.ReactiveUI.DryIoc.Tests
{
    public class HomePage : IViewFor<HomePageViewModel>
    {
        
        public HomePage(HomePageViewModel viewmodel)
        {
            ViewModel = viewmodel;
        }
        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = value as HomePageViewModel; }
        }
        public HomePageViewModel ViewModel { get; set; }
    }

    public class SettingsPage : IViewFor<SettingsPageViewModel>
    {

        public SettingsPage(SettingsPageViewModel viewmodel)
        {
            ViewModel = viewmodel;
        }
        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = value as SettingsPageViewModel; }
        }
        public SettingsPageViewModel ViewModel { get; set; }
    }
}
