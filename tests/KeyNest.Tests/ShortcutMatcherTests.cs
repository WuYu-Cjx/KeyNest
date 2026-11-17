using System.Linq;
using KeyNest.Core.Models;
using KeyNest.Core.Services;
using Xunit;

namespace KeyNest.Tests;

public class ShortcutMatcherTests
{
    private static ShortcutItem Item(string id, ModifierMask mask, string key = "D", string category = "系统")
        => new()
        {
            Id = id,
            Display = (mask == ModifierMask.None ? "无修饰键" : mask.ToLabel()) + " + " + key,
            ModifierMask = (int)mask,
            Modifiers = mask == ModifierMask.None
                ? new List<string>()
                : mask.ToLabel().Split('+').Select(s => s.Trim()).ToList(),
            Key = key,
            Action = LocalizedText.From("测试"),
            Category = LocalizedText.From(category),
            Os = "Win10/Win11"
        };

    private static ShortcutMatcher CreateMatcher() => new(new[]
    {
        Item("win-d", ModifierMask.Win),
        Item("win-ctrl-d", ModifierMask.Win | ModifierMask.Ctrl),
        Item("win-shift-s", ModifierMask.Win | ModifierMask.Shift, "S"),
        Item("win-ctrl-shift-b", ModifierMask.Win | ModifierMask.Ctrl | ModifierMask.Shift, "B"),
        Item("ctrl-shift-n", ModifierMask.Ctrl | ModifierMask.Shift, "N"),
        Item("ctrl-only-c", ModifierMask.Ctrl, "C"),
        Item("alt-tab", ModifierMask.Alt, "Tab"),
        Item("ctrl-alt-delete", ModifierMask.Ctrl | ModifierMask.Alt, "Delete"),
        Item("key-f5", ModifierMask.None, "F5")
    });

    [Fact]
    public void WinPrefix_ReturnsOnlyShortcutsContainingWin()
    {
        var sections = CreateMatcher().Match(ModifierMask.Win);
        var ids = sections.SelectMany(s => s.Items).Select(i => i.Id).ToList();

        Assert.Contains("win-d", ids);
        Assert.Contains("win-ctrl-d", ids);
        Assert.Contains("win-shift-s", ids);
        Assert.Contains("win-ctrl-shift-b", ids);
        Assert.DoesNotContain("ctrl-shift-n", ids);
        Assert.DoesNotContain("ctrl-only-c", ids);
        Assert.DoesNotContain("alt-tab", ids);
    }

    [Fact]
    public void CtrlPrefix_IncludesAllCombinationsContainingCtrl()
    {
        var sections = CreateMatcher().Match(ModifierMask.Ctrl);
        var ids = sections.SelectMany(s => s.Items).Select(i => i.Id).ToList();

        Assert.Contains("ctrl-only-c", ids);
        Assert.Contains("ctrl-shift-n", ids);
        Assert.Contains("win-ctrl-d", ids);
        Assert.Contains("ctrl-alt-delete", ids);
        Assert.DoesNotContain("alt-tab", ids);
        Assert.DoesNotContain("win-d", ids);

        // 完全等于 Ctrl 的分组必须排第一。
        Assert.Equal(ModifierMask.Ctrl, sections[0].Mask);
    }

    [Fact]
    public void AltPrefix_ExcludesOtherModifierOnlyShortcuts()
    {
        var sections = CreateMatcher().Match(ModifierMask.Alt);
        var ids = sections.SelectMany(s => s.Items).Select(i => i.Id).ToList();

        Assert.Contains("alt-tab", ids);
        Assert.Contains("ctrl-alt-delete", ids);
        Assert.DoesNotContain("ctrl-only-c", ids);
    }

    [Fact]
    public void CtrlAltPrefix_OnlyIncludesBothBits()
    {
        var sections = CreateMatcher().Match(ModifierMask.Ctrl | ModifierMask.Alt);
        var ids = sections.SelectMany(s => s.Items).Select(i => i.Id).ToList();

        Assert.Contains("ctrl-alt-delete", ids);
        Assert.DoesNotContain("ctrl-only-c", ids);
        Assert.DoesNotContain("alt-tab", ids);
        Assert.Equal(ModifierMask.Ctrl | ModifierMask.Alt, sections[0].Mask);
    }

    [Fact]
    public void WinPrefix_ExactGroupComesFirst()
    {
        var sections = CreateMatcher().Match(ModifierMask.Win);

        Assert.Equal(ModifierMask.Win, sections[0].Mask);
        Assert.Equal(0, sections[0].ExtraCount);
        Assert.True(sections.Zip(sections.Skip(1), (a, b) => a.ExtraCount <= b.ExtraCount).All(x => x));
    }

    [Fact]
    public void CtrlShiftPrefix_ExcludesWinOnlyAndCtrlOnly()
    {
        var sections = CreateMatcher().Match(ModifierMask.Ctrl | ModifierMask.Shift);
        var ids = sections.SelectMany(s => s.Items).Select(i => i.Id).ToList();

        Assert.Contains("ctrl-shift-n", ids);
        Assert.Contains("win-ctrl-shift-b", ids);
        Assert.DoesNotContain("win-d", ids);
        Assert.DoesNotContain("ctrl-only-c", ids);
        Assert.Equal(ModifierMask.Ctrl | ModifierMask.Shift, sections[0].Mask);
    }

    [Fact]
    public void NoModifierKeys_AreOnlyIncludedOnDemand()
    {
        var matcher = CreateMatcher();

        var without = matcher.Match(ModifierMask.Ctrl);
        Assert.DoesNotContain(without.SelectMany(s => s.Items), i => i.Id == "key-f5");

        var with = matcher.Match(ModifierMask.Ctrl, includeNoModifierKeys: true);
        var zeroSection = with.LastOrDefault();

        Assert.NotNull(zeroSection);
        Assert.Equal(ModifierMask.None, zeroSection!.Mask);
        Assert.Contains(zeroSection.Items, i => i.Id == "key-f5");
    }

    [Fact]
    public void MatchAll_ReturnsEveryGroupAndPutsNoModifierLast()
    {
        var sections = CreateMatcher().MatchAll(includeNoModifierKeys: true);

        Assert.True(sections.Count >= 6);
        Assert.Equal(ModifierMask.None, sections[^1].Mask);
        Assert.DoesNotContain(sections.Take(sections.Count - 1), s => s.Mask == ModifierMask.None);
    }

    [Fact]
    public void EmptyPrefix_ReturnsNothing()
    {
        Assert.Empty(CreateMatcher().Match(ModifierMask.None));
    }
}
