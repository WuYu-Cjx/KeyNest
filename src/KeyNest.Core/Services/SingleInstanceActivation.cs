using System;
using System.Threading;

namespace KeyNest.Core.Services;

/// <summary>
/// 单实例激活通道。第一个实例监听当前用户会话内的命名事件；
/// 后续实例（例如用户再次双击桌面快捷方式）只负责发出“打开设置”请求，然后静默退出。
/// 安装完成后的启动还会额外发出“启用监听”请求。不使用网络、不写注册表。
/// </summary>
public sealed class SingleInstanceActivation : IDisposable
{
    public const string EventName = "KeyNest.ShowSettings";
    public const string EnableEventName = "KeyNest.EnableListening";

    private readonly EventWaitHandle _showEvent;
    private readonly EventWaitHandle? _enableEvent;
    private readonly Thread _thread;
    private readonly Action _onActivated;
    private readonly Action? _onEnableRequested;

    private volatile bool _running = true;
    private bool _disposed;

    public SingleInstanceActivation(Action onActivated, Action? onEnableRequested = null)
    {
        _onActivated = onActivated ?? throw new ArgumentNullException(nameof(onActivated));
        _onEnableRequested = onEnableRequested;

        _showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, EventName);
        _enableEvent = onEnableRequested is null
            ? null
            : new EventWaitHandle(false, EventResetMode.AutoReset, EnableEventName);

        _thread = new Thread(Listen) { IsBackground = true, Name = "KeyNest.Activation" };
        _thread.Start();
    }

    /// <summary>
    /// 由后续实例调用：请求已运行的实例打开设置窗口；<paramref name="enableListening"/> 为 true 时同时启用监听。
    /// 返回是否成功送达（已有实例正在监听）。
    /// </summary>
    public static bool RequestActivation(bool enableListening = false)
    {
        var delivered = false;

        try
        {
            if (!EventWaitHandle.TryOpenExisting(EventName, out var handle))
            {
                return false;
            }

            using (handle)
            {
                handle.Set();
                delivered = true;
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"通知已运行实例失败：{ex.Message}");
        }

        if (!enableListening)
        {
            return delivered;
        }

        try
        {
            if (EventWaitHandle.TryOpenExisting(EnableEventName, out var enableHandle))
            {
                using (enableHandle)
                {
                    enableHandle.Set();
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"通知已运行实例启用监听失败：{ex.Message}");
        }

        return delivered;
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
            _showEvent.Set();
            _thread.Join(1000);
        }
        catch
        {
            // 退出阶段忽略清理异常。
        }

        _showEvent.Dispose();
        _enableEvent?.Dispose();
    }

    private void Listen()
    {
        var handles = _enableEvent is null
            ? new WaitHandle[] { _showEvent }
            : new WaitHandle[] { _showEvent, _enableEvent };

        while (_running)
        {
            // 500ms 超时用于响应退出请求，正常触发时立即返回。
            var index = WaitHandle.WaitAny(handles, 500);
            if (index == WaitHandle.WaitTimeout)
            {
                continue;
            }

            if (!_running)
            {
                return;
            }

            try
            {
                if (index == 0)
                {
                    _onActivated();
                }
                else
                {
                    _onEnableRequested?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"处理激活请求失败：{ex.Message}");
            }
        }
    }
}
