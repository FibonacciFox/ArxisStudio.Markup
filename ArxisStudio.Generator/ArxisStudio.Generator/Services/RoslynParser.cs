using Microsoft.CodeAnalysis;

namespace ArxisStudio.Markup.Json.Generator.Services
{
    public class ClassInfo
    {
        public string ClassName { get; }
        public string Namespace { get; }
        public string? BaseClass { get; }

        public ClassInfo(string className, string ns, string? baseClass)
        {
            ClassName = className;
            Namespace = ns;
            BaseClass = baseClass;
        }
    }

    public static class RoslynParser
    {
        public static ClassInfo ParseClassInfo(INamedTypeSymbol typeSymbol)
        {
            var className = typeSymbol.Name;
            var containingNamespace = typeSymbol.ContainingNamespace;
            var ns = containingNamespace == null || containingNamespace.IsGlobalNamespace
                ? "Global"
                : containingNamespace.ToDisplayString();
            var baseClass = typeSymbol.BaseType?.ToDisplayString();

            return new ClassInfo(className, ns, baseClass);
        }
    }
}
