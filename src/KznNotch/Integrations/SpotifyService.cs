using KznNotch.Services;

namespace KznNotch.Integrations;

// Spotify's media session is the primary integration; no OAuth, scraping, or player-specific polling.
public sealed class SpotifyService(MediaService media)
{
    public MediaService Media => media;
    public bool Active => media.Available && media.Source == "Spotify";
    public void Open() => Process.Start(new ProcessStartInfo("spotify:") { UseShellExecute = true });
}
