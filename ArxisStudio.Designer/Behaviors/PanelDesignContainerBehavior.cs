using ArxisStudio.Designer.Models;
using ArxisStudio.Markup;

namespace ArxisStudio.Designer.Behaviors;

/// <summary>
/// Базовое поведение для панелей с коллекцией Children.
/// </summary>
internal class PanelDesignContainerBehavior : IDesignContainerBehavior
{
    internal static PanelDesignContainerBehavior Instance { get; } = new();

    protected PanelDesignContainerBehavior()
    {
    }

    public virtual bool CanMove(UiNode node) => node.Design?.AllowMove ?? false;

    public virtual bool CanResize(UiNode node) => node.Design?.AllowResize ?? false;

    public virtual void Move(DesignMoveContext context)
    {
    }

    public virtual void Resize(DesignResizeContext context)
    {
    }

    public virtual DesignerDropTarget? HitTestDropTarget(DesignDropContext context)
    {
        return new DesignerDropTarget(context.ContainerNode, "Children");
    }

    public virtual bool CanAcceptChild(UiNode containerNode, UiNode childNode) => true;

    public virtual bool TryCreatePlacementIntent(DesignPlacementContext context, out DesignPlacementIntent intent)
    {
        intent = new DesignPlacementIntent(DesignPlacementKind.InsertChild, "Children");
        return true;
    }

    public virtual DesignPlacementVisualHint? BuildPlacementVisualHint(DesignPlacementContext context, DesignPlacementIntent intent)
    {
        return new DesignPlacementVisualHint(Label: $"{intent.SlotName}");
    }

    public virtual void ApplyPlacement(DesignApplyPlacementContext context)
    {
    }
}
