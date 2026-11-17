using System;
using System.Diagnostics;
using System.Security.Principal;

namespace KeyNest.Installer.Services;

/// <summary>管理员权限检测与自提升。只在用户选择“每机器安装”时使用。</summary>
public static class ElevationHelper
{
    public static bool IsElevated
    {
        get
        {
            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>以管理员身份重新启动安装器；成功返回 true，调用方应随后退出当前进程。</summary>
    public static bool TryRelaunchElevated()
    {
        var executable = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executable))
        {
            return false;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = executable,
                UseShellExecute = true,
                Verb = "runas"
            });
            return true;
        }
        catch
        {
            return false;
        }
    }
}
