using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using KeyNest.Core.Models;
using KeyNest.Core.Services;
using KeyNest.UI.Services;
using Microsoft.Win32;

namespace KeyNest.Installer.Services;

public readonly record struct InstallProgress(int Percent, string Message);

/// <summary>
/// 安装执行器：解压程序文件、创建快捷方式、写入卸载信息、按需写入开机自启。
/// 只在本机操作文件与注册表，不联网。
/// </summary>
public sealed class InstallService
{
    public async Task InstallAsync(InstallerContext context, IProgress<InstallProgress>? progress, CancellationToken cancellationToken)
    {
        var target = Path.GetFullPath(context.TargetDirectory);
        var appExe = Path.Combine(target, InstallerPaths.AppExeName);
        var uninstaller = Path.Combine(target, InstallerPaths.UninstallerExeName);

        progress?.Report(new InstallProgress(3, LocalizationService.Get("Installer.Progress.Validating")));


        var validation = InstallValidator.Validate(target, context.Scope);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(validation.Message);
        }

        if (!PayloadExtractor.PayloadExists)
        {
            throw new InvalidOperationException(LocalizationService.Get("Installer.Error.PayloadMissing"));
        }

        cancellationToken.ThrowIfCancellationRequested();

        progress?.Report(new InstallProgress(8, LocalizationService.Get("Installer.Progress.CreatingDirectory")));
        Directory.CreateDirectory(target);

        progress?.Report(new InstallProgress(16, LocalizationService.Get("Installer.Progress.Extracting")));
        await PayloadExtractor.ExtractAsync(
            target,
            percent => progress?.Report(new InstallProgress(
                16 + (int)(percent * 0.46),
                LocalizationService.Format("Installer.Progress.ExtractingFormat", percent))),
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        progress?.Report(new InstallProgress(66, LocalizationService.Get("Installer.Progress.Shortcuts")));
        CreateShortcuts(context, target, appExe, uninstaller);

        progress?.Report(new InstallProgress(80, LocalizationService.Get("Installer.Progress.UninstallEntry")));
        WriteUninstallEntry(context, target, appExe, uninstaller);

        progress?.Report(new InstallProgress(92, LocalizationService.Get("Installer.Progress.AutoStart")));
        ApplyAutoStart(context, appExe);

        EnsureUserLanguage(context.Language);

        progress?.Report(new InstallProgress(100, LocalizationService.Get("Installer.Progress.Completed")));
    }

    private static void CreateShortcuts(InstallerContext context, string target, string appExe, string uninstaller)
    {
        if (context.CreateDesktopShortcut)
        {
            var desktop = InstallerPaths.DesktopShortcutPath(context.Scope);
            Directory.CreateDirectory(Path.GetDirectoryName(desktop)!);
            ShellLink.Create(desktop, appExe, target, InstallerPaths.ProductShortName);
        }

        if (context.CreateStartMenuShortcut)
        {
            var folder = InstallerPaths.StartMenuFolder(context.Scope);
            Directory.CreateDirectory(folder);

            ShellLink.Create(
                Path.Combine(folder, InstallerPaths.ProductShortName + ".lnk"),
                appExe,
                target,
                InstallerPaths.ProductShortName);

            ShellLink.Create(
                Path.Combine(folder, LocalizationService.Get("Installer.Shortcut.Uninstall") + ".lnk"),
                uninstaller,
                target,
                LocalizationService.Get("Installer.Shortcut.Uninstall"),
                iconPath: appExe);
        }
    }

    private static void WriteUninstallEntry(InstallerContext context, string target, string appExe, string uninstaller)
    {
        var hive = context.Scope == InstallScope.PerMachine ? Registry.LocalMachine : Registry.CurrentUser;

        using var key = hive.CreateSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Uninstall\" + InstallerPaths.UninstallKeyName,
            writable: true)
            ?? throw new InvalidOperationException(LocalizationService.Get("Installer.Error.UninstallWrite"));

        key.SetValue("DisplayName", InstallerPaths.ProductDisplayName, RegistryValueKind.String);
        key.SetValue("DisplayVersion", InstallerPaths.ProductVersion, RegistryValueKind.String);
        key.SetValue("Publisher", InstallerPaths.Publisher, RegistryValueKind.String);
        key.SetValue("InstallLocation", target, RegistryValueKind.String);
        key.SetValue("DisplayIcon", appExe + ",0", RegistryValueKind.String);
        key.SetValue("UninstallString", $"\"{uninstaller}\"", RegistryValueKind.String);
        key.SetValue("QuietUninstallString", $"\"{uninstaller}\" --silent", RegistryValueKind.String);
        key.SetValue("InstallSource", Path.GetDirectoryName(Environment.ProcessPath) ?? target, RegistryValueKind.String);
        key.SetValue("NoModify", 1, RegistryValueKind.DWord);
        key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
        key.SetValue("EstimatedSize", EstimateSizeInKb(target), RegistryValueKind.DWord);
    }

    private static void ApplyAutoStart(InstallerContext context, string appExe)
    {
        using var runKey = Registry.CurrentUser.CreateSubKey(InstallerPaths.RunKeyPath, writable: true);
        if (runKey is null)
        {
            throw new InvalidOperationException(LocalizationService.Get("Installer.Error.AutoStartWrite"));
        }

        if (context.EnableAutoStart)
        {
            runKey.SetValue(InstallerPaths.RunValueName, $"\"{appExe}\" --tray", RegistryValueKind.String);
        }
        else
        {
            runKey.DeleteValue(InstallerPaths.RunValueName, throwOnMissingValue: false);
        }
    }

    /// <summary>
    /// 首次安装时把安装语言写入用户配置，使键巢首次启动即为对应语言。
    /// 已存在配置时保留用户原有设置，不覆盖语言。
    /// </summary>
    private static void EnsureUserLanguage(AppLanguage language)
    {
        try
        {
            AppPaths.EnsureDataDirectory();

            if (File.Exists(AppPaths.ConfigFile) || File.Exists(AppPaths.LegacySettingsFile))
            {
                return;
            }

            var settings = new AppSettings { Language = language };
            File.WriteAllText(AppPaths.ConfigFile, JsonStore.Serialize(settings));
        }
        catch
        {
            // 配置写入失败不影响安装结果，键巢启动后会使用默认中文。
        }
    }

    private static int EstimateSizeInKb(string directory)
    {
        try
        {
            long total = 0;
            foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
            {
                total += new FileInfo(file).Length;
            }

            var kb = total / 1024;
            return kb > int.MaxValue ? int.MaxValue : (int)kb;
        }
        catch
        {
            return 0;
        }
    }
}
