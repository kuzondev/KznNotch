using Forms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace KznNotch.Services;

public sealed class TrayService : IDisposable
{
    private readonly Forms.NotifyIcon _icon;
    private readonly Forms.ContextMenuStrip _menu;
    public TrayService(AppHost host)
    {
        _menu = new();
        _menu.Items.Add("Kzn Notch öffnen", null, (_, _) => Ui.Run(host.Windows.Expand));
        _menu.Items.Add("Einstellungen…", null, (_, _) => Ui.Run(host.Windows.OpenSettings));
        _menu.Items.Add("Kzn Store…", null, (_, _) => Ui.Run(host.Windows.OpenStore));
        _menu.Items.Add("Ein- / ausblenden", null, (_, _) => Ui.Run(host.Windows.ToggleVisibility));
        var gaming = new Forms.ToolStripMenuItem("Spielmodus");
        foreach (var (label, mode) in new[] { ("Aktiv · Normalbetrieb", GamingMode.Active), ("Minimalistisch im Spiel", GamingMode.Minimal), ("Aus während des Gamings", GamingMode.Hidden) })
        {
            var item = new Forms.ToolStripMenuItem(label) { Checked = host.Settings.Current.GamingMode == mode };
            item.Click += (_, _) => Ui.Run(() => { host.Settings.Current.GamingMode = mode; host.Settings.Save(host.Settings.Current); });
            gaming.DropDownItems.Add(item);
        }
        gaming.DropDownOpening += (_, _) => { for (var i = 0; i < gaming.DropDownItems.Count; i++) ((Forms.ToolStripMenuItem)gaming.DropDownItems[i]).Checked = (int)host.Settings.Current.GamingMode == i; };
        _menu.Items.Add(gaming);
        _menu.Items.Add("Testbenachrichtigung", null, (_, _) => Ui.Run(() => host.Notifications.Show("Kzn Notch ist bereit", "Windows-Benachrichtigungen sind verbunden.")));
        _menu.Items.Add("Klickdurchlässigkeit ausschalten", null, (_, _) => Ui.Run(() => { host.Settings.Current.ClickThroughCompact = false; host.Settings.Save(host.Settings.Current); }));
        _menu.Items.Add(new Forms.ToolStripSeparator());
        _menu.Items.Add("Beenden", null, (_, _) => Ui.Run(() => Application.Current.Shutdown()));
        using var stream = Application.GetResourceStream(new Uri("pack://application:,,,/KznNotch;component/Assets/kzn-notch.ico"))?.Stream;
        _icon = new() { Text = "Kzn Notch", Icon = stream is null ? (Drawing.Icon)Drawing.SystemIcons.Application.Clone() : new Drawing.Icon(stream), ContextMenuStrip = _menu, Visible = true };
        _icon.DoubleClick += (_, _) => Ui.Run(host.Windows.Expand);
        host.Notifications.FallbackRequested += (title, body) => _icon.ShowBalloonTip(5000, title, body, Forms.ToolTipIcon.Info);
    }
    public void Dispose() { _icon.Visible = false; _icon.Icon?.Dispose(); _icon.Dispose(); _menu.Dispose(); }
}
