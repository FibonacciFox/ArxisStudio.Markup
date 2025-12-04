using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using AvaloniaDesigner.Generator.Models;
using Microsoft.CodeAnalysis;

namespace AvaloniaDesigner.Generator.Services
{
    public class PropertyGenerator
    {
        private readonly TypeResolver _resolver;
        private readonly ValueFormatter _formatter;
        private readonly SourceProductionContext _context;
        private readonly string _assemblyName; 

        public PropertyGenerator(TypeResolver resolver, ValueFormatter formatter, SourceProductionContext context, string assemblyName)
        {
            _resolver = resolver;
            _formatter = formatter;
            _context = context;
            _assemblyName = assemblyName;
        }

        public void GeneratePropertyAssignment(IndentedTextWriter writer,
            string targetName,
            INamedTypeSymbol? targetTypeSymbol,
            string propertyName,
            PropertyModel model)
        {
            // 1. События
            if (targetTypeSymbol != null)
            {
                var eventSymbol = _resolver.FindEvent(targetTypeSymbol, propertyName);
                if (eventSymbol != null && model.Value is string handlerName)
                {
                    writer.WriteLine($"{targetName}.{propertyName} += {handlerName};");
                    return;
                }
            }

            IPropertySymbol? targetPropSymbol = targetTypeSymbol != null
                ? _resolver.FindProperty(targetTypeSymbol, propertyName)
                : null;

            // 2. Привязки
            if (!string.IsNullOrEmpty(model.BindingPath) && targetTypeSymbol != null)
            {
                var avaloniaProp = _resolver.FindAvaloniaPropertyField(targetTypeSymbol, propertyName);
                if (avaloniaProp != null)
                {
                    string propField = $"{targetTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{avaloniaProp.Name}";

                    string bindingCode = $"new global::Avalonia.Data.Binding(\"{model.BindingPath}\")";
                    var initializers = new List<string>();

                    if (!string.IsNullOrEmpty(model.BindingMode))
                        initializers.Add($"Mode = global::Avalonia.Data.BindingMode.{model.BindingMode}");

                    if (!string.IsNullOrEmpty(model.BindingStringFormat))
                        initializers.Add($"StringFormat = \"{model.BindingStringFormat}\"");

                    if (!string.IsNullOrEmpty(model.BindingElementName))
                        initializers.Add($"ElementName = \"{model.BindingElementName}\"");

                    if (!string.IsNullOrEmpty(model.BindingConverter))
                        initializers.Add($"Converter = (global::Avalonia.Data.Converters.IValueConverter)this.FindResource(\"{model.BindingConverter}\")");

                    if (model.BindingConverterParameter != null)
                    {
                        string paramVal = _formatter.Format(model.BindingConverterParameter, null);
                        initializers.Add($"ConverterParameter = {paramVal}");
                    }

                    if (model.BindingFallbackValue != null)
                    {
                        string fallbackVal = _formatter.Format(model.BindingFallbackValue, targetPropSymbol?.Type);
                        initializers.Add($"FallbackValue = {fallbackVal}");
                    }

                    if (model.BindingTargetNullValue != null)
                    {
                        string nullVal = _formatter.Format(model.BindingTargetNullValue, targetPropSymbol?.Type);
                        initializers.Add($"TargetNullValue = {nullVal}");
                    }

                    if (initializers.Count > 0)
                        bindingCode += " { " + string.Join(", ", initializers) + " }";

                    writer.WriteLine($"{targetName}.Bind({propField}, {bindingCode});");
                }
                else
                {
                    writer.WriteLine($"// ОШИБКА: Не найдено DependencyProperty для {propertyName}");
                }
                return;
            }

            // 3. Ресурсы (Исправление: IResourceNode -> IResourceHost)
            if (!string.IsNullOrEmpty(model.ResourceKey) && targetTypeSymbol != null)
            {
                var avaloniaProp = _resolver.FindAvaloniaPropertyField(targetTypeSymbol, propertyName);
                
                if (avaloniaProp != null)
                {
                    // Используем GetResourceObservable + Bind для корректной работы в конструкторе
                    string propField = $"{targetTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{avaloniaProp.Name}";
                    
                    // ВАЖНОЕ ИСПРАВЛЕНИЕ: приведение к IResourceHost вместо IResourceNode
                    writer.WriteLine($"{targetName}.Bind({propField}, this.GetResourceObservable(\"{model.ResourceKey}\"));");
                }
                else if (targetPropSymbol != null)
                {
                    // Fallback для обычных свойств (рискованно в конструкторе, но выбора нет)
                    string typeName = targetPropSymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    writer.WriteLine($"{targetName}.{propertyName} = ({typeName})this.FindResource(\"{model.ResourceKey}\");");
                }
                return;
            }

            // 4. Ассеты
            if (!string.IsNullOrEmpty(model.AssetPath) && targetTypeSymbol != null)
            {
                if (targetPropSymbol != null)
                {
                    string targetAssembly = !string.IsNullOrEmpty(model.AssetAssembly) ? model.AssetAssembly! : _assemblyName;
                    string cleanPath = model.AssetPath!.TrimStart('/');
                    string uriString = $"avares://{targetAssembly}/{cleanPath}";
                    
                    string uriCode = $"new global::System.Uri(\"{uriString}\")";
                    string streamCode = $"global::Avalonia.Platform.AssetLoader.Open({uriCode})";

                    if (_resolver.IsAssignableTo(targetPropSymbol.Type, "Avalonia.Media.IImage") || 
                        _resolver.IsAssignableTo(targetPropSymbol.Type, "Avalonia.Media.Imaging.Bitmap"))
                    {
                        writer.WriteLine($"{targetName}.{propertyName} = new global::Avalonia.Media.Imaging.Bitmap({streamCode});");
                    }
                    else if (_resolver.IsAssignableTo(targetPropSymbol.Type, "Avalonia.Controls.WindowIcon"))
                    {
                        writer.WriteLine($"{targetName}.{propertyName} = new global::Avalonia.Controls.WindowIcon({streamCode});");
                    }
                    else
                    {
                         writer.WriteLine($"{targetName}.{propertyName} = new global::Avalonia.Media.Imaging.Bitmap({streamCode});");
                    }
                }
                return;
            }

            // 5. Коллекции
            bool isCollectionTarget = targetPropSymbol != null && _resolver.IsCollectionType(targetPropSymbol.Type);
            if (isCollectionTarget && model.Items is { Count: > 0 })
            {
                string collectionName = $"{targetName}.{propertyName}";
                int index = 0;
                foreach (var elementModel in model.Items)
                {
                    if (string.IsNullOrEmpty(elementModel.Type)) { index++; continue; }

                    string? varName = GenerateNestedControl(writer, elementModel, $"{propertyName}_{index}");
                    if (varName != null)
                        writer.WriteLine($"{collectionName}.Add({varName});");
                    index++;
                }
                return;
            }

            // 6. Вложенные контролы
            if (!string.IsNullOrEmpty(model.Type))
            {
                string? assignedVarName = GenerateNestedControl(writer, model, propertyName);
                if (assignedVarName != null)
                    writer.WriteLine($"{targetName}.{propertyName} = {assignedVarName};");
                return;
            }

            // 7. Обычные значения
            if (model.Value != null)
            {
                bool handled = false;

                bool isReadOnly = targetPropSymbol != null && targetPropSymbol.SetMethod == null;
                if (isReadOnly)
                {
                    var parseMethod = targetPropSymbol!.Type.GetMembers("Parse")
                        .OfType<IMethodSymbol>()
                        .FirstOrDefault(m => 
                            m.IsStatic && 
                            m.DeclaredAccessibility == Accessibility.Public &&
                            m.Parameters.Length == 1 && 
                            m.Parameters[0].Type.SpecialType == SpecialType.System_String);

                    if (parseMethod != null && _resolver.IsCollectionType(targetPropSymbol.Type))
                    {
                        string typeName = targetPropSymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        string strVal = model.Value.ToString();
                        
                        writer.WriteLine($"{targetName}.{propertyName}.AddRange({typeName}.Parse(\"{strVal}\"));");
                        handled = true;
                    }
                }

                if (!handled)
                {
                    if (propertyName.Contains("."))
                    {
                        HandleAttachedProperty(writer, targetName, propertyName, model.Value);
                    }
                    else
                    {
                        string valueExpr = _formatter.Format(model.Value, targetPropSymbol?.Type);
                        writer.WriteLine($"{targetName}.{propertyName} = {valueExpr};");
                    }
                }
            }
        }

        private string? GenerateNestedControl(IndentedTextWriter writer, PropertyModel model, string propertyName)
        {
            string fullTypeName = $"global::{model.Type}";
            string? controlName = null;

            if (model.Properties.TryGetValue("Name", out var nameProp) && nameProp.Value is string s)
                controlName = s;

            string assignedVarName;
            if (!string.IsNullOrEmpty(controlName))
            {
                // Именованный контрол (поле класса), без регистрации NameScope
                assignedVarName = $"this.{controlName}";
                writer.WriteLine($"{assignedVarName} = new {fullTypeName}();");
            }
            else
            {
                // Анонимный контрол
                assignedVarName = $"_gen_{propertyName}_{Guid.NewGuid().ToString("N").Substring(0, 4)}";
                writer.WriteLine($"{fullTypeName} {assignedVarName} = new {fullTypeName}();");
            }

            var objectType = _resolver.ResolveType(model.Type);
            if (objectType != null && model.Properties != null)
            {
                foreach (var propEntry in model.Properties)
                {
                    if (propEntry.Key == "Name") continue;
                    GeneratePropertyAssignment(writer, assignedVarName, objectType, propEntry.Key, propEntry.Value);
                }
            }
            return assignedVarName;
        }
        
        private void HandleAttachedProperty(IndentedTextWriter writer, string targetName, string key, object value)
        {
            int lastDot = key.LastIndexOf('.');
            string ownerName = key.Substring(0, lastDot);
            string propName = key.Substring(lastDot + 1);
            var ownerType = _resolver.ResolveType(ownerName);
            var setter = ownerType != null ? _resolver.FindAttachedSetter(ownerType, propName) : null;

            if (ownerType != null && setter != null)
            {
                var valType = setter.Parameters[1].Type;
                string valueExpr = _formatter.Format(value, valType);
                writer.WriteLine($"global::{ownerName}.{setter.Name}({targetName}, {valueExpr});");
            }
        }
    }
}