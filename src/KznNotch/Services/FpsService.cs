using System.IO.Pipes;
using System.Text;

namespace KznNotch.Services;

public sealed class FpsService : ObservableObject, IDisposable
{
    private readonly GameModeService _gaming;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(500) };
    private readonly object _gate = new();
    private readonly Queue<(DateTime At, double Ms, string Chain)> _samples = new();
    private NamedPipeServerStream? _pipe;
    private StreamWriter? _writer;
    private FpsCsvParser _parser = new();
    private int _sentPid = -1, _targetPid;
    private bool _starting, _autoTried, _disposed;
    private string _status = "Im Spielmodus FPS auswählen. Messung benötigt ggf. eine Windows-Freigabe.";
    public string Status { get => _status; private set => Set(ref _status, value); }
    public double? FramesPerSecond { get; private set; }
    public double? FrameMilliseconds { get; private set; }
    public string OverlayText => FramesPerSecond is { } fps ? $"{fps:0} FPS" : "— FPS";
    public string FrameLabel => FrameMilliseconds is { } ms ? $"{ms:0.00} ms" : "— ms";
    public string GameLabel => _gaming.GameName.Length > 0 ? _gaming.GameName : "Kein aktives Spiel";
    public ICommand StartCommand { get; }
    public FpsService(GameModeService gaming)
    {
        _gaming = gaming;
        StartCommand = new AsyncCommand(() => StartAsync(), () => !_starting);
        _timer.Tick += (_, _) => Tick();
        _gaming.ModeChanged += GamingChanged;
        _gaming.PropertyChanged += GamingPropertyChanged;
    }
    private void GamingChanged(object? sender, EventArgs e)
    {
        Tick();
        if (_gaming.EffectiveMode == GamingMode.Fps) _timer.Start(); else _timer.Stop();
    }
    private void GamingPropertyChanged(object? sender, PropertyChangedEventArgs e)
    { if (e.PropertyName == nameof(GameModeService.GameProcessId)) GamingChanged(sender, EventArgs.Empty); }
    private void Tick()
    {
        var pid = _gaming.EffectiveMode == GamingMode.Fps ? _gaming.GameProcessId : 0;
        if (pid > 0 && !_autoTried) { _autoTried = true; _ = StartAsync(); }
        if (_writer is not null && _sentPid != pid)
        {
            _sentPid = _targetPid = pid; _parser = new();
            lock (_gate) _samples.Clear();
            try { _writer.WriteLine(pid.ToString(CultureInfo.InvariantCulture)); Status = pid > 0 ? "Warte auf Bildausgaben des aktiven Spiels …" : "FPS bereit · wartet auf ein aktives Spiel im FPS-Modus."; }
            catch (IOException) { Disconnect(); }
        }
        lock (_gate)
        {
            while (_samples.TryPeek(out var frame) && DateTime.UtcNow - frame.At > TimeSpan.FromSeconds(2)) _samples.Dequeue();
            var chain = _samples.GroupBy(x => x.Chain).OrderByDescending(x => x.Count()).FirstOrDefault();
            FrameMilliseconds = pid > 0 && chain is not null ? chain.Average(x => x.Ms) : null;
            FramesPerSecond = FrameMilliseconds is > 0 ? 1000 / FrameMilliseconds : null;
        }
        if (FramesPerSecond.HasValue) Status = "Live · Bildausgaben des aktiven Spiels, über 2 Sekunden geglättet.";
        else if (Status.StartsWith("Live", StringComparison.Ordinal)) Status = "Keine aktuellen Frames · Spiel pausiert, minimiert oder nicht messbar.";
        Raise(nameof(FramesPerSecond)); Raise(nameof(OverlayText)); Raise(nameof(FrameLabel)); Raise(nameof(GameLabel));
    }
    private async Task StartAsync()
    {
        if (_starting || _disposed) return;
        _starting = true; Disconnect();
        var name = "KznNotch-Fps-" + Guid.NewGuid().ToString("N");
        var pipe = new NamedPipeServerStream(name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        _pipe = pipe;
        try
        {
            var info = new ProcessStartInfo(Path.Combine(AppContext.BaseDirectory, "KznNotch.exe")) { UseShellExecute = true, WindowStyle = ProcessWindowStyle.Hidden, Arguments = "--fps-helper " + name };

            using var helper = Process.Start(info);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await pipe.WaitForConnectionAsync(timeout.Token);
            if (_disposed) { pipe.Dispose(); return; }
            _writer = new StreamWriter(pipe, new UTF8Encoding(false), 4096, true) { AutoFlush = true };
            _sentPid = -1; Status = "FPS-Messung bereit."; Tick();
            _ = Task.Run(() => ReadAsync(pipe));
        }
        catch (Exception ex) when (ex is Win32Exception or IOException or OperationCanceledException)
        { Disconnect(); Status = "FPS-Helfer nicht erreichbar. Windows-Messberechtigung prüfen."; }
        finally { _starting = false; CommandManager.InvalidateRequerySuggested(); }
    }
    private async Task ReadAsync(NamedPipeServerStream pipe)
    {
        try
        {
            using var reader = new StreamReader(pipe, Encoding.UTF8, false, 4096, true);
            while (await reader.ReadLineAsync() is { } line)
            {
                if (line.StartsWith("ERROR:", StringComparison.Ordinal)) { Status = line[6..]; continue; }
                if (_parser.TryRead(line, _targetPid, out var ms, out var chain)) lock (_gate)
                { _samples.Enqueue((DateTime.UtcNow, ms, chain)); while (_samples.Count > 10000) _samples.Dequeue(); }
            }
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException) { }
    }
    private void Disconnect()
    {
        try { _writer?.Dispose(); } catch (Exception ex) when (ex is IOException or ObjectDisposedException) { }
        _writer = null; _pipe?.Dispose(); _pipe = null;
        lock (_gate) _samples.Clear();
    }
    public void Dispose() { _disposed = true; _timer.Stop(); _gaming.ModeChanged -= GamingChanged; _gaming.PropertyChanged -= GamingPropertyChanged; Disconnect(); }
}
