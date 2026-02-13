// Views/EditPpeCategoryDialog.xaml.cs
using PersonalPPEManager.ViewModels;
using System.Windows;
using System.Diagnostics;
using System;


namespace PersonalPPEManager.Views
{
    public partial class EditPpeAssignmentDialog : Window
    {
        public EditPpeCategoryDialogViewModel ViewModel => DataContext as EditPpeCategoryDialogViewModel;

        public EditPpeAssignmentDialog() // 无参构造函数供XAML设计器使用
        {
            InitializeComponent();
        }

        // DataContextChanged 事件处理器，用于订阅ViewModel的事件
        private void EditPpeAssignmentDialog_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Debug.WriteLine("DEBUG: EditPpeCategoryDialog.DataContextChanged Fired.");
            if (e.OldValue is EditPpeCategoryDialogViewModel oldVm)
            {
                oldVm.RequestCloseDialog -= HandleRequestCloseDialog;
                Debug.WriteLine("DEBUG: EditPpeCategoryDialog.DataContextChanged: Unsubscribed from old ViewModel's RequestCloseDialog.");
            }
            if (e.NewValue is EditPpeCategoryDialogViewModel newVm)
            {
                newVm.RequestCloseDialog += HandleRequestCloseDialog;
                Debug.WriteLine("DEBUG: EditPpeCategoryDialog.DataContextChanged: Subscribed to new ViewModel's RequestCloseDialog.");
            }
        }

        private void HandleRequestCloseDialog(bool? dialogResult)
        {
            Debug.WriteLine($"DEBUG: EditPpeCategoryDialog.HandleRequestCloseDialog: Received request to close with DialogResult: {dialogResult}");
            try
            {
                // 只有当窗口是以 ShowDialog() 模态方式显示时，才能设置 DialogResult
                if (System.Windows.Interop.ComponentDispatcher.IsThreadModal)
                {
                    this.DialogResult = dialogResult;
                }
                else
                {
                    // 如果是非模态显示，直接关闭
                    this.Close();
                }
            }
            catch (InvalidOperationException ex)
            {
                Debug.WriteLine($"DEBUG: EditPpeCategoryDialog.HandleRequestCloseDialog: InvalidOperationException: {ex.Message}");
                // 如果窗口已经关闭或不是模态的，设置 DialogResult 会失败
                // 尝试直接关闭作为后备
                this.Close();
            }
        }
    }
}