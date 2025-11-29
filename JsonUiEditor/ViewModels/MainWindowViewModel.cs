using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Controls;
using Newtonsoft.Json;
using JsonUiEditor.Models;
using JsonUiEditor.Services;
using System;
using System.Linq;

namespace JsonUiEditor.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _jsonText = "";

        [ObservableProperty]
        private Control? _renderedContent;

        [ObservableProperty]
        private string _errorMessage = "";

        partial void OnJsonTextChanged(string value)
        {
            RenderUi();
        }

        private void RenderUi()
        {
            if (string.IsNullOrWhiteSpace(JsonText)) return;

            try
            {
                ErrorMessage = ""; 

                // 1. Десериализация в корневую модель (RootModel)
                var rootModel = JsonConvert.DeserializeObject<RootModel>(JsonText);

                if (rootModel == null || rootModel.Properties == null) return;
                
                // 2. Получаем корневой контрол из словаря Properties
                if (rootModel.Properties.TryGetValue("Content", out object? contentToken))
                {
                    // Преобразуем объект в ControlModel (через JObject)
                    var jObject = contentToken as Newtonsoft.Json.Linq.JObject;
                    if (jObject == null) throw new InvalidOperationException("Root Content is not a complex object.");
                    
                    var contentModel = jObject.ToObject<ControlModel>();
                    
                    if (contentModel == null) return;

                    // 3. Построение UI
                    RenderedContent = UiBuilder.Build(contentModel);
                }
            }
            catch (Exception ex)
            {
                // Для отладки
                ErrorMessage = $"Error: {ex.Message}";
            }
        }
        
        public MainWindowViewModel()
        {
            // Ваш предоставленный JSON-пример
            JsonText = @"{
  ""FormName"": ""SettingsPanel"",
  ""NamespaceSuffix"": ""Forms"",
  ""ParentClassType"": ""Avalonia.Controls.UserControl"",
  ""Properties"": {
    ""Content"": {
      ""Type"": ""Avalonia.Controls.Border"",
      ""Properties"": {
        ""BorderThickness"": ""3"",
        ""BorderBrush"": ""Green"",
        ""Child"": {
          ""Type"": ""Avalonia.Controls.ScrollViewer"",
          ""Properties"": {
            ""Content"": {
              ""Type"": ""Avalonia.Controls.StackPanel"",
              ""Properties"": {
                ""Children"": [
                  {
                    ""Type"": ""Avalonia.Controls.TextBlock"",
                    ""Properties"": {
                      ""Text"": ""Global Settings"",
                      ""FontSize"": 20
                    }
                  },
                  {
                    ""Type"": ""Avalonia.Controls.Expander"",
                    ""Properties"": {
                      ""Header"": ""Advanced Options"",
                      ""IsExpanded"": true,
                      ""Content"": {
                        ""Type"": ""Avalonia.Controls.StackPanel"",
                        ""Properties"": {
                          ""Margin"": ""10"",
                          ""Children"": [
                            {
                              ""Type"": ""Avalonia.Controls.TextBlock"",
                              ""Properties"": {
                                ""Text"": ""Option A""
                              }
                            },
                            {
                              ""Type"": ""Avalonia.Controls.ToggleSwitch"",
                              ""Properties"": {
                                ""IsChecked"": true
                              }
                            }
                          ]
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
    }
  }
}";
            RenderUi();
        }
    }
}