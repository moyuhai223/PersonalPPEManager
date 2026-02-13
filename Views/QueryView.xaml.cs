// Views/QueryView.xaml.cs
using PersonalPPEManager.ViewModels; // 确保引用了ViewModels命名空间
using System.Windows.Controls;
using System.Windows.Input; // 为了 MouseButtonEventArgs
// using PersonalPPEManager.Models; // 如果需要直接访问PPEAssignment类型，但通常ViewModel处理

namespace PersonalPPEManager.Views
{
    public partial class QueryView : UserControl
    {
        public QueryView()
        {
            InitializeComponent();
        }

        private void PpeDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // 确保 DataContext 是 QueryViewModel
            if (DataContext is QueryViewModel viewModel)
            {
                // DataGrid 的 SelectedItem 应该已经通过绑定更新了 ViewModel 中的 SelectedPpeAssignmentForEdit
                // 直接尝试执行命令
                if (viewModel.EditPpeAssignmentCommand.CanExecute(null))
                {
                    viewModel.EditPpeAssignmentCommand.Execute(null);
                }
            }
        }
    }
}