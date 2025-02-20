using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using BumpyScreen.Views;
using Windows.Win32;

namespace BumpyScreen.Taskbar;

public class TrayFlyoutContextMenu
{
    public WindowEx ContextMenuWindow = new()
    {
        IsAlwaysOnTop = true,
        IsShownInSwitchers = false,
        ExtendsContentIntoTitleBar = true,
        IsResizable = false,
    };

    public TrayFlyoutContextMenu()
    {
        ContextMenuWindow.SetForegroundWindow();
        ContextMenuWindow.SetWindowOpacity(0);
        ContextMenuWindow.Activated += ContextMenuWindow_Activated;
        
        var rootFrame = new Frame();
        ContextMenuWindow.Content = rootFrame;
        rootFrame.Navigate(typeof(TrayIconPage), string.Empty);
    }

    private void ContextMenuWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.Deactivated) ContextMenuWindow.Close();
    }
}