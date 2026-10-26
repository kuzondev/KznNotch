using Windows.UI.Notifications;
using Windows.UI.Notifications.Management;

namespace KznNotch.Integrations;

public sealed class WindowsDiscordNotificationSource : IDiscordNotificationSource
{
    private UserNotificationListener? _listener;
    private bool _subscribed;
    private bool _callbacksUnavailable;
    public event EventHandler? Changed;
    public static bool HasPackageIdentity
    {
        get { uint length = 0; return GetCurrentPackageFullName(ref length, IntPtr.Zero) == 122; }
    }
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern int GetCurrentPackageFullName(ref uint length, IntPtr name);
    public async Task<NotificationAccess> GetAccessAsync(bool requestPermission)
    {
        // A portable exe cannot grant itself the userNotificationListener capability.
        if (!HasPackageIdentity) return NotificationAccess.InstallationRequired;
        try
        {
            _listener ??= UserNotificationListener.Current;
            var status = _listener.GetAccessStatus();
            if (requestPermission && status == UserNotificationListenerAccessStatus.Unspecified) status = await _listener.RequestAccessAsync();
            if (status != UserNotificationListenerAccessStatus.Allowed) { Stop(); return status == UserNotificationListenerAccessStatus.Denied ? NotificationAccess.Denied : NotificationAccess.NotRequested; }
            if (!_subscribed && !_callbacksUnavailable)
            {
                try { _listener.NotificationChanged += NotificationChanged; _subscribed = true; }
                catch (Exception ex) when (ex is COMException or NotImplementedException) { _callbacksUnavailable = true; Log.Write("Notification listener foreground callbacks unavailable; using reconciliation."); }
            }
            return NotificationAccess.Allowed;
        }
        catch (Exception ex) when (ex is COMException or UnauthorizedAccessException or InvalidOperationException)
        { Log.Error("Notification listener access", ex); return NotificationAccess.Unavailable; }
    }
    public async Task<IReadOnlyList<DiscordNotification>> ReadAsync()
    {
        if (_listener is null || _listener.GetAccessStatus() != UserNotificationListenerAccessStatus.Allowed) return [];
        var notifications = await _listener.GetNotificationsAsync(NotificationKinds.Toast);
        var result = new List<DiscordNotification>();
        foreach (var notification in notifications)
        {
            try
            {
                // Filter before reading text. No other apps' messages are retained.
                if (!DiscordNotificationMapper.IsDiscord(notification.AppInfo.AppUserModelId, notification.AppInfo.DisplayInfo.DisplayName)) continue;
                var binding = notification.Notification.Visual.GetBinding(KnownNotificationBindings.ToastGeneric);
                var text = binding?.GetTextElements().Select(x => x.Text) ?? Enumerable.Empty<string>();
                result.Add(DiscordNotificationMapper.Map(notification.Id, text, notification.CreationTime));
            }
            catch (Exception ex) when (ex is COMException or ArgumentException) { Log.Write("A Discord notification was removed during enumeration."); }
        }
        return result.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).ToList();
    }
    private void NotificationChanged(UserNotificationListener sender, UserNotificationChangedEventArgs args) => Changed?.Invoke(this, EventArgs.Empty);
    public void Stop() { if (_subscribed && _listener is not null) { _listener.NotificationChanged -= NotificationChanged; _subscribed = false; } }
    public void Dispose() => Stop();
}
