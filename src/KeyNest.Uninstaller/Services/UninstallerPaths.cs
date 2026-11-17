using System;
using System.IO;

namespace KeyNest.Uninstaller.Services;

internal static class UninstallerPaths
{
    public const string ProductDisplayName = "键巢（KeyNest）";
    public const string ProductShortName = "键巢 KeyNest";
    public const string AppExeName = "KeyNest.App.exe";

    public const string RunValueName = "KeyNest";
    public const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    public const string UninstallKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\KeyNest";

    public static string InstallDirectory => AppContext.BaseDirectory;

    public static string UserDataDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "KeyNest");

    public static string DesktopShortcut(bool allUsers) =>
        Path.Combine(
            allUsers
                ? Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory)
                : Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            ProductShortName + ".lnk");

    public static string StartMenuFolder(bool allUsers) =>
        Path.Combine(
            allUsers
                ? Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms)
                : Environment.GetFolderPath(Environment.SpecialFolder.Programs),
            ProductShortName);
}
