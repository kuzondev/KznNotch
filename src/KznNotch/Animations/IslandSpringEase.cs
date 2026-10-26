using System.Windows.Media.Animation;

namespace KznNotch.Animations;

// A short, heavily damped response: approximately 1.2% overshoot, no idle loop.
// Written for this WPF shell; no reference-project implementation is used.
public sealed class IslandSpringEase : EasingFunctionBase
{
    protected override double EaseInCore(double time)
    {
        static double Response(double t) => 1 - Math.Exp(-7 * t) * (Math.Cos(5 * t) + 1.4 * Math.Sin(5 * t));
        return Response(time) / Response(1);
    }
    protected override Freezable CreateInstanceCore() => new IslandSpringEase();
}
