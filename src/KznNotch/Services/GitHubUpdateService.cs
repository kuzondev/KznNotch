using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Xml.Linq;
using System.Windows.Controls;

namespace KznNotch.Services;

public sealed record GitHubUpdate(Version Version, string Tag, string Url, string Sha256, string ReleaseUrl);

public static class GitHubUpdateService
{
    public const string Repository = "kuzondev/KznNotch";
    private static bool _checking;
    public static Version CurrentVersion => typeof(GitHubUpdateService).Assembly.GetName().Version ?? new(0, 0, 0, 0);
    private static readonly AsyncLocal<string?> _language = new();
    private static bool German => _language.Value == "de" || _language.Value is null && CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "de";
    private static string T(string de, string en) => German ? de : en;
    private static Version Normalize(Version v) => new(v.Major, v.Minor, Math.Max(0, v.Build), Math.Max(0, v.Revision));

    public static GitHubUpdate? Parse(string json, Version current)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.GetProperty("draft").GetBoolean() || root.GetProperty("prerelease").GetBoolean()) return null;
        var tag = root.GetProperty("tag_name").GetString() ?? "";
        var number = tag.StartsWith('v') ? tag[1..] : tag;
        if (!Version.TryParse(number, out var version) || Normalize(version) <= Normalize(current)) return null;
        var releaseUrl = root.GetProperty("html_url").GetString() ?? "";
        if (!TrustedUrl(releaseUrl, $"/{Repository}/releases/tag/")) throw new InvalidDataException("Unexpected release URL.");
        var asset = root.GetProperty("assets").EnumerateArray().FirstOrDefault(a => a.GetProperty("name").GetString() == $"KznNotch-{number}-x64.msix");
        if (asset.ValueKind == JsonValueKind.Undefined) return null;
        var url = asset.GetProperty("browser_download_url").GetString() ?? "";
        var digest = asset.TryGetProperty("digest", out var value) ? value.GetString() ?? "" : "";
        if (!TrustedUrl(url, $"/{Repository}/releases/download/{tag}/") || !digest.StartsWith("sha256:", StringComparison.Ordinal)
            || digest.Length != 71 || !digest[7..].All(Uri.IsHexDigit)) throw new InvalidDataException("Release asset has no valid SHA-256 or source.");
        return new(Normalize(version), tag, url, digest[7..], releaseUrl);
    }
    private static bool TrustedUrl(string value, string prefix) => Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && uri.Scheme == "https" && uri.Host == "github.com" && uri.Port == 443 && string.IsNullOrEmpty(uri.UserInfo)
        && uri.AbsolutePath.StartsWith(prefix, StringComparison.Ordinal) && uri.Query.Length == 0 && uri.Fragment.Length == 0;

    public static async Task<GitHubUpdate?> CheckAsync(HttpMessageHandler? handler = null)
    {
        using var client = new HttpClient(handler ?? new HttpClientHandler()) { Timeout = TimeSpan.FromSeconds(5) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"KznNotch/{CurrentVersion}");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var response = await client.GetAsync($"https://api.github.com/repos/{Repository}/releases/latest", HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > 1024 * 1024) throw new InvalidDataException("Release response too large.");
        await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
        using var buffer = new MemoryStream(); var bytes = new byte[16384]; int read;
        while ((read = await stream.ReadAsync(bytes, timeout.Token)) > 0)
        { if (buffer.Length + read > 1024 * 1024) throw new InvalidDataException("Release response too large."); buffer.Write(bytes, 0, read); }
        return Parse(System.Text.Encoding.UTF8.GetString(buffer.ToArray()), CurrentVersion);
    }

    public static async Task CheckAndPromptAsync(bool manual = false, string? language = null)
    {
        if (_checking) return; _checking = true;
        var previousLanguage = _language.Value; _language.Value = language;
        try
        {
            var update = await CheckAsync();
            Log.Write(update is null ? "GitHub update check: no newer compatible release." : "GitHub update check: " + update.Tag);
            if (Application.Current.Dispatcher.HasShutdownStarted) return;
            if (update is null)
            {
                if (manual) MessageBox.Show(T("Kein neueres installierbares Update verfügbar.", "No newer compatible update is available."), "Kzn Notch", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var message = T($"Kzn Notch {update.Tag} ist verfügbar.\nDeine Version: {CurrentVersion}\n\nMöchtest du jetzt aktualisieren?",
                $"Kzn Notch {update.Tag} is available.\nYour version: {CurrentVersion}\n\nDo you want to update?");
            if (MessageBox.Show(message, T("Update verfügbar", "Update available"), MessageBoxButton.YesNo, MessageBoxImage.Information, MessageBoxResult.No) != MessageBoxResult.Yes) return;
            await DownloadAndInstallAsync(update);
        }
        catch (Exception ex)
        {
            Log.Error("GitHub update check", ex);
            if (manual) MessageBox.Show(T("Die Updatesuche ist momentan nicht möglich. Bitte später erneut versuchen.", "Could not check for updates. Please try again later."), "Kzn Notch", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        finally { _checking = false; _language.Value = previousLanguage; }
    }
    private static async Task DownloadAndInstallAsync(GitHubUpdate update)
    {
        using var cancellation = new CancellationTokenSource();
        var progress = new ProgressBar { Minimum = 0, Maximum = 100, Height = 5, Margin = new Thickness(0, 18, 0, 18) };
        var cancel = new Button { Content = T("Abbrechen", "Cancel"), Padding = new Thickness(16, 6, 16, 6), HorizontalAlignment = HorizontalAlignment.Right };
        var text = new TextBlock { Text = T("Update wird heruntergeladen und geprüft…", "Downloading and verifying update…"), TextWrapping = TextWrapping.Wrap };
        var panel = new StackPanel { Margin = new Thickness(24) }; panel.Children.Add(text); panel.Children.Add(progress); panel.Children.Add(cancel);
        var window = new Window { Title = "Kzn Notch · Update", Width = 420, SizeToContent = SizeToContent.Height, ResizeMode = ResizeMode.NoResize, WindowStartupLocation = WindowStartupLocation.CenterScreen, Content = panel };
        Utilities.SettingsPalette.Apply(window, SettingsAppearanceMode.System);
        window.SetResourceReference(Window.BackgroundProperty, "CanvasBrush"); text.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
        cancel.Click += (_, _) => window.Close(); window.Closed += (_, _) => cancellation.Cancel(); window.Show();
        try
        {
            var path = await DownloadAndVerifyPackageAsync(update, Path.Combine(Log.DirectoryPath, "Updates"), new Progress<double>(p => progress.Value = p), cancellation.Token);
            if (!cancellation.IsCancellationRequested) Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        { Log.Error("Update download", ex); MessageBox.Show(T("Das Update konnte nicht installiert werden. Deine bisherige Version bleibt erhalten.", "The update could not be installed. Your current version is unchanged."), "Kzn Notch", MessageBoxButton.OK, MessageBoxImage.Warning); }
        finally { window.Close(); }
    }

    private static async Task<string> DownloadAndVerifyPackageAsync(GitHubUpdate update, string directory, IProgress<double> progress, CancellationToken cancellation)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".msix");
        using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(15) };
        try
        {
            using var response = await client.GetAsync(update.Url, HttpCompletionOption.ResponseHeadersRead, cancellation);
            response.EnsureSuccessStatusCode();
            if (response.RequestMessage?.RequestUri is not { Scheme: "https" } || response.RequestMessage.RequestUri.UserInfo.Length != 0)
                throw new InvalidDataException("Update must be served over HTTPS.");
            const long maximum = 1024L * 1024 * 1024;
            if (response.Content.Headers.ContentLength > maximum) throw new InvalidDataException("Update package too large.");
            await using var input = await response.Content.ReadAsStreamAsync(cancellation);
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            await using (var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
            {
                var buffer = new byte[81920]; long received = 0; int count;
                while ((count = await input.ReadAsync(buffer, cancellation)) > 0)
                {
                    received += count;
                    if (received > maximum) throw new InvalidDataException("Update package too large.");
                    hash.AppendData(buffer, 0, count);
                    await output.WriteAsync(buffer.AsMemory(0, count), cancellation);
                    var total = response.Content.Headers.ContentLength;
                    if (total is > 0) progress.Report(received * 100d / total.Value);
                }
            }
            if (!Convert.ToHexString(hash.GetHashAndReset()).Equals(update.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Update SHA-256 mismatch.");
            using var archive = ZipFile.OpenRead(path);
            var manifest = archive.GetEntry("AppxManifest.xml") ?? throw new InvalidDataException("MSIX manifest missing.");
            if (manifest.Length > 1024 * 1024) throw new InvalidDataException("MSIX manifest too large.");
            using var stream = manifest.Open();
            using var reader = System.Xml.XmlReader.Create(stream, new() { DtdProcessing = System.Xml.DtdProcessing.Prohibit, MaxCharactersInDocument = 1024 * 1024 });
            var identity = XDocument.Load(reader).Root?.Elements().FirstOrDefault(x => x.Name.LocalName == "Identity");
            if ((string?)identity?.Attribute("Name") != "Notchify.Desktop"
                || (string?)identity?.Attribute("Publisher") != "CN=Notchify Development"
                || !Version.TryParse((string?)identity?.Attribute("Version"), out var version) || version != update.Version)
                throw new InvalidDataException("MSIX identity or version does not match the release.");
            return path;
        }
        catch { if (File.Exists(path)) File.Delete(path); throw; }
    }
}
