using Microsoft.Win32;
using KznNotch.Storage;

namespace KznNotch.Services;

public sealed class SettingsService(JsonStore store, Action<bool>? configureStartup = null) : ObservableObject
{
    private string _startupStatus = "";
    public string StartupStatus { get => _startupStatus; private set => Set(ref _startupStatus, value); }
    public void RepairStartup()
    {
        try { (configureStartup ?? ConfigureStartup)(Current.StartWithWindows); Log.Write("Autostart: " + StartupStatus); }
        catch (Exception ex) { Log.Error("Startup registration", ex); StartupStatus = "Autostart konnte nicht eingerichtet werden: " + ex.Message; }
    }
    public AppSettings Current { get; private set; } = Load(store);
    public event EventHandler? Changed;
    public void RememberWidget(string id)
    {
        if (Current.LastSelectedWidgetId == id) return;
        Current.LastSelectedWidgetId = id;
        store.Save("settings.json", Current);
    }
    private static AppSettings Load(JsonStore store)
    {
        var result = store.Load("settings.json", () => new AppSettings()); result.Normalize();
        // Nearby advertisements have no stable authenticated identity across process lifetimes.
        result.TrackedBluetoothIds.RemoveAll(x => x.StartsWith("airpods-session:", StringComparison.Ordinal));
        if (result.CompactDeviceId.StartsWith("airpods-session:", StringComparison.Ordinal)) result.CompactDeviceId = "";
        return result;
    }
    public void Save(AppSettings settings)
    {
        settings.Normalize();
        (configureStartup ?? ConfigureStartup)(settings.StartWithWindows);
        store.Save("settings.json", settings);
        Current = settings;
        Changed?.Invoke(this, EventArgs.Empty);
    }
    private void ConfigureStartup(bool enabled)
    {
        bool packaged;
        try { _ = Windows.ApplicationModel.Package.Current.Id; packaged = true; }
        catch (InvalidOperationException) { packaged = false; }
        if (packaged)
        {
            var task = Windows.ApplicationModel.StartupTask.GetAsync("KznNotchStartup").AsTask().GetAwaiter().GetResult();
            if (!enabled) task.Disable();
            else if (task.State == Windows.ApplicationModel.StartupTaskState.Disabled)
                task.RequestEnableAsync().AsTask().GetAwaiter().GetResult();
            StartupStatus = task.State switch
            {
                Windows.ApplicationModel.StartupTaskState.Enabled => "Autostart ist in Windows aktiviert.",
                Windows.ApplicationModel.StartupTaskState.DisabledByUser => "In Windows deaktiviert. Bitte unter Einstellungen → Apps → Autostart Kzn Notch einschalten.",
                Windows.ApplicationModel.StartupTaskState.DisabledByPolicy => "Autostart wurde durch eine Windows-Richtlinie deaktiviert.",
                _ => "Autostart ist deaktiviert."
            };
            using var old = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            old?.DeleteValue("Notchify", false); old?.DeleteValue("KznNotch", false);
            return;
        }
        using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
        key.DeleteValue("Notchify", false); // Retire the old autorun name after the product rename.
        if (enabled)
        {
            var path = Environment.ProcessPath ?? throw new InvalidOperationException("Programmpfad nicht verfügbar.");
            key.SetValue("KznNotch", $"\"{path}\"");
        }
        else key.DeleteValue("KznNotch", false);
        StartupStatus = enabled ? "Autostart für die portable Version eingerichtet." : "Autostart ist deaktiviert.";
    }
}
