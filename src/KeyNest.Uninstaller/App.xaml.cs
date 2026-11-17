using System;
using System.IO;
using System.Windows;
using KeyNest.Core.Models;
using KeyNest.Core.Services;
using KeyNest.Uninstaller.Services;
using KeyNest.UI.Services;

namespace KeyNest.Uninstaller;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ThemeManager.Initialize(this);
        LocalizationService.Initialize(this, ReadLanguage());

        var window = new MainWindow();
        MainWindow = window;
        window.Show();
    }

    /// <summary>卸载界面沿用用户在键巢中选择的语言；读取失败时回退简体中文。</summary>
    private static AppLanguage ReadLanguage()
    {
        try
        {
            foreach (var fileName in new[] { "config.json", "settings.json" })
            {
                var path = Path.Combine(UninstallerPaths.UserDataDirectory, fileName);
                if (!File.Exists(path))
                {
                    continue;
                }

                var settings = JsonStore.Deserialize<AppSettings>(File.ReadAllText(path));
                if (settings is not null)
                {
                    return settings.Language;
                }
            }
        }
        catch
        {
            // 配置损坏时使用默认语言，不影响卸载。
        }

        return AppLanguage.Chinese;
    }
}
