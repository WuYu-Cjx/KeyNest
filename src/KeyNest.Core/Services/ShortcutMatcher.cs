using KeyNest.Core.Models;

namespace KeyNest.Core.Services;

/// <summary>
/// 匹配引擎：给定“当前按下的修饰键集合 P”，筛出所有满足 (K &amp; P) == P 的快捷键 K。
/// 排序规则：完全等于 P 的分组优先，其次是只多一个修饰键的分组，再多一个的再次之。
/// 无修饰键单键（K = 0）永远不会因 P 非空而匹配，只能在开启设置时作为“补充分组”追加。
/// 完全不含执行逻辑，只做展示数据筛选。
/// </summary>
public sealed class ShortcutMatcher
{
    private static readonly Dictionary<string, int> CategoryOrder = new(StringComparer.OrdinalIgnoreCase)
    {
        ["窗口"] = 0,
        ["系统"] = 1,
        ["系统安全"] = 2,
        ["虚拟桌面"] = 3,
        ["截图与录屏"] = 4,
        ["编辑"] = 5,
        ["导航"] = 6,
        ["输入与辅助"] = 7,
        ["资源管理器"] = 8,
        ["应用"] = 9,
        ["其他"] = 99
    };

    /// <summary>
    /// 分组展示顺序：先按“额外修饰键数量”排序，同数量时按此表排列，
    /// 保证单键修饰符在前、两两组合居中、三键/四键组合靠后。
    /// </summary>
    private static readonly Dictionary<int, int> GroupRank = new()
    {
        [1] = 0,    // Ctrl
        [2] = 1,    // Shift
        [4] = 2,    // Alt
        [8] = 3,    // Win
        [5] = 4,    // Ctrl+Alt
        [3] = 5,    // Ctrl+Shift
        [9] = 6,    // Ctrl+Win
        [6] = 7,    // Alt+Shift
        [12] = 8,   // Alt+Win
        [10] = 9,   // Shift+Win
        [7] = 10,   // Ctrl+Alt+Shift
        [11] = 11,  // Win+Ctrl+Shift
        [13] = 12,  // Ctrl+Alt+Win
        [14] = 13,  // Alt+Shift+Win
        [15] = 14   // Win+Ctrl+Alt+Shift
    };

    private readonly IReadOnlyList<ShortcutItem> _items;

    public ShortcutMatcher(IEnumerable<ShortcutItem> items)
    {
        _items = items.ToList();
    }

    public int Count => _items.Count;

    /// <summary>
    /// 按当前修饰键集合筛选。
    /// includeNoModifierKeys 为 true 时，追加一个“无修饰键”分组（仅补充显示，不参与触发）。
    /// </summary>
    public IReadOnlyList<ShortcutSection> Match(ModifierMask prefix, bool includeNoModifierKeys = false)
    {
        if (prefix == ModifierMask.None)
        {
            return Array.Empty<ShortcutSection>();
        }

        var prefixBits = (int)prefix;
        var buckets = new Dictionary<int, List<ShortcutItem>>();

        foreach (var item in _items)
        {
            var mask = (int)item.Mask;

            var take = mask == 0
                ? includeNoModifierKeys
                : (mask & prefixBits) == prefixBits;

            if (!take)
            {
                continue;
            }

            AddToBucket(buckets, mask, item);
        }

        return BuildSections(buckets, prefix);
    }

    /// <summary>全量概览（托盘手动显示时使用），按分组排列。</summary>
    public IReadOnlyList<ShortcutSection> MatchAll(bool includeNoModifierKeys = true)
    {
        var buckets = new Dictionary<int, List<ShortcutItem>>();

        foreach (var item in _items)
        {
            var mask = (int)item.Mask;

            if (mask == 0 && !includeNoModifierKeys)
            {
                continue;
            }

            AddToBucket(buckets, mask, item);
        }

        return BuildSections(buckets, prefix: null);
    }

    private static void AddToBucket(Dictionary<int, List<ShortcutItem>> buckets, int mask, ShortcutItem item)
    {
        if (!buckets.TryGetValue(mask, out var list))
        {
            list = new List<ShortcutItem>();
            buckets[mask] = list;
        }

        list.Add(item);
    }

    private static IReadOnlyList<ShortcutSection> BuildSections(
        Dictionary<int, List<ShortcutItem>> buckets,
        ModifierMask? prefix)
    {
        var sections = new List<ShortcutSection>(buckets.Count);

        var ordered = buckets
            .Where(pair => pair.Value.Count > 0)
            .OrderBy(pair => pair.Key == 0 ? 1 : 0)                 // 无修饰键分组永远在最后
            .ThenBy(pair => prefix is null ? 0 : ((ModifierMask)pair.Key).ExtraCount(prefix.Value))
            .ThenBy(pair => Rank(pair.Key))
            .ThenBy(pair => pair.Key);

        foreach (var pair in ordered)
        {
            var mask = (ModifierMask)pair.Key;
            var extra = prefix is null ? 0 : mask.ExtraCount(prefix.Value);

            sections.Add(new ShortcutSection(mask, extra, OrderItems(pair.Value)));
        }

        return sections;
    }

    private static List<ShortcutItem> OrderItems(List<ShortcutItem> items)
    {
        return items
            .OrderBy(item => CategoryOrder.TryGetValue(item.CategoryKey, out var order) ? order : 50)
            .ThenBy(item => item.Display, StringComparer.CurrentCulture)
            .ToList();
    }

    private static int Rank(int mask) => GroupRank.TryGetValue(mask, out var rank) ? rank : 99;
}
