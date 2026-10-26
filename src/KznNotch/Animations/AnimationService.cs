using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace KznNotch.Animations;

public sealed class AnimationService(Services.SettingsService settings)
{
    public TimeSpan Duration => SystemParameters.ClientAreaAnimation && !SystemParameters.HighContrast ? TimeSpan.FromMilliseconds(settings.Current.AnimationMilliseconds) : TimeSpan.Zero;
    public void Animate(Animatable target, DependencyProperty property, double to, bool spring = false)
    {
        var animation = new DoubleAnimation(to, new Duration(Duration)) { EasingFunction = spring ? new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = .1 } : new CubicEase { EasingMode = EasingMode.EaseOut } };
        target.BeginAnimation(property, animation, HandoffBehavior.SnapshotAndReplace);
    }
    public void Size(FrameworkElement element, double width, double height)
    {
        SizeAxis(element, FrameworkElement.WidthProperty, width);
        SizeAxis(element, FrameworkElement.HeightProperty, height);
    }
    private void SizeAxis(FrameworkElement element, DependencyProperty property, double value)
    {
        var presented = (double)element.GetValue(property);
        // Repeated service updates must not restart an already-settling shell.
        if (element.GetAnimationBaseValue(property) is double target && Math.Abs(target - value) < .1) return;
        if (!double.IsFinite(presented)) presented = value;
        element.BeginAnimation(property, null);
        element.SetValue(property, value);
        if (Duration == TimeSpan.Zero) return;
        element.BeginAnimation(property, new DoubleAnimation(presented, value, new Duration(Duration)) { EasingFunction = new IslandSpringEase { EasingMode = EasingMode.EaseIn }, FillBehavior = FillBehavior.Stop });
    }
    public void Reveal(UIElement element, double horizontalOffset = 0)
    {
        if (Duration == TimeSpan.Zero) { element.BeginAnimation(UIElement.OpacityProperty, null); element.Opacity = 1; element.RenderTransform = Transform.Identity; element.Effect = null; return; }
        element.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, new Duration(Duration)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
        var translate = new TranslateTransform();
        var scale = new ScaleTransform(horizontalOffset == 0 ? .97 : .99, .97);
        element.RenderTransformOrigin = new Point(.5, 0);
        var transforms = new TransformGroup(); transforms.Children.Add(scale); transforms.Children.Add(translate); element.RenderTransform = transforms;
        foreach (var property in new[] { ScaleTransform.ScaleXProperty, ScaleTransform.ScaleYProperty })
            scale.BeginAnimation(property, new DoubleAnimation(1, new Duration(Duration)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
        translate.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(horizontalOffset == 0 ? 4 : 0, 0, new Duration(Duration)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
        if (horizontalOffset != 0) translate.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(horizontalOffset, 0, new Duration(Duration)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
        if (settings.Current.Blur && Duration > TimeSpan.Zero)
        {
            var blur = new BlurEffect { Radius = 3, RenderingBias = RenderingBias.Performance }; element.Effect = blur;
            var animation = new DoubleAnimation(3, 0, new Duration(Duration));
            animation.Completed += (_, _) => { if (ReferenceEquals(element.Effect, blur)) element.Effect = null; };
            blur.BeginAnimation(BlurEffect.RadiusProperty, animation);
        }
        else element.Effect = null;
    }
    public void Fade(UIElement element, double opacity)
    {
        var current = element.Opacity;
        element.BeginAnimation(UIElement.OpacityProperty, null); element.Opacity = opacity;
        if (Duration > TimeSpan.Zero)
            element.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(current, opacity, new Duration(TimeSpan.FromMilliseconds(Math.Min(160, Duration.TotalMilliseconds)))) { FillBehavior = FillBehavior.Stop });
    }
}
