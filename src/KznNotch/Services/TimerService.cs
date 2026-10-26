using KznNotch.Storage;

namespace KznNotch.Services;

public sealed class TimerSnapshot
{
    public DateTimeOffset? Deadline { get; set; }
    public double RemainingSeconds { get; set; } = 1500;
    public double TotalSeconds { get; set; } = 1500;
    public bool Pomodoro { get; set; }
    public bool IsBreak { get; set; }
}

public sealed class TimerService : ObservableObject, IDisposable
{
    private readonly DispatcherTimer _ticker;
    private readonly JsonStore _store;
    private readonly EventService _events;
    private readonly NotificationService _notifications;
    private readonly TimerSnapshot _state;
    private string _customMinutes = "25";
    public bool Running => _state.Deadline.HasValue;
    public bool Pomodoro => _state.Pomodoro;
    public bool IsBreak => _state.IsBreak;
    public TimeSpan Remaining => TimeSpan.FromSeconds(Math.Max(0, _state.Deadline is { } end ? Math.Ceiling((end - DateTimeOffset.Now).TotalSeconds) : _state.RemainingSeconds));
    public string Display => Remaining.TotalHours >= 1 ? Remaining.ToString(@"h\:mm\:ss") : Remaining.ToString(@"mm\:ss");
    public string Label => Pomodoro ? IsBreak ? "Kurze Pause" : "Fokuszeit" : "Dein Timer";
    public string ToggleLabel => Running ? "Pause" : "Starten";
    public double Progress => _state.TotalSeconds > 0 ? Math.Clamp((1 - Remaining.TotalSeconds / _state.TotalSeconds) * 100, 0, 100) : 0;
    public string CustomMinutes { get => _customMinutes; set => Set(ref _customMinutes, value); }
    public ICommand PresetCommand { get; }
    public ICommand ToggleCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand CustomCommand { get; }
    public ICommand PomodoroCommand { get; }
    public TimerService(JsonStore store, EventService events, NotificationService notifications)
    {
        _store = store; _events = events; _notifications = notifications;
        _state = store.Load("timer.json", () => new TimerSnapshot());
        if (!double.IsFinite(_state.RemainingSeconds) || _state.RemainingSeconds < 0 || _state.RemainingSeconds > 86400) _state.RemainingSeconds = 1500;
        if (!double.IsFinite(_state.TotalSeconds) || _state.TotalSeconds <= 0 || _state.TotalSeconds > 86400) _state.TotalSeconds = 1500;
        PresetCommand = new RelayCommand(p => { if (int.TryParse(p?.ToString(), out var minutes)) Start(minutes, false); });
        ToggleCommand = new RelayCommand(_ => Toggle());
        ResetCommand = new RelayCommand(_ => { _state.Deadline = null; _state.RemainingSeconds = _state.TotalSeconds; Save(); Notify(); });
        CustomCommand = new RelayCommand(_ => { if (double.TryParse(CustomMinutes, out var minutes) && minutes > 0 && minutes <= 1440) Start(minutes, false); }, _ => double.TryParse(CustomMinutes, out var n) && n > 0 && n <= 1440);
        PomodoroCommand = new RelayCommand(_ => Start(25, true));
        _ticker = new(TimeSpan.FromSeconds(1), DispatcherPriority.Background, (_, _) => Tick(), Dispatcher.CurrentDispatcher);
        Tick();
    }
    public void Start(double minutes, bool pomodoro)
    {
        _state.TotalSeconds = _state.RemainingSeconds = Math.Clamp(minutes, 1.0 / 60, 1440) * 60;
        _state.Pomodoro = pomodoro; _state.IsBreak = false;
        _state.Deadline = DateTimeOffset.Now.AddSeconds(_state.RemainingSeconds); Save(); Notify();
    }
    private void Toggle()
    {
        if (Running) { _state.RemainingSeconds = Remaining.TotalSeconds; _state.Deadline = null; }
        else { if (_state.RemainingSeconds <= 0) _state.RemainingSeconds = _state.TotalSeconds; _state.Deadline = DateTimeOffset.Now.AddSeconds(_state.RemainingSeconds); }
        Save(); Notify();
    }
    private void Tick()
    {
        if (Running && Remaining <= TimeSpan.Zero)
        {
            var title = Pomodoro ? IsBreak ? "Pause beendet" : "Fokuszeit geschafft" : "Timer beendet";
            var detail = Pomodoro ? IsBreak ? "Bereit für die nächste Fokusphase." : "Zeit für 5 Minuten Pause." : TimeSpan.FromSeconds(_state.TotalSeconds).ToString(@"hh\:mm\:ss");
            _state.Deadline = null; _state.RemainingSeconds = 0;
            if (Pomodoro)
            {
                _state.IsBreak = !_state.IsBreak;
                _state.TotalSeconds = _state.RemainingSeconds = IsBreak ? 300 : 1500;
            }
            Save();
            _events.Publish(new(title, detail, "\uE916", EventPriority.Timer, TimeSpan.FromSeconds(8), "timer"));
            _notifications.Show(title, detail);
            System.Media.SystemSounds.Asterisk.Play();
        }
        if (Running) Notify(); else { Raise(nameof(Display)); Raise(nameof(ToggleLabel)); Raise(nameof(Running)); Raise(nameof(Label)); Raise(nameof(Progress)); }
    }
    private void Notify() { Raise(nameof(Display)); Raise(nameof(Running)); Raise(nameof(Pomodoro)); Raise(nameof(IsBreak)); Raise(nameof(Label)); Raise(nameof(ToggleLabel)); Raise(nameof(Progress)); }
    private void Save() => _store.Save("timer.json", _state);
    public void Dispose() { _ticker.Stop(); Save(); }
}
