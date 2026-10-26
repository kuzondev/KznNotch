using KznNotch.Storage;

namespace KznNotch.Services;

public sealed class ReminderService : ObservableObject, IDisposable
{
    private readonly JsonStore _store;
    private readonly NotificationService _notifications;
    private readonly EventService _events;
    private readonly DispatcherTimer _timer;
    private string _newTitle = "", _newTime = DateTime.Now.AddHours(1).ToString("HH:mm"), _error = "";
    private DateTime? _newDate = DateTime.Today;
    public ObservableCollection<ReminderItem> Items { get; }
    public string NewTitle { get => _newTitle; set { Set(ref _newTitle, value); CommandManager.InvalidateRequerySuggested(); } }
    public string NewTime { get => _newTime; set => Set(ref _newTime, value); }
    public DateTime? NewDate { get => _newDate; set => Set(ref _newDate, value); }
    public string Error { get => _error; private set => Set(ref _error, value); }
    public ICommand AddCommand { get; }
    public ICommand DeleteCommand { get; }
    public ReminderService(JsonStore store, EventService events, NotificationService notifications)
    {
        _store = store; _events = events; _notifications = notifications;
        Items = new(store.Load("reminders.json", () => new List<ReminderItem>()).Where(x => !x.Fired).OrderBy(x => x.DueAt));
        AddCommand = new RelayCommand(_ => Add(), _ => !string.IsNullOrWhiteSpace(NewTitle));
        DeleteCommand = new RelayCommand(p => { if (p is ReminderItem item) { Items.Remove(item); Save(); } });
        _timer = new(TimeSpan.FromSeconds(1), DispatcherPriority.Background, (_, _) => Check(), Dispatcher.CurrentDispatcher);
        Check();
    }
    private void Add()
    {
        if (NewDate is null || !TimeSpan.TryParseExact(NewTime, new[] { @"h\:mm", @"hh\:mm" }, CultureInfo.InvariantCulture, out var time) || time.TotalHours >= 24)
        { Error = "Bitte Datum und Uhrzeit (HH:mm) eingeben."; return; }
        var local = DateTime.SpecifyKind(NewDate.Value.Date + time, DateTimeKind.Local);
        if (TimeZoneInfo.Local.IsInvalidTime(local)) { Error = "Diese Uhrzeit existiert wegen der Zeitumstellung nicht."; return; }
        var due = new DateTimeOffset(local);
        if (due <= DateTimeOffset.Now) { Error = "Die Erinnerung muss in der Zukunft liegen."; return; }
        var item = new ReminderItem { Title = NewTitle.Trim(), DueAt = due };
        var index = Items.TakeWhile(x => x.DueAt <= due).Count(); Items.Insert(index, item);
        NewTitle = ""; Error = ""; Save();
    }
    private void Check()
    {
        foreach (var item in Items.Where(x => !x.Fired && x.DueAt <= DateTimeOffset.Now).ToList())
        {
            item.Fired = true; Items.Remove(item); Save();
            _events.Publish(new(item.Title, "Jetzt fällig", "\uEA8F", EventPriority.Reminder, TimeSpan.FromSeconds(10), $"reminder-{item.Id}"));
            _notifications.Show(item.Title, "Deine Erinnerung ist jetzt fällig.");
        }
    }
    private void Save() => _store.Save("reminders.json", Items.ToList());
    public void Dispose() => _timer.Stop();
}
