using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using ArxisStudio.Designer.Controls;

namespace ArxisStudio.Designer.States;

public abstract class DesignEditorItemState
{
    protected DesignEditorItemState(DesignEditorItem container)
    {
        Container = container;
    }

    protected DesignEditorItem Container { get; }

    public virtual void Enter(DesignEditorItemState? from)
    {
    }

    public virtual void Exit()
    {
    }

    public virtual void ReEnter(DesignEditorItemState from)
    {
    }

    public virtual void OnPointerPressed(PointerPressedEventArgs e)
    {
    }

    public virtual void OnPointerMoved(PointerEventArgs e)
    {
    }

    public virtual void OnPointerReleased(PointerReleasedEventArgs e)
    {
    }

    public virtual void OnResizeDelta(ResizeDeltaEventArgs e)
    {
    }
}

public sealed class ItemIdleState : DesignEditorItemState
{
    private Point _startPoint;
    private bool _isPressed;
    private bool _shouldSkipSelectionToggle;

    public ItemIdleState(DesignEditorItem container)
        : base(container)
    {
    }

    public override void ReEnter(DesignEditorItemState from)
    {
        _isPressed = false;
        _shouldSkipSelectionToggle = false;
    }

    public override void OnPointerPressed(PointerPressedEventArgs e)
    {
        var props = e.GetCurrentPoint(Container).Properties;
        if (!props.IsLeftButtonPressed || !Container.IsDraggable)
        {
            return;
        }

        e.Pointer.Capture(Container);
        e.Handled = true;
        _isPressed = true;

        var parent = Container.GetVisualParent();
        if (parent != null)
        {
            _startPoint = e.GetPosition((Visual)parent);
        }

        HandleSelectionOnPress(e);
    }

    public override void OnPointerMoved(PointerEventArgs e)
    {
        if (!_isPressed)
        {
            return;
        }

        var parent = Container.GetVisualParent();
        if (parent == null)
        {
            return;
        }

        var isAbsolute = parent is AbsolutePanel || parent is Canvas;
        if (!isAbsolute)
        {
            return;
        }

        var currentPoint = e.GetPosition((Visual)parent);
        if (Vector.Distance(_startPoint, currentPoint) > 3)
        {
            Container.PushState(new ItemDraggingState(Container, _startPoint));
        }
    }

    public override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (!_isPressed)
        {
            return;
        }

        HandleSelectionOnRelease(e);
        _isPressed = false;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private void HandleSelectionOnPress(PointerPressedEventArgs e)
    {
        var editor = Container.FindAncestorOfType<DesignEditor>();
        if (editor == null)
        {
            return;
        }

        var isCtrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        if (!Container.IsSelected)
        {
            if (!isCtrl)
            {
                editor.Selection.Clear();
            }

            editor.Selection.Select(editor.IndexFromContainer(Container));
            _shouldSkipSelectionToggle = true;
        }
        else
        {
            _shouldSkipSelectionToggle = false;
        }
    }

    private void HandleSelectionOnRelease(PointerReleasedEventArgs e)
    {
        var editor = Container.FindAncestorOfType<DesignEditor>();
        if (editor == null)
        {
            return;
        }

        var isCtrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        var index = editor.IndexFromContainer(Container);
        if (isCtrl)
        {
            if (_shouldSkipSelectionToggle)
            {
                return;
            }

            if (Container.IsSelected)
            {
                editor.Selection.Deselect(index);
            }
            else
            {
                editor.Selection.Select(index);
            }

            return;
        }

        if (Container.IsSelected && editor.Selection.Count > 1)
        {
            editor.Selection.Clear();
            editor.Selection.Select(index);
        }
    }
}

public sealed class ItemDraggingState : DesignEditorItemState
{
    private Point _previousPosition;
    private readonly Point _initialPosition;
    private Point _elementStartLocation;

    public ItemDraggingState(DesignEditorItem container, Point initialPosition)
        : base(container)
    {
        _initialPosition = initialPosition;
        _previousPosition = initialPosition;
    }

    public override void Enter(DesignEditorItemState? from)
    {
        _elementStartLocation = Container.Location;
        Container.RaiseEvent(new DragStartedEventArgs(_initialPosition.X, _initialPosition.Y)
        {
            RoutedEvent = DesignEditorItem.DragStartedEvent
        });
    }

    public override void Exit()
    {
        var total = _previousPosition - _initialPosition;
        Container.RaiseEvent(new DragCompletedEventArgs(total.X, total.Y, false)
        {
            RoutedEvent = DesignEditorItem.DragCompletedEvent
        });
    }

    public override void OnPointerMoved(PointerEventArgs e)
    {
        var parent = Container.GetVisualParent();
        if (parent == null)
        {
            return;
        }

        var currentPosition = e.GetPosition((Visual)parent);
        var totalDelta = currentPosition - _initialPosition;

        var newX = Math.Round(_elementStartLocation.X + totalDelta.X);
        var newY = Math.Round(_elementStartLocation.Y + totalDelta.Y);
        Container.Location = new Point(newX, newY);

        var frameDelta = currentPosition - _previousPosition;
        Container.RaiseEvent(new DragDeltaEventArgs(frameDelta.X, frameDelta.Y)
        {
            RoutedEvent = DesignEditorItem.DragDeltaEvent
        });

        _previousPosition = currentPosition;
        e.Handled = true;
    }

    public override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        Container.PopState();
        e.Pointer.Capture(null);
        e.Handled = true;
    }
}

public sealed class ItemResizingState : DesignEditorItemState
{
    private readonly ResizeDirection _direction;
    private Point _initialLocation;
    private Size _initialSize;
    private Vector _accumulatedDelta;

    public ItemResizingState(DesignEditorItem container, ResizeDirection direction)
        : base(container)
    {
        _direction = direction;
    }

    public override void Enter(DesignEditorItemState? from)
    {
        _initialLocation = Container.Location;
        _accumulatedDelta = Vector.Zero;

        var w = double.IsNaN(Container.Width) ? Container.Bounds.Width : Container.Width;
        var h = double.IsNaN(Container.Height) ? Container.Bounds.Height : Container.Height;
        Container.Width = w;
        Container.Height = h;
        _initialSize = new Size(w, h);
    }

    public override void OnResizeDelta(ResizeDeltaEventArgs e)
    {
        _accumulatedDelta += e.Delta;

        var newW = _initialSize.Width;
        var newH = _initialSize.Height;
        var newX = _initialLocation.X;
        var newY = _initialLocation.Y;
        var dx = _accumulatedDelta.X;
        var dy = _accumulatedDelta.Y;

        switch (_direction)
        {
            case ResizeDirection.Right:
                newW += dx;
                break;
            case ResizeDirection.Bottom:
                newH += dy;
                break;
            case ResizeDirection.Left:
                newW -= dx;
                newX += dx;
                break;
            case ResizeDirection.Top:
                newH -= dy;
                newY += dy;
                break;
            case ResizeDirection.BottomRight:
                newW += dx;
                newH += dy;
                break;
            case ResizeDirection.BottomLeft:
                newW -= dx;
                newX += dx;
                newH += dy;
                break;
            case ResizeDirection.TopRight:
                newW += dx;
                newH -= dy;
                newY += dy;
                break;
            case ResizeDirection.TopLeft:
                newW -= dx;
                newX += dx;
                newH -= dy;
                newY += dy;
                break;
        }

        if (newW < 10)
        {
            newW = 10;
        }

        if (newH < 10)
        {
            newH = 10;
        }

        Container.Width = Math.Round(newW);
        Container.Height = Math.Round(newH);

        if (Math.Abs(newX - _initialLocation.X) > 0.1 || Math.Abs(newY - _initialLocation.Y) > 0.1)
        {
            Container.Location = new Point(Math.Round(newX), Math.Round(newY));
        }

        Container.RaiseEvent(new ResizeDeltaEventArgs(e.Delta, _direction, DesignEditorItem.ResizeDeltaEvent));
    }
}
