using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;

namespace KznNotch.Animations;

public static class InteractionMotion
{
    public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
        "Enabled", typeof(bool), typeof(InteractionMotion), new PropertyMetadata(false, Changed));
    public static void SetEnabled(DependencyObject target, bool value) => target.SetValue(EnabledProperty, value);
    public static bool GetEnabled(DependencyObject target) => (bool)target.GetValue(EnabledProperty);
    private static void Changed(DependencyObject target, DependencyPropertyChangedEventArgs args)
    {
        if (target is not FrameworkElement element) return;
        if ((bool)args.NewValue)
        {
            element.RenderTransformOrigin = new(.5, .5);
            element.RenderTransform = new ScaleTransform(1, 1);
            element.MouseEnter += Enter; element.MouseLeave += Leave;
            element.PreviewMouseLeftButtonDown += Press; element.PreviewMouseLeftButtonUp += Release;
            element.IsEnabledChanged += EnabledChanged; element.LostMouseCapture += LostCapture;
        }
        else
        {
            element.MouseEnter -= Enter; element.MouseLeave -= Leave;
            element.PreviewMouseLeftButtonDown -= Press; element.PreviewMouseLeftButtonUp -= Release;
            element.IsEnabledChanged -= EnabledChanged; element.LostMouseCapture -= LostCapture;
            element.RenderTransform = Transform.Identity;
        }
    }
    private static void Enter(object sender, MouseEventArgs args) => Scale((FrameworkElement)sender, 1.035);
    private static void Leave(object sender, MouseEventArgs args) => Scale((FrameworkElement)sender, 1);
    private static void Press(object sender, MouseButtonEventArgs args) => Scale((FrameworkElement)sender, .94);
    private static void Release(object sender, MouseButtonEventArgs args) => Scale((FrameworkElement)sender, ((FrameworkElement)sender).IsMouseOver ? 1.035 : 1);
    private static void EnabledChanged(object sender, DependencyPropertyChangedEventArgs args) => Scale((FrameworkElement)sender, 1);
    private static void LostCapture(object sender, MouseEventArgs args) => Scale((FrameworkElement)sender, 1);
    private static void Scale(FrameworkElement element, double value)
    {
        if (element.RenderTransform is not ScaleTransform scale) return;
        if (!element.IsEnabled || !SystemParameters.ClientAreaAnimation || SystemParameters.HighContrast) value = 1;
        var duration = SystemParameters.ClientAreaAnimation ? TimeSpan.FromMilliseconds(130) : TimeSpan.Zero;
        foreach (var property in new[] { ScaleTransform.ScaleXProperty, ScaleTransform.ScaleYProperty })
            scale.BeginAnimation(property, new DoubleAnimation(value, new Duration(duration))
            { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } }, HandoffBehavior.SnapshotAndReplace);
    }
}
