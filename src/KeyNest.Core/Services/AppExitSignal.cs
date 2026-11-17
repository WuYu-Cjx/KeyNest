using System;
using System.Threading;

namespace KeyNest.Core.Services;

/// <summary>
/// 退出信号通道。卸载器在删除文件前发出请求，运行中的键巢收到后走正常退出流程，
/// 释放托盘图标与键盘钩子；若进程无响应，卸载器再回退到强制结束。
/// 事件仅在当前用户会话内有效，不使用网络、不写注册表。
/// </summary>
public sealed class AppExitSignal : IDisposable
{
    public const string EventName = "KeyNest.Exit";

    private readonly EventWaitHandle _event;
    private readonly Thread _thread;
    private readonly Action _onExit;
    private volatile bool _running = true;
    private bool _disposed;

    public AppExitSignal(Action onExit)
    {
        _onExit = onExit ?? throw new ArgumentNullException(nameof(onExit));
        _event = new EventWaitHandle(false, EventResetMode.AutoReset, EventName);
        _thread = new Thread(Listen) { IsBackground = true, Name = "KeyNest.ExitSignal" };
        _thread.Start();
    }

    /// <summary>由卸载器调用：请求正在运行的键巢退出。返回是否有实例接收。</summary>
    public static bool RequestExit()
    {
        try
        {
            if (!EventWaitHandle.TryOpenExisting(EventName, out var handle))
            {
                return false;
            }

            using (handle)
            {
                handle.Set();
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.Warn($"发送退出请求失败：{ex.Message}");
            return false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _running = false;

        try
        {
            _event.Set();
            _thread.Join(1000);
        }
        catch
        {
            // 退出阶段忽略清理异常。
        }

        _event.Dispose();
    }

    private void Listen()
    {
        while (_running)
        {
            if (!_event.WaitOne(500))
            {
                continue;
            }

            if (!_running)
            {
                return;
            }

            try
            {
                _onExit();
            }
            catch (Exception ex)
            {
                Logger.Warn($"处理退出请求失败：{ex.Message}");
            }
        }
    }
}
