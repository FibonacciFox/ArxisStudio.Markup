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

        private const string EnumJsonModel = @"
{
  ""AssetType"": ""UserControl"",
  ""Properties"": {
    ""Content"": {
      ""Type"": ""Avalonia.Controls.Border"",
      ""Properties"": {
        ""Child"": {
          ""Type"": ""Avalonia.Controls.TextBlock"",
          ""Properties"": {
            ""Name"": ""EnumText"",
            ""Text"": ""Hello enums!"",
            ""HorizontalAlignment"": ""Center"",
            ""VerticalAlignment"": ""Bottom"",
            ""TextWrapping"": ""Wrap"",
            ""FontWeight"": ""Bold""
          }
        }
      }
    }
  }
}
";

        private const string DockPanelJsonModel = @"
{
  ""AssetType"": ""UserControl"",
  ""Properties"": {
    ""Content"": {
      ""Type"": ""Avalonia.Controls.DockPanel"",
      ""Properties"": {
        ""Name"": ""RootDock"",
        ""Children"": [
          {
            ""Type"": ""Avalonia.Controls.Border"",
            ""Properties"": {
              ""Name"": ""TopBorder"",
              ""Avalonia.Controls.DockPanel.Dock"": ""Top""
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
            var source = GeneratorTestHelper.GetGeneratedSource(
                DummyUserControlSource,
                "EnumTestControl.cs",
                "TestApp.Forms.EnumTestControl.g.cs",
                ("EnumTestControl.Asset", EnumJsonModel));

            Assert.Contains("internal global::Avalonia.Controls.TextBlock EnumText;", source);
            Assert.Contains(
                "this.EnumText.HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Center;",
                source);
            Assert.Contains(
                "this.EnumText.VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Bottom;",
                source);
            Assert.Contains(
                "this.EnumText.TextWrapping = global::Avalonia.Media.TextWrapping.Wrap;",
                source);
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

            var source = GeneratorTestHelper.GetGeneratedSource(
                dockPanelUserControlSource,
                "DockPanelControl.cs",
                "TestApp.Forms.DockPanelControl.g.cs",
                ("DockPanelControl.Asset", DockPanelJsonModel));
            
            Assert.Contains("internal global::Avalonia.Controls.Border TopBorder;", source);
            Assert.Contains(
                "global::Avalonia.Controls.DockPanel.SetDock(this.TopBorder, global::Avalonia.Controls.Dock.Top);",
                source);
        }
    }
}
