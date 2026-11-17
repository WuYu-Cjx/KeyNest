using System.Text.Json;
using KeyNest.Core.Models;

namespace KeyNest.Core.Services;

public sealed class ShortcutFileData
{
    public int SchemaVersion { get; set; } = 1;

    public List<ShortcutItem> Items { get; set; } = new();
}

/// <summary>
/// 快捷键数据仓库。启动时一次性读入内存，后续查询全部走内存索引，保证悬浮窗无卡顿。
/// </summary>
public sealed class ShortcutRepository
{
    private ShortcutRepository(IReadOnlyList<ShortcutItem> items)
    {
        Items = items;
    }

    public IReadOnlyList<ShortcutItem> Items { get; }

    public static ShortcutRepository Empty { get; } = new(Array.Empty<ShortcutItem>());

    public static ShortcutRepository LoadFile(string path)
    {
        var json = File.ReadAllText(path);
        return Parse(json, path);
    }

    public static ShortcutRepository LoadStream(Stream stream, string sourceName)
    {
        using var reader = new StreamReader(stream);
        return Parse(reader.ReadToEnd(), sourceName);
    }

    public static ShortcutRepository Parse(string json, string sourceName = "inline")
    {
        try
        {
            var data = JsonStore.Deserialize<ShortcutFileData>(json);
            if (data?.Items is null)
            {
                Logger.Warn($"快捷键数据为空：{sourceName}");
                return Empty;
            }

            var normalized = Normalize(data.Items);
            Logger.Info($"已加载快捷键数据 {normalized.Count} 条（{sourceName}）");
            return new ShortcutRepository(normalized);
        }
        catch (JsonException ex)
        {
            Logger.Error($"快捷键数据解析失败：{sourceName}", ex);
            return Empty;
        }
    }

    private static List<ShortcutItem> Normalize(IEnumerable<ShortcutItem> items)
    {
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<ShortcutItem>();

        foreach (var item in items)
        {
            if (item is null || string.IsNullOrWhiteSpace(item.Id) || string.IsNullOrWhiteSpace(item.Display))
            {
                continue;
            }

            if (!seenIds.Add(item.Id))
            {
                Logger.Warn($"快捷键数据存在重复 Id，已忽略：{item.Id}");
                continue;
            }

            item.Modifiers ??= new List<string>();
            item.Action ??= new LocalizedText();
            item.Category ??= new LocalizedText();
            item.Notes ??= new LocalizedText();
            item.Key ??= string.Empty;
            item.Os ??= string.Empty;

            if (!item.Action.HasAny)
            {
                item.Action = LocalizedText.From(item.Display);
            }

            if (!item.Category.HasAny)
            {
                item.Category = new LocalizedText { ZhCn = "其他", EnUs = "Other" };
            }

            if (item.ModifierMask == 0 && item.Modifiers.Count > 0)
            {
                item.ModifierMask = (int)ModifierMaskExtensions.Parse(string.Join(",", item.Modifiers));
            }

            if (item.ModifierMask == 0 && string.IsNullOrWhiteSpace(item.Key))
            {
                Logger.Warn($"快捷键数据缺少修饰键与主键，已忽略：{item.Id}");
                continue;
            }

            result.Add(item);
        }

        return result
            .OrderBy(i => i.ModifierMask)
            .ThenBy(i => i.CategoryKey, StringComparer.OrdinalIgnoreCase)
            .ThenBy(i => i.Display, StringComparer.CurrentCulture)
            .ToList();
    }
}
