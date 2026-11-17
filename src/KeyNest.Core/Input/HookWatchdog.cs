using KeyNest.Core.Interop;
using KeyNest.Core.Services;

namespace KeyNest.Core.Input;

/// <summary>
/// 钩子看门狗：低级键盘钩子可能被系统或其他安全软件静默移除，
/// 回调本身只 PostMessage；这里每 5 秒检查一次，发现钩子丢失或长时间收不到回调但系统仍在活动时重装钩子。
/// 重装动作必须回到安装钩子的 UI 线程执行（低级钩子需要该线程有消息循环）。
/// </summary>
public sealed class HookWatchdog : IDisposable
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan StaleThreshold = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan ActiveWindow = TimeSpan.FromSeconds(45);

    private readonly KeyboardHook _hook;
    private readonly SynchronizationContext? _context;
    private readonly System.Threading.Timer _timer;
    private int _checking;
    private bool _disposed;

    public HookWatchdog(KeyboardHook hook, SynchronizationContext? context = null)
    {
        _hook = hook ?? throw new ArgumentNullException(nameof(hook));
        _context = context;
        _timer = new System.Threading.Timer(Check, null, Timeout.Infinite, Timeout.Infinite);
    }

    public void Start()
    {
        if (_disposed)
        {
            return;
        }

        _timer.Change(Interval, Interval);
    }

    public void Stop()
    {
        if (_disposed)
        {
            return;
        }

        _timer.Change(Timeout.Infinite, Timeout.Infinite);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _timer.Dispose();
    }

    private void Check(object? state)
    {
        if (_disposed || Interlocked.Exchange(ref _checking, 1) == 1)
        {
            return;
        }

        try
        {
            if (!_hook.IsHooked)
            {
                RequestReinstall("检测到钩子句柄丢失");
                return;
            }

            var idle = GetIdleTime();
            if (idle < ActiveWindow && DateTime.UtcNow - _hook.LastCallbackUtc > StaleThreshold)
            {
                RequestReinstall("检测到钩子长时间无回调，疑似被系统移除");
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"钩子看门狗检查失败：{ex.Message}");
        }
        finally
        {
            Interlocked.Exchange(ref _checking, 0);
        }
    }

    private void RequestReinstall(string reason)
    {
        Logger.Warn($"重装键盘钩子：{reason}");

        if (_context is not null)
        {
            _context.Post(_ => _hook.Reinstall(), null);
        }
        else
        {
            _hook.Reinstall();
        }
    }

    private static TimeSpan GetIdleTime()
    {
        var info = new NativeMethods.LastInputInfo
        {
            CbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.LastInputInfo>()
        };

        if (!NativeMethods.GetLastInputInfo(ref info))
        {
            return TimeSpan.Zero;
        }

        var idleMs = unchecked(Environment.TickCount - (int)info.DwTime);
        if (idleMs < 0)
        {
            idleMs = 0;
        }

        return TimeSpan.FromMilliseconds(idleMs);
    }
}
