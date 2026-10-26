using KznNotch.Services;
using KznNotch.Storage;
using KznNotch.Widgets;
using KznNotch.Animations;

namespace KznNotch.Core;

public sealed class AppHost : IDisposable
{
    public JsonStore Store { get; }
    public SettingsService Settings { get; }
    public ClockService Clock { get; }
    public BatteryService Battery { get; } = new();
    public EventService Events { get; } = new();
    public NotificationService Notifications { get; }
    public MediaService Media { get; }
    public TimerService Timer { get; }
    public SystemStatsService Stats { get; } = new();
    public TodoService Todos { get; }
    public ReminderService Reminders { get; }
    public WidgetRegistry Widgets { get; }
    public AnimationService Animation { get; }
    public WindowService Windows { get; }
    public GameModeService Gaming { get; }
    public FpsService Fps { get; }
    public GameCatalogService Games { get; }
    public Plugins.KznPluginService Plugins { get; }
    public TrayService Tray { get; }
    public BluetoothService Bluetooth { get; }
    public DiscordService Discord { get; }
    public Integrations.SpotifyService Spotify { get; }
    public AudioService Audio { get; }
    private AppSettings? _appearancePreview;
    public AppSettings Appearance => _appearancePreview ?? Settings.Current;
    public event EventHandler? AppearanceChanged;
    public void PreviewAppearance(AppSettings? value) { _appearancePreview = value; UpdateTheme(this, EventArgs.Empty); AppearanceChanged?.Invoke(this, EventArgs.Empty); }
    public AppHost(string? dataDirectory = null, Func<bool?>? gameDetector = null, Action<bool>? configureStartup = null)
    {
        Store = new(dataDirectory);
        Settings = new(Store, configureStartup); Clock = new(Settings); Notifications = new(Settings); Media = new(Events);
        Games = new(Store); Plugins = new(Store);
        Timer = new(Store, Events, Notifications); Todos = new(Store); Reminders = new(Store, Events, Notifications);
        Bluetooth = new(Settings, Events); Discord = new(Settings); Spotify = new(Media);
        Audio = new(Events);
        Animation = new(Settings); Widgets = new(Settings);
        Widgets.Register(new MediaWidget(Media)); Widgets.Register(new TimerWidget(Timer)); Widgets.Register(new SystemStatsWidget(Stats));
        Widgets.Register(new TodoWidget(Todos)); Widgets.Register(new ReminderWidget(Reminders));
        Widgets.Register(new BluetoothWidget(Bluetooth)); Widgets.Register(new DiscordWidget(Discord)); Widgets.Rebuild();
        Gaming = new(Settings, gameDetector, Games); Fps = new(Gaming);
        Widgets.Register(new FpsWidget(Fps)); Widgets.Rebuild();
        Windows = new(this); Tray = new(this);
        Settings.Changed += UpdateTheme; SystemParameters.StaticPropertyChanged += SystemThemeChanged; UpdateTheme(this, EventArgs.Empty);
    }
    public async Task InitializeAsync() { Windows.ShowNotches(); await Games.InitializeAsync(); Gaming.Initialize(); Bluetooth.Initialize(); await Media.InitializeAsync(); }
    private void UpdateTheme(object? sender, EventArgs args)
    {
        var resources = Application.Current.Resources;
        var appearance = Appearance;
        foreach (var (name, value) in new Dictionary<string, string>
        {
            ["AccentBrush"] = appearance.Accent, ["TextBrush"] = appearance.TextColor, ["MutedBrush"] = appearance.SecondaryColor,
            ["IslandTextBrush"] = appearance.TextColor, ["IslandMutedBrush"] = appearance.SecondaryColor,
            ["IslandBackgroundBrush"] = appearance.Background, ["ControlBrush"] = appearance.ButtonColor,
            ["IslandButtonBrush"] = appearance.ButtonColor, ["PrimaryButtonBrush"] = appearance.PrimaryButtonColor,
            ["PrimaryButtonTextBrush"] = appearance.PrimaryButtonTextColor, ["NavigationBrush"] = appearance.NavigationColor,
            ["SelectionBrush"] = appearance.SelectionColor, ["SelectionTextBrush"] = appearance.SelectionTextColor,
            ["BarBrush"] = appearance.BarColor, ["BarTrackBrush"] = appearance.BarTrackColor, ["TimerBrush"] = appearance.TimerColor,
            ["StrokeBrush"] = appearance.BorderColor, ["DividerBrush"] = appearance.BorderColor
        })
            resources[name] = SystemParameters.HighContrast ? (name.Contains("Text") || name.Contains("Muted") ? SystemColors.WindowTextBrush : SystemColors.WindowBrush) : new SolidColorBrush((Color)ColorConverter.ConvertFromString(value));
        resources["BarThickness"] = appearance.BarThickness;
        resources["ControlRadius"] = new CornerRadius(appearance.ControlRadius);
        resources["NotchFont"] = new FontFamily(appearance.FontFamilyName);
        if (!SystemParameters.HighContrast) return;
        resources["AccentBrush"] = SystemParameters.HighContrast ? SystemColors.HighlightBrush : new SolidColorBrush((Color)ColorConverter.ConvertFromString(Settings.Current.Accent));
        resources["TextBrush"] = SystemParameters.HighContrast ? SystemColors.WindowTextBrush : new SolidColorBrush(Color.FromRgb(242, 243, 247));
        resources["MutedBrush"] = SystemParameters.HighContrast ? SystemColors.GrayTextBrush : new SolidColorBrush(Color.FromRgb(174, 174, 184));
        resources["SurfaceBrush"] = SystemParameters.HighContrast ? SystemColors.WindowBrush : new SolidColorBrush(Color.FromRgb(28, 28, 30));
        resources["ControlBrush"] = SystemParameters.HighContrast ? SystemColors.ControlBrush : new SolidColorBrush(Color.FromRgb(44, 44, 46));
        resources["StrokeBrush"] = SystemParameters.HighContrast ? SystemColors.WindowTextBrush : new SolidColorBrush(Color.FromRgb(58, 58, 60));
        resources["DividerBrush"] = SystemParameters.HighContrast ? SystemColors.GrayTextBrush : new SolidColorBrush(Color.FromRgb(48, 48, 51));
    }
    private void SystemThemeChanged(object? sender, PropertyChangedEventArgs args) { if (args.PropertyName == nameof(SystemParameters.HighContrast)) Ui.Run(() => UpdateTheme(this, EventArgs.Empty)); }
    public void Dispose()
    {
        Settings.Changed -= UpdateTheme;
        SystemParameters.StaticPropertyChanged -= SystemThemeChanged;
        Tray.Dispose(); Windows.Dispose(); Fps.Dispose(); Gaming.Dispose(); Games.Dispose(); Widgets.Dispose(); Clock.Dispose(); Battery.Dispose(); Media.Dispose();
        Timer.Dispose(); Stats.Dispose(); Reminders.Dispose(); Bluetooth.Dispose(); Discord.Dispose(); Audio.Dispose(); Events.Dispose();
    }
}
