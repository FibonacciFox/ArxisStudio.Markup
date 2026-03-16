using System.Collections.Generic;
using System;
using System.Linq;
using ArxisStudio.Designer.Models;
using ArxisStudio.Markup;

namespace ArxisStudio.Designer.Behaviors;

/// <summary>
/// Поведение размещения для Grid.
/// </summary>
internal sealed class GridDesignContainerBehavior : PanelDesignContainerBehavior
{
    internal new static GridDesignContainerBehavior Instance { get; } = new();

    private GridDesignContainerBehavior()
    {
    }

    public override bool TryCreatePlacementIntent(DesignPlacementContext context, out DesignPlacementIntent intent)
    {
        var containerBounds = context.ContainerBounds;
        var rows = ResolveGridDimensionCount(context, "RowDefinitions", "Avalonia.Controls.Grid.Row");
        var columns = ResolveGridDimensionCount(context, "ColumnDefinitions", "Avalonia.Controls.Grid.Column");

        var row = 0;
        var column = 0;
        if (containerBounds.HasValue)
        {
            var bounds = containerBounds.Value;
            var cellHeight = Math.Max(1.0, bounds.Height / rows);
            var cellWidth = Math.Max(1.0, bounds.Width / columns);

            var localY = Clamp(context.PointerPosition.Y - bounds.Y, 0, Math.Max(0, bounds.Height - 1));
            var localX = Clamp(context.PointerPosition.X - bounds.X, 0, Math.Max(0, bounds.Width - 1));

            row = Clamp((int)(localY / cellHeight), 0, rows - 1);
            column = Clamp((int)(localX / cellWidth), 0, columns - 1);
        }

        var attached = new Dictionary<string, UiValue>
        {
            ["Avalonia.Controls.Grid.Row"] = new ScalarValue(row),
            ["Avalonia.Controls.Grid.Column"] = new ScalarValue(column)
        };

        intent = new DesignPlacementIntent(DesignPlacementKind.SetAttachedLayout, "Children", null, attached);
        return true;
    }

    public override DesignPlacementVisualHint? BuildPlacementVisualHint(DesignPlacementContext context, DesignPlacementIntent intent)
    {
        if (context.ContainerBounds == null)
        {
            return new DesignPlacementVisualHint(Label: "Grid cell");
        }

        var bounds = context.ContainerBounds.Value;
        var rows = ResolveGridDimensionCount(context, "RowDefinitions", "Avalonia.Controls.Grid.Row");
        var columns = ResolveGridDimensionCount(context, "ColumnDefinitions", "Avalonia.Controls.Grid.Column");

        var row = ReadAttachedInt(intent, "Avalonia.Controls.Grid.Row");
        var column = ReadAttachedInt(intent, "Avalonia.Controls.Grid.Column");

        var cellWidth = Math.Max(1.0, bounds.Width / columns);
        var cellHeight = Math.Max(1.0, bounds.Height / rows);
        var cellBounds = new Avalonia.Rect(
            bounds.X + column * cellWidth,
            bounds.Y + row * cellHeight,
            cellWidth,
            cellHeight);

        return new DesignPlacementVisualHint(
            HighlightBounds: cellBounds,
            Label: $"Grid cell ({row},{column})");
    }

    private static int ResolveGridDimensionCount(DesignPlacementContext context, string definitionsPropertyName, string attachedPropertyName)
    {
        var containerNode = context.ContainerNode;
        if (containerNode.Properties.TryGetValue(definitionsPropertyName, out var definitionsValue) &&
            definitionsValue is CollectionValue { Items.Count: > 0 } definitionsCollection)
        {
            return definitionsCollection.Items.Count;
        }

        var attachedValues = (context.ChildSlots ?? Array.Empty<DesignChildSlot>())
            .Select(slot => slot.Node)
            .Where(node => node.Properties.TryGetValue(attachedPropertyName, out _))
            .Select(node => node.Properties[attachedPropertyName])
            .OfType<ScalarValue>()
            .Select(value => Convert.ToString(value.Value))
            .Select(text => int.TryParse(text, out var parsed) ? parsed : 0)
            .ToList();

        if (attachedValues.Count > 0)
        {
            return Math.Max(1, attachedValues.Max() + 1);
        }

        return 1;
    }

    private static int ReadAttachedInt(DesignPlacementIntent intent, string propertyName)
    {
        if (intent.AttachedProperties != null &&
            intent.AttachedProperties.TryGetValue(propertyName, out var value) &&
            value is ScalarValue scalar &&
            int.TryParse(Convert.ToString(scalar.Value), out var parsed))
        {
            return parsed;
        }

        return 0;
    }

    private static double Clamp(double value, double min, double max)
    {
        if (value < min)
        {
            return min;
        }

        if (value > max)
        {
            return max;
        }

        return value;
    }

    private static int Clamp(int value, int min, int max)
    {
        if (value < min)
        {
            return min;
        }

        if (value > max)
        {
            return max;
        }

        return value;
    }
}
