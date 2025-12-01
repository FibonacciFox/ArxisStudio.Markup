using System;
using System.Text;
using AvaloniaDesigner.Generator.Models;
using Microsoft.CodeAnalysis;

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
            IPropertySymbol? targetPropSymbol = targetTypeSymbol != null 
                ? _resolver.FindProperty(targetTypeSymbol, propertyName) 
                : null;

            ITypeSymbol? valueTypeSymbol = targetPropSymbol?.Type;

            bool isCollectionTarget = targetPropSymbol != null 
                                      && _resolver.IsCollectionType(targetPropSymbol.Type);

            // --- 1. КОЛЛЕКЦИИ — только если Items реально есть ---
            if (isCollectionTarget && model.Items is { Count: > 0 })
            {
                string collectionName = $"{targetName}.{propertyName}";

                int index = 0;
                foreach (var elementModel in model.Items)
                {
                    if (string.IsNullOrEmpty(elementModel.Type))
                    {
                        index++;
                        continue;
                    }

                    string elementKey = $"{propertyName}_{index}";

                    string? varName = GenerateNestedControl(
                        sb,
                        elementModel,
                        elementKey);

                    if (varName != null)
                        sb.AppendLine($"            {collectionName}.Add({varName});");

                    index++;
                }

                return null;
            }

            // --- 2. Вложенный объект / контрол ---
            if (!string.IsNullOrEmpty(model.Type))
            {
                string? assignedVarName = GenerateNestedControl(sb, model, propertyName);
                
                if (assignedVarName != null)
                    sb.AppendLine($"            {targetName}.{propertyName} = {assignedVarName};");

                return assignedVarName;
            }

            // --- 3. Примитив ---
            if (model.Value != null)
            {
                if (propertyName.Contains("."))
                {
                    HandleAttachedProperty(sb, targetName, propertyName, model.Value);
                }
                else
                {
                    string valueExpr = _formatter.Format(model.Value, valueTypeSymbol);
                    sb.AppendLine($"            {targetName}.{propertyName} = {valueExpr};");
                }
            }
            
            return null;
        }
        
        private string? GenerateNestedControl(
            StringBuilder sb, 
            PropertyModel model, 
            string propertyName)
        {
            string fullTypeName = $"global::{model.Type}";
            string? assignedVarName;

            string? controlName = FindControlName(model);

            if (!string.IsNullOrEmpty(controlName))
            {
                assignedVarName = $"this.{controlName}";
                sb.AppendLine($"            {assignedVarName} = new {fullTypeName}();");
            }
            else
            {
                assignedVarName = $"_gen_{propertyName}_{Guid.NewGuid().ToString().Replace("-", "").Substring(0, 4)}";
                sb.AppendLine($"            {fullTypeName} {assignedVarName} = new {fullTypeName}();");
            }

            var objectType = _resolver.ResolveType(model.Type);

            if (objectType != null && model.Properties != null)
            {
                foreach (var propEntry in model.Properties)
                {
                    GeneratePropertyAssignment(
                        sb,
                        assignedVarName,
                        objectType,
                        propEntry.Key,
                        propEntry.Value);
                }
            }

            return assignedVarName;
        }
        
        private void HandleAttachedProperty(StringBuilder sb, string targetName, string key, object value)
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
        }
        
        private string? FindControlName(PropertyModel model)
        {
            if (model.Properties.TryGetValue("Name", out var nameProp)
                && nameProp.Value is string s)
            {
                return s;
            }

            return null;
        }
    }
}
