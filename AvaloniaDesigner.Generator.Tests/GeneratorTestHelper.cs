using System;
using System.Collections.Immutable;
using System.Linq;
using AvaloniaDesigner.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AvaloniaDesigner.Generator.Tests
{
    internal static class GeneratorTestHelper
    {
        public static ImmutableArray<GeneratedSourceResult> RunGenerator(
            string userSource,
            params (string path, string content)[] additionalFiles)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(userSource);

            // 🔹 Берём ВСЕ загруженные сборки (включая Avalonia) и добавляем как MetadataReference
            var references = AppDomain.CurrentDomain
                .GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .Select(a => MetadataReference.CreateFromFile(a.Location))
                .Cast<MetadataReference>()
                .ToList();

            var compilation = CSharpCompilation.Create(
                assemblyName: "TestAssembly",
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var additionalTexts = additionalFiles
                .Select(f => (AdditionalText)new InMemoryAdditionalText(f.path, f.content))
                .ToImmutableArray();

            var generator = new FormGenerator();
            var sourceGenerator = generator.AsSourceGenerator();

            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                [sourceGenerator],
                additionalTexts: additionalTexts);

            driver = driver.RunGeneratorsAndUpdateCompilation(
                compilation,
                out var outputCompilation,
                out var diagnostics);

            var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
            if (errors.Length > 0)
            {
                throw new Xunit.Sdk.XunitException(
                    "Generator produced compilation errors:\n" +
                    string.Join("\n", errors.Select(e => e.ToString())));
            }

            var runResult = driver.GetRunResult();

            return runResult.Results
                .SelectMany(r => r.GeneratedSources)
                .ToImmutableArray();
        }
    }
}
