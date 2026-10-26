using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Windows.Controls;
using KznNotch.Store;

namespace KznNotch.Services;

public sealed record GitHubUpdate(Version Version, string Tag, string Url, string Sha256, string ReleaseUrl);

public static class GitHubUpdateService
{
    public const string Repository = "kuzondev/KznNotch";
    private static bool _checking;
    public static Version CurrentVersion => typeof(GitHubUpdateService).Assembly.GetName().Version ?? new(0, 0, 0, 0);
    private static bool German => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "de";
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

    public static async Task CheckAndPromptAsync(bool manual = false)
    {
        if (_checking) return; _checking = true;
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
        finally { _checking = false; }
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
            var publisher = "CN=Notchify Development";
            try { publisher = Windows.ApplicationModel.Package.Current.Id.Publisher; } catch (InvalidOperationException) { }
            var product = new StoreProduct { PackageName = "Notchify.Desktop", Publisher = publisher, Version = update.Version.ToString(), DownloadUrl = update.Url, Sha256 = update.Sha256 };
            var path = await StoreCatalog.DownloadAsync(product, Path.Combine(Log.DirectoryPath, "Updates"), new Progress<double>(p => progress.Value = p), cancellation.Token);
            if (!cancellation.IsCancellationRequested) Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        { Log.Error("Update download", ex); MessageBox.Show(T("Das Update konnte nicht installiert werden. Deine bisherige Version bleibt erhalten.", "The update could not be installed. Your current version is unchanged."), "Kzn Notch", MessageBoxButton.OK, MessageBoxImage.Warning); }
        finally { window.Close(); }
    }
}
