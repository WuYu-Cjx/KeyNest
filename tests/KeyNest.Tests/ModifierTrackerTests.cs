using KeyNest.Core.Input;
using KeyNest.Core.Models;
using Xunit;

namespace KeyNest.Tests;

/// <summary>
/// 修饰键状态机回归测试。
/// 重点覆盖左右修饰键的专用虚拟键（0xA0–0xA5，均大于 127），
/// 这些值曾因位图只覆盖 0–127 而被丢弃，导致只有 Win 键能触发悬浮窗。
/// </summary>
public class ModifierTrackerTests
{
    private const int VkLShift = 0xA0;
    private const int VkRShift = 0xA1;
    private const int VkLControl = 0xA2;
    private const int VkRControl = 0xA3;
    private const int VkLMenu = 0xA4;
    private const int VkRMenu = 0xA5;
    private const int VkLWin = 0x5B;
    private const int VkRWin = 0x5C;

    [Theory]
    [InlineData(VkLControl, ModifierMask.Ctrl)]
    [InlineData(VkRControl, ModifierMask.Ctrl)]
    [InlineData(VkLShift, ModifierMask.Shift)]
    [InlineData(VkRShift, ModifierMask.Shift)]
    [InlineData(VkLMenu, ModifierMask.Alt)]
    [InlineData(VkRMenu, ModifierMask.Alt)]
    [InlineData(VkLWin, ModifierMask.Win)]
    [InlineData(VkRWin, ModifierMask.Win)]
    public void SingleModifier_PressAndRelease(int vk, ModifierMask expected)
    {
        var tracker = new ModifierTracker();

        Assert.True(tracker.Apply(vk, true, out var pressed));
        Assert.Equal(expected, pressed);

        Assert.True(tracker.Apply(vk, false, out var released));
        Assert.Equal(ModifierMask.None, released);
    }

    [Fact]
    public void LeftAndRight_PressingBothAndReleasingOne_KeepsMask()
    {
        var tracker = new ModifierTracker();

        tracker.Apply(VkLControl, true, out _);
        Assert.False(tracker.Apply(VkRControl, true, out var both));
        Assert.Equal(ModifierMask.Ctrl, both);

        Assert.False(tracker.Apply(VkLControl, false, out var still));
        Assert.Equal(ModifierMask.Ctrl, still);
    }

    [Fact]
    public void CombinedModifiers_AreMerged()
    {
        var tracker = new ModifierTracker();

        tracker.Apply(VkLControl, true, out _);
        tracker.Apply(VkLMenu, true, out var state);

        Assert.Equal(ModifierMask.Ctrl | ModifierMask.Alt, state);
    }

    [Fact]
    public void AutoRepeat_DoesNotReportChange()
    {
        var tracker = new ModifierTracker();

        Assert.True(tracker.Apply(VkLShift, true, out _));
        Assert.False(tracker.Apply(VkLShift, true, out var state));
        Assert.Equal(ModifierMask.Shift, state);
    }

    [Fact]
    public void Reset_ClearsAllModifiers()
    {
        var tracker = new ModifierTracker();

        tracker.Apply(VkLControl, true, out _);
        tracker.Apply(VkLWin, true, out _);
        tracker.Reset();

        Assert.Equal(ModifierMask.None, tracker.Current);
    }
}
