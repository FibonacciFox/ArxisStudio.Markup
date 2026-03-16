using System.IO;

namespace ArxisStudio.Editor.Models;

public sealed class OpenDocumentTab
{
    public OpenDocumentTab(string fullPath)
    {
        FullPath = fullPath;
    }

    public string FullPath { get; }

    public string Title => Path.GetFileName(FullPath);
}
