using CommunityToolkit.Mvvm.ComponentModel;
using PSTManager.Core.Enums;
using PSTManager.Core.Models;
using PSTManager.Core.Services;
using PSTManager.Dis;

namespace PSTManager.UI.ViewModels;

public enum PrimaryAction
{
    DownloadInstall,
    Update,
    Repair,
    SwitchBranch
}

public partial class MainWindowViewModel : ObservableObject
{
    private readonly VersionService _versionService;
    private readonly DownloadService _downloadService;
    private readonly InstallService _installService;
    private readonly ConfigService _configService;
    private readonly EnvironmentService _environmentService;
    private readonly GDistService _gdistService;
    private readonly LogService _log;

    [ObservableProperty]
    private string _windowTitle = "PST Manager";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    [NotifyPropertyChangedFor(nameof(PrimaryActionLabel))]
    [NotifyPropertyChangedFor(nameof(ShowLaunchButton))]
    private bool _isBusy;

    public bool IsNotBusy => !IsBusy;

    public HeaderViewModel Header { get; }
    public SidebarViewModel Sidebar { get; }
    public ContentViewModel Content { get; }
    public FooterViewModel Footer { get; }
    public DownloadProgressViewModel DownloadProgress { get; }

    public string PrimaryActionLabel => GetCurrentAction() switch
    {
        PrimaryAction.DownloadInstall => "Download & Install",
        PrimaryAction.Update => "Update",
        PrimaryAction.Repair => "Repair",
        PrimaryAction.SwitchBranch => $"Switch to {Sidebar.SelectedBranch}",
        _ => "Download & Install"
    };

    public bool ShowLaunchButton => Sidebar.IsInstalled && !IsBusy;

    public MainWindowViewModel(
        VersionService versionService,
        DownloadService downloadService,
        InstallService installService,
        ConfigService configService,
        EnvironmentService environmentService,
        GDistService gdistService,
        LogService log)
    {
        _versionService = versionService;
        _downloadService = downloadService;
        _installService = installService;
        _configService = configService;
        _environmentService = environmentService;
        _gdistService = gdistService;
        _log = log;

        _log.Info("MainWindowViewModel created");

        Header = new HeaderViewModel();
        Sidebar = new SidebarViewModel(this);
        Content = new ContentViewModel(this);
        Footer = new FooterViewModel(this);
        DownloadProgress = new DownloadProgressViewModel();

        Sidebar.PropertyChanged += OnSidebarPropertyChanged;

        _ = InitializeAsync();
    }

    private void OnSidebarPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SidebarViewModel.SelectedBranch) ||
            e.PropertyName == nameof(SidebarViewModel.IsInstalled) ||
            e.PropertyName == nameof(SidebarViewModel.InstalledBranch) ||
            e.PropertyName == nameof(SidebarViewModel.LatestStableVersion))
        {
            OnPropertyChanged(nameof(PrimaryActionLabel));
            OnPropertyChanged(nameof(ShowLaunchButton));
        }
    }

    private PrimaryAction GetCurrentAction()
    {
        if (!Sidebar.IsInstalled)
            return PrimaryAction.DownloadInstall;

        if (Sidebar.InstalledBranch != Sidebar.SelectedBranch)
            return PrimaryAction.SwitchBranch;

        return PrimaryAction.Repair;
    }

    private async Task InitializeAsync()
    {
        try
        {
            _log.Info("InitializeAsync: checking installation");
            var info = _installService.CheckInstallation();
            _log.Info($"InitializeAsync: IsInstalled={info.IsInstalled}, Branch={info.InstalledBranch}, DataDir={info.DataDirectory}");
            Sidebar.UpdateInstallation(info);
            Content.SetInstalledState(info.IsInstalled);

            if (info.InstalledBranch.HasValue)
            {
                Sidebar.SelectedBranch = info.InstalledBranch.Value;
                _log.Info($"InitializeAsync: set branch to {info.InstalledBranch.Value}");
            }

            _log.Info("InitializeAsync: fetching PSTM version");
            var localVersion = await _versionService.GetLatestPstmVersionAsync();
            Sidebar.LocalVersion = localVersion ?? "1.0.9";
            _log.Info($"InitializeAsync: PSTM version = {Sidebar.LocalVersion}");

            _log.Info("InitializeAsync: fetching latest stable PST version");
            var latestStable = await _versionService.GetLatestStableVersionAsync();
            if (latestStable != null)
            {
                Sidebar.LatestStableVersion = latestStable.VersionNumber;
                _log.Info($"InitializeAsync: latest stable = {latestStable.VersionNumber}");
            }
            else
            {
                _log.Warn("InitializeAsync: could not fetch latest stable version");
            }

            OnPropertyChanged(nameof(PrimaryActionLabel));
            OnPropertyChanged(nameof(ShowLaunchButton));

            _log.Info("InitializeAsync: launching GDist");
            _ = _gdistService.RunAsync();

            _log.Info("InitializeAsync: complete");
        }
        catch (Exception ex)
        {
            _log.Error("InitializeAsync failed", ex);
        }
    }

    public async Task ConfirmBranchSwitchAsync()
    {
        _log.Info("ConfirmBranchSwitchAsync");
        await StartDownloadAsync();
    }

    public void RevertBranchSelection()
    {
        if (Sidebar.InstalledBranch.HasValue)
        {
            Sidebar.SelectedBranch = Sidebar.InstalledBranch.Value;
            _log.Info($"RevertBranchSelection: reverted to {Sidebar.InstalledBranch.Value}");
        }
    }

    public async Task ExecutePrimaryActionAsync()
    {
        if (IsBusy) return;

        var action = GetCurrentAction();
        _log.Info($"ExecutePrimaryActionAsync: action={action}");

        switch (action)
        {
            case PrimaryAction.DownloadInstall:
                await StartDownloadAsync();
                break;

            case PrimaryAction.Update:
                await StartDownloadAsync();
                break;

            case PrimaryAction.Repair:
                await StartDownloadAsync();
                break;

            case PrimaryAction.SwitchBranch:
                Content.ShowConfirmSwitch();
                break;
        }
    }

    public async Task StartDownloadAsync()
    {
        if (IsBusy)
        {
            _log.Warn("StartDownloadAsync: already busy, ignoring");
            return;
        }

        IsBusy = true;
        var branch = Sidebar.SelectedBranch;

        var url = branch == BranchType.Stable
            ? _versionService.GetStableDownloadUrl()
            : _versionService.GetBetaDownloadUrl();

        _log.Info($"StartDownloadAsync: branch={branch}, url={url}");
        Content.SetDownloadingState();
        DownloadProgress.IsVisible = true;
        DownloadProgress.StatusText = "Starting download...";

        try
        {
            var downloadProgress = new Progress<double>(p =>
            {
                DownloadProgress.Progress = p;
                DownloadProgress.StatusText = $"Downloading... {p:F0}%";
            });

            var statusProgress = new Progress<string>(s =>
            {
                DownloadProgress.StatusText = s;
            });

            var tempDir = Path.Combine(Path.GetTempPath(), "PST_Downloads");
            _log.Info($"StartDownloadAsync: downloading to {tempDir}");
            var zipPath = await _downloadService.DownloadAsync(url, tempDir, downloadProgress, statusProgress);
            _log.Info($"StartDownloadAsync: download complete, zip={zipPath}");

            DownloadProgress.IsIndeterminate = true;
            DownloadProgress.StatusText = "Installing...";

            await _installService.InstallAsync(zipPath, branch, statusProgress);
            _log.Info("StartDownloadAsync: install complete");

            File.Delete(zipPath);
            _log.Info("StartDownloadAsync: cleaned up zip");

            DownloadProgress.IsVisible = false;
            Content.SetInstalledState(true);

            var info = _installService.CheckInstallation();
            Sidebar.UpdateInstallation(info);
        }
        catch (Exception ex)
        {
            _log.Error("StartDownloadAsync failed", ex);
            DownloadProgress.IsVisible = false;
            Content.SetErrorState($"Download failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void Launch()
    {
        _log.Info("Launch");
        var info = _installService.CheckInstallation();
        if (info.IsInstalled)
        {
            Content.SetLaunchingState();
            _installService.Launch(info);
            _ = Task.Run(async () =>
            {
                await Task.Delay(3000);
                Avalonia.Threading.Dispatcher.UIThread.Post(() => Content.ClearLaunchingState());
            });
        }
        else
        {
            _log.Warn("Launch: no installation found");
        }
    }

    public void OpenFolder()
    {
        _log.Info("OpenFolder");
        var info = _installService.CheckInstallation();
        _installService.OpenInstallationFolder(info);
    }

    public void OpenGitHub()
    {
        try
        {
            _log.Info("OpenGitHub");
            var url = "https://github.com/deafdudecomputers/PalworldSaveTools";
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }
        catch (Exception ex)
        {
            _log.Error("OpenGitHub failed", ex);
        }
    }
}
