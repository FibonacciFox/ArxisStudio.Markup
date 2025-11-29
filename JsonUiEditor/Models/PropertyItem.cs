using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json.Linq;
using JsonUiEditor.Services; // Для доступа к DesignerService

namespace JsonUiEditor.Models
{
    public partial class PropertyItem : ObservableObject
    {
        public string Name { get; } 

        // Ссылка на JSON-объект, содержащий словарь "Properties"
        public JObject ParentPropertiesObject { get; }

        // Значение, которое редактируется в панели
        [ObservableProperty]
        private object? _value; 

        public PropertyItem(string name, object? initialValue, JObject parent)
        {
            Name = name;
            _value = initialValue;
            ParentPropertiesObject = parent;
        }

        // Вызывается при изменении значения в UI-панели свойств
        partial void OnValueChanged(object? oldValue, object? newValue)
        {
            if (ParentPropertiesObject.ContainsKey(Name) && newValue != null)
            {
                // 1. Обновление JSON
                ParentPropertiesObject[Name] = JToken.FromObject(newValue);
                
                // 2. Оповещение сервиса о том, что JSON изменился, для перерисовки UI.
                DesignerService.Instance.NotifyJsonChanged(); 
            }
        }
    }
}