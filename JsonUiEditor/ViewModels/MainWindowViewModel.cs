using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Controls;
using Newtonsoft.Json;
using JsonUiEditor.Models;
using JsonUiEditor.Services;
using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using Avalonia.Media;

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
        
        // --- Свойства конструктора ---
        
        [ObservableProperty]
        private ControlNode? _controlTreeRoot;
        
        [ObservableProperty]
        private ControlNode? _selectedNode; 
        
        [ObservableProperty]
        private ObservableCollection<PropertyItem> _editableProperties = new();

        // Коллекция доступных контролов (для Toolbox)
        public ObservableCollection<JObject> AvailableControls { get; } = new()
        {
            new JObject
            {
                { "Type", "Avalonia.Controls.Button" },
                { "Properties", new JObject 
                    {
                        { "Content", "New Button" },
                        { "Width", 100 } 
                    }
                }
            },
            new JObject
            {
                { "Type", "Avalonia.Controls.StackPanel" },
                { "Properties", new JObject 
                    {
                        { "Orientation", "Vertical" },
                        { "Children", new JArray() } 
                    }
                }
            },
            new JObject
            {
                { "Type", "Avalonia.Controls.TextBlock" },
                { "Properties", new JObject 
                    {
                        { "Text", "New TextBlock" },
                        { "FontSize", 16 }
                    }
                }
            },
            new JObject
            {
                { "Type", "Avalonia.Controls.Border" },
                { "Properties", new JObject 
                    {
                        { "Width", 150 },
                        { "Height", 50 },
                        { "BorderBrush", "Gray" },
                        { "BorderThickness", 1 }
                    }
                }
            }
        };

        public MainWindowViewModel()
        {
            // Ваш JSON-пример
            JsonText = @"{
  ""FormName"": ""DrawingSurface"",
  ""NamespaceSuffix"": ""Forms"",
  ""ParentClassType"": ""Avalonia.Controls.UserControl"",
  ""Properties"": {
    ""Width"": 600,
    ""Height"": 600,
    ""Content"": {
      ""Type"": ""Avalonia.Controls.Canvas"",
      ""Properties"": {
        ""Width"": 400,
        ""Height"": 400,
        ""Background"": ""Black"",
        ""Children"": [
          {
            ""Type"": ""Avalonia.Controls.Shapes.Rectangle"",
            ""Properties"": {
              ""Width"": 100,
              ""Height"": 100,
              ""Fill"": ""Red"",
              ""Avalonia.Controls.Canvas.Left"": 50,
              ""Avalonia.Controls.Canvas.Top"": 50
            }
          },
          {
            ""Type"": ""Avalonia.Controls.Shapes.Ellipse"",
            ""Properties"": {
              ""Width"": 80,
              ""Height"": 80,
              ""Fill"": ""Blue"",
              ""Stroke"": ""White"",
              ""StrokeThickness"": 2,
              ""Avalonia.Controls.Canvas.Left"": 200,
              ""Avalonia.Controls.Canvas.Top"": 100
            }
          }
        ]
      }
    }
  }
}";
            DesignerService.Instance.JsonChanged += UpdateUIFromRootModel;
            UpdateTreeAndUIFromText();
        }

        partial void OnJsonTextChanged(string value)
        {
            UpdateTreeAndUIFromText();
        }

        partial void OnSelectedNodeChanged(ControlNode? oldNode, ControlNode? newNode)
        {
            LoadProperties(newNode);
            // Явно оповещаем команду об изменении SelectedNode
            DeleteSelectedControlCommand.NotifyCanExecuteChanged(); 
        }
        
        // --- Основная логика синхронизации и исправления ошибки ---

        private void UpdateTreeAndUIFromText()
        {
            try
            {
                ErrorMessage = "";
                var rootJson = JObject.Parse(JsonText);
                
                // Это первый запуск, просто строим и обновляем UI
                ControlTreeRoot = TreeBuilder.BuildTree(rootJson);

                UpdateUIFromRootModel();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"JSON Parse Error: {ex.Message}";
                ControlTreeRoot = null;
            }
        }

        private void UpdateUIFromRootModel()
        {
            if (ControlTreeRoot?.ParentJsonContainer == null) return;
            
            // 1. СОХРАНЯЕМ JSON-ссылку текущего выделенного узла
            JObject? selectedJson = SelectedNode?.JsonNode;
            
            try
            {
                var rootJson = (JObject)ControlTreeRoot.ParentJsonContainer;
                var rootModel = rootJson.ToObject<RootModel>();
                
                if (rootModel == null || rootModel.Properties == null) return;

                // 2. Обновляем JsonText (СИНХРОНИЗАЦИЯ)
                JsonText = JsonConvert.SerializeObject(rootModel, Formatting.Indented);

                // 3. Строим UI (Рендеринг)
                if (rootModel.Properties.TryGetValue("Content", out object? contentToken) && contentToken is JObject jObject)
                {
                    var contentModel = jObject.ToObject<ControlModel>();
                    var builtControl = UiBuilder.Build(contentModel!);

                    // Обертывание в корневой контейнер
                    var rootContainer = new Border
                    {
                        Width = rootModel.Properties.ContainsKey("Width") ? Convert.ToDouble(rootModel.Properties["Width"]) : double.NaN,
                        Height = rootModel.Properties.ContainsKey("Height") ? Convert.ToDouble(rootModel.Properties["Height"]) : double.NaN,
                        Child = builtControl
                    };
                    RenderedContent = rootContainer;
                }
                
                // 4. ПЕРЕСТРАИВАЕМ ДЕРЕВО: Это создает НОВЫЕ ControlNode объекты
                ControlTreeRoot = TreeBuilder.BuildTree(rootJson);
                
                // 5. ВОССТАНОВЛЕНИЕ: Ищем предыдущий выделенный JSON-объект в новом дереве
                if (selectedJson != null)
                {
                    var newlySelectedNode = FindNodeByJson(ControlTreeRoot, selectedJson);
                    
                    if (newlySelectedNode != null)
                    {
                        SelectedNode = newlySelectedNode;
                        // Принудительно уведомляем View о том, что SelectedNode изменился,
                        // чтобы TreeView обновил выделение
                        OnPropertyChanged(nameof(SelectedNode));
                    }
                }
                
                // 6. Перезагружаем свойства для текущего узла (который теперь восстановлен)
                LoadProperties(SelectedNode);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Rendering Error: {ex.Message}";
            }
        }

        // Вспомогательный метод: ищет узел в дереве по ссылке на исходный JObject
        private ControlNode? FindNodeByJson(ControlNode? root, JObject targetJson)
        {
            if (root == null) return null;

            // Сравнение по ссылке (так как JObject — это объект-ссылка)
            if (root.JsonNode == targetJson)
            {
                return root;
            }

            // Рекурсивный поиск
            foreach (var child in root.Children)
            {
                var found = FindNodeByJson(child, targetJson);
                if (found != null)
                {
                    return found;
                }
            }
            
            return null;
        }


        private void LoadProperties(ControlNode? node)
        {
            EditableProperties.Clear();

            if (node?.JsonNode.TryGetValue("Properties", out JToken? propertiesToken) == true && propertiesToken is JObject propertiesObject)
            {
                foreach (var prop in propertiesObject.Properties())
                {
                    if (prop.Value is JValue jValue)
                    {
                        var item = new PropertyItem(
                            prop.Name, 
                            jValue.Value, 
                            propertiesObject
                        );
                        EditableProperties.Add(item);
                    }
                }
            }
        }
        
        // --- Команды конструктора ---

        [RelayCommand]
        private void AddNewControl(JObject? controlTemplate)
        {
            if (SelectedNode == null)
            {
                ErrorMessage = "Please select a container control (e.g., StackPanel, Grid) first.";
                return;
            }
            
            if (controlTemplate == null) return;

            JObject newControlJson = (JObject)controlTemplate.DeepClone();

            if (!SelectedNode.JsonNode.TryGetValue("Properties", out JToken? propertiesToken) || propertiesToken is not JObject propertiesObject)
            {
                ErrorMessage = $"Selected node {SelectedNode.DisplayName} does not have a 'Properties' object.";
                return;
            }

            JArray? childrenArray = propertiesObject.Properties()
                                                  .Where(p => p.Value is JArray)
                                                  .Select(p => p.Value as JArray)
                                                  .FirstOrDefault();

            if (childrenArray == null)
            {
                ErrorMessage = $"Selected node '{SelectedNode.DisplayName}' does not have a suitable JArray (e.g., 'Children') property to add controls.";
                return;
            }

            childrenArray.Add(newControlJson);
            DesignerService.Instance.NotifyJsonChanged();
            ErrorMessage = "";
        }

        [RelayCommand(CanExecute = nameof(CanDeleteSelectedControl))]
        private void DeleteSelectedControl()
        {
            if (SelectedNode == null || SelectedNode.ParentJsonContainer == null) return;

            if (SelectedNode.ParentJsonContainer is JArray parentArray)
            {
                parentArray.Remove(SelectedNode.JsonNode);
            }
            else if (SelectedNode.ParentJsonContainer is JObject parentObject)
            {
                var propertyToRemove = parentObject.Properties()
                                                 .FirstOrDefault(p => p.Value == SelectedNode.JsonNode);
                
                propertyToRemove?.Remove();
            }

            SelectedNode = null;
            DesignerService.Instance.NotifyJsonChanged();
            ErrorMessage = "";
        }

        public bool CanDeleteSelectedControl => SelectedNode != null && 
                                               SelectedNode.ParentJsonContainer != null && 
                                               SelectedNode.DisplayName != "Root Form";
    }
}