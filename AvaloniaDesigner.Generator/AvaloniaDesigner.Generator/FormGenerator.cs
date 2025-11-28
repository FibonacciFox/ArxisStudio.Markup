using System;
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
            System.Collections.Immutable.ImmutableArray<AdditionalText> files)
        {
            string rootNamespace = compilation.AssemblyName ?? "DefaultApp";
            
            // Инициализируем сервисы один раз на компиляцию (TypeResolver кэширует Compilation внутри)
            var typeResolver = new TypeResolver(compilation);
            
            // Создаем строителя
            var builder = new FormClassBuilder(typeResolver, context);

            foreach (var file in files)
            {
                var json = file.GetText(context.CancellationToken)?.ToString();
                if (string.IsNullOrEmpty(json)) continue;

                try
                {
                    var model = JsonSerializer.Deserialize<FormModel>(json!, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (model is null) continue;

                    // Делегируем построение кода классу-строителю
                    string code = builder.Build(model, rootNamespace);

                    context.AddSource($"{model.FormName}.g.cs", SourceText.From(code, Encoding.UTF8));
                }
                catch (JsonException ex)
                {
                    // Логика ошибок
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor("ADG0001", "JSON Error", 
                        $"Error parsing {Path.GetFileName(file.Path)}: {ex.Message}", "Gen", DiagnosticSeverity.Error, true), 
                        Location.None));
                }
            }
        }
    }
}