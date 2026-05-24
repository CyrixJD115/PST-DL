using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PSTManager.Core.Enums;
using PSTManager.Core.Models;
using System.Collections.Generic;

namespace PSTManager.UI.ViewModels;

public partial class SidebarViewModel : ObservableObject
{
    private readonly MainWindowViewModel _main;

    [ObservableProperty]
    private string _localVersion = "1.0.9";

    [ObservableProperty]
    private string _latestStableVersion = "...";

    [ObservableProperty]
    private string _installedVersionText = "Not installed";

    [ObservableProperty]
    private string _installedBranchText = "";

    [ObservableProperty]
    private bool _isInstalled;

    [ObservableProperty]
    private BranchType _selectedBranch = BranchType.Stable;

    [ObservableProperty]
    private BranchType? _installedBranch;

    public List<BranchType> Branches { get; } = new() { BranchType.Stable, BranchType.Beta };

    public SidebarViewModel(MainWindowViewModel main)
    {
        _main = main;
    }

    public void UpdateInstallation(InstallationInfo info)
    {
        IsInstalled = info.IsInstalled;
        InstalledBranch = info.InstalledBranch;
        InstalledVersionText = info.IsInstalled ? "Installed" : "Not installed";
        InstalledBranchText = info.InstalledBranch?.ToString() ?? "";
    }

    [RelayCommand]
    private void OpenGitHub()
    {
        _main.OpenGitHub();
    }
}
