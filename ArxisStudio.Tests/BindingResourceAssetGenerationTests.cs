using Xunit;

namespace ArxisStudio.Markup.Json.Generator.Tests
{
    public class BindingResourceAssetGenerationTests
    {
        private const string DummyUserControlSource = @"
using Avalonia.Controls;

namespace TestApp.Views
{
    public partial class BindingResourceAssetControl : UserControl
    {
    }
}
";

        private const string JsonModel = @"
{
  ""SchemaVersion"": 1,
  ""Kind"": ""Control"",
  ""Class"": ""TestApp.Views.BindingResourceAssetControl"",
  ""Root"": {
    ""TypeName"": ""Avalonia.Controls.UserControl"",
    ""Properties"": {
      ""Background"": {
        ""$resource"": ""BackgroundBrush""
      },
      ""Content"": {
        ""TypeName"": ""Avalonia.Controls.StackPanel"",
        ""Properties"": {
          ""Children"": [
            {
              ""TypeName"": ""Avalonia.Controls.TextBlock"",
              ""Properties"": {
                ""Name"": ""MessageText"",
                ""Foreground"": {
                  ""$resource"": ""ForegroundBrush""
                },
                ""Text"": {
                  ""$binding"": ""UserName"",
                  ""Mode"": ""TwoWay"",
                  ""StringFormat"": ""Hello {0}"",
                  ""RelativeSource"": {
                    ""Mode"": ""Self""
                  }
                }
              }
            },
            {
              ""TypeName"": ""Avalonia.Controls.Image"",
              ""Properties"": {
                ""Name"": ""AvatarImage"",
                ""Source"": {
                  ""$asset"": {
                    ""Path"": ""/Assets/avatar.png"",
                    ""Assembly"": ""Sample.Assembly""
                  }
                }
              }
            }
          ]
        }
      }
    }
  }
}
";

        [Fact]
        public void Binding_resource_and_asset_values_should_be_generated_correctly()
        {
            var source = GeneratorTestHelper.GetGeneratedSource(
                DummyUserControlSource,
                "BindingResourceAssetControl.arxui.cs",
                "TestApp.Views.BindingResourceAssetControl.g.cs",
                ("BindingResourceAssetControl.arxui", JsonModel));

            Assert.Contains(
                "this.Bind(global::Avalonia.Controls.UserControl.BackgroundProperty, this.GetResourceObservable(\"BackgroundBrush\"));",
                source);
            Assert.Contains(
                "this.MessageText.Bind(global::Avalonia.Controls.TextBlock.TextProperty, new global::Avalonia.Data.Binding(\"UserName\") { Mode = global::Avalonia.Data.BindingMode.TwoWay, StringFormat = \"Hello {0}\", RelativeSource = new global::Avalonia.Data.RelativeSource(global::Avalonia.Data.RelativeSourceMode.Self) });",
                source);
            Assert.Contains(
                "this.MessageText.Bind(global::Avalonia.Controls.TextBlock.ForegroundProperty, this.GetResourceObservable(\"ForegroundBrush\"));",
                source);
            Assert.Contains(
                "this.AvatarImage.Source = new global::Avalonia.Media.Imaging.Bitmap(global::Avalonia.Platform.AssetLoader.Open(new global::System.Uri(\"avares://Sample.Assembly/Assets/avatar.png\")));",
                source);
        }
    }
}
