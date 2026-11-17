using System;
using System.IO;
using System.Linq;
using KeyNest.Core.Models;
using KeyNest.Core.Services;
using Xunit;

namespace KeyNest.Tests;

public class ShortcutDataTests
{
    private static ShortcutRepository Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "shortcuts.json");
        Assert.True(File.Exists(path), "未找到 shortcuts.json：" + path);
        return ShortcutRepository.LoadFile(path);
    }

    [Fact]
    public void Data_ContainsEnoughEntries()
    {
        Assert.True(Load().Items.Count >= 40);
    }

    [Fact]
    public void Data_IdsAreUnique()
    {
        var ids = Load().Items.Select(i => i.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void Data_MaskMatchesModifiers()
    {
        foreach (var item in Load().Items.Where(i => i.Modifiers.Count > 0))
        {
            var parsed = (int)ModifierMaskExtensions.Parse(string.Join(",", item.Modifiers));
            Assert.Equal(parsed, item.ModifierMask);
        }
    }

    [Fact]
    public void Data_CoversThreeKeyWinCombination()
    {
        var items = Load().Items;

        Assert.Contains(items, i => i.ModifierMask == (int)(ModifierMask.Win | ModifierMask.Ctrl) && i.Key == "D");
        Assert.Contains(items, i => i.ModifierMask == (int)(ModifierMask.Win | ModifierMask.Shift) && i.Key == "S");
        Assert.Contains(items, i => i.ModifierMask == (int)(ModifierMask.Win | ModifierMask.Ctrl | ModifierMask.Shift));
    }

    [Fact]
    public void Data_CoversSingleModifierGroups()
    {
        var items = Load().Items;

        Assert.Contains(items, i => i.ModifierMask == (int)ModifierMask.Ctrl);
        Assert.Contains(items, i => i.ModifierMask == (int)ModifierMask.Shift);
        Assert.Contains(items, i => i.ModifierMask == (int)ModifierMask.Alt);
        Assert.Contains(items, i => i.ModifierMask == (int)ModifierMask.Win);
    }

    [Fact]
    public void Data_CoversTwoModifierCombinations()
    {
        var items = Load().Items;

        Assert.Contains(items, i => i.ModifierMask == (int)(ModifierMask.Ctrl | ModifierMask.Alt));
        Assert.Contains(items, i => i.ModifierMask == (int)(ModifierMask.Ctrl | ModifierMask.Shift));
        Assert.Contains(items, i => i.ModifierMask == (int)(ModifierMask.Ctrl | ModifierMask.Win));
        Assert.Contains(items, i => i.ModifierMask == (int)(ModifierMask.Alt | ModifierMask.Shift));
        Assert.Contains(items, i => i.ModifierMask == (int)(ModifierMask.Alt | ModifierMask.Win));
        Assert.Contains(items, i => i.ModifierMask == (int)(ModifierMask.Shift | ModifierMask.Win));
    }

    [Fact]
    public void Data_CoversEveryModifierMaskFromZeroToFifteen()
    {
        var masks = Load().Items.Select(i => i.ModifierMask).ToHashSet();
        for (var mask = 0; mask <= 15; mask++)
        {
            Assert.True(masks.Contains(mask), $"shortcuts.json 缺少 modifierMask={mask} 的覆盖条目。");
        }
    }

    [Fact]
    public void Data_ContainsNoModifierSingleKeys()
    {
        var zero = Load().Items.Where(i => i.ModifierMask == 0).ToList();

        Assert.NotEmpty(zero);
        Assert.Contains(zero, i => i.Key == "F5");
        Assert.Contains(zero, i => i.Key == "Delete");
        Assert.All(zero, i => Assert.False(string.IsNullOrWhiteSpace(i.Key)));
    }

    [Fact]
    public void Data_EveryItemHasActionAndCategory()
    {
        foreach (var item in Load().Items)
        {
            Assert.True(item.Action.HasAny);
            Assert.True(item.Category.HasAny);
            Assert.False(string.IsNullOrWhiteSpace(item.Display));
        }
    }

    [Fact]
    public void Data_EveryItemHasBothLanguages()
    {
        foreach (var item in Load().Items)
        {
            Assert.False(string.IsNullOrWhiteSpace(item.Action.ZhCn));
            Assert.False(string.IsNullOrWhiteSpace(item.Action.EnUs));
            Assert.False(string.IsNullOrWhiteSpace(item.Category.ZhCn));
            Assert.False(string.IsNullOrWhiteSpace(item.Category.EnUs));
        }
    }
}
