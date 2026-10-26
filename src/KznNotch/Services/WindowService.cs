using Microsoft.Win32;
using KznNotch.Views;
using System.Diagnostics;

namespace KznNotch.Services;

public sealed class WindowService : IDisposable
{
    private readonly AppHost _host;
    private readonly List<NotchWindow> _windows = [];
    private SettingsWindow? _settings;
    private bool _hidden;
    public bool SettingsOpen => _settings is not null;
    public WindowService(AppHost host)
    {
        _host = host; host.Settings.Changed += SettingsChanged; host.Gaming.ModeChanged += GamingChanged; SystemEvents.DisplaySettingsChanged += DisplayChanged;
    }
    public void ShowNotches()
    {
        foreach (var window in _windows) window.Close(); _windows.Clear();
        var screens = System.Windows.Forms.Screen.AllScreens;
        var selected = _host.Settings.Current.Monitors switch
        {
            MonitorMode.All => screens,
            MonitorMode.Selected => [screens.FirstOrDefault(x => x.DeviceName == _host.Settings.Current.MonitorDevice) ?? System.Windows.Forms.Screen.PrimaryScreen ?? screens[0]],
            _ => new[] { System.Windows.Forms.Screen.PrimaryScreen ?? screens[0] }
        };
        foreach (var screen in selected) { var window = new NotchWindow(_host, screen); _windows.Add(window); }
        ApplyGamingMode();
    }
    public void ToggleVisibility() { _hidden = !_hidden; ApplyGamingMode(); }
    public void Expand() { _hidden = false; ApplyGamingMode(); if (_host.Gaming.EffectiveMode != GamingMode.Active) return; foreach (var window in _windows) { window.Show(); window.Expand(); } if (_windows.Count > 0) _windows[0].Activate(); }
    private void GamingChanged(object? sender, EventArgs args) => ApplyGamingMode();
    private void ApplyGamingMode()
    {
        var mode = _host.Gaming.EffectiveMode;
        foreach (var window in _windows)
        {
            window.SetGamingMinimal(mode is GamingMode.Minimal or GamingMode.Fps);
            if (_hidden || mode == GamingMode.Hidden) { window.Collapse(); window.Hide(); }
            else window.Show();
        }
    }
    public void OpenSettings()
    {
        foreach (var window in _windows) window.Collapse();
        if (_settings is null) { _settings = new(_host); _settings.Closed += (_, _) => _settings = null; _settings.Show(); }
        else { _settings.Show(); _settings.Activate(); }
    }
    private void SettingsChanged(object? sender, EventArgs e) => ShowNotches();
    public void OpenIntegrations() { OpenSettings(); _settings?.SelectIntegrations(); }
    public void OpenDesign() { OpenSettings(); _settings?.SelectDesign(); }
    public void OpenPlugins() { OpenSettings(); _settings?.SelectPlugins(); }
    public async void OpenStore()
    {
        try
        {
            var package = new Windows.Management.Deployment.PackageManager().FindPackagesForUser("")
                .FirstOrDefault(p => p.Id.Name == "KznStore.Desktop" && p.Id.Publisher == "CN=Notchify Development");
            if (package is not null)
            {
                foreach (var entry in await package.GetAppListEntriesAsync())
                    if (await entry.LaunchAsync()) return;
            }
            Process.Start(new ProcessStartInfo("https://github.com/kuzondev/KznStore/releases/latest") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Log.Error("Open Kzn Store", ex);
            MessageBox.Show("Kzn Store muss separat installiert werden. Den Installer findest du unter github.com/kuzondev/KznStore/releases.", "Kzn Store", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
    private void DisplayChanged(object? sender, EventArgs e) => Ui.Run(ShowNotches);
    public void Dispose()
    {
        _host.Settings.Changed -= SettingsChanged; SystemEvents.DisplaySettingsChanged -= DisplayChanged;
        _host.Gaming.ModeChanged -= GamingChanged;
        _settings?.Close(); foreach (var window in _windows) window.Close(); _windows.Clear();
    }
}
