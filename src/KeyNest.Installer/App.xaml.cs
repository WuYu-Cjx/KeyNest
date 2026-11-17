using System;
using System.Windows;
using System.Windows.Threading;
using KeyNest.Core.Models;
using KeyNest.Core.Services;
using KeyNest.Installer.Views;
using KeyNest.UI.Services;

namespace KeyNest.Installer;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 安装器同样跟随系统深浅色，并支持系统高对比度。
        ThemeManager.Initialize(this);

        // 安装语言默认简体中文，可在欢迎页即时切换。
        LocalizationService.Initialize(this, AppLanguage.Chinese);

        DispatcherUnhandledException += OnDispatcherUnhandledException;

        var window = new MainWindow();
        MainWindow = window;
        window.Show();
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Logger.Error("安装器未处理异常", e.Exception);
        MessageBox.Show(
            LocalizationService.Format("Installer.UnexpectedError", e.Exception.Message),
            LocalizationService.Get("Installer.WindowTitle"),
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }
}
