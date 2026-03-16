using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using ArxisStudio.Editor.Models;
using ArxisStudio.Editor.ViewModels;
using Newtonsoft.Json;

namespace ArxisStudio.Editor.Views
{
    public partial class MainWindow : Window
    {
        private Point? _toolboxDragStart;
        private ToolboxItem? _toolboxDragItem;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void OpenProjectButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is not MainWindowViewModel viewModel)
            {
                return;
            }

            var filePickerOptions = new FilePickerOpenOptions
            {
                AllowMultiple = false,
                Title = "Open Project",
                FileTypeFilter =
                [
                    new FilePickerFileType("Dotnet Solution or Project")
                    {
                        Patterns = ["*.sln", "*.csproj"]
                    }
                ]
            };

            var selectedFiles = await StorageProvider.OpenFilePickerAsync(filePickerOptions);
            var selectedFile = selectedFiles.Count > 0 ? selectedFiles[0] : null;
            if (selectedFile == null)
            {
                return;
            }

            viewModel.ProjectPathInput = selectedFile.Path.LocalPath;
            if (viewModel.LoadProjectCommand.CanExecute(null))
            {
                viewModel.LoadProjectCommand.Execute(null);
            }
        }

        private void ToolboxListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is not ListBox listBox || listBox.SelectedItem is not ToolboxItem)
            {
                return;
            }
        }

        private void ToolboxListBox_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is not ListBox listBox || listBox.SelectedItem is not ToolboxItem toolboxItem)
            {
                _toolboxDragStart = null;
                _toolboxDragItem = null;
                return;
            }

            _toolboxDragStart = e.GetPosition(listBox);
            _toolboxDragItem = toolboxItem;
        }

        private async void ToolboxListBox_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (sender is not ListBox listBox || _toolboxDragStart == null || _toolboxDragItem == null)
            {
                return;
            }

            if (!e.GetCurrentPoint(listBox).Properties.IsLeftButtonPressed)
            {
                return;
            }

            var currentPosition = e.GetPosition(listBox);
            var delta = currentPosition - _toolboxDragStart.Value;
            if (Math.Abs(delta.X) < 4 && Math.Abs(delta.Y) < 4)
            {
                return;
            }

            var payload = _toolboxDragItem.Template.ToString(Formatting.None);
#pragma warning disable CS0618
            var data = new DataObject();
            data.Set("application/x-arxui-template", payload);

            _toolboxDragStart = null;
            _toolboxDragItem = null;

            await DragDrop.DoDragDrop(e, data, DragDropEffects.Copy);
#pragma warning restore CS0618
        }

        private void ToolboxListBox_DoubleTapped(object? sender, TappedEventArgs e)
        {
            if (sender is not ListBox listBox || listBox.SelectedItem is not ToolboxItem selectedItem)
            {
                return;
            }

            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.AddNewControlCommand.Execute(selectedItem.Template);
            }
        }

        private void ProjectTreeView_DoubleTapped(object? sender, TappedEventArgs e)
        {
            if (sender is not TreeView treeView || treeView.SelectedItem is not ProjectTreeItem)
            {
                return;
            }

            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.OpenSelectedTreeItem();
            }
        }

        private void ProjectTreeOpenMenuItem_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem || menuItem.DataContext is not ProjectTreeItem item)
            {
                return;
            }

            if (DataContext is MainWindowViewModel viewModel &&
                viewModel.OpenTreeItemCommand.CanExecute(item))
            {
                viewModel.OpenTreeItemCommand.Execute(item);
            }
        }

        private void ProjectTreeRevealMenuItem_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem || menuItem.DataContext is not ProjectTreeItem item)
            {
                return;
            }

            if (DataContext is MainWindowViewModel viewModel &&
                viewModel.RevealTreeItemCommand.CanExecute(item))
            {
                viewModel.RevealTreeItemCommand.Execute(item);
            }
        }

        private void CloseTabButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (sender is not Button button || button.DataContext is not OpenDocumentTab tab)
            {
                return;
            }

            if (DataContext is MainWindowViewModel viewModel &&
                viewModel.CloseOpenDocumentTabCommand.CanExecute(tab))
            {
                viewModel.CloseOpenDocumentTabCommand.Execute(tab);
            }
        }

    }
}
