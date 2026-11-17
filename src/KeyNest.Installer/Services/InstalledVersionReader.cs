using System;
using Microsoft.Win32;

namespace KeyNest.Installer.Services;

/// <summary>读取已安装版本（只读注册表）。</summary>
public static class InstalledVersionReader
{
    private const string UninstallSubKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\KeyNest";

    public static string? GetInstalledVersion()
    {
        return ReadVersion(Registry.CurrentUser) ?? ReadVersion(Registry.LocalMachine);
    }

    private static string? ReadVersion(RegistryKey hive)
    {
        try
        {
            using var key = hive.OpenSubKey(UninstallSubKey, writable: false);
            return key?.GetValue("DisplayVersion") as string;
        }
        catch
        {
            return null;
        }
    }
}
