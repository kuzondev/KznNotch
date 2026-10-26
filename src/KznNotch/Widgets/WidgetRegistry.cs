using KznNotch.Services;

namespace KznNotch.Widgets;

public sealed class WidgetEntry(IWidget widget) : ObservableObject
{
    private double _width;
    private bool _isSelected;
    public bool IsSelected { get => _isSelected; set => Set(ref _isSelected, value); }
    public IWidget Widget { get; } = widget;
    public string Name => Widget.Id switch { "media" => "Musik", "timer" => "Fokus", "system" => "System", "todo" => "Aufgaben", "reminder" => "Erinnerungen", "bluetooth" => "Geräte", "discord" => "Discord", "fps" => "Gaming", _ => Widget.Name };
    public string Glyph => Widget.Id switch { "media" => "\uE8D6", "timer" => "\uE916", "system" => "\uE9D9", "todo" => "\uE73A", "reminder" => "\uEA8F", "bluetooth" => "\uE702", "discord" => "\uE8F2", "fps" => "\uE7FC", _ => "\uE713" };
    public double Width { get => _width; set => Set(ref _width, value); }
}

public sealed class WidgetRegistry : IDisposable
{
    private readonly Dictionary<string, IWidget> _widgets = [];
    private readonly SettingsService _settings;
    private readonly Dictionary<object, string> _windowSelections = [];
    public ObservableCollection<WidgetEntry> Visible { get; } = [];
    public WidgetRegistry(SettingsService settings) { _settings = settings; settings.Changed += SettingsChanged; }
    public void Register(IWidget widget) { _widgets.Add(widget.Id, widget); widget.Initialize(); }
    public void Rebuild()
    {
        Visible.Clear();
        foreach (var settings in _settings.Current.Widgets.Where(x => x.Enabled && (x.Id != "discord" || _settings.Current.DiscordEnabled)))
            if (_widgets.TryGetValue(settings.Id, out var widget)) Visible.Add(new(widget));
        Resize(_settings.Current.ExpandedWidth - 40);
        RefreshActive();
    }
    public void Resize(double width)
    {
        foreach (var item in Visible) item.Width = item.Widget.IsWide ? width : (width - 10) / 2;
    }
    public void SetWindowSelection(object owner, string? id)
    {
        if (id is null) _windowSelections.Remove(owner); else _windowSelections[owner] = id;
        RefreshActive();
    }
    private void RefreshActive() { foreach (var widget in _widgets.Values) widget.SetActive(_windowSelections.Values.Contains(widget.Id) && Visible.Any(x => x.Widget == widget)); }
    private void SettingsChanged(object? sender, EventArgs e) => Rebuild();
    public void Dispose() { _settings.Changed -= SettingsChanged; foreach (var widget in _widgets.Values) widget.Dispose(); }
}
