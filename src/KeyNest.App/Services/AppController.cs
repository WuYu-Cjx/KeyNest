using System;
using System.IO;
using System.Threading;
using System.Windows;
using KeyNest.App.Tray;
using KeyNest.App.ViewModels;
using KeyNest.App.Views;
using KeyNest.Core.Input;
using KeyNest.Core.Models;
using KeyNest.Core.Services;
using KeyNest.UI.Services;

namespace KeyNest.App.Services;

/// <summary>
/// 应用生命周期编排：单实例由 App 负责，这里管理托盘、监听开关、悬浮窗、设置窗口与钩子事件。
/// 任何情况下都不会执行快捷键，也不会拦截系统按键（除非用户显式开启“接管单按 Win”）。
/// </summary>
public sealed class AppController : IDisposable
{
    private readonly SettingsService _settings;
    private readonly AutoStartService _autoStart = new();
    private readonly ShortcutMatcher _matcher;
    private readonly KeyboardHook _hook;
    private readonly HookWatchdog _watchdog;
    private readonly FloatingViewModel _floatingViewModel = new();
    private readonly FloatingWindow _floatingWindow;

    private TrayIconHost? _tray;
    private SettingsWindow? _settingsWindow;
    private bool _disposed;
    private bool _renderedIncludeNoModifier;
    private ModifierMask _activePrefix = ModifierMask.None;
    private bool _showingOverview;

    public AppController(SettingsService settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        var repository = LoadShortcuts();
        _matcher = new ShortcutMatcher(repository.Items);

        _hook = new KeyboardHook();
        _hook.ModifierStateChanged += OnModifierStateChanged;
        _hook.KeyPressed += OnKeyPressed;
        _hook.TakeoverSingleWin = _settings.Current.Floating.TakeoverSingleWin;

        _watchdog = new HookWatchdog(_hook, SynchronizationContext.Current);

        _floatingWindow = new FloatingWindow(_floatingViewModel);
        _floatingWindow.DragCompleted += OnFloatingDragCompleted;

        _settings.Changed += OnSettingsChanged;
    }

    public bool IsListening { get; private set; }

    public SettingsService Settings => _settings;

    public void Start()
    {
        _tray = new TrayIconHost();
        _tray.EnableRequested += (_, _) => EnableListening();
        _tray.DisableRequested += (_, _) => DisableListening();
        _tray.TogglePanelRequested += (_, _) => TogglePanel();
        _tray.SettingsRequested += (_, _) => ShowSettings();
        _tray.AutoStartToggleRequested += (_, enabled) => _settings.Update(s => s.AutoStart = enabled);
        _tray.ExitRequested += (_, _) => Quit();

        _tray.SetListening(false);
        _tray.SetAutoStart(_settings.Current.AutoStart);
        _tray.SetPanelVisible(false);

        SyncAutoStartToSettings();
        LocalizationService.LanguageChanged += OnLanguageChanged;
        Logger.Info("托盘已就绪，当前状态：未监听");
    }

    /// <summary>语言切换：刷新托盘菜单与当前悬浮窗内容，不触碰钩子与匹配逻辑。</summary>
    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        _tray?.ApplyLanguage();

        if (!_floatingWindow.IsVisible)
        {
            return;
        }

        if (_showingOverview)
        {
            ShowOverview();
        }
        else if (_activePrefix != ModifierMask.None)
        {
            ShowPanel(_activePrefix);
        }
    }

    public bool EnableListening()
    {
        if (IsListening)
        {
            return true;
        }

        if (!_hook.Install())
        {
            _tray?.ShowNotification(
                LocalizationService.Get("App.ProductName"),
                LocalizationService.Get("Tray.HookFailed"));
            return false;
        }

        _hook.TakeoverSingleWin = _settings.Current.Floating.TakeoverSingleWin;
        _watchdog.Start();
        IsListening = true;
        _tray?.SetListening(true);
        Logger.Info("已启用监听");
        return true;
    }

    public void DisableListening()
    {
        if (!IsListening)
        {
            return;
        }

        _hook.Uninstall();
        _watchdog.Stop();

        IsListening = false;

        _floatingWindow.HidePanel();
        _floatingViewModel.Clear();
        _tray?.SetListening(false);
        _tray?.SetPanelVisible(false);
        Logger.Info("已停用监听");
    }

    public void Quit()
    {
        if (_disposed)
        {
            return;
        }

        DisableListening();
        _floatingWindow.Close();
        _settingsWindow?.Close();
        _settingsWindow = null;

        _tray?.Dispose();
        _tray = null;

        Logger.Info("键巢完全退出");
        System.Windows.Application.Current.Shutdown();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _settings.Changed -= OnSettingsChanged;
        LocalizationService.LanguageChanged -= OnLanguageChanged;
        _hook.ModifierStateChanged -= OnModifierStateChanged;
        _hook.KeyPressed -= OnKeyPressed;

        _watchdog.Dispose();
        _hook.Dispose();
        _tray?.Dispose();
        _tray = null;
    }

    private void TogglePanel()
    {
        if (_floatingWindow.IsVisible)
        {
            HideFloating();
            return;
        }

        if (!IsListening && !EnableListening())
        {
            return;
        }

        var state = _hook.CurrentModifiers;

        if (state != ModifierMask.None)
        {
            ShowPanel(state);
        }
        else
        {
            ShowOverview();
        }
    }

    private void OnModifierStateChanged(object? sender, ModifierStateChangedEventArgs e)
    {
        if (!IsListening)
        {
            return;
        }

        // P 变为空：立即隐藏，不做延迟隐藏。
        if (e.State == ModifierMask.None)
        {
            if (_floatingWindow.IsVisible)
            {
                HideFloating();
            }

            return;
        }

        // P 非空：每次集合变化都刷新，列表随按下/释放修饰键实时收窄或扩展。
        ShowPanel(e.State);
    }

    private void OnKeyPressed(object? sender, KeyPressedEventArgs e)
    {
        if (!IsListening || !_floatingWindow.IsVisible)
        {
            return;
        }

        // Esc 立即隐藏（悬浮窗不抢焦点，因此 Esc 由全局钩子捕获，仅用于关闭提示）。
        if (e.VirtualKey == 0x1B)
        {
            HideFloating();
            return;
        }

        _floatingViewModel.Highlight(e.VirtualKey);
    }

    private void ShowPanel(ModifierMask prefix)
    {
        var floating = _settings.Current.Floating;
        var sections = _matcher.Match(prefix, floating.IncludeNoModifierKeys);

        _floatingViewModel.Load(prefix, sections);
        _floatingWindow.ShowPanel(_settings.Current);

        _activePrefix = prefix;
        _showingOverview = false;
        _renderedIncludeNoModifier = floating.IncludeNoModifierKeys;
        _tray?.SetPanelVisible(true);
    }

    private void ShowOverview()
    {
        var floating = _settings.Current.Floating;
        var sections = _matcher.MatchAll(floating.IncludeNoModifierKeys);

        _floatingViewModel.LoadOverview(sections);
        _floatingWindow.ShowPanel(_settings.Current);

        _activePrefix = ModifierMask.None;
        _showingOverview = true;
        _renderedIncludeNoModifier = floating.IncludeNoModifierKeys;
        _tray?.SetPanelVisible(true);
    }

    private void HideFloating()
    {
        _floatingWindow.HidePanel();
        _floatingViewModel.Clear();
        _activePrefix = ModifierMask.None;
        _showingOverview = false;
        _tray?.SetPanelVisible(false);
    }

    private void OnFloatingDragCompleted(object? sender, EventArgs e)
    {
        var left = _floatingWindow.Left;
        var top = _floatingWindow.Top;

        _settings.Update(s =>
        {
            s.Position.HasValue = true;
            s.Position.Left = left;
            s.Position.Top = top;
        });
    }

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        var current = _settings.Current;

        if (LocalizationService.Current != current.Language)
        {
            LocalizationService.SetLanguage(current.Language);
        }

        ThemeManager.SetMode(current.Theme);
        _hook.TakeoverSingleWin = current.Floating.TakeoverSingleWin;
        _tray?.SetAutoStart(current.AutoStart);

        if (_autoStart.IsEnabled != current.AutoStart)
        {
            _autoStart.SetEnabled(current.AutoStart);
        }

        if (_floatingWindow.IsVisible)
        {
            _floatingWindow.ApplySettings(current);

            // 影响列表内容的设置变化时，立即按当前修饰键状态重新渲染。
            if (_renderedIncludeNoModifier != current.Floating.IncludeNoModifierKeys)
            {
                var state = _hook.CurrentModifiers;

                if (state != ModifierMask.None)
                {
                    ShowPanel(state);
                }
                else
                {
                    ShowOverview();
                }
            }

            if (!current.Position.HasValue)
            {
                _floatingWindow.ResetPositionToDefault();
            }
        }
    }

    public void ShowSettings()
    {
        if (_settingsWindow is { IsLoaded: true })
        {
            _settingsWindow.Activate();
            return;
        }

        var viewModel = new SettingsViewModel(_settings, CloseSettingsWindow);
        _settingsWindow = new SettingsWindow { DataContext = viewModel };
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    private void CloseSettingsWindow()
    {
        _settingsWindow?.Close();
        _settingsWindow = null;
    }

    private void SyncAutoStartToSettings()
    {
        var registryState = _autoStart.IsEnabled;
        if (registryState == _settings.Current.AutoStart)
        {
            return;
        }

        if (_settings.Current.AutoStart)
        {
            _autoStart.SetEnabled(true);
        }
        else
        {
            _autoStart.SetEnabled(false);
        }
    }

    private static ShortcutRepository LoadShortcuts()
    {
        try
        {
            if (File.Exists(AppPaths.ShortcutsFile))
            {
                return ShortcutRepository.LoadFile(AppPaths.ShortcutsFile);
            }
        }
        catch (Exception ex)
        {
            Logger.Error("读取外部快捷键数据失败", ex);
        }

        try
        {
            var assembly = typeof(AppController).Assembly;
            using var stream = assembly.GetManifestResourceStream("KeyNest.App.shortcuts.json");
            if (stream is not null)
            {
                return ShortcutRepository.LoadStream(stream, "内嵌快捷键数据");
            }
        }
        catch (Exception ex)
        {
            Logger.Error("读取内嵌快捷键数据失败", ex);
        }

        Logger.Warn("未能加载任何快捷键数据，悬浮窗将显示空列表");
        return ShortcutRepository.Empty;
    }
}
