using ArxisStudio.Designer.Models;
using System;
using System.Linq;
using ArxisStudio.Markup;

namespace ArxisStudio.Designer.Behaviors;

/// <summary>
/// Поведение размещения для StackPanel.
/// </summary>
internal sealed class StackPanelDesignContainerBehavior : PanelDesignContainerBehavior
{
    internal new static StackPanelDesignContainerBehavior Instance { get; } = new();

    private StackPanelDesignContainerBehavior()
    {
    }

    public override bool TryCreatePlacementIntent(DesignPlacementContext context, out DesignPlacementIntent intent)
    {
        var isHorizontal = string.Equals(
            (context.ContainerNode.Properties.TryGetValue("Orientation", out var orientationValue) &&
             orientationValue is ScalarValue { Value: string orientationText }
                ? orientationText
                : "Vertical"),
            "Horizontal",
            StringComparison.OrdinalIgnoreCase);

        var childSlots = context.ChildSlots?
            .OrderBy(slot => isHorizontal ? slot.Bounds.X : slot.Bounds.Y)
            .ToList();

        var insertIndex = childSlots?.Count ?? 0;
        if (childSlots != null)
        {
            foreach (var slot in childSlots)
            {
                var midpoint = isHorizontal
                    ? slot.Bounds.X + slot.Bounds.Width / 2.0
                    : slot.Bounds.Y + slot.Bounds.Height / 2.0;

                if ((isHorizontal && context.PointerPosition.X < midpoint) ||
                    (!isHorizontal && context.PointerPosition.Y < midpoint))
                {
                    insertIndex = slot.Index;
                    break;
                }
            }
        }

        intent = new DesignPlacementIntent(DesignPlacementKind.InsertChild, "Children", InsertIndex: insertIndex);
        return true;
    }

    public override DesignPlacementVisualHint? BuildPlacementVisualHint(DesignPlacementContext context, DesignPlacementIntent intent)
    {
        var isHorizontal = string.Equals(
            (context.ContainerNode.Properties.TryGetValue("Orientation", out var orientationValue) &&
             orientationValue is ScalarValue { Value: string orientationText }
                ? orientationText
                : "Vertical"),
            "Horizontal",
            StringComparison.OrdinalIgnoreCase);

        if (context.ContainerBounds == null)
        {
            return new DesignPlacementVisualHint(Label: "StackPanel insertion");
        }

        var containerBounds = context.ContainerBounds.Value;
        var slots = context.ChildSlots?
            .OrderBy(slot => isHorizontal ? slot.Bounds.X : slot.Bounds.Y)
            .ToList();
        var insertIndex = intent.InsertIndex ?? slots?.Count ?? 0;

        double lineCoordinate;
        if (slots == null || slots.Count == 0)
        {
            lineCoordinate = isHorizontal ? containerBounds.X : containerBounds.Y;
        }
        else if (insertIndex <= 0)
        {
            lineCoordinate = isHorizontal ? slots[0].Bounds.X : slots[0].Bounds.Y;
        }
        else if (insertIndex >= slots.Count)
        {
            var last = slots[slots.Count - 1];
            lineCoordinate = isHorizontal ? last.Bounds.Right : last.Bounds.Bottom;
        }
        else
        {
            var previous = slots[insertIndex - 1];
            var next = slots[insertIndex];
            lineCoordinate = isHorizontal
                ? (previous.Bounds.Right + next.Bounds.X) / 2.0
                : (previous.Bounds.Bottom + next.Bounds.Y) / 2.0;
        }

        var start = isHorizontal
            ? new Avalonia.Point(lineCoordinate, containerBounds.Y)
            : new Avalonia.Point(containerBounds.X, lineCoordinate);
        var end = isHorizontal
            ? new Avalonia.Point(lineCoordinate, containerBounds.Bottom)
            : new Avalonia.Point(containerBounds.Right, lineCoordinate);

        return new DesignPlacementVisualHint(
            HighlightBounds: containerBounds,
            InsertionLineStart: start,
            InsertionLineEnd: end,
            Label: "StackPanel insertion");
    }
}
