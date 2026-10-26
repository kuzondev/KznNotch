using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Text.RegularExpressions;
using KznNotch.Plugins;
using KznNotch.ViewModels;

namespace KznNotch.Views;

public sealed class KznSurface : UserControl
{
    public static readonly DependencyProperty DocumentProperty = DependencyProperty.Register(nameof(Document), typeof(KznDocument), typeof(KznSurface), new PropertyMetadata(null, Changed));
    public static readonly DependencyProperty HostProperty = DependencyProperty.Register(nameof(Host), typeof(AppHost), typeof(KznSurface), new PropertyMetadata(null, Changed));
    public static readonly DependencyProperty BaseDirectoryProperty = DependencyProperty.Register(nameof(BaseDirectory), typeof(string), typeof(KznSurface), new PropertyMetadata("", Changed));
    public KznDocument? Document { get => (KznDocument?)GetValue(DocumentProperty); set => SetValue(DocumentProperty, value); }
    public AppHost? Host { get => (AppHost?)GetValue(HostProperty); set => SetValue(HostProperty, value); }
    public string BaseDirectory { get => (string)GetValue(BaseDirectoryProperty); set => SetValue(BaseDirectoryProperty, value); }
    private AppHost? _observed;
    private readonly List<(KznNode Node, FrameworkElement Element)> _elements = [];
    public KznSurface()
    {
        Loaded += (_, _) => { Observe(); Build(); };
        Unloaded += (_, _) => Detach();
        IsVisibleChanged += (_, _) => UpdateValues();
    }
    private static void Changed(DependencyObject target, DependencyPropertyChangedEventArgs e)
    { var surface = (KznSurface)target; if (surface.IsLoaded) surface.Observe(); surface.Build(); }
    private void Observe()
    {
        if (_observed == Host) return; Detach(); _observed = Host;
        if (_observed is null) return;
        _observed.Clock.PropertyChanged += ValuesChanged; _observed.Media.PropertyChanged += ValuesChanged;
        _observed.Timer.PropertyChanged += ValuesChanged; _observed.Gaming.PropertyChanged += ValuesChanged; _observed.Fps.PropertyChanged += ValuesChanged;
    }
    private void Detach()
    {
        if (_observed is null) return;
        _observed.Clock.PropertyChanged -= ValuesChanged; _observed.Media.PropertyChanged -= ValuesChanged;
        _observed.Timer.PropertyChanged -= ValuesChanged; _observed.Gaming.PropertyChanged -= ValuesChanged; _observed.Fps.PropertyChanged -= ValuesChanged; _observed = null;
    }
    private void ValuesChanged(object? sender, PropertyChangedEventArgs e) => Ui.Run(() => { if (IsVisible) UpdateValues(); });
    private void Build()
    {
        _elements.Clear(); Content = null;
        if (Document is not { } doc) return;
        var panel = new StackPanel { Orientation = doc.Vertical ? Orientation.Vertical : Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        var foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(doc.Color)); foreground.Freeze();
        foreach (var node in doc.Nodes)
        {
            FrameworkElement element;
            if (node.Kind == "image")
            {
                try
                {
                    var path = KznCompiler.AssetPath(BaseDirectory, node.Value);
                    using var stream = File.OpenRead(path);
                    var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
                    var frame = decoder.Frames[0];
                    if (frame.PixelWidth > 16000 || frame.PixelHeight > 16000) throw new IOException("Bildabmessungen zu groß.");
                    stream.Position = 0;
                    var bitmap = new BitmapImage(); bitmap.BeginInit(); bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    if (frame.PixelWidth >= frame.PixelHeight) bitmap.DecodePixelWidth = node.Size * 2; else bitmap.DecodePixelHeight = node.Size * 2;
                    bitmap.StreamSource = stream; bitmap.EndInit(); bitmap.Freeze();
                    element = new Image { Source = bitmap, Width = node.Size, Height = node.Size, Stretch = Stretch.Uniform };
                    System.Windows.Automation.AutomationProperties.SetName(element, "Plugin-Bild: " + Path.GetFileName(node.Value));
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or FileFormatException or KznCompileException)
                { element = new TextBlock { Text = "Bild?", Foreground = foreground, ToolTip = ex.Message }; }
            }
            else if (node.Kind == "button")
            {
                var button = new Button { Content = node.Value, Style = TryFindResource("IslandButton") as Style, Foreground = foreground, Padding = new Thickness(8, 3, 8, 3), MinHeight = 28, FontFamily = new FontFamily(doc.Font), FontSize = doc.FontSize };
                void RunAction()
                {
                    if (Window.GetWindow(this)?.DataContext is NotchViewModel vm)
                    {
                        if (node.Action == "settings") vm.Host.Windows.OpenSettings();
                        else if (vm.VisibleWidgets.FirstOrDefault(x => x.Widget.Id == node.Action) is { } entry)
                        { vm.SelectedWidget = entry; vm.SetState(NotchState.Expanded); Window.GetWindow(this)?.Activate(); }
                        else Host?.Plugins.Notify("Diese Funktion zuerst unter Widgets aktivieren.");
                    }
                    else Host?.Plugins.Notify("Vorschau der Aktion: " + node.Action);
                }
                button.Click += (_, e) => { RunAction(); e.Handled = true; };
                button.PreviewMouseLeftButtonDown += (_, e) =>
                { if (Window.GetWindow(this)?.DataContext is NotchViewModel { ShowPluginCompact: true }) { RunAction(); e.Handled = true; } };
                element = button;
            }
            else element = new TextBlock { Foreground = foreground, FontFamily = new FontFamily(doc.Font), FontSize = doc.FontSize, MaxWidth = 240, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };
            element.Margin = doc.Vertical ? new Thickness(0, 0, 0, doc.Spacing) : new Thickness(0, 0, doc.Spacing, 0);
            panel.Children.Add(element); _elements.Add((node, element));
        }
        if (panel.Children.Count > 0) ((FrameworkElement)panel.Children[^1]).Margin = new Thickness(0);
        Content = panel; UpdateValues();
    }
    private void UpdateValues()
    {
        if (Host is not { } host) return;
        foreach (var (node, element) in _elements)
        {
            var visible = node.Condition switch { "media.playing" => host.Media.Playing, "timer.running" => host.Timer.Running, "gaming.active" => host.Gaming.IsGaming, _ => true };
            element.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            if (element is not TextBlock text || node.Kind == "image") continue;
            text.Text = node.Kind == "clock" ? DateTime.Now.ToString(node.Value, CultureInfo.CurrentCulture) : Regex.Replace(node.Value, "\\{(clock|date|media\\.title|media\\.artist|timer|fps)\\}", m => m.Groups[1].Value switch
            { "clock" => host.Clock.Time, "date" => DateTime.Now.ToString("d"), "media.title" => host.Media.Title, "media.artist" => host.Media.Artist, "timer" => host.Timer.Display, "fps" => host.Fps.OverlayText, _ => m.Value }, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
            text.ToolTip = text.Text;
        }
    }
}
