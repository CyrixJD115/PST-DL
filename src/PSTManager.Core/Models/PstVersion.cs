using PSTManager.Core.Enums;

namespace PSTManager.Core.Models;

public class PstVersion
{
    public string Tag { get; init; } = string.Empty;
    public string VersionNumber { get; init; } = string.Empty;
    public BranchType Branch { get; init; } = BranchType.Stable;
}
