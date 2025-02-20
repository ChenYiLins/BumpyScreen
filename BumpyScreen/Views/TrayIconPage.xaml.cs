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

        Loaded += TrayIconPage_Loaded;
    }

    private void TrayIconPage_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ContextFlyout.ShowAt(this);
    }
}
