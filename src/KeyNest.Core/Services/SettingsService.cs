using KeyNest.Core.Models;

namespace KeyNest.Core.Services;

/// <summary>
/// 配置读写。写入采用“临时文件 + 替换”，避免断电或崩溃造成配置损坏。
/// 任何异常都不会导致程序退出，最多回落到默认配置。
/// </summary>
public sealed class SettingsService
{
    private readonly string _filePath;
    private readonly bool _isDefaultPath;
    private readonly object _gate = new();
    private AppSettings _current;

    public SettingsService(string? filePath = null)
    {
        _isDefaultPath = filePath is null;
        _filePath = filePath ?? AppPaths.SettingsFile;
        _current = LoadFromDisk();
        Logger.Enabled = _current.EnableLogging;
    }

    public AppSettings Current => _current;

    public event EventHandler? Changed;

    /// <summary>修改并立即持久化；mutate 内部只应修改配置字段。</summary>
    public void Update(Action<AppSettings> mutate)
    {
        if (mutate is null)
        {
            return;
        }

        lock (_gate)
        {
            mutate(_current);
            Clamp(_current);
            SaveToDisk(_current);
            Logger.Enabled = _current.EnableLogging;
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Save()
    {
        lock (_gate)
        {
            Clamp(_current);
            SaveToDisk(_current);
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    private AppSettings LoadFromDisk()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                // 旧版本使用 settings.json；首次启动 config.json 时做一次性迁移。
                if (_isDefaultPath && File.Exists(AppPaths.LegacySettingsFile))
                {
                    var legacyJson = File.ReadAllText(AppPaths.LegacySettingsFile);
                    var migrated = JsonStore.Deserialize<AppSettings>(legacyJson) ?? new AppSettings();
                    Clamp(migrated);
                    SaveToDisk(migrated);
                    Logger.Info("已将旧版 settings.json 迁移为 config.json");
                    return migrated;
                }

                return new AppSettings();
            }

            var json = File.ReadAllText(_filePath);
            var settings = JsonStore.Deserialize<AppSettings>(json) ?? new AppSettings();
            Clamp(settings);
            return settings;
        }
        catch (Exception ex)
        {
            Logger.Warn($"读取配置失败，使用默认配置：{ex.Message}");
            return new AppSettings();
        }
    }

    private void SaveToDisk(AppSettings settings)
    {
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var tempPath = _filePath + ".tmp";
            File.WriteAllText(tempPath, JsonStore.Serialize(settings));

            if (File.Exists(_filePath))
            {
                File.Replace(tempPath, _filePath, null);
            }
            else
            {
                File.Move(tempPath, _filePath);
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"保存配置失败：{ex.Message}");
        }
    }

    private static void Clamp(AppSettings settings)
    {
        settings.Theme = Enum.IsDefined(typeof(ThemeMode), settings.Theme) ? settings.Theme : ThemeMode.System;

        if (!Enum.IsDefined(typeof(AppLanguage), settings.Language))
        {
            settings.Language = AppLanguage.Chinese;
            Logger.Warn("配置中的 language 非法，已回退为 zh-CN");
        }

        if (settings.Floating is null)
        {
            settings.Floating = new FloatingSettings();
        }

        settings.Floating.Opacity = Math.Clamp(settings.Floating.Opacity, 0.75, 1.0);

        if (settings.Position is null)
        {
            settings.Position = new WindowPosition();
        }

        if (double.IsNaN(settings.Position.Left) || double.IsInfinity(settings.Position.Left))
        {
            settings.Position.Left = 0;
        }

        if (double.IsNaN(settings.Position.Top) || double.IsInfinity(settings.Position.Top))
        {
            settings.Position.Top = 0;
        }
    }
}
