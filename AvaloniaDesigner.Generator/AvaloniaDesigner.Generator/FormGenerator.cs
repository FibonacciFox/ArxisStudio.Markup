using System;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Threading;
using AvaloniaDesigner.Generator.Builders;
using AvaloniaDesigner.Generator.Models;
using AvaloniaDesigner.Generator.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace AvaloniaDesigner.Generator
{
    [Generator(LanguageNames.CSharp)]
    public class FormGenerator : IIncrementalGenerator
    {
        private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy(false, true) },
            Converters = { new PropertyModelConverter() }
        };

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var pipeline = context.AdditionalTextsProvider
                .Where(f => Path.GetFileName(f.Path).EndsWith("Model.json", StringComparison.OrdinalIgnoreCase))
                .Select((text, token) => ParseJson(text, token))
                .Where(x => x.Model != null);

            var compilationAndModels = context.CompilationProvider.Combine(pipeline.Collect());

            context.RegisterSourceOutput(compilationAndModels, (spc, source) => 
                Execute(spc, source.Left, source.Right));
        }

        private static (AvaloniaModel? Model, string FileName) ParseJson(AdditionalText text, CancellationToken token)
        {
            var jsonContent = text.GetText(token)?.ToString();
            if (string.IsNullOrWhiteSpace(jsonContent)) return (null, text.Path);

            try
            {
                var model = JsonConvert.DeserializeObject<AvaloniaModel>(jsonContent!, _jsonSettings);
                return (model, Path.GetFileName(text.Path));
            }
            catch
            {
                return (null, text.Path);
            }
        }

        private void Execute(
            SourceProductionContext context,
            Compilation compilation,
            ImmutableArray<(AvaloniaModel? Model, string FileName)> models)
        {
            if (models.IsDefaultOrEmpty) return;

            var typeResolver = new TypeResolver(compilation);
            var builder = new FormClassBuilder(typeResolver, context);
            
            string rootNamespace = compilation.AssemblyName ?? "AvaloniaApp";
            string assemblyName = compilation.AssemblyName ?? "AvaloniaApp"; // Получаем имя сборки

            foreach (var (model, fileName) in models)
            {
                if (model is null) continue;

                try
                {
                    string code = builder.Build(model, rootNamespace, assemblyName);
                    context.AddSource($"{model.FormName}.g.cs", SourceText.From(code, Encoding.UTF8));
                }
                catch (Exception ex)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor("ADG0004", "Generation Error", $"Error generating {fileName}: {ex.Message}", "Gen", DiagnosticSeverity.Error, true),
                        Location.None));
                }
            }
        }
    }
}