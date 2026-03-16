using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using ArxisStudio.Designer.Models;
using ArxisStudio.Markup;

namespace ArxisStudio.Designer.Controls;

/// <summary>
/// Рисует design-time adorners поверх preview-сцены.
/// </summary>
public sealed class DesignerAdornerLayer : Control
{
    private static readonly Pen SelectionPen = new(SolidColorBrush.Parse("#4EA1F3"), 1);
    private static readonly Pen PlacementPen = new(SolidColorBrush.Parse("#61D095"), 1, dashStyle: new DashStyle(new[] { 4d, 3d }, 0));
    private static readonly IBrush HandleBrush = Brushes.White;
    private static readonly Size HandleSize = new(8, 8);

    /// <summary>
    /// Возвращает или задаёт preview-сцену, для которой рисуются adorners.
    /// </summary>
    public DesignerPreviewScene? Scene
    {
        get => _scene;
        set
        {
            _scene = value;
            InvalidateVisual();
        }
    }
    private DesignerPreviewScene? _scene;

    /// <summary>
    /// Возвращает или задаёт выбранный узел.
    /// </summary>
    public UiNode? SelectedNode
    {
        get => _selectedNode;
        set
        {
            _selectedNode = value;
            InvalidateVisual();
        }
    }
    private UiNode? _selectedNode;

    /// <summary>
    /// Возвращает или задаёт подсказку размещения для drag-drop.
    /// </summary>
    public DesignPlacementVisualHint? PlacementHint
    {
        get => _placementHint;
        set
        {
            _placementHint = value;
            InvalidateVisual();
        }
    }
    private DesignPlacementVisualHint? _placementHint;

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (Scene == null || SelectedNode == null || !Scene.TryGetBounds(SelectedNode, out var bounds))
        {
            return;
        }

        context.DrawRectangle(null, SelectionPen, bounds);

        foreach (var handle in GetHandleRects(bounds))
        {
            context.DrawRectangle(HandleBrush, SelectionPen, handle);
        }

        if (PlacementHint?.HighlightBounds is Rect highlightBounds)
        {
            context.DrawRectangle(null, PlacementPen, highlightBounds);
        }

        if (PlacementHint?.InsertionLineStart is Point start && PlacementHint.InsertionLineEnd is Point end)
        {
            context.DrawLine(PlacementPen, start, end);
        }
    }

    private static Rect[] GetHandleRects(Rect bounds)
    {
        var halfWidth = HandleSize.Width / 2;
        var halfHeight = HandleSize.Height / 2;

        Rect Create(double x, double y) => new(x - halfWidth, y - halfHeight, HandleSize.Width, HandleSize.Height);

        return
        [
            Create(bounds.Left, bounds.Top),
            Create(bounds.Right, bounds.Top),
            Create(bounds.Left, bounds.Bottom),
            Create(bounds.Right, bounds.Bottom)
        ];
    }
}
