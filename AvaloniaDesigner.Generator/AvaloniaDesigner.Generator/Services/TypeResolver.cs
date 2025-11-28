using System.Linq;
using Microsoft.CodeAnalysis;

namespace AvaloniaDesigner.Generator.Services
{
    /// <summary>
    /// Отвечает за анализ типов через Roslyn API.
    /// </summary>
    public class TypeResolver
    {
        private readonly Compilation _compilation;

        public TypeResolver(Compilation compilation)
        {
            _compilation = compilation;
        }

        public INamedTypeSymbol? ResolveType(string metadataName) 
            => _compilation.GetTypeByMetadataName(metadataName);

        public IPropertySymbol? FindProperty(INamedTypeSymbol type, string propertyName)
        {
            for (var t = type; t is not null; t = t.BaseType)
            {
                foreach (var member in t.GetMembers(propertyName))
                {
                    if (member is IPropertySymbol p) return p;
                }
            }
            return null;
        }

        public IMethodSymbol? FindAttachedSetter(INamedTypeSymbol ownerType, string attachedName)
        {
            string methodName = "Set" + attachedName;
            foreach (var member in ownerType.GetMembers(methodName))
            {
                if (member is IMethodSymbol m && m.IsStatic && m.Parameters.Length == 2)
                    return m;
            }
            return null;
        }

        public IPropertySymbol? FindContentProperty(INamedTypeSymbol type)
        {
            var contentAttributeSymbol = _compilation.GetTypeByMetadataName("Avalonia.Metadata.ContentAttribute");
            if (contentAttributeSymbol is null) return null;

            for (var t = type; t is not null; t = t.BaseType)
            {
                foreach (var member in t.GetMembers().OfType<IPropertySymbol>())
                {
                    foreach (var attr in member.GetAttributes())
                    {
                        if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, contentAttributeSymbol))
                            return member;
                    }
                }
            }
            return null;
        }

        public bool IsCollectionType(ITypeSymbol type)
        {
            // Простая проверка: если тип реализует System.Collections.Generic.ICollection<T> или имеет метод Add(object).
            var iCol = _compilation.GetTypeByMetadataName("System.Collections.Generic.ICollection`1");
            if (iCol != null && ImplementsInterface(type, iCol)) return true;

            var iList = _compilation.GetTypeByMetadataName("System.Collections.IList");
            if (iList != null && ImplementsInterface(type, iList)) return true;
            
            // Проверка на наличие публичного метода Add с одним параметром (как fallback)
            if (type.GetMembers("Add").OfType<IMethodSymbol>().Any(m => 
                m.DeclaredAccessibility == Accessibility.Public && m.Parameters.Length == 1)) return true;

            return false;
        }
        
        // Вспомогательный метод: определяет тип элемента коллекции
        public ITypeSymbol? FindCollectionElementType(ITypeSymbol collectionType)
        {
            // Ищем ICollection<T>
            var iCol = _compilation.GetTypeByMetadataName("System.Collections.Generic.ICollection`1");
            if (iCol != null)
            {
                var iColInstance = collectionType.AllInterfaces.FirstOrDefault(i => 
                    i.IsGenericType && SymbolEqualityComparer.Default.Equals(i.ConstructedFrom, iCol));
                
                return iColInstance?.TypeArguments.FirstOrDefault();
            }
            
            return null;
        }

        private static bool ImplementsInterface(ITypeSymbol type, INamedTypeSymbol interfaceSymbol)
        {
            if (type is INamedTypeSymbol namedType)
            {
                if (SymbolEqualityComparer.Default.Equals(namedType.ConstructedFrom, interfaceSymbol)) return true;
                return namedType.AllInterfaces.Any(i => 
                    i.IsGenericType && SymbolEqualityComparer.Default.Equals(i.ConstructedFrom, interfaceSymbol));
            }
            return false;
        }

        public Compilation Compilation => _compilation;
    }
}