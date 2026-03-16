using System;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ArxisStudio.Designer.Controls;

namespace ArxisStudio.Designer.Attached;

public static class Layout
{
    private static int _isInsidePositionChange;

    public static readonly AttachedProperty<double> XProperty =
        AvaloniaProperty.RegisterAttached<Control, double>(
            "X",
            typeof(Layout),
            double.NaN,
            inherits: false,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly AttachedProperty<double> YProperty =
        AvaloniaProperty.RegisterAttached<Control, double>(
            "Y",
            typeof(Layout),
            double.NaN,
            inherits: false,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly AttachedProperty<double> DesignXProperty =
        AvaloniaProperty.RegisterAttached<Control, double>(
            "DesignX",
            typeof(Layout),
            0d,
            inherits: false,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly AttachedProperty<double> DesignYProperty =
        AvaloniaProperty.RegisterAttached<Control, double>(
            "DesignY",
            typeof(Layout),
            0d,
            inherits: false,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly AttachedProperty<bool> IsTrackedProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>(
            "IsTracked",
            typeof(Layout),
            false,
            inherits: false);

    static Layout()
    {
        XProperty.Changed.AddClassHandler<Control>((s, _) => Track(s));
        YProperty.Changed.AddClassHandler<Control>((s, _) => Track(s));

        IsTrackedProperty.Changed.AddClassHandler<Control>((s, e) =>
        {
            if (e.NewValue is true)
            {
                Track(s);
            }
            else
            {
                Untrack(s);
            }
        });

        DesignXProperty.Changed.AddClassHandler<Control>((s, _) => OnDesignPositionChanged(s));
        DesignYProperty.Changed.AddClassHandler<Control>((s, _) => OnDesignPositionChanged(s));
    }

    public static void Track(Control? control)
    {
        if (control == null)
        {
            return;
        }

        control.LayoutUpdated -= OnLayoutUpdated;
        control.LayoutUpdated += OnLayoutUpdated;
        UpdateDesignPosition(control);
    }

    public static void Untrack(Control? control)
    {
        if (control == null)
        {
            return;
        }

        control.LayoutUpdated -= OnLayoutUpdated;
    }

    private static void OnLayoutUpdated(object? sender, EventArgs e)
    {
        if (sender is Control control)
        {
            UpdateDesignPosition(control);
        }
    }

    private static void UpdateDesignPosition(Control control)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (Interlocked.Exchange(ref _isInsidePositionChange, 1) == 1)
            {
                return;
            }

            try
            {
                Visual? reference = control.FindAncestorOfType<DesignSurface>()
                                    ?? control.FindAncestorOfType<DesignEditor>() as Visual;

                if (reference == null)
                {
                    return;
                }

                var position = control.TranslatePoint(new Point(0, 0), reference);
                if (!position.HasValue)
                {
                    return;
                }

                if (Math.Abs(GetDesignX(control) - position.Value.X) > 0.01)
                {
                    SetDesignX(control, position.Value.X);
                }

                if (Math.Abs(GetDesignY(control) - position.Value.Y) > 0.01)
                {
                    SetDesignY(control, position.Value.Y);
                }
            }
            finally
            {
                Interlocked.Exchange(ref _isInsidePositionChange, 0);
            }
        }, DispatcherPriority.Render);
    }

    private static void OnDesignPositionChanged(Control? control)
    {
        if (control == null || Interlocked.Exchange(ref _isInsidePositionChange, 1) == 1)
        {
            return;
        }

        try
        {
            if (control.GetVisualRoot() is null || control.GetVisualParent() is null)
            {
                void OnAttached(object? sender, VisualTreeAttachmentEventArgs e)
                {
                    control.AttachedToVisualTree -= OnAttached;
                    OnDesignPositionChanged(control);
                }

                control.AttachedToVisualTree += OnAttached;
                return;
            }

            Visual? root = control.FindAncestorOfType<DesignSurface>()
                           ?? control.FindAncestorOfType<DesignEditor>() as Visual;
            var parent = control.GetVisualParent();

            if (root == null || parent == null)
            {
                return;
            }

            var dx = GetDesignX(control);
            var dy = GetDesignY(control);
            var local = root.TranslatePoint(new Point(dx, dy), parent);
            if (!local.HasValue)
            {
                return;
            }

            SetX(control, local.Value.X);
            SetY(control, local.Value.Y);
        }
        finally
        {
            Interlocked.Exchange(ref _isInsidePositionChange, 0);
        }
    }

    public static double GetX(AvaloniaObject o) => o.GetValue(XProperty);
    public static void SetX(AvaloniaObject o, double v) => o.SetValue(XProperty, v);
    public static double GetY(AvaloniaObject o) => o.GetValue(YProperty);
    public static void SetY(AvaloniaObject o, double v) => o.SetValue(YProperty, v);
    public static double GetDesignX(AvaloniaObject o) => o.GetValue(DesignXProperty);
    public static void SetDesignX(AvaloniaObject o, double v) => o.SetValue(DesignXProperty, v);
    public static double GetDesignY(AvaloniaObject o) => o.GetValue(DesignYProperty);
    public static void SetDesignY(AvaloniaObject o, double v) => o.SetValue(DesignYProperty, v);
    public static bool GetIsTracked(AvaloniaObject o) => o.GetValue(IsTrackedProperty);
    public static void SetIsTracked(AvaloniaObject o, bool v) => o.SetValue(IsTrackedProperty, v);
}
