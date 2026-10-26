using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using KznNotch.Services;

namespace KznNotch.Smoke;

internal static class UpdateChecks
{
    public static async Task RunAsync(Action<bool, string> check)
    {
        string Release(string tag, string digest, bool prerelease = false, string host = "github.com") => JsonSerializer.Serialize(new
        {
            draft = false, prerelease, tag_name = tag,
            html_url = $"https://github.com/{GitHubUpdateService.Repository}/releases/tag/{tag}",
            assets = new[] { new { name = $"KznNotch-{tag.TrimStart('v')}-x64.msix", digest,
                browser_download_url = $"https://{host}/{GitHubUpdateService.Repository}/releases/download/{tag}/KznNotch-{tag.TrimStart('v')}-x64.msix" } }
        });
        var valid = Release("v0.10.0", "sha256:" + new string('a', 64));
        check(GitHubUpdateService.Parse(valid, new(0, 9, 0, 0))?.Version == new Version(0, 10, 0, 0), "Updater vergleicht GitHub-Versionen numerisch statt alphabetisch");
        check(GitHubUpdateService.Parse(valid, new(0, 10, 0, 0)) is null && GitHubUpdateService.Parse(valid, new(0, 11, 0, 0)) is null,
            "Updater bietet gleiche Versionen und Downgrades nicht an");
        check(GitHubUpdateService.Parse(Release("v0.10.0", "sha256:" + new string('a', 64), true), new(0, 9, 0)) is null,
            "Updater ignoriert Vorabversionen");
        foreach (var bad in new[] { Release("v0.10.0", ""), Release("v0.10.0", "sha256:" + new string('a', 64), host: "example.invalid") })
        {
            bool rejected = false; try { GitHubUpdateService.Parse(bad, new(0, 9, 0)); } catch (InvalidDataException) { rejected = true; }
            check(rejected, "Updater lehnt fremde Downloadquellen oder fehlende Prüfsummen ab");
        }
        check(await GitHubUpdateService.CheckAsync(new MissingRelease()) is null, "Updater behandelt noch nicht vorhandene Releases ohne Startfehler");
    }
    private sealed class MissingRelease : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound) { RequestMessage = request });
    }
}
