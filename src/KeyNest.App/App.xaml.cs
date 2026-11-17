using System;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using KeyNest.App.Services;
using KeyNest.Core.Models;
using KeyNest.Core.Services;
using KeyNest.UI.Services;

namespace KeyNest.App;

public partial class App : Application
{
    private SingleInstanceGuard? _guard;
    private SingleInstanceActivation? _activation;
    private AppExitSignal? _exitSignal;
    private AppController? _controller;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ThemeManager.Initialize(this);

        // 开机自启使用 --tray，只驻留托盘；安装完成后由安装器传入 --enabled，启动即启用监听；
        // 用户主动打开快捷方式时直接显示设置窗口，保持默认未监听。
        var startInTray = e.Args.Any(arg => string.Equals(arg, "--tray", StringComparison.OrdinalIgnoreCase));
        var startEnabled = e.Args.Any(arg => string.Equals(arg, "--enabled", StringComparison.OrdinalIgnoreCase));
        var languageArg = ReadArgument(e.Args, "--language");

        _guard = new SingleInstanceGuard();
        if (!_guard.IsFirstInstance)
        {
            if (!startInTray)
            {
                // 已有实例：请求它打开设置窗口（安装完成时同时启用监听），本进程静默退出。
                SingleInstanceActivation.RequestActivation(startEnabled);
            }

            Shutdown();
            return;
        }

        DispatcherUnhandledException += OnDispatcherUnhandledException;

        AppPaths.EnsureDataDirectory();

        var settings = new SettingsService();

        if (languageArg is not null && languageArg != settings.Current.Language)
        {
            settings.Update(s => s.Language = languageArg.Value);
        }

        LocalizationService.Initialize(this, settings.Current.Language);
        ThemeManager.SetMode(settings.Current.Theme);

        _controller = new AppController(settings);

        // 强制运行态：启动后默认不监听，只常驻托盘。
        _controller.Start();

        // 监听后续启动请求（例如用户再次双击快捷方式），在界面线程打开设置窗口；
        // 安装完成启动时还会请求启用监听。
        _activation = new SingleInstanceActivation(
            () => Dispatcher.BeginInvoke(new Action(() => _controller?.ShowSettings())),
            () => Dispatcher.BeginInvoke(new Action(() =>
            {
                _controller?.EnableListening();
                _controller?.ShowSettings();
            })));

        // 卸载器通过命名事件请求退出，确保托盘图标与键盘钩子被正常释放。
        _exitSignal = new AppExitSignal(
            () => Dispatcher.BeginInvoke(new Action(() => _controller?.Quit())));

        if (startEnabled)
        {
            _controller.EnableListening();
        }

        if (!startInTray)
        {
            _controller.ShowSettings();
        }

        Logger.Info("========== 键巢启动 ==========");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        DispatcherUnhandledException -= OnDispatcherUnhandledException;

        _activation?.Dispose();
        _activation = null;

        _exitSignal?.Dispose();
        _exitSignal = null;

        _controller?.Dispose();
        _controller = null;

        _guard?.Dispose();
        _guard = null;

        base.OnExit(e);
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Logger.Error("未处理的界面线程异常", e.Exception);
        e.Handled = true;
    }

    /// <summary>读取 “--language zh-CN|en-US” 形式的参数。</summary>
    private static AppLanguage? ReadArgument(string[] args, string name)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (!string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = args[i + 1];

            if (string.Equals(value, AppLanguageExtensions.ChineseCode, StringComparison.OrdinalIgnoreCase))
            {
                return AppLanguage.Chinese;
            }

            if (string.Equals(value, AppLanguageExtensions.EnglishCode, StringComparison.OrdinalIgnoreCase))
            {
                return AppLanguage.English;
            }
        }

        return null;
    }
}
