using System.Numerics;

namespace KeyNest.Core.Models;

public static class ModifierMaskExtensions
{
    /// <summary>返回掩码中修饰键的个数。</summary>
    public static int Count(this ModifierMask mask) => BitOperations.PopCount((uint)(int)mask);

    /// <summary>判断当前修饰键集合是否完整包含 required。</summary>
    public static bool ContainsAll(this ModifierMask mask, ModifierMask required)
        => ((int)mask & (int)required) == (int)required;

    /// <summary>两个集合的差集个数，用于“多一个修饰键”的排序权重。</summary>
    public static int ExtraCount(this ModifierMask mask, ModifierMask required)
        => mask.Count() - required.Count();

    /// <summary>
    /// 生成分组标题。单修饰键与两两组合使用 Ctrl/Alt/Shift/Win 顺序；
    /// 三键与四键组合使用需求约定的展示文案，例如 “Ctrl+Alt+Shift”“Win+Ctrl+Shift”
    /// “Win+Ctrl+Alt+Shift”。未知组合回退为通用拼接。
    /// </summary>
    public static string ToLabel(this ModifierMask mask)
    {
        switch (mask)
        {
            case ModifierMask.None: return string.Empty;
            case ModifierMask.Ctrl: return "Ctrl";
            case ModifierMask.Shift: return "Shift";
            case ModifierMask.Ctrl | ModifierMask.Shift: return "Ctrl+Shift";
            case ModifierMask.Alt: return "Alt";
            case ModifierMask.Ctrl | ModifierMask.Alt: return "Ctrl+Alt";
            case ModifierMask.Alt | ModifierMask.Shift: return "Alt+Shift";
            case ModifierMask.Ctrl | ModifierMask.Alt | ModifierMask.Shift: return "Ctrl+Alt+Shift";
            case ModifierMask.Win: return "Win";
            case ModifierMask.Ctrl | ModifierMask.Win: return "Ctrl+Win";
            case ModifierMask.Shift | ModifierMask.Win: return "Shift+Win";
            case ModifierMask.Ctrl | ModifierMask.Shift | ModifierMask.Win: return "Win+Ctrl+Shift";
            case ModifierMask.Alt | ModifierMask.Win: return "Alt+Win";
            case ModifierMask.Ctrl | ModifierMask.Alt | ModifierMask.Win: return "Ctrl+Alt+Win";
            case ModifierMask.Alt | ModifierMask.Shift | ModifierMask.Win: return "Alt+Shift+Win";
            case ModifierMask.Ctrl | ModifierMask.Alt | ModifierMask.Shift | ModifierMask.Win:
                return "Win+Ctrl+Alt+Shift";
        }

        var parts = new List<string>(4);
        if (mask.HasFlag(ModifierMask.Ctrl)) parts.Add("Ctrl");
        if (mask.HasFlag(ModifierMask.Alt)) parts.Add("Alt");
        if (mask.HasFlag(ModifierMask.Shift)) parts.Add("Shift");
        if (mask.HasFlag(ModifierMask.Win)) parts.Add("Win");
        return string.Join("+", parts);
    }

    /// <summary>把逗号/加号分隔的修饰键名解析为掩码，无法识别返回 None。</summary>
    public static ModifierMask Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return ModifierMask.None;
        }

        var result = ModifierMask.None;
        foreach (var raw in text.Split(new[] { ',', '+', ' ', '|' }, StringSplitOptions.RemoveEmptyEntries))
        {
            result |= raw.Trim().ToLowerInvariant() switch
            {
                "ctrl" or "control" => ModifierMask.Ctrl,
                "shift" => ModifierMask.Shift,
                "alt" or "menu" => ModifierMask.Alt,
                "win" or "windows" or "meta" => ModifierMask.Win,
                _ => ModifierMask.None
            };
        }

        return result;
    }
}
