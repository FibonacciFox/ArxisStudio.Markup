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
  ""AssetType"": ""UserControl"",
  ""Properties"": {
    ""Content"": {
      ""Type"": ""Avalonia.Controls.Border"",
      ""Properties"": {
        ""Name"": ""NamedBorder"",
        ""BorderThickness"": ""3"",
        ""BorderBrush"": ""Gray"",

        ""Background"": {
          ""Type"": ""Avalonia.Media.SolidColorBrush"",
          ""Properties"": {
            ""Color"": ""LightGreen"",
            ""Opacity"": 0.5
          }
        },

        ""Child"": {
          ""Type"": ""Avalonia.Controls.TextBlock"",
          ""Properties"": {
            ""Text"": ""Hello Brush!""
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
            var solidColorBrushSource = GeneratorTestHelper.GetGeneratedSource(
                DummyUserControlSource,
                "SolidColorBrush.cs",
                "TestApp.Forms.SolidColorBrush.g.cs",
                ("SolidColorBrush.Asset", JsonModel));

            Assert.DoesNotContain("double.Parse(\"0.5\")", solidColorBrushSource);
            Assert.Contains("Opacity = 0.5", solidColorBrushSource);
        }
    }
}
