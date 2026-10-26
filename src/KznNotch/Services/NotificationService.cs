using CommunityToolkit.WinUI.Notifications;

namespace KznNotch.Services;

public sealed class NotificationService(SettingsService settings)
{
    public event Action<string, string>? FallbackRequested;
    public void Show(string title, string body)
    {
        if (!settings.Current.Notifications) return;
        try { new ToastContentBuilder().AddText(title).AddText(body).Show(); }
        catch (Exception ex) { Log.Error("Windows notification", ex); FallbackRequested?.Invoke(title, body); }
    }
}
