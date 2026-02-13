// Views/EmployeeManagementView.xaml.cs
using System.Windows.Controls;

namespace PersonalPPEManager.Views
{
    /// <summary>
    /// Interaction logic for EmployeeManagementView.xaml
    /// </summary>
    public partial class EmployeeManagementView : UserControl
    {
        public EmployeeManagementView()
        {
            InitializeComponent();
            // DataContext 通常在 MainWindow 中设置，或者在 XAML 中声明式设置
            // DataContext = new ViewModels.EmployeeManagementViewModel(); 
        }
    }
}