using System;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json.Linq;

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

        public string Format(object element, ITypeSymbol? targetType)
        {
            if (targetType is null || element is null)
                return FormatLegacy(element);

            string targetTypeName = targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            // --- 0. СНАЧАЛА: ENUM ---
            if (targetType.TypeKind == TypeKind.Enum)
            {
                return FormatEnum(element, targetType);
            }

            // --- 1. ПРИМИТИВЫ ОТ NEWTONSOFT.JSON ---

            if (element is string s)
            {
                // Числовые типы — генерируем литерал, а не Parse(...)
                if (IsNumeric(targetType))
                {
                    // В JSON уже должен быть формат с точкой ("0.3"), просто возвращаем как есть
                    return s;
                }

                string escapedString = Escape(s);

                if (IsBrush(targetType))
                    return $"global::Avalonia.Media.Brush.Parse(\"{escapedString}\")";

                if (HasStaticParseMethod(targetType))
                    return $"{targetTypeName}.Parse(\"{escapedString}\")";

                return $"\"{escapedString}\"";
            }

            if (targetType.SpecialType == SpecialType.System_Boolean && element is bool b)
                return b ? "true" : "false";

            if (IsNumeric(targetType))
            {
                // Числа ВСЕГДА форматируем через InvariantCulture,
                // чтобы получить 0.3, а не 0,3
                if (element is IFormattable formattable)
                    return formattable.ToString(null, CultureInfo.InvariantCulture);

                return Convert.ToString(element, CultureInfo.InvariantCulture) ?? "0";
            }

            // --- 2. JToken (если Newtonsoft.Json дал JToken, а не примитив) ---

            if (element is JToken token)
            {
                if (targetType.TypeKind == TypeKind.Enum)
                {
                    return FormatEnum(token.ToString(), targetType);
                }

                if (token.Type == JTokenType.String)
                {
                    string tokenString = token.ToString();

                    if (IsNumeric(targetType))
                    {
                        // Строка с числом — доверяем JSON (с точкой)
                        return tokenString;
                    }

                    string escapedString = Escape(tokenString);

                    if (IsBrush(targetType))
                        return $"global::Avalonia.Media.Brush.Parse(\"{escapedString}\")";

                    if (HasStaticParseMethod(targetType))
                        return $"{targetTypeName}.Parse(\"{escapedString}\")";

                    return $"\"{escapedString}\"";
                }

                if (IsNumeric(targetType))
                {
                    // Числовой JToken → форматируем через InvariantCulture
                    if (token is JValue jv && jv.Value is IFormattable f)
                        return f.ToString(null, CultureInfo.InvariantCulture);

                    if (token is JValue jv2)
                        return Convert.ToString(jv2.Value, CultureInfo.InvariantCulture) ?? "0";

                    return Convert.ToString(token, CultureInfo.InvariantCulture) ?? "0";
                }


                // Остальное — как есть
                return token.ToString();
            }

            // --- 3. Фоллбек ---
            return FormatLegacy(element);
        }

        // =================================================================
        //                      ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
        // =================================================================

        private string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

        private string FormatEnum(object el, ITypeSymbol type)
        {
            if (el is string s)
                return $"{type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{s}";

            return el.ToString() ?? "0";
        }

        private bool IsNumeric(ITypeSymbol type)
            => type.SpecialType is SpecialType.System_SByte or SpecialType.System_Byte or
                                   SpecialType.System_Int16 or SpecialType.System_UInt16 or
                                   SpecialType.System_Int32 or SpecialType.System_UInt32 or
                                   SpecialType.System_Int64 or SpecialType.System_UInt64 or
                                   SpecialType.System_Single or SpecialType.System_Double or
                                   SpecialType.System_Decimal;

        private string FormatLegacy(object? element)
        {
            if (element is string s) return $"\"{Escape(s)}\"";
            if (element is bool b) return b ? "true" : "false";

            // На всякий случай: если сюда попадут числа — тоже Invariant
            if (element is IFormattable f)
                return f.ToString(null, CultureInfo.InvariantCulture);

            return element?.ToString() ?? "null";
        }

        private bool HasStaticParseMethod(ITypeSymbol type)
        {
            return type.GetMembers("Parse")
                .OfType<IMethodSymbol>()
                .Any(m =>
                    m.IsStatic &&
                    m.DeclaredAccessibility == Accessibility.Public &&
                    m.Parameters.Length == 1 &&
                    m.Parameters[0].Type.SpecialType == SpecialType.System_String
                );
        }

        private bool IsBrush(ITypeSymbol type)
        {
            return IsAssignableTo(type, "Avalonia.Media.IBrush") ||
                   IsAssignableTo(type, "Avalonia.Media.Brush");
        }

        private bool IsAssignableTo(ITypeSymbol type, string fullName)
        {
            var targetType = _resolver.ResolveType(fullName);
            if (targetType == null) return false;

            return SymbolEqualityComparer.Default.Equals(type, targetType) ||
                   type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, targetType));
        }
    }
}
