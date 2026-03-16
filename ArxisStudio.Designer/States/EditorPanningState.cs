using Avalonia;
using Avalonia.Input;

namespace ArxisStudio.Designer.States;

public sealed class EditorPanningState : EditorState
{
    private Point _startMousePosition;
    private Point _startViewportLocation;

    public EditorPanningState(DesignEditor editor)
        : base(editor)
    {
    }

    public override void Enter(EditorState? from)
    {
        _startMousePosition = Editor.GetPositionForInput(Editor);
        _startViewportLocation = Editor.ViewportLocation;
        Editor.Cursor = new Cursor(StandardCursorType.Hand);
    }

    public override void Exit()
    {
        Editor.Cursor = Cursor.Default;
    }

    public override void OnPointerMoved(PointerEventArgs e)
    {
        var currentMousePos = e.GetPosition(Editor);
        var diffScreen = _startMousePosition - currentMousePos;
        Editor.ViewportLocation = _startViewportLocation + (diffScreen / Editor.ViewportZoom);
    }

    public override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        Editor.PopState();
    }

    public override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        Editor.HandleZoom(e);
    }
}
