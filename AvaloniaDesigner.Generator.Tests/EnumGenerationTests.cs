using System.Linq;
using Xunit;

namespace AvaloniaDesigner.Generator.Tests
{
    public class EnumGenerationTests
    {
        // Небольшой заглушечный исходник с partial UserControl под генерацию
        private const string DummyUserControlSource = @"
using Avalonia.Controls;

namespace TestApp.Forms
{
    public partial class EnumTestControl : UserControl
    {
    }
}
";

        // JSON c кучей enum-ов на обычных свойствах
        private const string EnumJsonModel = @"
{
  ""FormName"": ""EnumTestControl"",
  ""NamespaceSuffix"": ""Forms"",
  ""ParentClassType"": ""Avalonia.Controls.UserControl"",
  ""Properties"": {
    ""Content"": {
      ""Type"": ""Avalonia.Controls.Border"",
      ""Properties"": {
        ""Child"": {
          ""Type"": ""Avalonia.Controls.TextBlock"",
          ""Properties"": {
            ""Name"": { ""Value"": ""EnumText"" },
            ""Text"": { ""Value"": ""Hello enums!"" },
            ""HorizontalAlignment"": { ""Value"": ""Center"" },
            ""VerticalAlignment"": { ""Value"": ""Bottom"" },
            ""TextWrapping"": { ""Value"": ""Wrap"" },
            ""FontWeight"": { ""Value"": ""Bold"" }
          }
        }
      }
    }
  }
}
";

        // JSON для проверки attached enum: DockPanel.Dock
        private const string DockPanelJsonModel = @"
{
  ""FormName"": ""DockPanelControl"",
  ""NamespaceSuffix"": ""Forms"",
  ""ParentClassType"": ""Avalonia.Controls.UserControl"",
  ""Properties"": {
    ""Content"": {
      ""Type"": ""Avalonia.Controls.DockPanel"",
      ""Properties"": {
        ""Name"": { ""Value"": ""RootDock"" },
        ""Children"": [
          {
            ""Type"": ""Avalonia.Controls.Border"",
            ""Properties"": {
              ""Name"": { ""Value"": ""TopBorder"" },
              ""Avalonia.Controls.DockPanel.Dock"": { ""Value"": ""Top"" }
            }
          }
        ]
      }
    }
  }
}
";

        [Fact]
        public void Enum_properties_should_be_generated_with_fully_qualified_enum_members()
        {
            // act
            var generated = GeneratorTestHelper.RunGenerator(
                DummyUserControlSource,
                ("EnumTestControl.Model.json", EnumJsonModel));

            var source = generated
                .FirstOrDefault(g => g.HintName == "EnumTestControl.g.cs")
                .SourceText
                .ToString();
            
            Assert.Fail(source);

            // Проверяем, что текстовый блок объявлен
            Assert.Contains("internal global::Avalonia.Controls.TextBlock EnumText;", source);

            // HorizontalAlignment.Center
            Assert.Contains(
                "this.EnumText.HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Center;",
                source);

            // VerticalAlignment.Bottom
            Assert.Contains(
                "this.EnumText.VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Bottom;",
                source);

            // TextWrapping.Wrap
            Assert.Contains(
                "this.EnumText.TextWrapping = global::Avalonia.Media.TextWrapping.Wrap;",
                source);

            // FontWeight.Bold
            Assert.Contains(
                "this.EnumText.FontWeight = global::Avalonia.Media.FontWeight.Bold;",
                source);
        }

        [Fact]
        public void Attached_enum_DockPanel_Dock_should_be_generated_correctly()
        {
            const string dockPanelUserControlSource = @"
using Avalonia.Controls;

namespace TestApp.Forms
{
    public partial class DockPanelControl : UserControl
    {
    }
}
";

            var generated = GeneratorTestHelper.RunGenerator(
                dockPanelUserControlSource,
                ("DockPanelControl.Model.json", DockPanelJsonModel));

            var source = generated
                .FirstOrDefault(g => g.HintName == "DockPanelControl.g.cs")
                .SourceText
                .ToString();
            
            Assert.Fail(source);
            
            // Поле TopBorder
            Assert.Contains("internal global::Avalonia.Controls.Border TopBorder;", source);

            // Attached property: DockPanel.SetDock(..., Dock.Top)
            Assert.Contains(
                "global::Avalonia.Controls.DockPanel.SetDock(this.TopBorder, global::Avalonia.Controls.Dock.Top);",
                source);
        }
    }
}
