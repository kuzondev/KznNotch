using KznNotch.Utilities;
using System.Net.NetworkInformation;

namespace KznNotch.Services;

public sealed class SystemStatsService : ObservableObject, IDisposable
{
    private readonly DispatcherTimer _timer;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private long _idle, _kernel, _user, _received, _sent;
    private double _sampleTime;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private double _cpu, _ram, _ramTotal, _download, _upload;
    private double? _gpu;
    private IntPtr _gpuQuery, _gpuCounter;
    private bool _disposed;
    private readonly object _gpuLock = new();
    public double Cpu { get => _cpu; private set { Set(ref _cpu, value); Raise(nameof(CpuLabel)); } }
    public string CpuLabel => $"{Cpu:0}%";
    public double RamUsed { get => _ram; private set { Set(ref _ram, value); Raise(nameof(RamLabel)); Raise(nameof(RamPercent)); } }
    public double RamTotal { get => _ramTotal; private set { Set(ref _ramTotal, value); Raise(nameof(RamLabel)); Raise(nameof(RamPercent)); } }
    public string RamLabel => $"{RamUsed:0.0} / {RamTotal:0.#} GB";
    public double RamPercent => RamTotal > 0 ? RamUsed / RamTotal * 100 : 0;
    public double? Gpu { get => _gpu; private set { Set(ref _gpu, value); Raise(nameof(GpuLabel)); } }
    public string GpuLabel => Gpu is { } v ? $"{v:0}%" : "Nicht verfügbar";
    public string Download => Rate(_download);
    public string Upload => Rate(_upload);
    public SystemStatsService()
    {
        _timer = new() { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += async (_, _) => await SampleAsync();
    }
    public void SetActive(bool active)
    {
        if (_disposed) return;
        if (active && !_timer.IsEnabled) { _sampleTime = 0; _timer.Start(); _ = SampleAsync(); }
        else if (!active) _timer.Stop();
    }
    private async Task SampleAsync()
    {
        if (!await _gate.WaitAsync(0)) return;
        try
        {
            var snapshot = await Task.Run(Sample);
            if (_disposed) return;
            Cpu = snapshot.Cpu; RamTotal = snapshot.Total; RamUsed = snapshot.Used; Gpu = snapshot.Gpu;
            _download = snapshot.Down; _upload = snapshot.Up; Raise(nameof(Download)); Raise(nameof(Upload));
        }
        catch (Exception ex) { Log.Error("System stats", ex); }
        finally { _gate.Release(); }
    }
    private (double Cpu, double Used, double Total, double? Gpu, double Down, double Up) Sample()
    {
        var cpu = Cpu;
        if (NativeMethods.GetSystemTimes(out var idleTime, out var kernelTime, out var userTime))
        {
            var idle = ToLong(idleTime); var kernel = ToLong(kernelTime); var user = ToLong(userTime);
            var total = kernel - _kernel + user - _user;
            if (_sampleTime > 0 && total > 0) cpu = Math.Clamp((1 - (double)(idle - _idle) / total) * 100, 0, 100);
            _idle = idle; _kernel = kernel; _user = user;
        }
        var memory = new NativeMethods.MemoryStatus { Length = (uint)Marshal.SizeOf<NativeMethods.MemoryStatus>() };
        NativeMethods.GlobalMemoryStatusEx(ref memory);
        long received = 0, sent = 0;
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces().Where(x => x.OperationalStatus == OperationalStatus.Up && x.NetworkInterfaceType != NetworkInterfaceType.Loopback))
        {
            var stats = nic.GetIPStatistics(); received += stats.BytesReceived; sent += stats.BytesSent;
        }
        var now = _clock.Elapsed.TotalSeconds; var delta = now - _sampleTime;
        var down = _sampleTime > 0 && delta > 0 ? Math.Max(0, received - _received) / delta : 0;
        var up = _sampleTime > 0 && delta > 0 ? Math.Max(0, sent - _sent) / delta : 0;
        _received = received; _sent = sent; _sampleTime = now;
        return (cpu, (memory.TotalPhys - memory.AvailPhys) / 1073741824d, memory.TotalPhys / 1073741824d, ReadGpu(), down, up);
    }
    private double? ReadGpu() { lock (_gpuLock) { return _disposed ? null : ReadGpuCore(); } }
    private double? ReadGpuCore()
    {
        // Windows' GPU Engine counters depend on the installed WDDM driver. No fabricated fallback.
        if (_gpuQuery == IntPtr.Zero)
        {
            if (PdhOpenQuery(null, IntPtr.Zero, out _gpuQuery) != 0) return null;
            if (PdhAddEnglishCounter(_gpuQuery, @"\GPU Engine(*)\Utilization Percentage", IntPtr.Zero, out _gpuCounter) != 0) return null;
            PdhCollectQueryData(_gpuQuery); return null;
        }
        if (_gpuCounter == IntPtr.Zero || PdhCollectQueryData(_gpuQuery) != 0) return null;
        uint bytes = 0, count = 0;
        PdhGetFormattedCounterArray(_gpuCounter, 0x200, ref bytes, out count, IntPtr.Zero);
        if (bytes == 0 || bytes > 10_000_000) return null;
        var buffer = Marshal.AllocHGlobal((int)bytes);
        try
        {
            if (PdhGetFormattedCounterArray(_gpuCounter, 0x200, ref bytes, out count, buffer) != 0) return null;
            var engines = new Dictionary<string, double>();
            for (var i = 0; i < count; i++)
            {
                var item = Marshal.PtrToStructure<CounterItem>(buffer + i * Marshal.SizeOf<CounterItem>());
                var name = Marshal.PtrToStringUni(item.Name) ?? "";
                if (item.Value.Status > 1 || !double.IsFinite(item.Value.Value)) continue;
                // Sum processes per physical engine, then use the busiest engine as Task Manager does.
                var key = System.Text.RegularExpressions.Regex.Replace(name, @"pid_\d+_", "");
                engines[key] = engines.GetValueOrDefault(key) + item.Value.Value;
            }
            return engines.Count > 0 ? Math.Clamp(engines.Values.Max(), 0, 100) : null;
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }
    private static long ToLong(System.Runtime.InteropServices.ComTypes.FILETIME time) => ((long)time.dwHighDateTime << 32) | (uint)time.dwLowDateTime;
    private static string Rate(double n) => n >= 1048576 ? $"{n / 1048576:0.0} MB/s" : $"{n / 1024:0} KB/s";
    [StructLayout(LayoutKind.Sequential)] private struct CounterValue { public uint Status; public double Value; }
    [StructLayout(LayoutKind.Sequential)] private struct CounterItem { public IntPtr Name; public CounterValue Value; }
    [DllImport("pdh.dll", CharSet = CharSet.Unicode)] private static extern uint PdhOpenQuery(string? source, IntPtr data, out IntPtr query);
    [DllImport("pdh.dll", CharSet = CharSet.Unicode)] private static extern uint PdhAddEnglishCounter(IntPtr query, string path, IntPtr data, out IntPtr counter);
    [DllImport("pdh.dll")] private static extern uint PdhCollectQueryData(IntPtr query);
    [DllImport("pdh.dll", CharSet = CharSet.Unicode)] private static extern uint PdhGetFormattedCounterArray(IntPtr counter, uint format, ref uint bytes, out uint count, IntPtr buffer);
    [DllImport("pdh.dll")] private static extern uint PdhCloseQuery(IntPtr query);
    public void Dispose() { _disposed = true; _timer.Stop(); lock (_gpuLock) { if (_gpuQuery != IntPtr.Zero) { PdhCloseQuery(_gpuQuery); _gpuQuery = IntPtr.Zero; } } }
}
