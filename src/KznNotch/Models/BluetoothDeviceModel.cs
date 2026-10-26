namespace KznNotch.Models;

public sealed class BluetoothDeviceModel : ObservableObject
{
    private string _name = "Bluetooth-Gerät";
    private bool _connected, _tracked;
    private int? _battery;
    private Integrations.AirPodsAdvertisement? _airPods;
    public bool IsNearbyAirPods { get; init; }
    public DateTimeOffset LastAdvertisement { get; private set; }
    public bool Available => Connected || (IsNearbyAirPods && _airPods is not null);
    public void UpdateAirPods(Integrations.AirPodsAdvertisement? value, DateTimeOffset timestamp)
    {
        _airPods = value; LastAdvertisement = timestamp;
        Raise(nameof(Battery)); Raise(nameof(BatteryLabel)); Raise(nameof(Detail)); Raise(nameof(Available));
    }
    public string Id { get; init; } = "";
    public string ContainerId { get; set; } = "";
    public bool LowEnergy { get; init; }
    public string Name { get => _name; set => Set(ref _name, value); }
    public bool Connected { get => _connected; set { Set(ref _connected, value); Raise(nameof(Detail)); Raise(nameof(BatteryLabel)); } }
    public bool Tracked { get => _tracked; set => Set(ref _tracked, value); }
    public int? Battery { get => _airPods is { } a ? a.Left is { } l && a.Right is { } r ? Math.Min(l, r) : a.Left ?? a.Right : _battery; set { Set(ref _battery, value); Raise(nameof(Detail)); Raise(nameof(BatteryLabel)); } }
    public string BatteryLabel => IsNearbyAirPods ? Battery is int p ? $"≈ {p}%" : "Nicht verfügbar" : !Connected ? "Getrennt" : Battery is int n ? $"{n}%" : "Nicht verfügbar";
    public string Detail => IsNearbyAirPods ? _airPods is { } a
        ? $"In der Nähe · L {Part(a.Left, a.LeftCharging)} · R {Part(a.Right, a.RightCharging)} · Case {Part(a.Case, a.CaseCharging)}"
        : "Kein aktuelles BLE-Signal · Case öffnen, um zu aktualisieren"
        : !Connected ? "Nicht verbunden" : Battery is int ? "Verbunden" : "Windows meldet keinen Akkustand";
    private static string Part(int? value, bool charging) => value is int n ? $"≈{n}%{(charging ? " ⚡" : "")}" : "—";
}
