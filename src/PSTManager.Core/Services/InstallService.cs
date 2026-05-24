using System.IO.Compression;
using PSTManager.Core.Enums;
using PSTManager.Core.Models;

namespace PSTManager.Core.Services;

public class InstallService
{
    private readonly EnvironmentService _environment;
    private readonly ConfigService _config;
    private readonly LogService _log;

    public InstallService(EnvironmentService environment, ConfigService config, LogService log)
    {
        _environment = environment;
        _config = config;
        _log = log;
    }

    public InstallationInfo CheckInstallation()
    {
        var dataDir = _environment.GetDataDirectory();
        var sourceDir = Path.Combine(dataDir, "source");

        if (!Directory.Exists(sourceDir))
        {
            _log.Debug($"CheckInstallation: source dir not found at {sourceDir}");
            return new InstallationInfo
            {
                IsInstalled = false,
                DataDirectory = dataDir
            };
        }

        var branch = _config.GetSavedBranch(dataDir);
        _log.Info($"CheckInstallation: installed, branch={branch}");
        return new InstallationInfo
        {
            IsInstalled = true,
            InstalledVersion = "installed",
            InstalledBranch = branch,
            DataDirectory = dataDir
        };
    }

    public async Task InstallAsync(
        string zipPath,
        BranchType branch,
        IProgress<string>? statusProgress = null,
        CancellationToken ct = default)
    {
        var dataDir = _environment.GetDataDirectory();
        var sourceDir = Path.Combine(dataDir, "source");
        var extractTemp = Path.Combine(Path.GetTempPath(), "PST_extract");

        _log.Info($"InstallAsync: zip={zipPath}, branch={branch}, dataDir={dataDir}");

        statusProgress?.Report("Extracting archive...");

        if (Directory.Exists(extractTemp))
        {
            Directory.Delete(extractTemp, true);
        }
        Directory.CreateDirectory(extractTemp);

        _log.Info($"InstallAsync: extracting to {extractTemp}");
        ZipFile.ExtractToDirectory(zipPath, extractTemp);

        statusProgress?.Report("Preparing installation directory...");

        _log.Info("InstallAsync: removing old data dir");
        if (Directory.Exists(dataDir))
        {
            Directory.Delete(dataDir, true);
        }
        Directory.CreateDirectory(dataDir);

        var entries = Directory.GetDirectories(extractTemp);
        var extractedDir = entries.FirstOrDefault();
        if (extractedDir != null)
        {
            _log.Info($"InstallAsync: copying {extractedDir} -> {sourceDir}");
            CopyDirectory(extractedDir, sourceDir);
        }
        else
        {
            var files = Directory.GetFiles(extractTemp);
            if (files.Length > 0)
            {
                _log.Info($"InstallAsync: copying {files.Length} files to {sourceDir}");
                Directory.CreateDirectory(sourceDir);
                foreach (var file in files)
                {
                    var dest = Path.Combine(sourceDir, Path.GetFileName(file));
                    File.Copy(file, dest);
                }
            }
        }

        if (Directory.Exists(extractTemp))
        {
            Directory.Delete(extractTemp, true);
        }

        statusProgress?.Report("Saving branch configuration...");
        _config.SaveBranch(dataDir, branch);
        _log.Info($"InstallAsync: saved branch config as {branch}");

        statusProgress?.Report("Ensuring uv is available...");
        await _environment.EnsureUvInstalled(statusProgress);

        statusProgress?.Report("Generating launcher...");
        await GenerateLauncherAsync(dataDir, sourceDir, branch);

        statusProgress?.Report("Installation complete!");
        _log.Info("InstallAsync: complete");
    }

    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var dest = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, dest);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var dest = Path.Combine(destDir, Path.GetFileName(dir));
            CopyDirectory(dir, dest);
        }
    }

    private async Task GenerateLauncherAsync(string dataDir, string sourceDir, BranchType branch)
    {
        var launcherPath = _environment.GetLauncherPath(dataDir);
        _log.Info($"GenerateLauncher: path={launcherPath}, sourceDir={sourceDir}");

        if (_environment.IsWindows)
        {
            var content = $"""
$env:Path = "$env:USERPROFILE\.local\bin;$env:USERPROFILE\.cargo\bin;$env:Path"
Set-Location "{sourceDir}"
uv python install 3.13
uv run ./start.py $args
""";
            await File.WriteAllTextAsync(launcherPath, content);
        }
        else
        {
            var content = $""""
#!/bin/bash
export PATH="$HOME/.local/bin:$HOME/.cargo/bin:$PATH"
cd "{sourceDir}" || exit 1
uv python install 3.13
uv run ./start.py "$@"
"""";
            await File.WriteAllTextAsync(launcherPath, content);
            MakeExecutable(launcherPath);
        }

        _log.Info("GenerateLauncher: written");
    }

    private void MakeExecutable(string path)
    {
        _log.Debug($"MakeExecutable: {path}");
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "chmod",
            Arguments = $"+x \"{path}\"",
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var proc = System.Diagnostics.Process.Start(psi);
    }

    public void Launch(InstallationInfo info)
    {
        if (!info.IsInstalled) return;

        var uvPath = _environment.FindUvBinary();
        if (string.IsNullOrEmpty(uvPath))
        {
            _log.Error("Launch: uv binary not found");
            return;
        }

        var sourceDir = info.SourceDirectory;
        if (!Directory.Exists(sourceDir))
        {
            _log.Error($"Launch: source dir not found at {sourceDir}");
            return;
        }

        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = uvPath,
                Arguments = "run ./start.py",
                WorkingDirectory = sourceDir,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            _log.Info($"Launch: uv={uvPath}, args={psi.Arguments}, wd={psi.WorkingDirectory}");
            var proc = System.Diagnostics.Process.Start(psi);
            if (proc != null)
            {
                proc.EnableRaisingEvents = true;
                proc.Exited += (_, _) =>
                {
                    _log.Info($"Launch: process exited with code {proc.ExitCode}");
                    proc.Dispose();
                };
                _log.Info($"Launch: process started (pid={proc.Id})");
            }
        }
        catch (Exception ex)
        {
            _log.Error("Launch: failed to start process", ex);
        }
    }

    public void OpenInstallationFolder(InstallationInfo info)
    {
        if (!Directory.Exists(info.DataDirectory))
        {
            _log.Warn($"OpenInstallationFolder: dir not found {info.DataDirectory}");
            return;
        }

        _log.Info($"OpenInstallationFolder: {info.DataDirectory}");
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = _environment.IsWindows ? "explorer.exe" : (_environment.IsMacOS ? "open" : "xdg-open"),
            Arguments = _environment.IsWindows ? $"\"{info.DataDirectory}\"" : info.DataDirectory,
            UseShellExecute = true
        };
        System.Diagnostics.Process.Start(psi);
    }
}
