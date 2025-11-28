using System.Collections.Generic;
using System.Text.Json;


namespace AvaloniaDesigner.Generator.Models
{
    public class AvaloniaModel
    {
        public string FormName { get; set; } = "GeneratedView";
        public string NamespaceSuffix { get; set; } = "Views";
        public string ParentClassType { get; set; } = ""; 
        
        // Новое свойство для свойств самой формы (this.Width, this.Title и т.д.)
        public Dictionary<string, JsonElement> Properties { get; set; } = new(); 

        public RootContainerModel RootContainer { get; set; } = new();
        public List<ControlModel> Controls { get; set; } = new();
    }

    public class RootContainerModel
    {
        public string Type { get; set; } = ""; // Полное имя типа (e.g., Avalonia.Controls.Grid)
        public Dictionary<string, JsonElement> Properties { get; set; } = new();
    }

    public class ControlModel
    {
        public string Type { get; set; } = ""; // Полное имя типа (e.g., Avalonia.Controls.Button)
        public string Name { get; set; } = ""; // Имя контрола
        public Dictionary<string, JsonElement> Properties { get; set; } = new();
    }
}