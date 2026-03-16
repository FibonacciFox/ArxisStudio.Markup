using Avalonia;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace ArxisStudio.Designer.States;

public sealed class EditorIdleState : EditorState
{
    public EditorIdleState(DesignEditor editor)
        : base(editor)
    {
    }

    public override void OnPointerPressed(PointerPressedEventArgs e)
    {
        var props = e.GetCurrentPoint(Editor).Properties;
        var modifiers = e.KeyModifiers;

        if (props.IsMiddleButtonPressed || (props.IsLeftButtonPressed && modifiers.HasFlag(KeyModifiers.Alt)))
        {
            Editor.PushState(new EditorPanningState(Editor));
            return;
        }

        var source = e.Source as Avalonia.Visual;
        var itemContainer = source?.FindAncestorOfType<DesignEditorItem>();
        if (props.IsLeftButtonPressed && itemContainer == null)
        {
            Editor.PushState(new EditorSelectingState(Editor));
        }
    }

    public override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        Editor.HandleZoom(e);
    }
}
