using System.Collections.ObjectModel;

namespace ArxisStudio.Editor.Models;

public sealed class ProjectTreeItem
{
    public ProjectTreeItem(string name, string fullPath, bool isDirectory, string? kind = null)
    {
        Name = name;
        FullPath = fullPath;
        IsDirectory = isDirectory;
        Kind = kind;
    }

    public string Name { get; }

    public string FullPath { get; }

    public bool IsDirectory { get; }

    public string? Kind { get; }

    public string KindLabel => IsDirectory ? "folder" : string.IsNullOrWhiteSpace(Kind) ? "file" : Kind!;

    public string Badge
    {
        get
        {
            if (IsDirectory)
            {
                return "DIR";
            }

            return KindLabel.ToUpperInvariant() switch
            {
                "ARXUI" => "UI",
                "AXAML" => "XAML",
                "CS" => "CS",
                "CSPROJ" => "PROJ",
                "SLN" => "SLN",
                "JSON" => "JSON",
                _ => "FILE"
            };
        }
    }

    public ObservableCollection<ProjectTreeItem> Children { get; } = new();
}
