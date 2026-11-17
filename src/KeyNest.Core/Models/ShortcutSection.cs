namespace KeyNest.Core.Models;

/// <summary>匹配结果中的一个分组，按“额外修饰键数量”升序排列。</summary>
public sealed class ShortcutSection
{
    public ShortcutSection(ModifierMask mask, int extraCount, IReadOnlyList<ShortcutItem> items)
    {
        Mask = mask;
        ExtraCount = extraCount;
        Items = items;
    }

    /// <summary>语言中立的组合标签，例如 “Ctrl+Alt”；无修饰键分组返回空字符串。</summary>
    public string Label => Mask.ToLabel();

    public ModifierMask Mask { get; }

    public int ExtraCount { get; }

    public IReadOnlyList<ShortcutItem> Items { get; }
}
