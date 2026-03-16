using ArxisStudio.Designer.Models;
using ArxisStudio.Markup;

namespace ArxisStudio.Designer.Behaviors;

/// <summary>
/// Определяет design-time поведение контейнера.
/// </summary>
public interface IDesignContainerBehavior
{
    /// <summary>
    /// Определяет, может ли указанный узел быть перемещён внутри контейнера.
    /// </summary>
    bool CanMove(UiNode node);

    /// <summary>
    /// Определяет, может ли указанный узел изменять размеры внутри контейнера.
    /// </summary>
    bool CanResize(UiNode node);

    /// <summary>
    /// Выполняет перемещение узла в контексте контейнера.
    /// </summary>
    void Move(DesignMoveContext context);

    /// <summary>
    /// Выполняет изменение размеров узла в контексте контейнера.
    /// </summary>
    void Resize(DesignResizeContext context);

    /// <summary>
    /// Выполняет design-time hit testing для drop-операции.
    /// </summary>
    DesignerDropTarget? HitTestDropTarget(DesignDropContext context);

    /// <summary>
    /// Определяет, может ли контейнер принять указанный дочерний узел.
    /// </summary>
    bool CanAcceptChild(UiNode containerNode, UiNode childNode);

    /// <summary>
    /// Пытается вычислить намерение размещения для текущего drop-контекста.
    /// </summary>
    bool TryCreatePlacementIntent(DesignPlacementContext context, out DesignPlacementIntent intent);

    /// <summary>
    /// Возвращает визуальную подсказку для computed placement intent.
    /// </summary>
    DesignPlacementVisualHint? BuildPlacementVisualHint(DesignPlacementContext context, DesignPlacementIntent intent);

    /// <summary>
    /// Применяет вычисленное размещение внутри контейнера.
    /// </summary>
    void ApplyPlacement(DesignApplyPlacementContext context);
}
