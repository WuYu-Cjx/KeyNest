using System.Runtime.InteropServices;
using KeyNest.Core.Interop;
using KeyNest.Core.Models;
using KeyNest.Core.Services;

namespace KeyNest.Core.Input;

/// <summary>
/// 自研 WH_KEYBOARD_LL 低级键盘钩子封装。
/// 回调中只做三件事：更新修饰键状态、通过 PostMessage 向隐藏消息窗投递轻量事件、
/// 按需（且仅在用户显式开启时）拦截单按 Win。其余情况一律 CallNextHookEx。
/// 不读取字符内容、不写盘、不联网、不保存按键历史。
/// </summary>
public sealed class KeyboardHook : IDisposable
{
    private readonly NativeMethods.LowLevelKeyboardProc _proc;
    private readonly ModifierTracker _tracker = new();
    private readonly HookMessageWindow _messageWindow;

    private IntPtr _handle;
    private bool _disposed;
    private bool _soloWinCandidate;

    public KeyboardHook()
    {
        _proc = HookCallback;
        _messageWindow = new HookMessageWindow();
        _messageWindow.ModifierChanged += OnModifierMessage;
        _messageWindow.KeyPressed += OnKeyMessage;
    }

    public event EventHandler<ModifierStateChangedEventArgs>? ModifierStateChanged;

    public event EventHandler<KeyPressedEventArgs>? KeyPressed;

    /// <summary>可选：接管单按 Win。默认 false，系统开始菜单行为完全不受影响。</summary>
    public bool TakeoverSingleWin { get; set; }

    public bool IsHooked => _handle != IntPtr.Zero;

    public DateTime LastCallbackUtc { get; private set; } = DateTime.MinValue;

    public ModifierMask CurrentModifiers => _tracker.Current;

    public bool Install()
    {
        if (_disposed)
        {
            return false;
        }

        if (IsHooked)
        {
            return true;
        }

        _tracker.Reset();
        _soloWinCandidate = false;

        _handle = NativeMethods.SetWindowsHookExW(
            NativeMethods.WH_KEYBOARD_LL,
            _proc,
            NativeMethods.GetModuleHandleW(null),
            0);

        if (_handle == IntPtr.Zero)
        {
            Logger.Error($"安装键盘钩子失败，Win32 错误码：{Marshal.GetLastWin32Error()}");
            return false;
        }

        Logger.Info("键盘钩子已安装");
        return true;
    }

    public void Uninstall()
    {
        if (!IsHooked)
        {
            return;
        }

        if (NativeMethods.UnhookWindowsHookEx(_handle))
        {
            Logger.Info("键盘钩子已卸载");
        }
        else
        {
            Logger.Warn($"卸载键盘钩子返回失败，错误码：{Marshal.GetLastWin32Error()}");
        }

        _handle = IntPtr.Zero;
        _tracker.Reset();
        _soloWinCandidate = false;
    }

    public void Reinstall()
    {
        Uninstall();
        Install();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Uninstall();
        _messageWindow.ModifierChanged -= OnModifierMessage;
        _messageWindow.KeyPressed -= OnKeyMessage;
        _messageWindow.Close();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode < 0)
        {
            return NativeMethods.CallNextHookEx(_handle, nCode, wParam, lParam);
        }

        var suppress = false;

        try
        {
            LastCallbackUtc = DateTime.UtcNow;

            var message = wParam.ToInt32();
            var isDown = message is NativeMethods.WM_KEYDOWN or NativeMethods.WM_SYSKEYDOWN;
            var isUp = message is NativeMethods.WM_KEYUP or NativeMethods.WM_SYSKEYUP;

            if (isDown || isUp)
            {
                var data = Marshal.PtrToStructure<NativeMethods.KbdLlHookStruct>(lParam);
                var vk = (int)data.VkCode;
                var isModifier = ModifierTracker.IsModifierVk(vk);

                if (_tracker.Apply(vk, isDown, out var state))
                {
                    PostModifierState(state);
                }

                if (isDown)
                {
                    if (!isModifier)
                    {
                        _soloWinCandidate = false;
                        PostKeyPressed(vk);
                    }
                    else if (!ModifierTracker.IsWinKey(vk))
                    {
                        _soloWinCandidate = false;
                    }
                    else
                    {
                        // 只有“从按下 Win 开始，期间没有按过其它键”才算单按 Win。
                        _soloWinCandidate = true;
                    }
                }
                else if (isUp && ModifierTracker.IsWinKey(vk))
                {
                    if (_soloWinCandidate && TakeoverSingleWin)
                    {
                        // 仅拦截 Win 抬起事件，阻止开始菜单弹出；系统快捷键（Win+R/Win+D 等）不受影响。
                        suppress = true;
                        Logger.Info("已接管单按 Win（本次抬起被拦截）");
                    }

                    _soloWinCandidate = false;
                }
                else if (isUp && !isModifier)
                {
                    _soloWinCandidate = false;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error("键盘钩子回调异常", ex);
        }

        return suppress
            ? new IntPtr(1)
            : NativeMethods.CallNextHookEx(_handle, nCode, wParam, lParam);
    }

    private void PostModifierState(ModifierMask state)
    {
        NativeMethods.PostMessage(
            _messageWindow.HandleSafe,
            HookMessageWindow.WmModifierChanged,
            IntPtr.Zero,
            new IntPtr((int)state));
    }

    private void PostKeyPressed(int virtualKey)
    {
        NativeMethods.PostMessage(
            _messageWindow.HandleSafe,
            HookMessageWindow.WmKeyPressed,
            new IntPtr(virtualKey),
            new IntPtr(1));
    }

    private void OnModifierMessage(object? sender, int mask)
    {
        ModifierStateChanged?.Invoke(this, new ModifierStateChangedEventArgs((ModifierMask)mask));
    }

    private void OnKeyMessage(object? sender, int virtualKey)
    {
        KeyPressed?.Invoke(this, new KeyPressedEventArgs(virtualKey));
    }
}
