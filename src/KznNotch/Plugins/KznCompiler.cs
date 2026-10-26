using System.Text.Json;
using System.Text.RegularExpressions;

namespace KznNotch.Plugins;

public sealed record KznNode(string Kind, string Value, int Size, string? Condition, string? Action, int Line);
public sealed class KznDocument
{
    public string Name { get; set; } = "Mein Plugin";
    public string Description { get; set; } = "";
    public string Font { get; set; } = "Segoe UI";
    public double FontSize { get; set; } = 13;
    public string Color { get; set; } = "#F2F3F7";
    public double Spacing { get; set; } = 8;
    public bool Vertical { get; set; }
    public List<KznNode> Nodes { get; } = [];
}
public sealed class KznCompileException(int line, string message) : Exception($"Zeile {line}: {message}") { }

public static class KznCompiler
{
    private static readonly Regex Tokens = new("\"(?:\\\\.|[^\"\\\\])*\"|\\S+", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
    public static KznDocument Compile(string source)
    {
        if (System.Text.Encoding.UTF8.GetByteCount(source) > 65536) throw new KznCompileException(1, "Eine .kzn-Datei darf höchstens 64 KB enthalten.");
        var document = new KznDocument(); var header = false; string? condition = null;
        var lines = source.Replace("\r", "").Split('\n');
        if (lines.Length > 512) throw new KznCompileException(1, "Höchstens 512 Zeilen sind erlaubt.");
        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index].Trim(); var number = index + 1;
            if (line.Length == 0 || line.StartsWith("//", StringComparison.Ordinal)) continue;
            string[] values;
            try { values = Tokens.Matches(line).Select(m => m.Value.StartsWith('"') ? JsonSerializer.Deserialize<string>(m.Value)! : m.Value).ToArray(); }
            catch (Exception ex) when (ex is JsonException or RegexMatchTimeoutException) { throw new KznCompileException(number, "Ungültige Zeichenfolge. Pfade mit / schreiben, Text in doppelte Anführungszeichen setzen."); }
            if (values.Length == 0) continue;
            void Count(int count) { if (values.Length != count) throw new KznCompileException(number, $"{values[0]} erwartet {count - 1} Argument(e)."); }
            double Numeric(int at, double min, double max)
            {
                if (!double.TryParse(values[at], NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || !double.IsFinite(value) || value < min || value > max)
                    throw new KznCompileException(number, $"Zahl zwischen {min} und {max} erwartet.");
                return value;
            }
            if (!header)
            { if (values.Length != 2 || values[0] != "kzn" || values[1] != "1") throw new KznCompileException(number, "Mit kzn 1 beginnen."); header = true; continue; }
            if (condition is not null && values[0] is not ("text" or "clock" or "image" or "button" or "end")) throw new KznCompileException(number, "Im when-Block sind nur Anzeigeelemente erlaubt.");
            switch (values[0])
            {
                case "plugin": Count(2); if (values[1].Length is < 1 or > 80) throw new KznCompileException(number, "Pluginname: 1 bis 80 Zeichen."); document.Name = values[1]; break;
                case "description": Count(2); if (values[1].Length > 240) throw new KznCompileException(number, "Beschreibung zu lang."); document.Description = values[1]; break;
                case "target": Count(2); if (values[1] != "compact") throw new KznCompileException(number, "Kzn 1 unterstützt target compact."); break;
                case "font": Count(3); if (values[1].Length > 120 || values[1].IndexOfAny([':', '/', '\\']) >= 0) throw new KznCompileException(number, "Name einer installierten Schriftart erwartet."); document.Font = values[1]; document.FontSize = Numeric(2, 8, 32); break;
                case "color": Count(2); try { _ = (Color)ColorConverter.ConvertFromString(values[1]); document.Color = values[1]; } catch { throw new KznCompileException(number, "Gültige Farbe wie #A8BDFF erwartet."); } break;
                case "spacing": Count(2); document.Spacing = Numeric(1, 0, 24); break;
                case "layout": Count(2); if (values[1] is not ("row" or "column")) throw new KznCompileException(number, "layout row oder layout column erwartet."); document.Vertical = values[1] == "column"; break;
                case "when": Count(2); if (condition is not null || values[1] is not ("media.playing" or "timer.running" or "gaming.active")) throw new KznCompileException(number, "when media.playing, timer.running oder gaming.active; keine Verschachtelung."); condition = values[1]; break;
                case "end": Count(1); if (condition is null) throw new KznCompileException(number, "Kein when-Block offen."); condition = null; break;
                case "text": Count(2); if (values[1].Length > 500) throw new KznCompileException(number, "Text ist zu lang."); document.Nodes.Add(new("text", values[1], 0, condition, null, number)); break;
                case "clock": Count(2); try { if (values[1].Length > 60) throw new FormatException(); _ = DateTime.Now.ToString(values[1], CultureInfo.CurrentCulture); } catch (FormatException) { throw new KznCompileException(number, "Ungültiges Uhrzeitformat."); } document.Nodes.Add(new("clock", values[1], 0, condition, null, number)); break;
                case "image": Count(3); _ = AssetPath(Path.GetTempPath(), values[1]); document.Nodes.Add(new("image", values[1], (int)Numeric(2, 12, 64), condition, null, number)); break;
                case "button": Count(3); if (values[2] is not ("media" or "timer" or "system" or "todo" or "reminder" or "bluetooth" or "discord" or "fps" or "settings")) throw new KznCompileException(number, "Unbekannte Notch-Aktion."); document.Nodes.Add(new("button", values[1], 0, condition, values[2], number)); break;
                default: throw new KznCompileException(number, $"Unbekannter Befehl: {values[0]}");
            }
            if (document.Nodes.Count > 12) throw new KznCompileException(number, "Maximal 12 Anzeigeelemente pro Plugin.");
        }
        if (!header || document.Nodes.Count == 0) throw new KznCompileException(lines.Length, "Mindestens ein text-, clock-, image- oder button-Element ergänzen.");
        if (condition is not null) throw new KznCompileException(lines.Length, "when-Block mit end schließen.");
        return document;
    }
    public static string AssetPath(string directory, string relative)
    {
        if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative) || relative.Contains(':')) throw new KznCompileException(1, "Bilder müssen einen relativen lokalen Pfad besitzen.");
        var path = Path.GetFullPath(Path.Combine(directory, relative.Replace('/', Path.DirectorySeparatorChar)));
        if (!path.StartsWith(Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new KznCompileException(1, "Bildpfad verlässt den Plugin-Ordner.");
        if (!new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".ico" }.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase)) throw new KznCompileException(1, "Bilder: PNG, JPG, BMP, GIF oder ICO verwenden.");
        return path;
    }
    public static void ValidateAssets(KznDocument document, string directory)
    {
        foreach (var node in document.Nodes.Where(x => x.Kind == "image"))
        {
            var path = AssetPath(directory, node.Value);
            if (!File.Exists(path) || new FileInfo(path).Length > 10 * 1024 * 1024) throw new KznCompileException(node.Line, "Bild fehlt oder ist größer als 10 MB: " + node.Value);
            try
            {
                using var stream = File.OpenRead(path);
                var decoder = System.Windows.Media.Imaging.BitmapDecoder.Create(stream, System.Windows.Media.Imaging.BitmapCreateOptions.DelayCreation, System.Windows.Media.Imaging.BitmapCacheOption.None);
                if (decoder.Frames.Count == 0 || decoder.Frames[0].PixelWidth > 16000 || decoder.Frames[0].PixelHeight > 16000) throw new IOException("Bildabmessungen zu groß.");
            }
            catch (Exception ex) when (ex is IOException or NotSupportedException or FileFormatException or ArgumentException)
            { throw new KznCompileException(node.Line, "Bild kann nicht geladen werden: " + node.Value); }
        }
    }
}
