using Windows.Media.Control;
using System.Windows.Media.Imaging;

namespace KznNotch.Services;

public sealed class MediaService : ObservableObject, IDisposable
{
    private GlobalSystemMediaTransportControlsSessionManager? _manager;
    private GlobalSystemMediaTransportControlsSession? _session;
    private readonly EventService _events;
    private readonly SemaphoreSlim _refresh = new(1, 1);
    private string _title = "Keine Wiedergabe", _artist = "Starte Musik oder ein Video", _source = "Medien", _status = "";
    private ImageSource? _cover;
    private bool _playing, _available, _canToggle, _canNext, _canPrevious, _canSeek, _disposed;
    private TimeSpan _position, _duration;
    private DateTimeOffset _timelineAt;
    private TimeSpan _basePosition;
    private string _coverKey = "";
    public string Title { get => _title; private set => Set(ref _title, value); }
    public string Artist { get => _artist; private set => Set(ref _artist, value); }
    public string Source { get => _source; private set => Set(ref _source, value); }
    public string Status { get => _status; private set => Set(ref _status, value); }
    public ImageSource? Cover { get => _cover; private set => Set(ref _cover, value); }
    public bool Available { get => _available; private set => Set(ref _available, value); }
    public bool Playing { get => _playing; private set { if (Set(ref _playing, value)) Raise(nameof(PlayGlyph)); } }
    public string PlayGlyph => Playing ? "\uE769" : "\uE768";
    public bool CanToggle { get => _canToggle; private set => Set(ref _canToggle, value); }
    public bool CanNext { get => _canNext; private set => Set(ref _canNext, value); }
    public bool CanPrevious { get => _canPrevious; private set => Set(ref _canPrevious, value); }
    public bool CanSeek { get => _canSeek; private set => Set(ref _canSeek, value); }
    public TimeSpan Position { get => _position; private set { Set(ref _position, value); Raise(nameof(Elapsed)); Raise(nameof(Progress)); } }
    public TimeSpan Duration { get => _duration; private set { Set(ref _duration, value); Raise(nameof(Total)); Raise(nameof(Progress)); } }
    public string Elapsed => Format(Position);
    public string Total => Format(Duration);
    public double Progress => Duration.TotalSeconds > 0 ? Math.Clamp(Position.TotalSeconds / Duration.TotalSeconds * 100, 0, 100) : 0;
    public ICommand ToggleCommand { get; }
    public ICommand NextCommand { get; }
    public ICommand PreviousCommand { get; }
    public MediaService(EventService events)
    {
        _events = events;
        ToggleCommand = new AsyncCommand(async () => { if (_session is { } s && !await s.TryTogglePlayPauseAsync()) Status = "Player hat die Steuerung abgelehnt"; }, () => CanToggle);
        NextCommand = new AsyncCommand(async () => { if (_session is { } s && !await s.TrySkipNextAsync()) Status = "Nächster Titel nicht verfügbar"; }, () => CanNext);
        PreviousCommand = new AsyncCommand(async () => { if (_session is { } s && !await s.TrySkipPreviousAsync()) Status = "Vorheriger Titel nicht verfügbar"; }, () => CanPrevious);
    }
    public async Task InitializeAsync()
    {
        try
        {
            _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            _manager.CurrentSessionChanged += ManagerChanged;
            _manager.SessionsChanged += SessionsChanged;
            await RefreshAsync();
        }
        catch (Exception ex) { Log.Error("Media initialization", ex); Status = "Windows-Medienzugriff nicht verfügbar"; }
    }
    private void ManagerChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args) => QueueRefresh();
    private void SessionsChanged(GlobalSystemMediaTransportControlsSessionManager sender, SessionsChangedEventArgs args) => QueueRefresh();
    private void PropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args) => QueueRefresh();
    private void PlaybackChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args) => QueueRefresh();
    private void TimelineChanged(GlobalSystemMediaTransportControlsSession sender, TimelinePropertiesChangedEventArgs args) => QueueRefresh();
    private void QueueRefresh() => Ui.Run(async () => await RefreshAsync());
    public async Task RefreshAsync()
    {
        await _refresh.WaitAsync();
        try
        {
            if (_disposed || _manager is null) return;
            var selected = _manager.GetCurrentSession();
            if (!ReferenceEquals(_session, selected))
            {
                Detach(); _session = selected;
                if (_session is not null)
                {
                    _session.MediaPropertiesChanged += PropertiesChanged;
                    _session.PlaybackInfoChanged += PlaybackChanged;
                    _session.TimelinePropertiesChanged += TimelineChanged;
                }
            }
            if (_session is null)
            {
                Available = Playing = CanToggle = CanNext = CanPrevious = CanSeek = false;
                Title = "Keine Wiedergabe"; Artist = "Starte Musik oder ein Video"; Cover = null; Position = Duration = TimeSpan.Zero; Source = "Medien"; return;
            }
            var session = _session;
            var props = await session.TryGetMediaPropertiesAsync();
            if (_disposed) return;
            var coverKey = $"{session.SourceAppUserModelId}|{props.Title}|{props.Artist}|{props.AlbumTitle}";
            var changed = _coverKey != coverKey;
            Title = string.IsNullOrWhiteSpace(props.Title) ? "Unbenannte Wiedergabe" : props.Title;
            Artist = string.IsNullOrWhiteSpace(props.Artist) ? props.AlbumTitle : props.Artist;
            Source = FriendlySource(session.SourceAppUserModelId);
            var playback = session.GetPlaybackInfo();
            Playing = playback.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
            Available = true;
            CanToggle = playback.Controls.IsPlayPauseToggleEnabled;
            CanNext = playback.Controls.IsNextEnabled;
            CanPrevious = playback.Controls.IsPreviousEnabled;
            CanSeek = playback.Controls.IsPlaybackPositionEnabled;
            var timeline = session.GetTimelineProperties();
            _basePosition = timeline.Position - timeline.StartTime;
            _timelineAt = timeline.LastUpdatedTime;
            Duration = timeline.EndTime > timeline.StartTime ? timeline.EndTime - timeline.StartTime : TimeSpan.Zero;
            UpdatePosition();
            if (changed || Cover is null)
            {
                _coverKey = coverKey;
                Cover = null;
                if (props.Thumbnail is not null)
                {
                    using var stream = await props.Thumbnail.OpenReadAsync();
                    using var input = stream.AsStreamForRead();
                    var image = new BitmapImage(); image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad; image.DecodePixelWidth = 160; image.StreamSource = input; image.EndInit(); image.Freeze(); Cover = image;
                }
            }
            Status = "";
            if (changed && Playing) _events.Publish(new(Title, Artist, "\uE8D6", EventPriority.Media, TimeSpan.FromSeconds(3), "media"));
            CommandManager.InvalidateRequerySuggested();
        }
        catch (Exception ex) { Log.Error("Media refresh", ex); Status = "Player momentan nicht erreichbar"; }
        finally { _refresh.Release(); }
    }
    public void UpdatePosition()
    {
        var elapsed = Playing && _timelineAt != default ? DateTimeOffset.UtcNow - _timelineAt : TimeSpan.Zero;
        var value = _basePosition + (elapsed > TimeSpan.Zero ? elapsed : TimeSpan.Zero);
        Position = Duration > TimeSpan.Zero ? TimeSpan.FromTicks(Math.Clamp(value.Ticks, 0, Duration.Ticks)) : TimeSpan.Zero;
    }
    public async Task SeekAsync(double fraction)
    {
        if (_session is null || !CanSeek || Duration <= TimeSpan.Zero) return;
        try { var start = _session.GetTimelineProperties().StartTime; await _session.TryChangePlaybackPositionAsync((start + Duration * Math.Clamp(fraction, 0, 1)).Ticks); }
        catch (Exception ex) { Log.Error("Media seek", ex); }
    }
    private static string Format(TimeSpan value) => value.TotalHours >= 1 ? value.ToString(@"h\:mm\:ss") : value.ToString(@"m\:ss");
    private static string FriendlySource(string app)
    {
        if (app.Contains("spotify", StringComparison.OrdinalIgnoreCase)) return "Spotify";
        if (app.Contains("chrome", StringComparison.OrdinalIgnoreCase)) return "Google Chrome";
        if (app.Contains("msedge", StringComparison.OrdinalIgnoreCase)) return "Microsoft Edge";
        if (app.Contains("firefox", StringComparison.OrdinalIgnoreCase)) return "Firefox";
        if (app.Contains("vlc", StringComparison.OrdinalIgnoreCase)) return "VLC";
        return Path.GetFileNameWithoutExtension(app.Split('!')[0]);
    }
    private void Detach()
    {
        if (_session is null) return;
        _session.MediaPropertiesChanged -= PropertiesChanged;
        _session.PlaybackInfoChanged -= PlaybackChanged;
        _session.TimelinePropertiesChanged -= TimelineChanged;
    }
    public void Dispose()
    {
        _disposed = true; Detach();
        if (_manager is not null) { _manager.CurrentSessionChanged -= ManagerChanged; _manager.SessionsChanged -= SessionsChanged; }
    }
}
