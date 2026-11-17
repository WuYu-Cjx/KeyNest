using System;
using System.Windows.Forms;

namespace KeyNest.Core.Input;

/// <summary>
/// 钩子专用的隐藏消息接收窗。键盘回调只通过 PostMessage 把“修饰键状态变化”和“主键按下”
/// 两个轻量消息投递到 UI 线程；不在这里执行任何快捷键，也不保存按键历史。
/// </summary>
internal sealed class HookMessageWindow : NativeWindow
{
    internal const int WmModifierChanged = 0x8001;
    internal const int WmKeyPressed = 0x8002;

    internal event EventHandler<int>? ModifierChanged;

    internal event EventHandler<int>? KeyPressed;

    internal HookMessageWindow()
    {
        var parameters = new CreateParams
        {
            Caption = "KeyNest.HookMessageSink",
            ClassStyle = 0,
            Style = 0,
            ExStyle = 0,
            Parent = IntPtr.Zero,
            X = 0,
            Y = 0,
            Width = 1,
            Height = 1
        };

        CreateHandle(parameters);
    }

    internal IntPtr HandleSafe => Handle;

    internal void Close()
    {
        if (Handle != IntPtr.Zero)
        {
            DestroyHandle();
        }
    }

    protected override void WndProc(ref Message message)
    {
        switch (message.Msg)
        {
            case WmModifierChanged:
                ModifierChanged?.Invoke(this, message.LParam.ToInt32());
                return;

            case WmKeyPressed:
                KeyPressed?.Invoke(this, message.WParam.ToInt32());
                return;

            default:
                base.WndProc(ref message);
                return;
        }
    }
}
