using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PSTManager.UI.ViewModels;

public partial class FooterViewModel : ObservableObject
{
    private readonly MainWindowViewModel _main;

    public FooterViewModel(MainWindowViewModel main)
    {
        _main = main;
        main.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainWindowViewModel.IsNotBusy))
                OnPropertyChanged(nameof(IsNotBusy));
        };
        main.Sidebar.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SidebarViewModel.IsInstalled) ||
                e.PropertyName == nameof(SidebarViewModel.SelectedBranch) ||
                e.PropertyName == nameof(SidebarViewModel.InstalledBranch) ||
                e.PropertyName == nameof(SidebarViewModel.LatestStableVersion))
            {
                OnPropertyChanged(nameof(PrimaryActionLabel));
                OnPropertyChanged(nameof(ShowLaunchButton));
                OnPropertyChanged(nameof(IsInstalled));
            }
        };
        main.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainWindowViewModel.PrimaryActionLabel))
                OnPropertyChanged(nameof(PrimaryActionLabel));
            if (e.PropertyName == nameof(MainWindowViewModel.ShowLaunchButton))
                OnPropertyChanged(nameof(ShowLaunchButton));
        };
    }

    public bool IsNotBusy => _main.IsNotBusy;
    public bool IsInstalled => _main.Sidebar.IsInstalled;
    public string PrimaryActionLabel => _main.PrimaryActionLabel;
    public bool ShowLaunchButton => _main.ShowLaunchButton;

    [RelayCommand]
    private void Launch()
    {
        _main.Launch();
    }

    [RelayCommand]
    private void PrimaryAction()
    {
        _ = _main.ExecutePrimaryActionAsync();
    }

    [RelayCommand]
    private void OpenFolder()
    {
        _main.OpenFolder();
    }
}
