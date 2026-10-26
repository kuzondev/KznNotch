using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media.Effects;
using KznNotch.ViewModels;
using KznNotch.Utilities;

namespace KznNotch.Views;

public partial class NotchWindow : Window
{
    private readonly NotchViewModel _vm;
    private readonly System.Windows.Forms.Screen _screen;
    private readonly DispatcherTimer _collapse = new() { Interval = TimeSpan.FromMilliseconds(350) };
    private readonly DispatcherTimer _mediaTick = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly DispatcherTimer _hover = new() { Interval = TimeSpan.FromMilliseconds(120) };
    private NotchState? _presentedState;
    private string? _presentedFunction;
    private int _presentedIndex;
    private object? _presentedEvent;
    private HwndSource? _source;
    private double _scale = 1;
    public NotchWindow(AppHost host, System.Windows.Forms.Screen screen)
    {
        _screen = screen; _vm = new(host); DataContext = _vm; InitializeComponent();
        SourceInitialized += (_, _) =>
        {
            _source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle); _source.AddHook(WindowProc);
            NativeMethods.SetNoActivate(this, true); PositionWindow(); UpdateLayoutState();
        };
        // WPF can apply its initial window placement after SourceInitialized.
        // Anchor again once the first layout is complete, using monitor bounds.
        Loaded += (_, _) => { PositionWindow(); UpdateLayoutState(); };
        _collapse.Tick += (_, _) => { _collapse.Stop(); if (!Shell.IsMouseOver && !_vm.IsExpanded) _vm.SetState(NotchState.Compact); };
        _mediaTick.Tick += (_, _) => host.Media.UpdatePosition();
        _hover.Tick += (_, _) => { _hover.Stop(); if (Shell.IsMouseOver && _vm.State == NotchState.Compact) _vm.SetState(NotchState.Hover); };
        _vm.LayoutChanged += LayoutChanged;
        host.AppearanceChanged += LayoutChanged;
        Closed += (_, _) => { host.AppearanceChanged -= LayoutChanged; _collapse.Stop(); _hover.Stop(); _mediaTick.Stop(); _source?.RemoveHook(WindowProc); _vm.LayoutChanged -= LayoutChanged; _vm.Dispose(); };
        Deactivated += (_, _) => { if (_vm.IsExpanded && !host.Windows.SettingsOpen) _vm.SetState(NotchState.Compact); };
    }
    private void PositionWindow()
    {
        var settings = _vm.Host.Settings.Current;
        var minimal = _vm.GamingMinimal;
        var bounds = _screen.Bounds;
        var monitor = NativeMethods.MonitorFromPoint(new(bounds.Left + 1, bounds.Top + 1), 2);
        NativeMethods.GetDpiForMonitor(monitor, 0, out var dpi, out _); _scale = dpi > 0 ? dpi / 96d : 1;
        Width = minimal ? 160 : Math.Min(Math.Max(settings.HoverWidth, settings.CompactWidth) + 40, bounds.Width / _scale);
        var offsetY = settings.Presentation == NotchPresentation.ScreenEdge ? 0 : settings.OffsetY;
        Height = Math.Min(500, bounds.Height / _scale - offsetY - 16);
        Height = minimal ? 52 : Math.Max(120, Height);
        var left = settings.Position switch { NotchPosition.Left => bounds.Left + 12 * _scale, NotchPosition.Right => bounds.Right - (Width + 12) * _scale, _ => bounds.Left + (bounds.Width - Width * _scale) / 2 };
        left = Math.Clamp(left + settings.OffsetX * _scale, bounds.Left, Math.Max(bounds.Left, bounds.Right - Width * _scale));
        var top = Math.Clamp(bounds.Top + offsetY * _scale, bounds.Top, Math.Max(bounds.Top, bounds.Bottom - Height * _scale));
        if (minimal)
        {
            var corner = settings.GamingCorner;
            left = corner is GamingCorner.TopLeft or GamingCorner.BottomLeft ? bounds.Left : bounds.Right - Width * _scale;
            top = corner is GamingCorner.BottomLeft or GamingCorner.BottomRight ? bounds.Bottom - Height * _scale : bounds.Top;
        }
        NativeMethods.SetWindowPos(new WindowInteropHelper(this).Handle, new IntPtr(-1), (int)left, (int)top, (int)(Width * _scale), (int)(Height * _scale), 0x0010 | 0x0040);
    }
    private IntPtr WindowProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == 0x21 && !_vm.IsExpanded) { handled = true; return new IntPtr(3); }
        if (message == 0x84)
        {
            var x = (short)(lParam.ToInt64() & 0xFFFF); var y = (short)((lParam.ToInt64() >> 16) & 0xFFFF);
            var local = Shell.PointFromScreen(new Point(x, y));
            if (local.X < 0 || local.Y < 0 || local.X >= Shell.ActualWidth || local.Y >= Shell.ActualHeight) { handled = true; return new IntPtr(-1); }
        }
        return IntPtr.Zero;
    }
    private void LayoutChanged(object? sender, EventArgs e) => UpdateLayoutState();
    private void UpdateLayoutState()
    {
        var s = _vm.Host.Settings.Current;
        var width = _vm.ShowDetails ? s.HoverWidth : _vm.Host.Events.HasEvent ? Math.Max(340, s.CompactWidth) : s.CompactWidth;
        if (_vm.ShowDefaultCompact) width = Math.Max(width, 24 + _vm.CompactItems.Sum(item => Math.Min(128, item.Text.Length * 7) + 20 + (item.Image is not null || item.Glyph.Length > 0 ? 24 : 0)));
        var height = _vm.ShowDetails ? FunctionHeight() : _vm.Host.Events.HasEvent ? 76 : s.CompactHeight;
        if (_vm.GamingMinimal) { width = 132; height = 32; }
        height = Math.Min(height, Height - 8);
        width = Math.Min(width, Width - 16);
        Compact.Height = s.CompactHeight;
        Shell.CornerRadius = s.Presentation == NotchPresentation.ScreenEdge ? new CornerRadius(0, 0, s.CornerRadius, s.CornerRadius) : new CornerRadius(s.CornerRadius);
        if (_vm.GamingMinimal) Shell.CornerRadius = new CornerRadius(16);
        Shell.Margin = new Thickness(0);
        EventPanel.CornerRadius = Shell.CornerRadius;
        Shell.BorderBrush = SystemParameters.HighContrast ? SystemColors.WindowTextBrush : new SolidColorBrush(Color.FromArgb(18, 255, 255, 255));
        Shell.BorderThickness = SystemParameters.HighContrast ? new Thickness(1) : new Thickness(0);
        var color = SystemParameters.HighContrast ? SystemColors.WindowColor : (Color)ColorConverter.ConvertFromString(_vm.Host.Appearance.Background); color.A = SystemParameters.HighContrast ? (byte)255 : (byte)(s.Opacity * 255); Shell.Background = new SolidColorBrush(color);
        Shell.Effect = s.Shadow ? new DropShadowEffect { BlurRadius = 18, ShadowDepth = 4, Opacity = .2, Color = Colors.Black, RenderingBias = RenderingBias.Performance } : null;
        // WPF layered windows cannot use DWM Acrylic. A restrained surface blur is applied to transition content only.
        _vm.Host.Animation.Size(Shell, width, height);
        if (_presentedState != _vm.State) { _vm.Host.Animation.Reveal(Details); _presentedState = _vm.State; }
        if (!ReferenceEquals(_presentedEvent, _vm.Host.Events.Current))
        {
            _presentedEvent = _vm.Host.Events.Current;
            if (_vm.ShowEvent) _vm.Host.Animation.Reveal(EventPanel);
        }
        if (_presentedFunction != _vm.SelectedWidget?.Widget.Id)
        {
            FunctionScroll.ScrollToTop();
            var index = _vm.SelectedWidget is { } selected ? _vm.VisibleWidgets.IndexOf(selected) : 0;
            if (_vm.ShowDetails) _vm.Host.Animation.Reveal(FunctionContent, index >= _presentedIndex ? 10 : -10);
            _presentedIndex = index;
            _presentedFunction = _vm.SelectedWidget?.Widget.Id;
        }
        if (_vm.ShowDetails && _vm.SelectedWidget?.Widget.Id == "media") _mediaTick.Start(); else _mediaTick.Stop();
        NativeMethods.SetNoActivate(this, !_vm.IsExpanded);
        UpdateClickThrough();
    }
    private double FunctionHeight() => _vm.Host.Appearance.OpenHeight;
    public void Expand() => _vm.SetState(NotchState.Expanded);
    public void SetGamingMinimal(bool value)
    {
        if (_vm.GamingMinimal == value) return;
        _hover.Stop(); _collapse.Stop(); _vm.SetGamingMinimal(value);
        if (_source is not null) PositionWindow();
        UpdateLayoutState();
    }
    public void Collapse() => _vm.SetState(NotchState.Compact);
    private void Shell_Enter(object sender, MouseEventArgs e) { _collapse.Stop(); if (!_vm.GamingMinimal && _vm.State == NotchState.Compact && _vm.Host.Plugins.ActiveDocument?.Nodes.Any(x => x.Kind == "button") != true) _hover.Start(); }
    private void Shell_Leave(object sender, MouseEventArgs e) { _hover.Stop(); if (!_vm.IsExpanded) _collapse.Start(); }
    private void Shell_Click(object sender, MouseButtonEventArgs e)
    {
        for (var source = e.OriginalSource as DependencyObject; source is not null && source != Shell; source = source is Visual ? VisualTreeHelper.GetParent(source) : (source as FrameworkContentElement)?.Parent)
            if (source is ButtonBase or TextBoxBase or Selector or DatePicker) return;
        if (!_vm.IsExpanded) { Expand(); Activate(); }
    }
    private void Collapse_Click(object sender, RoutedEventArgs e) { Collapse(); e.Handled = true; }
    private void Settings_Click(object sender, RoutedEventArgs e) { _vm.Host.Windows.OpenSettings(); e.Handled = true; }
    private void Dismiss_Click(object sender, RoutedEventArgs e) { _vm.Host.Events.Dismiss(); e.Handled = true; }
    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) { Collapse(); e.Handled = true; }
        else if (Keyboard.Modifiers == ModifierKeys.Alt && e.SystemKey is Key.Left or Key.Right)
        {
            (e.SystemKey == Key.Left ? _vm.PreviousWidgetCommand : _vm.NextWidgetCommand).Execute(null); e.Handled = true;
        }
    }
    private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_vm.ShowPluginCompact)
            for (var source = e.OriginalSource as DependencyObject; source is not null; source = source is Visual ? VisualTreeHelper.GetParent(source) : (source as FrameworkContentElement)?.Parent)
                if (source is KznSurface) { _hover.Stop(); _collapse.Stop(); return; }
        for (var element = e.OriginalSource as DependencyObject; element is not null; element = element is Visual ? VisualTreeHelper.GetParent(element) : (element as FrameworkContentElement)?.Parent)
            if (element is ButtonBase or TextBoxBase or Selector or DatePicker)
            {
                // A deliberate interaction pins the hover surface before WPF handles the
                // press. This also enables normal focus/capture on the no-activate HWND.
                _hover.Stop(); _collapse.Stop();
                if (!_vm.IsExpanded) { _vm.SetState(NotchState.Expanded); Activate(); }
                return;
            }
    }
    private void UpdateClickThrough()
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero) return;
        var transparent = _vm.GamingMinimal || _vm.Host.Settings.Current.ClickThroughCompact && _vm.State == NotchState.Compact;
        var style = NativeMethods.GetWindowLong(handle, NativeMethods.GwlExStyle);
        // An explicit click-through mode deliberately disables hover. Tray restores interaction.
        var next = transparent ? style | NativeMethods.WsExTransparent : style & ~NativeMethods.WsExTransparent;
        if (style != next) NativeMethods.SetWindowLong(handle, NativeMethods.GwlExStyle, next);
    }
}
