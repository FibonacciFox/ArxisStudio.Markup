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
            
            string targetTypeName = targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            
            // --- 1. Стандартная обработка примитивов (строки, bool, числа, enum) ---
            
            if (targetType.SpecialType == SpecialType.System_String)
                return $"\"{Escape(element.GetString() ?? "")}\"";

            if (targetType.SpecialType == SpecialType.System_Boolean)
                return GetBoolString(element);

            if (IsNumeric(targetType))
                return element.GetRawText();

            if (targetType.TypeKind == TypeKind.Enum)
                return FormatEnum(element, targetType);

            // --- 2. Обработка сложных типов (Thickness, Brush, CornerRadius и т.д.) ---

            // Если входное значение — число или строка (т.е. XAML-подобная строка)
            if (element.ValueKind == JsonValueKind.String || element.ValueKind == JsonValueKind.Number)
            {
                // Конвертируем JSON-значение в строку для парсинга
                string stringValue = element.ValueKind switch
                {
                    JsonValueKind.String => element.GetString() ?? "",
                    JsonValueKind.Number => element.GetRawText(), 
                    _ => element.ToString() ?? ""
                };
                
                string escapedString = Escape(stringValue);

                // А) Специальная обработка для IBrush/Brush
                if (IsBrush(targetType))
                {
                    return $"global::Avalonia.Media.Brush.Parse(\"{escapedString}\")";
                }
                
                // Б) Обнаружение метода Parse с помощью Roslyn для всех остальных сложных типов
                if (HasStaticParseMethod(targetType))
                {
                    return $"{targetTypeName}.Parse(\"{escapedString}\")";
                }
            }
            
            // Если не смогли преобразовать:
            return FormatLegacy(element);
        }
        
        // =================================================================
        //                      ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
        // =================================================================
        
        private string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

        private string GetBoolString(JsonElement el)
        {
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
            => type.SpecialType is SpecialType.System_SByte or SpecialType.System_Byte or
                                   SpecialType.System_Int16 or SpecialType.System_UInt16 or
                                   SpecialType.System_Int32 or SpecialType.System_UInt32 or
                                   SpecialType.System_Int64 or SpecialType.System_UInt64 or
                                   SpecialType.System_Single or SpecialType.System_Double or
                                   SpecialType.System_Decimal;

        private string FormatLegacy(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.String) return $"\"{Escape(element.GetString() ?? "")}\"";
            return element.ValueKind == JsonValueKind.True ? "true" : 
                   element.ValueKind == JsonValueKind.False ? "false" : element.GetRawText();
        }

        /// <summary>
        /// Проверяет наличие статического публичного метода Parse(string) на типе.
        /// </summary>
        private bool HasStaticParseMethod(ITypeSymbol type)
        {
            return type.GetMembers("Parse")
                .OfType<IMethodSymbol>()
                .Any(m => 
                    m.IsStatic && 
                    // ИСПРАВЛЕНО: Используем DeclaredAccessibility для проверки публичности
                    m.DeclaredAccessibility == Accessibility.Public && 
                    m.Parameters.Length == 1 && 
                    m.Parameters[0].Type.SpecialType == SpecialType.System_String
                );
        }

        /// <summary>
        /// Проверяет, является ли тип или его интерфейс IBrush/Brush.
        /// </summary>
        private bool IsBrush(ITypeSymbol type)
        {
            return IsAssignableTo(type, "Avalonia.Media.IBrush") || 
                   IsAssignableTo(type, "Avalonia.Media.Brush");
        }

        /// <summary>
        /// Проверяет, может ли тип быть назначен указанному интерфейсу/классу по имени.
        /// </summary>
        private bool IsAssignableTo(ITypeSymbol type, string fullName)
        {
            var targetType = _resolver.ResolveType(fullName);
            if (targetType == null) return false;
            
            return SymbolEqualityComparer.Default.Equals(type, targetType) || 
                   type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, targetType));
        }
    }
}