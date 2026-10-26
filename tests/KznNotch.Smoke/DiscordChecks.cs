using System.IO;
using KznNotch.Core;
using KznNotch.Models;
using KznNotch.Services;
using KznNotch.Storage;
using KznNotch.Integrations;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using KznNotch.Views;

namespace KznNotch.Smoke;

internal static class DiscordChecks
{
    public static async Task RunAsync(string directory, Action<bool, string> check)
    {
        check(DiscordNotificationMapper.IsDiscord("com.squirrel.Discord.Discord", "") && DiscordNotificationMapper.IsDiscord("com.squirrel.DiscordPTB.DiscordPTB", "Discord PTB") && !DiscordNotificationMapper.IsDiscord("Mail", "Message about Discord"), "Discord-Filter verwendet die Absender-App, nicht Nachrichteninhalte");
        var now = DateTimeOffset.Now;
        var mapped = DiscordNotificationMapper.Map(1, ["Memo", "Hallo", "Zweite Zeile"], now);
        check(mapped.Sender == "Memo" && mapped.Message.Contains("Zweite Zeile") && mapped.CreatedAt == now, "Discord-Vorschau erhält Absender, Nachricht und Zeitstempel");
        check(DiscordNotificationMapper.Map(2, [], now).Message.Length > 0 && DiscordNotificationMapper.Map(3, ["Nachricht"], now).Sender == "Discord", "Verdeckte und einzeilige Benachrichtigungen werden sauber dargestellt");
        var store = new JsonStore(Path.Combine(directory, "discord"));
        store.Save("settings.json", new AppSettings { DiscordEnabled = true });
        var settings = new SettingsService(store, _ => { });
        using var source = new FakeSource { Items = [mapped, new(2, "Alex", "Neu", now.AddSeconds(1)), mapped] };
        using var service = new DiscordService(settings, source, () => true);
        await service.RequestAccessAsync();
        check(service.Running && service.NotificationCount == 2 && service.Notifications[0].Sender == "Alex", "Discord wird erkannt; Benachrichtigungen werden dedupliziert, gezählt und sortiert");
        source.Items = []; await service.RequestAccessAsync();
        check(service.NotificationCount == 0 && !service.HasNotifications, "Im Windows-Mitteilungscenter entfernte Meldungen verschwinden aus der Notch");
        source.Items = [mapped]; await service.RequestAccessAsync();
        source.Access = NotificationAccess.Denied; await service.RequestAccessAsync();
        check(service.Notifications.Count == 0 && service.NotificationCount == 0 && service.NeedsPermission, "Entzogene Freigabe löscht die Anzeige und erklärt den benötigten Zugriff");
        source.Access = NotificationAccess.Allowed;
        source.Pending = new TaskCompletionSource<IReadOnlyList<DiscordNotification>>();
        var pending = service.RequestAccessAsync();
        await source.ReadEntered.Task;
        settings.Current.DiscordEnabled = false; settings.Save(settings.Current);
        source.Pending.SetResult([mapped]); await pending;
        check(!service.Enabled && service.NotificationCount == 0 && service.Notifications.Count == 0 && source.StopCount > 0, "Ausschalten während einer laufenden Abfrage verwirft deren Ergebnis");
        check(Directory.GetFiles(Path.Combine(directory, "discord")).All(p => !File.ReadAllText(p).Contains("Hallo")), "Discord-Nachrichten werden nicht lokal gespeichert");
        using var windows = new WindowsDiscordNotificationSource();
        if (!WindowsDiscordNotificationSource.HasPackageIdentity)
            check(await windows.GetAccessAsync(false) == NotificationAccess.InstallationRequired, "Portable EXE meldet fehlende Paketidentität ohne Freigabe vorzutäuschen");
    }
    private sealed class FakeSource : IDiscordNotificationSource
    {
        public event EventHandler? Changed { add { } remove { } }
        public NotificationAccess Access { get; set; } = NotificationAccess.Allowed;
        public IReadOnlyList<DiscordNotification> Items { get; set; } = [];
        public TaskCompletionSource<IReadOnlyList<DiscordNotification>>? Pending { get; set; }
        public TaskCompletionSource<bool> ReadEntered { get; private set; } = new();
        public int StopCount { get; private set; }
        public Task<NotificationAccess> GetAccessAsync(bool requestPermission) => Task.FromResult(Access);
        public Task<IReadOnlyList<DiscordNotification>> ReadAsync()
        {
            if (Pending is null) return Task.FromResult(Items);
            ReadEntered.TrySetResult(true); return Pending.Task;
        }
        public void Stop() => StopCount++;
        public void Dispose() => Stop();
    }
    public static async Task VerifyCardAsync(string directory, Action<bool, string> check)
    {
        var store = new JsonStore(Path.Combine(directory, "discord-card"));
        store.Save("settings.json", new AppSettings { DiscordEnabled = true });
        using var source = new FakeSource { Items = [new(1, "Testabsender", "Sichtbare Testnachricht", DateTimeOffset.Now)] };
        using var service = new DiscordService(new SettingsService(store, _ => { }), source, () => true);
        await service.RequestAccessAsync();
        var card = new DiscordCard { DataContext = service, NotificationItems = service.Notifications, Width = 400 };
        card.Measure(new Size(400, 600)); card.Arrange(new Rect(0, 0, 400, card.DesiredSize.Height)); card.UpdateLayout();
        var text = Texts(card).ToList();
        check(text.Contains("Testabsender") && text.Contains("Sichtbare Testnachricht") && text.Contains("1 Benachrichtigung"), "Discord-Karte rendert Absender, Nachricht und Zähler mit echten WPF-Bindings");
    }
    private static IEnumerable<string> Texts(DependencyObject root)
    {
        if (root is TextBlock text) yield return text.Text;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++) foreach (var value in Texts(VisualTreeHelper.GetChild(root, i))) yield return value;
    }
}
