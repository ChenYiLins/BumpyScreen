using BumpyScreen.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace BumpyScreen.Views;

public sealed partial class TrayIconPage : Page
{
    public TrayIconViewModel ViewModel
    {
        get;
    }

    public TrayIconPage()
    {
        ViewModel = App.GetService<TrayIconViewModel>();
        InitializeComponent();
    }
}
