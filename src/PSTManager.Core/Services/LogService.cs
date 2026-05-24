using System.Runtime.InteropServices;

namespace PSTManager.Core.Services;

public class LogService
{
    private readonly string _logDir;
    private readonly string _logPath;
    private readonly object _lock = new();

    public LogService()
    {
        _logDir = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "pstm")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "pstm");

        Directory.CreateDirectory(_logDir);
        _logPath = Path.Combine(_logDir, "pstm.log");

        RotateIfNeeded();
        Info("=== PST Manager session started ===");
        Info($"Log file: {_logPath}");
        Info($"OS: {Environment.OSVersion}");
        Info($"Runtime: {Environment.Version}");
        Info($"Platform: {RuntimeInformation.OSDescription}");
        Info($"Architecture: {RuntimeInformation.ProcessArchitecture}");
    }

    public void Debug(string message)
    {
#if DEBUG
        Write("DEBUG", message);
#endif
    }

    public void Info(string message) => Write("INFO", message);

    public void Warn(string message) => Write("WARN", message);

    public void Error(string message) => Write("ERROR", message);

    public void Error(string message, Exception ex)
    {
        Write("ERROR", $"{message}: {ex.GetType().Name}: {ex.Message}");
        if (ex.StackTrace != null)
            Write("ERROR", $"  Stack: {ex.StackTrace}");
        if (ex.InnerException != null)
        {
            Write("ERROR", $"  Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            if (ex.InnerException.StackTrace != null)
                Write("ERROR", $"  Inner Stack: {ex.InnerException.StackTrace}");
        }
    }

    private void Write(string level, string message)
    {
        try
        {
            lock (_lock)
            {
                var line = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} [{level,-5}] {message}{Environment.NewLine}";
                File.AppendAllText(_logPath, line);
            }
        }
        catch
        {
        }
    }

    private void RotateIfNeeded()
    {
        try
        {
            if (!File.Exists(_logPath)) return;

            var info = new FileInfo(_logPath);
            if (info.Length > 5 * 1024 * 1024)
            {
                var backup = _logPath + ".old";
                if (File.Exists(backup)) File.Delete(backup);
                File.Move(_logPath, backup);
            }
        }
        catch
        {
        }
    }
}
