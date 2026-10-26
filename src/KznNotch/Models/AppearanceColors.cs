namespace KznNotch.Models;

public static class AppearanceColors
{
    public static readonly AppSettings Defaults = new();
    public static readonly (string Key, string Label)[] Fields =
    [
        ("Background", "Notch-Hintergrund"), ("Accent", "Akzent & Fokus"), ("TextColor", "Primärer Text"),
        ("SecondaryColor", "Sekundärer Text & Symbole"), ("BarColor", "Fortschrittsbalken"),
        ("BarTrackColor", "Balken-Hintergrund"), ("ButtonColor", "Buttons & Eingabefelder"),
        ("PrimaryButtonColor", "Hauptbutton"), ("PrimaryButtonTextColor", "Text auf Hauptbutton"),
        ("NavigationColor", "Funktionsleiste"), ("SelectionColor", "Ausgewählter Tab"),
        ("SelectionTextColor", "Symbol auf ausgewähltem Tab"), ("TimerColor", "Fokus & Timer"), ("BorderColor", "Konturen & Trennlinien")
    ];
    public static readonly System.Reflection.PropertyInfo[] Properties = Fields.Select(x => typeof(AppSettings).GetProperty(x.Key)!).ToArray();
}
