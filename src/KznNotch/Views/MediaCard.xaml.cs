using System.Windows.Controls;
using KznNotch.Services;

namespace KznNotch.Views;
public partial class MediaCard : UserControl
{
    private INotifyPropertyChanged? _observed;
    public MediaCard()
    {
        InitializeComponent();
        Loaded += (_, _) => Observe();
        Unloaded += (_, _) => { if (_observed is not null) _observed.PropertyChanged -= TrackChanged; _observed = null; };
        DataContextChanged += (_, _) => { if (IsLoaded) Observe(); };
    }
    private void Observe()
    {
        if (_observed is not null) _observed.PropertyChanged -= TrackChanged;
        _observed = DataContext as INotifyPropertyChanged;
        if (_observed is not null) _observed.PropertyChanged += TrackChanged;
    }
    private void TrackChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MediaService.Title) || !IsVisible) return;
        if (Window.GetWindow(this)?.DataContext is ViewModels.NotchViewModel vm)
        { vm.Host.Animation.Reveal(Album); vm.Host.Animation.Reveal(TrackInfo, 8); }
    }
    private async void Seek_Click(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MediaService media && sender is FrameworkElement element && element.ActualWidth > 0) await media.SeekAsync(e.GetPosition(element).X / element.ActualWidth);
        e.Handled = true;
    }
}
