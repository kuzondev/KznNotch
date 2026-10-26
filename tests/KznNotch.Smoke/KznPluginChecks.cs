using System.IO;
using KznNotch.Plugins;
using KznNotch.Services;
using KznNotch.Storage;

namespace KznNotch.Smoke;

internal static class KznPluginChecks
{
    internal static void Run(string root, Action<bool, string> check)
    {
        var doc = KznCompiler.Compile("kzn 1\nplugin \"Eigene Uhr\"\nfont \"Consolas\" 16\ncolor \"#A8BDFF\"\nclock \"HH:mm:ss\"\nwhen media.playing\ntext \"{media.title}\"\nend\nbutton \"Fokus\" timer\n");
        check(doc.Name == "Eigene Uhr" && doc.Font == "Consolas" && doc.FontSize == 16 && doc.Nodes.Count == 3 && doc.Nodes[1].Condition == "media.playing", "Kzn 1 kompiliert Schrift, Uhrzeit, Bedingungen und Buttons");
        foreach (var source in new[] { "kzn 2\ntext \"x\"", "kzn 1\nexec \"calc.exe\"", "kzn 1\nimage \"../secret.png\" 28", "kzn 1\nimage \"https://example.org/x.png\" 28", "kzn 1\nfont \"file:///font.ttf\" 12\ntext \"x\"", "kzn 1\nwhen media.playing\ntext \"x\"" })
        {
            var rejected = false; try { KznCompiler.Compile(source); } catch (KznCompileException) { rejected = true; }
            check(rejected, "Kzn lehnt ungültige Version, Fremdcode, externe Pfade oder offene Blöcke ab");
        }
        var directory = Path.Combine(root, "plugin-checks"); Directory.CreateDirectory(directory);
        var store = new JsonStore(directory); var plugins = new KznPluginService(store);
        check(plugins.Library.Count >= 3, "Lokale Plugin-Bibliothek enthält startfähige Vorlagen");
        plugins.New("clock"); plugins.Source = "kzn 1\nplugin \"Persistenztest\"\nfont \"Consolas\" 15\nclock \"HH:mm\"\n"; plugins.Save(true);
        check(plugins.HasActive && new KznPluginService(store).ActiveDocument?.Name == "Persistenztest", "Aktives .kzn-Plugin und Schrift bleiben nach Neustart erhalten");
        plugins.Source = "kzn 1\nexec \"bad\""; plugins.Save(true);
        check(plugins.ActiveDocument?.Name == "Persistenztest" && new KznPluginService(store).ActiveDocument?.Name == "Persistenztest", "Fehlerhafter Code überschreibt kein funktionsfähiges aktives Plugin");
        var input = Path.Combine(directory, "source"); Directory.CreateDirectory(Path.Combine(input, "assets"));
        File.WriteAllBytes(Path.Combine(input, "assets", "pixel.png"), Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aN1cAAAAASUVORK5CYII="));
        File.WriteAllText(Path.Combine(input, "image.kzn"), "kzn 1\nplugin \"Importbild\"\nimage \"assets/pixel.png\" 28\n");
        plugins.Import(Path.Combine(input, "image.kzn")); plugins.Save(true);
        check(plugins.ActiveDocument?.Nodes.Single().Kind == "image" && File.Exists(Path.Combine(plugins.ActiveDirectory, "assets", "pixel.png")), "Bild-Plugin importiert Assets in die eigene Bibliothek");
        var export = Path.Combine(directory, "export"); Directory.CreateDirectory(export); plugins.Export(Path.Combine(export, "shared.kzn"));
        var exported = KznCompiler.Compile(File.ReadAllText(Path.Combine(export, "shared.kzn"))); KznCompiler.ValidateAssets(exported, export);
        check(exported.Nodes.Single().Value.StartsWith("assets/kzn-"), ".kzn-Export enthält portable relative Bilder statt Verweisen auf den Ursprungsordner");
        plugins.DisableCommand.Execute(null); check(!plugins.HasActive && !new KznPluginService(store).HasActive, "Standardanzeige deaktiviert das Plugin dauerhaft");
    }
    internal static void Games(string root, Action<bool, string> check)
    {
        var steam = Path.Combine(root, "test-steam"); Directory.CreateDirectory(Path.Combine(steam, "steamapps", "common", "Test Game"));
        File.WriteAllText(Path.Combine(steam, "steamapps", "appmanifest_1.acf"), "\"AppState\" { \"name\" \"Test Game\" \"installdir\" \"Test Game\" }");
        var epic = Path.Combine(root, "test-epic"); Directory.CreateDirectory(epic); var epicGame = Path.Combine(root, "epic-game"); Directory.CreateDirectory(epicGame);
        File.WriteAllText(Path.Combine(epic, "test.item"), System.Text.Json.JsonSerializer.Serialize(new { DisplayName = "Epic Test", InstallLocation = epicGame }));
        var xbox = Path.Combine(root, "test-xbox"); Directory.CreateDirectory(Path.Combine(xbox, "Xbox Test"));
        var found = GameCatalogService.Discover([steam], epic, [xbox]);
        check(found.Count == 3 && found.Any(x => x.Name == "Test Game") && found.Any(x => x.Name == "Epic Test"), "Spielebibliotheken werden ohne manuelle EXE-Liste aus Manifesten erkannt");
        check(!GameCatalogService.IsWithin(Path.Combine(root, "test-steam-fake", "game.exe"), steam), "Spieleerkennung verwechselt ähnlich benannte Nachbarordner nicht");
        var store = new JsonStore(Path.Combine(root, "catalog")); using var catalog = new GameCatalogService(store);
        catalog.Observe("newgame.exe", Path.Combine(root, "newgame.exe"));
        using var restored = new GameCatalogService(store);
        check(restored.Games.Count == 1 && restored.ContainsPath(Path.Combine(root, "newgame.exe")), "Neu erkannte Spiele werden automatisch lokal gemerkt");
    }
}
