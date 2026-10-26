using System;
using System.Diagnostics;
using System.IO;

internal static class Program
{
    [STAThread]
    private static void Main() => Process.Start(new ProcessStartInfo(Path.Combine(AppContext.BaseDirectory, "KznNotch.exe"), "--store") { UseShellExecute = true });
}
