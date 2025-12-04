using System;
using Avalonia.Interactivity;
using AvaloniaDesigner.Generator.Sample.ViewModels;

namespace AvaloniaDesigner.Generator.Sample.Views;

public partial class  UserProfile
{
    public UserProfile()
    {
        InitializeComponent();

        DataContext = new UserProfileViewModel();
    }

    private void OnClickMyMethod(object? sender, RoutedEventArgs e)
    {
        Console.WriteLine("Hello World!");
    }
}