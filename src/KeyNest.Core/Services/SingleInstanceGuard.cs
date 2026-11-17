using KeyNest.Core.Services;

namespace KeyNest.Core.Services;

/// <summary>
/// 单实例保护。互斥体名固定为 KeyNest.SingleInstance（当前用户会话内唯一）。
/// </summary>
public sealed class SingleInstanceGuard : IDisposable
{
    public const string MutexName = "KeyNest.SingleInstance";

    private Mutex? _mutex;

    public SingleInstanceGuard(string name = MutexName)
    {
        try
        {
            _mutex = new Mutex(initiallyOwned: true, name, out var createdNew);
            IsFirstInstance = createdNew;
        }
        catch (Exception ex)
        {
            Logger.Warn($"创建单实例互斥体失败，按首次实例处理：{ex.Message}");
            IsFirstInstance = true;
        }
    }

    public bool IsFirstInstance { get; }

    public void Dispose()
    {
        if (_mutex is null)
        {
            return;
        }

        try
        {
            if (IsFirstInstance)
            {
                _mutex.ReleaseMutex();
            }
        }
        catch
        {
            // 忽略释放失败。
        }
        finally
        {
            _mutex.Dispose();
            _mutex = null;
        }
    }
}
