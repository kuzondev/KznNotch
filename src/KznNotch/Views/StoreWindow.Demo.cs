using System.Windows.Controls;
using System.Windows.Automation;
using KznNotch.Store;

namespace KznNotch.Views;

public sealed partial class StoreWindow
{
    // Opt-in local preview only. Imported catalogs cannot create simulated products.
    private readonly StoreProduct _demoProduct = new()
    {
        Id = "local-kzn-test", Name = "KZN Test", Version = "1.0.0.0", PackageName = "Kzn.LocalPreview", Publisher = "Kzn",
        Description = new() { ["de"] = "Dein Testplatz für den Kzn Store. Probiere den Installationsablauf aus.", ["en"] = "Your Kzn Store playground. Try the installation flow." }
    };
    private readonly int _demoStars = Random.Shared.Next(80, 8000);
    private bool _demoEnabled, _demoInstalled, _demoInstalling;
    private int _demoProgress;
    private CancellationTokenSource? _demoCancellation;
    private bool IsDemo(StoreProduct product) => ReferenceEquals(product, _demoProduct);

    private void ButtonIcon(Button button, string label, string glyph)
    {
        var content = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
        content.Children.Add(new TextBlock { Text = glyph, FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"), FontSize = 13, Margin = new Thickness(0, 0, 7, 0), VerticalAlignment = VerticalAlignment.Center });
        content.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center });
        button.Content = content; AutomationProperties.SetName(button, label);
    }
    private StackPanel ProductActions(StoreProduct product)
    {
        var actions = new StackPanel { MinWidth = 120, MaxWidth = 180 };
        Button primary;
        if (IsDemo(product))
        {
            var key = _demoInstalling ? "DemoInstalling" : _demoInstalled ? "DemoOpen" : "Install";
            primary = Pill(key, () => { if (_demoInstalled) { _statusKey = "DemoComplete"; _status.Text = _strings[_statusKey]; } else _ = SimulateDemoAsync(); }, !_demoInstalled);
            primary.IsEnabled = !_demoInstalling;
            AutomationProperties.SetAutomationId(primary, "DemoInstall");
            ButtonIcon(primary, _demoInstalling ? $"{_strings[key]} {_demoProgress}%" : _strings[key], _demoInstalled ? "\uE768" : "\uE896");
        }
        else
        {
            primary = ProductAction(product);
            if (primary.Content as string == _strings["Install"] || primary.Content as string == _strings["Update"])
                ButtonIcon(primary, primary.Content.ToString()!, "\uE896");
        }
        actions.Children.Add(primary);
        if (IsDemo(product) || GitHubCommunityClient.ValidRepository(product.Repository))
        {
            var source = Pill("GitHub", () => OpenRepository(product)); source.Margin = new Thickness(0, 5, 0, 0); source.FontSize = 12;
            source.Background = Brushes.Transparent; source.ToolTip = _strings[IsDemo(product) ? "DemoSource" : "Source"];
            ButtonIcon(source, "GitHub", "\uE943");
            if (IsDemo(product)) AutomationProperties.SetAutomationId(source, "DemoSource");
            actions.Children.Add(source);
        }
        if (IsDemo(product) && _demoInstalling)
        {
            actions.Children.Add(new ProgressBar { Value = _demoProgress, Minimum = 0, Maximum = 100, Height = 4, Margin = new Thickness(4, 4, 4, 6) });
            var cancel = Pill("Cancel", () => _demoCancellation?.Cancel()); cancel.FontSize = 11; cancel.MinHeight = 26; actions.Children.Add(cancel);
        }
        if (IsDemo(product) && _demoInstalled)
        {
            var reset = Pill("DemoReset", () => { _demoInstalled = false; _demoProgress = 0; _statusKey = "DemoOnly"; Build(); });
            reset.FontSize = 11; reset.MinHeight = 26; reset.Background = Brushes.Transparent;
            AutomationProperties.SetAutomationId(reset, "DemoReset"); actions.Children.Add(reset);
        }
        return actions;
    }
    private async Task SimulateDemoAsync()
    {
        if (_demoInstalling || _demoInstalled) return;
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        _demoCancellation = cancellation; _demoInstalling = true; _demoProgress = 0; _statusKey = "DemoOnly";
        _status.Text = _strings[_statusKey]; RenderProducts();
        try
        {
            for (int step = 1; step <= 20; step++)
            {
                await Task.Delay(130, cancellation.Token);
                _demoProgress = step * 5; if (!_closed) RenderProducts();
            }
            _demoInstalled = true; _statusKey = "DemoComplete";
        }
        catch (OperationCanceledException) { _demoProgress = 0; _statusKey = "Cancelled"; }
        finally
        {
            _demoInstalling = false; _demoCancellation = null;
            if (!_closed) { _status.Text = _strings[_statusKey]; RenderProducts(); }
        }
    }
    private void OpenDemoSource()
    {
        const string url = "https://github.com/kuzondev/KznNotch/blob/main/src/KznNotch/Views/StoreWindow.Demo.cs";
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch (Exception ex) { _status.Text = _strings["Error"] + ex.Message; }
    }
}
