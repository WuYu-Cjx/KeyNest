using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using KeyNest.App.Services;
using KeyNest.Core.Models;
using KeyNest.Core.Mvvm;
using KeyNest.Core.Services;
using KeyNest.UI.Services;

namespace KeyNest.App.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settings;
    private readonly Action _close;

    public SettingsViewModel(SettingsService settings, Action close)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _close = close ?? throw new ArgumentNullException(nameof(close));

        ResetPositionCommand = new RelayCommand(ResetPosition);
        OpenDataFolderCommand = new RelayCommand(OpenDataFolder);
        OpenLicenseCommand = new RelayCommand(OpenLicense);
        UninstallCommand = new RelayCommand(Uninstall);
        CloseCommand = new RelayCommand(_close);
    }

    public string ProductName => LocalizationService.Get("App.ProductName");

    public string VersionText
    {
        get
        {
            var version = typeof(SettingsViewModel).Assembly.GetName().Version;
            return version is null ? "1.0.0" : $"{version.Major}.{version.Minor}.{version.Build}";
        }
    }

    public string VersionDisplay => LocalizationService.Format("Settings.About.VersionFormat", VersionText);

    public string DataFolderPath => AppPaths.DataDirectory;

    public string LicenseSummary => LocalizationService.Get("Settings.About.LicenseSummary");

    // ===== 常规 =====

    public bool AutoStart
    {
        get => _settings.Current.AutoStart;
        set
        {
            if (value == AutoStart)
            {
                return;
            }

            _settings.Update(s => s.AutoStart = value);
            OnPropertyChanged();
        }
    }

    // ===== 主题 =====

    public bool ThemeFollowSystem
    {
        get => _settings.Current.Theme == ThemeMode.System;
        set
        {
            if (value)
            {
                SetTheme(ThemeMode.System);
            }
        }
    }

    public bool ThemeLight
    {
        get => _settings.Current.Theme == ThemeMode.Light;
        set
        {
            if (value)
            {
                SetTheme(ThemeMode.Light);
            }
        }
    }

    public bool ThemeDark
    {
        get => _settings.Current.Theme == ThemeMode.Dark;
        set
        {
            if (value)
            {
                SetTheme(ThemeMode.Dark);
            }
        }
    }

    // ===== 语言 =====

    public bool LanguageChinese
    {
        get => _settings.Current.Language == AppLanguage.Chinese;
        set
        {
            if (value)
            {
                SetLanguage(AppLanguage.Chinese);
            }
        }
    }

    public bool LanguageEnglish
    {
        get => _settings.Current.Language == AppLanguage.English;
        set
        {
            if (value)
            {
                SetLanguage(AppLanguage.English);
            }
        }
    }

    // ===== 悬浮窗 =====

    public double OpacityValue
    {
        get => _settings.Current.Floating.Opacity;
        set
        {
            var clamped = Math.Clamp(value, 0.75, 1.0);
            if (Math.Abs(clamped - OpacityValue) < 0.005)
            {
                return;
            }

            _settings.Update(s => s.Floating.Opacity = clamped);
            OnPropertyChanged();
            OnPropertyChanged(nameof(OpacityText));
        }
    }

    public string OpacityText => $"{OpacityValue * 100:0}%";

    public bool TakeoverSingleWin
    {
        get => _settings.Current.Floating.TakeoverSingleWin;
        set
        {
            if (value == TakeoverSingleWin)
            {
                return;
            }

            _settings.Update(s => s.Floating.TakeoverSingleWin = value);
            OnPropertyChanged();
        }
    }

    /// <summary>在已展开的悬浮窗中补充显示无修饰键单键快捷键（默认关闭）。</summary>
    public bool IncludeNoModifierKeys
    {
        get => _settings.Current.Floating.IncludeNoModifierKeys;
        set
        {
            if (value == IncludeNoModifierKeys)
            {
                return;
            }

            _settings.Update(s => s.Floating.IncludeNoModifierKeys = value);
            OnPropertyChanged();
        }
    }

    public ICommand ResetPositionCommand { get; }

    public ICommand OpenDataFolderCommand { get; }

    public ICommand OpenLicenseCommand { get; }

    public ICommand UninstallCommand { get; }

    public ICommand CloseCommand { get; }

    private void SetTheme(ThemeMode mode)
    {
        if (_settings.Current.Theme == mode)
        {
            return;
        }

        _settings.Update(s => s.Theme = mode);

        OnPropertyChanged(nameof(ThemeFollowSystem));
        OnPropertyChanged(nameof(ThemeLight));
        OnPropertyChanged(nameof(ThemeDark));
    }

    private void SetLanguage(AppLanguage language)
    {
        if (_settings.Current.Language == language)
        {
            return;
        }

        // 保存后由 AppController 统一应用语言，确保托盘与悬浮窗同步刷新。
        _settings.Update(s => s.Language = language);

        OnPropertyChanged(nameof(LanguageChinese));
        OnPropertyChanged(nameof(LanguageEnglish));
        OnPropertyChanged(nameof(ProductName));
        OnPropertyChanged(nameof(VersionDisplay));
        OnPropertyChanged(nameof(LicenseSummary));
    }

    private void ResetPosition()
    {
        _settings.Update(s =>
        {
            s.Position.HasValue = false;
            s.Position.Left = 0;
            s.Position.Top = 0;
        });
    }

    private static void OpenDataFolder()
    {
        try
        {
            AppPaths.EnsureDataDirectory();
            Process.Start(new ProcessStartInfo
            {
                FileName = AppPaths.DataDirectory,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                LocalizationService.Format("Settings.Error.OpenDataFolderFormat", ex.Message),
                LocalizationService.Get("App.ProductName"),
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private static void OpenLicense()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "THIRD-PARTY-NOTICES.txt");
        if (!File.Exists(path))
        {
            MessageBox.Show(
                LocalizationService.Get("Settings.Error.LicenseMissing"),
                LocalizationService.Get("App.ProductName"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                LocalizationService.Format("Settings.Error.OpenLicenseFormat", ex.Message),
                LocalizationService.Get("App.ProductName"),
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void Uninstall()
    {
        var uninstaller = Path.Combine(AppContext.BaseDirectory, "KeyNest.Uninstaller.exe");
        if (!File.Exists(uninstaller))
        {
            MessageBox.Show(
                LocalizationService.Get("Settings.Uninstall.Missing"),
                LocalizationService.Get("App.ProductName"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var confirm = MessageBox.Show(
            LocalizationService.Get("Settings.Uninstall.Confirm"),
            LocalizationService.Get("App.ProductName"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo { FileName = uninstaller, UseShellExecute = true });

            // 卸载由独立进程执行；立即退出本进程，释放托盘图标并解除文件占用。
            _close();
            Application.Current?.Shutdown();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                LocalizationService.Format("Settings.Uninstall.StartFailedFormat", ex.Message),
                LocalizationService.Get("App.ProductName"),
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}
