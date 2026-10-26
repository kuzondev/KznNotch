using KznNotch.Integrations;

namespace KznNotch.Services;

public sealed class DiscordService : ObservableObject, IDisposable
{
    private readonly IDiscordNotificationSource _source;
    private readonly SettingsService _settings;
    private readonly Func<bool> _runningProbe;
    private readonly DispatcherTimer _poll = new() { Interval = TimeSpan.FromSeconds(5) };
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _enabled, _running, _disposed;
    private int _notificationCount;
    private int _generation;
    private NotificationAccess _access;
    private string _status = "Discord Integration deaktiviert";
    public bool Enabled { get => _enabled; private set { Set(ref _enabled, value); Raise(nameof(ShowPreview)); } }
    public bool Running { get => _running; private set { Set(ref _running, value); Raise(nameof(RunningLabel)); } }
    public string RunningLabel => Running ? "Discord läuft" : "Discord läuft nicht";
    public string Status { get => _status; private set => Set(ref _status, value); }
    public NotificationAccess Access { get => _access; private set { Set(ref _access, value); Raise(nameof(NeedsPermission)); Raise(nameof(NeedsInstallation)); } }
    public bool NeedsPermission => Enabled && Access is NotificationAccess.NotRequested or NotificationAccess.Denied or NotificationAccess.Unavailable;
    public bool NeedsInstallation => Enabled && Access == NotificationAccess.InstallationRequired;
    public int NotificationCount { get => _notificationCount; private set { Set(ref _notificationCount, value); Raise(nameof(CountLabel)); Raise(nameof(HasNotifications)); Raise(nameof(ShowPreview)); } }
    public string CountLabel => $"{NotificationCount} {(NotificationCount == 1 ? "Benachrichtigung" : "Benachrichtigungen")}";
    public bool HasNotifications => NotificationCount > 0;
    public bool ShowPreview => Enabled && HasNotifications;
    public string EmptyLabel => Access == NotificationAccess.Allowed ? "Keine Discord-Benachrichtigungen im Windows-Mitteilungscenter. Discord muss Desktop-Benachrichtigungen an Windows senden." : Status;
    public string VoiceStatus => "Voice-Channel, Mute/Deafen und Aktivität werden von Discord ohne autorisierte Anbindung nicht bereitgestellt.";
    public ObservableCollection<DiscordNotification> Notifications { get; } = [];
    public ObservableCollection<DiscordNotification> LatestNotifications { get; } = [];
    public ICommand PermissionCommand { get; }
    public ICommand WindowsSettingsCommand { get; }
    public ICommand OpenCommand { get; }
    public DiscordService(SettingsService settings, IDiscordNotificationSource? source = null, Func<bool>? runningProbe = null)
    {
        _settings = settings; _source = source ?? new WindowsDiscordNotificationSource(); _runningProbe = runningProbe ?? ProbeRunning;
        PermissionCommand = new AsyncCommand(() => RefreshAsync(true));
        WindowsSettingsCommand = new RelayCommand(_ => Open("ms-settings:privacy-notifications"));
        OpenCommand = new RelayCommand(_ => Open("discord:"));
        _source.Changed += SourceChanged; _poll.Tick += async (_, _) => await RefreshAsync();
        settings.Changed += SettingsChanged; ApplySettings();
    }
    public Task RequestAccessAsync() => RefreshAsync(true);
    // Continue listening in Compact mode; only the integration switch stops monitoring.
    public void SetActive(bool active) { }
    private void ApplySettings()
    {
        if (_disposed || Enabled == _settings.Current.DiscordEnabled) return;
        Enabled = _settings.Current.DiscordEnabled;
        _generation++;
        if (Enabled) { _poll.Start(); _ = RefreshAsync(); }
        else { _poll.Stop(); _source.Stop(); Running = false; Access = NotificationAccess.NotRequested; Clear(); Status = "Discord Integration deaktiviert"; Raise(nameof(EmptyLabel)); }
    }
    private async Task RefreshAsync(bool requestPermission = false)
    {
        if (_disposed || !Enabled) return;
        if (requestPermission) await _gate.WaitAsync(); else if (!await _gate.WaitAsync(0)) return;
        try
        {
            if (_disposed || !Enabled) return;
            var generation = _generation;
            var running = await Task.Run(_runningProbe);
            if (!IsCurrent(generation)) return;
            Running = running;
            var access = await _source.GetAccessAsync(requestPermission);
            if (!IsCurrent(generation)) { if (!Enabled) _source.Stop(); return; }
            Access = access;
            if (access == NotificationAccess.Allowed)
            {
                var notifications = await _source.ReadAsync();
                if (!IsCurrent(generation)) return;
                var currentAccess = await _source.GetAccessAsync(false);
                if (!IsCurrent(generation)) return;
                if (currentAccess != NotificationAccess.Allowed) { Clear(); Status = "Windows-Benachrichtigungszugriff wurde entzogen."; Access = currentAccess; return; }
                var ordered = notifications.DistinctBy(x => x.Id).OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).ToList();
                NotificationCount = ordered.Count;
                Replace(Notifications, ordered.Take(20)); Replace(LatestNotifications, ordered.Take(3));
                Status = "Windows-Benachrichtigungen verbunden";
            }
            else
            {
                Clear();
                Status = access switch
                {
                    NotificationAccess.InstallationRequired => "Für Discord-Benachrichtigungen bitte die installierte Kzn Notch-Version verwenden.",
                    NotificationAccess.Denied => "Windows-Zugriff verweigert. In den Windows-Datenschutzeinstellungen Benachrichtigungszugriff erlauben.",
                    NotificationAccess.NotRequested => "Einmalig den Windows-Zugriff auf Benachrichtigungen erlauben.",
                    _ => "Windows-Benachrichtigungszugriff ist momentan nicht verfügbar."
                };
            }
        }
        catch (Exception ex) { Clear(); Status = "Windows-Benachrichtigungen momentan nicht erreichbar."; Log.Error("Discord notification refresh", ex); }
        finally { Raise(nameof(EmptyLabel)); _gate.Release(); }
    }
    private static void Replace(ObservableCollection<DiscordNotification> target, IEnumerable<DiscordNotification> values)
    {
        var list = values.ToList(); if (target.SequenceEqual(list)) return;
        target.Clear(); foreach (var item in list) target.Add(item);
    }
    private void Clear() { Notifications.Clear(); LatestNotifications.Clear(); NotificationCount = 0; }
    private bool IsCurrent(int generation) => !_disposed && Enabled && generation == _generation;
    private void SourceChanged(object? sender, EventArgs e) => Ui.Run(async () => await RefreshAsync());
    private void SettingsChanged(object? sender, EventArgs e) => ApplySettings();
    private static bool ProbeRunning()
    {
        var running = false;
        foreach (var name in new[] { "Discord", "DiscordPTB", "DiscordCanary", "DiscordDevelopment" })
            foreach (var process in Process.GetProcessesByName(name)) { running = true; process.Dispose(); }
        return running;
    }
    private static void Open(string uri) { try { Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true }); } catch (Exception ex) { Log.Error("Discord open", ex); } }
    public void Dispose() { _disposed = true; _poll.Stop(); _settings.Changed -= SettingsChanged; _source.Changed -= SourceChanged; _source.Dispose(); Clear(); }
}
