using Microsoft.Win32;
using KznNotch.Utilities;

namespace KznNotch.Services;

public sealed class BatteryService : ObservableObject, IDisposable
{
    private bool _hasBattery, _charging;
    private int? _percent;
    public bool HasBattery { get => _hasBattery; private set => Set(ref _hasBattery, value); }
    public int? Percent { get => _percent; private set { if (Set(ref _percent, value)) Raise(nameof(Label)); } }
    public bool Charging { get => _charging; private set { if (Set(ref _charging, value)) Raise(nameof(Label)); } }
    public string Label => Percent is int p ? $"{p}%{(Charging ? "  ↯" : "")}" : "—";
    public BatteryService() { SystemEvents.PowerModeChanged += PowerChanged; Refresh(); }
    private void PowerChanged(object sender, PowerModeChangedEventArgs e) => Ui.Run(Refresh);
    private void Refresh()
    {
        if (!NativeMethods.GetSystemPowerStatus(out var status)) { HasBattery = false; return; }
        HasBattery = status.BatteryFlag != 255 && (status.BatteryFlag & 128) == 0;
        Percent = HasBattery && status.BatteryLifePercent <= 100 ? status.BatteryLifePercent : null;
        Charging = HasBattery && (status.BatteryFlag & 8) != 0;
    }
    public void Dispose() => SystemEvents.PowerModeChanged -= PowerChanged;
}
