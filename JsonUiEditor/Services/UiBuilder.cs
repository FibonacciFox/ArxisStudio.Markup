using Avalonia.Controls;
using Avalonia.Media;
using Avalonia;
using System;
using System.Linq;
using System.Reflection;
using JsonUiEditor.Models;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Collections.Generic;

namespace JsonUiEditor.Services
{
    public static class UiBuilder
    {
        private static readonly Dictionary<string, Type> _typeCache = new();

        public static Control Build(ControlModel model)
        {
            var controlType = FindType(model.Type);
            if (controlType == null)
                return new TextBlock { Text = $"Error: Type '{model.Type}' not found", Foreground = Brushes.Red };

            var control = (Control)Activator.CreateInstance(controlType)!;

            if (model.Properties != null)
            {
                foreach (var prop in model.Properties)
                {
                    ApplyProperty(control, prop.Key, prop.Value);
                }
            }
            
            return control;
        }

        private static void ApplyProperty(Control control, string propName, object value)
        {
            try
            {
                // 1. УНИВЕРСАЛЬНАЯ ОБРАБОТКА КОЛЛЕКЦИЙ (Children, Items, RowDefinitions)
                if (value is JArray jArray)
                {
                    ApplyCollectionProperty(control, propName, jArray);
                    return; 
                }
                
                // 2. ОБРАБОТКА СЛОЖНЫХ ОБЪЕКТОВ И ВЛОЖЕННЫХ КОНТРОЛОВ (JObject)
                if (value is JObject jObject)
                {
                    var nestedModel = jObject.ToObject<ControlModel>();
                    if (nestedModel == null || string.IsNullOrEmpty(nestedModel.Type)) return;
                    
                    var complexObject = CreateComplexObject(nestedModel);
                    if (complexObject == null) return;
                    
                    SetComplexProperty(control, propName, complexObject);
                    return;
                }
                
                // 3. ОБРАБОТКА ПРИМИТИВНЫХ СВОЙСТВ (Text, Width, BorderThickness)
                
                var avaloniaProp = AvaloniaPropertyRegistry.Instance.FindRegistered(control, propName);
                
                Type targetType;
                if (avaloniaProp != null)
                {
                    targetType = avaloniaProp.PropertyType;
                }
                else
                {
                    var propInfo = control.GetType().GetProperty(propName);
                    if (propInfo == null || !propInfo.CanWrite) return;
                    targetType = propInfo.PropertyType;
                }
                
                object? convertedValue = ConvertPrimitive(value, targetType);
                if (convertedValue != null)
                {
                    if (avaloniaProp != null)
                        control.SetValue(avaloniaProp, convertedValue);
                    else
                        control.GetType().GetProperty(propName)?.SetValue(control, convertedValue);
                }
            }
            catch (Exception ex)
            {
                // Логгирование
            }
        }
        
        /// <summary>
        /// Универсально находит свойство-коллекцию и добавляет в него элементы из JArray.
        /// </summary>
        private static void ApplyCollectionProperty(Control control, string propName, JArray jArray)
        {
            // 1. Найти свойство-коллекцию по имени (например, ListBox.Items или Grid.RowDefinitions)
            var collectionProp = control.GetType().GetProperty(propName);
            if (collectionProp == null) return;

            // 2. Получить экземпляр коллекции
            var collection = collectionProp.GetValue(control);
            if (collection == null) return;

            // 3. Найти метод Add() в коллекции через Reflection
            var addMethod = collection.GetType().GetMethod("Add");
            if (addMethod == null) return;
            
            // Определяем, что за элемент ждет коллекция (ControlModel или примитив)
            var collectionType = addMethod.GetParameters().FirstOrDefault()?.ParameterType;

            // 4. Построить и добавить каждый элемент
            foreach (var jToken in jArray)
            {
                object? builtItem = null;
                
                if (jToken is JObject)
                {
                    // Если элемент — сложный объект (Control, GradientStop и т.д.)
                    var childModel = jToken.ToObject<ControlModel>();
                    if (childModel != null)
                    {
                        builtItem = CreateComplexObject(childModel);
                    }
                }
                else
                {
                    // Если элемент — примитив (например, ItemsSource = ["a", "b"])
                    builtItem = ConvertPrimitive(jToken.Value<object>()!, collectionType ?? typeof(object));
                }

                if (builtItem != null)
                {
                    // Вызвать collection.Add(builtItem)
                    addMethod.Invoke(collection, new[] { builtItem });
                }
            }
        }

        private static object? CreateComplexObject(ControlModel model)
        {
            // Если это контрол, вызываем главную функцию рекурсивно
            if (typeof(Control).IsAssignableFrom(FindType(model.Type)))
            {
                return Build(model);
            }

            // Если это сложный тип (Brush, Thickness, GradientStop)
            var complexType = FindType(model.Type);
            if (complexType == null) return null;
            
            var complexObject = Activator.CreateInstance(complexType);
            if (complexObject == null) return null;
            
            // Устанавливаем свойства ComplexObject (Color, Opacity, StartPoint)
            if (model.Properties != null)
            {
                foreach(var nestedProp in model.Properties)
                {
                    var objPropInfo = complexObject.GetType().GetProperty(nestedProp.Key);
                    if (objPropInfo != null && objPropInfo.CanWrite)
                    {
                        object? convertedValue = ConvertPrimitive(nestedProp.Value, objPropInfo.PropertyType);
                        objPropInfo.SetValue(complexObject, convertedValue);
                    }
                }
            }
            return complexObject;
        }

        private static void SetComplexProperty(Control control, string propName, object complexObject)
        {
            var propInfo = control.GetType().GetProperty(propName);
            if (propInfo != null && propInfo.CanWrite)
            {
                propInfo.SetValue(control, complexObject);
            }
        }

        private static object? ConvertPrimitive(object value, Type targetType)
        {
            string strValue = value.ToString()!;
            
            if (targetType.IsEnum)
            {
                 return Enum.Parse(targetType, strValue);
            }
            
            var parser = targetType.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
            if (parser != null)
            {
                return parser.Invoke(null, new object[] { strValue });
            }

            var converter = TypeDescriptor.GetConverter(targetType);
            if (converter.CanConvertFrom(typeof(string)))
            {
                return converter.ConvertFrom(strValue);
            }

            if (targetType == typeof(string)) return strValue;
            if (targetType == typeof(double)) return double.Parse(strValue);
            if (targetType == typeof(int)) return int.Parse(strValue);
            if (targetType == typeof(bool)) return bool.Parse(strValue);

            return value; 
        }

        private static Type? FindType(string typeName)
        {
            if (_typeCache.TryGetValue(typeName, out var type)) return type;

            string normalizedName = typeName;
            if (!normalizedName.Contains("."))
                normalizedName = "Avalonia.Controls." + typeName;

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var typeFound = asm.GetType(normalizedName);
                if (typeFound != null)
                {
                    _typeCache[typeName] = typeFound;
                    return typeFound;
                }
            }
            return null;
        }
    }
}