namespace KeyNest.Core.Services;

/// <summary>
/// 快捷键数据中的主键文本与 Windows 虚拟键码之间的映射（自研实现，仅覆盖数据文件用到的键）。
/// </summary>
public static class KeyNames
{
    private static readonly Dictionary<string, int> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Tab"] = 0x09,
        ["Enter"] = 0x0D,
        ["Backspace"] = 0x08,
        ["Esc"] = 0x1B,
        ["Escape"] = 0x1B,
        ["Space"] = 0x20,
        ["PageUp"] = 0x21,
        ["PageDown"] = 0x22,
        ["End"] = 0x23,
        ["Home"] = 0x24,
        ["Left"] = 0x25,
        ["Up"] = 0x26,
        ["Right"] = 0x27,
        ["Down"] = 0x28,
        ["PrintScreen"] = 0x2C,
        ["Insert"] = 0x2D,
        ["Delete"] = 0x2E,
        [","] = 0xBC,
        ["Comma"] = 0xBC,
        ["-"] = 0xBD,
        ["Minus"] = 0xBD,
        ["."] = 0xBE,
        ["Period"] = 0xBE,
        ["/"] = 0xBF,
        ["Slash"] = 0xBF,
        [";"] = 0xBA,
        ["Semicolon"] = 0xBA,
        ["Plus"] = 0xBB,
        ["Pause"] = 0x13
    };

    /// <summary>把数据文件中的键名转换为虚拟键码，未知返回 0。</summary>
    public static int ToVirtualKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return 0;
        }

        var text = key.Trim();
        if (Map.TryGetValue(text, out var known))
        {
            return known;
        }

        if (text.Length == 1)
        {
            var ch = char.ToUpperInvariant(text[0]);
            if (ch is >= 'A' and <= 'Z' or >= '0' and <= '9')
            {
                return ch;
            }
        }

        if (text.StartsWith('F') && int.TryParse(text.AsSpan(1), out var fn) && fn is >= 1 and <= 24)
        {
            return 0x70 + fn - 1;
        }

        return 0;
    }

    public static bool Matches(string? key, int virtualKey)
    {
        var mapped = ToVirtualKey(key);
        return mapped != 0 && mapped == virtualKey;
    }
}
