namespace KeyNest.Core.Services;

/// <summary>
/// 极简日志，仅记录运行异常与生命周期事件，绝不记录按键内容或用户输入。
/// </summary>
public static class Logger
{
    private static readonly object Gate = new();

    /// <summary>诊断日志总开关，默认关闭；由配置中的 EnableLogging 控制。</summary>
    public static bool Enabled { get; set; }

    public static void Info(string message) => Write("INFO", message);

    public static void Warn(string message) => Write("WARN", message);

    public static void Error(string message, Exception? ex = null)
        => Write("ERROR", ex is null ? message : $"{message} :: {ex.GetType().Name}: {ex.Message}");

    private static void Write(string level, string message)
    {
        if (!Enabled)
        {
            return;
        }

        try
        {
            AppPaths.EnsureDataDirectory();
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}{Environment.NewLine}";

            lock (Gate)
            {
                var info = new FileInfo(AppPaths.LogFile);
                if (info.Exists && info.Length > 256 * 1024)
                {
                    File.Delete(AppPaths.LogFile);
                }

                File.AppendAllText(AppPaths.LogFile, line);
            }
        }
        catch
        {
            // 日志失败不影响主流程。
        }
    }
}
