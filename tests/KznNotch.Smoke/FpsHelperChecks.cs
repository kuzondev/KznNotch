using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;

namespace KznNotch.Smoke;

internal static class FpsHelperChecks
{
    internal static async Task RunAsync(Action<bool, string> check)
    {
        var name = "KznNotch-Fps-" + Guid.NewGuid().ToString("N");
        using var pipe = new NamedPipeServerStream(name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        var info = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, CreateNoWindow = true };
        // The suite is launched by dotnet.exe; run the actual app's helper entry point.
        if (string.Equals(Path.GetFileNameWithoutExtension(Environment.ProcessPath), "dotnet", StringComparison.OrdinalIgnoreCase)) info.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "KznNotch.dll"));
        else info.FileName = Path.Combine(AppContext.BaseDirectory, "KznNotch.exe");
        info.ArgumentList.Add("--fps-helper"); info.ArgumentList.Add(name);
        using var helper = Process.Start(info)!;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try
        {
            await pipe.WaitForConnectionAsync(timeout.Token);
            using var reader = new StreamReader(pipe, Encoding.UTF8, false, 4096, true);
            using var writer = new StreamWriter(pipe, new UTF8Encoding(false), 4096, true) { AutoFlush = true };
            await writer.WriteLineAsync(Environment.ProcessId.ToString());
            var line = await reader.ReadLineAsync(timeout.Token);
            check(line is not null && (line.StartsWith("Application,") || line.StartsWith("ERROR:")), "Echter FPS-Helfer verbindet sich und meldet Messschema oder Windows-Zugriffsfehler");
            await writer.WriteLineAsync("0");
        }
        finally { pipe.Dispose(); }
        await helper.WaitForExitAsync(timeout.Token);
        check(helper.ExitCode == 0, "FPS-Helfer beendet sich nach Trennung der gleichbenutzergebundenen Pipe");
    }
}
