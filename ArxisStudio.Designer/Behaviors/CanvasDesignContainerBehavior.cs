using System.Collections.Generic;
using ArxisStudio.Designer.Models;
using ArxisStudio.Markup;

namespace ArxisStudio.Designer.Behaviors;

/// <summary>
/// Поведение размещения для Canvas.
/// </summary>
internal sealed class CanvasDesignContainerBehavior : PanelDesignContainerBehavior
{
    internal new static CanvasDesignContainerBehavior Instance { get; } = new();

    private CanvasDesignContainerBehavior()
    {
    }

    public override bool TryCreatePlacementIntent(DesignPlacementContext context, out DesignPlacementIntent intent)
    {
        var attached = new Dictionary<string, UiValue>
        {
            ["Avalonia.Controls.Canvas.Left"] = new ScalarValue(context.PointerPosition.X),
            ["Avalonia.Controls.Canvas.Top"] = new ScalarValue(context.PointerPosition.Y)
        };

        intent = new DesignPlacementIntent(DesignPlacementKind.SetAttachedLayout, "Children", null, attached);
        return true;
    }

    public override DesignPlacementVisualHint? BuildPlacementVisualHint(DesignPlacementContext context, DesignPlacementIntent intent)
    {
        return new DesignPlacementVisualHint(Label: "Canvas placement");
    }
}
