using Avalonia.Controls;
using AvaloniaDesigner.Generator.Sample.ViewModels;

namespace AvaloniaDesigner.Generator.Sample.Views;

public partial class UserProfile : UserControl
{
    public UserProfile()
    {
        InitializeComponent();
        
        DataContext = new UserProfileViewModel() ;
        
        global::Avalonia.Controls.Design.SetWidth(this, 800);
    }
}