using Microsoft.Win32;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace KznNotch.Services;

public sealed class GameCatalogEntry
{
    public string Name { get; set; } = "";
    public string RootPath { get; set; } = "";
    public string Source { get; set; } = "";
    public DateTimeOffset? LastSeen { get; set; }
    public string Detail => Source + (LastSeen is { } seen ? $" · zuletzt {seen:dd.MM. HH:mm}" : " · automatisch gefunden");
}

public sealed class GameCatalogService : ObservableObject, IDisposable
{
    private readonly Storage.JsonStore _store;
    private readonly List<GameCatalogEntry> _learned;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMinutes(1) };
    private List<GameCatalogEntry> _installed = [];
    private bool _refreshing, _disposed;
    public ObservableCollection<GameCatalogEntry> Games { get; } = [];
    public string Status { get; private set; } = "Spiele werden beim Start automatisch ermittelt.";
    public ICommand RefreshCommand { get; }
    public GameCatalogService(Storage.JsonStore store)
    {
        _store = store; _learned = store.Load("detected-games.json", () => new List<GameCatalogEntry>()).Where(x => x is not null && !string.IsNullOrWhiteSpace(x.RootPath)).Take(5000).ToList();
        RefreshCommand = new AsyncCommand(RefreshAsync, () => !_refreshing);
        _timer.Tick += async (_, _) => await RefreshAsync(); Rebuild();
    }
    public async Task InitializeAsync() { await RefreshAsync(); if (!_disposed) _timer.Start(); }
    public async Task RefreshAsync()
    {
        if (_refreshing || _disposed) return;
        _refreshing = true;
        try
        {
            var installed = await Task.Run(ScanWindows);
            if (_disposed) return;
            _installed = installed; Rebuild();
            Status = $"{Games.Count} Spiele · Steam, Epic, Xbox und GOG · Aktualisierung jede Minute";
            Raise(nameof(Status));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        { Status = "Einige Bibliotheken sind nicht lesbar. Erkannte Spiele bleiben verfügbar."; Raise(nameof(Status)); }
        finally { _refreshing = false; CommandManager.InvalidateRequerySuggested(); }
    }
    public bool ContainsPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        return _installed.Concat(_learned).Any(x => IsWithin(path, x.RootPath));
    }
    public static bool IsWithin(string path, string root)
    {
        try
        {
            var full = Path.GetFullPath(path); var folder = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return full.Equals(folder, StringComparison.OrdinalIgnoreCase) || full.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException) { return false; }
    }
    public void Observe(string executable, string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        var known = _learned.FirstOrDefault(x => x.RootPath.Equals(path, StringComparison.OrdinalIgnoreCase));
        if (known is not null && DateTimeOffset.Now - known.LastSeen < TimeSpan.FromMinutes(1)) return;
        var installed = _installed.FirstOrDefault(x => IsWithin(path, x.RootPath));
        if (known is null) _learned.Add(known = new() { Name = installed?.Name ?? executable, RootPath = path, Source = installed?.Source ?? "Windows-Vollbilderkennung" });
        known.LastSeen = DateTimeOffset.Now;
        _store.Save("detected-games.json", _learned); Rebuild();
    }
    private void Rebuild()
    {
        var entries = _installed.Concat(_learned.Where(x => !_installed.Any(i => IsWithin(x.RootPath, i.RootPath))))
            .GroupBy(x => x.RootPath, StringComparer.OrdinalIgnoreCase).Select(x => x.First()).OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase);
        Games.Clear(); foreach (var entry in entries) Games.Add(entry);
    }
    public static List<GameCatalogEntry> Discover(IEnumerable<string> steamRoots, string epicDirectory, IEnumerable<string> xboxRoots)
    {
        var games = new List<GameCatalogEntry>();
        foreach (var steam in steamRoots.Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { steam };
            var libraries = Path.Combine(steam, "steamapps", "libraryfolders.vdf");
            if (File.Exists(libraries)) foreach (Match match in Regex.Matches(ReadSmall(libraries), "\"path\"\\s+\"([^\"]+)\"", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1))) roots.Add(match.Groups[1].Value.Replace("\\\\", "\\"));
            foreach (var root in roots)
            {
                var apps = Path.Combine(root, "steamapps"); if (!Directory.Exists(apps)) continue;
                foreach (var manifest in SafeFiles(apps, "appmanifest_*.acf"))
                {
                    try
                    {
                        var text = ReadSmall(manifest);
                        string Field(string key) => Regex.Match(text, "\"" + key + "\"\\s+\"([^\"]+)\"", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)).Groups[1].Value;
                        var folder = Field("installdir"); var install = Path.GetFullPath(Path.Combine(apps, "common", folder));
                        if (folder.Length > 0 && IsWithin(install, Path.Combine(apps, "common")) && Directory.Exists(install)) games.Add(new() { Name = Field("name") is { Length: > 0 } name ? name : folder, RootPath = install, Source = "Steam" });
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or RegexMatchTimeoutException) { }
                }
            }
        }
        if (Directory.Exists(epicDirectory)) foreach (var manifest in SafeFiles(epicDirectory, "*.item"))
        {
            try
            {
                using var json = JsonDocument.Parse(ReadSmall(manifest)); var item = json.RootElement;
                if (item.TryGetProperty("InstallLocation", out var location) && item.TryGetProperty("DisplayName", out var name) && location.GetString() is { } path && Directory.Exists(path))
                    games.Add(new() { Name = name.GetString() ?? "Epic-Spiel", RootPath = path, Source = "Epic" });
            }
            catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException or InvalidOperationException) { }
        }
        foreach (var root in xboxRoots.Where(Directory.Exists))
        {
            try { foreach (var path in Directory.EnumerateDirectories(root).Take(1000)) games.Add(new() { Name = Path.GetFileName(path), RootPath = path, Source = "Xbox" }); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        }
        return games;
    }
    private static List<GameCatalogEntry> ScanWindows()
    {
        var steam = new List<string> { Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam") };
        try { if (Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) is string path) steam.Add(path); }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException) { }
        var xbox = DriveInfo.GetDrives().Where(x => x.DriveType == DriveType.Fixed && x.IsReady).Select(x => Path.Combine(x.RootDirectory.FullName, "XboxGames"));
        var games = Discover(steam, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Epic", "EpicGamesLauncher", "Data", "Manifests"), xbox);
        foreach (var view in new[] { RegistryView.Registry32, RegistryView.Registry64 })
        {
            try
            {
                using var machine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
                using var gog = machine.OpenSubKey(@"SOFTWARE\GOG.com\Games"); if (gog is null) continue;
                foreach (var id in gog.GetSubKeyNames().Take(5000))
                { using var game = gog.OpenSubKey(id); if (game?.GetValue("path") is string folder && Directory.Exists(folder)) games.Add(new() { Name = game.GetValue("gameName") as string ?? id, RootPath = folder, Source = "GOG" }); }
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException) { }
        }
        return games;
    }
    private static string[] SafeFiles(string directory, string pattern) { try { return Directory.EnumerateFiles(directory, pattern).Take(5000).ToArray(); } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return []; } }
    private static string ReadSmall(string path)
    { try { return new FileInfo(path).Length <= 1024 * 1024 ? File.ReadAllText(path) : ""; } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return ""; } }
    public void Dispose() { _disposed = true; _timer.Stop(); }
}
