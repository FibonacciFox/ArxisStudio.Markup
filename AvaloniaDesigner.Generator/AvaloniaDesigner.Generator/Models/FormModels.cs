using System.Collections.Generic;
using System.Text.Json;


namespace AvaloniaDesigner.Generator.Models
{
    public class FormModel
    {
        public string FormName { get; set; } = "GeneratedForm";
        public string NamespaceSuffix { get; set; } = "Forms";
        
        // НОВОЕ СВОЙСТВО: Базовый класс для генерируемого partial класса
        public string ParentClassType { get; set; } = "Avalonia.Controls.UserControl"; 
        
        public RootContainerModel RootContainer { get; set; } = new RootContainerModel();
        public List<ControlModel> Controls { get; set; } = new List<ControlModel>();
    }

    public class RootContainerModel
    {
        public string Type { get; set; } = ""; // Полное имя типа (e.g., Avalonia.Controls.Grid)
        public Dictionary<string, JsonElement> Properties { get; set; } = new Dictionary<string, JsonElement>();
    }

    public class ControlModel
    {
        public string Type { get; set; } = ""; // Полное имя типа (e.g., Avalonia.Controls.Button)
        public string Name { get; set; } = ""; // Имя контрола
        public Dictionary<string, JsonElement> Properties { get; set; } = new Dictionary<string, JsonElement>();
    }
}