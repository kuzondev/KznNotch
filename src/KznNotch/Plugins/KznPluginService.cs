using System.Text.Json;

namespace KznNotch.Plugins;

public sealed class KznPluginEntry
{
    public string Id { get; init; } = "";
    public string Directory { get; init; } = "";
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public string Path => System.IO.Path.Combine(Directory, "main.kzn");
}

public sealed class KznPluginService : ObservableObject
{
    private readonly Storage.JsonStore _store;
    private readonly string _root;
    private string _source = "", _editorDirectory = "", _activeDirectory = "", _status = "", _activeId = "";
    private KznPluginEntry? _selected;
    private KznDocument? _previewDocument, _activeDocument;
    public ObservableCollection<KznPluginEntry> Library { get; } = [];
    public string Source { get => _source; set { if (Set(ref _source, value)) { PreviewDocument = null; Status = "Ungespeicherte Änderungen · Vorschau prüfen."; } } }
    public string EditorDirectory { get => _editorDirectory; private set => Set(ref _editorDirectory, value); }
    public string ActiveDirectory { get => _activeDirectory; private set => Set(ref _activeDirectory, value); }
    public string Status { get => _status; private set => Set(ref _status, value); }
    public string ActiveLabel => ActiveDocument is { } doc ? "Aktiv: " + doc.Name : "Standardanzeige aktiv";
    public bool HasActive => ActiveDocument is not null;
    public KznDocument? ActiveDocument { get => _activeDocument; private set { Set(ref _activeDocument, value); Raise(nameof(HasActive)); Raise(nameof(ActiveLabel)); ActiveChanged?.Invoke(this, EventArgs.Empty); } }
    public KznDocument? PreviewDocument { get => _previewDocument; private set => Set(ref _previewDocument, value); }
    public KznPluginEntry? Selected
    {
        get => _selected;
        set
        {
            if (!Set(ref _selected, value) || value is null) return;
            try { EditorDirectory = value.Directory; Source = ReadCode(value.Path); Preview(); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { Status = ex.Message; }
        }
    }
    public ICommand PreviewCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand ActivateCommand { get; }
    public ICommand DisableCommand { get; }
    public ICommand NewCommand { get; }
    public event EventHandler? ActiveChanged;
    public void Notify(string message) => Status = message;
    public KznPluginService(Storage.JsonStore store)
    {
        _store = store; _root = Path.Combine(store.DirectoryPath, "Plugins"); Directory.CreateDirectory(_root);
        PreviewCommand = new RelayCommand(_ => Preview());
        SaveCommand = new RelayCommand(_ => Save(false));
        ActivateCommand = new RelayCommand(_ => Save(true));
        DisableCommand = new RelayCommand(_ => { _activeId = ""; _store.Save("plugin-state.json", ""); ActiveDirectory = ""; ActiveDocument = null; Status = "Plugin deaktiviert · normale Notch wiederhergestellt."; });
        NewCommand = new RelayCommand(template => New(template?.ToString() ?? "clock"));
        Seed("example-clock", ClockExample); Seed("example-media", MediaExample); Seed("example-focus", FocusExample);
        SeedImageExample(); Refresh();
        _activeId = store.Load("plugin-state.json", () => "");
        var active = Library.FirstOrDefault(x => x.Id == _activeId);
        if (active is not null)
        {
            try { var doc = KznCompiler.Compile(ReadCode(active.Path)); KznCompiler.ValidateAssets(doc, active.Directory); ActiveDirectory = active.Directory; ActiveDocument = doc; }
            catch (Exception ex) when (ex is IOException or KznCompileException or UnauthorizedAccessException or ArgumentException) { Status = "Gespeichertes Plugin nicht geladen: " + ex.Message; }
        }
        Selected = active ?? Library.FirstOrDefault();
    }
    public void Preview()
    {
        try { var doc = KznCompiler.Compile(Source); KznCompiler.ValidateAssets(doc, EditorDirectory); PreviewDocument = doc; Status = "Kzn 1 · gültig · " + doc.Nodes.Count + " Elemente. Vorschau ist noch nicht aktiviert."; }
        catch (Exception ex) when (ex is KznCompileException or IOException or ArgumentException or UnauthorizedAccessException) { PreviewDocument = null; Status = ex.Message; }
    }
    public void Save(bool activate)
    {
        try
        {
            var doc = KznCompiler.Compile(Source); KznCompiler.ValidateAssets(doc, EditorDirectory);
            Directory.CreateDirectory(EditorDirectory);
            var path = Path.Combine(EditorDirectory, "main.kzn"); File.WriteAllText(path + ".tmp", Source); File.Move(path + ".tmp", path, true);
            var id = Path.GetFileName(EditorDirectory); Refresh(); Selected = Library.First(x => x.Id == id);
            if (activate || _activeId == id)
            { _activeId = id; _store.Save("plugin-state.json", id); ActiveDirectory = EditorDirectory; ActiveDocument = doc; }
            Status = activate ? "Gespeichert und aktiviert. Das Plugin ersetzt die geschlossene Anzeige." : "Plugin gespeichert.";
        }
        catch (Exception ex) when (ex is KznCompileException or IOException or UnauthorizedAccessException or ArgumentException) { Status = ex.Message; }
    }
    public void New(string template)
    {
        Selected = null; EditorDirectory = Path.Combine(_root, Guid.NewGuid().ToString("N")); Directory.CreateDirectory(EditorDirectory);
        Source = template switch { "media" => MediaExample.Replace("Medien kompakt", "Mein Medienplugin"), "focus" => FocusExample.Replace("Fokus-Schnellstart", "Mein Fokusplugin"), _ => ClockExample.Replace("Uhr pur", "Mein Plugin") };
        Preview();
    }
    public void Import(string path)
    {
        try
        {
            var code = ReadCode(path); var doc = KznCompiler.Compile(code); var origin = Path.GetDirectoryName(Path.GetFullPath(path))!;
            KznCompiler.ValidateAssets(doc, origin);
            var destination = Path.Combine(_root, Guid.NewGuid().ToString("N")); Directory.CreateDirectory(destination);
            foreach (var image in doc.Nodes.Where(x => x.Kind == "image"))
            { var target = KznCompiler.AssetPath(destination, image.Value); Directory.CreateDirectory(Path.GetDirectoryName(target)!); File.Copy(KznCompiler.AssetPath(origin, image.Value), target, true); }
            Selected = null; EditorDirectory = destination; Source = code; Save(false); Status = ".kzn importiert. Zum Anwenden aktivieren.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or KznCompileException) { Status = ex.Message; }
    }
    public void Export(string destination)
    {
        try
        {
            var doc = KznCompiler.Compile(Source); KznCompiler.ValidateAssets(doc, EditorDirectory);
            var lines = Source.Replace("\r", "").Split('\n'); var folder = Path.GetDirectoryName(Path.GetFullPath(destination))!;
            var assetRoot = "assets/kzn-" + Guid.NewGuid().ToString("N");
            foreach (var node in doc.Nodes.Where(x => x.Kind == "image"))
            {
                var relative = assetRoot + "/" + Guid.NewGuid().ToString("N") + Path.GetExtension(node.Value);
                var target = KznCompiler.AssetPath(folder, relative); Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(KznCompiler.AssetPath(EditorDirectory, node.Value), target);
                lines[node.Line - 1] = $"image {JsonSerializer.Serialize(relative)} {node.Size}";
            }
            File.WriteAllText(destination, string.Join(Environment.NewLine, lines));
            Status = ".kzn exportiert. Zugehörigen assets-Ordner beim Teilen mitgeben.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or KznCompileException) { Status = ex.Message; }
    }
    public void AddImage(string path)
    {
        try
        {
            var relative = "assets/" + Guid.NewGuid().ToString("N") + Path.GetExtension(path).ToLowerInvariant();
            var target = KznCompiler.AssetPath(EditorDirectory, relative);
            if (new FileInfo(path).Length > 10 * 1024 * 1024) throw new IOException("Bild darf höchstens 10 MB groß sein.");
            Directory.CreateDirectory(Path.GetDirectoryName(target)!); File.Copy(path, target);
            Source += Environment.NewLine + $"image {JsonSerializer.Serialize(relative)} 28";
            Preview(); Status = "Bild eingefügt · zum Anwenden speichern und aktivieren.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or KznCompileException) { Status = ex.Message; }
    }
    public void NewImage(string path)
    { New("clock"); Source = "kzn 1\nplugin \"Mein Bild\"\ndescription \"Bild statt Uhr\"\ntarget compact\n"; AddImage(path); }
    private void Refresh()
    {
        Library.Clear();
        foreach (var directory in Directory.EnumerateDirectories(_root).OrderBy(x => x))
        {
            var path = Path.Combine(directory, "main.kzn"); if (!File.Exists(path)) continue;
            string name = Path.GetFileName(directory), description = "Code prüfen";
            try { var doc = KznCompiler.Compile(ReadCode(path)); name = doc.Name; description = doc.Description; }
            catch (Exception ex) when (ex is IOException or KznCompileException or UnauthorizedAccessException) { }
            Library.Add(new() { Id = Path.GetFileName(directory), Directory = directory, Name = name, Description = description });
        }
    }
    private static string ReadCode(string path) => new FileInfo(path).Length <= 65536 ? File.ReadAllText(path) : throw new IOException(".kzn-Datei ist größer als 64 KB.");
    private void Seed(string id, string code)
    { var directory = Path.Combine(_root, id); Directory.CreateDirectory(directory); var path = Path.Combine(directory, "main.kzn"); if (!File.Exists(path)) File.WriteAllText(path, code); }
    private void SeedImageExample()
    {
        var directory = Path.Combine(_root, "example-image"); var asset = Path.Combine(directory, "assets", "kzn.png");
        if (File.Exists(Path.Combine(directory, "main.kzn"))) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(asset)!);
            using var icon = System.Drawing.Icon.ExtractAssociatedIcon(Path.Combine(AppContext.BaseDirectory, "KznNotch.exe"));
            if (icon is null) return;
            using var bitmap = icon.ToBitmap(); bitmap.Save(asset, System.Drawing.Imaging.ImageFormat.Png);
            Seed("example-image", "kzn 1\nplugin \"Bild statt Uhr\"\ndescription \"Eigenes Bild dauerhaft in der geschlossenen Notch\"\ntarget compact\nimage \"assets/kzn.png\" 28\n");
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or System.Runtime.InteropServices.ExternalException) { }
    }
    public const string ClockExample = "kzn 1\nplugin \"Uhr pur\"\ndescription \"Eigene Schrift und Uhrzeit\"\ntarget compact\nfont \"Segoe UI\" 14\ncolor \"#F2F3F7\"\nclock \"HH:mm\"\n";
    public const string MediaExample = "kzn 1\nplugin \"Medien kompakt\"\ndescription \"Titel während der Wiedergabe\"\ntarget compact\nfont \"Segoe UI\" 12\nspacing 8\nclock \"HH:mm\"\nwhen media.playing\ntext \"{media.title}\"\nend\n";
    public const string FocusExample = "kzn 1\nplugin \"Fokus-Schnellstart\"\ndescription \"Fokus per Button öffnen\"\ntarget compact\nfont \"Segoe UI\" 12\nclock \"HH:mm\"\nbutton \"Fokus\" timer\nwhen timer.running\ntext \"{timer}\"\nend\n";
}
