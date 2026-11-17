namespace KeyNest.Core.Models;

/// <summary>
/// 修饰键位掩码。与 shortcuts.json 中的 modifierMask 字段一一对应：
/// Ctrl = 1，Shift = 2，Alt = 4，Win = 8。
/// 左右键在逻辑上合并，不区分 L/R。
/// </summary>
[Flags]
public enum ModifierMask
{
    None = 0,
    Ctrl = 1,
    Shift = 2,
    Alt = 4,
    Win = 8
}
