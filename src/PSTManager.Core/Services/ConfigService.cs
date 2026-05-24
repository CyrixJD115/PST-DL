using PSTManager.Core.Enums;

namespace PSTManager.Core.Services;

public class ConfigService
{
    private readonly EnvironmentService _environment;
    private readonly string _configDir;

    public ConfigService(EnvironmentService environment)
    {
        _environment = environment;
        _configDir = Path.Combine(
            _environment.IsWindows
                ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share"),
            "pstm"
        );
        Directory.CreateDirectory(_configDir);
    }

    public string GetChannelFilePath(string dataDir)
    {
        return Path.Combine(dataDir, "channel");
    }

    public BranchType? GetSavedBranch(string dataDir)
    {
        var path = GetChannelFilePath(dataDir);
        if (!File.Exists(path)) return null;

        var content = File.ReadAllText(path).Trim().ToLowerInvariant();
        return content switch
        {
            "beta" => BranchType.Beta,
            _ => BranchType.Stable
        };
    }

    public void SaveBranch(string dataDir, BranchType branch)
    {
        var path = GetChannelFilePath(dataDir);
        Directory.CreateDirectory(dataDir);
        var value = branch == BranchType.Beta ? "beta" : "main";
        File.WriteAllText(path, value);
    }
}
