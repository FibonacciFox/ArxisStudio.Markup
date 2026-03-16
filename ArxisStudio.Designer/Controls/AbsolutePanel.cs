using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.VisualTree;
using ArxisStudio.Designer.Attached;

namespace ArxisStudio.Designer.Controls;

public class AbsolutePanel : Panel
{
    public static readonly StyledProperty<Rect> ExtentProperty =
        AvaloniaProperty.Register<AbsolutePanel, Rect>(nameof(Extent));

    public Rect Extent
    {
        get => GetValue(ExtentProperty);
        set => SetValue(ExtentProperty, value);
    }

    static AbsolutePanel()
    {
        Layout.XProperty.Changed.AddClassHandler<Control>((s, _) => InvalidateParentLayout(s));
        Layout.YProperty.Changed.AddClassHandler<Control>((s, _) => InvalidateParentLayout(s));
    }

    private static void InvalidateParentLayout(Control control)
    {
        if (control.GetVisualParent() is AbsolutePanel panel)
        {
            panel.InvalidateMeasure();
            panel.InvalidateArrange();
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var infinite = new Size(double.PositiveInfinity, double.PositiveInfinity);
        double minX = 0;
        double minY = 0;
        double maxX = 0;
        double maxY = 0;
        var hasItems = false;

        foreach (var child in Children)
        {
            child.Measure(infinite);

            var x = Layout.GetX(child);
            var y = Layout.GetY(child);
            var effectiveX = double.IsNaN(x) ? 0 : x;
            var effectiveY = double.IsNaN(y) ? 0 : y;
            var size = child.DesiredSize;

            if (size.Width <= 0 || size.Height <= 0)
            {
                continue;
            }

            hasItems = true;
            if (effectiveX < minX)
            {
                minX = effectiveX;
            }

            if (effectiveY < minY)
            {
                minY = effectiveY;
            }

            if (effectiveX + size.Width > maxX)
            {
                maxX = effectiveX + size.Width;
            }

            if (effectiveY + size.Height > maxY)
            {
                maxY = effectiveY + size.Height;
            }
        }

        SetCurrentValue(ExtentProperty, hasItems ? new Rect(minX, minY, maxX - minX, maxY - minY) : new Rect());

        var resultW = double.IsPositiveInfinity(availableSize.Width) ? maxX : availableSize.Width;
        var resultH = double.IsPositiveInfinity(availableSize.Height) ? maxY : availableSize.Height;
        return new Size(resultW, resultH);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (var child in Children)
        {
            Layout.Track(child);

            var x = Layout.GetX(child);
            var y = Layout.GetY(child);

            double finalX;
            double finalY;
            var finalW = child.DesiredSize.Width;
            var finalH = child.DesiredSize.Height;

            if (!double.IsNaN(x))
            {
                finalX = x;
            }
            else
            {
                finalX = child.HorizontalAlignment switch
                {
                    HorizontalAlignment.Center => (finalSize.Width - child.DesiredSize.Width) / 2,
                    HorizontalAlignment.Right => finalSize.Width - child.DesiredSize.Width,
                    HorizontalAlignment.Stretch => 0,
                    _ => 0
                };

                if (child.HorizontalAlignment == HorizontalAlignment.Stretch)
                {
                    finalW = finalSize.Width;
                }
            }

            if (!double.IsNaN(y))
            {
                finalY = y;
            }
            else
            {
                finalY = child.VerticalAlignment switch
                {
                    VerticalAlignment.Center => (finalSize.Height - child.DesiredSize.Height) / 2,
                    VerticalAlignment.Bottom => finalSize.Height - child.DesiredSize.Height,
                    VerticalAlignment.Stretch => 0,
                    _ => 0
                };

                if (child.VerticalAlignment == VerticalAlignment.Stretch)
                {
                    finalH = finalSize.Height;
                }
            }

            child.Arrange(new Rect(finalX, finalY, finalW, finalH));
        }

        return finalSize;
    }
}
