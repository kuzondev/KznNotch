using KznNotch.Services;
using KznNotch.Widgets;

namespace KznNotch.ViewModels;

public sealed class CompactItem : ObservableObject
{
    private string _text = "", _glyph = "";
    private ImageSource? _image;
    public string Text { get => _text; set => Set(ref _text, value); }
    public string Glyph { get => _glyph; set => Set(ref _glyph, value); }
    public ImageSource? Image { get => _image; set => Set(ref _image, value); }
    public string Id { get; init; } = "";
}

public sealed class NotchViewModel : ObservableObject, IDisposable
{
    private WidgetEntry? _selectedWidget;
    public AppHost Host { get; }
    public ObservableCollection<CompactItem> CompactItems { get; } = [];
    public ObservableCollection<WidgetEntry> VisibleWidgets { get; } = [];
    public NotchState State { get; private set; }
    public bool IsExpanded => State == NotchState.Expanded;
    public bool GamingMinimal { get; private set; }
    public string GamingLabel => Host.Gaming.EffectiveMode == GamingMode.Fps ? Host.Fps.OverlayText : Host.Clock.Time;
    public bool ShowEvent => Host.Events.HasEvent && !GamingMinimal;
    public bool ShowCompact => State == NotchState.Compact && !Host.Events.HasEvent && !GamingMinimal;
    public bool ShowDefaultCompact => ShowCompact && !Host.Plugins.HasActive;
    public bool ShowPluginCompact => ShowCompact && Host.Plugins.HasActive;
    public bool ShowDetails => State != NotchState.Compact && !GamingMinimal;
    public bool ShowHover => State == NotchState.Hover;
    public bool ShowDate => Host.Settings.Current.ShowDate;
    public bool ShowBattery => Host.Settings.Current.ShowBattery && Host.Battery.HasBattery;
    public bool HasWidgets => VisibleWidgets.Count > 0;
    public string ModeTitle => SelectedWidget?.Name ?? "Keine Funktionen";
    public WidgetEntry? SelectedWidget
    {
        get => _selectedWidget;
        set
        {
            if (value is not null && !VisibleWidgets.Contains(value)) return;
            if (!Set(ref _selectedWidget, value)) return;
            foreach (var entry in VisibleWidgets) entry.IsSelected = entry == value;
            if (value is not null) Host.Settings.RememberWidget(value.Widget.Id);
            ActivateSelection(); Raise(nameof(ModeTitle)); LayoutChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public ICommand PreviousWidgetCommand { get; }
    public ICommand NextWidgetCommand { get; }
    public ICommand SelectWidgetCommand { get; }
    public event EventHandler? LayoutChanged;

    public NotchViewModel(AppHost host)
    {
        Host = host;
        SelectWidgetCommand = new RelayCommand(value => { if (value is WidgetEntry entry && VisibleWidgets.Contains(entry)) SelectedWidget = entry; });
        PreviousWidgetCommand = new RelayCommand(_ => Cycle(-1), _ => VisibleWidgets.Count > 1);
        NextWidgetCommand = new RelayCommand(_ => Cycle(1), _ => VisibleWidgets.Count > 1);
        host.Clock.PropertyChanged += Changed; host.Battery.PropertyChanged += Changed;
        host.Fps.PropertyChanged += FpsChanged;
        host.Plugins.ActiveChanged += PluginChanged;
        host.Media.PropertyChanged += Changed; host.Bluetooth.PropertyChanged += Changed;
        host.Discord.PropertyChanged += Changed; host.Events.PropertyChanged += EventChanged;
        host.Settings.Changed += SettingsChanged; host.Widgets.Visible.CollectionChanged += WidgetsChanged;
        RefreshWidgets(); RefreshCompact();
    }
    public void SetState(NotchState state)
    {
        if (GamingMinimal && state != NotchState.Compact) return;
        if (State == state) return;
        State = state; ActivateSelection();
        Raise(nameof(IsExpanded)); Raise(nameof(ShowCompact)); Raise(nameof(ShowDetails)); Raise(nameof(ShowHover));
        Raise(nameof(ShowDefaultCompact)); Raise(nameof(ShowPluginCompact));
        LayoutChanged?.Invoke(this, EventArgs.Empty);
    }
    public void SetGamingMinimal(bool value)
    {
        GamingMinimal = value; SetState(NotchState.Compact); ActivateSelection();
        Raise(nameof(GamingMinimal)); Raise(nameof(ShowCompact)); Raise(nameof(ShowDetails)); Raise(nameof(ShowEvent));
        Raise(nameof(ShowDefaultCompact)); Raise(nameof(ShowPluginCompact));
        LayoutChanged?.Invoke(this, EventArgs.Empty);
    }
    private void ActivateSelection() => Host.Widgets.SetWindowSelection(this, ShowDetails ? SelectedWidget?.Widget.Id : null);
    private void Cycle(int direction)
    {
        if (VisibleWidgets.Count == 0) return;
        var index = SelectedWidget is null ? 0 : VisibleWidgets.IndexOf(SelectedWidget);
        SelectedWidget = VisibleWidgets[(index + direction + VisibleWidgets.Count) % VisibleWidgets.Count];
    }
    private void Changed(object? sender, PropertyChangedEventArgs e)
    {
        RefreshCompact(); Raise(nameof(ShowBattery)); Raise(nameof(GamingLabel));
    }
    private void FpsChanged(object? sender, PropertyChangedEventArgs e) { if (e.PropertyName == nameof(Services.FpsService.OverlayText)) Raise(nameof(GamingLabel)); }
    private void PluginChanged(object? sender, EventArgs e) { Raise(nameof(ShowDefaultCompact)); Raise(nameof(ShowPluginCompact)); LayoutChanged?.Invoke(this, EventArgs.Empty); }
    private void EventChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(EventService.Current)) return;
        Raise(nameof(ShowCompact)); Raise(nameof(ShowEvent)); LayoutChanged?.Invoke(this, EventArgs.Empty);
        Raise(nameof(ShowDefaultCompact)); Raise(nameof(ShowPluginCompact));
    }
    private void SettingsChanged(object? sender, EventArgs e)
    {
        RefreshCompact(); Raise(nameof(ShowDate)); Raise(nameof(ShowBattery)); LayoutChanged?.Invoke(this, EventArgs.Empty);
    }
    private void WidgetsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => RefreshWidgets();
    private void RefreshWidgets()
    {
        // Rebuild emits Clear/Add notifications. Do not replace the remembered id during an intermediate collection state.
        var wanted = Host.Settings.Current.LastSelectedWidgetId;
        VisibleWidgets.Clear();
        foreach (var entry in Host.Widgets.Visible) VisibleWidgets.Add(new(entry.Widget));
        _selectedWidget = VisibleWidgets.FirstOrDefault(x => x.Widget.Id == wanted) ?? VisibleWidgets.FirstOrDefault();
        foreach (var entry in VisibleWidgets) entry.IsSelected = entry == _selectedWidget;
        Raise(nameof(SelectedWidget)); Raise(nameof(ModeTitle)); Raise(nameof(HasWidgets));
        ActivateSelection(); CommandManager.InvalidateRequerySuggested(); LayoutChanged?.Invoke(this, EventArgs.Empty);
    }
    public void RefreshCompact()
    {
        var values = new List<CompactItem>();
        foreach (var id in Host.Settings.Current.CompactLayout)
        {
            CompactItem? item = id switch
            {
                "clock" => new() { Id = id, Text = ShowDate ? $"{Host.Clock.Time} · {DateTime.Now:dd.MM.}" : Host.Clock.Time },
                "battery" when ShowBattery => new() { Id = id, Glyph = "\uE83F", Text = Host.Battery.Label },
                "media" when Host.Media.Playing => new() { Id = id, Image = Host.Media.Cover, Glyph = Host.Media.Cover is null ? "\uE9D9" : "", Text = "" },
                "bluetooth" when Host.Bluetooth.CompactDevice is { } device => new() { Id = id, Glyph = "\uE7F6", Text = device.BatteryLabel },
                _ => null
            };
            if (item is not null) values.Add(item);
        }
        if (Host.Discord.ShowPreview) values.Add(new() { Id = "discord", Glyph = "\uE8F2", Text = Host.Discord.NotificationCount.ToString(CultureInfo.InvariantCulture) });
        if (!CompactItems.Select(x => x.Id).SequenceEqual(values.Select(x => x.Id)))
        {
            CompactItems.Clear(); foreach (var item in values) CompactItems.Add(item); LayoutChanged?.Invoke(this, EventArgs.Empty);
        }
        else for (var i = 0; i < values.Count; i++) { CompactItems[i].Text = values[i].Text; CompactItems[i].Glyph = values[i].Glyph; CompactItems[i].Image = values[i].Image; }
    }
    public void Dispose()
    {
        Host.Widgets.SetWindowSelection(this, null);
        Host.Clock.PropertyChanged -= Changed; Host.Battery.PropertyChanged -= Changed;
        Host.Fps.PropertyChanged -= FpsChanged;
        Host.Plugins.ActiveChanged -= PluginChanged;
        Host.Media.PropertyChanged -= Changed; Host.Bluetooth.PropertyChanged -= Changed;
        Host.Discord.PropertyChanged -= Changed; Host.Widgets.Visible.CollectionChanged -= WidgetsChanged;
        Host.Events.PropertyChanged -= EventChanged; Host.Settings.Changed -= SettingsChanged;
    }
}
