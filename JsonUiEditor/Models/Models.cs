using System.Collections.Generic;

namespace JsonUiEditor.Models
{
    // Аналог того, что мы делали для генератора, но проще
    public class ControlModel
    {
        public string Type { get; set; } = "";
        
        // Свойства контрола (Width, Background и т.д.)
        public Dictionary<string, object> Properties { get; set; } = new();
        
        // Для контейнеров (Panel, StackPanel) - список детей
        public List<ControlModel>? Children { get; set; }
        
        // Для ContentControl (Border, ScrollViewer) - один ребенок
        public ControlModel? Content { get; set; }
    }
    
    public class RootModel
    {
        // Обертка, если в JSON есть метаданные формы, но нам нужен только контент
        public ControlModel? Content { get; set; }
        // Или если JSON сразу описывает контрол, можно использовать ControlModel напрямую
    }
}