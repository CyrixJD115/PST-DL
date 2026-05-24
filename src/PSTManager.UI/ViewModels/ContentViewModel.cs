using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PSTManager.UI.ViewModels;

public enum AppState
{
    Idle,
    Downloading,
    Installed,
    Error,
    ConfirmSwitch,
    Launching
}

public partial class ContentViewModel : ObservableObject
{
    private readonly MainWindowViewModel? _main;

    public ContentViewModel() { }

    public ContentViewModel(MainWindowViewModel main)
    {
        _main = main;
    }

    [ObservableProperty]
    private AppState _currentState = AppState.Idle;

    [ObservableProperty]
    private string _statusMessage = "Welcome to PST Manager";

    [ObservableProperty]
    private string _actionButtonText = "Download & Install";

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private bool _isBackgroundVisible = true;

    public void SetInstalledState(bool installed)
    {
        if (installed)
        {
            CurrentState = AppState.Installed;
            StatusMessage = "PST is ready to launch";
        }
        else
        {
            CurrentState = AppState.Idle;
            StatusMessage = "No installation found";
        }
    }

    public void SetDownloadingState()
    {
        CurrentState = AppState.Downloading;
        StatusMessage = "Downloading...";
    }

    public void SetErrorState(string message)
    {
        CurrentState = AppState.Error;
        ErrorMessage = message;
        StatusMessage = "Something went wrong";
    }

    public void SetLaunchingState()
    {
        CurrentState = AppState.Launching;
        StatusMessage = "Launching PST...";
    }

    public void ClearLaunchingState()
    {
        if (CurrentState == AppState.Launching)
        {
            SetInstalledState(true);
        }
    }

    public void ShowConfirmSwitch()
    {
        CurrentState = AppState.ConfirmSwitch;
        StatusMessage = "Switching branch will reinstall PST. Continue?";
    }

    [RelayCommand]
    private void ConfirmSwitch()
    {
        if (_main == null) return;
        _ = _main.ConfirmBranchSwitchAsync();
    }

    [RelayCommand]
    private void CancelSwitch()
    {
        SetInstalledState(true);
        _main?.RevertBranchSelection();
    }
}
