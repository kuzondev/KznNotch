namespace KznNotch.Views;

// Stylized playback state, not an audio-level measurement. Runs only while visible.
public sealed class PlaybackIndicator : FrameworkElement
{
    public static readonly DependencyProperty IsPlayingProperty = DependencyProperty.Register(nameof(IsPlaying), typeof(bool), typeof(PlaybackIndicator), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender, Changed));
    public bool IsPlaying { get => (bool)GetValue(IsPlayingProperty); set => SetValue(IsPlayingProperty, value); }
    public static readonly DependencyProperty MotionMillisecondsProperty = DependencyProperty.Register(nameof(MotionMilliseconds), typeof(double), typeof(PlaybackIndicator), new PropertyMetadata(260d, Changed));
    public double MotionMilliseconds { get => (double)GetValue(MotionMillisecondsProperty); set => SetValue(MotionMillisecondsProperty, value); }
    private readonly DispatcherTimer _tick = new() { Interval = TimeSpan.FromMilliseconds(40) };
    private readonly System.Diagnostics.Stopwatch _time = new();
    public PlaybackIndicator()
    {
        Width = 22; Height = 22; IsHitTestVisible = false;
        _tick.Tick += (_, _) => InvalidateVisual();
        Loaded += (_, _) => { SystemParameters.StaticPropertyChanged += SystemChanged; Update(); };
        Unloaded += (_, _) => { SystemParameters.StaticPropertyChanged -= SystemChanged; _tick.Stop(); _time.Stop(); };
        IsVisibleChanged += (_, _) => Update();
    }
    private static void Changed(DependencyObject target, DependencyPropertyChangedEventArgs e) => ((PlaybackIndicator)target).Update();
    private void SystemChanged(object? sender, PropertyChangedEventArgs e) => Update();
    private void Update()
    {
        if (IsLoaded && IsVisible && IsPlaying && MotionMilliseconds > 0 && SystemParameters.ClientAreaAnimation && !SystemParameters.HighContrast)
        { _time.Start(); _tick.Start(); }
        else { _tick.Stop(); _time.Stop(); }
        InvalidateVisual();
    }
    protected override void OnRender(DrawingContext context)
    {
        var brush = SystemParameters.HighContrast ? SystemColors.WindowTextBrush : TryFindResource("AccentBrush") as Brush ?? Brushes.MediumPurple;
        for (var i = 0; i < 4; i++)
        {
            var height = IsPlaying ? 7 + 12 * (.5 + .5 * Math.Sin(_time.Elapsed.TotalSeconds * 5 + i * 1.7)) : 5;
            context.DrawRoundedRectangle(brush, null, new Rect(i * 5.5, (22 - height) / 2, 3, height), 1.5, 1.5);
        }
    }
}
