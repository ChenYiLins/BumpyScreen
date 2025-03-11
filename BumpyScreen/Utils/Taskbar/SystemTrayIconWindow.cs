using System.ComponentModel;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace BumpyScreen.Utils.Taskbar;

public class SystemTrayIconWindow : IDisposable
{
    private SystemTrayIcon? _trayIcon;

    private readonly WNDPROC _windowProcedure;

    private HWND _windowHandle;
    internal HWND WindowHandle
        => _windowHandle;

    public unsafe SystemTrayIconWindow(SystemTrayIcon icon)
    {
        _windowProcedure = WindowProc;
        _trayIcon = icon;
        var text = "BumpyScreenTrayIcon_" + _trayIcon.Id;

        fixed (char* ptr = text)
        {
            WNDCLASSEXW param = new()
            {
                cbSize = (uint)Marshal.SizeOf(typeof(WNDCLASSEXW)),
                style = WNDCLASS_STYLES.CS_DBLCLKS,
                lpfnWndProc = _windowProcedure,
                cbClsExtra = 0,
                cbWndExtra = 0,
                hInstance = PInvoke.GetModuleHandle(default(PCWSTR)),
                lpszClassName = ptr
            };

            PInvoke.RegisterClassEx(in param);
        }

        _windowHandle = PInvoke.CreateWindowEx(
            WINDOW_EX_STYLE.WS_EX_NOACTIVATE | WINDOW_EX_STYLE.WS_EX_TOPMOST | WINDOW_EX_STYLE.WS_EX_TOOLWINDOW,
            text,
            string.Empty,
            WINDOW_STYLE.WS_POPUPWINDOW,
            0,
            0,
            0,
            0,
            default,
            null,
            null,
            null);

        var rgn = PInvoke.CreateRectRgn(0, 0, 0, 0);
        PInvoke.SetWindowRgn(_windowHandle, rgn, false);
        PInvoke.ShowWindow(_windowHandle, SHOW_WINDOW_CMD.SW_SHOW);

        if (_windowHandle == default) throw new Win32Exception("ERR: Message window handle was not a valid pointer.");
    }

    private LRESULT WindowProc(HWND hWnd, uint uMsg, WPARAM wParam, LPARAM lParam)
    {
        if (_trayIcon != null)
        {
            return _trayIcon.WindowProc(hWnd, uMsg, wParam, lParam);
        }
        else
        {
            return (LRESULT)0;
        }
    }

    public void Dispose()
    {
        if (_windowHandle != default)
        {
            PInvoke.DestroyWindow(_windowHandle);
            _windowHandle = default;
        }

        _trayIcon = null;
    }
}