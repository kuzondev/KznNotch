namespace KznNotch.Models;

public sealed record DiscordNotification(uint Id, string Sender, string Message, DateTimeOffset CreatedAt)
{
    public string TimeLabel => CreatedAt.ToLocalTime().ToString("dd.MM. · HH:mm");
}
