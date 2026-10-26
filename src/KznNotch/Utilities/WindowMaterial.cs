using System.Windows.Interop;

namespace KznNotch.Utilities;

internal static class WindowMaterial
{
    [StructLayout(LayoutKind.Sequential)] private struct Margins { public int Left, Right, Top, Bottom; }
    [DllImport("dwmapi.dll")] private static extern int DwmExtendFrameIntoClientArea(IntPtr window, ref Margins margins);

    public static bool ApplyMica(Window window, bool darkAppearance = true)
    {
        var handle = new WindowInteropHelper(window).Handle;
        var dark = darkAppearance ? 1 : 0; NativeMethods.DwmSetWindowAttribute(handle, 20, ref dark, sizeof(int));
        if (!SystemParameters.HighContrast && OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22621))
        {
            var backdrop = 2; // DWMSBT_MAINWINDOW: native Mica, on a normal non-layered settings HWND.
            if (NativeMethods.DwmSetWindowAttribute(handle, 38, ref backdrop, sizeof(int)) == 0)
            {
                var margins = new Margins { Left = -1, Right = -1, Top = -1, Bottom = -1 };
                DwmExtendFrameIntoClientArea(handle, ref margins);
                if (HwndSource.FromHwnd(handle)?.CompositionTarget is { } target) target.BackgroundColor = Colors.Transparent;
                window.Background = Brushes.Transparent;
                return true;
            }
        }
        window.Background = SystemParameters.HighContrast ? SystemColors.WindowBrush : new SolidColorBrush(darkAppearance ? Color.FromRgb(23, 27, 34) : Color.FromRgb(245, 245, 247));
        return false;
    }
}
