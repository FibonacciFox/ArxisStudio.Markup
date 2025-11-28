using System.Text;
using AvaloniaDesigner.Generator.Models;
using Microsoft.CodeAnalysis;
using System.Text.Json;
using System;
using System.Linq;

namespace AvaloniaDesigner.Generator.Services
{
    public class PropertyGenerator
    {
        private readonly TypeResolver _resolver;
        private readonly ValueFormatter _formatter;
        private readonly SourceProductionContext _context;

        public PropertyGenerator(TypeResolver resolver, ValueFormatter formatter, SourceProductionContext context)
        {
            _resolver = resolver;
            _formatter = formatter;
            _context = context;
        }
        
        public string? GeneratePropertyAssignment(
            StringBuilder sb, 
            string targetName, 
            INamedTypeSymbol? targetTypeSymbol, 
            string propertyName, 
            PropertyModel model)
        {
            string? assignedVarName = null;
            
            IPropertySymbol? targetPropSymbol = targetTypeSymbol != null 
                ? _resolver.FindProperty(targetTypeSymbol, propertyName) 
                : null;
            ITypeSymbol? valueTypeSymbol = targetPropSymbol?.Type;

            // --- НОВАЯ ЛОГИКА: ОБРАБОТКА СВОЙСТВ-КОЛЛЕКЦИЙ (Children, Items) ---
            if (targetPropSymbol != null && _resolver.IsCollectionType(targetPropSymbol.Type))
            {
                // Имя коллекции, например: "this.MainPanel.Children"
                string collectionName = $"{targetName}.{propertyName}";

                // Проходим по элементам коллекции ("0", "1", "2"...)
                foreach (var elementEntry in model.Properties.OrderBy(e => e.Key))
                {
                    var elementModel = elementEntry.Value;
                    
                    // Рекурсивно генерируем сам элемент. 
                    // targetName = null, т.к. мы не присваиваем его свойству, а добавляем в коллекцию.
                    // assignedVarName будет this.MyCheckBox или _gen_...
                    string? elementVarName = GenerateNestedControl(
                        sb, 
                        elementModel, 
                        elementEntry.Key, 
                        targetTypeSymbol: null); // Присвоение не нужно
                    
                    // 4. Добавляем созданный элемент в коллекцию (если он был создан)
                    if (!string.IsNullOrEmpty(elementVarName))
                    {
                        sb.AppendLine($"            {collectionName}.Add({elementVarName});");
                    }
                }
                return null; // Свойству (Children) присваивать нечего, мы работали с коллекцией
            }
            
            // 1. Обработка вложенного объекта/контрола (Type задан)
            if (!string.IsNullOrEmpty(model.Type))
            {
                // Генерируем вложенный контрол как поле или локальную переменную
                assignedVarName = GenerateNestedControl(sb, model, propertyName, targetTypeSymbol);
                
                // Присваиваем созданную переменную свойству целевого объекта
                if (!string.IsNullOrEmpty(assignedVarName))
                {
                    sb.AppendLine($"            {targetName}.{propertyName} = {assignedVarName};");
                }
                return assignedVarName;
            }
            
            // 2. Обработка примитивного значения (Value задан)
            if (model.Value.HasValue)
            {
                string propertyKey = propertyName;
                
                if (propertyKey.Contains("."))
                {
                    HandleAttachedProperty(sb, targetName, propertyKey, model.Value.Value);
                }
                else
                {
                    string valueExpr = _formatter.Format(model.Value.Value, valueTypeSymbol);
                    sb.AppendLine($"            {targetName}.{propertyKey} = {valueExpr};");
                }
            }
            
            return null;
        }

        private string? GenerateNestedControl(
            StringBuilder sb, 
            PropertyModel model, 
            string propertyName, 
            INamedTypeSymbol? targetTypeSymbol)
        {
            string fullTypeName = $"global::{model.Type}";
            string? assignedVarName;
            
            // Ищем имя внутри свойств (Name=...)
            string? controlName = FindControlName(model);

            if (!string.IsNullOrEmpty(controlName)) 
            {
                // 1. Поле класса
                assignedVarName = $"this.{controlName}"; 
                sb.AppendLine($"            {assignedVarName} = new {fullTypeName}();");
            }
            else
            {
                // 2. Локальная, безымянная переменная
                assignedVarName = $"_gen_{propertyName}_{Guid.NewGuid().ToString().Replace("-", "").Substring(0, 4)}";
                sb.AppendLine($"            {fullTypeName} {assignedVarName} = new {fullTypeName}();");
            }

            // Устанавливаем свойства этого нового объекта (рекурсивно)
            var newObjectType = _resolver.ResolveType(model.Type);
            foreach (var innerPropEntry in model.Properties)
            {
                // Рекурсивный вызов. targetName теперь - это assignedVarName (новый контрол)
                GeneratePropertyAssignment(
                    sb, 
                    targetName: assignedVarName, 
                    targetTypeSymbol: newObjectType, 
                    propertyName: innerPropEntry.Key, 
                    model: innerPropEntry.Value);
            }
            
            return assignedVarName;
        }

        private void HandleAttachedProperty(StringBuilder sb, string targetName, string key, JsonElement value)
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
                sb.AppendLine($"            global::{ownerName}.{setter.Name}({targetName}, {valueExpr});");
            }
            else
            {
                _context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor("ADG0002", "Attached Property Error", 
                    $"Setter not found for {key}", "Generation", DiagnosticSeverity.Warning, true), Location.None));
            }
        }
        
        private string? FindControlName(PropertyModel model)
        {
            if (model.Properties.TryGetValue("Name", out PropertyModel? nameProp) && nameProp.Value.HasValue)
            {
                if (nameProp.Value.Value.ValueKind == JsonValueKind.String)
                {
                    return nameProp.Value.Value.GetString();
                }
            }
            return null;
        }
    }
}