using System;
using System.IO;
using KeyNest.Core.Services;
using Microsoft.Win32;

namespace KeyNest.App.Services;

/// <summary>
/// 开机自启：只写当前用户 HKCU\Software\Microsoft\Windows\CurrentVersion\Run，值名固定为 KeyNest。
/// 使用 --tray 参数启动，程序启动后仍然只常驻托盘且默认不监听。
/// </summary>
public sealed class AutoStartService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "KeyNest";

    public string ExecutablePath =>
        Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "KeyNest.App.exe");

    public string Command => $"\"{ExecutablePath}\" --tray";

    public bool IsEnabled
    {
        get
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
                return key?.GetValue(ValueName) is string value && value.Length > 0;
            }
            catch (Exception ex)
            {
                Logger.Warn($"读取开机自启项失败：{ex.Message}");
                return false;
            }
        }
    }

    public bool SetEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
            if (key is null)
            {
                return false;
            }

            if (enabled)
            {
                key.SetValue(ValueName, Command, RegistryValueKind.String);
                Logger.Info($"已写入开机自启：{Command}");
            }
            else
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
                Logger.Info("已移除开机自启项");
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.Warn($"写入开机自启项失败：{ex.Message}");
            return false;
        }
    }
}
