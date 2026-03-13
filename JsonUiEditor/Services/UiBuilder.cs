using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using AvaloniaDesigner.Contracts;

namespace JsonUiEditor.Services
{
    public static class UiBuilder
    {
        private static readonly Dictionary<string, Type> TypeCache = new();

        public static Control Build(PropertyModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Type))
            {
                return new TextBlock { Text = "Error: Root control type is missing", Foreground = Brushes.Red };
            }

            var controlType = FindType(model.Type);
            if (controlType == null)
            {
                return new TextBlock { Text = $"Error: Type '{model.Type}' not found", Foreground = Brushes.Red };
            }

            var control = (Control)Activator.CreateInstance(controlType)!;

            foreach (var property in model.Properties)
            {
                ApplyProperty(control, property.Key, property.Value);
            }

            return control;
        }

        private static void ApplyProperty(Control control, string propertyName, PropertyModel model)
        {
            try
            {
                if (propertyName.Contains("."))
                {
                    ApplyAttachedProperty(control, propertyName, model);
                    return;
                }

                if (model.Items is { Count: > 0 })
                {
                    ApplyCollectionProperty(control, propertyName, model.Items);
                    return;
                }

                if (!string.IsNullOrWhiteSpace(model.Type))
                {
                    var complexObject = CreateComplexObject(model);
                    if (complexObject != null)
                    {
                        SetComplexProperty(control, propertyName, complexObject);
                    }

                    return;
                }

                // Designer preview currently ignores runtime-only concepts that need Avalonia binding/resource context.
                if (!string.IsNullOrWhiteSpace(model.BindingPath) ||
                    !string.IsNullOrWhiteSpace(model.ResourceKey) ||
                    !string.IsNullOrWhiteSpace(model.AssetPath))
                {
                    return;
                }

                var avaloniaProperty = AvaloniaPropertyRegistry.Instance.FindRegistered(control, propertyName);
                var targetType = avaloniaProperty?.PropertyType;

                if (targetType == null)
                {
                    var propertyInfo = control.GetType().GetProperty(propertyName);
                    if (propertyInfo == null || !propertyInfo.CanWrite)
                    {
                        return;
                    }

                    targetType = propertyInfo.PropertyType;
                }

                var convertedValue = ConvertPropertyValue(model, targetType);
                if (convertedValue == null)
                {
                    return;
                }

                if (avaloniaProperty != null)
                {
                    control.SetValue(avaloniaProperty, convertedValue);
                }
                else
                {
                    control.GetType().GetProperty(propertyName)?.SetValue(control, convertedValue);
                }
            }
            catch
            {
                // Preview should stay resilient even when a single property cannot be rendered.
            }
        }

        private static void ApplyAttachedProperty(Control control, string propertyName, PropertyModel model)
        {
            var setter = FindAttachedPropertySetMethod(propertyName);
            if (setter == null)
            {
                return;
            }

            var targetType = setter.GetParameters()[1].ParameterType;
            var convertedValue = ConvertPropertyValue(model, targetType);
            if (convertedValue == null)
            {
                return;
            }

            setter.Invoke(null, new[] { control, convertedValue });
        }

        private static MethodInfo? FindAttachedPropertySetMethod(string propertyName)
        {
            var lastDotIndex = propertyName.LastIndexOf('.');
            if (lastDotIndex == -1)
            {
                return null;
            }

            var ownerTypeName = propertyName.Substring(0, lastDotIndex);
            var attachedPropertyName = propertyName.Substring(lastDotIndex + 1);
            var setterName = "Set" + attachedPropertyName;

            var ownerType = FindType(ownerTypeName);
            if (ownerType == null)
            {
                return null;
            }

            return ownerType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(method => method.Name == setterName)
                .Where(method => method.GetParameters().Length == 2)
                .FirstOrDefault(method => typeof(AvaloniaObject).IsAssignableFrom(method.GetParameters()[0].ParameterType));
        }

        private static void ApplyCollectionProperty(object parentObject, string propertyName, IReadOnlyCollection<PropertyModel> items)
        {
            var collectionProperty = parentObject.GetType().GetProperty(propertyName);
            if (collectionProperty == null)
            {
                return;
            }

            var collection = collectionProperty.GetValue(parentObject);
            if (collection == null)
            {
                return;
            }

            var addMethod = collection.GetType().GetMethod("Add");
            if (addMethod == null)
            {
                return;
            }

            var collectionItemType = addMethod.GetParameters().FirstOrDefault()?.ParameterType ?? typeof(object);
            foreach (var item in items)
            {
                object? builtItem = null;

                if (!string.IsNullOrWhiteSpace(item.Type))
                {
                    builtItem = CreateComplexObject(item);
                }
                else if (item.Value != null)
                {
                    builtItem = ConvertPrimitive(item.Value, collectionItemType);
                }

                if (builtItem != null)
                {
                    addMethod.Invoke(collection, new[] { builtItem });
                }
            }
        }

        private static object? CreateComplexObject(PropertyModel model)
        {
            var resolvedType = FindType(model.Type);
            if (resolvedType == null)
            {
                return null;
            }

            if (typeof(Control).IsAssignableFrom(resolvedType))
            {
                return Build(model);
            }

            var complexObject = Activator.CreateInstance(resolvedType);
            if (complexObject == null)
            {
                return null;
            }

            foreach (var nestedProperty in model.Properties)
            {
                if (nestedProperty.Value.Items is { Count: > 0 })
                {
                    ApplyCollectionProperty(complexObject, nestedProperty.Key, nestedProperty.Value.Items);
                    continue;
                }

                var propertyInfo = complexObject.GetType().GetProperty(nestedProperty.Key);
                if (propertyInfo == null || !propertyInfo.CanWrite)
                {
                    continue;
                }

                var convertedValue = ConvertPropertyValue(nestedProperty.Value, propertyInfo.PropertyType);
                propertyInfo.SetValue(complexObject, convertedValue);
            }

            return complexObject;
        }

        private static void SetComplexProperty(Control control, string propertyName, object complexObject)
        {
            var propertyInfo = control.GetType().GetProperty(propertyName);
            if (propertyInfo != null && propertyInfo.CanWrite)
            {
                propertyInfo.SetValue(control, complexObject);
            }
        }

        private static object? ConvertPropertyValue(PropertyModel model, Type targetType)
        {
            if (model.Value == null)
            {
                return null;
            }

            return ConvertPrimitive(model.Value, targetType);
        }

        private static object? ConvertPrimitive(object value, Type targetType)
        {
            var stringValue = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;

            if (targetType.IsEnum)
            {
                return Enum.Parse(targetType, stringValue);
            }

            var parser = targetType.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
            if (parser != null)
            {
                return parser.Invoke(null, new object[] { stringValue });
            }

            var converter = TypeDescriptor.GetConverter(targetType);
            if (converter.CanConvertFrom(typeof(string)))
            {
                return converter.ConvertFromInvariantString(stringValue);
            }

            if (targetType == typeof(string))
            {
                return stringValue;
            }

            if (targetType == typeof(double))
            {
                return double.Parse(stringValue, CultureInfo.InvariantCulture);
            }

            if (targetType == typeof(int))
            {
                return int.Parse(stringValue, CultureInfo.InvariantCulture);
            }

            if (targetType == typeof(bool))
            {
                return bool.Parse(stringValue);
            }

            return value;
        }

        private static Type? FindType(string? typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                return null;
            }

            if (TypeCache.TryGetValue(typeName, out var cachedType))
            {
                return cachedType;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var exactType = assembly.GetType(typeName);
                if (exactType != null)
                {
                    TypeCache[typeName] = exactType;
                    return exactType;
                }
            }

            var commonPrefixes = new[]
            {
                "Avalonia.Controls.",
                "Avalonia.Controls.Shapes.",
                "Avalonia.Media."
            };

            foreach (var prefix in commonPrefixes)
            {
                var prefixedName = prefix + typeName;
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var prefixedType = assembly.GetType(prefixedName);
                    if (prefixedType != null)
                    {
                        TypeCache[typeName] = prefixedType;
                        return prefixedType;
                    }
                }
            }

            return null;
        }
    }
}
