using System.Text.Json;
using System.IO;
using System.Windows;
using KznNotch.Core;
using KznNotch.Models;
using KznNotch.Services;
using KznNotch.Storage;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using KznNotch.Views;
using KznNotch.ViewModels;

namespace KznNotch.Smoke;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        if (args.FirstOrDefault() == "--interactive-preview")
        {
            AppHost? previewHost = null;
            application.Startup += (_, _) =>
            {
                application.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/KznNotch;component/Views/Theme.xaml") });
                application.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/KznNotch;component/Views/WidgetTemplates.xaml") });
                previewHost = new AppHost(Path.GetFullPath(args[1]));
                previewHost.Settings.Current.Notifications = false;
                previewHost.Settings.Current.ClickThroughCompact = false;
                var notch = new NotchWindow(previewHost, System.Windows.Forms.Screen.PrimaryScreen!) { Title = "Kzn Notch · Klicktest", ShowInTaskbar = true };
                notch.Closed += (_, _) => application.Shutdown();
                notch.Show(); notch.Expand();
                new SettingsWindow(previewHost) { Title = "Kzn Notch · Designvorschau" }.Show();
            };
            application.Exit += (_, _) => previewHost?.Dispose();
            return application.Run();
        }
        var exitCode = 1;
        application.Startup += async (_, _) =>
        {
            try { await RunAsync(args.Length > 0 ? args[0] : Path.Combine(Path.GetTempPath(), "KznNotch-Smoke", Guid.NewGuid().ToString("N"))); exitCode = 0; }
            catch (Exception ex) { Console.Error.WriteLine(ex); }
            finally { application.Shutdown(exitCode); }
        };
        application.Run(); return exitCode;
    }
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException("FAIL: " + name);
        Console.WriteLine("PASS: " + name);
    }
    private static async Task RunAsync(string directory)
    {
        Directory.CreateDirectory(directory);
        var store = new JsonStore(directory);
        store.Save("settings.json", new AppSettings { Notifications = false, BluetoothEnabled = false });
        var settings = new SettingsService(store);
        KznPluginChecks.Run(directory, Check); KznPluginChecks.Games(directory, Check);
        await UpdateChecks.RunAsync(Check);
        var customColors = new AppSettings { BarColor = "#12ABCD", SecondaryColor = "#ABCDEF", NavigationColor = "#102030", OpenHeight = 420, BarThickness = 7 };
        store.Save("custom-colors.json", customColors);
        var colorsAgain = store.Load("custom-colors.json", () => new AppSettings()); colorsAgain.Normalize();
        Check(colorsAgain.BarColor == "#12ABCD" && colorsAgain.SecondaryColor == "#ABCDEF" && colorsAgain.NavigationColor == "#102030" && colorsAgain.OpenHeight == 420 && colorsAgain.BarThickness == 7, "Designfarben und Formwerte werden unabhängig gespeichert");
        var invalidColors = new AppSettings { BarColor = "invalid", OpenHeight = double.NaN, BarThickness = -5 }; invalidColors.Normalize();
        Check(invalidColors.BarColor == AppearanceColors.Defaults.BarColor && invalidColors.OpenHeight == 340 && invalidColors.BarThickness == 2, "Ungültige neue Designwerte werden sicher normalisiert");
        var fpsParser = new FpsCsvParser();
        fpsParser.TryRead("Application,ProcessID,SwapChainAddress,msBetweenPresents", 42, out _, out _);
        Check(fpsParser.TryRead("game.exe,42,0x1,16.6667", 42, out var frameMs, out var swapchain) && Math.Abs(1000 / frameMs - 60) < .1 && swapchain == "0x1", "FPS-Parser berechnet echte Bildrate aus PresentMon-Intervallen");
        Check(!fpsParser.TryRead("other.exe,43,0x1,8.3", 42, out _, out _) && !fpsParser.TryRead("game.exe,42,0x1,NaN", 42, out _, out _) && !fpsParser.TryRead("game.exe,42,0x1,0", 42, out _, out _), "FPS-Parser verwirft andere Prozesse sowie ungültige Framezeiten");
        await FpsHelperChecks.RunAsync(Check);
        store.Save("appearance-roundtrip.json", new AppSettings { SettingsAppearance = SettingsAppearanceMode.Dark });
        Check(store.Load("appearance-roundtrip.json", () => new AppSettings()).SettingsAppearance == SettingsAppearanceMode.Dark, "Dunkles Erscheinungsbild wird ohne Datenverlust gespeichert");
        AirPodsChecks.Run(Check);
        var malformed = new AppSettings { CompactWidth = double.NaN, ExpandedWidth = -1, Accent = "invalid" }; malformed.Normalize();
        Check(double.IsFinite(malformed.CompactWidth) && malformed.ExpandedWidth >= 480 && malformed.Accent == "#B9A3FF", "Beschädigte Größen und Farben werden normalisiert");
        File.WriteAllText(Path.Combine(directory, "corrupt.json"), "{invalid");
        Check(store.Load("corrupt.json", () => "recovered") == "recovered" && Directory.GetFiles(directory, "corrupt.json.corrupt-*").Length > 0, "Beschädigtes JSON wird gesichert und abgefangen");
        using var events = new EventService();
        events.Publish(new("Lautstärke", "20%", "", EventPriority.Volume, TimeSpan.FromSeconds(20), "volume"));
        events.Publish(new("Erinnerung", "Jetzt", "", EventPriority.Reminder, TimeSpan.FromSeconds(20), "reminder"));
        Check(events.Current?.Title == "Erinnerung", "Reminder unterbricht ein Ereignis niedrigerer Priorität");
        events.Dismiss(); Check(events.Current?.Title == "Lautstärke", "Unterbrochenes Ereignis wird fortgesetzt"); events.Dismiss();
        var todos = new TodoService(store) { NewTitle = "Persistente Aufgabe", NewPriority = 2, NewDue = DateTime.Today.AddDays(1) };
        todos.AddCommand.Execute(null); todos.Items[0].Completed = true;
        var restored = new TodoService(store);
        Check(restored.Items.Count == 1 && restored.Items[0].Completed && restored.Items[0].Priority == 2 && restored.Items[0].DueDate.HasValue, "Aufgabe mit Priorität, Datum und Status überlebt erneutes Laden");
        restored.DeleteCommand.Execute(restored.Items[0]); Check(new TodoService(store).Items.Count == 0, "Aufgabe wird dauerhaft gelöscht");
        var notifications = new NotificationService(settings);
        store.Save("reminders.json", new List<ReminderItem> { new() { Title = "Während des Schlafmodus fällig", DueAt = DateTimeOffset.Now.AddMinutes(-2) } });
        using var reminders = new ReminderService(store, events, notifications);
        Check(reminders.Items.Count == 0 && events.Current?.Title == "Während des Schlafmodus fällig", "Überfällige Erinnerung wird nach Neustart genau einmal verarbeitet"); events.Dismiss();
        using var secondReminders = new ReminderService(store, events, notifications);
        Check(!events.HasEvent, "Erledigte Erinnerung wird nach erneutem Start nicht wiederholt");
        using var timer = new TimerService(store, events, notifications);
        timer.Start(1, false); timer.ToggleCommand.Execute(null);
        using (var restoredTimer = new TimerService(store, events, notifications)) Check(!restoredTimer.Running && restoredTimer.Remaining.TotalSeconds > 0, "Pausierter Timer wird gespeichert");
        timer.Start(.02, false); await Task.Delay(2700);
        Check(!timer.Running && timer.Remaining == TimeSpan.Zero && events.Current?.Priority == EventPriority.Timer, "Timer endet mit priorisiertem Ereignis"); events.Dismiss();
        timer.Start(25, true); timer.ResetCommand.Execute(null); Check(timer.Pomodoro && !timer.Running && timer.Remaining.TotalMinutes == 25, "Pomodoro startet mit 25 Minuten und lässt sich zurücksetzen");
        using var battery = new BatteryService();
        Check(battery.Percent is null || battery.Percent is >= 0 and <= 100, "Windows-Batterieabfrage liefert einen gültigen oder fehlenden Wert");
        using var media = new MediaService(events); await media.InitializeAsync();
        Check(string.IsNullOrEmpty(media.Status), "Windows Global Media Sessions lassen sich initialisieren");
        using var stats = new SystemStatsService(); stats.SetActive(true); await Task.Delay(1800); stats.SetActive(false);
        Check(stats.RamTotal > 0 && stats.RamUsed <= stats.RamTotal && double.IsFinite(stats.Cpu) && stats.Cpu is >= 0 and <= 100, "Echte Windows-CPU- und RAM-Abfrage");
        await DiscordChecks.RunAsync(directory, Check);
        await VerifyWindowsAsync(directory);
        await GamingChecks.RunAsync(directory, Check);
        var report = new { Timestamp = DateTimeOffset.Now, Checks = "passed", BatteryPresent = battery.HasBattery, BatteryPercent = battery.Percent, MediaSession = media.Available, MediaSource = media.Source, CpuPercent = stats.Cpu, RamUsedGb = stats.RamUsed, RamTotalGb = stats.RamTotal, GpuPercent = stats.Gpu };
        File.WriteAllText(Path.Combine(directory, "report.json"), JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine(JsonSerializer.Serialize(report));
    }
    private static async Task VerifyWindowsAsync(string directory)
    {
        var resources = Application.Current.Resources.MergedDictionaries;
        resources.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/KznNotch;component/Views/Theme.xaml") });
        resources.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/KznNotch;component/Views/WidgetTemplates.xaml") });
        await DiscordChecks.VerifyCardAsync(directory, Check);
        using var host = new AppHost(Path.Combine(directory, "ui-data"));
        host.Settings.Current.Notifications = false; host.Settings.Current.BluetoothEnabled = false;
        host.Settings.Current.AnimationMilliseconds = 0;
        host.Settings.Current.Widgets.First(x => x.Id == "fps").Enabled = true; host.Widgets.Rebuild();
        var window = new NotchWindow(host, System.Windows.Forms.Screen.PrimaryScreen!) { IsHitTestVisible = false };
        try
        {
            window.Show(); window.Expand(); await Task.Delay(1600);
            var shell = (FrameworkElement)window.FindName("Shell");
            Check(Math.Abs(shell.ActualWidth - host.Settings.Current.HoverWidth) < 2 && Math.Abs(shell.ActualHeight - 340) < 2, "Musik öffnet sich als einzelne kompakte Funktion");
            var vm = (NotchViewModel)window.DataContext;
            Check(((System.Windows.Controls.Border)shell).CornerRadius.TopLeft == 0, "Notch schließt bündig am Bildschirmrand ab");
            Check(Math.Abs(shell.PointToScreen(new Point(0, 0)).Y - System.Windows.Forms.Screen.PrimaryScreen!.Bounds.Top) <= 1,
                "Sichtbare Notch liegt nach dem Laden direkt am oberen Monitorrand");
            Check(!window.ShowInTaskbar && window.Topmost && window.AllowsTransparency, "Overlay ist transparent, oben und ohne Taskbar-Eintrag");
            SaveImage(shell, Path.Combine(directory, "music.png"));
            var picker = (System.Windows.Controls.ItemsControl)window.FindName("FunctionPicker");
            foreach (var choice in ((NotchViewModel)window.DataContext).VisibleWidgets.ToArray())
            {
                var presenter = (System.Windows.Controls.ContentPresenter)picker.ItemContainerGenerator.ContainerFromItem(choice);
                var button = (System.Windows.Controls.Button)VisualTreeHelper.GetChild(presenter, 0);
                ((NotchViewModel)window.DataContext).SetState(NotchState.Hover);
                button.RaiseEvent(new System.Windows.Input.MouseButtonEventArgs(System.Windows.Input.Mouse.PrimaryDevice, 0, System.Windows.Input.MouseButton.Left)
                { RoutedEvent = System.Windows.Input.Mouse.PreviewMouseDownEvent });
                Check(((NotchViewModel)window.DataContext).IsExpanded, $"Notch-Schaltfläche {choice.Name} hält die Hover-Ansicht bei Interaktion offen");
                var peer = new System.Windows.Automation.Peers.ButtonAutomationPeer(button);
                ((System.Windows.Automation.Provider.IInvokeProvider)peer.GetPattern(System.Windows.Automation.Peers.PatternInterface.Invoke)).Invoke();
                await Task.Delay(80);
                SaveImage(shell, Path.Combine(directory, $"widget-{choice.Widget.Id}.png"));
                Check(Math.Abs(shell.ActualHeight - 340) < 2 && Math.Abs(shell.ActualWidth - host.Settings.Current.HoverWidth) < 2, $"{choice.Name}: einheitliche Notch-Größe");
                Check(((NotchViewModel)window.DataContext).SelectedWidget == choice && choice.IsSelected,
                    $"Notch-Schaltfläche {choice.Name} führt ihren gebundenen Befehl aus");
            }
            ((NotchViewModel)window.DataContext).SelectedWidget = ((NotchViewModel)window.DataContext).VisibleWidgets.First(x => x.Widget.Id == "media");
            window.Collapse(); await Task.Delay(400);
            Check(shell.ActualHeight >= 31 && shell.ActualHeight <= host.Settings.Current.CompactHeight + 1, "Closed ist der kompakte Normalzustand");
            SaveImage(shell, Path.Combine(directory, "compact.png"));
            host.Plugins.Import(Path.Combine(directory, "plugin-checks", "source", "image.kzn")); host.Plugins.Save(true);
            await Task.Delay(100);
            var pluginSurface = (KznSurface)window.FindName("PluginSurface");
            Check(vm.ShowPluginCompact && !vm.ShowDefaultCompact && pluginSurface.Content is System.Windows.Controls.StackPanel imagePanel && imagePanel.Children.OfType<System.Windows.Controls.Image>().Any(x => x.Source is not null), "Aktiviertes Bild-Plugin ersetzt die echte Uhranzeige und rendert sein Bild");
            SaveImage(shell, Path.Combine(directory, "plugin-image.png"));
            host.Plugins.New("clock"); host.Plugins.Source = "kzn 1\nplugin \"Schrift-Test\"\nfont \"Consolas\" 16\nclock \"HH:mm\"\n"; host.Plugins.Save(true); await Task.Delay(80);
            Check(pluginSurface.Content is System.Windows.Controls.StackPanel fontPanel && fontPanel.Children.OfType<System.Windows.Controls.TextBlock>().Any(x => x.FontFamily.Source == "Consolas"), ".kzn-Schriftart wirkt im laufenden Notch-Renderer");
            SaveImage(shell, Path.Combine(directory, "plugin-font.png"));
            host.Plugins.New("focus"); host.Plugins.Save(true); await Task.Delay(80);
            var pluginButton = ((System.Windows.Controls.StackPanel)pluginSurface.Content).Children.OfType<System.Windows.Controls.Button>().Single();
            pluginButton.RaiseEvent(new System.Windows.Input.MouseButtonEventArgs(System.Windows.Input.Mouse.PrimaryDevice, 0, System.Windows.Input.MouseButton.Left) { RoutedEvent = System.Windows.Input.Mouse.PreviewMouseDownEvent });
            Check(vm.IsExpanded && vm.SelectedWidget?.Widget.Id == "timer", ".kzn-Button öffnet die gewünschte Funktion aus der geschlossenen Notch");
            host.Plugins.DisableCommand.Execute(null); window.Collapse();
            vm.SelectedWidget = vm.VisibleWidgets.First(x => x.Widget.Id == "timer");
            vm.SetState(NotchState.Hover); await Task.Delay(150);
            Check(vm.SelectedWidget.Widget.Id == "timer" && shell.ActualHeight <= 340, "Hover zeigt ausschließlich die zuletzt gewählte Funktion");
            SaveImage(shell, Path.Combine(directory, "timer.png"));
            vm.NextWidgetCommand.Execute(null); vm.PreviousWidgetCommand.Execute(null);
            Check(vm.SelectedWidget.Widget.Id == "timer" && new SettingsService(new JsonStore(Path.Combine(directory, "ui-data"))).Current.LastSelectedWidgetId == "timer", "Funktionswechsel und persistierte Auswahl funktionieren");
            host.Settings.Current.Presentation = NotchPresentation.DynamicIsland; window.Collapse(); await Task.Delay(150);
            Check(((System.Windows.Controls.Border)shell).CornerRadius.TopLeft > 0, "Dynamic Island ist auch oben abgerundet");
            SaveImage(shell, Path.Combine(directory, "island.png"));
            var settings = new SettingsWindow(host);
            try { settings.Show(); await Task.Delay(200); Check(settings.IsVisible, "Einstellungen und benutzerdefinierte Steuerelemente werden geladen"); SaveImage((FrameworkElement)settings.Content, Path.Combine(directory, "settings.png"));
                ((System.Windows.Controls.TabItem)settings.FindName("GamingTab")).IsSelected = true; await Task.Delay(100);
                SaveImage((FrameworkElement)settings.Content, Path.Combine(directory, "gaming-settings.png"));
                var tabs = (System.Windows.Controls.TabControl)settings.FindName("SettingsTabs");
                foreach (System.Windows.Controls.TabItem tab in tabs.Items)
                {
                    tab.IsSelected = true; await Task.Delay(80);
                    SaveImage((FrameworkElement)settings.Content, Path.Combine(directory, $"settings-{tabs.Items.IndexOf(tab)}.png"));
                }
                settings.Width = settings.MinWidth; settings.Height = settings.MinHeight;
                tabs.SelectedIndex = 0; await Task.Delay(80);
                SaveImage((FrameworkElement)settings.Content, Path.Combine(directory, "settings-compact.png"));
                Check(((FrameworkElement)tabs.SelectedContent).ActualWidth > 450, "Einstellungen behalten bei minimaler Fensterbreite nutzbare Inhaltsfläche");
                var settingsVm = (SettingsViewModel)settings.DataContext;
                var savedBarColor = host.Settings.Current.BarColor;
                settingsVm.Colors.First(x => x.Key == "BarColor").Value = "#12ABCD";
                Check(((SolidColorBrush)Application.Current.Resources["BarBrush"]).Color == (Color)ColorConverter.ConvertFromString("#12ABCD") && host.Settings.Current.BarColor == savedBarColor, "Farbauswahl wirkt live ohne ungespeicherte Werte zu persistieren");
                settingsVm.ApplyColorPresetCommand.Execute("ocean");
                Check(settingsVm.Colors.First(x => x.Key == "BarColor").Value == "#7DD3FC", "Design-Preset aktualisiert interaktive Farbfelder");
                settingsVm.ApplyColorPresetCommand.Execute("default");
                window.Expand(); settingsVm.PreviewOpenHeight = 400; await Task.Delay(80);
                Check(Math.Abs(shell.ActualHeight - 400) < 2 && host.Settings.Current.OpenHeight == 340, "Gemeinsame Tabhöhe lässt sich ohne Speichern live ändern");
                settingsVm.PreviewOpenHeight = 340; window.Collapse();
                foreach (var appearance in new[] { SettingsAppearanceMode.Light, SettingsAppearanceMode.Dark })
                {
                    settingsVm.AppearanceIndex = (int)appearance;
                    await Task.Delay(60);
                    var foreground = (SolidColorBrush)settings.FindResource("TextBrush");
                    var background = (SolidColorBrush)settings.FindResource("SurfaceBrush");
                    Check(Contrast(foreground.Color, background.Color) >= 7, $"{appearance}: Textkontrast mindestens 7:1");
                    settings.Width = 980; settings.Height = 760;
                    foreach (System.Windows.Controls.TabItem tab in tabs.Items)
                    {
                        tab.IsSelected = true; await Task.Delay(60);
                        SaveImage((FrameworkElement)settings.Content, Path.Combine(directory, $"{appearance.ToString().ToLowerInvariant()}-settings-{tabs.Items.IndexOf(tab)}.png"));
                    }
                }
            }
            finally { settings.Close(); }
            Check(host.Appearance == host.Settings.Current, "Schließen der Einstellungen beendet die Designvorschau");
            if (SystemParameters.ClientAreaAnimation)
            {
                host.Settings.Current.AnimationMilliseconds = 180;
                window.Collapse(); await Task.Delay(220); window.Expand(); await Task.Delay(60);
                Check(shell.ActualWidth > host.Settings.Current.CompactWidth && shell.ActualWidth < host.Settings.Current.HoverWidth,
                    "Öffnen animiert die Notch zwischen geschlossener und geöffneter Breite");
                SaveImage(shell, Path.Combine(directory, "spring-opening.png"));
                window.Collapse(); await Task.Delay(230);
                Check(shell.ActualHeight >= 31 && shell.ActualHeight <= host.Settings.Current.CompactHeight + 1,
                    "Unterbrochene Animation endet ohne Sprung im angeforderten Zustand");
                host.Settings.Current.AnimationMilliseconds = 0;
                window.Expand();
                Check(!shell.HasAnimatedProperties && Math.Abs(shell.Width - host.Settings.Current.HoverWidth) < 2, "0 ms entfernt Shell-Animationen vollständig");
            }
        }
        finally { window.Close(); }
    }
    private static void SaveImage(FrameworkElement element, string path)
    {
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(element.ActualWidth), (int)Math.Ceiling(element.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        var visual = new DrawingVisual();
        using (var drawing = visual.RenderOpen()) drawing.DrawRectangle(new VisualBrush(element), null, new Rect(0, 0, element.ActualWidth, element.ActualHeight));
        bitmap.Render(visual); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var output = File.Create(path); encoder.Save(output);
    }
    private static double Contrast(Color first, Color second)
    {
        static double Channel(byte value) { var c = value / 255d; return c <= .04045 ? c / 12.92 : Math.Pow((c + .055) / 1.055, 2.4); }
        static double Luminance(Color color) => .2126 * Channel(color.R) + .7152 * Channel(color.G) + .0722 * Channel(color.B);
        var a = Luminance(first); var b = Luminance(second);
        return (Math.Max(a, b) + .05) / (Math.Min(a, b) + .05);
    }
}
