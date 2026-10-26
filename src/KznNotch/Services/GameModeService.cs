using KznNotch.Utilities;

namespace KznNotch.Services;

public sealed class GameModeService : ObservableObject, IDisposable
{
    private readonly SettingsService _settings;
    private readonly Func<bool?> _detect;
    private readonly GameCatalogService? _catalog;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private bool _started, _isGaming;
    private int _misses;
    public int GameProcessId { get; private set; }
    public string GameName { get; private set; } = "";
    public bool IsGaming => _isGaming;
    public GamingMode EffectiveMode => IsGaming ? _settings.Current.GamingMode : GamingMode.Active;
    public string Status => _settings.Current.GamingMode == GamingMode.Active ? "Aktiv · normaler Betrieb, auch im Spiel"
        : IsGaming ? EffectiveMode == GamingMode.Hidden ? "Spiel erkannt · Notch ausgeblendet" : EffectiveMode == GamingMode.Fps ? "Spiel erkannt · FPS in der gewählten Ecke" : "Spiel erkannt · minimale Uhr in der gewählten Ecke"
        : "Bereit · außerhalb eines Spiels normaler Betrieb";
    public event EventHandler? ModeChanged;
    public GameModeService(SettingsService settings, Func<bool?>? detect = null, GameCatalogService? catalog = null)
    {
        _settings = settings; _detect = detect ?? DetectWindowsGame; _catalog = catalog;
        _timer.Tick += (_, _) => Refresh(); settings.Changed += SettingsChanged;
    }
    public void Initialize() { _started = true; Configure(); }
    private void Configure()
    {
        _timer.Stop(); _misses = 0;
        if (_settings.Current.GamingMode == GamingMode.Active) SetGaming(false);
        else if (_started) { Refresh(); _timer.Start(); }
        Raise(nameof(Status)); Raise(nameof(EffectiveMode)); ModeChanged?.Invoke(this, EventArgs.Empty);
    }
    public void Refresh()
    {
        if (_settings.Current.GamingMode == GamingMode.Active) { SetGaming(false); return; }
        bool? detected;
        try { detected = _detect(); }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException or ArgumentException) { detected = false; }
        // Opening our tray/settings must not let an overlay flash over the game.
        if (detected is null) return;
        if (detected.Value) { _misses = 0; SetGaming(true); }
        else if (++_misses >= 2) SetGaming(false);
    }
    private void SetGaming(bool value)
    {
        if (_isGaming == value) return;
        _isGaming = value; Raise(nameof(IsGaming)); Raise(nameof(EffectiveMode)); Raise(nameof(Status));
        if (!value) { GameProcessId = 0; GameName = ""; Raise(nameof(GameProcessId)); Raise(nameof(GameName)); }
        ModeChanged?.Invoke(this, EventArgs.Empty);
    }
    public static bool MatchesGame(string executable, string path, bool exclusiveDirect3D)
    {
        var name = Path.GetFileNameWithoutExtension(executable);
        if (name.Equals("KznNotch", StringComparison.OrdinalIgnoreCase) ||
            new[] { "explorer", "steam", "steamwebhelper", "EpicGamesLauncher", "chrome", "msedge", "firefox", "vlc" }.Contains(name, StringComparer.OrdinalIgnoreCase)) return false;
        if (exclusiveDirect3D) return true;
        var normalized = path.Replace('/', '\\');
        return normalized.Contains("\\steamapps\\common\\", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains("\\XboxGames\\", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains("\\Epic Games\\", StringComparison.OrdinalIgnoreCase);
    }
    private bool? DetectWindowsGame()
    {
        var foreground = NativeMethods.GetForegroundWindow();
        if (foreground == IntPtr.Zero) return false;
        NativeMethods.GetWindowThreadProcessId(foreground, out var pid);
        if (pid == Environment.ProcessId) return null;
        if (pid == 0) return false;
        using var process = Process.GetProcessById((int)pid);
        string path = "";
        try { path = process.MainModule?.FileName ?? ""; }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException) { }
        var exclusive = NativeMethods.SHQueryUserNotificationState(out var state) == 0 && state == 3;
        // Windows provides an exclusive D3D signal, not an authoritative list of all games.
        // Borderless/windowed games use their foreground executable or installation directory.
        var excluded = new[] { "explorer", "steam", "steamwebhelper", "EpicGamesLauncher", "chrome", "msedge", "firefox", "vlc", "KznNotch" }.Contains(process.ProcessName, StringComparer.OrdinalIgnoreCase);
        var matches = !excluded && (MatchesGame(process.ProcessName, path, exclusive) || _catalog?.ContainsPath(path) == true);
        if (matches) { GameProcessId = (int)pid; GameName = process.ProcessName; _catalog?.Observe(process.ProcessName, path); Raise(nameof(GameProcessId)); Raise(nameof(GameName)); }
        return matches;
    }
    private void SettingsChanged(object? sender, EventArgs args) => Configure();
    public void Dispose() { _timer.Stop(); _settings.Changed -= SettingsChanged; }
}
