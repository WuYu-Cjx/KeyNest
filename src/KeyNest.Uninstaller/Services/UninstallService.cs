using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Win32;
using KeyNest.Core.Services;
using KeyNest.UI.Services;

namespace KeyNest.Uninstaller.Services;

/// <summary>
/// 卸载执行器：结束进程、删除快捷方式、移除开机自启与卸载信息、按需保留用户配置，
/// 最后安排删除安装目录（含自身）。
/// </summary>
public sealed class UninstallService
{
    public void Run(bool keepUserData, IProgress<string>? progress)
    {
        var installDirectory = Path.GetFullPath(UninstallerPaths.InstallDirectory);

        progress?.Report(LocalizationService.Get("Uninstaller.Progress.Stopping"));
        StopRunningApp(installDirectory);

        progress?.Report(LocalizationService.Get("Uninstaller.Progress.Shortcuts"));
        DeleteShortcuts();

        progress?.Report(LocalizationService.Get("Uninstaller.Progress.AutoStart"));
        DeleteRunValue();

        progress?.Report(LocalizationService.Get("Uninstaller.Progress.UninstallInfo"));
        DeleteUninstallKeys();

        if (!keepUserData)
        {
            progress?.Report(LocalizationService.Get("Uninstaller.Progress.Config"));
            TryDeleteDirectory(UninstallerPaths.UserDataDirectory);
        }

        progress?.Report(LocalizationService.Get("Uninstaller.Progress.Files"));
        DeleteProgramFiles(installDirectory);

        progress?.Report(LocalizationService.Get("Uninstaller.Progress.Done"));
    }

    /// <summary>在卸载窗口关闭后调用，延迟删除安装目录（含卸载器自身）。</summary>
    public static void ScheduleSelfDestruct(string installDirectory)
    {
        try
        {
            // 多次重试删除，避免卸载器自身或系统释放句柄稍慢导致目录残留。
            var arguments =
                $"/c for /l %i in (1,1,15) do @(ping -n 2 127.0.0.1 >nul & rmdir /s /q \"{installDirectory}\" 2>nul & if not exist \"{installDirectory}\" exit /b)";

            Process.Start(new ProcessStartInfo("cmd.exe", arguments)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            });
        }
        catch
        {
            // 自删除失败时保留安装目录，用户可手动删除。
        }
    }

    private static void StopRunningApp(string installDirectory)
    {
        var root = installDirectory.TrimEnd(Path.DirectorySeparatorChar);

        // 先请求正常退出，让托盘图标、键盘钩子与配置写入都有机会释放；
        // 未响应时再按进程名强制结束。
        if (AppExitSignal.RequestExit())
        {
            Thread.Sleep(1500);
        }

        // 托盘程序没有主窗口，CloseMainWindow 可能只关掉设置窗口而进程仍驻留，
        // 因此必须在等待超时后强制结束，并多轮确认进程确实已退出。
        for (var round = 0; round < 3; round++)
        {
            var found = false;

            foreach (var name in new[] { "KeyNest.App", "KeyNest" })
            {
                Process[] processes;

                try
                {
                    processes = Process.GetProcessesByName(name);
                }
                catch
                {
                    continue;
                }

                foreach (var process in processes)
                {
                    using (process)
                    {
                        try
                        {
                            if (process.HasExited)
                            {
                                continue;
                            }

                            var path = TryGetProcessPath(process);
                            if (!string.IsNullOrEmpty(path)
                                && !path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            found = true;
                            process.CloseMainWindow();

                            if (!process.WaitForExit(1200))
                            {
                                Terminate(process);
                            }
                        }
                        catch
                        {
                            // 路径读取或结束进程失败时，兜底强制结束。
                            Terminate(process);
                        }
                    }
                }
            }

            if (!found)
            {
                return;
            }

            Thread.Sleep(250);
        }
    }

    private static string? TryGetProcessPath(Process process)
    {
        try
        {
            return process.MainModule?.FileName;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>先尝试 .NET 结束进程，仍存活时再用 taskkill 兜底。</summary>
    private static void Terminate(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(3000);
            }
        }
        catch
        {
            // 忽略，继续尝试 taskkill。
        }

        if (HasExited(process))
        {
            return;
        }

        try
        {
            using var taskkill = Process.Start(new ProcessStartInfo("taskkill", $"/PID {process.Id} /F /T")
            {
                CreateNoWindow = true,
                UseShellExecute = false
            });

            taskkill?.WaitForExit(3000);
        }
        catch
        {
            // 两种方式都失败时保留进程，卸载流程继续。
        }
    }

    private static bool HasExited(Process process)
    {
        try
        {
            return process.HasExited;
        }
        catch
        {
            return true;
        }
    }

    private static void DeleteShortcuts()
    {
        foreach (var allUsers in new[] { false, true })
        {
            TryDeleteFile(UninstallerPaths.DesktopShortcut(allUsers));
            TryDeleteDirectory(UninstallerPaths.StartMenuFolder(allUsers));
        }
    }

    private static void DeleteRunValue()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(UninstallerPaths.RunKeyPath, writable: true);
            key?.DeleteValue(UninstallerPaths.RunValueName, throwOnMissingValue: false);
        }
        catch
        {
            // 忽略。
        }
    }

    private static void DeleteUninstallKeys()
    {
        TryDeleteUninstallKey(Registry.CurrentUser);
        TryDeleteUninstallKey(Registry.LocalMachine);
    }

    private static void TryDeleteUninstallKey(RegistryKey hive)
    {
        try
        {
            hive.DeleteSubKeyTree(UninstallerPaths.UninstallKeyPath, throwOnMissingSubKey: false);
        }
        catch
        {
            // 权限不足时忽略（例如非管理员卸载每机器安装）。
        }
    }

    private static void DeleteProgramFiles(string installDirectory)
    {
        try
        {
            var self = Environment.ProcessPath;

            foreach (var file in Directory.EnumerateFiles(installDirectory, "*", SearchOption.AllDirectories))
            {
                if (!string.IsNullOrEmpty(self)
                    && string.Equals(Path.GetFullPath(file), Path.GetFullPath(self), StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                TryDeleteFile(file);
            }

            foreach (var directory in Directory.EnumerateDirectories(installDirectory, "*", SearchOption.AllDirectories))
            {
                try
                {
                    if (!Directory.EnumerateFileSystemEntries(directory).Any())
                    {
                        Directory.Delete(directory, recursive: false);
                    }
                }
                catch
                {
                    // 忽略。
                }
            }
        }
        catch
        {
            // 忽略。
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // 忽略。
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // 忽略。
        }
    }
}
