using PSTManager.Core.Enums;

namespace PSTManager.Core.Models;

public class InstallationInfo
{
    public bool IsInstalled { get; init; }
    public string? InstalledVersion { get; init; }
    public BranchType? InstalledBranch { get; init; }
    public string DataDirectory { get; init; } = string.Empty;
    public string SourceDirectory => Path.Combine(DataDirectory, "source");
}
