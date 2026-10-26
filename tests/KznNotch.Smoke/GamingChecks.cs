using System.IO;
using System.Windows;
using KznNotch.Core;
using KznNotch.Models;
using KznNotch.Services;
using KznNotch.ViewModels;
using KznNotch.Views;

namespace KznNotch.Smoke;

internal static class GamingChecks
{
    public static async Task RunAsync(string directory, Action<bool, string> check)
    {
        check(!GameModeService.MatchesGame("custom", "", false), "Spielmodus: unbekannte EXE wird nicht willkürlich als Spiel eingestuft");
        check(GameModeService.MatchesGame("game", @"D:\SteamLibrary\steamapps\common\Game\game.exe", false), "Spielmodus: Spiele aus Steam-Bibliotheken werden erkannt");
        check(!GameModeService.MatchesGame("chrome", "", true) && !GameModeService.MatchesGame("steamwebhelper", "", true), "Spielmodus: Browser und Launcher lösen keinen Spielmodus aus");
        check(!GameModeService.MatchesGame("editor", "", false) && GameModeService.MatchesGame("game", "", true), "Spielmodus: unbekannte normale App bleibt aktiv; Direct3D-Vollbild wird erkannt");
        bool? game = false;
        using var host = new AppHost(Path.Combine(directory, "gaming-data"), () => game, _ => { });
        host.Settings.Current.AnimationMilliseconds = 0;
        host.Settings.Current.GamingMode = GamingMode.Hidden;
        host.Windows.ShowNotches();
        NotchWindow[] Windows() => Application.Current.Windows.OfType<NotchWindow>().Where(x => x.DataContext is NotchViewModel vm && vm.Host == host).ToArray();
        game = true; host.Gaming.Refresh();
        check(Windows().Length > 0 && Windows().All(x => !x.IsVisible), "Spielmodus: erkannter Spielstart blendet die Notch vollständig aus");
        host.Events.Publish(new("Gaming-Test", "Hinweis", "", EventPriority.Reminder, TimeSpan.FromSeconds(10), "gaming"));
        host.Windows.Expand();
        check(Windows().All(x => !x.IsVisible), "Spielmodus: Hinweise und Öffnen umgehen den Aus-Modus nicht");
        host.Events.Dismiss();
        host.Settings.Current.GamingMode = GamingMode.Minimal; host.Settings.Save(host.Settings.Current);
        await Task.Delay(150);
        check(Windows().All(x => x.IsVisible && ((NotchViewModel)x.DataContext).GamingMinimal && x.Width == 160), "Spielmodus: minimalistische Uhr ersetzt die normale Notch");
        var window = Windows()[0];
        foreach (var corner in Enum.GetValues<GamingCorner>())
        {
            host.Settings.Current.GamingCorner = corner; window.SetGamingMinimal(false); window.SetGamingMinimal(true);
            var screen = System.Windows.Forms.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(window).Handle).Bounds;
            var position = window.PointToScreen(new Point(0, 0));
            var onRight = position.X > screen.Left + screen.Width / 2d;
            var onBottom = position.Y > screen.Top + screen.Height / 2d;
            check(onRight == (corner is GamingCorner.TopRight or GamingCorner.BottomRight) && onBottom == (corner is GamingCorner.BottomLeft or GamingCorner.BottomRight), "Spielmodus: Position " + corner);
        }
        window.Expand();
        check(((NotchViewModel)window.DataContext).State == NotchState.Compact, "Spielmodus: minimale Uhr öffnet kein Hover- oder Klickmenü");
        game = null; host.Gaming.Refresh(); check(host.Gaming.IsGaming, "Spielmodus: eigene Einstellungen verursachen kein Flackern");
        game = false; host.Gaming.Refresh(); check(host.Gaming.IsGaming, "Spielmodus: kurze Fokuswechsel werden entprellt"); host.Gaming.Refresh();
        check(!host.Gaming.IsGaming && Windows().All(x => x.IsVisible && !((NotchViewModel)x.DataContext).GamingMinimal), "Spielmodus: nach Verlassen des Spiels wird der Normalzustand wiederhergestellt");
        host.Settings.Current.GamingMode = GamingMode.Active; host.Settings.Save(host.Settings.Current); game = true; host.Gaming.Refresh();
        check(host.Gaming.EffectiveMode == GamingMode.Active && Windows().All(x => x.IsVisible), "Spielmodus: Aktiv lässt die Notch im Normalbetrieb");
    }
}
