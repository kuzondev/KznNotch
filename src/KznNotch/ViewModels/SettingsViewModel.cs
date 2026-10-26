using System.Text.Json;

namespace KznNotch.ViewModels;

public sealed class WidgetChoice : ObservableObject
{
    private bool _enabled;
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Glyph => Id switch { "media" => "\uE8D6", "timer" => "\uE916", "system" => "\uE9D9", "todo" => "\uE73A", "reminder" => "\uEA8F", "bluetooth" => "\uE702", "discord" => "\uE8F2", "fps" => "\uE7FC", "clock" => "\uE916", "battery" => "\uEBAA", _ => "\uE713" };
    public bool Enabled { get => _enabled; set => Set(ref _enabled, value); }
}
public sealed class MonitorChoice
{
    public string Device { get; init; } = "";
    public string Name { get; init; } = "";
}
public sealed class BluetoothChoice(BluetoothDeviceModel device, bool selected) : ObservableObject
{
    private bool _selected = selected;
    public BluetoothDeviceModel Device { get; } = device;
    public bool Selected { get => _selected; set => Set(ref _selected, value); }
}

public sealed class SettingsViewModel : ObservableObject, IDisposable
{
    public AppHost Host { get; }
    public AppSettings Edit { get; }
    public ObservableCollection<AppearanceColorOption> Colors { get; } = [];
    public ICommand ApplyColorPresetCommand { get; }
    public double PreviewBarThickness { get => Edit.BarThickness; set { Edit.BarThickness = value; Raise(); Host.PreviewAppearance(Edit); } }
    public double PreviewControlRadius { get => Edit.ControlRadius; set { Edit.ControlRadius = value; Raise(); Host.PreviewAppearance(Edit); } }
    public double PreviewOpenHeight { get => Edit.OpenHeight; set { Edit.OpenHeight = value; Raise(); Host.PreviewAppearance(Edit); } }
    public string PreviewFontFamily { get => Edit.FontFamilyName; set { if (string.IsNullOrWhiteSpace(value)) return; Edit.FontFamilyName = value; Raise(); Host.PreviewAppearance(Edit); } }
    public List<string> FontFamilies { get; } = Fonts.SystemFontFamilies.Select(x => x.Source).OrderBy(x => x).ToList();
    private bool _settingPreset;
    public string ColorContrast
    {
        get
        {
            static double Luminance(string value)
            {
                var color = (Color)ColorConverter.ConvertFromString(value);
                static double C(byte b) { var v = b / 255d; return v <= .04045 ? v / 12.92 : Math.Pow((v + .055) / 1.055, 2.4); }
                return .2126 * C(color.R) + .7152 * C(color.G) + .0722 * C(color.B);
            }
            static double Ratio(double a, double b) => (Math.Max(a, b) + .05) / (Math.Min(a, b) + .05);
            var surface = Luminance(Edit.Background);
            return $"Kontrast: Text {Ratio(Luminance(Edit.TextColor), surface):0.0}:1 · Sekundär {Ratio(Luminance(Edit.SecondaryColor), surface):0.0}:1";
        }
    }
    public int AppearanceIndex
    {
        get => (int)Edit.SettingsAppearance;
        set { Edit.SettingsAppearance = (SettingsAppearanceMode)value; Raise(); }
    }
    public ObservableCollection<WidgetChoice> Widgets { get; }
    public ObservableCollection<WidgetChoice> CompactWidgets { get; }
    public ObservableCollection<BluetoothChoice> Devices { get; } = [];
    public List<MonitorChoice> Monitors { get; } = System.Windows.Forms.Screen.AllScreens.Select((x, i) => new MonitorChoice { Device = x.DeviceName, Name = $"Bildschirm {i + 1} · {x.Bounds.Width} × {x.Bounds.Height}{(x.Primary ? " · Hauptmonitor" : "")}" }).ToList();
    public Array Positions { get; } = Enum.GetValues<NotchPosition>();
    public Array MonitorModes { get; } = Enum.GetValues<MonitorMode>();
    public int GamingModeIndex { get => (int)Edit.GamingMode; set { Edit.GamingMode = (GamingMode)value; Raise(); } }
    public int GamingCornerIndex { get => (int)Edit.GamingCorner; set { Edit.GamingCorner = (GamingCorner)value; Raise(); } }
    private string _message = "";
    public string Message { get => _message; set => Set(ref _message, value); }
    public bool ScreenEdgeSelected
    {
        get => Edit.Presentation == NotchPresentation.ScreenEdge;
        set { if (!value) return; Edit.Presentation = NotchPresentation.ScreenEdge; Raise(); Raise(nameof(IslandSelected)); }
    }
    public bool IslandSelected
    {
        get => Edit.Presentation == NotchPresentation.DynamicIsland;
        set { if (!value) return; Edit.Presentation = NotchPresentation.DynamicIsland; Raise(); Raise(nameof(ScreenEdgeSelected)); }
    }
    public bool DiscordIntegrationEnabled
    {
        get => Edit.DiscordEnabled;
        set
        {
            if (Edit.DiscordEnabled == value) return;
            Edit.DiscordEnabled = value; Raise(); _ = SaveDiscordIntegrationAsync(value);
        }
    }
    private async Task SaveDiscordIntegrationAsync(bool enabled)
    {
        try
        {
            var snapshot = JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(Host.Settings.Current))!;
            snapshot.DiscordEnabled = enabled;
            var widget = snapshot.Widgets.FirstOrDefault(x => x.Id == "discord"); if (widget is not null) widget.Enabled = enabled;
            if (enabled && widget is not null) { snapshot.Widgets.Remove(widget); snapshot.Widgets.Insert(0, widget); }
            var choice = Widgets.FirstOrDefault(x => x.Id == "discord"); if (choice is not null) choice.Enabled = enabled;
            if (enabled && choice is not null) Widgets.Move(Widgets.IndexOf(choice), 0);
            Host.Settings.Save(snapshot);
            if (enabled) await Host.Discord.RequestAccessAsync();
            Message = enabled ? "Discord Integration aktiviert." : "Discord Integration deaktiviert.";
        }
        catch (Exception ex) { Log.Error("Discord integration settings", ex); Message = "Discord-Einstellung konnte nicht gespeichert werden."; }
    }
    public ICommand SaveCommand { get; }
    public ICommand ResetCommand { get; }
    public SettingsViewModel(AppHost host)
    {
        Host = host;
        Edit = JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(host.Settings.Current))!;
        foreach (var (key, label) in AppearanceColors.Fields)
        {
            var option = new AppearanceColorOption(key, label, (string)typeof(AppSettings).GetProperty(key)!.GetValue(Edit)!);
            option.PropertyChanged += ColorChanged; Colors.Add(option);
        }
        ApplyColorPresetCommand = new RelayCommand(value => ApplyPreset(value?.ToString() ?? "default"));
        SyncDevices(); host.Bluetooth.Devices.CollectionChanged += DevicesChanged;
        var names = new Dictionary<string, string> { ["clock"] = "Uhr", ["battery"] = "Computerakku", ["bluetooth"] = "Bluetooth-Akku", ["media"] = "Medien", ["timer"] = "Timer & Fokus", ["system"] = "Systemauslastung", ["todo"] = "Aufgaben", ["reminder"] = "Erinnerungen", ["discord"] = "Discord", ["fps"] = "Gaming & FPS" };
        Widgets = new(Edit.Widgets.Select(x => new WidgetChoice { Id = x.Id, Name = names.GetValueOrDefault(x.Id, x.Id), Enabled = x.Enabled }));
        CompactWidgets = new(Edit.CompactLayout.Concat(new[] { "clock", "battery", "bluetooth", "media" }.Except(Edit.CompactLayout)).Select(x => new WidgetChoice { Id = x, Name = names.GetValueOrDefault(x, x), Enabled = Edit.CompactLayout.Contains(x) }));
        SaveCommand = new RelayCommand(_ => Save());
        ResetCommand = new RelayCommand(_ => { host.Settings.Save(new AppSettings()); Message = "Standardwerte geladen. Fenster erneut öffnen, um sie zu bearbeiten."; });
    }
    private void Save()
    {
        try
        {
            // Invalid colors are surfaced before normalization so accidental input is not silently discarded.
            if (Colors.Any(x => !x.IsValid)) throw new FormatException();
            Edit.Widgets = Widgets.Select(x => new WidgetSettings { Id = x.Id, Enabled = x.Enabled }).ToList();
            Edit.CompactLayout = CompactWidgets.Where(x => x.Enabled || x.Id == "clock").Select(x => x.Id).ToList();
            Edit.TrackedBluetoothIds = Devices.Where(x => x.Selected).Select(x => x.Device.Id).Concat(Edit.TrackedBluetoothIds.Where(id => Devices.All(x => x.Device.Id != id))).Distinct().ToList();
            // Save a copy: later edits in this window must not mutate the running configuration.
            var snapshot = JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(Edit))!;
            snapshot.LastSelectedWidgetId = Host.Settings.Current.LastSelectedWidgetId;
            Host.Settings.Save(snapshot); Message = "Gespeichert. Deine Notch wurde aktualisiert.";
        }
        catch (FormatException) { Message = "Bitte gültige Farben wie #080808 oder #B9A3FF eingeben."; }
        catch (Exception ex) { Log.Error("Save settings", ex); Message = $"Speichern fehlgeschlagen: {ex.Message}"; }
    }
    private void SyncDevices()
    {
        foreach (var old in Devices.Where(x => !Host.Bluetooth.Devices.Contains(x.Device)).ToList()) Devices.Remove(old);
        foreach (var device in Host.Bluetooth.Devices.Where(d => Devices.All(x => x.Device != d))) Devices.Add(new(device, Edit.TrackedBluetoothIds.Contains(device.Id)));
    }
    private void DevicesChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => SyncDevices();
    private void ColorChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(AppearanceColorOption.Value) || sender is not AppearanceColorOption option || !option.IsValid) return;
        typeof(AppSettings).GetProperty(option.Key)!.SetValue(Edit, option.Value);
        Raise(nameof(ColorContrast));
        if (!_settingPreset) Host.PreviewAppearance(Edit);
    }
    private void ApplyPreset(string name)
    {
        _settingPreset = true;
        foreach (var option in Colors) option.Value = (string)typeof(AppSettings).GetProperty(option.Key)!.GetValue(AppearanceColors.Defaults)!;
        var accent = name == "ocean" ? "#7DD3FC" : name == "forest" ? "#8DE0B0" : Edit.Accent;
        if (name != "default")
            foreach (var option in Colors.Where(x => x.Key is "Accent" or "BarColor" or "TimerColor")) option.Value = accent;
        _settingPreset = false; Host.PreviewAppearance(Edit);
    }
    public void Dispose()
    {
        Host.Bluetooth.Devices.CollectionChanged -= DevicesChanged;
        foreach (var color in Colors) color.PropertyChanged -= ColorChanged;
        Host.PreviewAppearance(null);
    }
}
