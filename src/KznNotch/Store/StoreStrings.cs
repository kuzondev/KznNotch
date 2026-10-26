namespace KznNotch.Store;

public sealed class StoreStrings
{
    public string Language { get; set; } = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "de" ? "de" : "en";
    private static readonly Dictionary<string, (string De, string En)> Text = new()
    {
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
