using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace KeyNest.UI.Interop;

/// <summary>
/// 自研窗口辅助：不抢焦点工具窗样式、Win11 圆角与深色标题栏。
/// 全部使用 Windows 公开 API，不依赖任何第三方组件。
/// </summary>
public static class WindowEffects
{
    private const int GwlExStyle = -20;
    private const int WsExToolWindow = 0x00000080;
    private const int WsExNoActivate = 0x08000000;

    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaWindowCornerPreference = 33;
    private const int DwmcpRound = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out NativePoint point);

    /// <summary>让悬浮窗不出现在 Alt+Tab，且点击时不抢占前台焦点。</summary>
    public static void MakeNoActivateToolWindow(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        var style = GetWindowLong(hwnd, GwlExStyle);
        SetWindowLong(hwnd, GwlExStyle, style | WsExToolWindow | WsExNoActivate);
    }

    /// <summary>Win11 原生圆角；Win10 上调用会返回失败并被忽略（由 XAML 自绘圆角兜底）。</summary>
    public static void TryEnableRoundedCorners(Window window)
    {
        var hwnd = GetHandle(window);
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        var preference = DwmcpRound;
        _ = DwmSetWindowAttribute(hwnd, DwmwaWindowCornerPreference, ref preference, sizeof(int));
    }

    /// <summary>同步原生标题栏深浅色（仅用于系统标题栏窗口）。</summary>
    public static void TrySetDarkTitleBar(Window window, bool dark)
    {
        var hwnd = GetHandle(window);
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        var value = dark ? 1 : 0;
        _ = DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkMode, ref value, sizeof(int));
    }

    /// <summary>取当前鼠标屏幕坐标（物理像素）。</summary>
    public static bool TryGetCursorPosition(out Point position)
    {
        if (GetCursorPos(out var native))
        {
            position = new Point(native.X, native.Y);
            return true;
        }

        position = default;
        return false;
    }

    private static IntPtr GetHandle(Window window)
    {
        if (!window.IsLoaded && PresentationSource.FromVisual(window) is null)
        {
            return IntPtr.Zero;
        }

        return new WindowInteropHelper(window).Handle;
    }
}
