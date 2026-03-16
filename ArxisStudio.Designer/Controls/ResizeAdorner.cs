using System;
using Avalonia;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace ArxisStudio.Designer.Controls;

public enum ResizeDirection
{
    Top,
    Bottom,
    Left,
    Right,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}

public class ResizeDeltaEventArgs : RoutedEventArgs
{
    public ResizeDeltaEventArgs(Vector delta, ResizeDirection direction, RoutedEvent routedEvent)
        : base(routedEvent)
    {
        Delta = delta;
        Direction = direction;
    }

    public Vector Delta { get; }
    public ResizeDirection Direction { get; }
}

public class ResizeStartedEventArgs : RoutedEventArgs
{
    public ResizeStartedEventArgs(Vector vector, ResizeDirection direction, RoutedEvent routedEvent)
        : base(routedEvent)
    {
        Vector = vector;
        Direction = direction;
    }

    public ResizeDirection Direction { get; }
    public Vector Vector { get; }
}

[TemplatePart("PART_TopLeft", typeof(Thumb))]
[TemplatePart("PART_Top", typeof(Thumb))]
[TemplatePart("PART_TopRight", typeof(Thumb))]
[TemplatePart("PART_Right", typeof(Thumb))]
[TemplatePart("PART_BottomRight", typeof(Thumb))]
[TemplatePart("PART_Bottom", typeof(Thumb))]
[TemplatePart("PART_BottomLeft", typeof(Thumb))]
[TemplatePart("PART_Left", typeof(Thumb))]
public class ResizeAdorner : TemplatedControl
{
    public static readonly StyledProperty<IBrush> AdornerBrushProperty =
        AvaloniaProperty.Register<ResizeAdorner, IBrush>(nameof(AdornerBrush), Brushes.DodgerBlue);

    public static readonly StyledProperty<double> HandleSizeProperty =
        AvaloniaProperty.Register<ResizeAdorner, double>(nameof(HandleSize), 8.0);

    public static readonly RoutedEvent<ResizeDeltaEventArgs> ResizeDeltaEvent =
        RoutedEvent.Register<ResizeDeltaEventArgs>(nameof(ResizeDelta), RoutingStrategies.Bubble, typeof(ResizeAdorner));

    public static readonly RoutedEvent<ResizeStartedEventArgs> ResizeStartedEvent =
        RoutedEvent.Register<ResizeStartedEventArgs>(nameof(ResizeStarted), RoutingStrategies.Bubble, typeof(ResizeAdorner));

    public static readonly RoutedEvent<VectorEventArgs> ResizeCompletedEvent =
        RoutedEvent.Register<VectorEventArgs>(nameof(ResizeCompleted), RoutingStrategies.Bubble, typeof(ResizeAdorner));

    public IBrush AdornerBrush
    {
        get => GetValue(AdornerBrushProperty);
        set => SetValue(AdornerBrushProperty, value);
    }

    public double HandleSize
    {
        get => GetValue(HandleSizeProperty);
        set => SetValue(HandleSizeProperty, value);
    }

    public event EventHandler<ResizeDeltaEventArgs> ResizeDelta
    {
        add => AddHandler(ResizeDeltaEvent, value);
        remove => RemoveHandler(ResizeDeltaEvent, value);
    }

    public event EventHandler<ResizeStartedEventArgs> ResizeStarted
    {
        add => AddHandler(ResizeStartedEvent, value);
        remove => RemoveHandler(ResizeStartedEvent, value);
    }

    public event EventHandler<VectorEventArgs> ResizeCompleted
    {
        add => AddHandler(ResizeCompletedEvent, value);
        remove => RemoveHandler(ResizeCompletedEvent, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        BindThumb(e, "PART_TopLeft", ResizeDirection.TopLeft);
        BindThumb(e, "PART_Top", ResizeDirection.Top);
        BindThumb(e, "PART_TopRight", ResizeDirection.TopRight);
        BindThumb(e, "PART_Right", ResizeDirection.Right);
        BindThumb(e, "PART_BottomRight", ResizeDirection.BottomRight);
        BindThumb(e, "PART_Bottom", ResizeDirection.Bottom);
        BindThumb(e, "PART_BottomLeft", ResizeDirection.BottomLeft);
        BindThumb(e, "PART_Left", ResizeDirection.Left);
    }

    private void BindThumb(TemplateAppliedEventArgs e, string name, ResizeDirection direction)
    {
        if (e.NameScope.Find(name) is not Thumb thumb)
        {
            return;
        }

        thumb.DragStarted += (_, args) =>
        {
            RaiseEvent(new ResizeStartedEventArgs(args.Vector, direction, ResizeStartedEvent));
        };

        thumb.DragDelta += (_, args) =>
        {
            RaiseEvent(new ResizeDeltaEventArgs(args.Vector, direction, ResizeDeltaEvent));
        };

        thumb.DragCompleted += (_, args) =>
        {
            RaiseEvent(new VectorEventArgs
            {
                RoutedEvent = ResizeCompletedEvent,
                Vector = args.Vector
            });
        };
    }
}
