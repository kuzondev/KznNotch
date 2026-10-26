namespace KznNotch.Integrations;

public enum NotificationAccess { NotRequested, Allowed, Denied, InstallationRequired, Unavailable }
public interface IDiscordNotificationSource : IDisposable
{
    event EventHandler? Changed;
    Task<NotificationAccess> GetAccessAsync(bool requestPermission);
    Task<IReadOnlyList<DiscordNotification>> ReadAsync();
    void Stop();
}

public static class DiscordNotificationMapper
{
    public static bool IsDiscord(string? appId, string? displayName)
    {
        var id = appId ?? "";
        if (new[] { "com.squirrel.Discord.Discord", "com.squirrel.DiscordCanary.DiscordCanary", "com.squirrel.DiscordPTB.DiscordPTB", "com.squirrel.DiscordDevelopment.DiscordDevelopment" }.Any(x => string.Equals(id, x, StringComparison.OrdinalIgnoreCase))) return true;
        if (id.StartsWith("DiscordInc.Discord_", StringComparison.OrdinalIgnoreCase) && id.Contains('!')) return true;
        // Match the originating app only, never the notification's message text.
        return new[] { "Discord", "Discord PTB", "Discord Canary", "Discord Development" }.Any(x => string.Equals(displayName, x, StringComparison.OrdinalIgnoreCase));
    }
    public static DiscordNotification Map(uint id, IEnumerable<string> text, DateTimeOffset createdAt)
    {
        var lines = text.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToArray();
        return lines.Length switch
        {
            0 => new(id, "Discord", "Kein Vorschautext von Windows verfügbar", createdAt),
            1 => new(id, "Discord", lines[0], createdAt),
            _ => new(id, lines[0], string.Join(Environment.NewLine, lines.Skip(1)), createdAt)
        };
    }
}
