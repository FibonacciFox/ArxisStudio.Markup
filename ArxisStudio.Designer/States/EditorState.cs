using Avalonia.Input;

namespace ArxisStudio.Designer.States;

public abstract class EditorState
{
    protected EditorState(DesignEditor editor)
    {
        Editor = editor;
    }

    protected DesignEditor Editor { get; }

    public virtual void Enter(EditorState? from)
    {
    }

    public virtual void Exit()
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

    public virtual void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
    }
}
