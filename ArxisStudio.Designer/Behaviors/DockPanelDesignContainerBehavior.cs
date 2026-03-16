using System.Collections.Generic;
using ArxisStudio.Designer.Models;
using ArxisStudio.Markup;

namespace ArxisStudio.Designer.Behaviors;

/// <summary>
/// Поведение размещения для DockPanel.
/// </summary>
internal sealed class DockPanelDesignContainerBehavior : PanelDesignContainerBehavior
{
    internal new static DockPanelDesignContainerBehavior Instance { get; } = new();

    private DockPanelDesignContainerBehavior()
    {
    }

    public override bool TryCreatePlacementIntent(DesignPlacementContext context, out DesignPlacementIntent intent)
    {
        var attached = new Dictionary<string, UiValue>
        {
            ["Avalonia.Controls.DockPanel.Dock"] = new ScalarValue("Left")
        };

        intent = new DesignPlacementIntent(DesignPlacementKind.SetAttachedLayout, "Children", null, attached);
        return true;
    }

    public override DesignPlacementVisualHint? BuildPlacementVisualHint(DesignPlacementContext context, DesignPlacementIntent intent)
    {
        return new DesignPlacementVisualHint(Label: "DockPanel edge");
    }
}
