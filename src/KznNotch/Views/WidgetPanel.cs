using System.Windows.Controls;
using KznNotch.Widgets;

namespace KznNotch.Views;

// Measure against the real scroll viewport; fixed WrapPanel widths wrap incorrectly at some DPI/scrollbar sizes.
public sealed class WidgetPanel : Panel
{
    private readonly List<Rect> _positions = [];
    protected override Size MeasureOverride(Size availableSize)
    {
        _positions.Clear();
        var width = double.IsFinite(availableSize.Width) ? Math.Max(0, availableSize.Width) : 540;
        const double gap = 8;
        double x = 0, y = 0, rowHeight = 0;
        foreach (UIElement child in InternalChildren)
        {
            var entry = (child as FrameworkElement)?.DataContext as WidgetEntry;
            var wide = entry?.Widget.IsWide != false || width < 420;
            var itemWidth = wide ? width : Math.Max(0, (width - gap) / 2);
            if (x > 0 && (wide || x + itemWidth > width + .1)) { y += rowHeight + gap; x = 0; rowHeight = 0; }
            if (entry is not null) entry.Width = itemWidth;
            child.Measure(new Size(itemWidth, double.PositiveInfinity));
            var height = child.DesiredSize.Height;
            _positions.Add(new Rect(x, y, itemWidth, height));
            rowHeight = Math.Max(rowHeight, height);
            x += itemWidth + gap;
            if (wide) { y += rowHeight + gap; x = 0; rowHeight = 0; }
        }
        return new Size(width, y + rowHeight);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        for (var i = 0; i < InternalChildren.Count && i < _positions.Count; i++) InternalChildren[i].Arrange(_positions[i]);
        return finalSize;
    }
}
