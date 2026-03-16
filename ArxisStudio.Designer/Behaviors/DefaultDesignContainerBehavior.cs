using ArxisStudio.Designer.Models;
using ArxisStudio.Markup;

namespace ArxisStudio.Designer.Behaviors;

/// <summary>
/// Базовая реализация контейнерного design-time поведения без специальных правил layout.
/// </summary>
public sealed class DefaultDesignContainerBehavior : IDesignContainerBehavior
{
    /// <summary>
    /// Экземпляр поведения по умолчанию.
    /// </summary>
    public static DefaultDesignContainerBehavior Instance { get; } = new();

    private DefaultDesignContainerBehavior()
    {
    }

    /// <inheritdoc />
    public bool CanMove(UiNode node) => node.Design?.AllowMove ?? false;

    /// <inheritdoc />
    public bool CanResize(UiNode node) => node.Design?.AllowResize ?? false;

    /// <inheritdoc />
    public void Move(DesignMoveContext context)
    {
    }

    /// <inheritdoc />
    public void Resize(DesignResizeContext context)
    {
    }

    /// <inheritdoc />
    public DesignerDropTarget? HitTestDropTarget(DesignDropContext context) => null;

    /// <inheritdoc />
    public bool CanAcceptChild(UiNode containerNode, UiNode childNode)
    {
        return false;
    }

    /// <inheritdoc />
    public bool TryCreatePlacementIntent(DesignPlacementContext context, out DesignPlacementIntent intent)
    {
        intent = new DesignPlacementIntent(DesignPlacementKind.InsertChild, "Children");
        return false;
    }

    /// <inheritdoc />
    public DesignPlacementVisualHint? BuildPlacementVisualHint(DesignPlacementContext context, DesignPlacementIntent intent)
    {
        return null;
    }

    /// <inheritdoc />
    public void ApplyPlacement(DesignApplyPlacementContext context)
    {
    }
}
