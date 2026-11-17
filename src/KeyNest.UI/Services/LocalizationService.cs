using System;
using System.Globalization;
using System.Windows;
using KeyNest.Core.Models;

namespace KeyNest.UI.Services;

/// <summary>
/// 本地化管理器：通过替换合并字典中的语言字典实现运行时切换。
/// 所有界面文本使用 DynamicResource，切换语言后立即刷新，且与主题字典完全独立。
/// 安装器在欢迎页也可直接调用 SetLanguage 实现即时切换。
/// </summary>
public static class LocalizationService
{
    private static readonly Uri ChineseUri = new("pack://application:,,,/KeyNest.UI;component/Resources/Strings.zh-CN.xaml", UriKind.Absolute);
    private static readonly Uri EnglishUri = new("pack://application:,,,/KeyNest.UI;component/Resources/Strings.en-US.xaml", UriKind.Absolute);

    private static Application? _app;

    public static AppLanguage Current { get; private set; } = AppLanguage.Chinese;

    public static string CurrentCode => Current.ToCode();

    /// <summary>语言切换完成后触发，供托盘菜单、缓存文案等非绑定内容刷新。</summary>
    public static event EventHandler? LanguageChanged;

    public static void Initialize(Application app, AppLanguage language)
    {
        _app = app ?? throw new ArgumentNullException(nameof(app));
        Apply(language);
    }

    public static void SetLanguage(AppLanguage language)
    {
        if (language == Current && _app is not null)
        {
            return;
        }

        Apply(language);
    }

    /// <summary>读取当前语言下的字符串；键不存在时返回键名，便于发现遗漏。</summary>
    public static string Get(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return string.Empty;
        }

        if (_app is not null && _app.TryFindResource(key) is string text)
        {
            return text;
        }

        return key;
    }

    public static string Format(string key, params object?[] args)
    {
        var template = Get(key);

        try
        {
            return string.Format(CultureInfo.CurrentCulture, template, args);
        }
        catch (FormatException)
        {
            return template;
        }
    }

    private static void Apply(AppLanguage language)
    {
        Current = language;

        SetUiCulture(language);

        if (_app is null)
        {
            return;
        }

        var merged = _app.Resources.MergedDictionaries;
        var uri = language == AppLanguage.English ? EnglishUri : ChineseUri;
        var dictionary = new ResourceDictionary { Source = uri };

        var index = FindLanguageDictionaryIndex(merged);
        if (index >= 0)
        {
            merged[index] = dictionary;
        }
        else
        {
            merged.Insert(0, dictionary);
        }

        LanguageChanged?.Invoke(null, EventArgs.Empty);
    }

    private static void SetUiCulture(AppLanguage language)
    {
        try
        {
            var culture = CultureInfo.GetCultureInfo(language.ToCode());
            CultureInfo.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }
        catch (CultureNotFoundException)
        {
            // 系统缺少对应语言包时保持原样，界面文本仍按资源字典切换。
        }
    }

    private static int FindLanguageDictionaryIndex(System.Collections.Generic.IList<ResourceDictionary> dictionaries)
    {
        for (var i = 0; i < dictionaries.Count; i++)
        {
            var source = dictionaries[i].Source?.OriginalString;
            if (string.IsNullOrEmpty(source))
            {
                continue;
            }

            if (source.Contains("/Resources/Strings.zh-CN.xaml", StringComparison.OrdinalIgnoreCase)
                || source.Contains("/Resources/Strings.en-US.xaml", StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }
}
