using ArxisStudio.Markup.Json.Loader.Abstractions;

namespace ArxisStudio.Markup.Json.Loader.Services;

/// <summary>
/// Реализация <see cref="IPathResolver"/> на основе project-directory и assembly-name.
/// </summary>
public sealed class ProjectContextPathResolver : IPathResolver
{
    private readonly string _projectDirectory;
    private readonly string _assemblyName;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="ProjectContextPathResolver"/>.
    /// </summary>
    public ProjectContextPathResolver(string projectDirectory, string assemblyName)
    {
        _projectDirectory = projectDirectory;
        _assemblyName = assemblyName;
    }

    /// <inheritdoc />
    public string? ResolvePath(string path, string? assemblyName = null)
    {
        return ProjectPathResolver.ResolveProjectRelativePath(path, assemblyName, _projectDirectory, _assemblyName);
    }
}
