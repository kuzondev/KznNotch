using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Automation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using KznNotch.Store;

namespace KznNotch.Views;

public sealed partial class StoreWindow
{
    private Border Surface(UIElement child, double radius = 20, double padding = 20)
    {
        var border = new Border { Child = child, CornerRadius = new CornerRadius(radius), Padding = new Thickness(padding) };
        border.SetResourceReference(Border.BackgroundProperty, "SurfaceBrush"); return border;
    }
    private Button Pill(string key, Action click, bool primary = false)
    {
        var button = Action(key, click); button.MinHeight = 36; button.MinWidth = 86; button.FontWeight = FontWeights.SemiBold;
        button.BorderThickness = new Thickness(0); button.SetResourceReference(Control.BackgroundProperty, primary ? "PrimaryActionBrush" : "ControlBrush");
        button.SetResourceReference(Control.ForegroundProperty, primary ? "OnAccentBrush" : "AccentBrush"); return button;
    }
    private FrameworkElement AppIcon(StoreProduct product, double size)
    {
        UIElement child;
        if (product.Id == "kzn-notch")
        {
            var mark = new Grid { Width = size, Height = size };
            mark.Children.Add(new System.Windows.Shapes.Ellipse { Width = size * .76, Height = size * .43, Stroke = Brushes.White, StrokeThickness = size * .08, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center });
            mark.Children.Add(new System.Windows.Shapes.Ellipse { Width = size * .12, Height = size * .12, Fill = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, RenderTransform = new TranslateTransform(size * .19, 0) });
            child = mark;
        }
        else { var initial = Label(product.Name[..1], size * .45); initial.HorizontalAlignment = HorizontalAlignment.Center; initial.VerticalAlignment = VerticalAlignment.Center; child = initial; }
        return new Border { Child = child, Width = size, Height = size, CornerRadius = new CornerRadius(size * .23), Background = product.Id == "kzn-notch" ? new SolidColorBrush(Color.FromRgb(8, 10, 14)) : (Brush)FindResource("PreviewBrush"), ClipToBounds = true };
    }
    private void Navigate(string page)
    {
        _page = page; _library = page == "library"; _selectedProduct = null; Build();
        if (SystemParameters.ClientAreaAnimation && !SystemParameters.HighContrast)
            _products.BeginAnimation(OpacityProperty, new DoubleAnimation(.45, 1, TimeSpan.FromMilliseconds(160)));
    }
    private void Build()
    {
        if (_closed) return;
        _building = true;
        var root = new Grid(); root.SetResourceReference(Panel.BackgroundProperty, "CanvasBrush");
        root.RowDefinitions.Add(new() { Height = GridLength.Auto }); root.RowDefinitions.Add(new()); root.RowDefinitions.Add(new() { Height = GridLength.Auto });
        var header = new Grid { Margin = new Thickness(30, 22, 30, 18) }; root.Children.Add(header);
        header.ColumnDefinitions.Add(new()); header.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        var heading = new StackPanel(); header.Children.Add(heading);
        var culture = CultureInfo.GetCultureInfo(_strings.Language == "de" ? "de-DE" : "en-US");
        var date = Label(DateTime.Now.ToString("dddd, d. MMMM", culture).ToUpper(culture), 11, true); date.FontWeight = FontWeights.SemiBold; heading.Children.Add(date);
        var title = Label(_selectedProduct is not null ? _selectedProduct.Name : _page == "today" ? _strings["Today"] : _strings[_library ? "Library" : "Apps"], 32);
        title.FontWeight = FontWeights.Bold; title.Margin = new Thickness(0, 4, 0, 0); heading.Children.Add(title);
        var controls = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center }; Grid.SetColumn(controls, 1); header.Children.Add(controls);
        var updates = Pill(_checkingUpdates ? "Checking" : "CheckUpdates", () => _ = CheckUpdatesAsync()); updates.IsEnabled = !_checkingUpdates && _download is null; updates.Margin = new Thickness(8, 0, 12, 0); controls.Children.Add(updates);
        var languages = new ComboBox { Width = 94, Padding = new Thickness(8), VerticalAlignment = VerticalAlignment.Center, ItemsSource = new[] { "Deutsch", "English" }, SelectedIndex = _strings.Language == "de" ? 0 : 1 };
        AutomationProperties.SetName(languages, _strings["Language"]); controls.Children.Add(languages);
        languages.SelectionChanged += (_, _) => { _strings.Language = languages.SelectedIndex == 0 ? "de" : "en"; SavePreferences(); Build(); };
        var body = new Grid { Margin = new Thickness(30, 0, 30, 0) }; Grid.SetRow(body, 1); root.Children.Add(body);
        body.RowDefinitions.Add(new() { Height = GridLength.Auto }); body.RowDefinitions.Add(new()); body.RowDefinitions.Add(new() { Height = GridLength.Auto });
        var toolbar = new DockPanel { Margin = new Thickness(0, 0, 0, 18) }; body.Children.Add(toolbar);
        if (_selectedProduct is not null)
        { var back = Pill("Back", () => { _selectedProduct = null; Build(); }); back.HorizontalAlignment = HorizontalAlignment.Left; toolbar.Children.Add(back); }
        else
        {
            var import = Pill("Import", ImportCatalog); import.Margin = new Thickness(12, 0, 0, 0); import.IsEnabled = _download is null; DockPanel.SetDock(import, Dock.Right); toolbar.Children.Add(import);
            if (_search.Parent is Panel old) old.Children.Remove(_search);
            _search.BorderThickness = new Thickness(0); _search.MinHeight = 38;
            AutomationProperties.SetName(_search, _strings["Search"]);
            var searchWrap = new Grid(); searchWrap.Children.Add(_search);
            _searchHint = Label("⌕  " + _strings["Search"], 14, true); _searchHint.IsHitTestVisible = false;
            _searchHint.VerticalAlignment = VerticalAlignment.Center; _searchHint.Margin = new Thickness(12, 0, 0, 0);
            _searchHint.Visibility = _search.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed; searchWrap.Children.Add(_searchHint);
            toolbar.Children.Add(new Border { Child = searchWrap, CornerRadius = new CornerRadius(12), ClipToBounds = true });
        }
        if (_products.Parent is ScrollViewer oldScroll) oldScroll.Content = null;
        var scroll = new ScrollViewer { Content = _products, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Padding = new Thickness(0, 0, 4, 0) };
        Grid.SetRow(scroll, 1); body.Children.Add(scroll);
        var feedback = new StackPanel { Margin = new Thickness(0, 12, 0, 12) }; Grid.SetRow(feedback, 2); body.Children.Add(feedback);
        if (_progress.Parent is Panel oldProgress) oldProgress.Children.Remove(_progress); feedback.Children.Add(_progress);
        if (_status.Parent is Panel oldStatus) oldStatus.Children.Remove(_status);
        _status.SetResourceReference(TextBlock.ForegroundProperty, "MutedBrush"); _status.FontSize = 11; _status.Margin = new Thickness(0, 6, 0, 0);
        AutomationProperties.SetLiveSetting(_status, AutomationLiveSetting.Polite); feedback.Children.Add(_status);
        if (_download is not null) feedback.Children.Add(Pill("Cancel", () => _download.Cancel()));
        _status.Text = _strings[_statusKey] + (_statusKey == "Error" ? _error : "");
        var tabs = new UniformGrid { Columns = 3, MaxWidth = 600, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(20, 8, 20, 8) };
        foreach (var (page, key, symbol) in new[] { ("today", "Today", "\uE8A5"), ("apps", "Apps", "\uE71D"), ("library", "Library", "\uE8F1") })
        {
            var tab = Action(key, () => Navigate(page)); tab.MinWidth = 150; tab.MinHeight = 48; tab.Background = Brushes.Transparent; tab.BorderBrush = Brushes.Transparent;
            tab.SetResourceReference(Control.ForegroundProperty, _page == page ? "AccentBrush" : "MutedBrush");
            var tabContent = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            tabContent.Children.Add(new TextBlock { Text = symbol, FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"), FontSize = 19, HorizontalAlignment = HorizontalAlignment.Center });
            tabContent.Children.Add(new TextBlock { Text = _strings[key], FontSize = 11, Margin = new Thickness(0, 3, 0, 0), HorizontalAlignment = HorizontalAlignment.Center });
            tab.Content = tabContent; AutomationProperties.SetName(tab, _strings[key]); tabs.Children.Add(tab);
        }
        var tabBar = new Border { Child = tabs, BorderThickness = new Thickness(0, 1, 0, 0) }; tabBar.SetResourceReference(Border.BorderBrushProperty, "DividerBrush"); tabBar.SetResourceReference(Border.BackgroundProperty, "SidebarBrush"); Grid.SetRow(tabBar, 2); root.Children.Add(tabBar);
        Content = root; _building = false; RenderProducts();
    }
    private Windows.ApplicationModel.Package? Installed(StoreProduct product)
    {
        try { return new Windows.Management.Deployment.PackageManager().FindPackagesForUser("").FirstOrDefault(x => x.Id.Name == product.PackageName && x.Id.Publisher == product.Publisher); }
        catch (Exception ex) { Log.Error("Store installed packages", ex); return null; }
    }
    private string Stars(StoreProduct product)
    {
        if (IsDemo(product)) return _demoStars.ToString("N0", CultureInfo.GetCultureInfo(_strings.Language == "de" ? "de-DE" : "en-US"));
        if (_community.TryGetValue(product.Repository, out var community)) return community is null ? _strings["OfflineStars"] : community.Stars.ToString("N0", CultureInfo.GetCultureInfo(_strings.Language == "de" ? "de-DE" : "en-US"));
        return _refreshingCommunity ? _strings["LoadingStars"] : _strings["OfflineStars"];
    }
    private Button CommunityButton(StoreProduct product)
    {
        var button = Pill("Stars", () => OpenRepository(product)); button.Content = "☆ " + Stars(product) + " · " + _strings[IsDemo(product) ? "DemoStars" : "Stars"];
        button.ToolTip = _strings[IsDemo(product) ? "DemoOnly" : "StarsHint"]; button.Background = Brushes.Transparent; button.HorizontalAlignment = HorizontalAlignment.Left; button.Padding = new Thickness(0, 5, 0, 5); return button;
    }
    private void OpenRepository(StoreProduct product)
    {
        if (IsDemo(product)) { OpenDemoSource(); return; }
        if (!GitHubCommunityClient.ValidRepository(product.Repository)) return;
        try { Process.Start(new ProcessStartInfo("https://github.com/" + product.Repository) { UseShellExecute = true }); }
        catch (Exception ex) { _status.Text = _strings["Error"] + ex.Message; }
    }
    private Button ProductAction(StoreProduct product)
    {
        var installed = Installed(product); var version = installed?.Id.Version;
        var installedVersion = version is { } v ? new Version(v.Major, v.Minor, v.Build, v.Revision) : null;
        var hasUpdate = installedVersion is not null && Version.Parse(product.Version) > installedVersion;
        var key = installed is not null && (!hasUpdate || string.IsNullOrEmpty(product.DownloadUrl)) ? "Open" : string.IsNullOrEmpty(product.DownloadUrl) ? "Unavailable" : hasUpdate ? "Update" : "Install";
        var button = Pill(key, () => { if (key == "Open") Open(product, installed!); else _ = InstallAsync(product); }, key is "Install" or "Update");
        button.IsEnabled = _download is null && key != "Unavailable"; return button;
    }
    private FrameworkElement Feature(StoreProduct product, bool detail = false)
    {
        var grid = new Grid { MinHeight = 220 }; grid.ColumnDefinitions.Add(new() { Width = new GridLength(1.2, GridUnitType.Star) }); grid.ColumnDefinitions.Add(new());
        var copy = new StackPanel { Margin = new Thickness(24) }; grid.Children.Add(copy);
        var kicker = new TextBlock { Text = _strings[detail ? "Preview" : "Featured"], Foreground = new SolidColorBrush(Color.FromRgb(163, 185, 219)), FontSize = 11, FontWeight = FontWeights.SemiBold }; copy.Children.Add(kicker);
        copy.Children.Add(new TextBlock { Text = _strings["HeroTitle"], Foreground = Brushes.White, FontSize = 28, FontWeight = FontWeights.Bold, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 10) });
        copy.Children.Add(new TextBlock { Text = _strings["HeroSubtitle"], Foreground = new SolidColorBrush(Color.FromRgb(198, 210, 229)), FontSize = 13, TextWrapping = TextWrapping.Wrap });
        if (!detail) { var discover = Pill("ViewApp", () => { _selectedProduct = product; Build(); }); discover.Margin = new Thickness(0, 16, 0, 0); discover.HorizontalAlignment = HorizontalAlignment.Left; copy.Children.Add(discover); }
        var preview = new Grid { Margin = new Thickness(10, 26, 22, 26), VerticalAlignment = VerticalAlignment.Center }; Grid.SetColumn(preview, 1); grid.Children.Add(preview);
        var island = new StackPanel { Margin = new Thickness(18, 14, 18, 14) };
        island.Children.Add(new TextBlock { Text = "Kzn Notch", Foreground = Brushes.White, FontWeight = FontWeights.SemiBold, FontSize = 13 });
        island.Children.Add(new TextBlock { Text = _strings.Language == "de" ? "Zeit für deinen Fokus." : "Time to find your focus.", Foreground = new SolidColorBrush(Color.FromRgb(172, 177, 189)), FontSize = 12, Margin = new Thickness(0, 12, 0, 8), TextWrapping = TextWrapping.Wrap });
        island.Children.Add(new TextBlock { Text = "25:00", Foreground = Brushes.White, FontSize = 35, FontWeight = FontWeights.Medium, HorizontalAlignment = HorizontalAlignment.Center });
        island.Children.Add(new TextBlock { Text = "♫     ◷     ☑     ♧", Foreground = new SolidColorBrush(Color.FromRgb(163, 185, 219)), FontSize = 16, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 14, 0, 0) });
        preview.Children.Add(new Border { Background = new SolidColorBrush(Color.FromRgb(6, 8, 13)), CornerRadius = new CornerRadius(22), Child = island, MaxWidth = 270 });
        return new Border { Child = grid, CornerRadius = new CornerRadius(24), Background = new SolidColorBrush(Color.FromRgb(24, 39, 63)), Margin = new Thickness(0, 0, 0, 22) };
    }
    private void RenderProducts()
    {
        _products.Children.Clear();
        if (_selectedProduct is { } selected) { RenderDetail(selected); return; }
        var products = (_demoEnabled ? _catalog.Products.Concat(new[] { _demoProduct }) : _catalog.Products).Where(p => (p.Name + " " + p.Describe(_strings.Language)).Contains(_search.Text, StringComparison.CurrentCultureIgnoreCase)).Where(p => !_library || (IsDemo(p) ? _demoInstalled : Installed(p) is not null)).ToList();
        if (_page == "today" && _search.Text.Length == 0 && products.FirstOrDefault(p => p.Id == "kzn-notch") is { } feature) _products.Children.Add(Feature(feature));
        if (products.Count == 0) { _products.Children.Add(Label(_strings["Empty"], 15, true)); return; }
        var section = Label(_strings[_page == "today" ? "ForYou" : _library ? "Library" : "AllApps"], 21); section.FontWeight = FontWeights.Bold; section.Margin = new Thickness(0, 0, 0, 12); _products.Children.Add(section);
        var list = new StackPanel(); _products.Children.Add(Surface(list));
        foreach (var product in products)
        {
            var row = new Grid { Margin = new Thickness(0, 10, 0, 10) }; row.ColumnDefinitions.Add(new() { Width = new GridLength(74) }); row.ColumnDefinitions.Add(new()); row.ColumnDefinitions.Add(new() { Width = GridLength.Auto }); list.Children.Add(row);
            row.Children.Add(AppIcon(product, 60));
            var copy = new StackPanel { Margin = new Thickness(0, 0, 14, 0) }; Grid.SetColumn(copy, 1); row.Children.Add(copy);
            var name = Pill("ViewApp", () => { _selectedProduct = product; Build(); }); name.Content = product.Name; name.FontSize = 17; name.Padding = new Thickness(0); name.MinHeight = 22; name.Background = Brushes.Transparent; name.SetResourceReference(Control.ForegroundProperty, "TextBrush"); copy.Children.Add(name);
            copy.Children.Add(Label(product.Describe(_strings.Language), 12, true));
            if (IsDemo(product)) copy.Children.Add(Label(_strings["DemoBadge"], 11, true));
            if (IsDemo(product) || GitHubCommunityClient.ValidRepository(product.Repository)) copy.Children.Add(CommunityButton(product));
            var action = ProductActions(product); Grid.SetColumn(action, 2); action.VerticalAlignment = VerticalAlignment.Center; row.Children.Add(action);
            if (product != products[^1]) { var line = new Border { Height = 1, Margin = new Thickness(74, 2, 0, 2) }; line.SetResourceReference(Border.BackgroundProperty, "DividerBrush"); list.Children.Add(line); }
        }
    }
    private void RenderDetail(StoreProduct product)
    {
        var header = new Grid { Margin = new Thickness(0, 0, 0, 22) }; header.ColumnDefinitions.Add(new() { Width = new GridLength(108) }); header.ColumnDefinitions.Add(new()); header.Children.Add(AppIcon(product, 90));
        var title = new StackPanel(); Grid.SetColumn(title, 1); header.Children.Add(title); title.Children.Add(Label(product.Name, 27)); title.Children.Add(Label(product.Describe(_strings.Language), 14, true));
        var action = ProductActions(product); action.HorizontalAlignment = HorizontalAlignment.Left; action.Margin = new Thickness(0, 12, 0, 0); title.Children.Add(action); _products.Children.Add(header);
        var info = new UniformGrid { Columns = 3, Margin = new Thickness(0, 0, 0, 24) };
        foreach (var (label, value) in new[] { (_strings[IsDemo(product) ? "DemoStars" : "Stars"], Stars(product)), (_strings["Version"], product.Version), (_strings["Platform"], "Windows 11") })
        { var cell = new StackPanel { Margin = new Thickness(10) }; cell.Children.Add(Label(label.ToUpperInvariant(), 11, true)); var number = Label(value, 20); number.FontWeight = FontWeights.SemiBold; cell.Children.Add(number); info.Children.Add(cell); }
        _products.Children.Add(Surface(info, 18, 6)); var spacer = new Border { Height = 22 }; _products.Children.Add(spacer);
        if (product.Id == "kzn-notch") _products.Children.Add(Feature(product, true));
        var about = Label(_strings["AboutApp"], 22); about.FontWeight = FontWeights.Bold; _products.Children.Add(about);
        var description = Label(product.Describe(_strings.Language), 15); description.Margin = new Thickness(0, 10, 0, 12); _products.Children.Add(description);
        if (IsDemo(product))
        {
            _products.Children.Add(Label(_strings["DemoOnly"], 13, true));
            var hide = Pill("HideDemo", () => { _demoCancellation?.Cancel(); _demoEnabled = false; _selectedProduct = null; SavePreferences(); Build(); });
            hide.HorizontalAlignment = HorizontalAlignment.Left; hide.Margin = new Thickness(0, 16, 0, 16); _products.Children.Add(hide);
        }
        if (GitHubCommunityClient.ValidRepository(product.Repository))
        { _products.Children.Add(CommunityButton(product)); _products.Children.Add(Label(_strings["StarsHint"], 12, true)); var source = Pill("Source", () => OpenRepository(product)); source.HorizontalAlignment = HorizontalAlignment.Left; source.Margin = new Thickness(0, 12, 0, 12); _products.Children.Add(source); }
    }
    private async Task RefreshCommunityAsync(bool force = false)
    {
        if (_refreshingCommunity || _closed || !force && DateTimeOffset.Now - _lastCommunityRefresh < TimeSpan.FromMinutes(10)) return;
        _refreshingCommunity = true; RenderProducts();
        try
        {
            using var gate = new SemaphoreSlim(4);
            await Task.WhenAll(_catalog.Products.Select(p => p.Repository).Where(GitHubCommunityClient.ValidRepository).Distinct(StringComparer.OrdinalIgnoreCase).Select(async repository =>
            { await gate.WaitAsync(_lifetime.Token); try { _community[repository] = await _communityLoader(repository, _lifetime.Token); } finally { gate.Release(); } }));
            _lastCommunityRefresh = DateTimeOffset.Now;
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { Log.Error("Store GitHub community", ex); }
        finally { _refreshingCommunity = false; if (!_closed) RenderProducts(); }
    }
    private async Task CheckUpdatesAsync()
    {
        if (_checkingUpdates) return; _checkingUpdates = true; Build();
        try { await Task.WhenAll(RefreshCommunityAsync(true), _updateChecker(_strings.Language)); }
        catch (Exception ex) { _statusKey = "Error"; _error = ex.Message; }
        finally { _checkingUpdates = false; Build(); }
    }
}
