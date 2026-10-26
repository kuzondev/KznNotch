using Microsoft.Win32;
using System.Windows.Interop;

namespace KznNotch.Utilities;

internal static class SettingsPalette
{
    internal static bool IsDark(SettingsAppearanceMode mode)
    {
        if (mode != SettingsAppearanceMode.System) return mode == SettingsAppearanceMode.Dark;
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
        }
        catch { return false; }
    }

    internal static void Apply(Window window, SettingsAppearanceMode mode)
    {
        var dark = IsDark(mode);
        var colors = new Dictionary<string, string>
        {
            ["CanvasBrush"] = dark ? "#101216" : "#F2F2F7",
            ["SidebarBrush"] = dark ? "#15181D" : "#F2F2F7",
            ["SurfaceBrush"] = dark ? "#1D2026" : "#FFFFFF",
            ["ControlBrush"] = dark ? "#292D35" : "#FFFFFF",
            ["TextBrush"] = dark ? "#F5F5F7" : "#1D1D1F",
            ["MutedBrush"] = dark ? "#A3AAB7" : "#6C6C70",
            ["AccentBrush"] = dark ? "#1682FF" : "#007AFF",
            ["PrimaryActionBrush"] = "#0066D6",
            ["StrokeBrush"] = dark ? "#414752" : "#D1D1D6",
            ["DividerBrush"] = dark ? "#343A44" : "#E5E5EA",
            ["HoverBrush"] = dark ? "#252B35" : "#DCDCE3",
            ["SelectionWashBrush"] = dark ? "#17345C" : "#E5E5EA",
            ["NavIconBrush"] = dark ? "#292F39" : "#E6E6EB",
            ["NavIconForegroundBrush"] = dark ? "#D1D7E0" : "#48484A",
            ["NavSelectedBrush"] = dark ? "#174A87" : "#0066D6",
            ["NavSelectedTextBrush"] = "#FFFFFF",
            ["BrandTileBrush"] = dark ? "#34343A" : "#252528",
            ["BrandMarkBrush"] = "#FFFFFF",
            ["PreviewBrush"] = dark ? "#252D3A" : "#E5EBF6",
            ["TrackBrush"] = dark ? "#555D6B" : "#D2D2D7",
            ["SwitchBrush"] = dark ? "#5A606B" : "#C7C7CC",
            ["SwitchOnBrush"] = "#34C759",
            ["ThumbBrush"] = "#FFFFFF",
            ["OnAccentBrush"] = "#FFFFFF",
            ["FeedbackBrush"] = dark ? "#FFFFFF" : "#111111",
            ["ScrollThumbBrush"] = dark ? "#8A8A94" : "#AAAAAF"
        };
        foreach (var (name, hex) in colors)
        {
            Brush brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            if (SystemParameters.HighContrast)
                brush = name switch
                {
                    "TextBrush" or "MutedBrush" or "StrokeBrush" or "DividerBrush" or "ScrollThumbBrush" or "NavIconForegroundBrush" => SystemColors.WindowTextBrush,
                    "AccentBrush" => SystemColors.HighlightBrush,
                    "PrimaryActionBrush" => SystemColors.HighlightBrush,
                    "OnAccentBrush" or "NavSelectedTextBrush" => SystemColors.HighlightTextBrush,
                    "ThumbBrush" or "BrandMarkBrush" => SystemColors.ControlTextBrush,
                    "NavIconBrush" or "NavSelectedBrush" or "BrandTileBrush" => SystemColors.WindowBrush,
                    "SwitchOnBrush" => SystemColors.HighlightBrush,
                    _ => SystemColors.WindowBrush
                };
            window.Resources[name] = brush;
        }
        if (new WindowInteropHelper(window).Handle != IntPtr.Zero) WindowMaterial.ApplyMica(window, dark);
    }
}
