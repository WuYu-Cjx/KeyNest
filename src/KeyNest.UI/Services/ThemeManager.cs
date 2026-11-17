using System.Windows;
using System.Windows.Threading;
using KeyNest.Core.Models;

namespace KeyNest.UI.Services;

/// <summary>
/// 主题管理器：在浅色 / 深色 / 跟随系统之间实时切换。
/// 通过替换合并字典中的主题字典实现，所有控件使用 DynamicResource，因此切换即时生效。
/// 系统开启高对比度时自动使用高对比度主题。
/// </summary>
public static class ThemeManager
{
    private static readonly Uri LightUri = new("pack://application:,,,/KeyNest.UI;component/Themes/Light.xaml", UriKind.Absolute);
    private static readonly Uri DarkUri = new("pack://application:,,,/KeyNest.UI;component/Themes/Dark.xaml", UriKind.Absolute);
    private static readonly Uri HighContrastUri = new("pack://application:,,,/KeyNest.UI;component/Themes/HighContrast.xaml", UriKind.Absolute);

    private static Application? _app;
    private static DispatcherTimer? _pollTimer;
    private static bool? _appliedDark;
    private static bool _appliedHighContrast;

    public static ThemeMode Mode { get; private set; } = ThemeMode.System;

    public static bool IsDark { get; private set; }

    public static bool IsHighContrast { get; private set; }

    public static event EventHandler? ThemeApplied;

    public static void Initialize(Application app)
    {
        _app = app ?? throw new ArgumentNullException(nameof(app));

        Apply(force: true);

        if (_pollTimer is not null)
        {
            return;
        }

        // 轻量轮询系统主题，避免依赖额外组件；每 3 秒一次，开销可忽略。
        _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _pollTimer.Tick += (_, _) =>
        {
            if (Mode == ThemeMode.System)
            {
                Apply(force: false);
            }
            else if (_appliedHighContrast != SystemThemeReader.IsHighContrast)
            {
                Apply(force: false);
            }
        };
        _pollTimer.Start();
    }

    public static void SetMode(ThemeMode mode)
    {
        Mode = mode;
        Apply(force: false);
    }

    private static void Apply(bool force)
    {
        if (_app is null)
        {
            return;
        }

        var highContrast = SystemThemeReader.IsHighContrast;
        var dark = Mode switch
        {
            ThemeMode.Dark => true,
            ThemeMode.Light => false,
            _ => SystemThemeReader.IsDark()
        };

        if (!force && _appliedDark == dark && _appliedHighContrast == highContrast)
        {
            IsDark = dark;
            IsHighContrast = highContrast;
            return;
        }

        var uri = highContrast ? HighContrastUri : dark ? DarkUri : LightUri;
        var merged = _app.Resources.MergedDictionaries;
        var index = FindThemeDictionaryIndex(merged);

        if (index >= 0)
        {
            merged[index] = new ResourceDictionary { Source = uri };
        }
        else
        {
            merged.Insert(0, new ResourceDictionary { Source = uri });
        }

        _appliedDark = dark;
        _appliedHighContrast = highContrast;
        IsDark = dark;
        IsHighContrast = highContrast;

        ThemeApplied?.Invoke(null, EventArgs.Empty);
    }

    private static int FindThemeDictionaryIndex(ICollection<ResourceDictionary> dictionaries)
    {
        var list = dictionaries as IList<ResourceDictionary> ?? dictionaries.ToList();

        for (var i = 0; i < list.Count; i++)
        {
            var source = list[i].Source?.OriginalString;
            if (string.IsNullOrEmpty(source))
            {
                continue;
            }

            if (source.Contains("/Themes/Light.xaml", StringComparison.OrdinalIgnoreCase)
                || source.Contains("/Themes/Dark.xaml", StringComparison.OrdinalIgnoreCase)
                || source.Contains("/Themes/HighContrast.xaml", StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }
}
