namespace KeyNest.Core.Models;

/// <summary>一条快捷键说明数据，只用于展示，不包含任何执行逻辑。</summary>
public sealed class ShortcutItem
{
    public string Id { get; set; } = string.Empty;

    /// <summary>展示用的组合键文本，例如 “Win + Ctrl + D”。</summary>
    public string Display { get; set; } = string.Empty;

    /// <summary>修饰键名称数组，例如 ["Win", "Ctrl"]。</summary>
    public List<string> Modifiers { get; set; } = new();

    /// <summary>修饰键掩码，缺省时会由 Modifiers 推导。</summary>
    public int ModifierMask { get; set; }

    /// <summary>主键标识，例如 “D”“S”“Left”；空字符串表示该条目只由修饰键组成。</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>功能说明，支持 zh-CN / en-US。</summary>
    public LocalizedText Action { get; set; } = new();

    /// <summary>分类名，支持 zh-CN / en-US；排序始终使用中文键，保证语言切换不改变顺序。</summary>
    public LocalizedText Category { get; set; } = new();

    /// <summary>适用系统，例如 “Win10/Win11”“Win11”。</summary>
    public string Os { get; set; } = string.Empty;

    /// <summary>补充说明，支持 zh-CN / en-US。</summary>
    public LocalizedText Notes { get; set; } = new();

    [System.Text.Json.Serialization.JsonIgnore]
    public ModifierMask Mask => (KeyNest.Core.Models.ModifierMask)ModifierMask;

    [System.Text.Json.Serialization.JsonIgnore]
    public string ModifierLabel => Mask.ToLabel();

    [System.Text.Json.Serialization.JsonIgnore]
    public bool HasNotes => Notes.HasAny;

    public string ActionText(AppLanguage language) => Action.Get(language);

    public string CategoryKey => Category.ZhCn;

    public string NotesText(AppLanguage language) => Notes.Get(language);


    [System.Text.Json.Serialization.JsonIgnore]
    public bool HasOsBadge => !string.IsNullOrWhiteSpace(Os)
                              && !string.Equals(Os, "Win10/Win11", StringComparison.OrdinalIgnoreCase);
}
