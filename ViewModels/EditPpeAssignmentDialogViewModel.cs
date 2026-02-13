// ViewModels/EditPpeAssignmentDialogViewModel.cs
using PersonalPPEManager.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows; // For MessageBox (optional validation feedback)
using System.Linq; // For FirstOrDefault

namespace PersonalPPEManager.ViewModels
{
    public class EditPpeAssignmentDialogViewModel : BaseViewModel
    {
        private PPEAssignment _currentPpeAssignment;
        public PPEAssignment CurrentPpeAssignment
        {
            get => _currentPpeAssignment;
            set => SetProperty(ref _currentPpeAssignment, value);
        }

        public ObservableCollection<string> ShoeConditions { get; }

        // 用于通知View关闭的事件
        public event Action<bool?> RequestCloseDialog;

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public EditPpeAssignmentDialogViewModel(PPEAssignment ppeAssignmentToEdit)
        {
            System.Diagnostics.Debug.WriteLine($"DEBUG: EditPpeAssignmentDialogViewModel_ctor: Editing AssignmentID: {ppeAssignmentToEdit?.AssignmentID}");
            // 创建一个副本进行编辑，以支持取消操作
            CurrentPpeAssignment = CreateCopy(ppeAssignmentToEdit);

            ShoeConditions = new ObservableCollection<string> { "新", "旧" };
            // 如果 CurrentPpeAssignment.Condition 为 null 或不在 ShoeConditions 中，
            // SelectedItem 不会自动选中。确保 Condition 有一个有效值或 ComboBox 能处理 null。
            // 如果 Condition 是鞋类且为空，可以设置一个默认值：
            if (IsShoeType(CurrentPpeAssignment.PPE_Type) && string.IsNullOrEmpty(CurrentPpeAssignment.Condition))
            {
                CurrentPpeAssignment.Condition = ShoeConditions.FirstOrDefault();
            }


            SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
            CancelCommand = new RelayCommand(ExecuteCancel);
        }

        private PPEAssignment CreateCopy(PPEAssignment original)
        {
            if (original == null) return new PPEAssignment { IsActive = true, IssueDate = DateTime.Today }; // Default for a new one, though this VM is for edit

            return new PPEAssignment
            {
                AssignmentID = original.AssignmentID,
                EmployeeID_FK = original.EmployeeID_FK,
                PPE_Type = original.PPE_Type,
                ItemSpecificCode = original.ItemSpecificCode,
                IssueDate = original.IssueDate,
                Size = original.Size,
                Condition = original.Condition,
                IsActive = original.IsActive,
                Remarks = original.Remarks
                // IsActiveText and FormattedIssueDate are display-only, no need to copy if not editable.
            };
        }

        // 辅助方法，判断是否为鞋类，以便控制 Condition 字段的逻辑 (如果转换器不够用)
        // 这个逻辑也可以放在 ShoeTypeToVisibilityConverter 中，ViewModel 中通常不直接处理Visibility
        // 但这里可以用它来决定是否给 Condition 赋默认值
        private bool IsShoeType(string ppeType)
        {
            // 确保这些字符串与 ShoeTypeToVisibilityConverter 中的一致
            const string SafetyShoeType = "白色劳保鞋";
            const string CanvasShoeType = "白色帆布鞋";
            return ppeType.Equals(SafetyShoeType, StringComparison.OrdinalIgnoreCase) ||
                   ppeType.Equals(CanvasShoeType, StringComparison.OrdinalIgnoreCase);
        }


        private bool CanExecuteSave(object parameter)
        {
            // 添加必要的校验逻辑
            if (CurrentPpeAssignment == null) return false;

            // 例如：物品编号对于某些类型是必填的
            if (CurrentPpeAssignment.PPE_Type == "洁净服" || CurrentPpeAssignment.PPE_Type == "帽子")
            {
                if (string.IsNullOrWhiteSpace(CurrentPpeAssignment.ItemSpecificCode)) return false;
            }
            if (IsShoeType(CurrentPpeAssignment.PPE_Type))
            {
                if (string.IsNullOrWhiteSpace(CurrentPpeAssignment.Size)) return false;
                if (string.IsNullOrWhiteSpace(CurrentPpeAssignment.Condition)) return false;
            }
            if (CurrentPpeAssignment.PPE_Type == "洁净服" && string.IsNullOrWhiteSpace(CurrentPpeAssignment.Size))
            {
                return false;
            }

            if (CurrentPpeAssignment.IssueDate == null) return false;

            return true; // 如果所有校验通过
        }

        private void ExecuteSave(object parameter)
        {
            System.Diagnostics.Debug.WriteLine($"DEBUG: EditPpeAssignmentDialogViewModel.ExecuteSave: Saving AssignmentID: {CurrentPpeAssignment?.AssignmentID}");
            if (!CanExecuteSave(null)) // 再次校验
            {
                MessageBox.Show("请填写所有必填项或修正错误。", "验证错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 触发事件，通知View关闭并传递true表示保存成功
            RequestCloseDialog?.Invoke(true);
        }

        private void ExecuteCancel(object parameter)
        {
            System.Diagnostics.Debug.WriteLine($"DEBUG: EditPpeAssignmentDialogViewModel.ExecuteCancel: Cancelling edit for AssignmentID: {CurrentPpeAssignment?.AssignmentID}");
            // 触发事件，通知View关闭并传递false表示取消
            RequestCloseDialog?.Invoke(false);
        }
    }
}