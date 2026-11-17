namespace KeyNest.Core.Services;

/// <summary>
/// 应用固定路径。所有用户数据仅保存在本机 %APPDATA%\KeyNest，不联网、不上传。
/// </summary>
public static class AppPaths
{
    public const string ProductName = "键巢（KeyNest）";
    public const string ProductNameShort = "键巢 KeyNest";
    public const string DataFolderName = "KeyNest";

    public static string DataDirectory { get; } =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            DataFolderName);

    public static string ConfigFile => Path.Combine(DataDirectory, "config.json");

    /// <summary>旧版本配置文件，仅用于一次性迁移。</summary>
    public static string LegacySettingsFile => Path.Combine(DataDirectory, "settings.json");

    public static string SettingsFile => ConfigFile;

    public static string LogFile => Path.Combine(DataDirectory, "keynest.log");

    /// <summary>安装目录下的快捷键数据文件。</summary>
    public static string ShortcutsFile =>
        Path.Combine(AppContext.BaseDirectory, "Assets", "Data", "shortcuts.json");

    public static void EnsureDataDirectory()
    {
        Directory.CreateDirectory(DataDirectory);
    }
}
