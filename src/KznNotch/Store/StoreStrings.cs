namespace KznNotch.Store;

public sealed class StoreStrings
{
    public string Language { get; set; } = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "de" ? "de" : "en";
    private static readonly Dictionary<string, (string De, string En)> Text = new()
    {
        ["Today"] = ("Heute", "Today"), ["Apps"] = ("Apps", "Apps"),
        ["Featured"] = ("IM FOKUS", "FEATURED"), ["HeroTitle"] = ("Kleine Notch.\nGroße Möglichkeiten.", "Small notch.\nBig possibilities."),
        ["HeroSubtitle"] = ("Musik, Fokus und dein Alltag. Immer griffbereit.", "Music, focus and your day. Always within reach."),
        ["ViewApp"] = ("App entdecken", "Explore app"), ["ForYou"] = ("Für deinen Alltag", "For your everyday"),
        ["CheckUpdates"] = ("Updates prüfen", "Check for updates"), ["Checking"] = ("Updates werden geprüft…", "Checking for updates…"),
        ["Stars"] = ("GitHub-Sterne", "GitHub stars"), ["Community"] = ("Community", "Community"),
        ["LoadingStars"] = ("Wird geladen…", "Loading…"), ["OfflineStars"] = ("Nicht verfügbar", "Unavailable"),
        ["StarsHint"] = ("Sterne des GitHub-Repositories, keine App-Bewertung.", "Stars on the GitHub repository, not an app rating."),
        ["AboutApp"] = ("Über diese App", "About this app"), ["Version"] = ("Version", "Version"),
        ["Platform"] = ("Plattform", "Platform"), ["Back"] = ("Zurück", "Back"), ["Source"] = ("Auf GitHub ansehen", "View on GitHub"),
        ["Preview"] = ("VORSCHAU", "PREVIEW"), ["AllApps"] = ("Alle Apps", "All apps"),
        ["Discover"] = ("Entdecken", "Discover"), ["Library"] = ("Mediathek", "Library"),
        ["Title"] = ("Deine Kzn-Programme.", "Your Kzn apps."),
        ["Subtitle"] = ("Ein Platz für alle deine Programme. Weitere Apps kommen mit neuen Katalogen hinzu.", "One place for all your apps. New catalogs bring more apps."),
        ["Search"] = ("Programme suchen", "Search apps"), ["Import"] = ("Katalog hinzufügen", "Add catalog"),
        ["Refresh"] = ("Aktualisieren", "Refresh"), ["Installed"] = ("Installiert", "Installed"),
        ["Available"] = ("Verfügbar", "Available"), ["Open"] = ("Öffnen", "Open"),
        ["Install"] = ("Installieren", "Install"), ["Update"] = ("Update laden", "Get update"),
        ["Unavailable"] = ("Noch kein Download", "No download yet"), ["Cancel"] = ("Abbrechen", "Cancel"),
        ["Empty"] = ("Keine passenden Programme gefunden.", "No matching apps found."),
        ["Ready"] = ("Bereit. Installationen werden mit dem Windows-App-Installer bestätigt.", "Ready. Confirm installations in Windows App Installer."),
        ["Downloading"] = ("Installer wird heruntergeladen und geprüft…", "Downloading and verifying installer…"),
        ["Installer"] = ("Geprüft. Bestätige die Installation im Windows-App-Installer und aktualisiere danach die Mediathek.", "Verified. Confirm installation in Windows App Installer, then refresh your library."),
        ["Imported"] = ("Katalog geprüft und übernommen.", "Catalog validated and added."),
        ["Error"] = ("Aktion fehlgeschlagen: ", "Action failed: "), ["Cancelled"] = ("Download abgebrochen.", "Download cancelled."),
        ["Publisher"] = ("Herausgeber", "Publisher"),
        ["Language"] = ("Sprache", "Language"), ["Catalog"] = ("Lokaler Programmkatalog", "Local app catalog"),
        ["CatalogNote"] = ("Importiere nur Kataloge aus einer Quelle, der du vertraust. Sie legen Downloads und Herausgeber fest.", "Only import catalogs from a source you trust. They define downloads and publishers.")
    };
    public string this[string key] => Text.TryGetValue(key, out var item) ? Language == "de" ? item.De : item.En : key;
}
