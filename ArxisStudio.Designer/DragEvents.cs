using Avalonia.Interactivity;

namespace ArxisStudio.Designer;

public class DragStartedEventArgs : RoutedEventArgs
{
    public DragStartedEventArgs(double horizontalOffset, double verticalOffset)
    {
        HorizontalOffset = horizontalOffset;
        VerticalOffset = verticalOffset;
    }

    public double HorizontalOffset { get; }

    public double VerticalOffset { get; }
}

public class DragDeltaEventArgs : RoutedEventArgs
{
    public DragDeltaEventArgs(double horizontalChange, double verticalChange)
    {
        HorizontalChange = horizontalChange;
        VerticalChange = verticalChange;
    }

    public double HorizontalChange { get; }

    public double VerticalChange { get; }
}

public class DragCompletedEventArgs : RoutedEventArgs
{
    public DragCompletedEventArgs(double horizontalChange, double verticalChange, bool canceled)
    {
        HorizontalChange = horizontalChange;
        VerticalChange = verticalChange;
        Canceled = canceled;
    }

    public double HorizontalChange { get; }

    public double VerticalChange { get; }

    public bool Canceled { get; }
}
