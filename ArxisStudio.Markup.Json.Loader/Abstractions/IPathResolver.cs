namespace ArxisStudio.Markup.Json.Loader.Abstractions;

/// <summary>
/// Разрешает пути внешних ресурсов (<c>.axaml</c>, ассеты) для loader.
/// </summary>
public interface IPathResolver
{
    /// <summary>
    /// Пытается разрешить путь ресурса в локальный путь файловой системы.
    /// </summary>
    /// <param name="path">Путь или URI ресурса.</param>
    /// <param name="assemblyName">Имя сборки, если путь привязан к сборке.</param>
    /// <returns>Локальный путь к файлу или <see langword="null"/>, если разрешить путь невозможно.</returns>
    string? ResolvePath(string path, string? assemblyName = null);
}
