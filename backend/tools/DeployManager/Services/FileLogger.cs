using System.Diagnostics;
using System.IO;
using System.Text;

namespace DeployManager.Services;

/// <summary>
/// 简单的文件日志服务
/// 用于在生产环境中记录调试信息
/// </summary>
public static class FileLogger
{
    private static readonly object _lock = new();
    private static string? _logPath;

    /// <summary>
    /// 获取日志文件路径
    /// </summary>
    public static string LogPath
    {
        get
        {
            if (_logPath == null)
            {
                var appDir = AppDomain.CurrentDomain.BaseDirectory;
                var logsDir = Path.Combine(appDir, "logs");

                // 如果在 tools 子目录中，向上查找
                if (appDir.EndsWith("tools\\") || appDir.EndsWith("tools/") ||
                    appDir.EndsWith("manager\\") || appDir.EndsWith("manager/"))
                {
                    var parentDir = Directory.GetParent(appDir);
                    if (parentDir != null)
                    {
                        logsDir = Path.Combine(parentDir.FullName, "logs");
                    }
                }

                if (!Directory.Exists(logsDir))
                {
                    Directory.CreateDirectory(logsDir);
                }

                _logPath = Path.Combine(logsDir, $"deploymanager-{DateTime.Now:yyyyMMdd}.log");
            }
            return _logPath;
        }
    }

    /// <summary>
    /// 记录信息日志
    /// </summary>
    public static void Info(string message)
    {
        Log("INFO", message);
    }

    /// <summary>
    /// 记录警告日志
    /// </summary>
    public static void Warning(string message)
    {
        Log("WARN", message);
    }

    /// <summary>
    /// 记录错误日志
    /// </summary>
    public static void Error(string message, Exception? ex = null)
    {
        var fullMessage = ex != null ? $"{message} - Exception: {ex.Message}\n{ex.StackTrace}" : message;
        Log("ERROR", fullMessage);
    }

    /// <summary>
    /// 记录调试日志
    /// </summary>
    public static void Debug(string message)
    {
#if DEBUG
        Log("DEBUG", message);
#endif
        System.Diagnostics.Debug.WriteLine($"[DeployManager] {message}");
    }

    /// <summary>
    /// 写入日志
    /// </summary>
    private static void Log(string level, string message)
    {
        lock (_lock)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var logLine = $"[{timestamp}] [{level}] {message}\n";

                File.AppendAllText(LogPath, logLine, Encoding.UTF8);
            }
            catch
            {
                // 忽略日志写入失败
            }
        }
    }
}
