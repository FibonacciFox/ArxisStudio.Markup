using System.Collections.Generic;
using System.Text.Json;

namespace AvaloniaDesigner.Generator.Models
{
    // Вспомогательный класс для сбора информации о полях
    public class ControlInfo
    {
        public string Type { get; set; } = "";
        public string? Name { get; set; } = "";
    }

    // Класс для представления свойства (примитив или вложенный объект)
    public class PropertyModel
    {
        public string Type { get; set; } = ""; 
        // ВАЖНО: Это поле используется для ВРЕМЕННОЙ передачи имени 
        // контрола (если оно было найдено в Properties) между вызовами генератора.
        public string Name { get; set; } = ""; 
        public JsonElement? Value { get; set; } 
        public Dictionary<string, PropertyModel> Properties { get; set; } = new();
    }
    
    // Основной класс AvaloniaModel
    public class AvaloniaModel
    {
        public string FormName { get; set; } = "GeneratedView";
        public string NamespaceSuffix { get; set; } = "Views";
        public string ParentClassType { get; set; } = ""; 
        
        public Dictionary<string, PropertyModel> Properties { get; set; } = new(); 
    }
}