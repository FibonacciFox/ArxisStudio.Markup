using System.Linq;
using Xunit;

namespace AvaloniaDesigner.Generator.Tests
{
    public class SolidColorBrushTests
    {
        private const string DummyUserControlSource = @"
using Avalonia.Controls;

namespace TestApp.Forms
{
    public partial class SolidColorBrush : UserControl
    {
    }
}
";

        private const string JsonModel = @"
{
  ""FormName"": ""SolidColorBrush"",
  ""NamespaceSuffix"": ""Forms"",
  ""ParentClassType"": ""Avalonia.Controls.UserControl"",
  ""Properties"": {
    ""Content"": {
      ""Type"": ""Avalonia.Controls.Border"",
      ""Properties"": {
        ""Name"": { ""Value"": ""NamedBorder"" },
        ""BorderThickness"": { ""Value"": ""3"" },
        ""BorderBrush"": { ""Value"": ""Gray"" },

        ""Background"": {
          ""Type"": ""Avalonia.Media.SolidColorBrush"",
          ""Properties"": {
            ""Color"": { ""Value"": ""LightGreen"" },
            ""Opacity"": { ""Value"": ""0.5"" }
          }
        },

        ""Child"": {
          ""Type"": ""Avalonia.Controls.TextBlock"",
          ""Properties"": {
            ""Text"": { ""Value"": ""Hello Brush!"" }
          }
        }
      }
    }
  }
}
";

        [Fact]
        public void Opacity_should_be_generated_as_literal_not_double_parse()
        {
            // act
            var generated = GeneratorTestHelper.RunGenerator(
                DummyUserControlSource,
                ("SolidColorBrush.Model.json", JsonModel));

            var solidColorBrushSource = generated
                .FirstOrDefault(g => g.HintName == "SolidColorBrush.g.cs")
                .SourceText
                .ToString();

            // assert: нет double.Parse("0.5")
            Assert.DoesNotContain("double.Parse(\"0.5\")", solidColorBrushSource);

            // assert: есть присваивание литерала 0.5
            Assert.Contains("Opacity = 0.5", solidColorBrushSource);
        }
    }
}