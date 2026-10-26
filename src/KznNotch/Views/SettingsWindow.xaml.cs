using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using KznNotch.ViewModels;

namespace KznNotch.Views;

public partial class SettingsWindow : Window
{
    private Point _dragStart;
    private WidgetChoice? _dragItem;
    private ListBox? _dragSource;
    private ListBox? _lastList;
    private SettingsViewModel ViewModel => (SettingsViewModel)DataContext;
    public SettingsWindow(AppHost host)
    {
        DataContext = new SettingsViewModel(host); InitializeComponent();
        ApplyAppearance();
        SourceInitialized += (_, _) => ApplyAppearance();
        ViewModel.PropertyChanged += AppearanceChanged;
        Microsoft.Win32.SystemEvents.UserPreferenceChanged += SystemAppearanceChanged;
        SettingsTabs.SelectionChanged += PaneChanged;
        SizeChanged += (_, _) => AdjustSidebar();
        Loaded += (_, _) => AdjustSidebar();
        Closed += (_, _) =>
        {
            ViewModel.PropertyChanged -= AppearanceChanged;
            Microsoft.Win32.SystemEvents.UserPreferenceChanged -= SystemAppearanceChanged;
            ViewModel.Dispose();
        };
    }
    private void ApplyAppearance() => Utilities.SettingsPalette.Apply(this, ViewModel.Edit.SettingsAppearance);
    private void Store_Click(object sender, RoutedEventArgs e) => ViewModel.Host.Windows.OpenStore();
    private async void Update_Click(object sender, RoutedEventArgs e) => await Services.GitHubUpdateService.CheckAndPromptAsync(true);
    private void StartupSettings_Click(object sender, RoutedEventArgs e)
        => Process.Start(new ProcessStartInfo("ms-settings:startupapps") { UseShellExecute = true });
    private void AdjustSidebar()
    {
        if (SettingsTabs.Template.FindName("SidebarColumn", SettingsTabs) is not ColumnDefinition column) return;
        column.Width = new GridLength(ActualWidth < 900 ? 176 : 210);
        SaveFooterContent.Margin = new Thickness(ActualWidth < 900 ? 204 : 238, 12, 28, 12);
        if (SettingsTabs.Template.FindName("SidebarSubtitle", SettingsTabs) is FrameworkElement subtitle)
            subtitle.Visibility = ActualWidth < 900 ? Visibility.Collapsed : Visibility.Visible;
        if (SettingsTabs.Template.FindName("SidebarFooter", SettingsTabs) is FrameworkElement footer)
            footer.Visibility = ActualWidth < 900 ? Visibility.Collapsed : Visibility.Visible;
    }
    private void AppearanceChanged(object? sender, PropertyChangedEventArgs e)
    { if (e.PropertyName == nameof(SettingsViewModel.AppearanceIndex)) ApplyAppearance(); }
    private void SystemAppearanceChanged(object sender, Microsoft.Win32.UserPreferenceChangedEventArgs e)
        => Dispatcher.BeginInvoke(new Action(ApplyAppearance));
    private void PaneChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.Source == SettingsTabs && SettingsTabs.SelectedContent is UIElement pane)
            ViewModel.Host.Animation.Reveal(pane, 6);
    }
    private void PickColor_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: AppearanceColorOption option }) return;
        using var dialog = new System.Windows.Forms.ColorDialog { FullOpen = true };
        if (option.IsValid)
        {
            var color = (Color)ColorConverter.ConvertFromString(option.Value);
            dialog.Color = System.Drawing.Color.FromArgb(color.R, color.G, color.B);
        }
        var owner = new System.Windows.Forms.NativeWindow();
        owner.AssignHandle(new System.Windows.Interop.WindowInteropHelper(this).Handle);
        try { if (dialog.ShowDialog(owner) == System.Windows.Forms.DialogResult.OK) option.Value = $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}"; }
        finally { owner.ReleaseHandle(); }
    }
    private void Layout_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(this); _dragSource = sender as ListBox; _lastList = _dragSource;
        _dragItem = FindRow(e.OriginalSource as DependencyObject)?.DataContext as WidgetChoice;
        if (FindAncestor<ButtonBase>(e.OriginalSource as DependencyObject) is not null) _dragItem = null;
    }
    public void SelectIntegrations() => IntegrationsTab.IsSelected = true;
    public void SelectDesign() => DesignTab.IsSelected = true;
    public void SelectPlugins() => PluginsTab.IsSelected = true;
    private void ImportPlugin_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "Kzn-Code (*.kzn)|*.kzn", CheckFileExists = true };
        if (dialog.ShowDialog(this) == true) ViewModel.Host.Plugins.Import(dialog.FileName);
    }
    private void ExportPlugin_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "Kzn-Code (*.kzn)|*.kzn", DefaultExt = ".kzn", AddExtension = true, FileName = "MeinPlugin.kzn" };
        if (dialog.ShowDialog(this) == true) ViewModel.Host.Plugins.Export(dialog.FileName);
    }
    private void PluginImage_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "Bilder|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.ico", CheckFileExists = true };
        if (dialog.ShowDialog(this) != true) return;
        if ((sender as FrameworkElement)?.Tag as string == "replace") ViewModel.Host.Plugins.NewImage(dialog.FileName);
        else ViewModel.Host.Plugins.AddImage(dialog.FileName);
    }
    private void Layout_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _dragItem is null || _dragSource is null) return;
        var delta = e.GetPosition(this) - _dragStart;
        if (Math.Abs(delta.Y) < SystemParameters.MinimumVerticalDragDistance && Math.Abs(delta.X) < SystemParameters.MinimumHorizontalDragDistance) return;
        DragDrop.DoDragDrop(_dragSource, new DataObject(typeof(WidgetChoice), _dragItem), DragDropEffects.Move); _dragItem = null;
    }
    private void Layout_Drop(object sender, DragEventArgs e)
    {
        if (sender is not ListBox list || list != _dragSource || e.Data.GetData(typeof(WidgetChoice)) is not WidgetChoice item || list.ItemsSource is not ObservableCollection<WidgetChoice> items) return;
        var target = FindRow(e.OriginalSource as DependencyObject)?.DataContext as WidgetChoice;
        var index = target is null ? items.Count - 1 : items.IndexOf(target);
        if (index >= 0) items.Move(items.IndexOf(item), index);
        e.Handled = true;
    }
    private void Layout_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = sender == _dragSource && e.Data.GetDataPresent(typeof(WidgetChoice)) ? DragDropEffects.Move : DragDropEffects.None;
        if (sender is ListBox list && FindRow(e.OriginalSource as DependencyObject) is { } row) list.SelectedItem = row.DataContext;
        e.Handled = true;
    }
    private static ListBoxItem? FindRow(DependencyObject? source) => FindAncestor<ListBoxItem>(source);
    private static T? FindAncestor<T>(DependencyObject? source) where T : DependencyObject
    {
        while (source is not null) { if (source is T result) return result; source = source is Visual ? VisualTreeHelper.GetParent(source) : (source as FrameworkContentElement)?.Parent; }
        return null;
    }
    private void Move(int direction)
    {
        if (_lastList?.SelectedItem is not WidgetChoice item || _lastList.ItemsSource is not ObservableCollection<WidgetChoice> items) return;
        var from = items.IndexOf(item); var to = Math.Clamp(from + direction, 0, items.Count - 1); items.Move(from, to);
    }
    private void MoveUp_Click(object sender, RoutedEventArgs e) => Move(-1);
    private void MoveDown_Click(object sender, RoutedEventArgs e) => Move(1);
    private static void Open(string uri) { try { Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true }); } catch (Exception ex) { Log.Error("Open application", ex); } }
    private void Spotify_Click(object sender, RoutedEventArgs e) => Open("spotify:");
    private void Discord_Click(object sender, RoutedEventArgs e) => Open("discord:");
    private void Data_Click(object sender, RoutedEventArgs e) => Open(Log.DirectoryPath);
}
