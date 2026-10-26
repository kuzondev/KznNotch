namespace KznNotch.Services;

public enum EventPriority { Reminder = 0, Timer = 1, Call = 2, Media = 3, Volume = 4, Bluetooth = 5 }
public sealed record NotchEvent(string Title, string Detail, string Glyph, EventPriority Priority, TimeSpan Duration, string Key);

public sealed class EventService : ObservableObject, IDisposable
{
    private readonly List<NotchEvent> _queue = [];
    private readonly DispatcherTimer _expiry = new();
    private NotchEvent? _current;
    public NotchEvent? Current { get => _current; private set { Set(ref _current, value); Raise(nameof(HasEvent)); } }
    public bool HasEvent => Current is not null;
    public EventService() { _expiry.Tick += (_, _) => Advance(); }
    public void Publish(NotchEvent item) => Ui.Run(() =>
    {
        _queue.RemoveAll(x => x.Key == item.Key);
        if (Current is null || item.Priority < Current.Priority || item.Key == Current.Key)
        {
            if (Current is not null && item.Priority < Current.Priority) _queue.Add(Current);
            Show(item);
        }
        else
        {
            _queue.Add(item);
            if (_queue.Count > 20) _queue.RemoveAt(_queue.Count - 1);
        }
    });
    private void Show(NotchEvent item) { Current = item; _expiry.Stop(); _expiry.Interval = item.Duration; _expiry.Start(); }
    public void Dismiss() => Advance();
    private void Advance()
    {
        _expiry.Stop();
        var next = _queue.OrderBy(x => x.Priority).FirstOrDefault();
        if (next is null) Current = null;
        else { _queue.Remove(next); Show(next); }
    }
    public void Dispose() { _expiry.Stop(); _queue.Clear(); }
}
