using System.Collections.Generic;
using System.Text.Json;


namespace AvaloniaDesigner.Generator.Models
{
    /// <summary>
    /// Корневая модель формы, десериализуемая из JSON.
    /// Содержит общие метаданные и структуру UI.
    /// </summary>
    public class FormModel
    {
        public string FormName { get; set; } = "GeneratedForm";
        public string NamespaceSuffix { get; set; } = "Forms";
        public RootContainerModel RootContainer { get; set; } = new RootContainerModel();
        public List<ControlModel> Controls { get; set; } = new List<ControlModel>();
    }

    /// <summary>
    /// Модель, описывающая корневой контейнер формы (например, Grid или StackPanel).
    /// </summary>
    public class RootContainerModel
    {
        public string Type { get; set; } = ""; // Полное имя типа (e.g., Avalonia.Controls.Grid)
        
        // Свойства контейнера. Используем JsonElement для отложенной обработки типов.
        public Dictionary<string, JsonElement> Properties { get; set; } = new Dictionary<string, JsonElement>();
        
        // Специфическое свойство для Grid
        public List<string>? RowDefinitions { get; set; } 
    }

    /// <summary>
    /// Модель, описывающая отдельный контрол.
    /// </summary>
    public class ControlModel
    {
        public string Type { get; set; } = ""; // Полное имя типа (e.g., Avalonia.Controls.Button)
        public string Name { get; set; } = ""; // Имя контрола, используемое для создания поля (e.g., _buttonSubmit)
        
        // Свойства контрола. Используем JsonElement для отложенной обработки типов.
        public Dictionary<string, JsonElement> Properties { get; set; } = new Dictionary<string, JsonElement>();
    }
}