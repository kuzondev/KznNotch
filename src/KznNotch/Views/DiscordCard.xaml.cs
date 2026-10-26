using System.Windows.Controls;
using System.Collections;

namespace KznNotch.Views;

public partial class DiscordCard : UserControl
{
    public static readonly DependencyProperty NotificationItemsProperty = DependencyProperty.Register(nameof(NotificationItems), typeof(IEnumerable), typeof(DiscordCard));
    public IEnumerable? NotificationItems { get => (IEnumerable?)GetValue(NotificationItemsProperty); set => SetValue(NotificationItemsProperty, value); }
    public DiscordCard() => InitializeComponent();
}
