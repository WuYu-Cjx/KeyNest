using System;
using System.IO;
using KeyNest.Core.Models;
using KeyNest.Core.Services;
using Xunit;

namespace KeyNest.Tests;

public class SettingsServiceTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), "keynest-test-" + Guid.NewGuid().ToString("N"), "settings.json");

    [Fact]
    public void Defaults_AreOffAndFollowSystem()
    {
        var service = new SettingsService(_path);

        Assert.False(service.Current.AutoStart);
        Assert.Equal(ThemeMode.System, service.Current.Theme);
        Assert.False(service.Current.Floating.TakeoverSingleWin);
        Assert.False(service.Current.Floating.IncludeNoModifierKeys);
    }

    [Fact]
    public void Update_PersistsAndRaisesChanged()
    {
        var service = new SettingsService(_path);
        var raised = 0;
        service.Changed += (_, _) => raised++;

        service.Update(s =>
        {
            s.Theme = ThemeMode.Dark;
            s.Floating.Opacity = 0.9;
        });

        Assert.Equal(1, raised);

        var reloaded = new SettingsService(_path);
        Assert.Equal(ThemeMode.Dark, reloaded.Current.Theme);
        Assert.Equal(0.9, reloaded.Current.Floating.Opacity, 3);
    }

    [Fact]
    public void Update_ClampsOutOfRangeValues()
    {
        var service = new SettingsService(_path);

        service.Update(s =>
        {
            s.Floating.Opacity = 3.0;
        });

        Assert.Equal(1.0, service.Current.Floating.Opacity, 3);

        service.Update(s =>
        {
            s.Floating.Opacity = 0.1;
        });

        Assert.Equal(0.75, service.Current.Floating.Opacity, 3);
    }

    [Fact]
    public void Defaults_LanguageIsSimplifiedChinese()
    {
        var service = new SettingsService(_path);

        Assert.Equal(AppLanguage.Chinese, service.Current.Language);
        Assert.Equal("zh-CN", service.Current.Language.ToCode());
    }

    [Fact]
    public void Update_PersistsLanguage()
    {
        var service = new SettingsService(_path);

        service.Update(s => s.Language = AppLanguage.English);

        var reloaded = new SettingsService(_path);
        Assert.Equal(AppLanguage.English, reloaded.Current.Language);
        Assert.Contains("\"en-US\"", File.ReadAllText(_path), StringComparison.Ordinal);
    }

    public void Dispose()
    {
        try
        {
            var directory = Path.GetDirectoryName(_path);
            if (directory is not null && Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
        catch
        {
            // 忽略清理失败。
        }
    }
}
