using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Mixins;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using ArxisStudio.Designer.Attached;
using ArxisStudio.Designer.Controls;
using ArxisStudio.Designer.States;

namespace ArxisStudio.Designer;

[TemplatePart("PART_Border", typeof(Border))]
[TemplatePart("PART_Resizer", typeof(ResizeAdorner))]
[PseudoClasses(":selected", ":dragging", ":resizing")]
public class DesignEditorItem : ContentControl, ISelectable, IDesignEditorItem
{
    private readonly Stack<DesignEditorItemState> _states = new();
    private ResizeAdorner? _resizeAdorner;

    public static readonly StyledProperty<bool> IsSelectedProperty =
        SelectingItemsControl.IsSelectedProperty.AddOwner<DesignEditorItem>();

    public static readonly StyledProperty<Point> LocationProperty =
        AvaloniaProperty.Register<DesignEditorItem, Point>(nameof(Location));

    public static readonly StyledProperty<bool> IsDraggableProperty =
        AvaloniaProperty.Register<DesignEditorItem, bool>(nameof(IsDraggable), true);

    public static readonly StyledProperty<IBrush> SelectedBrushProperty =
        AvaloniaProperty.Register<DesignEditorItem, IBrush>(nameof(SelectedBrush), Brushes.Orange);

    public static readonly StyledProperty<Thickness> SelectedBorderThicknessProperty =
        AvaloniaProperty.Register<DesignEditorItem, Thickness>(nameof(SelectedBorderThickness), new Thickness(2));

    public static readonly DirectProperty<DesignEditorItem, Thickness> SelectedMarginProperty =
        AvaloniaProperty.RegisterDirect<DesignEditorItem, Thickness>(nameof(SelectedMargin), o => o.SelectedMargin);

    public static readonly RoutedEvent<DragStartedEventArgs> DragStartedEvent =
        RoutedEvent.Register<DragStartedEventArgs>(nameof(DragStarted), RoutingStrategies.Bubble, typeof(DesignEditorItem));

    public static readonly RoutedEvent<DragDeltaEventArgs> DragDeltaEvent =
        RoutedEvent.Register<DragDeltaEventArgs>(nameof(DragDelta), RoutingStrategies.Bubble, typeof(DesignEditorItem));

    public static readonly RoutedEvent<DragCompletedEventArgs> DragCompletedEvent =
        RoutedEvent.Register<DragCompletedEventArgs>(nameof(DragCompleted), RoutingStrategies.Bubble, typeof(DesignEditorItem));

    public static readonly RoutedEvent<ResizeDeltaEventArgs> ResizeDeltaEvent =
        RoutedEvent.Register<ResizeDeltaEventArgs>(nameof(ResizeDelta), RoutingStrategies.Bubble, typeof(DesignEditorItem));

    public static readonly RoutedEvent<VectorEventArgs> ResizeStartedEvent =
        RoutedEvent.Register<VectorEventArgs>(nameof(ResizeStarted), RoutingStrategies.Bubble, typeof(DesignEditorItem));

    public static readonly RoutedEvent<VectorEventArgs> ResizeCompletedEvent =
        RoutedEvent.Register<VectorEventArgs>(nameof(ResizeCompleted), RoutingStrategies.Bubble, typeof(DesignEditorItem));

    static DesignEditorItem()
    {
        SelectableMixin.Attach<DesignEditorItem>(IsSelectedProperty);
        FocusableProperty.OverrideDefaultValue<DesignEditorItem>(true);
    }

    public DesignEditorItem()
    {
        _states.Push(new ItemIdleState(this));
    }

    public DesignEditorItemState CurrentState => _states.Count > 0 ? _states.Peek() : null!;

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public Point Location
    {
        get => GetValue(LocationProperty);
        set => SetValue(LocationProperty, value);
    }

    public bool IsDraggable
    {
        get => GetValue(IsDraggableProperty);
        set => SetValue(IsDraggableProperty, value);
    }

    public IBrush SelectedBrush
    {
        get => GetValue(SelectedBrushProperty);
        set => SetValue(SelectedBrushProperty, value);
    }

    public Thickness SelectedBorderThickness
    {
        get => GetValue(SelectedBorderThicknessProperty);
        set => SetValue(SelectedBorderThicknessProperty, value);
    }

    public Thickness SelectedMargin => new(
        -SelectedBorderThickness.Left,
        -SelectedBorderThickness.Top,
        -SelectedBorderThickness.Right,
        -SelectedBorderThickness.Bottom);

    public event EventHandler<DragStartedEventArgs> DragStarted
    {
        add => AddHandler(DragStartedEvent, value);
        remove => RemoveHandler(DragStartedEvent, value);
    }

    public event EventHandler<DragDeltaEventArgs> DragDelta
    {
        add => AddHandler(DragDeltaEvent, value);
        remove => RemoveHandler(DragDeltaEvent, value);
    }

    public event EventHandler<DragCompletedEventArgs> DragCompleted
    {
        add => AddHandler(DragCompletedEvent, value);
        remove => RemoveHandler(DragCompletedEvent, value);
    }

    public event EventHandler<ResizeDeltaEventArgs> ResizeDelta
    {
        add => AddHandler(ResizeDeltaEvent, value);
        remove => RemoveHandler(ResizeDeltaEvent, value);
    }

    public event EventHandler<VectorEventArgs> ResizeStarted
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

        if (_resizeAdorner != null)
        {
            _resizeAdorner.ResizeStarted -= OnAdornerResizeStarted;
            _resizeAdorner.ResizeDelta -= OnAdornerResizeDelta;
            _resizeAdorner.ResizeCompleted -= OnAdornerResizeCompleted;
        }

        _resizeAdorner = e.NameScope.Find<ResizeAdorner>("PART_Resizer");
        if (_resizeAdorner == null)
        {
            return;
        }

        _resizeAdorner.ResizeStarted += OnAdornerResizeStarted;
        _resizeAdorner.ResizeDelta += OnAdornerResizeDelta;
        _resizeAdorner.ResizeCompleted += OnAdornerResizeCompleted;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SelectedBorderThicknessProperty)
        {
            RaisePropertyChanged(SelectedMarginProperty, default, SelectedMargin);
            return;
        }

        if (change.Property == IsSelectedProperty)
        {
            UpdatePseudoClasses();
            return;
        }

        if (change.Property == LocationProperty)
        {
            Layout.SetX(this, Location.X);
            Layout.SetY(this, Location.Y);
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!e.Handled)
        {
            CurrentState.OnPointerPressed(e);
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        CurrentState.OnPointerMoved(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        CurrentState.OnPointerReleased(e);
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        while (_states.Count > 1)
        {
            PopState();
        }
    }

    public void PushState(DesignEditorItemState state)
    {
        var previous = CurrentState;
        _states.Push(state);
        state.Enter(previous);
        UpdatePseudoClassesState(state);
    }

    public void PopState()
    {
        if (_states.Count <= 1)
        {
            return;
        }

        var current = _states.Pop();
        current.Exit();
        CurrentState.ReEnter(current);
        UpdatePseudoClassesState(CurrentState);
    }

    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(":selected", IsSelected);
    }

    private void UpdatePseudoClassesState(DesignEditorItemState state)
    {
        PseudoClasses.Set(":dragging", state is ItemDraggingState);
        PseudoClasses.Set(":resizing", state is ItemResizingState);
    }

    private void OnAdornerResizeStarted(object? sender, ResizeStartedEventArgs e)
    {
        PushState(new ItemResizingState(this, e.Direction));
        RaiseEvent(new VectorEventArgs
        {
            RoutedEvent = ResizeStartedEvent,
            Vector = e.Vector
        });
        e.Handled = true;
    }

    private void OnAdornerResizeDelta(object? sender, ResizeDeltaEventArgs e)
    {
        CurrentState.OnResizeDelta(e);
        e.Handled = true;
    }

    private void OnAdornerResizeCompleted(object? sender, VectorEventArgs e)
    {
        PopState();
        RaiseEvent(new VectorEventArgs
        {
            RoutedEvent = ResizeCompletedEvent,
            Vector = e.Vector
        });
        e.Handled = true;
    }
}
