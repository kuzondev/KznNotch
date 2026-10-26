namespace KznNotch.Models;

public enum NotchState { Compact, Hover, Expanded }
public enum NotchPosition { Center, Left, Right }
public enum MonitorMode { Primary, Selected, All }
public enum NotchPresentation { ScreenEdge, DynamicIsland }
public enum GamingMode { Active, Minimal, Hidden, Fps }
public enum GamingCorner { TopRight, TopLeft, BottomRight, BottomLeft }
public enum SettingsAppearanceMode { System, Light, Dark }

public sealed class WidgetSettings
{
    public string Id { get; set; } = "";
    public bool Enabled { get; set; } = true;
}

public sealed class AppSettings
{
    public SettingsAppearanceMode SettingsAppearance { get; set; } = SettingsAppearanceMode.Dark;
    public GamingMode GamingMode { get; set; } = GamingMode.Hidden;
    public GamingCorner GamingCorner { get; set; } = GamingCorner.TopRight;
    public NotchPresentation Presentation { get; set; } = NotchPresentation.ScreenEdge;
    public string LastSelectedWidgetId { get; set; } = "media";
    public double CompactWidth { get; set; } = 220;
    public double CompactHeight { get; set; } = 40;
    public double HoverWidth { get; set; } = 430;
    public double ExpandedWidth { get; set; } = 600;
    public double CornerRadius { get; set; } = 24;
    public double Opacity { get; set; } = .97;
    public string Background { get; set; } = "#080808";
    public string Accent { get; set; } = "#B9A3FF";
    public string TextColor { get; set; } = "#F2F3F7";
    public string SecondaryColor { get; set; } = "#AEAEB8";
    public string BarColor { get; set; } = "#DDDEE5";
    public string BarTrackColor { get; set; } = "#29292E";
    public string ButtonColor { get; set; } = "#222225";
    public string PrimaryButtonColor { get; set; } = "#F3F3F5";
    public string PrimaryButtonTextColor { get; set; } = "#111113";
    public string NavigationColor { get; set; } = "#202023";
    public string SelectionColor { get; set; } = "#F5F5F7";
    public string SelectionTextColor { get; set; } = "#171719";
    public string TimerColor { get; set; } = "#FFAA45";
    public string BorderColor { get; set; } = "#3A3A3C";
    public double OpenHeight { get; set; } = 340;
    public double BarThickness { get; set; } = 4;
    public double ControlRadius { get; set; } = 13;
    public string FontFamilyName { get; set; } = "Segoe UI Variable Text";
    public bool Blur { get; set; } = true;
    public bool Shadow { get; set; } = true;
    public double AnimationMilliseconds { get; set; } = 260;
    public double OffsetX { get; set; }
    public double OffsetY { get; set; } = 10;
    public NotchPosition Position { get; set; }
    public MonitorMode Monitors { get; set; }
    public string MonitorDevice { get; set; } = "";
    public bool ShowDate { get; set; }
    public bool ShowSeconds { get; set; }
    public bool ShowBattery { get; set; } = true;
    public bool StartWithWindows { get; set; }
    public bool ClickThroughCompact { get; set; }
    public bool Notifications { get; set; } = true;
    public bool BluetoothEnabled { get; set; } = true;
    public bool AirPodsEnabled { get; set; }
    public bool DiscordEnabled { get; set; }
    public string CompactDeviceId { get; set; } = "";
    public List<string> TrackedBluetoothIds { get; set; } = [];
    public List<string> CompactLayout { get; set; } = ["battery", "bluetooth", "media", "clock"];
    public List<WidgetSettings> Widgets { get; set; } = [
        new() { Id = "media" }, new() { Id = "timer" }, new() { Id = "system" },
        new() { Id = "todo" }, new() { Id = "reminder" }, new() { Id = "bluetooth" },
        new() { Id = "discord", Enabled = false }
    ];

    public void Normalize()
    {
        if (!Enum.IsDefined(SettingsAppearance)) SettingsAppearance = SettingsAppearanceMode.System;
        if (!Enum.IsDefined(GamingMode)) GamingMode = GamingMode.Hidden;
        if (!Enum.IsDefined(GamingCorner)) GamingCorner = GamingCorner.TopRight;
        if (!Enum.IsDefined(Presentation)) Presentation = NotchPresentation.ScreenEdge;
        if (string.IsNullOrWhiteSpace(LastSelectedWidgetId)) LastSelectedWidgetId = "media";
        CompactWidth = Finite(CompactWidth, 220, 160, 500);
        CompactHeight = Finite(CompactHeight, 40, 32, 72);
        HoverWidth = Finite(HoverWidth, 430, CompactWidth, 650);
        ExpandedWidth = Finite(ExpandedWidth, 600, Math.Max(480, HoverWidth), 900);
        CornerRadius = Finite(CornerRadius, 24, 8, 48);
        Opacity = Finite(Opacity, .97, .35, 1);
        AnimationMilliseconds = Finite(AnimationMilliseconds, 300, 0, 1000);
        OffsetX = Finite(OffsetX, 0, -10000, 10000);
        OffsetY = Finite(OffsetY, 10, 0, 10000);
        if (!Enum.IsDefined(Position)) Position = NotchPosition.Center;
        if (!Enum.IsDefined(Monitors)) Monitors = MonitorMode.Primary;
        if (!ValidColor(Background)) Background = "#080808";
        if (!ValidColor(Accent)) Accent = "#B9A3FF";
        foreach (var property in AppearanceColors.Properties)
            if (!ValidColor((string?)property.GetValue(this))) property.SetValue(this, property.GetValue(AppearanceColors.Defaults));
        OpenHeight = Finite(OpenHeight, 340, 340, 460);
        BarThickness = Finite(BarThickness, 4, 2, 8);
        ControlRadius = Finite(ControlRadius, 13, 4, 24);
        if (string.IsNullOrWhiteSpace(FontFamilyName) || FontFamilyName.Length > 120 || FontFamilyName.IndexOfAny([':', '/', '\\']) >= 0) FontFamilyName = "Segoe UI Variable Text";
        TrackedBluetoothIds ??= [];
        CompactLayout ??= ["battery", "bluetooth", "media", "clock"];
        CompactLayout = CompactLayout.Where(x => new[] { "clock", "battery", "bluetooth", "media" }.Contains(x)).Distinct().ToList();
        if (!CompactLayout.Contains("clock")) CompactLayout.Add("clock");
        Widgets ??= [];
        var defaults = new[] { "media", "timer", "system", "todo", "reminder", "bluetooth", "discord", "fps" };
        Widgets = Widgets.Where(x => x is not null && defaults.Contains(x.Id)).DistinctBy(x => x.Id).ToList();
        foreach (var id in defaults) if (Widgets.All(x => x.Id != id)) Widgets.Add(new() { Id = id, Enabled = id is not ("discord" or "fps") });
    }
    private static double Finite(double value, double fallback, double min, double max) => double.IsFinite(value) ? Math.Clamp(value, min, max) : fallback;
    private static bool ValidColor(string? color)
    {
        try { return color is not null && ColorConverter.ConvertFromString(color) is Color; }
        catch { return false; }
    }
}
