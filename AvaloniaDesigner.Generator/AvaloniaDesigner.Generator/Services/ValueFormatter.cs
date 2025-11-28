using System.Linq;
using System.Text.Json;
using Microsoft.CodeAnalysis;

namespace AvaloniaDesigner.Generator.Services
{
    /// <summary>
    /// Отвечает за генерацию C# кода для значений свойств (строки, числа, Parse, Enums).
    /// </summary>
    public class ValueFormatter
    {
        private readonly TypeResolver _resolver;

        public ValueFormatter(TypeResolver resolver)
        {
            _resolver = resolver;
        }

        public string Format(JsonElement element, ITypeSymbol? targetType)
        {
            if (targetType is null) return FormatLegacy(element);

            // String
            if (targetType.SpecialType == SpecialType.System_String)
                return $"\"{Escape(element.ToString())}\"";

            // Bool
            if (targetType.SpecialType == SpecialType.System_Boolean)
                return GetBoolString(element);

            // Numeric primitives
            if (IsNumeric(targetType))
                return element.GetRawText();

            // Enum
            if (targetType.TypeKind == TypeKind.Enum)
                return FormatEnum(element, targetType);

            // Complex Types (Parse logic)
            if (element.ValueKind == JsonValueKind.String)
                return FormatComplexType(element.GetString() ?? "", targetType);

            return FormatLegacy(element);
        }

        private string FormatComplexType(string text, ITypeSymbol targetType)
        {
            // 1. Ищем Parse на самом типе
            if (HasStaticParseMethod(targetType, targetType))
            {
                return $"{targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.Parse(\"{Escape(text)}\")";
            }

            // 2. Специфичные хелперы Avalonia (Brush, RowDefs, ColDefs)
            // Упрощенная логика для примера, можно расширить как в оригинале
            if (IsAssignableTo(targetType, "Avalonia.Media.IBrush"))
                return $"global::Avalonia.Media.Brush.Parse(\"{Escape(text)}\")";
            
            // Fallback
            return $"\"{Escape(text)}\"";
        }
        
        // ... Вспомогательные методы (GetBoolString, FormatEnum, IsNumeric, Escape) ...
        // Код Escape и прочее переносится сюда из оригинала почти 1 в 1.
        
        private string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

        private string GetBoolString(JsonElement el)
        {
             // Логика true/false из оригинала
             if (el.ValueKind == JsonValueKind.True) return "true";
             if (el.ValueKind == JsonValueKind.False) return "false";
             if (el.ValueKind == JsonValueKind.String) 
                 return bool.TryParse(el.GetString(), out var b) && b ? "true" : "false";
             return "false";
        }

        private string FormatEnum(JsonElement el, ITypeSymbol type)
        {
             if (el.ValueKind == JsonValueKind.String)
                return $"{type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{el.GetString()}";
             return el.GetRawText();
        }

        private bool IsNumeric(ITypeSymbol type) 
            => type.SpecialType is SpecialType.System_Int32 or SpecialType.System_Double or SpecialType.System_Single; // и т.д.

        private string FormatLegacy(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.String) return $"\"{Escape(element.GetString() ?? "")}\"";
            return element.ValueKind == JsonValueKind.True ? "true" : 
                   element.ValueKind == JsonValueKind.False ? "false" : element.GetRawText();
        }

        private bool HasStaticParseMethod(ITypeSymbol type, ITypeSymbol returnType)
        {
            return type.GetMembers("Parse").OfType<IMethodSymbol>()
                .Any(m => m.IsStatic && m.Parameters.Length == 1 && 
                          m.Parameters[0].Type.SpecialType == SpecialType.System_String);
        }

        private bool IsAssignableTo(ITypeSymbol type, string interfaceName)
        {
            var iface = _resolver.ResolveType(interfaceName);
            if (iface == null) return false;
            
            // Упрощенная проверка
            return SymbolEqualityComparer.Default.Equals(type, iface) || 
                   type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, iface));
        }
    }
}