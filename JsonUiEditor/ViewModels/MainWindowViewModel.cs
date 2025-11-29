using CommunityToolkit.Mvvm.ComponentModel; // Если используется CommunityToolkit
using Avalonia.Controls;
using Newtonsoft.Json;
using JsonUiEditor.Models;
using JsonUiEditor.Services;
using System;

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

        // Этот метод вызывается автоматически при изменении JsonText (если используете CommunityToolkit)
        // Либо вызовите его в сеттере свойства JsonText
        partial void OnJsonTextChanged(string value)
        {
            RenderUi();
        }

        private void RenderUi()
        {
            if (string.IsNullOrWhiteSpace(JsonText)) return;

            try
            {
                ErrorMessage = ""; // Очищаем ошибки

                // 1. Десериализация JSON
                // Сначала пробуем как полный объект с Content
                // Для простоты примера, будем считать, что JSON описывает сразу ControlModel
                var model = JsonConvert.DeserializeObject<ControlModel>(JsonText);

                if (model == null) return;

                // 2. Построение UI
                RenderedContent = UiBuilder.Build(model);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error: {ex.Message}";
                // Опционально: очистить превью или оставить старое
            }
        }
        
        // Начальный JSON для примера
        public MainWindowViewModel()
        {
            JsonText = @"{
  ""Type"": ""StackPanel"",
  ""Properties"": {
    ""Background"": ""#F0F0F0"",
    ""Margin"": ""20""
  },
  ""Children"": [
    {
      ""Type"": ""TextBlock"",
      ""Properties"": {
        ""Text"": ""Hello from JSON!"",
        ""FontSize"": ""24"",
        ""Foreground"": ""Blue"",
        ""HorizontalAlignment"": ""Center""
      }
    },
    {
      ""Type"": ""Button"",
      ""Properties"": {
        ""Content"": ""Click Me"",
        ""HorizontalAlignment"": ""Center""
      }
    }
  ]
}";
        }
    }
}