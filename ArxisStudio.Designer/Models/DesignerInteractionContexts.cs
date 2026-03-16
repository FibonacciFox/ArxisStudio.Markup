using Avalonia;
using ArxisStudio.Markup;
using System.Collections.Generic;

namespace ArxisStudio.Designer.Models;

/// <summary>
/// Контекст операции перемещения узла.
/// </summary>
/// <param name="Node">Перемещаемый узел.</param>
/// <param name="Delta">Смещение относительно исходной позиции.</param>
public sealed record DesignMoveContext(UiNode Node, Vector Delta);

/// <summary>
/// Контекст операции изменения размеров узла.
/// </summary>
/// <param name="Node">Узел, размеры которого изменяются.</param>
/// <param name="Bounds">Новые границы узла.</param>
public sealed record DesignResizeContext(UiNode Node, Rect Bounds);

/// <summary>
/// Контекст операции drag-and-drop в контейнере.
/// </summary>
/// <param name="ContainerNode">Узел контейнера.</param>
/// <param name="PointerPosition">Позиция указателя в координатах surface.</param>
public sealed record DesignDropContext(UiNode ContainerNode, Point PointerPosition);

/// <summary>
/// Определяет цель drop-операции в дизайнере.
/// </summary>
/// <param name="ContainerNode">Контейнер, принимающий узел.</param>
/// <param name="SlotName">Имя слота или свойства контейнера.</param>
/// <param name="InsertIndex">Индекс вставки в коллекцию, если применимо.</param>
public sealed record DesignerDropTarget(UiNode ContainerNode, string SlotName, int? InsertIndex = null);

/// <summary>
/// Определяет тип намерения размещения узла внутри контейнера.
/// </summary>
public enum DesignPlacementKind
{
    /// <summary>
    /// Размещение в одиночный слот Content.
    /// </summary>
    SetContent,

    /// <summary>
    /// Вставка в коллекцию дочерних элементов.
    /// </summary>
    InsertChild,

    /// <summary>
    /// Вставка с добавлением attached-layout свойств.
    /// </summary>
    SetAttachedLayout
}

/// <summary>
/// Контекст вычисления размещения узла в контейнере.
/// </summary>
/// <param name="ContainerNode">Контейнер назначения.</param>
/// <param name="PointerPosition">Позиция указателя в координатах surface.</param>
/// <param name="DraggedNode">Перемещаемый/добавляемый узел.</param>
/// <param name="ContainerBounds">Границы контейнера в координатах surface, если известны.</param>
/// <param name="ChildSlots">Слоты дочерних узлов контейнера, если доступны.</param>
public sealed record DesignPlacementContext(
    UiNode ContainerNode,
    Point PointerPosition,
    UiNode? DraggedNode = null,
    Rect? ContainerBounds = null,
    IReadOnlyList<DesignChildSlot>? ChildSlots = null);

/// <summary>
/// Слот дочернего узла контейнера, используемый для вычисления placement intent.
/// </summary>
/// <param name="Node">Дочерний узел.</param>
/// <param name="Index">Индекс в контейнерной коллекции.</param>
/// <param name="Bounds">Границы дочернего узла в координатах surface.</param>
public sealed record DesignChildSlot(
    UiNode Node,
    int Index,
    Rect Bounds);

/// <summary>
/// Намерение размещения узла в контейнере.
/// </summary>
/// <param name="Kind">Тип размещения.</param>
/// <param name="SlotName">Целевой слот/свойство контейнера.</param>
/// <param name="InsertIndex">Индекс вставки для коллекции, если применимо.</param>
/// <param name="AttachedProperties">Набор attached-свойств, применяемых при вставке.</param>
public sealed record DesignPlacementIntent(
    DesignPlacementKind Kind,
    string SlotName,
    int? InsertIndex = null,
    IReadOnlyDictionary<string, UiValue>? AttachedProperties = null);

/// <summary>
/// Визуальная подсказка для preview drop-операции.
/// </summary>
/// <param name="HighlightBounds">Границы подсветки контейнера/ячейки.</param>
/// <param name="InsertionLineStart">Начало линии вставки.</param>
/// <param name="InsertionLineEnd">Конец линии вставки.</param>
/// <param name="Label">Текст подсказки.</param>
public sealed record DesignPlacementVisualHint(
    Rect? HighlightBounds = null,
    Point? InsertionLineStart = null,
    Point? InsertionLineEnd = null,
    string? Label = null);

/// <summary>
/// Контекст применения вычисленного намерения размещения.
/// </summary>
/// <param name="ContainerNode">Контейнер назначения.</param>
/// <param name="ChildNode">Размещаемый узел.</param>
/// <param name="Intent">Вычисленное намерение размещения.</param>
public sealed record DesignApplyPlacementContext(
    UiNode ContainerNode,
    UiNode ChildNode,
    DesignPlacementIntent Intent);

/// <summary>
/// Запрос на размещение узла из drag-drop операции в дизайнере.
/// </summary>
/// <param name="ContainerNode">Контейнер назначения.</param>
/// <param name="Intent">Вычисленное намерение размещения.</param>
/// <param name="SerializedNode">Сериализованный шаблон узла (JSON).</param>
public sealed record DesignerPlacementRequest(
    UiNode ContainerNode,
    DesignPlacementIntent Intent,
    string SerializedNode);

/// <summary>
/// Контекст построения design surface.
/// </summary>
/// <param name="Zoom">Текущий масштаб surface.</param>
public sealed record DesignerSurfaceContext(double Zoom = 1.0);
