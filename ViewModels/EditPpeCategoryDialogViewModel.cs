// ViewModels/EditPpeCategoryDialogViewModel.cs
using PersonalPPEManager.Models;
using System;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows; // For MessageBox
using System.Diagnostics;

namespace PersonalPPEManager.ViewModels
{
    public class EditPpeCategoryDialogViewModel : BaseViewModel
    {
        private PpeCategory _currentCategory;
        public PpeCategory CurrentCategory
        {
            get => _currentCategory;
            set => SetProperty(ref _currentCategory, value);
        }

        private string _windowTitle;
        public string WindowTitle
        {
            get => _windowTitle;
            set => SetProperty(ref _windowTitle, value);
        }

        private bool _isNew;

        public event Action<bool?> RequestCloseDialog; // True for save, False for cancel

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public EditPpeCategoryDialogViewModel(PpeCategory category, bool isNew)
        {
            _isNew = isNew;
            WindowTitle = isNew ? "添加新类别" : "编辑类别";

            // 创建副本进行编辑，避免直接修改传入的对象，除非保存
            CurrentCategory = new PpeCategory
            {
                CategoryID = category.CategoryID, // 如果是新的，ID通常是0或未设置
                CategoryName = category.CategoryName,
                Remarks = category.Remarks
            };
            Debug.WriteLine($"DEBUG: EditPpeCategoryDialogViewModel_ctor: IsNew={_isNew}, CategoryID={CurrentCategory.CategoryID}, Name='{CurrentCategory.CategoryName}'");

            SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
            CancelCommand = new RelayCommand(ExecuteCancel);
        }

        private bool CanExecuteSave(object parameter)
        {
            return CurrentCategory != null && !string.IsNullOrWhiteSpace(CurrentCategory.CategoryName);
        }

        private void ExecuteSave(object parameter)
        {
            Debug.WriteLine($"DEBUG: EditPpeCategoryDialogViewModel.ExecuteSave: Attempting to save. CategoryName='{CurrentCategory?.CategoryName}'");
            if (!CanExecuteSave(null))
            {
                MessageBox.Show("类别名称不能为空。", "验证错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            // 在这里不直接执行数据库操作，而是通过 RequestCloseDialog 通知调用者
            RequestCloseDialog?.Invoke(true); // true表示用户点击了保存
        }

        private void ExecuteCancel(object parameter)
        {
            Debug.WriteLine("DEBUG: EditPpeCategoryDialogViewModel.ExecuteCancel: Cancelling.");
            RequestCloseDialog?.Invoke(false); // false表示用户点击了取消
        }
    }
}