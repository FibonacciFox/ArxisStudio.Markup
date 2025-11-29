using Avalonia.Controls;
using Avalonia.Media;
using Avalonia;
using System;
using JsonUiEditor.Models;
using Newtonsoft.Json.Linq;

namespace JsonUiEditor.Services
{
    public static class UiBuilder
    {
        public static Control Build(ControlModel model)
        {
            // 1. Создание экземпляра контрола по имени типа
            var controlType = FindType(model.Type);
            if (controlType == null)
                return new TextBlock { Text = $"Error: Type '{model.Type}' not found", Foreground = Brushes.Red };

            var control = (Control)Activator.CreateInstance(controlType)!;

            // 2. Применение свойств
            foreach (var prop in model.Properties)
            {
                ApplyProperty(control, prop.Key, prop.Value);
            }

            // 3. Рекурсия: Обработка Children (для панелей)
            if (model.Children != null && control is Panel panel)
            {
                foreach (var childModel in model.Children)
                {
                    panel.Children.Add(Build(childModel));
                }
            }

            // 4. Рекурсия: Обработка Content (для Border, Button, etc.)
            if (model.Content != null && control is ContentControl contentControl)
            {
                contentControl.Content = Build(model.Content);
            }

            return control;
        }

        private static void ApplyProperty(Control control, string propName, object value)
        {
            try
            {
                // Находим свойство C# через рефлексию
                var propInfo = control.GetType().GetProperty(propName);
                if (propInfo == null || !propInfo.CanWrite) return;

                object? convertedValue = null;
                string strValue = value.ToString()!;

                // Если значение из JSON пришло как JObject (сложный объект), пока пропустим или возьмем ToString
                if (value is JObject) return; 

                // --- ЛОГИКА КОНВЕРТАЦИИ ---
                
                // 1. Если свойство Brush (Цвет)
                if (typeof(IBrush).IsAssignableFrom(propInfo.PropertyType))
                {
                    convertedValue = Brush.Parse(strValue);
                }
                // 2. Если свойство Thickness (Margin, Padding)
                else if (propInfo.PropertyType == typeof(Thickness))
                {
                    convertedValue = Thickness.Parse(strValue);
                }
                // 3. Простые типы (double, int, bool, string)
                else
                {
                    // Используем Convert.ChangeType для базовых типов
                    convertedValue = Convert.ChangeType(strValue, propInfo.PropertyType);
                }

                if (convertedValue != null)
                {
                    propInfo.SetValue(control, convertedValue);
                }
            }
            catch
            {
                // Игнорируем ошибки конвертации отдельных свойств, чтобы не ломать весь UI
            }
        }

        // Поиск типа в загруженных сборках Avalonia
        private static Type? FindType(string typeName)
        {
            // Упрощенный поиск. Пробуем добавить префикс Avalonia.Controls, если его нет
            if (!typeName.Contains("."))
                typeName = "Avalonia.Controls." + typeName;

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = asm.GetType(typeName);
                if (type != null) return type;
            }
            return null;
        }
    }
}