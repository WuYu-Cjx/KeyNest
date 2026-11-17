namespace KeyNest.Core.Models;

public enum ThemeMode
{
    System = 0,
    Light = 1,
    Dark = 2
}

public sealed class AppSettings
{
    public int SchemaVersion { get; set; } = 1;

    public ThemeMode Theme { get; set; } = ThemeMode.System;

    /// <summary>界面语言，默认中文。</summary>
    public AppLanguage Language { get; set; } = AppLanguage.Chinese;

    /// <summary>诊断日志默认关闭；关闭时不写任何日志文件。</summary>
    public bool EnableLogging { get; set; }

    /// <summary>是否已写入开机自启。仅当用户在安装器或设置中显式开启时为 true。</summary>
    public bool AutoStart { get; set; }

    public FloatingSettings Floating { get; set; } = new();

    public WindowPosition Position { get; set; } = new();

    /// <summary>首次运行标记，用于展示一次性的隐私说明，不影响监听状态。</summary>
    public bool PrivacyNoticeShown { get; set; }
}

public sealed class FloatingSettings
{
    /// <summary>悬浮窗不透明度，0.75–1.0。</summary>
    public double Opacity { get; set; } = 0.96;

    /// <summary>是否接管单按 Win（默认关闭）。开启后单按 Win 不会打开开始菜单。</summary>
    public bool TakeoverSingleWin { get; set; }

    /// <summary>
    /// 是否在已展开的悬浮窗中补充显示“无修饰键单键快捷键”（默认关闭）。
    /// 开启后也不会因为单按 F5/Delete 等自动弹出悬浮窗。
    /// </summary>
    public bool IncludeNoModifierKeys { get; set; }
}

public sealed class WindowPosition
{
    public bool HasValue { get; set; }

    public double Left { get; set; }

    public double Top { get; set; }
}
