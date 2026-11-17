using System;
using System.IO;

namespace KeyNest.Installer.Services;

public enum InstallScope
{
    PerUser = 0,
    PerMachine = 1
}

/// <summary>安装器使用的固定名称与路径。</summary>
public static class InstallerPaths
{
    public const string ProductDisplayName = "键巢（KeyNest）";
    public const string ProductShortName = "键巢 KeyNest";
    public const string ProductVersion = "1.0.0";
    public const string Publisher = "KeyNest";

    public const string AppExeName = "KeyNest.App.exe";
    public const string UninstallerExeName = "KeyNest.Uninstaller.exe";

    public const string UninstallKeyName = "KeyNest";
    public const string RunValueName = "KeyNest";
    public const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public static string DefaultPerUserDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs",
            "KeyNest");

    public static string DefaultPerMachineDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "KeyNest");

    public static string GetDefaultDirectory(InstallScope scope) =>
        scope == InstallScope.PerMachine ? DefaultPerMachineDirectory : DefaultPerUserDirectory;

    /// <summary>用户配置目录，卸载时可选保留。</summary>
    public static string UserDataDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "KeyNest");

    public static string DesktopShortcutPath(InstallScope scope) =>
        Path.Combine(
            scope == InstallScope.PerMachine
                ? Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory)
                : Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            ProductShortName + ".lnk");

    public static string StartMenuFolder(InstallScope scope) =>
        Path.Combine(
            scope == InstallScope.PerMachine
                ? Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms)
                : Environment.GetFolderPath(Environment.SpecialFolder.Programs),
            ProductShortName);
}
