using Microsoft.Win32;
using KznNotch.Views;

namespace KznNotch.Services;

public sealed class WindowService : IDisposable
{
    private readonly AppHost _host;
    private readonly List<NotchWindow> _windows = [];
    private SettingsWindow? _settings;
    private StoreWindow? _store;
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
    public void OpenStore()
    {
        if (_store is null) { _store = new(); _store.Closed += (_, _) => _store = null; _store.Show(); }
        else { _store.Show(); if (_store.WindowState == WindowState.Minimized) _store.WindowState = WindowState.Normal; _store.Activate(); }
    }
    private void DisplayChanged(object? sender, EventArgs e) => Ui.Run(ShowNotches);
    public void Dispose()
    {
        _host.Settings.Changed -= SettingsChanged; SystemEvents.DisplaySettingsChanged -= DisplayChanged;
        _host.Gaming.ModeChanged -= GamingChanged;
        _store?.Close(); _settings?.Close(); foreach (var window in _windows) window.Close(); _windows.Clear();
    }
}
