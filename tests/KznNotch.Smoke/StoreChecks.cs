using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using KznNotch.Store;
using KznNotch.Views;

namespace KznNotch.Smoke;

internal static class StoreChecks
{
    public static async Task RunAsync(string root, Action<bool, string> check)
    {
        var dir = Path.Combine(root, "store-checks"); Directory.CreateDirectory(dir);
        var catalog = StoreCatalog.Default;
        catalog.Products.Add(new() { Id = "next-app", Name = "Next App", Version = "1.0.0.0", PackageName = "Kzn.Next", Publisher = "CN=Kzn", Description = new() { ["en"] = "Next app" } });
        var path = Path.Combine(dir, "catalog.json"); File.WriteAllText(path, JsonSerializer.Serialize(catalog));
        check(StoreCatalog.Read(path).Products.Count == 2 && catalog.Products[1].Describe("de") == "Next app", "Store unterstützt mehrere Programme und Sprach-Fallback");
        var product = catalog.Products[1]; product.DownloadUrl = "http://example.invalid/app.msix";
        File.WriteAllText(path, JsonSerializer.Serialize(catalog));
        bool rejected = false; try { StoreCatalog.Read(path); } catch (InvalidDataException) { rejected = true; }
        check(rejected, "Store lehnt unsichere Downloadquellen ab");
        product.DownloadUrl = "https://example.invalid/app.msix"; product.Sha256 = new string('a', 64);
        var packagePath = Path.Combine(dir, "fixture.msix");
        using (var zip = ZipFile.Open(packagePath, ZipArchiveMode.Create))
        { using var writer = new StreamWriter(zip.CreateEntry("AppxManifest.xml").Open()); writer.Write("<Package><Identity Name=\"Kzn.Next\" Publisher=\"CN=Kzn\" Version=\"1.0.0.0\"/></Package>"); }
        StoreCatalog.ValidatePackage(packagePath, product);
        var bytes = File.ReadAllBytes(packagePath); product.Sha256 = Convert.ToHexString(SHA256.HashData(bytes));
        var downloaded = await StoreCatalog.DownloadAsync(product, Path.Combine(dir, "downloads"), new Progress<double>(), CancellationToken.None, new FixtureHandler(bytes));
        check(File.Exists(downloaded), "Store prüft heruntergeladene MSIX-Prüfsumme und Paketidentität");
        product.Publisher = "CN=Other"; rejected = false;
        try { StoreCatalog.ValidatePackage(packagePath, product); } catch (InvalidDataException) { rejected = true; }
        check(rejected, "Store lehnt abweichenden Paket-Herausgeber ab"); product.Publisher = "CN=Kzn";
        product.Sha256 = new string('0', 64); rejected = false;
        var badDir = Path.Combine(dir, "bad-download");
        try { await StoreCatalog.DownloadAsync(product, badDir, new Progress<double>(), CancellationToken.None, new FixtureHandler(bytes)); } catch (InvalidDataException) { rejected = true; }
        check(rejected && !Directory.EnumerateFiles(badDir).Any(), "Store verwirft manipulierte Downloads vollständig");
        product.DownloadUrl = ""; File.WriteAllText(path, JsonSerializer.Serialize(catalog));
    }
    public static async Task WindowAsync(string root, Action<bool, string> check, Action<FrameworkElement, string> capture)
    {
        var dir = Path.Combine(root, "store-ui"); Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "preferences.json"), "{\"Language\":\"de\"}");
        var window = new StoreWindow(dir);
        try
        {
            window.Show(); await Task.Delay(150);
            check(window.IsVisible && Descendants<TextBlock>(window).Any(x => x.Text == "Deine Kzn-Programme."), "Store zeigt echte mehrsprachige Oberfläche");
            capture((FrameworkElement)window.Content, Path.Combine(root, "store-de.png"));
            Descendants<ComboBox>(window).Single().SelectedIndex = 1; await Task.Delay(100);
            check(Descendants<TextBlock>(window).Any(x => x.Text == "Your Kzn apps."), "Sprachwechsel aktualisiert den Store unmittelbar");
            capture((FrameworkElement)window.Content, Path.Combine(root, "store-en.png"));
            Descendants<TextBox>(window).Single().Text = "not-found";
            check(Descendants<TextBlock>(window).Any(x => x.Text == "No matching apps found."), "Store-Suche zeigt einen lokalisierten Leerzustand");
            window.Width = 740; await Task.Delay(50);
            capture((FrameworkElement)window.Content, Path.Combine(root, "store-small.png"));
        }
        finally { window.Close(); }
        var reopened = new StoreWindow(dir);
        try { reopened.Show(); check(Descendants<ComboBox>(reopened).Single().SelectedIndex == 1, "Store merkt sich die ausgewählte Sprache"); }
        finally { reopened.Close(); }
    }
    private static System.Collections.Generic.IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i); if (child is T typed) yield return typed;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }
    private sealed class FixtureHandler(byte[] bytes) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { RequestMessage = request, Content = new ByteArrayContent(bytes) });
    }
}
