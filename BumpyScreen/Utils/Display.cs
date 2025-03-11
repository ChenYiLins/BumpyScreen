using System.Runtime.InteropServices;
using Windows.Devices.Display.Core;
using Windows.Win32;
using Windows.Win32.Graphics.Gdi;

namespace BumpyScreen.Utils;

public class Display
{
    public enum DisplayRotation
    {
        Rotate0,
        Rotate90,
        Rotate180,
        Rotate270
    }

    public static unsafe bool Rotate(uint displayNumber, DisplayRotation rotation)
    {
        var result = false;

        if (displayNumber == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(displayNumber), "First display is 1");
        }

        // 获取显示器设备信息
        var displayDevice = new DISPLAY_DEVICEW
        {
            cb = (uint)Marshal.SizeOf<DISPLAY_DEVICEW>()
        };

        // 枚举显示器设备
        if (!PInvoke.EnumDisplayDevices(null, displayNumber - 1, ref displayDevice, 0))
        {
            throw new ArgumentOutOfRangeException(nameof(displayNumber), "Number is greater than connected displays");
        }

        // 获取当前显示模式
        var devMode = new DEVMODEW
        {
            dmSize = (ushort)Marshal.SizeOf<DEVMODEW>()
        };

        if (0 != PInvoke.EnumDisplaySettings(displayDevice.DeviceName.ToString(), ENUM_DISPLAY_SETTINGS_MODE.ENUM_CURRENT_SETTINGS, ref devMode))
        {
            // 计算是否需要交换宽高
            if (((int)devMode.Anonymous1.Anonymous2.dmDisplayOrientation + (int)rotation) % 2 == 1)
            {
                (devMode.dmPelsHeight, devMode.dmPelsWidth) = (devMode.dmPelsWidth, devMode.dmPelsHeight);
            }

            // 设置旋转方向
            devMode.Anonymous1.Anonymous2.dmDisplayOrientation = rotation switch
            {
                DisplayRotation.Rotate90 => DEVMODE_DISPLAY_ORIENTATION.DMDO_270,
                DisplayRotation.Rotate180 => DEVMODE_DISPLAY_ORIENTATION.DMDO_180,
                DisplayRotation.Rotate270 => DEVMODE_DISPLAY_ORIENTATION.DMDO_90,
                DisplayRotation.Rotate0 => DEVMODE_DISPLAY_ORIENTATION.DMDO_DEFAULT,
                _ => throw new ArgumentException("无效的旋转参数")
            };

            // 标记要修改的字段
            //devMode.dmFields |= DEVMODE_FIELD_FLAGS.DM_DISPLAYORIENTATION;

            // 应用显示设置
            var ret = PInvoke.ChangeDisplaySettingsEx(displayDevice.DeviceName.ToString(), devMode, CDS_TYPE.CDS_UPDATEREGISTRY, null);
            result = ret == 0;
        }

        return result;
    }

    public static DisplayRotation GetDisplayOrientation(uint displayNumber)
    {
        // 获取显示器设备信息
        var displayDevice = new DISPLAY_DEVICEW
        {
            cb = (uint)Marshal.SizeOf<DISPLAY_DEVICEW>()
        };

        // 枚举显示器设备
        if (!PInvoke.EnumDisplayDevices(null, displayNumber - 1, ref displayDevice, 0))
        {
            throw new ArgumentOutOfRangeException(nameof(displayNumber), "Number is greater than connected displays");
        }

        // 获取当前显示模式
        var devMode = new DEVMODEW
        {
            dmSize = (ushort)Marshal.SizeOf<DEVMODEW>()
        };

        if (0 != PInvoke.EnumDisplaySettings(displayDevice.DeviceName.ToString(), ENUM_DISPLAY_SETTINGS_MODE.ENUM_CURRENT_SETTINGS, ref devMode))
        {
            return devMode.Anonymous1.Anonymous2.dmDisplayOrientation switch
            {
                DEVMODE_DISPLAY_ORIENTATION.DMDO_270 => DisplayRotation.Rotate90,
                DEVMODE_DISPLAY_ORIENTATION.DMDO_180 => DisplayRotation.Rotate180,
                DEVMODE_DISPLAY_ORIENTATION.DMDO_90 => DisplayRotation.Rotate270,
                DEVMODE_DISPLAY_ORIENTATION.DMDO_DEFAULT => DisplayRotation.Rotate0,
                _ => DisplayRotation.Rotate0,
            };
        }
        else
        {
            return DisplayRotation.Rotate0;
        }
    }

    public static void ResetAllRotations()
    {
        uint displayIndex = 0;
        while (true)
        {
            var displayDevice = new DISPLAY_DEVICEW
            {
                cb = (uint)Marshal.SizeOf<DISPLAY_DEVICEW>()
            };

            if (!PInvoke.EnumDisplayDevices(
                lpDevice: null,
                iDevNum: displayIndex,
                lpDisplayDevice: ref displayDevice,
                dwFlags: 0))
            {
                break; // 无更多显示器
            }

            try
            {
                Rotate(displayIndex + 1, DisplayRotation.Rotate0);
            }
            catch (ArgumentException)
            {
                // 跳过无效显示器
            }

            displayIndex++;
        }
    }
}