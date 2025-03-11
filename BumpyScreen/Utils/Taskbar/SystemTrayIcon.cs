using System.Drawing;
using System.Runtime.InteropServices;
using BumpyScreen.Views;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;
using WinRT;

namespace BumpyScreen.Utils.Taskbar;

// The code about this part, I try to creat TrayIcon just like the Files(https://github.com/files-community/Files)
// And I found the way to display a modern Flyout from WinUI3, this way is come from MicaForEveryone(https://github.com/MicaForEveryone/MicaForEveryone)
// Although the ideas of the two are the same, but I found some bugs such as the mouse is always in a "busy" state after click the TrayIcon, maybe the message was not processed normally
public partial class SystemTrayIcon : IDisposable
{
    // Constants
    private const uint WM_UNIQUE_MESSAGE = 2048u;

    // Fields
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue;

    private static readonly string _pathApp = AppDomain.CurrentDomain.BaseDirectory;

    private DesktopWindowXamlSource? _source;

    private static readonly Guid _trayIconGuid = new("CDD3158B-5EED-45C7-A8E4-848F5D12B0F2");

    private readonly SystemTrayIconWindow _IconWindow;

    private readonly uint _taskbarRestartMessageId;

    private bool _notifyIconCreated;

    public enum DWM_WINDOW_CORNER_PREFERENCE
    {
        DWMWCP_DEFAULT = 0,
        DWMWCP_DONOTROUND = 1,
        DWMWCP_ROUND = 2,
        DWMWCP_ROUNDSMALL = 3
    }

    // Properties

    public Guid Id
    {
        get; private set;
    }

    private bool _IsVisible;
    public bool IsVisible
    {
        get => _IsVisible;
        private set
        {
            if (_IsVisible != value)
            {
                _IsVisible = value;

                if (!value)
                    DeleteNotifyIcon();
                else
                    CreateOrModifyNotifyIcon();
            }
        }
    }

    private string _Tooltip;
    public string Tooltip
    {
        get => _Tooltip;
        set
        {
            if (_Tooltip != value)
            {
                _Tooltip = value;

                CreateOrModifyNotifyIcon();
            }
        }
    }

    private System.Drawing.Icon _Icon;
    public System.Drawing.Icon Icon
    {
        get => _Icon;
        set
        {
            if (_Icon != value)
            {
                _Icon = value;

                CreateOrModifyNotifyIcon();
            }
        }
    }

    // Constructor

    public SystemTrayIcon()
    {
        _Icon = new System.Drawing.Icon(_pathApp + @"Assets\WindowIcon.ico");
        _Tooltip = "BumpyScreen";
        _taskbarRestartMessageId = PInvoke.RegisterWindowMessage("TaskbarCreated");

        Id = _trayIconGuid;
        _IconWindow = new SystemTrayIconWindow(this);

        _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        CreateOrModifyNotifyIcon();
    }

    // Public Methods

    public SystemTrayIcon Show()
    {
        IsVisible = true;

        return this;
    }

    public SystemTrayIcon Hide()
    {
        IsVisible = false;

        return this;
    }

    // Private Methods

    private void CreateOrModifyNotifyIcon()
    {
        if (IsVisible)
        {
            NOTIFYICONDATAW lpData = default;

            lpData.cbSize = (uint)Marshal.SizeOf(typeof(NOTIFYICONDATAW));
            lpData.hWnd = _IconWindow.WindowHandle;
            lpData.uCallbackMessage = WM_UNIQUE_MESSAGE;
            lpData.hIcon = (Icon != null) ? new HICON(Icon.Handle) : default;
            lpData.guidItem = Id;
            lpData.uFlags = NOTIFY_ICON_DATA_FLAGS.NIF_MESSAGE | NOTIFY_ICON_DATA_FLAGS.NIF_ICON | NOTIFY_ICON_DATA_FLAGS.NIF_TIP | NOTIFY_ICON_DATA_FLAGS.NIF_GUID | NOTIFY_ICON_DATA_FLAGS.NIF_SHOWTIP;
            lpData.szTip = _Tooltip ?? string.Empty;

            if (!_notifyIconCreated)
            {
                // Delete the existing icon
                PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_DELETE, in lpData);

                _notifyIconCreated = true;

                // Add a new icon
                PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_ADD, in lpData);

                lpData.Anonymous.uVersion = 4u;

                // Set the icon handler version
                // NOTE: Do not omit this code. If you remove, the icon won't be shown.
                PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_SETVERSION, in lpData);
            }
            else
            {
                // Modify the existing icon
                PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_MODIFY, in lpData);
            }
        }
    }

    private void DeleteNotifyIcon()
    {
        if (_notifyIconCreated)
        {
            _notifyIconCreated = false;

            NOTIFYICONDATAW lpData = default;

            lpData.cbSize = (uint)Marshal.SizeOf(typeof(NOTIFYICONDATAW));
            lpData.hWnd = _IconWindow.WindowHandle;
            lpData.guidItem = Id;
            lpData.uFlags = NOTIFY_ICON_DATA_FLAGS.NIF_MESSAGE | NOTIFY_ICON_DATA_FLAGS.NIF_ICON | NOTIFY_ICON_DATA_FLAGS.NIF_TIP | NOTIFY_ICON_DATA_FLAGS.NIF_GUID | NOTIFY_ICON_DATA_FLAGS.NIF_SHOWTIP;

            // Delete the existing icon
            PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_DELETE, in lpData);
        }
    }


    internal LRESULT WindowProc(HWND hWnd, uint uMsg, WPARAM wParam, LPARAM lParam)
    {
        switch (uMsg)
        {
            case WM_UNIQUE_MESSAGE:
                {
                    if ((uint)(lParam.Value & 0xFFFF) == PInvoke.WM_RBUTTONUP)
                    {
                        _dispatcherQueue.TryEnqueue(() =>
                        {
                            PInvoke.SetForegroundWindow(hWnd);
                            Point mousePos;
                            PInvoke.GetCursorPos(out mousePos);
                            var scaleFactor = PInvoke.GetDpiForWindow(hWnd) / 96f;

                            var page = (TrayIconPage)(_source!.Content);
                            page.ContextFlyout.As<MenuFlyout>().ShowAt(page, new Windows.Foundation.Point((mousePos.X + 10) / scaleFactor, (mousePos.Y - 10) / scaleFactor));
                        });
                    }

                    break;
                }
            case PInvoke.WM_DESTROY:
                {
                    DeleteNotifyIcon();

                    break;
                }
            case PInvoke.WM_CREATE:
                {
                    // Bind Xaml to Win32 Windows
                    _source = new();
                    var thing = Win32Interop.GetWindowIdFromWindow(hWnd);
                    _source.Initialize(thing);
                    _source.Content = new TrayIconPage();

                    // Indicates that the XAML content will automatically adjust as the parent window is resized, at the same time display Xaml
                    _source.SiteBridge.ResizePolicy = Microsoft.UI.Content.ContentSizePolicy.ResizeContentToParentWindow;
                    _source.SiteBridge.Show();

                    break;
                }
            default:
                {
                    if (uMsg == _taskbarRestartMessageId)
                    {
                        DeleteNotifyIcon();
                        CreateOrModifyNotifyIcon();
                    }

                    return PInvoke.DefWindowProc(hWnd, uMsg, wParam, lParam);
                }
        }
        return default;
    }

    public void Dispose()
    {
        _IconWindow.Dispose();
    }
}