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
            object? convertedValue = null; 
            
            try
            {
                // 1. ПОИСК И УСТАНОВКА ATTACHED СВОЙСТВ (через статический метод Set*)
                Type targetType;
                if (propName.Contains("."))
                {
                    var setMethod = FindAttachedPropertySetMethod(propName);
                    if (setMethod != null)
                    {
                         // Тип для конвертации берется из второго параметра метода Set*
                         targetType = setMethod.GetParameters()[1].ParameterType; 
                         convertedValue = ConvertPrimitive(value, targetType); 
                         
                         if (convertedValue != null)
                         {
                             // Вызываем Canvas.SetLeft(control, convertedValue)
                             setMethod.Invoke(null, new object[] { control, convertedValue });
                             return; 
                         }
                    }
                }
                
                // 2. ОБРАБОТКА КОЛЛЕКЦИЙ
                if (value is JArray jArray)
                {
                    ApplyCollectionProperty(control, propName, jArray);
                    return; 
                }
                
                // 3. ОБРАБОТКА СЛОЖНЫХ ОБЪЕКТОВ И ВЛОЖЕННЫХ КОНТРОЛОВ (JObject)
                if (value is JObject jObject)
                {
                    if (jObject.ContainsKey("Binding"))
                    {
                        // Логика Binding будет здесь
                        return;
                    }

                    var nestedModel = jObject.ToObject<ControlModel>();
                    if (nestedModel == null || string.IsNullOrEmpty(nestedModel.Type)) return;
                    
                    var complexObject = CreateComplexObject(nestedModel);
                    if (complexObject == null) return;
                    
                    SetComplexProperty(control, propName, complexObject);
                    return;
                }
                
                // 4. ОБРАБОТКА СТАНДАРТНЫХ СВОЙСТВ 
                
                var avaloniaProp = AvaloniaPropertyRegistry.Instance.FindRegistered(control, propName);

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
                
                convertedValue = ConvertPrimitive(value, targetType); 
                
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

        private static MethodInfo? FindAttachedPropertySetMethod(string propName)
        {
            int lastDotIndex = propName.LastIndexOf('.');
            if (lastDotIndex == -1) return null;

            string ownerTypeNameFull = propName.Substring(0, lastDotIndex);
            string propertyName = propName.Substring(lastDotIndex + 1);
            string setMethodName = "Set" + propertyName; 

            Type? ownerType = FindType(ownerTypeNameFull); 
            
            if (ownerType == null) return null;

            var candidateMethods = ownerType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == setMethodName)
                .Where(m => m.GetParameters().Length == 2)
                .Where(m => typeof(AvaloniaObject).IsAssignableFrom(m.GetParameters()[0].ParameterType))
                .FirstOrDefault();

            return candidateMethods;
        }

        private static void ApplyCollectionProperty(object parentObject, string propName, JArray jArray)
        {
            var collectionProp = parentObject.GetType().GetProperty(propName);
            if (collectionProp == null) return;

            var collection = collectionProp.GetValue(parentObject);
            if (collection == null) return;

            var addMethod = collection.GetType().GetMethod("Add");
            if (addMethod == null) return;
            
            foreach (var jToken in jArray)
            {
                object? builtItem = null;
                
                if (jToken is JObject)
                {
                    var childModel = jToken.ToObject<ControlModel>();
                    if (childModel != null)
                    {
                        builtItem = CreateComplexObject(childModel);
                    }
                }
                else
                {
                    var collectionType = addMethod.GetParameters().FirstOrDefault()?.ParameterType ?? typeof(object);
                    builtItem = ConvertPrimitive(jToken.Value<object>()!, collectionType);
                }

                if (builtItem != null)
                {
                    addMethod.Invoke(collection, new[] { builtItem });
                }
            }
        }

        private static object? CreateComplexObject(ControlModel model)
        {
            if (typeof(Control).IsAssignableFrom(FindType(model.Type)))
            {
                return Build(model);
            }

            var complexType = FindType(model.Type);
            if (complexType == null) return null;
            
            var complexObject = Activator.CreateInstance(complexType);
            if (complexObject == null) return null;
            
            if (model.Properties != null)
            {
                foreach(var nestedProp in model.Properties)
                {
                    if (nestedProp.Value is JArray jArray)
                    {
                        ApplyCollectionProperty(complexObject, nestedProp.Key, jArray);
                        continue;
                    }
                    
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

        private static Type? FindType(string? typeName)
        {
            if (_typeCache.TryGetValue(typeName, out var type)) return type;

            Type? typeFound = null;

            // 1. Поиск по EXACT имени
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                typeFound = asm.GetType(typeName);
                if (typeFound != null)
                {
                    _typeCache[typeName] = typeFound;
                    return typeFound;
                }
            }

            // 2. Поиск с общими префиксами
            string[] commonPrefixes = 
            {
                "Avalonia.Controls.",
                "Avalonia.Controls.Shapes.",
                "Avalonia.Media."
            };

            foreach (var prefix in commonPrefixes)
            {
                string prefixedName = prefix + typeName;
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    typeFound = asm.GetType(prefixedName);
                    if (typeFound != null)
                    {
                        _typeCache[typeName] = typeFound;
                        return typeFound;
                    }
                }
            }
            
            return null;
        }
    }
}