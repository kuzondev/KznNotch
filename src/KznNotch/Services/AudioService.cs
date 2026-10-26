using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace KznNotch.Services;

public sealed class AudioService : IDisposable, IMMNotificationClient
{
    private readonly EventService _events;
    private readonly MMDeviceEnumerator _enumerator = new();
    private MMDevice? _output, _input;
    private bool _disposed;
    private int _lastVolume = -1;
    private bool? _lastMute;
    public AudioService(EventService events)
    {
        _events = events;
        try { _enumerator.RegisterEndpointNotificationCallback(this); Attach(); }
        catch (Exception ex) { Log.Error("Core Audio", ex); }
    }
    private void Attach()
    {
        if (_disposed) return;
        Detach(); _lastVolume = -1; _lastMute = null;
        try
        {
            _output = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            _output.AudioEndpointVolume.OnVolumeNotification += OutputChanged;
            _lastVolume = (int)Math.Round(_output.AudioEndpointVolume.MasterVolumeLevelScalar * 100);
        }
        catch (COMException ex) { Log.Error("Audio output", ex); }
        try
        {
            _input = _enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
            _input.AudioEndpointVolume.OnVolumeNotification += InputChanged;
            _lastMute = _input.AudioEndpointVolume.Mute;
        }
        catch (COMException ex) { Log.Error("Audio input", ex); }
    }
    private void OutputChanged(AudioVolumeNotificationData data) => Ui.Run(() =>
    {
        if (_disposed) return;
        var volume = data.Muted ? 0 : (int)Math.Round(data.MasterVolume * 100);
        if (volume == _lastVolume) return;
        _lastVolume = volume;
        _events.Publish(new("Lautstärke", data.Muted ? "Stummgeschaltet" : $"{volume}%", data.Muted ? "\uE74F" : "\uE767", EventPriority.Volume, TimeSpan.FromSeconds(2), "volume"));
    });
    private void InputChanged(AudioVolumeNotificationData data) => Ui.Run(() =>
    {
        if (_disposed || data.Muted == _lastMute) return;
        _lastMute = data.Muted;
        _events.Publish(new(data.Muted ? "Mikrofon stumm" : "Mikrofon aktiv", "Windows-Standardmikrofon", "\uE720", EventPriority.Call, TimeSpan.FromSeconds(3), "microphone"));
    });
    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string deviceId) { if (role is Role.Multimedia or Role.Communications) Ui.Run(Attach); }
    public void OnDeviceStateChanged(string deviceId, DeviceState state) { }
    public void OnDeviceAdded(string deviceId) { }
    public void OnDeviceRemoved(string deviceId) { }
    public void OnPropertyValueChanged(string deviceId, PropertyKey key) { }
    private void Detach()
    {
        if (_output is not null) { _output.AudioEndpointVolume.OnVolumeNotification -= OutputChanged; _output.Dispose(); _output = null; }
        if (_input is not null) { _input.AudioEndpointVolume.OnVolumeNotification -= InputChanged; _input.Dispose(); _input = null; }
    }
    public void Dispose()
    {
        _disposed = true;
        try { _enumerator.UnregisterEndpointNotificationCallback(this); } catch (COMException) { }
        Detach(); _enumerator.Dispose();
    }
}
