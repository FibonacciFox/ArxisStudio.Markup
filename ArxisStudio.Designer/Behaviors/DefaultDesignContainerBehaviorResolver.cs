using ArxisStudio.Markup;
using System;

namespace ArxisStudio.Designer.Behaviors;

/// <summary>
/// Разрешатель контейнерного поведения по умолчанию.
/// </summary>
public sealed class DefaultDesignContainerBehaviorResolver : IDesignContainerBehaviorResolver
{
    /// <summary>
    /// Экземпляр разрешателя по умолчанию.
    /// </summary>
    public static DefaultDesignContainerBehaviorResolver Instance { get; } = new();

    private DefaultDesignContainerBehaviorResolver()
    {
    }

    /// <inheritdoc />
    public IDesignContainerBehavior Resolve(UiNode? containerNode)
    {
        var typeName = containerNode?.TypeName;
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return DefaultDesignContainerBehavior.Instance;
        }

        var normalizedTypeName = typeName!;

        return normalizedTypeName switch
        {
            "Avalonia.Controls.Canvas" => CanvasDesignContainerBehavior.Instance,
            "Avalonia.Controls.Grid" => GridDesignContainerBehavior.Instance,
            "Avalonia.Controls.StackPanel" => StackPanelDesignContainerBehavior.Instance,
            "Avalonia.Controls.DockPanel" => DockPanelDesignContainerBehavior.Instance,
            "Avalonia.Controls.UserControl" => ContentControlDesignContainerBehavior.Instance,
            "Avalonia.Controls.ContentControl" => ContentControlDesignContainerBehavior.Instance,
            "Avalonia.Controls.Border" => ContentControlDesignContainerBehavior.Instance,
            _ => normalizedTypeName.EndsWith("Panel", StringComparison.Ordinal) ? PanelDesignContainerBehavior.Instance : DefaultDesignContainerBehavior.Instance
        };
    }
}
