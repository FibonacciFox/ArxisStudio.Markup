using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Selection;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using ArxisStudio.Designer.States;

namespace ArxisStudio.Designer;

public class DesignEditor : SelectingItemsControl
{
    private const double ZoomFactor = 1.1;
    private const double ZoomTolerance = 0.0001;

    private readonly Stack<EditorState> _states = new();
    private readonly TranslateTransform _translateTransform = new();
    private readonly ScaleTransform _scaleTransform = new();
    private readonly TranslateTransform _dpiTranslateTransform = new();
    private Point _lastMousePosition;
    private bool _isSelecting;
    private Rect _selectedArea;
    private Rect _itemsExtent;

    public new static readonly DirectProperty<SelectingItemsControl, ISelectionModel> SelectionProperty =
        SelectingItemsControl.SelectionProperty;

    public new static readonly DirectProperty<SelectingItemsControl, IList?> SelectedItemsProperty =
        SelectingItemsControl.SelectedItemsProperty;

    public new static readonly StyledProperty<SelectionMode> SelectionModeProperty =
        SelectingItemsControl.SelectionModeProperty.AddOwner<DesignEditor>();

    public static readonly StyledProperty<Point> ViewportLocationProperty =
        AvaloniaProperty.Register<DesignEditor, Point>(nameof(ViewportLocation));

    public static readonly StyledProperty<double> ViewportZoomProperty =
        AvaloniaProperty.Register<DesignEditor, double>(nameof(ViewportZoom), 1.0);

    public static readonly StyledProperty<double> MinZoomProperty =
        AvaloniaProperty.Register<DesignEditor, double>(nameof(MinZoom), 0.1);

    public static readonly StyledProperty<double> MaxZoomProperty =
        AvaloniaProperty.Register<DesignEditor, double>(nameof(MaxZoom), 5.0);

    public static readonly StyledProperty<Transform> ViewportTransformProperty =
        AvaloniaProperty.Register<DesignEditor, Transform>(nameof(ViewportTransform), new TransformGroup());

    public static readonly StyledProperty<Transform> DpiScaledViewportTransformProperty =
        AvaloniaProperty.Register<DesignEditor, Transform>(nameof(DpiScaledViewportTransform), new TransformGroup());

    public static readonly StyledProperty<ControlTheme?> SelectionRectangleStyleProperty =
        AvaloniaProperty.Register<DesignEditor, ControlTheme?>(nameof(SelectionRectangleStyle));

    public static readonly DirectProperty<DesignEditor, bool> IsSelectingProperty =
        AvaloniaProperty.RegisterDirect<DesignEditor, bool>(nameof(IsSelecting), o => o.IsSelecting, (o, v) => o.IsSelecting = v);

    public static readonly DirectProperty<DesignEditor, Rect> SelectedAreaProperty =
        AvaloniaProperty.RegisterDirect<DesignEditor, Rect>(nameof(SelectedArea), o => o.SelectedArea, (o, v) => o.SelectedArea = v);

    public static readonly DirectProperty<DesignEditor, Rect> ItemsExtentProperty =
        AvaloniaProperty.RegisterDirect<DesignEditor, Rect>(nameof(ItemsExtent), o => o.ItemsExtent, (o, v) => o.ItemsExtent = v);

    static DesignEditor()
    {
        FocusableProperty.OverrideDefaultValue<DesignEditor>(true);
        ViewportLocationProperty.Changed.AddClassHandler<DesignEditor>((x, _) => x.UpdateTransforms());
        ViewportZoomProperty.Changed.AddClassHandler<DesignEditor>((x, _) => x.UpdateTransforms());

        DesignEditorItem.DragStartedEvent.AddClassHandler<DesignEditor>((x, e) => x.OnItemsDragStarted(e));
        DesignEditorItem.DragDeltaEvent.AddClassHandler<DesignEditor>((x, e) => x.OnItemsDragDelta(e));
        DesignEditorItem.DragCompletedEvent.AddClassHandler<DesignEditor>((x, e) => x.OnItemsDragCompleted(e));
    }

    public DesignEditor()
    {
        SelectionMode = SelectionMode.Multiple;

        var contentGroup = new TransformGroup();
        contentGroup.Children.Add(_scaleTransform);
        contentGroup.Children.Add(_translateTransform);
        SetCurrentValue(ViewportTransformProperty, contentGroup);

        var dpiGroup = new TransformGroup();
        dpiGroup.Children.Add(_scaleTransform);
        dpiGroup.Children.Add(_dpiTranslateTransform);
        SetCurrentValue(DpiScaledViewportTransformProperty, dpiGroup);

        _states.Push(new EditorIdleState(this));
    }

    public EditorState CurrentState => _states.Count > 0 ? _states.Peek() : null!;

    public KeyModifiers LastInputModifiers { get; private set; }

    public new ISelectionModel Selection
    {
        get => base.Selection;
        set => base.Selection = value;
    }

    public new IList? SelectedItems
    {
        get => base.SelectedItems;
        set => base.SelectedItems = value;
    }

    public new SelectionMode SelectionMode
    {
        get => base.SelectionMode;
        set => base.SelectionMode = value;
    }

    public Point ViewportLocation
    {
        get => GetValue(ViewportLocationProperty);
        set => SetValue(ViewportLocationProperty, value);
    }

    public double ViewportZoom
    {
        get => GetValue(ViewportZoomProperty);
        set => SetValue(ViewportZoomProperty, value);
    }

    public double MinZoom
    {
        get => GetValue(MinZoomProperty);
        set => SetValue(MinZoomProperty, value);
    }

    public double MaxZoom
    {
        get => GetValue(MaxZoomProperty);
        set => SetValue(MaxZoomProperty, value);
    }

    public Transform ViewportTransform
    {
        get => GetValue(ViewportTransformProperty);
        set => SetValue(ViewportTransformProperty, value);
    }

    public Transform DpiScaledViewportTransform
    {
        get => GetValue(DpiScaledViewportTransformProperty);
        set => SetValue(DpiScaledViewportTransformProperty, value);
    }

    public ControlTheme? SelectionRectangleStyle
    {
        get => GetValue(SelectionRectangleStyleProperty);
        set => SetValue(SelectionRectangleStyleProperty, value);
    }

    public bool IsSelecting
    {
        get => _isSelecting;
        set => SetAndRaise(IsSelectingProperty, ref _isSelecting, value);
    }

    public Rect SelectedArea
    {
        get => _selectedArea;
        set => SetAndRaise(SelectedAreaProperty, ref _selectedArea, value);
    }

    public Rect ItemsExtent
    {
        get => _itemsExtent;
        set => SetAndRaise(ItemsExtentProperty, ref _itemsExtent, value);
    }

    public void PushState(EditorState state)
    {
        var previous = _states.Count > 0 ? _states.Peek() : null;
        _states.Push(state);
        state.Enter(previous);
    }

    public void PopState()
    {
        if (_states.Count <= 1)
        {
            return;
        }

        var current = _states.Pop();
        current.Exit();
    }

    public Point GetWorldPosition(Point screenPoint)
    {
        return (screenPoint / ViewportZoom) + ViewportLocation;
    }

    public Point GetPositionForInput(Visual relativeTo)
    {
        return _lastMousePosition;
    }

    public void HandleZoom(PointerWheelEventArgs e)
    {
        var prevZoom = ViewportZoom;
        var newZoom = e.Delta.Y > 0 ? prevZoom * ZoomFactor : prevZoom / ZoomFactor;
        newZoom = Math.Max(GetValue(MinZoomProperty), Math.Min(GetValue(MaxZoomProperty), newZoom));

        if (Math.Abs(newZoom - prevZoom) <= ZoomTolerance)
        {
            return;
        }

        var mousePos = e.GetPosition(this);
        var correction = (Vector)mousePos / prevZoom - (Vector)mousePos / newZoom;
        ViewportZoom = newZoom;
        ViewportLocation += correction;
    }

    public void CommitSelection(Rect bounds, bool isCtrlPressed)
    {
        if (Presenter?.Panel == null)
        {
            return;
        }

        using (Selection.BatchUpdate())
        {
            if (!isCtrlPressed)
            {
                Selection.Clear();
            }

            foreach (var child in Presenter.Panel.Children)
            {
                if (child is not DesignEditorItem container)
                {
                    continue;
                }

                if (bounds.Intersects(new Rect(container.Location, container.Bounds.Size)))
                {
                    Selection.Select(IndexFromContainer(container));
                }
            }
        }
    }

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        return NeedsContainer<DesignEditorItem>(item, out recycleKey);
    }

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
    {
        return new DesignEditorItem();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (e.Root is TopLevel topLevel)
        {
            topLevel.ScalingChanged += OnScreenScalingChanged;
        }

        UpdateTransforms();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (e.Root is TopLevel topLevel)
        {
            topLevel.ScalingChanged -= OnScreenScalingChanged;
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        _lastMousePosition = e.GetPosition(this);
        LastInputModifiers = e.KeyModifiers;
        CurrentState.OnPointerPressed(e);

        if (!e.Handled)
        {
            base.OnPointerPressed(e);
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        _lastMousePosition = e.GetPosition(this);
        CurrentState.OnPointerMoved(e);
        base.OnPointerMoved(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        CurrentState.OnPointerReleased(e);
        base.OnPointerReleased(e);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        if (!e.Handled)
        {
            CurrentState.OnPointerWheelChanged(e);
            e.Handled = true;
        }
    }

    private void OnItemsDragStarted(DragStartedEventArgs e)
    {
        e.Handled = true;
    }

    private void OnItemsDragDelta(DragDeltaEventArgs e)
    {
        if (IsSelecting || CurrentState is EditorPanningState)
        {
            return;
        }

        var items = SelectedItems;
        if (items == null || items.Count == 0)
        {
            return;
        }

        var delta = new Vector(e.HorizontalChange, e.VerticalChange);
        foreach (var item in items)
        {
            var container = ContainerFromItem(item) as DesignEditorItem;
            if (container == null && item is DesignEditorItem directItem)
            {
                container = directItem;
            }

            if (container != null && container.IsDraggable)
            {
                container.Location += delta;
            }
        }

        e.Handled = true;
    }

    private void OnItemsDragCompleted(DragCompletedEventArgs e)
    {
        e.Handled = true;
    }

    private void OnScreenScalingChanged(object? sender, EventArgs e)
    {
        UpdateTransforms();
    }

    private void UpdateTransforms()
    {
        _scaleTransform.ScaleX = ViewportZoom;
        _scaleTransform.ScaleY = ViewportZoom;

        var x = -ViewportLocation.X * ViewportZoom;
        var y = -ViewportLocation.Y * ViewportZoom;
        _translateTransform.X = x;
        _translateTransform.Y = y;

        var root = this.GetVisualRoot();
        var renderScaling = root?.RenderScaling ?? 1.0;
        _dpiTranslateTransform.X = Math.Round(x * renderScaling) / renderScaling;
        _dpiTranslateTransform.Y = Math.Round(y * renderScaling) / renderScaling;

        var vg = new TransformGroup();
        vg.Children.Add(_scaleTransform);
        vg.Children.Add(_translateTransform);
        SetCurrentValue(ViewportTransformProperty, vg);

        var dg = new TransformGroup();
        dg.Children.Add(_scaleTransform);
        dg.Children.Add(_dpiTranslateTransform);
        SetCurrentValue(DpiScaledViewportTransformProperty, dg);
    }
}
