using System.Collections.Generic;

namespace JsonUiEditor.Models
{
    /// <summary>
    /// Представляет любой контрол или сложный объект (Brush, Thickness),
    /// который задан через Type (поскольку Properties имеет нестандартную структуру).
    /// </summary>
    public class ControlModel
    {
        // Обязательное поле для определения типа Avalonia
        public string Type { get; set; } = "";
        
        // Свойства этого контрола или сложного объекта. 
        // Может содержать примитивы (string, int) ИЛИ вложенные ControlModel (JObject).
        public Dictionary<string, object>? Properties { get; set; }
        
        // Коллекции (например, Children для Panel)
        public List<ControlModel>? Children { get; set; }
    }
    
    /// <summary>
    /// Представляет корневую структуру JSON-файла.
    /// </summary>
    public class RootModel
    {
        public string? FormName { get; set; }
        public string? NamespaceSuffix { get; set; }
        public string? ParentClassType { get; set; }
        
        // Словарь Properties корневого контрола (в нем будет ключ "Content")
        public Dictionary<string, object>? Properties { get; set; }
    }
}