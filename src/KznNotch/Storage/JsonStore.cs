using System.Text.Json;

namespace KznNotch.Storage;

public sealed class JsonStore
{
    private readonly string _directory;
    public string DirectoryPath => _directory;
    private readonly object _gate = new();
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    public JsonStore(string? directory = null)
    {
        _directory = directory ?? Log.DirectoryPath;
        Directory.CreateDirectory(_directory);
        if (directory is null)
        {
            // Preserve old data and never overwrite files from the renamed installation.
            var legacy = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Notchify");
            foreach (var name in new[] { "settings.json", "todos.json", "reminders.json", "timer.json" })
            {
                var oldPath = Path.Combine(legacy, name); var newPath = Path.Combine(_directory, name);
                if (File.Exists(oldPath) && !File.Exists(newPath)) File.Copy(oldPath, newPath);
            }
        }
    }
    public T Load<T>(string name, Func<T> fallback)
    {
        lock (_gate)
        {
            var path = Path.Combine(_directory, name);
            if (!File.Exists(path)) return fallback();
            try { return JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options) ?? fallback(); }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
            {
                Log.Error($"Read {name}", ex);
                if (ex is JsonException) File.Copy(path, path + $".corrupt-{DateTime.Now:yyyyMMddHHmmss}", true);
                return fallback();
            }
        }
    }
    public void Save<T>(string name, T value)
    {
        lock (_gate)
        {
            var path = Path.Combine(_directory, name);
            var temp = path + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(value, Options));
            File.Move(temp, path, true);
        }
    }
}
