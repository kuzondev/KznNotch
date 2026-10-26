using System.Windows.Controls;
using System.Windows.Automation;
using KznNotch.Store;
using KznNotch.Storage;

namespace KznNotch.Views;

public sealed partial class StoreWindow : Window
{
    private readonly JsonStore _storage;
    private readonly StoreStrings _strings = new();
    private StoreCatalog _catalog = StoreCatalog.Default;
    private readonly StackPanel _products = new();
    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap };
    private readonly ProgressBar _progress = new() { Minimum = 0, Maximum = 100, Height = 4, Visibility = Visibility.Collapsed };
    private readonly TextBox _search = new() { MinWidth = 150, Padding = new Thickness(10, 7, 10, 7) };
    private TextBlock? _searchHint;
    private CancellationTokenSource? _download;
    private bool _library;
    private bool _building;
    private string _statusKey = "Ready";
    private string _error = "";
    private string _page = "today";
    private StoreProduct? _selectedProduct;
    private readonly Func<string, CancellationToken, Task<GitHubCommunity?>> _communityLoader;
    private readonly Func<string, Task> _updateChecker;
    private readonly Dictionary<string, GitHubCommunity?> _community = new(StringComparer.OrdinalIgnoreCase);
    private readonly CancellationTokenSource _lifetime = new();
    private bool _refreshingCommunity, _checkingUpdates, _closed;
    private DateTimeOffset _lastCommunityRefresh;
    public StoreWindow(string? dataDirectory = null, Func<string, CancellationToken, Task<GitHubCommunity?>>? communityLoader = null, Func<string, Task>? updateChecker = null, bool enableDemo = false)
    {
        _communityLoader = communityLoader ?? ((repository, cancellation) => GitHubCommunityClient.FetchAsync(repository, cancellation));
        _updateChecker = updateChecker ?? (language => Services.GitHubUpdateService.CheckAndPromptAsync(true, language));
        _storage = new(dataDirectory ?? Path.Combine(Log.DirectoryPath, "Store"));
        Title = "Kzn Store"; Width = 980; Height = 820; MinWidth = 640; MinHeight = 600;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI"); FontSize = 13;
        var preferences = _storage.Load("preferences.json", () => new StorePreferences());
        if (preferences.Language is "de" or "en") _strings.Language = preferences.Language;
        _demoEnabled = preferences.DemoEnabled || enableDemo;
        if (enableDemo) SavePreferences();
        var catalogPath = Path.Combine(_storage.DirectoryPath, "catalog.json");
        if (File.Exists(catalogPath))
        {
            try { _catalog = StoreCatalog.Read(catalogPath); }
            catch (Exception ex) { Log.Error("Store catalog", ex); }
        }
        ApplyStoreAppearance();
        SetResourceReference(BackgroundProperty, "CanvasBrush"); SetResourceReference(ForegroundProperty, "TextBrush");
        var buttonStyle = new Style(typeof(Button));
        buttonStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(14, 7, 14, 7)));
        buttonStyle.Setters.Add(new Setter(Control.BackgroundProperty, new DynamicResourceExtension("ControlBrush")));
        buttonStyle.Setters.Add(new Setter(Control.ForegroundProperty, new DynamicResourceExtension("TextBrush")));
        buttonStyle.Setters.Add(new Setter(Control.BorderBrushProperty, new DynamicResourceExtension("StrokeBrush")));
        var borderFactory = new FrameworkElementFactory(typeof(Border), "ButtonBorder");
        borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(18));
        borderFactory.SetBinding(Border.BorderThicknessProperty, new System.Windows.Data.Binding("BorderThickness") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
        borderFactory.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
        borderFactory.SetBinding(Border.BorderBrushProperty, new System.Windows.Data.Binding("BorderBrush") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
        borderFactory.SetBinding(Border.PaddingProperty, new System.Windows.Data.Binding("Padding") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
        var presenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
        presenterFactory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        presenterFactory.SetValue(ContentPresenter.RecognizesAccessKeyProperty, true); borderFactory.AppendChild(presenterFactory);
        var template = new ControlTemplate(typeof(Button)) { VisualTree = borderFactory };
        var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        hover.Setters.Add(new Setter(Border.BackgroundProperty, new DynamicResourceExtension("HoverBrush"), "ButtonBorder")); template.Triggers.Add(hover);
        var focus = new Trigger { Property = UIElement.IsKeyboardFocusedProperty, Value = true };
        focus.Setters.Add(new Setter(Border.BorderBrushProperty, new DynamicResourceExtension("AccentBrush"), "ButtonBorder")); template.Triggers.Add(focus);
        var disabled = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
        disabled.Setters.Add(new Setter(UIElement.OpacityProperty, .5)); template.Triggers.Add(disabled);
        var pressed = new Trigger { Property = Button.IsPressedProperty, Value = true };
        pressed.Setters.Add(new Setter(UIElement.OpacityProperty, .65)); template.Triggers.Add(pressed);
        buttonStyle.Setters.Add(new Setter(Control.TemplateProperty, template));
        Resources[typeof(Button)] = buttonStyle;
        _search.SetResourceReference(Control.BackgroundProperty, "ControlBrush");
        _search.SetResourceReference(Control.ForegroundProperty, "TextBrush");
        _search.SetResourceReference(Control.BorderBrushProperty, "StrokeBrush");
        _search.TextChanged += (_, _) => { if (_searchHint is not null) _searchHint.Visibility = _search.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed; if (!_building) RenderProducts(); };
        SourceInitialized += (_, _) => ApplyStoreAppearance();
        Activated += (_, _) => { if (_download is null) RenderProducts(); };
        Loaded += async (_, _) => await RefreshCommunityAsync();
        Closed += (_, _) => { _closed = true; _lifetime.Cancel(); _download?.Cancel(); Microsoft.Win32.SystemEvents.UserPreferenceChanged -= AppearanceChanged; };
        Microsoft.Win32.SystemEvents.UserPreferenceChanged += AppearanceChanged;
        Build();
    }
    private void AppearanceChanged(object sender, Microsoft.Win32.UserPreferenceChangedEventArgs e)
        => Dispatcher.BeginInvoke(ApplyStoreAppearance);
    private void ApplyStoreAppearance()
    {
        Utilities.SettingsPalette.Apply(this, SettingsAppearanceMode.System);
        if (!SystemParameters.HighContrast)
            Resources["AccentBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Utilities.SettingsPalette.IsDark(SettingsAppearanceMode.System) ? "#70B5FF" : "#0062C4"));
    }
    private TextBlock Label(string text, double size = 13, bool muted = false)
    {
        var label = new TextBlock { Text = text, FontSize = size, TextWrapping = TextWrapping.Wrap };
        label.SetResourceReference(TextBlock.ForegroundProperty, muted ? "MutedBrush" : "TextBrush"); return label;
    }
    private Button Action(string key, Action click)
    {
        var button = new Button { Content = _strings[key], HorizontalContentAlignment = HorizontalAlignment.Left };
        button.Click += (_, _) => click(); return button;
    }
    private async void Open(StoreProduct product, Windows.ApplicationModel.Package package)
    {
        try
        {
            if (product.Id == "kzn-notch")
            {
                Process.Start(new ProcessStartInfo(Path.Combine(package.InstalledLocation.Path, "KznNotch.exe")) { UseShellExecute = true }); return;
            }
            var entries = await package.GetAppListEntriesAsync();
            if (entries.Count == 0 || !await entries[0].LaunchAsync()) throw new InvalidOperationException("Application could not be launched.");
        }
        catch (Exception ex) { _status.Text = _strings["Error"] + ex.Message; }
    }
    private void ImportCatalog()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "Kzn catalog (*.json)|*.json", Title = _strings["Import"] };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            var incoming = StoreCatalog.Read(dialog.FileName);
            if (MessageBox.Show(this, _strings["CatalogNote"] + "\n\n" + string.Join("\n", incoming.Products.Select(p => $"{p.Name} · {p.Publisher}")), _strings["Import"], MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;
            var merged = new StoreCatalog { Products = _catalog.Products.Where(p => incoming.Products.All(i => !i.Id.Equals(p.Id, StringComparison.OrdinalIgnoreCase))).Concat(incoming.Products).ToList() };
            if (merged.Products.Count > 200) throw new InvalidDataException("Maximum 200 products.");
            _storage.Save("catalog.json", merged); _catalog = merged; _statusKey = "Imported"; Build(); _ = RefreshCommunityAsync(true);
        }
        catch (Exception ex) { _status.Text = _strings["Error"] + ex.Message; }
    }
    private async Task InstallAsync(StoreProduct product)
    {
        if (_download is not null) return;
        using var cancellation = new CancellationTokenSource(); _download = cancellation; _statusKey = "Downloading";
        _progress.Value = 0; _progress.Visibility = Visibility.Visible; Build();
        try
        {
            var path = await StoreCatalog.DownloadAsync(product, Path.Combine(_storage.DirectoryPath, "Downloads"), new Progress<double>(p => _progress.Value = p), cancellation.Token);
            if (cancellation.IsCancellationRequested) return;
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); _statusKey = "Installer";
        }
        catch (OperationCanceledException) { _statusKey = "Cancelled"; }
        catch (Exception ex) { _statusKey = "Error"; _error = ex.Message; Log.Error("Store install", ex); }
        finally { _download = null; _progress.Visibility = Visibility.Collapsed; Build(); }
    }
    private void SavePreferences() => _storage.Save("preferences.json", new StorePreferences { Language = _strings.Language, DemoEnabled = _demoEnabled });
    private sealed class StorePreferences { public string Language { get; set; } = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "de" ? "de" : "en"; public bool DemoEnabled { get; set; } }
}
