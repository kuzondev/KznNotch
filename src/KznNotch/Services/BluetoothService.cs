using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;
using Windows.Devices.Bluetooth.Advertisement;
using KznNotch.Integrations;

namespace KznNotch.Services;

public sealed class BluetoothService : ObservableObject, IDisposable
{
    private const string ConnectionKey = "System.Devices.Aep.IsConnected";
    private const string ContainerKey = "System.Devices.Aep.ContainerId";
    // Driver-populated Windows property; not a guaranteed contract on every Bluetooth profile.
    private const string BatteryKey = "{104EA319-6EE2-4701-BD47-8DDBF425BBE5} 2";
    private readonly SettingsService _settings;
    private readonly EventService _events;
    private readonly List<DeviceWatcher> _watchers = [];
    private readonly DispatcherTimer _batteryTimer = new() { Interval = TimeSpan.FromSeconds(60) };
    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private BluetoothLEAdvertisementWatcher? _airPodsWatcher;
    private readonly Dictionary<ulong, (string Id, DateTimeOffset Last)> _nearby = [];
    private readonly DispatcherTimer _freshnessTimer = new() { Interval = TimeSpan.FromSeconds(15) };
    private bool _disposed, _enumerating = true;
    private bool _bluetoothEnabled, _airPodsEnabled;
    private int _enumerations;
    private string _status = "Geräte werden gesucht…";
    private string _airPodsStatus = "AirPods-Empfang deaktiviert";
    public string AirPodsStatus { get => _airPodsStatus; private set => Set(ref _airPodsStatus, value); }
    public ObservableCollection<BluetoothDeviceModel> Devices { get; } = [];
    public ObservableCollection<BluetoothDeviceModel> TrackedDevices { get; } = [];
    public string Status { get => _status; private set => Set(ref _status, value); }
    public BluetoothDeviceModel? CompactDevice => Devices.FirstOrDefault(x => x.Id == _settings.Current.CompactDeviceId && x.Tracked && x.Available && x.Battery.HasValue);
    public ICommand RefreshCommand { get; }
    public BluetoothService(SettingsService settings, EventService events)
    {
        _settings = settings; _events = events;
        RefreshCommand = new AsyncCommand(RefreshBatteriesAsync);
        _batteryTimer.Tick += async (_, _) => await RefreshBatteriesAsync();
        _freshnessTimer.Tick += (_, _) => ExpireAirPods();
        settings.Changed += SettingsChanged;
    }
    public void Initialize() => RestartWatchers();
    private void RestartWatchers()
    {
        StopWatchers();
        _bluetoothEnabled = _settings.Current.BluetoothEnabled;
        _airPodsEnabled = _settings.Current.AirPodsEnabled;
        foreach (var device in Devices) device.PropertyChanged -= DeviceChanged;
        Devices.Clear(); TrackedDevices.Clear();
        if (!_settings.Current.BluetoothEnabled) { Status = "Bluetooth-Integration deaktiviert"; return; }
        _enumerating = true; _enumerations = 0;
        try
        {
            foreach (var selector in new[] { BluetoothDevice.GetDeviceSelectorFromPairingState(true), BluetoothLEDevice.GetDeviceSelectorFromPairingState(true) })
            {
                var watcher = DeviceInformation.CreateWatcher(selector, new[] { ConnectionKey, ContainerKey, BatteryKey }, DeviceInformationKind.AssociationEndpoint);
                watcher.Added += Added; watcher.Updated += Updated; watcher.Removed += Removed; watcher.EnumerationCompleted += EnumerationCompleted;
                _watchers.Add(watcher); watcher.Start();
            }
            Status = "Gekoppelte Geräte werden gesucht…"; _batteryTimer.Start();
            StartAirPods();
        }
        catch (Exception ex) { Log.Error("Bluetooth enumeration", ex); Status = "Bluetooth-Zugriff nicht verfügbar"; }
    }
    private void StartAirPods()
    {
        if (!_settings.Current.AirPodsEnabled) return;
        try
        {
            _airPodsWatcher = new() { ScanningMode = BluetoothLEScanningMode.Passive };
            _airPodsWatcher.Received += AirPodsReceived;
            _airPodsWatcher.Stopped += AirPodsStopped;
            _airPodsWatcher.Start(); _freshnessTimer.Start();
            AirPodsStatus = "Empfang aktiv · Case in der Nähe des PCs öffnen. BLE-Auswahl gilt nur für die aktuelle Funkadresse.";
        }
        catch (Exception ex) { Log.Error("AirPods BLE receiver", ex); AirPodsStatus = "AirPods-Empfang nicht verfügbar. Bluetooth-Adapter prüfen."; }
    }
    private void AirPodsStopped(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementWatcherStoppedEventArgs args) => Ui.Run(() =>
    {
        if (sender == _airPodsWatcher && args.Error != BluetoothError.Success) AirPodsStatus = "AirPods-Empfang gestoppt. Bluetooth aktivieren und Integration erneut einschalten.";
    });
    private void AirPodsReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
    {
        if (_disposed || args.RawSignalStrengthInDBm < -70) return;
        foreach (var data in args.Advertisement.ManufacturerData)
        {
            if (data.CompanyId != 0x004C || data.Data.Length > 64) continue;
            using var reader = DataReader.FromBuffer(data.Data);
            var bytes = new byte[data.Data.Length]; reader.ReadBytes(bytes);
            var decoded = AirPodsAdvertisement.Decode(data.CompanyId, bytes);
            if (decoded is null) continue;
            Ui.Run(() => ApplyAirPods(sender, args.BluetoothAddress, decoded));
        }
    }
    private void ApplyAirPods(BluetoothLEAdvertisementWatcher sender, ulong address, AirPodsAdvertisement reading)
    {
        if (_disposed || sender != _airPodsWatcher) return;
        var now = DateTimeOffset.UtcNow;
        if (_nearby.TryGetValue(address, out var previous) && now - previous.Last < TimeSpan.FromSeconds(2)) return;
        if (!_nearby.ContainsKey(address) && _nearby.Count >= 32) return;
        // A random BLE address proves neither pairing nor ownership. Never match by name/RSSI.
        var id = previous.Id ?? "airpods-session:" + Guid.NewGuid().ToString("N");
        _nearby[address] = (id, now);
        var device = Devices.FirstOrDefault(x => x.Id == id);
        if (device is null)
        {
            var model = reading.Model is 0x200E or 0x2014 or 0x2024 ? "AirPods Pro" : "AirPods";
            device = new() { Id = id, Name = $"{model} · BLE {_nearby.Count}", IsNearbyAirPods = true,
                Tracked = _settings.Current.TrackedBluetoothIds.Contains(id) };
            device.PropertyChanged += DeviceChanged; Devices.Add(device); SyncTracked();
        }
        device.UpdateAirPods(reading, now);
    }
    private void ExpireAirPods()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var device in Devices.Where(x => x.IsNearbyAirPods && now - x.LastAdvertisement > TimeSpan.FromSeconds(90)).ToList())
        {
            device.UpdateAirPods(null, device.LastAdvertisement);
            if (!device.Tracked) { device.PropertyChanged -= DeviceChanged; Devices.Remove(device); }
        }
        foreach (var address in _nearby.Where(x => now - x.Value.Last > TimeSpan.FromMinutes(10)).Select(x => x.Key).ToList()) _nearby.Remove(address);
    }
    private void Added(DeviceWatcher sender, DeviceInformation info) => Ui.Run(() =>
    {
        if (_disposed || !_watchers.Contains(sender) || Devices.Any(x => x.Id == info.Id)) return;
        var device = new BluetoothDeviceModel { Id = info.Id, Name = string.IsNullOrWhiteSpace(info.Name) ? "Bluetooth-Gerät" : info.Name, LowEnergy = _watchers.IndexOf(sender) == 1, Tracked = _settings.Current.TrackedBluetoothIds.Contains(info.Id) };
        Apply(device, info.Properties); device.PropertyChanged += DeviceChanged; Devices.Add(device); SyncTracked();
    });
    private void Updated(DeviceWatcher sender, DeviceInformationUpdate info) => Ui.Run(() =>
    {
        if (_disposed || !_watchers.Contains(sender)) return;
        var device = Devices.FirstOrDefault(x => x.Id == info.Id); if (device is null) return;
        var wasConnected = device.Connected; Apply(device, info.Properties);
        if (!_enumerating && device.Tracked && wasConnected != device.Connected)
        {
            _events.Publish(new(device.Name, device.Connected ? $"Verbunden{(device.Battery is { } b ? $" · {b}%" : "")}" : "Verbindung getrennt", "\uE702", EventPriority.Bluetooth, TimeSpan.FromSeconds(4), device.Id));
            if (device.Connected) _ = RefreshBatteriesAsync();
        }
    });
    private static void Apply(BluetoothDeviceModel device, IReadOnlyDictionary<string, object> properties)
    {
        if (properties.TryGetValue(ConnectionKey, out var connected) && connected is bool b) device.Connected = b;
        if (properties.TryGetValue(ContainerKey, out var container) && container is not null) device.ContainerId = container.ToString() ?? "";
        if (properties.TryGetValue("System.ItemNameDisplay", out var name) && name is string text && !string.IsNullOrWhiteSpace(text)) device.Name = text;
        if (properties.TryGetValue(BatteryKey, out var battery) && TryBattery(battery) is { } level) device.Battery = level;
        if (!device.Connected) device.Battery = null;
    }
    private void Removed(DeviceWatcher sender, DeviceInformationUpdate info) => Ui.Run(() =>
    {
        if (!_watchers.Contains(sender)) return;
        var device = Devices.FirstOrDefault(x => x.Id == info.Id); if (device is null) return;
        device.PropertyChanged -= DeviceChanged; Devices.Remove(device); SyncTracked();
    });
    private void EnumerationCompleted(DeviceWatcher sender, object args) => Ui.Run(async () =>
    {
        if (!_watchers.Contains(sender) || ++_enumerations < _watchers.Count) return;
        _enumerating = false; Status = Devices.Count == 0 ? "Keine gekoppelten Bluetooth-Geräte gefunden" : "Wähle die Geräte aus, die du überwachen möchtest.";
        await RefreshBatteriesAsync();
    });
    private void DeviceChanged(object? sender, PropertyChangedEventArgs e) { if (e.PropertyName is nameof(BluetoothDeviceModel.Battery) or nameof(BluetoothDeviceModel.Connected) or nameof(BluetoothDeviceModel.Tracked)) Raise(nameof(CompactDevice)); }
    private void SyncTracked()
    {
        TrackedDevices.Clear(); foreach (var device in Devices.Where(x => x.Tracked)) TrackedDevices.Add(device); Raise(nameof(CompactDevice));
    }
    public async Task RefreshBatteriesAsync()
    {
        if (_disposed || !_settings.Current.BluetoothEnabled || !await _refreshGate.WaitAsync(0)) return;
        try
        {
            var selected = Devices.Where(x => x.Tracked && x.Connected).ToList();
            if (selected.Count == 0) return;
            // This mirrors the battery Windows exposes for classic/HFP/HID devices, grouped by container.
            var pnp = await DeviceInformation.FindAllAsync(null, new[] { BatteryKey, "System.Devices.ContainerId" }, DeviceInformationKind.Device);
            foreach (var device in selected)
            {
                if (_disposed) return;
                var candidates = pnp.Where(x => x.Properties.TryGetValue("System.Devices.ContainerId", out var container) && !string.IsNullOrEmpty(device.ContainerId) && string.Equals(container?.ToString(), device.ContainerId, StringComparison.OrdinalIgnoreCase));
                int? level = candidates.Select(x => x.Properties.TryGetValue(BatteryKey, out var value) ? TryBattery(value) : null).FirstOrDefault(x => x.HasValue);
                if (!level.HasValue && device.LowEnergy) level = await ReadGattAsync(device.Id);
                if (!_disposed && device.Connected) device.Battery = level;
            }
            Raise(nameof(CompactDevice));
        }
        catch (Exception ex) { Log.Error("Bluetooth batteries", ex); Status = "Einige Geräte lassen sich momentan nicht abfragen."; }
        finally { _refreshGate.Release(); }
    }
    private static int? TryBattery(object? value)
    {
        try { if (value is null) return null; var n = Convert.ToInt32(value, CultureInfo.InvariantCulture); return n is >= 0 and <= 100 ? n : null; }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException) { return null; }
    }
    private static async Task<int?> ReadGattAsync(string id)
    {
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var device = await BluetoothLEDevice.FromIdAsync(id).AsTask(timeout.Token);
            if (device is null) return null;
            var services = await device.GetGattServicesForUuidAsync(GattServiceUuids.Battery, BluetoothCacheMode.Cached).AsTask(timeout.Token);
            if (services.Status != GattCommunicationStatus.Success) return null;
            try
            {
                foreach (var service in services.Services)
                {
                    var chars = await service.GetCharacteristicsForUuidAsync(GattCharacteristicUuids.BatteryLevel).AsTask(timeout.Token);
                    if (chars.Status != GattCommunicationStatus.Success) continue;
                    foreach (var characteristic in chars.Characteristics)
                    {
                        var read = await characteristic.ReadValueAsync(BluetoothCacheMode.Uncached).AsTask(timeout.Token);
                        if (read.Status != GattCommunicationStatus.Success || read.Value.Length == 0) continue;
                        using var reader = DataReader.FromBuffer(read.Value); var value = reader.ReadByte(); if (value <= 100) return value;
                    }
                }
            }
            finally { foreach (var service in services.Services) service.Dispose(); }
        }
        catch (Exception ex) { Log.Error("Bluetooth GATT battery", ex); }
        return null;
    }
    private void SettingsChanged(object? sender, EventArgs e)
    {
        if (_bluetoothEnabled != _settings.Current.BluetoothEnabled || _airPodsEnabled != _settings.Current.AirPodsEnabled)
        {
            RestartWatchers(); return;
        }
        foreach (var device in Devices) device.Tracked = _settings.Current.TrackedBluetoothIds.Contains(device.Id);
        SyncTracked(); _ = RefreshBatteriesAsync();
    }
    private void StopWatchers()
    {
        _batteryTimer.Stop();
        _freshnessTimer.Stop();
        AirPodsStatus = "AirPods-Empfang deaktiviert";
        if (_airPodsWatcher is { } airPods)
        {
            _airPodsWatcher = null; airPods.Received -= AirPodsReceived; airPods.Stopped -= AirPodsStopped;
            if (airPods.Status == BluetoothLEAdvertisementWatcherStatus.Started) airPods.Stop();
        }
        foreach (var watcher in _watchers)
        {
            watcher.Added -= Added; watcher.Updated -= Updated; watcher.Removed -= Removed; watcher.EnumerationCompleted -= EnumerationCompleted;
            if (watcher.Status is DeviceWatcherStatus.Started or DeviceWatcherStatus.EnumerationCompleted) watcher.Stop();
        }
        _watchers.Clear();
    }
    public void Dispose() { _disposed = true; _settings.Changed -= SettingsChanged; StopWatchers(); foreach (var device in Devices) device.PropertyChanged -= DeviceChanged; }
}
