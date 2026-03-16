using ArxisStudio.Designer.Models;
using ArxisStudio.Markup;

namespace ArxisStudio.Designer.Behaviors;

/// <summary>
/// Поведение размещения для контейнеров с одиночным слотом Content.
/// </summary>
internal sealed class ContentControlDesignContainerBehavior : IDesignContainerBehavior
{
    internal static ContentControlDesignContainerBehavior Instance { get; } = new();

    private ContentControlDesignContainerBehavior()
    {
    }

    public bool CanMove(UiNode node) => node.Design?.AllowMove ?? false;

    public bool CanResize(UiNode node) => node.Design?.AllowResize ?? false;

    public void Move(DesignMoveContext context)
    {
    }

    public void Resize(DesignResizeContext context)
    {
    }

    public DesignerDropTarget? HitTestDropTarget(DesignDropContext context)
    {
        return new DesignerDropTarget(context.ContainerNode, "Content");
    }

    public bool CanAcceptChild(UiNode containerNode, UiNode childNode) => true;

    public bool TryCreatePlacementIntent(DesignPlacementContext context, out DesignPlacementIntent intent)
    {
        intent = new DesignPlacementIntent(DesignPlacementKind.SetContent, "Content");
        return true;
    }

    public DesignPlacementVisualHint? BuildPlacementVisualHint(DesignPlacementContext context, DesignPlacementIntent intent)
    {
        return new DesignPlacementVisualHint(Label: "Content");
    }

    public void ApplyPlacement(DesignApplyPlacementContext context)
    {
    }
}
