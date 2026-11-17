using System.Windows;
using Microsoft.Win32;

namespace KeyNest.UI.Services;

/// <summary>读取系统深浅色与高对比度设置（只读注册表，不写入）。</summary>
internal static class SystemThemeReader
{
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    public static bool IsDark()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey, writable: false);
            if (key?.GetValue("AppsUseLightTheme") is int value)
            {
                return value == 0;
            }
        }
        catch
        {
            // 读取失败按浅色处理。
        }

        return false;
    }

    public static bool IsHighContrast => SystemParameters.HighContrast;
}
