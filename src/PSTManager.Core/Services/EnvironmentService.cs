using System.Runtime.InteropServices;

namespace PSTManager.Core.Services;

public class EnvironmentService
{
    private readonly LogService _log;

    public EnvironmentService(LogService log)
    {
        _log = log;
    }

    public bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    public bool IsMacOS => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    public string GetDataDirectory()
    {
        if (IsWindows)
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "palworldsavetools");
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".local", "share", "palworldsavetools");
    }

    public string GetLauncherPath(string dataDir)
    {
        return IsWindows
            ? Path.Combine(dataDir, "pst.ps1")
            : Path.Combine(dataDir, "pst");
    }

    public string? FindUvBinary()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        string[] candidates = IsWindows
            ? new[]
            {
                Path.Combine(home, ".local", "bin", "uv.exe"),
                Path.Combine(home, ".cargo", "bin", "uv.exe"),
            }
            : new[]
            {
                Path.Combine(home, ".local", "bin", "uv"),
                Path.Combine(home, ".cargo", "bin", "uv"),
            };

        foreach (var path in candidates)
        {
            if (File.Exists(path))
            {
                _log.Info($"FindUvBinary: found at {path}");
                return path;
            }
        }

        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = IsWindows ? "where" : "which",
                Arguments = "uv",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = System.Diagnostics.Process.Start(psi);
            if (proc != null)
            {
                proc.WaitForExit();
                if (proc.ExitCode == 0)
                {
                    var output = proc.StandardOutput.ReadLine()?.Trim();
                    if (!string.IsNullOrEmpty(output))
                    {
                        _log.Info($"FindUvBinary: found via which at {output}");
                        return output;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _log.Warn($"FindUvBinary: which/where failed: {ex.Message}");
        }

        _log.Warn("FindUvBinary: uv not found");
        return null;
    }

    public async Task EnsureUvInstalled(IProgress<string>? progress = null)
    {
        _log.Info("EnsureUvInstalled: checking for uv");

        if (FindUvBinary() != null)
        {
            _log.Info("EnsureUvInstalled: uv found");
            return;
        }

        _log.Info("EnsureUvInstalled: uv not found, installing...");
        progress?.Report("Installing uv...");

        try
        {
            if (IsWindows)
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = "-ExecutionPolicy ByPass -c \"irm https://astral.sh/uv/install.ps1 | iex\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var proc = System.Diagnostics.Process.Start(psi);
                if (proc != null) await proc.WaitForExitAsync();
            }
            else
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = "-c \"curl -LsSf https://astral.sh/uv/install.sh | sh\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var proc = System.Diagnostics.Process.Start(psi);
                if (proc != null) await proc.WaitForExitAsync();
            }

            _log.Info("EnsureUvInstalled: install complete");
        }
        catch (Exception ex)
        {
            _log.Error("EnsureUvInstalled: install failed", ex);
            throw;
        }

        progress?.Report("uv installed");
    }
}
