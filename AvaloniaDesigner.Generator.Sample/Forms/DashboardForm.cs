using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvaloniaDesigner.Generator.Sample.Forms 
{
    public partial class DashboardForm : UserControl
    {
        public DashboardForm()
        {
           
            InitializeComponent();
          
            ActionButton.Click += ActionButton_Click;
        }

        private void ActionButton_Click(object? sender, RoutedEventArgs e)
        {
            HeaderLabel1.Text = "Test";
            
        }
    }
}