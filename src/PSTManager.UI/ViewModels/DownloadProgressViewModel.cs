using CommunityToolkit.Mvvm.ComponentModel;

namespace PSTManager.UI.ViewModels;

public partial class DownloadProgressViewModel : ObservableObject
{
    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private string _statusText = "";

    [ObservableProperty]
    private bool _isVisible;

    [ObservableProperty]
    private bool _isIndeterminate;
}
