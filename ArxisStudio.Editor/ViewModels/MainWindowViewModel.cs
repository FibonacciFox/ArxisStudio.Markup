using ArxisStudio.Markup.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Controls;
using ArxisStudio.Markup.Json.Loader.Services;
using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using ArxisStudio.Markup.Json.Loader.Models;

namespace ArxisStudio.Markup.Json.Loader.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly ProjectDiscoveryService _projectDiscoveryService = new();
        private bool _suppressJsonTextChanged;
        private string? _currentDocumentPath;

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

        [ObservableProperty]
        private string _projectPathInput = "";

        [ObservableProperty]
        private string _projectSummary = "No project loaded.";

        [ObservableProperty]
        private ProjectContext? _loadedProject;

        [ObservableProperty]
        private ProjectFileItem? _selectedProjectFile;

        public ObservableCollection<ProjectFileItem> ProjectArxuiFiles { get; } = new();

        // Коллекция доступных контролов (для Toolbox)
        public ObservableCollection<JObject> AvailableControls { get; } = new()
        {
            new JObject
            {
                { "TypeName", "Avalonia.Controls.Button" },
                { "Properties", new JObject 
                    {
                        { "Content", "New Button" },
                        { "Width", 100 } 
                    }
                }
            },
            new JObject
            {
                { "TypeName", "Avalonia.Controls.StackPanel" },
                { "Properties", new JObject 
                    {
                        { "Orientation", "Vertical" },
                        { "Children", new JArray() } 
                    }
                }
            },
            new JObject
            {
                { "TypeName", "Avalonia.Controls.TextBlock" },
                { "Properties", new JObject 
                    {
                        { "Text", "New TextBlock" },
                        { "FontSize", 16 }
                    }
                }
            },
            new JObject
            {
                { "TypeName", "Avalonia.Controls.Border" },
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
            ProjectPathInput = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ArxisStudio.Template", "ArxisStudio.Markup.Template.csproj"));
            JsonText = @"{
  ""SchemaVersion"": 1,
  ""Kind"": ""Control"",
  ""Root"": {
    ""TypeName"": ""Avalonia.Controls.UserControl"",
    ""Properties"": {
      ""Width"": 600,
      ""Height"": 600,
      ""Content"": {
        ""TypeName"": ""Avalonia.Controls.Canvas"",
        ""Properties"": {
          ""Width"": 400,
          ""Height"": 400,
          ""Background"": ""Black"",
          ""Children"": [
            {
              ""TypeName"": ""Avalonia.Controls.Shapes.Rectangle"",
              ""Properties"": {
                ""Width"": 100,
                ""Height"": 100,
                ""Fill"": ""Red"",
                ""Avalonia.Controls.Canvas.Left"": 50,
                ""Avalonia.Controls.Canvas.Top"": 50
              }
            },
            {
              ""TypeName"": ""Avalonia.Controls.Shapes.Ellipse"",
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
  }
}";
            DesignerService.Instance.JsonChanged += OnJsonChanged;
            UpdateTreeAndUIFromText();
        }

        private void OnJsonChanged(DesignerChangeKind changeKind)
        {
            UpdateUIFromRootModel(changeKind == DesignerChangeKind.Structure);
        }

        partial void OnJsonTextChanged(string value)
        {
            if (_suppressJsonTextChanged)
            {
                return;
            }

            UpdateTreeAndUIFromText();
        }

        partial void OnSelectedNodeChanged(ControlNode? oldValue, ControlNode? newValue)
        {
            LoadProperties(newValue);
            // Явно оповещаем команду об изменении SelectedNode
            DeleteSelectedControlCommand.NotifyCanExecuteChanged(); 
        }

        partial void OnSelectedProjectFileChanged(ProjectFileItem? oldValue, ProjectFileItem? newValue)
        {
            OpenSelectedProjectFileCommand.NotifyCanExecuteChanged();
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

        private void UpdateUIFromRootModel(bool rebuildTree = true)
        {
            if (ControlTreeRoot?.ParentJsonContainer == null) return;
            
            // 1. СОХРАНЯЕМ JSON-ссылку текущего выделенного узла
            JObject? selectedJson = SelectedNode?.JsonNode;
            
            try
            {
                var rootJson = (JObject)ControlTreeRoot.ParentJsonContainer;
                var rootModel = ArxuiSerializer.Deserialize(rootJson.ToString());
                
                if (rootModel == null) return;

                // 2. Обновляем JsonText (СИНХРОНИЗАЦИЯ)
                _suppressJsonTextChanged = true;
                try
                {
                    JsonText = ArxuiSerializer.Serialize(rootModel);
                    if (!string.IsNullOrWhiteSpace(_currentDocumentPath))
                    {
                        File.WriteAllText(_currentDocumentPath!, JsonText);
                    }
                }
                finally
                {
                    _suppressJsonTextChanged = false;
                }

                // 3. Строим UI (Рендеринг)
                RenderedContent = UiBuilder.Build(rootModel.Root, LoadedProject);
                
                if (!rebuildTree)
                {
                    return;
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
                            jValue, 
                            propertiesObject
                        );
                        EditableProperties.Add(item);
                    }
                }
            }
        }
        
        // --- Команды конструктора ---

        [RelayCommand]
        private void LoadProject()
        {
            try
            {
                ErrorMessage = "";
                var project = _projectDiscoveryService.Load(ProjectPathInput);
                LoadedProject = project;

                ProjectArxuiFiles.Clear();
                foreach (var file in project.ArxuiFiles)
                {
                    ProjectArxuiFiles.Add(file);
                }

                ProjectSummary =
                    $"Project: {Path.GetFileName(project.ProjectPath)} | Assembly: {project.AssemblyName} | TFM: {project.TargetFramework} | .arxui: {project.ArxuiFiles.Count} | .axaml: {project.AxamlFiles.Count}";

                if (ProjectArxuiFiles.Count > 0)
                {
                    SelectedProjectFile = ProjectArxuiFiles[0];
                    OpenProjectFile(ProjectArxuiFiles[0]);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Project Load Error: {ex.Message}";
            }
        }

        [RelayCommand(CanExecute = nameof(CanOpenSelectedProjectFile))]
        private void OpenSelectedProjectFile()
        {
            if (SelectedProjectFile != null)
            {
                OpenProjectFile(SelectedProjectFile);
            }
        }

        private void OpenProjectFile(ProjectFileItem file)
        {
            try
            {
                ErrorMessage = "";
                _currentDocumentPath = file.FullPath;

                _suppressJsonTextChanged = true;
                try
                {
                    JsonText = File.ReadAllText(file.FullPath);
                }
                finally
                {
                    _suppressJsonTextChanged = false;
                }

                UpdateTreeAndUIFromText();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Open File Error: {ex.Message}";
            }
        }

        public bool CanOpenSelectedProjectFile => SelectedProjectFile != null;

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
            DesignerService.Instance.NotifyJsonChanged(DesignerChangeKind.Structure);
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
            DesignerService.Instance.NotifyJsonChanged(DesignerChangeKind.Structure);
            ErrorMessage = "";
        }

        public bool CanDeleteSelectedControl => SelectedNode != null && 
                                               SelectedNode.ParentJsonContainer != null && 
                                               SelectedNode.DisplayName != "Root Document";
    }
}
