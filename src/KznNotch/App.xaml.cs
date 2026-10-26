namespace KznNotch;

public partial class App : Application
{
    private Mutex? _singleInstance;
    private AppHost? _host;
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args.Length == 2 && e.Args[0] == "--fps-helper")
        {
            try { await Services.FpsCaptureHelper.RunAsync(e.Args[1]); }
            catch (Exception ex) { Log.Error("FPS helper", ex); }
            Shutdown(); return;
        }
        _singleInstance = new Mutex(true, @"Local\KznNotch.Desktop", out var created);
        if (!created) { _singleInstance.Dispose(); _singleInstance = null; Shutdown(); return; }
        DispatcherUnhandledException += (_, args) => { Log.Error("UI", args.Exception); MessageBox.Show("Kzn Notch konnte eine Aktion nicht ausführen. Details stehen in %AppData%\\Kzn Notch\\kzn-notch.log.", "Kzn Notch", MessageBoxButton.OK, MessageBoxImage.Warning); args.Handled = true; };
        TaskScheduler.UnobservedTaskException += (_, args) => { Log.Error("Background", args.Exception); args.SetObserved(); };
        try
        {
            _host = new AppHost(); _host.Settings.RepairStartup(); await _host.InitializeAsync();
            if (e.Args.Contains("--expanded")) _host.Windows.Expand();
            if (e.Args.Contains("--plugins")) _host.Windows.OpenPlugins();
            else if (e.Args.Contains("--design")) _host.Windows.OpenDesign();
            else if (e.Args.Contains("--integrations")) _host.Windows.OpenIntegrations();
            else if (e.Args.Contains("--settings")) _host.Windows.OpenSettings();
            _ = Services.GitHubUpdateService.CheckAndPromptAsync();
        }
        catch (Exception ex) { Log.Error("Startup", ex); MessageBox.Show($"Kzn Notch konnte nicht starten.\n\n{ex.Message}", "Kzn Notch"); Shutdown(1); }
    }
    protected override void OnExit(ExitEventArgs e)
    {
        _host?.Dispose();
        if (_singleInstance is not null) { _singleInstance.ReleaseMutex(); _singleInstance.Dispose(); }
        base.OnExit(e);
    }
}
