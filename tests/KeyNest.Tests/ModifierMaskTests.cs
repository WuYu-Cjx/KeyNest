using KeyNest.Core.Models;
using KeyNest.Core.Services;
using Xunit;

namespace KeyNest.Tests;

public class ModifierMaskTests
{
    [Fact]
    public void ToLabel_UsesCanonicalOrder()
    {
        Assert.Equal("Ctrl", ModifierMask.Ctrl.ToLabel());
        Assert.Equal("Win", ModifierMask.Win.ToLabel());
        Assert.Equal("Ctrl+Alt", (ModifierMask.Ctrl | ModifierMask.Alt).ToLabel());
        Assert.Equal("Ctrl+Win", (ModifierMask.Ctrl | ModifierMask.Win).ToLabel());
        Assert.Equal("Alt+Shift", (ModifierMask.Alt | ModifierMask.Shift).ToLabel());
        Assert.Equal("Ctrl+Alt+Shift", (ModifierMask.Ctrl | ModifierMask.Alt | ModifierMask.Shift).ToLabel());
        Assert.Equal("Win+Ctrl+Shift", (ModifierMask.Ctrl | ModifierMask.Shift | ModifierMask.Win).ToLabel());
        Assert.Equal("Win+Ctrl+Alt+Shift",
            (ModifierMask.Ctrl | ModifierMask.Alt | ModifierMask.Shift | ModifierMask.Win).ToLabel());
    }

    [Fact]
    public void Parse_RecognizesAliases()
    {
        Assert.Equal(ModifierMask.Ctrl | ModifierMask.Shift, ModifierMaskExtensions.Parse("ctrl, shift"));
        Assert.Equal(ModifierMask.Win, ModifierMaskExtensions.Parse("Windows"));
        Assert.Equal(ModifierMask.None, ModifierMaskExtensions.Parse("hyper"));
    }

    [Fact]
    public void ContainsAll_RequiresEveryBit()
    {
        var state = ModifierMask.Ctrl | ModifierMask.Shift | ModifierMask.Win;

        Assert.True(state.ContainsAll(ModifierMask.Ctrl | ModifierMask.Shift));
        Assert.False(ModifierMask.Ctrl.ContainsAll(ModifierMask.Ctrl | ModifierMask.Shift));
    }

    [Fact]
    public void ExtraCount_IsZeroForExactMatch()
    {
        Assert.Equal(0, (ModifierMask.Win | ModifierMask.Ctrl).ExtraCount(ModifierMask.Win | ModifierMask.Ctrl));
        Assert.Equal(1, (ModifierMask.Win | ModifierMask.Ctrl).ExtraCount(ModifierMask.Win));
    }
}
