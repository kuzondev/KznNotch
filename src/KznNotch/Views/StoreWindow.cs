using System.Windows.Controls;
using System.Windows.Automation;
using KznNotch.Store;
using KznNotch.Storage;

namespace KznNotch.Views;

public sealed class StoreWindow : Window
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
    public StoreWindow(string? dataDirectory = null)
    {
        _storage = new(dataDirectory ?? Path.Combine(Log.DirectoryPath, "Store"));
        Title = "Kzn Store"; Width = 960; Height = 700; MinWidth = 740; MinHeight = 500;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI"); FontSize = 13;
        var language = _storage.Load("preferences.json", () => new StorePreferences()).Language;
        if (language is "de" or "en") _strings.Language = language;
        var catalogPath = Path.Combine(_storage.DirectoryPath, "catalog.json");
        if (File.Exists(catalogPath))
        {
            try { _catalog = StoreCatalog.Read(catalogPath); }
            catch (Exception ex) { Log.Error("Store catalog", ex); }
        }
        Utilities.SettingsPalette.Apply(this, SettingsAppearanceMode.System);
        SetResourceReference(BackgroundProperty, "CanvasBrush"); SetResourceReference(ForegroundProperty, "TextBrush");
        var buttonStyle = new Style(typeof(Button));
        buttonStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(14, 7, 14, 7)));
        buttonStyle.Setters.Add(new Setter(Control.BackgroundProperty, new DynamicResourceExtension("ControlBrush")));
        buttonStyle.Setters.Add(new Setter(Control.ForegroundProperty, new DynamicResourceExtension("TextBrush")));
        buttonStyle.Setters.Add(new Setter(Control.BorderBrushProperty, new DynamicResourceExtension("StrokeBrush")));
        var borderFactory = new FrameworkElementFactory(typeof(Border), "ButtonBorder");
        borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
        borderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(1));
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
        buttonStyle.Setters.Add(new Setter(Control.TemplateProperty, template));
        Resources[typeof(Button)] = buttonStyle;
        _search.SetResourceReference(Control.BackgroundProperty, "ControlBrush");
        _search.SetResourceReference(Control.ForegroundProperty, "TextBrush");
        _search.SetResourceReference(Control.BorderBrushProperty, "StrokeBrush");
        _search.TextChanged += (_, _) => { if (_searchHint is not null) _searchHint.Visibility = _search.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed; if (!_building) RenderProducts(); };
        SourceInitialized += (_, _) => Utilities.SettingsPalette.Apply(this, SettingsAppearanceMode.System);
        Activated += (_, _) => { if (_download is null) RenderProducts(); };
        Closed += (_, _) => { _download?.Cancel(); Microsoft.Win32.SystemEvents.UserPreferenceChanged -= AppearanceChanged; };
        Microsoft.Win32.SystemEvents.UserPreferenceChanged += AppearanceChanged;
        Build();
    }
    private void AppearanceChanged(object sender, Microsoft.Win32.UserPreferenceChangedEventArgs e)
        => Dispatcher.BeginInvoke(() => Utilities.SettingsPalette.Apply(this, SettingsAppearanceMode.System));
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
    private void Build()
    {
        _building = true;
        var root = new Grid(); root.SetResourceReference(Panel.BackgroundProperty, "CanvasBrush"); root.ColumnDefinitions.Add(new() { Width = new GridLength(190) }); root.ColumnDefinitions.Add(new());
        var sidebar = new DockPanel { Margin = new Thickness(16, 26, 16, 20), LastChildFill = false };
        var sidebarBackground = new Border { Child = sidebar }; sidebarBackground.SetResourceReference(Border.BackgroundProperty, "SidebarBrush"); root.Children.Add(sidebarBackground);
        var brand = Label("Kzn Store", 22); brand.FontWeight = FontWeights.SemiBold; brand.Margin = new Thickness(10, 0, 0, 30); DockPanel.SetDock(brand, Dock.Top); sidebar.Children.Add(brand);
        var navigation = new StackPanel(); DockPanel.SetDock(navigation, Dock.Top); sidebar.Children.Add(navigation);
        var discover = Action("Discover", () => { _library = false; Build(); }); discover.Margin = new Thickness(0, 0, 0, 6);
        var library = Action("Library", () => { _library = true; Build(); });
        (_library ? library : discover).SetResourceReference(Control.BackgroundProperty, "SelectionWashBrush");
        navigation.Children.Add(discover); navigation.Children.Add(library);
        var footer = new StackPanel(); DockPanel.SetDock(footer, Dock.Bottom); sidebar.Children.Add(footer);
        footer.Children.Add(Label(_strings["Language"], 11, true));
        var languages = new ComboBox { Margin = new Thickness(0, 8, 0, 18), Padding = new Thickness(8), ItemsSource = new[] { "Deutsch", "English" }, SelectedIndex = _strings.Language == "de" ? 0 : 1 };
        AutomationProperties.SetName(languages, _strings["Language"]);
        languages.SelectionChanged += (_, _) => { _strings.Language = languages.SelectedIndex == 0 ? "de" : "en"; _storage.Save("preferences.json", new StorePreferences { Language = _strings.Language }); Build(); };
        footer.Children.Add(languages); footer.Children.Add(Label(_strings["Catalog"], 11, true));
        var content = new Grid { Margin = new Thickness(30, 26, 30, 22) }; Grid.SetColumn(content, 1); root.Children.Add(content);
        content.RowDefinitions.Add(new() { Height = GridLength.Auto }); content.RowDefinitions.Add(new() { Height = GridLength.Auto });
        content.RowDefinitions.Add(new()); content.RowDefinitions.Add(new() { Height = GridLength.Auto });
        var heading = new StackPanel(); heading.Children.Add(Label(_library ? _strings["Library"] : _strings["Title"], 28));
        var subtitle = Label(_strings["Subtitle"], 13, true); subtitle.Margin = new Thickness(0, 8, 0, 22); heading.Children.Add(subtitle); content.Children.Add(heading);
        var toolbar = new DockPanel { Margin = new Thickness(0, 0, 0, 20) }; Grid.SetRow(toolbar, 1); content.Children.Add(toolbar);
        var refresh = Action("Refresh", RenderProducts); DockPanel.SetDock(refresh, Dock.Right); toolbar.Children.Add(refresh);
        var import = Action("Import", ImportCatalog); import.Margin = new Thickness(8, 0, 8, 0); import.IsEnabled = _download is null; DockPanel.SetDock(import, Dock.Right); toolbar.Children.Add(import);
        if (_search.Parent is Panel old) old.Children.Remove(_search);
        AutomationProperties.SetName(_search, _strings["Search"]); _search.ToolTip = _strings["Search"];
        var searchWrap = new Grid(); searchWrap.Children.Add(_search);
        _searchHint = Label(_strings["Search"], 13, true); _searchHint.IsHitTestVisible = false;
        _searchHint.VerticalAlignment = VerticalAlignment.Center; _searchHint.Margin = new Thickness(12, 0, 0, 0);
        _searchHint.Visibility = _search.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed; searchWrap.Children.Add(_searchHint); toolbar.Children.Add(searchWrap);
        if (_products.Parent is ScrollViewer oldScroll) oldScroll.Content = null;
        var scroll = new ScrollViewer { Content = _products, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled }; Grid.SetRow(scroll, 2); content.Children.Add(scroll);
        var bottom = new StackPanel { Margin = new Thickness(0, 16, 0, 0) }; Grid.SetRow(bottom, 3); content.Children.Add(bottom);
        if (_progress.Parent is Panel oldProgress) oldProgress.Children.Remove(_progress);
        _progress.Margin = new Thickness(0, 0, 0, 12); bottom.Children.Add(_progress);
        if (_status.Parent is Panel oldStatus) oldStatus.Children.Remove(_status);
        _status.SetResourceReference(TextBlock.ForegroundProperty, "MutedBrush"); AutomationProperties.SetLiveSetting(_status, AutomationLiveSetting.Polite); bottom.Children.Add(_status);
        if (_download is not null) bottom.Children.Add(Action("Cancel", () => _download.Cancel()));
        _status.Text = _strings[_statusKey] + (_statusKey == "Error" ? _error : ""); Content = root; _building = false; RenderProducts();
    }
    private Windows.ApplicationModel.Package? Installed(StoreProduct product)
    {
        try { return new Windows.Management.Deployment.PackageManager().FindPackagesForUser("").FirstOrDefault(x => x.Id.Name == product.PackageName && x.Id.Publisher == product.Publisher); }
        catch (Exception ex) { Log.Error("Store installed packages", ex); return null; }
    }
    private void RenderProducts()
    {
        _products.Children.Clear();
        foreach (var product in _catalog.Products.Where(p => (p.Name + " " + p.Describe(_strings.Language)).Contains(_search.Text, StringComparison.CurrentCultureIgnoreCase)))
        {
            var installed = Installed(product); if (_library && installed is null) continue;
            var row = new Grid(); row.ColumnDefinitions.Add(new() { Width = new GridLength(64) }); row.ColumnDefinitions.Add(new()); row.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
            var initials = Label(product.Id == "kzn-notch" ? "◒" : product.Name[..1], 30); initials.HorizontalAlignment = HorizontalAlignment.Center; initials.VerticalAlignment = VerticalAlignment.Center;
            var tile = new Border { Child = initials, Width = 50, Height = 50, CornerRadius = new CornerRadius(13), VerticalAlignment = VerticalAlignment.Top }; tile.SetResourceReference(Border.BackgroundProperty, "PreviewBrush"); row.Children.Add(tile);
            var details = new StackPanel { Margin = new Thickness(6, 0, 18, 0) }; Grid.SetColumn(details, 1); row.Children.Add(details);
            var name = Label(product.Name, 17); name.FontWeight = FontWeights.SemiBold; details.Children.Add(name);
            var description = Label(product.Describe(_strings.Language), 13, true); description.Margin = new Thickness(0, 7, 0, 8); details.Children.Add(description);
            string version = product.Version;
            if (installed is not null) { var v = installed.Id.Version; version = $"{v.Major}.{v.Minor}.{v.Build}.{v.Revision}"; }
            details.Children.Add(Label($"{_strings[installed is null ? "Available" : "Installed"]} · {version}", 11, true));
            details.Children.Add(Label($"{_strings["Publisher"]}: {product.Publisher}", 11, true));
            var hasUpdate = installed is not null && System.Version.Parse(product.Version) > System.Version.Parse(version);
            var key = installed is not null && (!hasUpdate || string.IsNullOrEmpty(product.DownloadUrl)) ? "Open" : string.IsNullOrEmpty(product.DownloadUrl) ? "Unavailable" : hasUpdate ? "Update" : "Install";
            var action = Action(key, () => { if (key == "Open") Open(product, installed!); else _ = InstallAsync(product); });
            action.VerticalAlignment = VerticalAlignment.Top; action.IsEnabled = _download is null && key != "Unavailable"; Grid.SetColumn(action, 2); row.Children.Add(action);
            var card = new Border { Child = row, CornerRadius = new CornerRadius(14), Padding = new Thickness(18), Margin = new Thickness(0, 0, 0, 12) }; card.SetResourceReference(Border.BackgroundProperty, "SurfaceBrush"); _products.Children.Add(card);
        }
        if (_products.Children.Count == 0) _products.Children.Add(Label(_strings["Empty"], 14, true));
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
            _storage.Save("catalog.json", merged); _catalog = merged; _statusKey = "Imported"; Build();
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
    private sealed class StorePreferences { public string Language { get; set; } = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "de" ? "de" : "en"; }
}
