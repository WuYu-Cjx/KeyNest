using KeyNest.Core.Models;

namespace KeyNest.Core.Input;

/// <summary>
/// 修饰键状态机。左右键合并统计，同时按下的左右键不会互相覆盖。
/// 只在钩子线程内写入，UI 线程只读取快照值。
/// </summary>
public sealed class ModifierTracker
{
    private const int VkShift = 0x10;
    private const int VkControl = 0x11;
    private const int VkMenu = 0x12;
    private const int VkLShift = 0xA0;
    private const int VkRShift = 0xA1;
    private const int VkLControl = 0xA2;
    private const int VkRControl = 0xA3;
    private const int VkLMenu = 0xA4;
    private const int VkRMenu = 0xA5;
    private const int VkLWin = 0x5B;
    private const int VkRWin = 0x5C;

    // 虚拟键范围 0–255，用 4 个 ulong 组成 256 位位图。
    // 注意：左右修饰键的专用虚拟键（如 VK_LCONTROL=0xA2、VK_LSHIFT=0xA0、VK_LMENU=0xA4）
    // 都大于 127，必须覆盖完整 0–255 范围，否则 Ctrl/Shift/Alt 会被丢弃。
    private readonly ulong[] _down = new ulong[4];
    private int _mask;
    private readonly object _gate = new();

    public ModifierMask Current => (ModifierMask)Volatile.Read(ref _mask);

    public static bool IsWinKey(int vk) => vk is VkLWin or VkRWin;

    public static bool IsModifierVk(int vk) => vk switch
    {
        VkShift or VkControl or VkMenu => true,
        VkLShift or VkRShift => true,
        VkLControl or VkRControl => true,
        VkLMenu or VkRMenu => true,
        VkLWin or VkRWin => true,
        _ => false
    };

    /// <summary>
    /// 更新按键状态并返回修饰键掩码是否发生变化。
    /// 同一按键的自动重复（重复 KEYDOWN）不会重复计数。
    /// </summary>
    public bool Apply(int vk, bool isDown, out ModifierMask mask)
    {
        lock (_gate)
        {
            var before = _mask;
            if (isDown)
            {
                MarkDown(vk);
            }
            else
            {
                MarkUp(vk);
            }

            _mask = (int)ComputeMask();
            Volatile.Write(ref _mask, _mask);
            mask = (ModifierMask)_mask;
            return _mask != before;
        }
    }

    public void Reset()
    {
        lock (_gate)
        {
            Array.Clear(_down, 0, _down.Length);
            Volatile.Write(ref _mask, 0);
        }
    }

    private bool MarkDown(int vk)
    {
        if ((uint)vk > 0xFF)
        {
            return false;
        }

        var index = vk >> 6;
        var bit = 1UL << (vk & 63);
        if ((_down[index] & bit) != 0)
        {
            return false;
        }

        _down[index] |= bit;
        return true;
    }

    private void MarkUp(int vk)
    {
        if ((uint)vk > 0xFF)
        {
            return;
        }

        _down[vk >> 6] &= ~(1UL << (vk & 63));
    }

    private bool IsDown(int vk)
    {
        if ((uint)vk > 0xFF)
        {
            return false;
        }

        return (_down[vk >> 6] & (1UL << (vk & 63))) != 0;
    }

    private ModifierMask ComputeMask()
    {
        var mask = ModifierMask.None;

        if (IsDown(VkLControl) || IsDown(VkRControl) || IsDown(VkControl))
        {
            mask |= ModifierMask.Ctrl;
        }

        if (IsDown(VkLShift) || IsDown(VkRShift) || IsDown(VkShift))
        {
            mask |= ModifierMask.Shift;
        }

        if (IsDown(VkLMenu) || IsDown(VkRMenu) || IsDown(VkMenu))
        {
            mask |= ModifierMask.Alt;
        }

        if (IsDown(VkLWin) || IsDown(VkRWin))
        {
            mask |= ModifierMask.Win;
        }

        return mask;
    }
}
