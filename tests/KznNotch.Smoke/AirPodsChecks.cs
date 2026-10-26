using KznNotch.Integrations;
using KznNotch.Models;
using KznNotch.Services;
using KznNotch.Storage;
using System.IO;

namespace KznNotch.Smoke;

internal static class AirPodsChecks
{
    public static void Run(Action<bool, string> check)
    {
        // Synthetic protocol fixture, not captured device data.
        byte[] packet = new byte[27];
        packet[0] = 7; packet[1] = 25; packet[3] = 0x14; packet[4] = 0x20;
        packet[5] = 0x20; packet[6] = 0x36; packet[7] = 0x58;
        var reading = AirPodsAdvertisement.Decode(0x4C, packet)!;
        check(reading.Left == 60 && reading.Right == 30 && reading.Case == 80 && reading.LeftCharging && !reading.RightCharging && reading.CaseCharging,
            "AirPods BLE: getrennte Akkus und Ladezustände werden dekodiert");
        packet[5] = 0;
        var reversed = AirPodsAdvertisement.Decode(0x4C, packet)!;
        check(reversed.Left == 30 && reversed.Right == 60 && !reversed.LeftCharging && reversed.RightCharging,
            "AirPods BLE: sendende Seite wird korrekt links/rechts zugeordnet");
        packet[6] = 0xFF; packet[7] = 0x0F;
        var missing = AirPodsAdvertisement.Decode(0x4C, packet)!;
        check(missing.Left is null && missing.Right is null && missing.Case is null,
            "AirPods BLE: fehlende Werte werden nicht als voller oder leerer Akku ausgegeben");
        check(AirPodsAdvertisement.Decode(0x4C, packet.AsSpan(0, 9)) is null && AirPodsAdvertisement.Decode(1, packet) is null,
            "AirPods BLE: fremde Hersteller und abgeschnittene Pakete werden verworfen");
        packet[3] = 0x0A;
        check(AirPodsAdvertisement.Decode(0x4C, packet) is null, "AirPods Max werden nicht mit dem Earbud-Format interpretiert");
        var device = new BluetoothDeviceModel { IsNearbyAirPods = true };
        device.UpdateAirPods(reading, DateTimeOffset.UtcNow);
        check(device.Available && !device.Connected && device.Battery == 30 && device.Detail.Contains("Case"),
            "AirPods BLE: Nähe wird nicht als Windows-Verbindung ausgegeben; kompakter Wert nutzt den niedrigeren Ohrhörer");
        device.UpdateAirPods(null, DateTimeOffset.UtcNow);
        check(!device.Available && device.Battery is null, "AirPods BLE: veraltete Daten verschwinden aus der kompakten Anzeige");
        var random = new Random(27);
        for (int size = 0; size < 128; size++) { var bytes = new byte[size]; random.NextBytes(bytes); AirPodsAdvertisement.Decode(0x4C, bytes); }
        check(true, "AirPods BLE: variable und ungültige Paketlängen bleiben sicher");
        var directory = Path.Combine(Path.GetTempPath(), "KznNotch-AirPods-" + Guid.NewGuid().ToString("N"));
        var store = new JsonStore(directory);
        store.Save("settings.json", new AppSettings { BluetoothEnabled = false });
        var settings = new SettingsService(store, _ => { });
        using var events = new EventService();
        using var bluetooth = new BluetoothService(settings, events);
        var selected = new BluetoothDeviceModel { Id = "airpods-session:test", IsNearbyAirPods = true };
        selected.UpdateAirPods(reading, DateTimeOffset.UtcNow); bluetooth.Devices.Add(selected);
        settings.Current.TrackedBluetoothIds.Add(selected.Id); settings.Current.CompactDeviceId = selected.Id;
        settings.Save(settings.Current);
        check(bluetooth.TrackedDevices.Contains(selected) && bluetooth.CompactDevice == selected,
            "AirPods BLE: Auswahl speichern erhält den empfangenen Akkustand ohne Scanner-Neustart");
        var reloaded = new SettingsService(store, _ => { });
        check(reloaded.Current.TrackedBluetoothIds.Count == 0 && reloaded.Current.CompactDeviceId == "",
            "AirPods BLE: temporäre Funkidentität wird nach Neustart nicht als dauerhaftes Gerät behandelt");
    }
}
