using System.Linq;
using Microsoft.CodeAnalysis;
using Xunit;

namespace ArxisStudio.Markup.Json.Generator.Tests
{
    public class DiagnosticGenerationTests
    {
        private const string PartialUserControlSource = @"
using Avalonia.Controls;

namespace TestApp.Views
{
    public partial class ValidControl : UserControl
    {
    }
}
";

        private const string NonPartialUserControlSource = @"
using Avalonia.Controls;

namespace TestApp.Views
{
    public class NonPartialControl : UserControl
    {
    }
}
";

        [Fact]
        public void Missing_class_should_report_ADG0005()
        {
            const string json = @"
{
  ""SchemaVersion"": 1,
  ""Kind"": ""Control"",
  ""Root"": {
    ""TypeName"": ""Avalonia.Controls.UserControl"",
    ""Properties"": {}
  }
}
";

            var diagnostics = GeneratorTestHelper.GetGeneratorDiagnostics(
                PartialUserControlSource,
                "ValidControl.arxui.cs",
                ("ValidControl.arxui", json));

            Assert.Contains(diagnostics, d => d.Id == "ADG0005" && d.Severity == DiagnosticSeverity.Error);
        }

        [Fact]
        public void Unknown_class_should_report_ADG0006()
        {
            const string json = @"
{
  ""SchemaVersion"": 1,
  ""Kind"": ""Control"",
  ""Class"": ""TestApp.Views.DoesNotExist"",
  ""Root"": {
    ""TypeName"": ""Avalonia.Controls.UserControl"",
    ""Properties"": {}
  }
}
";

            var diagnostics = GeneratorTestHelper.GetGeneratorDiagnostics(
                PartialUserControlSource,
                "ValidControl.arxui.cs",
                ("ValidControl.arxui", json));

            Assert.Contains(diagnostics, d => d.Id == "ADG0006" && d.Severity == DiagnosticSeverity.Error);
        }

        [Fact]
        public void Non_partial_class_should_report_ADG0007()
        {
            const string json = @"
{
  ""SchemaVersion"": 1,
  ""Kind"": ""Control"",
  ""Class"": ""TestApp.Views.NonPartialControl"",
  ""Root"": {
    ""TypeName"": ""Avalonia.Controls.UserControl"",
    ""Properties"": {}
  }
}
";

            var diagnostics = GeneratorTestHelper.GetGeneratorDiagnostics(
                NonPartialUserControlSource,
                "NonPartialControl.arxui.cs",
                ("NonPartialControl.arxui", json));

            Assert.Contains(diagnostics, d => d.Id == "ADG0007" && d.Severity == DiagnosticSeverity.Error);
        }

        [Fact]
        public void Incompatible_kind_should_report_ADG0008()
        {
            const string json = @"
{
  ""SchemaVersion"": 1,
  ""Kind"": ""Window"",
  ""Class"": ""TestApp.Views.ValidControl"",
  ""Root"": {
    ""TypeName"": ""Avalonia.Controls.Window"",
    ""Properties"": {}
  }
}
";

            var diagnostics = GeneratorTestHelper.GetGeneratorDiagnostics(
                PartialUserControlSource,
                "ValidControl.arxui.cs",
                ("ValidControl.arxui", json));

            Assert.Contains(diagnostics, d => d.Id == "ADG0008" && d.Severity == DiagnosticSeverity.Error);
        }

        [Fact]
        public void Duplicate_class_should_report_ADG0009_for_each_asset()
        {
            const string json1 = @"
{
  ""SchemaVersion"": 1,
  ""Kind"": ""Control"",
  ""Class"": ""TestApp.Views.ValidControl"",
  ""Root"": {
    ""TypeName"": ""Avalonia.Controls.UserControl"",
    ""Properties"": {}
  }
}
";

            const string json2 = @"
{
  ""SchemaVersion"": 1,
  ""Kind"": ""Control"",
  ""Class"": ""TestApp.Views.ValidControl"",
  ""Root"": {
    ""TypeName"": ""Avalonia.Controls.UserControl"",
    ""Properties"": {}
  }
}
";

            var diagnostics = GeneratorTestHelper.GetGeneratorDiagnostics(
                PartialUserControlSource,
                "ValidControl.arxui.cs",
                ("ValidControl.arxui", json1),
                ("AnotherValidControl.arxui", json2));

            Assert.Equal(2, diagnostics.Count(d => d.Id == "ADG0009" && d.Severity == DiagnosticSeverity.Error));
        }

        [Fact]
        public void Incompatible_root_kind_should_report_ADG0010()
        {
            const string json = @"
{
  ""SchemaVersion"": 1,
  ""Kind"": ""Window"",
  ""Class"": ""TestApp.Views.ValidControl"",
  ""Root"": {
    ""TypeName"": ""Avalonia.Controls.UserControl"",
    ""Properties"": {}
  }
}
";

            var diagnostics = GeneratorTestHelper.GetGeneratorDiagnostics(
                PartialUserControlSource,
                "ValidControl.arxui.cs",
                ("ValidControl.arxui", json));

            Assert.Contains(diagnostics, d => d.Id == "ADG0010" && d.Severity == DiagnosticSeverity.Error);
        }

        [Fact]
        public void Incompatible_root_target_class_should_report_ADG0011()
        {
            const string json = @"
{
  ""SchemaVersion"": 1,
  ""Kind"": ""Control"",
  ""Class"": ""TestApp.Views.ValidControl"",
  ""Root"": {
    ""TypeName"": ""Avalonia.Controls.Border"",
    ""Properties"": {}
  }
}
";

            var diagnostics = GeneratorTestHelper.GetGeneratorDiagnostics(
                PartialUserControlSource,
                "ValidControl.arxui.cs",
                ("ValidControl.arxui", json));

            Assert.Contains(diagnostics, d => d.Id == "ADG0011" && d.Severity == DiagnosticSeverity.Error);
        }

        [Fact]
        public void Styles_without_class_should_not_report_ADG0005()
        {
            const string json = @"
{
  ""SchemaVersion"": 1,
  ""Kind"": ""Styles"",
  ""Root"": {
    ""TypeName"": ""Avalonia.Styling.Styles"",
    ""Properties"": {}
  }
}
";

            var diagnostics = GeneratorTestHelper.GetGeneratorDiagnostics(
                PartialUserControlSource,
                "ValidControl.arxui.cs",
                ("Styles.arxui", json));

            Assert.DoesNotContain(diagnostics, d => d.Id == "ADG0005");
        }
    }
}
