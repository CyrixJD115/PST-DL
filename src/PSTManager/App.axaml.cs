using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PSTManager.UI.ViewModels;
using PSTManager.UI.Views;

namespace PSTManager;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow();
            var viewModel = CreateViewModel();
            mainWindow.DataContext = viewModel;
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static MainWindowViewModel CreateViewModel()
    {
        var log = new PSTManager.Core.Services.LogService();
        log.Info("App starting");

        var http = new System.Net.Http.HttpClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd("PST-Manager/1.0");

        var environment = new PSTManager.Core.Services.EnvironmentService(log);
        var config = new PSTManager.Core.Services.ConfigService(environment);
        var version = new PSTManager.Core.Services.VersionService(http, log);
        var download = new PSTManager.Core.Services.DownloadService(http, log);
        var install = new PSTManager.Core.Services.InstallService(environment, config, log);
        var gdist = new PSTManager.Dis.GDistService();

        log.Info($"Data directory: {environment.GetDataDirectory()}");

        return new MainWindowViewModel(version, download, install, config, environment, gdist, log);
    }
}
