// Views/EditPpeCategoryDialog.xaml.cs
using PersonalPPEManager.ViewModels;
using System;
using System.Diagnostics;
using System.Windows;

namespace PersonalPPEManager.Views
{
    public partial class EditPpeCategoryDialog : Window
    {
        // ViewModel 属性（可选，但推荐保留以便在需要时从代码隐藏访问DataContext）
        public EditPpeCategoryDialogViewModel ViewModel => DataContext as EditPpeCategoryDialogViewModel;

        // --- 确保这是文件中唯一的 EditPpeCategoryDialog 构造函数 ---
        public EditPpeCategoryDialog()
        {
            InitializeComponent();
            Debug.WriteLine("DEBUG: EditPpeCategoryDialog parameterless constructor called.");
            // DataContextChanged 事件已在 XAML 文件中的 Window 标签上关联:
            // DataContextChanged="EditPpeCategoryDialog_DataContextChanged"
        }
        // 步骤说明：
        // 1. 检查 ViewModel 的 RequestCloseDialog 事件声明，通常是自定义事件委托类型（如 Action<bool?> 或 EventHandler<bool?> 等）。
        // 2. 明确指定委托类型进行事件订阅和取消订阅，避免编译器因重载或签名模糊导致的二义性。
        // 3. 假设事件声明为 public event Action<bool?> RequestCloseDialog;，则需强制转换为 Action<bool?>。
        // 4. 如果事件类型不同，请用对应的委托类型强制转换。

        // 修改如下：
        private void EditPpeCategoryDialog_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Debug.WriteLine("DEBUG: EditPpeCategoryDialog.DataContextChanged Fired.");
            if (e.OldValue is EditPpeCategoryDialogViewModel oldVm)
            {
                // 明确指定委托类型，消除二义性
                oldVm.RequestCloseDialog -= new Action<bool?>(HandleRequestCloseDialog);
                Debug.WriteLine("DEBUG: EditPpeCategoryDialog.DataContextChanged: Unsubscribed from old ViewModel's RequestCloseDialog.");
            }
            if (e.NewValue is EditPpeCategoryDialogViewModel newVm)
            {
                // 明确指定委托类型，消除二义性
                newVm.RequestCloseDialog += new Action<bool?>(HandleRequestCloseDialog);
                Debug.WriteLine("DEBUG: EditPpeCategoryDialog.DataContextChanged: Subscribed to new ViewModel's RequestCloseDialog.");
            }
        }

        // 处理ViewModel关闭请求的方法
        private void HandleRequestCloseDialog(bool? dialogResult)
        {
            Debug.WriteLine($"DEBUG: EditPpeCategoryDialog.HandleRequestCloseDialog: Received request to close with DialogResult: {dialogResult}");
            try
            {
                if (System.Windows.Interop.ComponentDispatcher.IsThreadModal && IsLoaded)
                {
                    this.DialogResult = dialogResult;
                }
                else if (IsLoaded)
                {
                    this.Close();
                }
            }
            catch (InvalidOperationException ex)
            {
                Debug.WriteLine($"DEBUG: EditPpeCategoryDialog.HandleRequestCloseDialog: InvalidOperationException: {ex.Message}");
                if (IsLoaded) this.Close();
            }
        }
    }
}