namespace KznNotch.Services;

public sealed class ClockService : ObservableObject, IDisposable
{
    private readonly DispatcherTimer _timer;
    private readonly SettingsService _settings;
    public string Time => DateTime.Now.ToString(_settings.Current.ShowSeconds ? "HH:mm:ss" : "HH:mm");
    public string Date => DateTime.Now.ToString("ddd, dd. MMMM", CultureInfo.GetCultureInfo("de-DE"));
    public ClockService(SettingsService settings)
    {
        _settings = settings;
        _timer = new(TimeSpan.FromSeconds(1), DispatcherPriority.Background, (_, _) => { Raise(nameof(Time)); Raise(nameof(Date)); }, Dispatcher.CurrentDispatcher);
    }
    public void Dispose() => _timer.Stop();
}
