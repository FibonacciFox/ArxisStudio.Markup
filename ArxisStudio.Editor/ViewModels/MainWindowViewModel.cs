using ArxisStudio.Markup;
using ArxisStudio.Markup.Json;
using Avalonia;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Controls;
using ArxisStudio.Markup.Json.Loader;
using ArxisStudio.Editor.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using ArxisStudio.Editor.Models;
using System.Threading.Tasks;
using ArxisStudio.Designer.Abstractions;
using ArxisStudio.Designer.Models;
using ArxisStudio.Designer.Services;
using ArxisStudio.Markup.Json.Loader.Services;
using ArxisStudio.Markup.Workspace.Models;
using ArxisStudio.Markup.Workspace.Services;
using ArxisStudio.Designer;

namespace ArxisStudio.Editor.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly ProjectDiscoveryService _projectDiscoveryService = new();
        private readonly RoslynWorkspaceService _workspaceService = new();
        private readonly ArxuiSemanticValidator _semanticValidator = new();
        private bool _suppressJsonTextChanged;
        private bool _selectionSyncInProgress;
        private bool _suppressTabSelectionChanged;
        private string? _currentDocumentPath;
        private Process? _runningProcess;
        private Dictionary<string, UiNode> _uiNodesByPath = new();
        private Dictionary<string, ControlNode> _controlNodesByPath = new();

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
        private WorkspaceContext? _loadedWorkspace;

        [ObservableProperty]
        private ProjectFileItem? _selectedProjectFile;

        [ObservableProperty]
        private ProjectTreeItem? _selectedProjectTreeItem;

        [ObservableProperty]
        private OpenDocumentTab? _selectedOpenDocumentTab;

        [ObservableProperty]
        private string _workspaceMode = "Designer";

        [ObservableProperty]
        private double _previewZoom = 1.0;

        [ObservableProperty]
        private string _runOutput = "";

        [ObservableProperty]
        private string _runStatus = "No process started.";

        [ObservableProperty]
        private bool _isProjectRunning;

        [ObservableProperty]
        private double _previewSurfaceWidth = 1280;

        [ObservableProperty]
        private double _previewSurfaceHeight = 800;

        [ObservableProperty]
        private UiDocument? _currentDocument;

        public bool IsDesignerMode => WorkspaceMode == "Designer";

        public bool IsSourceMode => WorkspaceMode == "Source";

        public bool IsSplitMode => WorkspaceMode == "Split";

        public string PreviewZoomPercent => $"{PreviewZoom * 100:0}%";

        public bool CanRunProject => !IsProjectRunning && !string.IsNullOrWhiteSpace(ProjectPathInput);

        public bool CanStopProject => IsProjectRunning;

        public IDesignerPreviewBuilder DesignerPreviewBuilder { get; } = new EditorDesignerPreviewBuilder();

        public IDesignerSelectionService DesignerSelectionService { get; } = new DesignerSelectionService();

        public ObservableCollection<ProjectFileItem> ProjectArxuiFiles { get; } = new();

        public ObservableCollection<ProjectTreeItem> ProjectTreeItems { get; } = new();

        public ObservableCollection<OpenDocumentTab> OpenDocumentTabs { get; } = new();

        public ObservableCollection<ToolboxItem> AvailableControls { get; } = new();

        public ObservableCollection<DesignEditorItem> DesignerItems { get; } = new();
        public ObservableCollection<DesignEditorItem> SplitDesignerItems { get; } = new();

        public MainWindowViewModel()
        {
            ProjectPathInput = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ArxisStudio.Template", "ArxisStudio.Markup.Template.csproj"));
            DesignerService.Instance.JsonChanged += OnJsonChanged;
            DesignerSelectionService.SelectedNodeChanged += OnDesignerSelectionChanged;
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
            SyncPreviewSelectionFromOutline(newValue);
            // Явно оповещаем команду об изменении SelectedNode
            DeleteSelectedControlCommand.NotifyCanExecuteChanged(); 
        }

        partial void OnSelectedProjectFileChanged(ProjectFileItem? oldValue, ProjectFileItem? newValue)
        {
            OpenSelectedProjectFileCommand.NotifyCanExecuteChanged();
        }

        partial void OnSelectedProjectTreeItemChanged(ProjectTreeItem? oldValue, ProjectTreeItem? newValue)
        {
            if (newValue == null || newValue.IsDirectory || !newValue.FullPath.EndsWith(".arxui", StringComparison.OrdinalIgnoreCase))
            {
                SelectedProjectFile = null;
                OpenSelectedProjectFileCommand.NotifyCanExecuteChanged();
                return;
            }

            SelectedProjectFile = ProjectArxuiFiles.FirstOrDefault(file =>
                string.Equals(file.FullPath, newValue.FullPath, StringComparison.OrdinalIgnoreCase));
            OpenSelectedProjectFileCommand.NotifyCanExecuteChanged();
        }

        partial void OnSelectedOpenDocumentTabChanged(OpenDocumentTab? oldValue, OpenDocumentTab? newValue)
        {
            if (_suppressTabSelectionChanged || newValue == null)
            {
                return;
            }

            var file = ResolveArxuiFile(newValue.FullPath);
            if (file == null)
            {
                return;
            }

            SelectedProjectFile = file;
            SelectedProjectTreeItem = FindTreeItemByPath(file.FullPath, ProjectTreeItems);
            OpenProjectFile(file, ensureTabSelection: false);
        }

        partial void OnLoadedProjectChanged(ProjectContext? value)
        {
            if (DesignerPreviewBuilder is EditorDesignerPreviewBuilder builder)
            {
                builder.ProjectContext = value;
            }

            RunProjectCommand.NotifyCanExecuteChanged();
        }

        partial void OnLoadedWorkspaceChanged(WorkspaceContext? value)
        {
            RebuildToolbox(value);
            LoadProperties(SelectedNode);
        }

        partial void OnProjectPathInputChanged(string value)
        {
            OnPropertyChanged(nameof(CanRunProject));
            RunProjectCommand.NotifyCanExecuteChanged();
        }

        partial void OnWorkspaceModeChanged(string value)
        {
            OnPropertyChanged(nameof(IsDesignerMode));
            OnPropertyChanged(nameof(IsSourceMode));
            OnPropertyChanged(nameof(IsSplitMode));
        }

        partial void OnPreviewZoomChanged(double value)
        {
            if (value < 0.25)
            {
                PreviewZoom = 0.25;
                return;
            }

            if (value > 3.0)
            {
                PreviewZoom = 3.0;
                return;
            }

            OnPropertyChanged(nameof(PreviewZoomPercent));
        }

        partial void OnIsProjectRunningChanged(bool value)
        {
            OnPropertyChanged(nameof(CanRunProject));
            OnPropertyChanged(nameof(CanStopProject));
            RunProjectCommand.NotifyCanExecuteChanged();
            StopProjectCommand.NotifyCanExecuteChanged();
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
                CurrentDocument = null;
                DesignerItems.Clear();
                SplitDesignerItems.Clear();
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

                PreviewSurfaceWidth = rootModel.Design?.SurfaceWidth ?? 1280;
                PreviewSurfaceHeight = rootModel.Design?.SurfaceHeight ?? 800;
                CurrentDocument = rootModel;
                _uiNodesByPath = BuildUiNodeIndex(rootModel.Root);

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
                var builder = DesignerPreviewBuilder as EditorDesignerPreviewBuilder;
                var primaryScene = (builder ?? new EditorDesignerPreviewBuilder()).Build(rootModel, new DesignerSurfaceContext(PreviewZoom));
                var splitScene = (builder ?? new EditorDesignerPreviewBuilder()).Build(rootModel, new DesignerSurfaceContext(PreviewZoom));

                RenderedContent = primaryScene.RootControl;
                RebuildDesignerItems(primaryScene.RootControl, splitScene.RootControl);
                
                if (!rebuildTree)
                {
                    return;
                }

                // 4. ПЕРЕСТРАИВАЕМ ДЕРЕВО: Это создает НОВЫЕ ControlNode объекты
                ControlTreeRoot = TreeBuilder.BuildTree(rootJson);
                _controlNodesByPath = BuildControlNodeIndex(ControlTreeRoot);
                
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
                DesignerItems.Clear();
                SplitDesignerItems.Clear();
            }
        }

        private void RebuildDesignerItems(Control? primaryPreview, Control? splitPreview)
        {
            DesignerItems.Clear();
            SplitDesignerItems.Clear();

            if (primaryPreview != null)
            {
                DesignerItems.Add(new DesignEditorItem
                {
                    Content = primaryPreview,
                    Location = new Point(0, 0),
                    Width = PreviewSurfaceWidth,
                    Height = PreviewSurfaceHeight,
                    IsDraggable = false
                });
            }

            if (splitPreview != null)
            {
                SplitDesignerItems.Add(new DesignEditorItem
                {
                    Content = splitPreview,
                    Location = new Point(0, 0),
                    Width = PreviewSurfaceWidth,
                    Height = PreviewSurfaceHeight,
                    IsDraggable = false
                });
            }

            if (DesignerItems.Count == 0 && SplitDesignerItems.Count == 0)
            {
                ErrorMessage = "Designer preview is empty: preview builder returned no visual root.";
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

            if (node?.JsonNode.TryGetValue("Properties", out JToken? propertiesToken) != true || propertiesToken is not JObject propertiesObject)
            {
                return;
            }

            var typeName = node.JsonNode["TypeName"]?.ToString();
            if (!string.IsNullOrWhiteSpace(typeName) &&
                LoadedWorkspace != null &&
                LoadedWorkspace.Types.TryGetValue(typeName, out var typeMetadata))
            {
                foreach (var property in typeMetadata.Properties.Where(property => property.CanWrite && !property.IsCollection))
                {
                    propertiesObject.TryGetValue(property.Name, out var existingValue);
                    EditableProperties.Add(new PropertyItem(property.Name, existingValue, propertiesObject, property.TypeName));
                }

                foreach (var prop in propertiesObject.Properties().Where(prop => prop.Value is JValue)
                             .Where(prop => typeMetadata.Properties.All(metadata => !string.Equals(metadata.Name, prop.Name, StringComparison.Ordinal))))
                {
                    EditableProperties.Add(new PropertyItem(prop.Name, prop.Value, propertiesObject));
                }

                return;
            }

            foreach (var prop in propertiesObject.Properties())
            {
                if (prop.Value is JValue jValue)
                {
                    EditableProperties.Add(new PropertyItem(prop.Name, jValue, propertiesObject));
                }
            }
        }

        private void RebuildToolbox(WorkspaceContext? workspace)
        {
            AvailableControls.Clear();

            if (workspace == null)
            {
                return;
            }

            foreach (var type in workspace.FrameworkTypes.Values
                         .Concat(workspace.Types.Values)
                         .GroupBy(type => type.FullName, StringComparer.Ordinal)
                         .Select(group => group.First())
                         .Where(type => type.IsControl && !type.IsTopLevel)
                         .OrderBy(type => type.Name, StringComparer.Ordinal))
            {
                var properties = new JObject();
                if (type.Properties.Any(property => string.Equals(property.Name, "Children", StringComparison.Ordinal)))
                {
                    properties["Children"] = new JArray();
                }

                var template = new JObject
                {
                    ["TypeName"] = type.FullName,
                    ["Properties"] = properties
                };

                AvailableControls.Add(new ToolboxItem(type.FullName, type.Name, template));
            }
        }

        private void OnDesignerSelectionChanged(object? sender, UiNode? node)
        {
            if (_selectionSyncInProgress)
            {
                return;
            }

            try
            {
                _selectionSyncInProgress = true;

                if (node == null)
                {
                    SelectedNode = null;
                    return;
                }

                var path = FindUiNodePath(node);
                if (path != null && _controlNodesByPath.TryGetValue(path, out var controlNode))
                {
                    SelectedNode = controlNode;
                }
            }
            finally
            {
                _selectionSyncInProgress = false;
            }
        }

        private void SyncPreviewSelectionFromOutline(ControlNode? node)
        {
            if (_selectionSyncInProgress)
            {
                return;
            }

            try
            {
                _selectionSyncInProgress = true;

                if (node == null)
                {
                    DesignerSelectionService.Select(null);
                    return;
                }

                if (_uiNodesByPath.TryGetValue(node.NodePath, out var uiNode))
                {
                    DesignerSelectionService.Select(uiNode);
                }
                else
                {
                    DesignerSelectionService.Select(null);
                }
            }
            finally
            {
                _selectionSyncInProgress = false;
            }
        }

        private string? FindUiNodePath(UiNode target)
        {
            foreach (var pair in _uiNodesByPath)
            {
                if (ReferenceEquals(pair.Value, target))
                {
                    return pair.Key;
                }
            }

            return null;
        }

        private static Dictionary<string, UiNode> BuildUiNodeIndex(UiNode root)
        {
            var index = new Dictionary<string, UiNode>();
            VisitUiNode(root, "", index);
            return index;
        }

        private static void VisitUiNode(UiNode node, string currentPath, IDictionary<string, UiNode> index)
        {
            index[currentPath] = node;

            foreach (var property in node.Properties)
            {
                if (property.Value is NodeValue nodeValue)
                {
                    VisitUiNode(nodeValue.Node, AppendPath(currentPath, property.Key), index);
                }
                else if (property.Value is CollectionValue collectionValue)
                {
                    for (var i = 0; i < collectionValue.Items.Count; i++)
                    {
                        if (collectionValue.Items[i] is NodeValue collectionNodeValue)
                        {
                            VisitUiNode(collectionNodeValue.Node, $"{AppendPath(currentPath, property.Key)}[{i}]", index);
                        }
                    }
                }
            }
        }

        private static Dictionary<string, ControlNode> BuildControlNodeIndex(ControlNode? root)
        {
            var index = new Dictionary<string, ControlNode>();
            if (root == null)
            {
                return index;
            }

            VisitControlNode(root, index);
            return index;
        }

        private static void VisitControlNode(ControlNode node, IDictionary<string, ControlNode> index)
        {
            if (!string.IsNullOrWhiteSpace(node.NodePath) && node.JsonNode["TypeName"] != null)
            {
                index[node.NodePath] = node;
            }

            foreach (var child in node.Children)
            {
                VisitControlNode(child, index);
            }
        }

        private static string AppendPath(string parentPath, string segment)
        {
            return string.IsNullOrWhiteSpace(parentPath) ? segment : $"{parentPath}/{segment}";
        }
        
        // --- Команды конструктора ---

        [RelayCommand]
        private void LoadProject()
        {
            try
            {
                ErrorMessage = "";
                var project = _projectDiscoveryService.Load(ProjectPathInput);
                var workspace = _workspaceService.Load(ProjectPathInput);
                LoadedProject = project;
                LoadedWorkspace = workspace;

                ProjectArxuiFiles.Clear();
                foreach (var file in project.ArxuiFiles)
                {
                    ProjectArxuiFiles.Add(file);
                }
                BuildProjectTree(project.ProjectDirectory);
                OpenDocumentTabs.Clear();
                SelectedOpenDocumentTab = null;

                ProjectSummary =
                    $"Project: {Path.GetFileName(project.ProjectPath)} | Assembly: {project.AssemblyName} | TFM: {project.TargetFramework} | .arxui: {project.ArxuiFiles.Count} | .axaml: {project.AxamlFiles.Count} | indexed types: {workspace.Types.Count}";

                if (ProjectArxuiFiles.Count > 0)
                {
                    var startupFile = SelectStartupDocument(ProjectArxuiFiles, workspace) ?? ProjectArxuiFiles[0];
                    SelectedProjectFile = startupFile;
                    SelectedProjectTreeItem = FindTreeItemByPath(startupFile.FullPath, ProjectTreeItems);
                    OpenProjectFile(startupFile);
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

        private void OpenProjectFile(ProjectFileItem file, bool ensureTabSelection = true)
        {
            try
            {
                ErrorMessage = "";
                _currentDocumentPath = file.FullPath;
                EnsureOpenTab(file, ensureTabSelection);

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

                if (LoadedWorkspace != null && CurrentDocument != null)
                {
                    var diagnostics = _semanticValidator.Validate(CurrentDocument, LoadedWorkspace);
                    if (diagnostics.Count > 0)
                    {
                        ErrorMessage = string.Join(Environment.NewLine, diagnostics.Select(diagnostic => diagnostic.Message));
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Open File Error: {ex.Message}";
            }
        }

        public bool CanOpenSelectedProjectFile => SelectedProjectFile != null;

        public void OpenSelectedTreeItem()
        {
            if (SelectedProjectTreeItem == null)
            {
                return;
            }

            OpenTreeItem(SelectedProjectTreeItem);
        }

        [RelayCommand]
        private void OpenTreeItem(ProjectTreeItem? item)
        {
            if (item == null || item.IsDirectory)
            {
                return;
            }

            var file = ResolveArxuiFile(item.FullPath);

            if (file == null)
            {
                return;
            }

            SelectedProjectFile = file;
            SelectedProjectTreeItem = item;
            OpenProjectFile(file);
        }

        [RelayCommand]
        private void RevealTreeItem(ProjectTreeItem? item)
        {
            if (item == null)
            {
                return;
            }

            try
            {
                if (!File.Exists(item.FullPath) && !Directory.Exists(item.FullPath))
                {
                    return;
                }

                var target = item.IsDirectory ? $"\"{item.FullPath}\"" : $"/select,\"{item.FullPath}\"";
                var startInfo = new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = target,
                    UseShellExecute = true
                };
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Explorer Error: {ex.Message}";
            }
        }

        [RelayCommand]
        private void CloseOpenDocumentTab(OpenDocumentTab? tab)
        {
            if (tab == null)
            {
                return;
            }

            var index = OpenDocumentTabs.IndexOf(tab);
            if (index < 0)
            {
                return;
            }

            var wasActive = SelectedOpenDocumentTab == tab;
            OpenDocumentTabs.RemoveAt(index);

            if (!wasActive)
            {
                return;
            }

            if (OpenDocumentTabs.Count == 0)
            {
                _suppressTabSelectionChanged = true;
                try
                {
                    SelectedOpenDocumentTab = null;
                }
                finally
                {
                    _suppressTabSelectionChanged = false;
                }
                return;
            }

            var nextIndex = Math.Min(index, OpenDocumentTabs.Count - 1);
            _suppressTabSelectionChanged = true;
            try
            {
                SelectedOpenDocumentTab = OpenDocumentTabs[nextIndex];
            }
            finally
            {
                _suppressTabSelectionChanged = false;
            }

            var file = ResolveArxuiFile(SelectedOpenDocumentTab.FullPath);
            if (file != null)
            {
                SelectedProjectFile = file;
                SelectedProjectTreeItem = FindTreeItemByPath(file.FullPath, ProjectTreeItems);
                OpenProjectFile(file, ensureTabSelection: false);
            }
        }

        private static ProjectFileItem? SelectStartupDocument(IEnumerable<ProjectFileItem> files, WorkspaceContext? workspace)
        {
            if (workspace != null)
            {
                foreach (var document in workspace.Documents)
                {
                    if (!document.IsPreviewable)
                    {
                        continue;
                    }

                    var matchedFile = files.FirstOrDefault(file =>
                        string.Equals(file.FullPath, document.FullPath, StringComparison.OrdinalIgnoreCase));

                    if (matchedFile != null)
                    {
                        return matchedFile;
                    }
                }
            }

            foreach (var file in files)
            {
                var kind = TryReadDocumentKind(file.FullPath);
                if (kind == UiDocumentKind.Window || kind == UiDocumentKind.Control)
                {
                    return file;
                }
            }

            return files.FirstOrDefault();
        }

        private static UiDocumentKind? TryReadDocumentKind(string path)
        {
            try
            {
                using var stream = File.OpenRead(path);
                using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                var json = JObject.Parse(reader.ReadToEnd());
                return json["Kind"]?.ToObject<UiDocumentKind>();
            }
            catch
            {
                return null;
            }
        }

        [RelayCommand]
        private void SetWorkspaceMode(string? mode)
        {
            if (string.IsNullOrWhiteSpace(mode))
            {
                return;
            }

            WorkspaceMode = mode;
        }

        [RelayCommand]
        private void ZoomIn()
        {
            PreviewZoom = Math.Round(PreviewZoom + 0.1, 2);
        }

        [RelayCommand]
        private void ZoomOut()
        {
            PreviewZoom = Math.Round(PreviewZoom - 0.1, 2);
        }

        [RelayCommand]
        private void ResetZoom()
        {
            PreviewZoom = 1.0;
        }

        [RelayCommand(CanExecute = nameof(CanRunProject))]
        private async Task RunProject()
        {
            if (IsProjectRunning || string.IsNullOrWhiteSpace(ProjectPathInput))
            {
                return;
            }

            try
            {
                if (LoadedProject == null || !string.Equals(LoadedProject.ProjectPath, ProjectPathInput, StringComparison.OrdinalIgnoreCase))
                {
                    var project = _projectDiscoveryService.Load(ProjectPathInput);
                    LoadedProject = project;
                }

                if (LoadedProject == null)
                {
                    RunStatus = "Project is not loaded.";
                    return;
                }

                var projectPath = LoadedProject.ProjectPath;
                var workingDirectory = Path.GetDirectoryName(projectPath) ?? Environment.CurrentDirectory;

                RunOutput = "";
                RunStatus = $"Starting {Path.GetFileName(projectPath)}...";

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = $"run --project \"{projectPath}\"",
                        WorkingDirectory = workingDirectory,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    },
                    EnableRaisingEvents = true
                };

                process.OutputDataReceived += (_, args) => AppendRunOutput(args.Data);
                process.ErrorDataReceived += (_, args) => AppendRunOutput(args.Data);

                if (!process.Start())
                {
                    RunStatus = "Failed to start process.";
                    return;
                }

                _runningProcess = process;
                IsProjectRunning = true;
                RunStatus = $"Running PID {process.Id}";
                AppendRunOutput($"> dotnet run --project \"{projectPath}\"");

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await process.WaitForExitAsync();

                var exitCode = process.ExitCode;
                _runningProcess = null;
                IsProjectRunning = false;
                RunStatus = $"Process finished with exit code {exitCode}.";
                AppendRunOutput($"Process finished with exit code {exitCode}.");
            }
            catch (Exception ex)
            {
                _runningProcess = null;
                IsProjectRunning = false;
                RunStatus = "Run failed.";
                AppendRunOutput($"Run error: {ex.Message}");
                ErrorMessage = $"Run Error: {ex.Message}";
            }
        }

        [RelayCommand(CanExecute = nameof(CanStopProject))]
        private void StopProject()
        {
            if (_runningProcess == null || _runningProcess.HasExited)
            {
                return;
            }

            try
            {
                AppendRunOutput($"Stopping PID {_runningProcess.Id}...");
                _runningProcess.Kill(entireProcessTree: true);
                RunStatus = "Stopping process...";
            }
            catch (Exception ex)
            {
                AppendRunOutput($"Stop error: {ex.Message}");
                ErrorMessage = $"Stop Error: {ex.Message}";
            }
        }

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

        public void PlaceControlFromDesigner(DesignerPlacementRequest request)
        {
            if (request == null)
            {
                return;
            }

            if (!TryFindControlNodeByUiNode(request.ContainerNode, out var containerNode) || containerNode == null)
            {
                ErrorMessage = "Drop target container was not found in the current JSON tree.";
                return;
            }

            try
            {
                var newControlJson = JObject.Parse(request.SerializedNode);
                ApplyPlacementIntent(containerNode.JsonNode, request.Intent, newControlJson);
                DesignerService.Instance.NotifyJsonChanged(DesignerChangeKind.Structure);
                ErrorMessage = "";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Drop Placement Error: {ex.Message}";
            }
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

        private bool TryFindControlNodeByUiNode(UiNode containerNode, out ControlNode? controlNode)
        {
            controlNode = null;
            var path = FindUiNodePath(containerNode);
            if (path == null)
            {
                return false;
            }

            return _controlNodesByPath.TryGetValue(path, out controlNode);
        }

        private static void ApplyPlacementIntent(JObject containerJsonNode, DesignPlacementIntent intent, JObject childNodeJson)
        {
            var properties = EnsurePropertiesObject(containerJsonNode);

            switch (intent.Kind)
            {
                case DesignPlacementKind.SetContent:
                    properties[intent.SlotName] = childNodeJson;
                    break;
                case DesignPlacementKind.InsertChild:
                case DesignPlacementKind.SetAttachedLayout:
                {
                    var collection = EnsureCollectionProperty(properties, intent.SlotName);
                    if (intent.InsertIndex.HasValue && intent.InsertIndex.Value >= 0 && intent.InsertIndex.Value <= collection.Count)
                    {
                        collection.Insert(intent.InsertIndex.Value, childNodeJson);
                    }
                    else
                    {
                        collection.Add(childNodeJson);
                    }

                    if (intent.AttachedProperties != null && intent.AttachedProperties.Count > 0)
                    {
                        var childProperties = EnsurePropertiesObject(childNodeJson);
                        foreach (var property in intent.AttachedProperties)
                        {
                            childProperties[property.Key] = ToJsonToken(property.Value);
                        }
                    }

                    break;
                }
            }
        }

        private static JObject EnsurePropertiesObject(JObject node)
        {
            if (node.TryGetValue("Properties", out var existing) && existing is JObject propertiesObject)
            {
                return propertiesObject;
            }

            var created = new JObject();
            node["Properties"] = created;
            return created;
        }

        private static JArray EnsureCollectionProperty(JObject properties, string propertyName)
        {
            if (properties.TryGetValue(propertyName, out var existing) && existing is JArray array)
            {
                return array;
            }

            var created = new JArray();
            properties[propertyName] = created;
            return created;
        }

        private static JToken ToJsonToken(UiValue value)
        {
            return value switch
            {
                ScalarValue scalar => scalar.Value == null ? JValue.CreateNull() : JToken.FromObject(scalar.Value),
                ResourceValue resource => new JObject { ["$resource"] = resource.Key },
                UriReferenceValue asset when string.IsNullOrWhiteSpace(asset.Assembly) => new JObject { ["$asset"] = asset.Path },
                UriReferenceValue asset => new JObject
                {
                    ["$asset"] = new JObject
                    {
                        ["Path"] = asset.Path,
                        ["Assembly"] = asset.Assembly
                    }
                },
                _ => JValue.CreateNull()
            };
        }

        private void AppendRunOutput(string? line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            Dispatcher.UIThread.Post(() =>
            {
                RunOutput = string.IsNullOrWhiteSpace(RunOutput)
                    ? line
                    : $"{RunOutput}{Environment.NewLine}{line}";
            });
        }

        private void EnsureOpenTab(ProjectFileItem file, bool ensureTabSelection)
        {
            var tab = OpenDocumentTabs.FirstOrDefault(openTab =>
                string.Equals(openTab.FullPath, file.FullPath, StringComparison.OrdinalIgnoreCase));

            if (tab == null)
            {
                tab = new OpenDocumentTab(file.FullPath);
                OpenDocumentTabs.Add(tab);
            }

            if (!ensureTabSelection)
            {
                return;
            }

            _suppressTabSelectionChanged = true;
            try
            {
                SelectedOpenDocumentTab = tab;
            }
            finally
            {
                _suppressTabSelectionChanged = false;
            }
        }

        private ProjectFileItem? ResolveArxuiFile(string fullPath)
        {
            if (!fullPath.EndsWith(".arxui", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return ProjectArxuiFiles.FirstOrDefault(projectFile =>
                string.Equals(projectFile.FullPath, fullPath, StringComparison.OrdinalIgnoreCase));
        }

        private void BuildProjectTree(string projectDirectory)
        {
            ProjectTreeItems.Clear();

            if (string.IsNullOrWhiteSpace(projectDirectory) || !Directory.Exists(projectDirectory))
            {
                return;
            }

            var root = BuildTreeItem(projectDirectory);
            ProjectTreeItems.Add(root);
        }

        private static ProjectTreeItem BuildTreeItem(string path)
        {
            var isDirectory = Directory.Exists(path);
            var name = isDirectory ? new DirectoryInfo(path).Name : Path.GetFileName(path);
            var kind = isDirectory ? "folder" : Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
            var item = new ProjectTreeItem(name, path, isDirectory, kind);

            if (!isDirectory)
            {
                return item;
            }

            var directories = Directory.GetDirectories(path)
                .Where(directory => !IsIgnoredDirectory(directory))
                .OrderBy(directory => directory, StringComparer.OrdinalIgnoreCase);

            foreach (var directory in directories)
            {
                item.Children.Add(BuildTreeItem(directory));
            }

            var files = Directory.GetFiles(path)
                .OrderBy(file => file, StringComparer.OrdinalIgnoreCase);

            foreach (var file in files)
            {
                item.Children.Add(BuildTreeItem(file));
            }

            return item;
        }

        private static bool IsIgnoredDirectory(string directoryPath)
        {
            var name = Path.GetFileName(directoryPath);
            return string.Equals(name, "bin", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(name, "obj", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(name, ".git", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(name, ".idea", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(name, ".vs", StringComparison.OrdinalIgnoreCase);
        }

        private static ProjectTreeItem? FindTreeItemByPath(string fullPath, IEnumerable<ProjectTreeItem> nodes)
        {
            foreach (var node in nodes)
            {
                if (string.Equals(node.FullPath, fullPath, StringComparison.OrdinalIgnoreCase))
                {
                    return node;
                }

                var child = FindTreeItemByPath(fullPath, node.Children);
                if (child != null)
                {
                    return child;
                }
            }

            return null;
        }
    }
}
