using System;
using Avalonia;
using Avalonia.Input;

namespace ArxisStudio.Designer.States;

public sealed class EditorSelectingState : EditorState
{
    private Point _startLocationWorld;

    public EditorSelectingState(DesignEditor editor)
        : base(editor)
    {
    }

    public override void Enter(EditorState? from)
    {
        Editor.IsSelecting = true;
        _startLocationWorld = Editor.GetWorldPosition(Editor.GetPositionForInput(Editor));

        if (!Editor.LastInputModifiers.HasFlag(KeyModifiers.Control))
        {
            Editor.SelectedItem = null;
            Editor.Selection.Clear();
        }

        Editor.SelectedArea = new Rect(_startLocationWorld, new Size(0, 0));
    }

    public override void Exit()
    {
        Editor.IsSelecting = false;
        Editor.SelectedArea = new Rect(0, 0, 0, 0);
    }

    public override void OnPointerMoved(PointerEventArgs e)
    {
        var currentMousePosWorld = Editor.GetWorldPosition(e.GetPosition(Editor));

        var x = Math.Min(_startLocationWorld.X, currentMousePosWorld.X);
        var y = Math.Min(_startLocationWorld.Y, currentMousePosWorld.Y);
        var w = Math.Abs(_startLocationWorld.X - currentMousePosWorld.X);
        var h = Math.Abs(_startLocationWorld.Y - currentMousePosWorld.Y);

        Editor.SelectedArea = new Rect(x, y, w, h);
    }

    public override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        Editor.CommitSelection(Editor.SelectedArea, e.KeyModifiers.HasFlag(KeyModifiers.Control));
        Editor.PopState();
    }
}
