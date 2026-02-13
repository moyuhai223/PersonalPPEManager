// ViewModels/EditPpeMasterItemDialogViewModel.cs
using PersonalPPEManager.Models;
using System;
using System.Collections.ObjectModel; // 为了 ObservableCollection
using System.Collections.Generic;   // 为了 IEnumerable
using System.ComponentModel;
using System.Windows.Input;
using System.Windows; // For MessageBox
using System.Diagnostics;
using System.Linq; // For FirstOrDefault

namespace PersonalPPEManager.ViewModels
{
    public class EditPpeMasterItemDialogViewModel : BaseViewModel
    {
        private PpeMasterItem _currentMasterItem;
        public PpeMasterItem CurrentMasterItem
        {
            get => _currentMasterItem;
            set => SetProperty(ref _currentMasterItem, value);
        }

        private string _windowTitle;
        public string WindowTitle
        {
            get => _windowTitle;
            set => SetProperty(ref _windowTitle, value);
        }

        private bool _isNew;

        // 用于类别下拉框的数据源
        public ObservableCollection<PpeCategory> AvailableCategories { get; }

        public event Action<bool?> RequestCloseDialog; // True for save, False for cancel

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public EditPpeMasterItemDialogViewModel(PpeMasterItem masterItem, IEnumerable<PpeCategory> availableCategories, bool isNew)
        {
            _isNew = isNew;
            WindowTitle = isNew ? "添加新主数据条目" : "编辑主数据条目";

            AvailableCategories = new ObservableCollection<PpeCategory>(availableCategories ?? new List<PpeCategory>());

            CurrentMasterItem = CreateCopy(masterItem); // 使用副本进行编辑

            // 如果是新建，并且有可用类别，默认选中第一个类别 (如果适用)
            if (_isNew && AvailableCategories.Any())
            {
                // CurrentMasterItem.CategoryID_FK = AvailableCategories.First().CategoryID; // 这行会导致绑定问题，因为 ComboBox 的 SelectedValue 是 CategoryID_FK
                // 而 CurrentMasterItem 还没有被賦值，所以它的 CategoryID_FK 是 0
                // 应该在 XAML 的 ComboBox 中设置一个默认选中或提示，或者确保 CategoryID_FK 有初始值
                // 对于新建，确保 CategoryID_FK 有一个有效值或允许用户选择
                if (CurrentMasterItem.CategoryID_FK == 0 && AvailableCategories.Any())
                {
                    // 这行可以不要，让用户必须选择一个类别
                    // CurrentMasterItem.CategoryID_FK = AvailableCategories.First().CategoryID;
                }
            }

            Debug.WriteLine($"DEBUG: EditPpeMasterItemDialogViewModel_ctor: IsNew={_isNew}, ItemMasterID={CurrentMasterItem.ItemMasterID}, ItemCode='{CurrentMasterItem.ItemMasterCode}'");

            SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
            CancelCommand = new RelayCommand(ExecuteCancel);
        }

        private PpeMasterItem CreateCopy(PpeMasterItem original)
        {
            if (original == null) return new PpeMasterItem(); // Should not happen if called correctly
            return new PpeMasterItem
            {
                ItemMasterID = original.ItemMasterID,
                ItemMasterCode = original.ItemMasterCode,
                ItemName = original.ItemName,
                CategoryID_FK = original.CategoryID_FK,
                Size = original.Size,
                UnitOfMeasure = original.UnitOfMeasure,
                ExpectedLifespanDays = original.ExpectedLifespanDays,
                DefaultRemarks = original.DefaultRemarks,
                CurrentStock = original.CurrentStock,
                LowStockThreshold = original.LowStockThreshold
            };
        }

        private bool CanExecuteSave(object parameter)
        {
            if (CurrentMasterItem == null) return false;
            if (string.IsNullOrWhiteSpace(CurrentMasterItem.ItemMasterCode)) return false;
            if (string.IsNullOrWhiteSpace(CurrentMasterItem.ItemName)) return false;
            if (CurrentMasterItem.CategoryID_FK <= 0) return false; // 必须选择一个有效类别
            // 可以添加更多校验，例如数字字段是否为有效数字等
            return true;
        }

        private void ExecuteSave(object parameter)
        {
            Debug.WriteLine($"DEBUG: EditPpeMasterItemDialogViewModel.ExecuteSave: Attempting to save. ItemCode='{CurrentMasterItem?.ItemMasterCode}'");
            if (!CanExecuteSave(null))
            {
                string errors = "请确保以下字段已正确填写：\n";
                if (string.IsNullOrWhiteSpace(CurrentMasterItem.ItemMasterCode)) errors += "- 用品主代码不能为空\n";
                if (string.IsNullOrWhiteSpace(CurrentMasterItem.ItemName)) errors += "- 用品名称不能为空\n";
                if (CurrentMasterItem.CategoryID_FK <= 0) errors += "- 必须选择所属类别\n";
                // TODO: 针对数字字段（如库存、寿命、阈值）的 IsNullOrWhiteSpace 可能不适用，需要数字校验
                // 例如： if (!int.TryParse(CurrentMasterItem.ExpectedLifespanDays?.ToString(), out _)) errors += "- 预计寿命必须是有效数字\n";
                // 为简化，暂时只做非空检查

                MessageBox.Show(errors, "验证错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            RequestCloseDialog?.Invoke(true);
        }

        private void ExecuteCancel(object parameter)
        {
            Debug.WriteLine("DEBUG: EditPpeMasterItemDialogViewModel.ExecuteCancel: Cancelling.");
            RequestCloseDialog?.Invoke(false);
        }
    }
}