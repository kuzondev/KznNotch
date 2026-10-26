using System.IO.Pipes;
using System.Text;

namespace KznNotch.Services;

internal static class FpsCaptureHelper
{
    // A separate, optionally elevated process only runs the bundled capture tool.
    // Its same-user pipe accepts integer PIDs, never executable paths or commands.
    internal static async Task RunAsync(string pipeName)
    {
        if (!pipeName.StartsWith("KznNotch-Fps-", StringComparison.Ordinal) || pipeName.Length > 80) return;
        using var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await pipe.ConnectAsync(15000);
        using var reader = new StreamReader(pipe, Encoding.UTF8, false, 4096, true);
        using var writer = new StreamWriter(pipe, new UTF8Encoding(false), 4096, true) { AutoFlush = true };
        Process? capture = null; Task pump = Task.CompletedTask;
        var session = pipeName;
        async Task StopAsync()
        {
            if (capture is null) return;
            try { if (!capture.HasExited) capture.Kill(true); } catch (Exception ex) when (ex is InvalidOperationException or Win32Exception) { }
            try { await pump; } catch (Exception ex) when (ex is IOException or ObjectDisposedException) { }
            capture.Dispose(); capture = null;
            using var cleanup = Start(["--session_name", session, "--terminate_existing_session"]);
            if (cleanup is not null) { _ = cleanup.StandardOutput.ReadToEndAsync(); _ = cleanup.StandardError.ReadToEndAsync(); await cleanup.WaitForExitAsync(); }
        }
        try
        {
            while (await reader.ReadLineAsync() is { } command)
            {
                await StopAsync();
                if (!int.TryParse(command, out var pid) || pid <= 0) continue;
                capture = Start(["--process_id", pid.ToString(CultureInfo.InvariantCulture), "--output_stdout", "--v1_metrics", "--no_track_gpu", "--no_track_display", "--no_track_input", "--session_name", session, "--terminate_on_proc_exit"]);
                if (capture is null) { await writer.WriteLineAsync("ERROR:Capture konnte nicht gestartet werden."); continue; }
                var current = capture;
                pump = Task.Run(async () =>
                {
                    var errors = current.StandardError.ReadToEndAsync();
                    while (await current.StandardOutput.ReadLineAsync() is { } line) await writer.WriteLineAsync(line);
                    await current.WaitForExitAsync();
                    var error = await errors;
                    if (current.ExitCode != 0) await writer.WriteLineAsync("ERROR:" + (error.Contains("access denied", StringComparison.OrdinalIgnoreCase) ? "Windows-Zugriff fehlt. FPS benötigt Mitgliedschaft in Leistungsprotokollbenutzer oder einen separat genehmigten erhöhten Helfer." : "Keine Messdaten. Spiel und FPS-Freigabe prüfen."));
                });
            }
        }
        catch (IOException) { }
        finally { await StopAsync(); }
    }
    private static Process? Start(string[] arguments)
    {
        var info = new ProcessStartInfo(Path.Combine(AppContext.BaseDirectory, "Tools", "PresentMon.exe")) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in arguments) info.ArgumentList.Add(argument);
        return Process.Start(info);
    }
}
