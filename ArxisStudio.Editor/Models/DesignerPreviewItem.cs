using Avalonia;
using Avalonia.Controls;

namespace ArxisStudio.Editor.Models;

public sealed class DesignerPreviewItem
{
    public DesignerPreviewItem(Control content, Point location, double width, double height)
    {
        Content = content;
        Location = location;
        Width = width;
        Height = height;
    }

    public Control Content { get; }

    public Point Location { get; }

    public double Width { get; }

    public double Height { get; }
}
