using System;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using AvaloniaDesigner.Generator.Models;
using AvaloniaDesigner.Generator.Services;
using AvaloniaDesigner.Generator.Builders;

namespace AvaloniaDesigner.Generator
{
    [Generator(LanguageNames.CSharp)]
    public class FormGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var jsonFiles = context.AdditionalTextsProvider
                .Where(f => Path.GetFileName(f.Path).EndsWith("Model.json", StringComparison.OrdinalIgnoreCase));

            var compilationAndFiles = context.CompilationProvider.Combine(jsonFiles.Collect());

            context.RegisterSourceOutput(compilationAndFiles, (spc, source) => Execute(spc, source.Left, source.Right));
        }

        private void Execute(
            SourceProductionContext context,
            Compilation compilation,
            ImmutableArray<AdditionalText> files)
        {
            string rootNamespace = compilation.AssemblyName ?? "DefaultApp";
            
            var typeResolver = new TypeResolver(compilation);
            var builder = new FormClassBuilder(typeResolver, context);

            foreach (var file in files)
            {
                var jsonContent = file.GetText(context.CancellationToken)?.ToString();
                if (string.IsNullOrEmpty(jsonContent)) continue;

                try
                {
                    // Десериализуем в AvaloniaModel
                    var avaloniaModel = JsonSerializer.Deserialize<AvaloniaModel>(
                        jsonContent!, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (avaloniaModel is null) continue;

                    // Передаем модель в билдер
                    string code = builder.Build(avaloniaModel, rootNamespace);

                    context.AddSource($"{avaloniaModel.FormName}.g.cs", SourceText.From(code, Encoding.UTF8));
                }
                catch (JsonException ex)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor("ADG0001", "JSON Parsing Error", 
                        $"Error parsing {Path.GetFileName(file.Path)}: {ex.Message}", 
                        "Generation", DiagnosticSeverity.Error, true), 
                        Location.None));
                }
            }
        }
    }
}