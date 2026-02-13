// ViewModels/PpeMasterManagementViewModel.cs
using PersonalPPEManager.Models;
using PersonalPPEManager.DataAccess;
using PersonalPPEManager.Services;
using PersonalPPEManager.Views; // For EditPpeCategoryDialog and EditPpeMasterItemDialog
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Linq;
using System.Diagnostics;
using System.Windows;

namespace PersonalPPEManager.ViewModels
{
    /// <summary>
    /// 辅助类，用于在DataGrid中显示PpeMasterItem及其CategoryName
    /// </summary>
    public class PpeMasterItemDisplay : PpeMasterItem
    {
        public string CategoryName { get; set; }
    }

    public class PpeMasterManagementViewModel : BaseViewModel
    {
        #region Fields
        private ObservableCollection<PpeCategory> _allCategories;
        private PpeCategory _selectedCategory;

        private ObservableCollection<PpeMasterItemDisplay> _displayMasterItems;
        private PpeMasterItemDisplay _selectedMasterItemDisplay;
        private PpeMasterItem _selectedMasterItemData; // 实际用于编辑或删除的原始PpeMasterItem对象

        private ObservableCollection<PpeCategory> _allCategoriesForFilter;
        private int? _selectedFilterCategoryID; // 0 或 null 代表 "所有类别"
        private List<PpeMasterItem> _allMasterItemsInternal; // 内部持有的、从数据库加载的完整主数据列表
        #endregion

        #region Properties
        public ObservableCollection<PpeCategory> AllCategories
        {
            get => _allCategories;
            set => SetProperty(ref _allCategories, value);
        }

        public PpeCategory SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    if (EditCategoryCommand is RelayCommand editCmd) editCmd.RaiseCanExecuteChanged();
                    if (DeleteCategoryCommand is RelayCommand deleteCmd) deleteCmd.RaiseCanExecuteChanged();
                }
            }
        }

        public ObservableCollection<PpeMasterItemDisplay> DisplayMasterItems
        {
            get => _displayMasterItems;
            set => SetProperty(ref _displayMasterItems, value);
        }

        public PpeMasterItemDisplay SelectedMasterItemDisplay
        {
            get => _selectedMasterItemDisplay;
            set
            {
                if (SetProperty(ref _selectedMasterItemDisplay, value))
                {
                    if (_selectedMasterItemDisplay != null && _allMasterItemsInternal != null)
                    {
                        _selectedMasterItemData = _allMasterItemsInternal.FirstOrDefault(item => item.ItemMasterID == _selectedMasterItemDisplay.ItemMasterID);
                        Debug.WriteLine($"DEBUG: PpeMasterMgmtVM.SelectedMasterItemDisplay_set: _selectedMasterItemData updated. ID: {_selectedMasterItemData?.ItemMasterID}");
                    }
                    else
                    {
                        _selectedMasterItemData = null;
                        Debug.WriteLine($"DEBUG: PpeMasterMgmtVM.SelectedMasterItemDisplay_set: _selectedMasterItemData set to null.");
                    }

                    if (EditMasterItemCommand is RelayCommand editCmd) editCmd.RaiseCanExecuteChanged();
                    if (DeleteMasterItemCommand is RelayCommand deleteCmd) deleteCmd.RaiseCanExecuteChanged();
                }
            }
        }

        public ObservableCollection<PpeCategory> AllCategoriesForFilter
        {
            get => _allCategoriesForFilter;
            set => SetProperty(ref _allCategoriesForFilter, value);
        }

        public int? SelectedFilterCategoryID
        {
            get => _selectedFilterCategoryID;
            set
            {
                if (SetProperty(ref _selectedFilterCategoryID, value))
                {
                    ApplyMasterItemFilter();
                }
            }
        }
        #endregion

        #region Commands
        // Category Commands
        public ICommand LoadCategoriesCommand { get; }
        public ICommand AddCategoryCommand { get; }
        public ICommand EditCategoryCommand { get; }
        public ICommand DeleteCategoryCommand { get; }

        // Master Item Commands
        public ICommand LoadMasterItemsCommand { get; }
        public ICommand FilterMasterItemsCommand { get; }
        public ICommand ClearMasterItemFilterCommand { get; }
        public ICommand AddMasterItemCommand { get; }
        public ICommand EditMasterItemCommand { get; }
        public ICommand DeleteMasterItemCommand { get; }
        #endregion

        public PpeMasterManagementViewModel()
        {
            Debug.WriteLine("DEBUG: PpeMasterManagementViewModel_ctor: Constructor started.");
            AllCategories = new ObservableCollection<PpeCategory>();
            DisplayMasterItems = new ObservableCollection<PpeMasterItemDisplay>();
            AllCategoriesForFilter = new ObservableCollection<PpeCategory>();
            _allMasterItemsInternal = new List<PpeMasterItem>();

            LoadCategoriesCommand = new RelayCommand(ExecuteLoadCategories);
            AddCategoryCommand = new RelayCommand(ExecuteAddCategory);
            EditCategoryCommand = new RelayCommand(ExecuteEditCategory, CanExecuteEditOrDeleteCategory);
            DeleteCategoryCommand = new RelayCommand(ExecuteDeleteCategory, CanExecuteEditOrDeleteCategory);

            LoadMasterItemsCommand = new RelayCommand(ExecuteLoadMasterItems);
            FilterMasterItemsCommand = new RelayCommand(ApplyMasterItemFilter);
            ClearMasterItemFilterCommand = new RelayCommand(ExecuteClearMasterItemFilter);
            AddMasterItemCommand = new RelayCommand(ExecuteAddMasterItem);
            EditMasterItemCommand = new RelayCommand(ExecuteEditMasterItem, CanExecuteEditOrDeleteMasterItem);
            DeleteMasterItemCommand = new RelayCommand(ExecuteDeleteMasterItem, CanExecuteEditOrDeleteMasterItem);

            ExecuteLoadCategories(null);
            ExecuteLoadMasterItems(null);
            Debug.WriteLine("DEBUG: PpeMasterManagementViewModel_ctor: Constructor finished. Initial data loaded.");
        }

        #region Category Command Methods
        private void ExecuteLoadCategories(object parameter = null)
        {
            Debug.WriteLine("DEBUG: PpeMasterMgmtVM.ExecuteLoadCategories: Loading categories.");
            try
            {
                var categoriesFromDb = SQLiteDataAccess.GetAllCategories();
                AllCategories = new ObservableCollection<PpeCategory>(categoriesFromDb.OrderBy(c => c.CategoryName));

                var currentFilterId = SelectedFilterCategoryID; // 保存当前筛选
                AllCategoriesForFilter.Clear();
                AllCategoriesForFilter.Add(new PpeCategory { CategoryID = 0, CategoryName = "(所有类别)" });
                foreach (var cat in AllCategories)
                {
                    AllCategoriesForFilter.Add(cat);
                }
                // 尝试恢复之前的筛选，如果之前的筛选ID仍然有效
                if (currentFilterId.HasValue && AllCategoriesForFilter.Any(c => c.CategoryID == currentFilterId.Value))
                {
                    SelectedFilterCategoryID = currentFilterId;
                }
                else if (AllCategoriesForFilter.Any()) // 否则，默认选中“(所有类别)”
                {
                    SelectedFilterCategoryID = AllCategoriesForFilter.First().CategoryID;
                }


                Debug.WriteLine($"DEBUG: PpeMasterMgmtVM.ExecuteLoadCategories: Loaded {AllCategories.Count} categories.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DEBUG: PpeMasterMgmtVM.ExecuteLoadCategories: EXCEPTION - {ex.Message}");
                MessageBox.Show($"加载劳保用品类别失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanExecuteEditOrDeleteCategory(object parameter)
        {
            return SelectedCategory != null && SelectedCategory.CategoryID > 0;
        }

        private void ExecuteAddCategory(object parameter)
        {
            Debug.WriteLine("DEBUG: PpeMasterMgmtVM.ExecuteAddCategory: Initiating add new category.");
            var newCategory = new PpeCategory();
            var dialogViewModel = new EditPpeCategoryDialogViewModel(newCategory, true);

            var dialog = new EditPpeCategoryDialog
            {
                DataContext = dialogViewModel,
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                PpeCategory categoryToAdd = dialogViewModel.CurrentCategory;
                Debug.WriteLine($"DEBUG: PpeMasterMgmtVM.ExecuteAddCategory: Dialog confirmed save for CategoryName: '{categoryToAdd.CategoryName}'");
                int newId = SQLiteDataAccess.AddCategory(categoryToAdd);
                if (newId > 0)
                {
                    LoggingService.LogAction("类别管理", $"添加新类别: '{categoryToAdd.CategoryName}' (ID: {newId})");
                    ExecuteLoadCategories(null); // 重新加载类别列表以显示新增项 (这也会更新筛选列表)
                    MessageBox.Show($"类别 “{categoryToAdd.CategoryName}” 添加成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"添加类别 “{categoryToAdd.CategoryName}” 失败。可能是名称已存在或其他数据库错误。", "添加失败", MessageBoxButton.OK, MessageBoxImage.Error);
                    Debug.WriteLine("DEBUG: PpeMasterMgmtVM.ExecuteAddCategory: AddCategory returned failure.");
                }
            }
            else { Debug.WriteLine("DEBUG: PpeMasterMgmtVM.ExecuteAddCategory: Add category dialog was cancelled."); }
        }

        private void ExecuteEditCategory(object parameter)
        {
            if (!CanExecuteEditOrDeleteCategory(null)) return;

            Debug.WriteLine($"DEBUG: PpeMasterMgmtVM.ExecuteEditCategory: Initiating edit for category ID: {SelectedCategory.CategoryID}, Name: '{SelectedCategory.CategoryName}'");
            var dialogViewModel = new EditPpeCategoryDialogViewModel(SelectedCategory, false);

            var dialog = new EditPpeCategoryDialog
            {
                DataContext = dialogViewModel,
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                PpeCategory categoryToUpdate = dialogViewModel.CurrentCategory;
                Debug.WriteLine($"DEBUG: PpeMasterMgmtVM.ExecuteEditCategory: Dialog confirmed save for CategoryID: {categoryToUpdate.CategoryID}, NewName: '{categoryToUpdate.CategoryName}'");
                if (SQLiteDataAccess.UpdateCategory(categoryToUpdate))
                {
                    LoggingService.LogAction("类别管理", $"修改类别: '{categoryToUpdate.CategoryName}' (ID: {categoryToUpdate.CategoryID})");
                    ExecuteLoadCategories(null);
                    ExecuteLoadMasterItems(null); // 类别名称可能改变，主数据列表中的CategoryName也需要刷新
                    MessageBox.Show($"类别 “{categoryToUpdate.CategoryName}” 更新成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"更新类别 “{categoryToUpdate.CategoryName}” 失败。可能是名称与其他类别重复或其他数据库错误。", "更新失败", MessageBoxButton.OK, MessageBoxImage.Error);
                    Debug.WriteLine("DEBUG: PpeMasterMgmtVM.ExecuteEditCategory: UpdateCategory returned failure.");
                }
            }
            else { Debug.WriteLine($"DEBUG: PpeMasterMgmtVM.ExecuteEditCategory: Edit category dialog was cancelled for ID: {SelectedCategory.CategoryID}"); }
        }

        private void ExecuteDeleteCategory(object parameter)
        {
            if (!CanExecuteEditOrDeleteCategory(null)) return;

            Debug.WriteLine($"DEBUG: PpeMasterMgmtVM.ExecuteDeleteCategory: Attempting to delete category ID: {SelectedCategory.CategoryID}, Name: '{SelectedCategory.CategoryName}'");

            var masterItemsInThisCategory = SQLiteDataAccess.GetMasterItemsByCategoryId(SelectedCategory.CategoryID);
            if (masterItemsInThisCategory != null && masterItemsInThisCategory.Any())
            {
                MessageBox.Show($"无法删除类别 “{SelectedCategory.CategoryName}”，因为它已被 {masterItemsInThisCategory.Count} 个主数据条目使用。\n请先删除或修改这些主数据条目。", "删除失败", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"DEBUG: PpeMasterMgmtVM.ExecuteDeleteCategory: Category ID {SelectedCategory.CategoryID} is in use by {masterItemsInThisCategory.Count} master items.");
                return;
            }

            if (MessageBox.Show($"确定要删除类别 “{SelectedCategory.CategoryName}” 吗？此操作不可撤销。", "确认删除类别", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                bool success = false;
                string categoryNameForLog = SelectedCategory.CategoryName;
                int categoryIdForLog = SelectedCategory.CategoryID;
                try
                {
                    success = SQLiteDataAccess.DeleteCategory(SelectedCategory.CategoryID);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"DEBUG: PpeMasterMgmtVM.ExecuteDeleteCategory: EXCEPTION during delete: {ex.Message}");
                    MessageBox.Show($"删除类别时发生数据库错误: {ex.Message}", "删除失败", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (success)
                {
                    LoggingService.LogAction("类别管理", $"删除类别: '{categoryNameForLog}' (ID: {categoryIdForLog})");
                    ExecuteLoadCategories(null);
                    // SelectedCategory = null; // Let the list refresh handle this implicitly
                    MessageBox.Show($"类别 “{categoryNameForLog}” 删除成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    Debug.WriteLine($"DEBUG: PpeMasterMgmtVM.ExecuteDeleteCategory: Category ID {categoryIdForLog} deleted successfully.");
                }
                else
                {
                    MessageBox.Show($"删除类别 “{categoryNameForLog}” 失败。", "删除失败", MessageBoxButton.OK, MessageBoxImage.Error);
                    Debug.WriteLine($"DEBUG: PpeMasterMgmtVM.ExecuteDeleteCategory: DeleteCategory returned false for ID: {categoryIdForLog}");
                }
            }
            else { Debug.WriteLine($"DEBUG: PpeMasterMgmtVM.ExecuteDeleteCategory: Deletion cancelled by user for Category ID: {SelectedCategory.CategoryID}"); }
        }
        #endregion

        #region Master Item Command Methods
        private void ExecuteLoadMasterItems(object parameter = null)
        {
            Debug.WriteLine("DEBUG: PpeMasterMgmtVM.ExecuteLoadMasterItems: Loading all master items internally.");
            try
            {
                _allMasterItemsInternal = SQLiteDataAccess.GetAllMasterItems() ?? new List<PpeMasterItem>();
                ApplyMasterItemFilter();
                Debug.WriteLine($"DEBUG: PpeMasterMgmtVM.ExecuteLoadMasterItems: Loaded {_allMasterItemsInternal.Count} total master items internally.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DEBUG: PpeMasterMgmtVM.ExecuteLoadMasterItems: EXCEPTION - {ex.Message}");
                MessageBox.Show($"加载劳保用品主数据失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyMasterItemFilter(object parameter = null)
        {
            Debug.WriteLine($"DEBUG: PpeMasterMgmtVM.ApplyMasterItemFilter: Applying filter. CategoryID: {SelectedFilterCategoryID}");
            if (_allMasterItemsInternal == null)
            {
                DisplayMasterItems?.Clear();
                return;
            }

            IEnumerable<PpeMasterItem> filteredSource = _allMasterItemsInternal;
            if (SelectedFilterCategoryID.HasValue && SelectedFilterCategoryID.Value > 0)
            {
                filteredSource = _allMasterItemsInternal.Where(item => item.CategoryID_FK == SelectedFilterCategoryID.Value);
            }

            DisplayMasterItems.Clear();
            if (AllCategories == null) { Debug.WriteLine("DEBUG: PpeMasterMgmtVM.ApplyMasterItemFilter: AllCategories is null, cannot map CategoryName."); }

            foreach (var item in filteredSource.OrderBy(i => i.ItemName))
            {
                var category = AllCategories?.FirstOrDefault(c => c.CategoryID == item.CategoryID_FK);
                DisplayMasterItems.Add(new PpeMasterItemDisplay
                {
                    ItemMasterID = item.ItemMasterID,
                    ItemMasterCode = item.ItemMasterCode,
                    ItemName = item.ItemName,
                    CategoryID_FK = item.CategoryID_FK,
                    Size = item.Size,
                    UnitOfMeasure = item.UnitOfMeasure,
                    ExpectedLifespanDays = item.ExpectedLifespanDays,
                    DefaultRemarks = item.DefaultRemarks,
                    CurrentStock = item.CurrentStock,
                    LowStockThreshold = item.LowStockThreshold,
                    CategoryName = category?.CategoryName ?? "未知类别"
                });
            }
            Debug.WriteLine($"DEBUG: PpeMasterMgmtVM.ApplyMasterItemFilter: DisplayMasterItems updated with {DisplayMasterItems.Count} items.");
        }

        private void ExecuteClearMasterItemFilter(object parameter)
        {
            SelectedFilterCategoryID = 0; // 0 代表“(所有类别)”
            Debug.WriteLine("DEBUG: PpeMasterMgmtVM.ExecuteClearMasterItemFilter: Filter cleared (SelectedFilterCategoryID set to 0).");
        }

        private bool CanExecuteEditOrDeleteMasterItem(object parameter) => SelectedMasterItemDisplay != null;

        private void ExecuteAddMasterItem(object parameter)
        {
            Debug.WriteLine("DEBUG: PpeMasterMgmtVM.ExecuteAddMasterItem: Initiating add new master item.");
            var newItem = new PpeMasterItem { CurrentStock = 0, LowStockThreshold = 0 };

            if (AllCategories == null || !AllCategories.Any(c => c.CategoryID > 0))
            {
                MessageBox.Show("请先添加至少一个劳保用品类别，然后才能添加主数据条目。", "无可用类别", MessageBoxButton.OK, MessageBoxImage.Warning);
                Debug.WriteLine("DEBUG: PpeMasterMgmtVM.ExecuteAddMasterItem: No real categories available.");
                return;
            }

            var categoriesForDialog = new ObservableCollection<PpeCategory>(AllCategories.Where(c => c.CategoryID > 0).OrderBy(c => c.CategoryName));
            var dialogViewModel = new EditPpeMasterItemDialogViewModel(newItem, categoriesForDialog, true);

            var dialog = new EditPpeMasterItemDialog
            {
                DataContext = dialogViewModel,
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                PpeMasterItem itemToAdd = dialogViewModel.CurrentMasterItem;
                Debug.WriteLine($"DEBUG: PpeMasterMgmtVM.ExecuteAddMasterItem: Dialog confirmed save for ItemCode: '{itemToAdd.ItemMasterCode}', Name: '{itemToAdd.ItemName}'");

                int newId = SQLiteDataAccess.AddMasterItem(itemToAdd);
                if (newId > 0)
                {
                    LoggingService.LogAction("主数据管理", $"添加新主数据条目: '{itemToAdd.ItemName}' ({itemToAdd.ItemMasterCode}), ID: {newId}");
                    ExecuteLoadMasterItems(null);
                    MessageBox.Show($"主数据条目 “{itemToAdd.ItemName}” 添加成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"添加主数据条目 “{itemToAdd.ItemName}” 失败。\n可能原因：用品主代码已存在、未选择所属类别，或必填项为空。", "添加失败", MessageBoxButton.OK, MessageBoxImage.Error);
                    Debug.WriteLine($"DEBUG: PpeMasterMgmtVM.ExecuteAddMasterItem: AddMasterItem returned failure for ItemCode: '{itemToAdd.ItemMasterCode}'.");
                }
            }
            else { Debug.WriteLine("DEBUG: PpeMasterMgmtVM.ExecuteAddMasterItem: Add master item dialog was cancelled."); }
        }

        private void ExecuteEditMasterItem(object parameter)
        {
            if (_selectedMasterItemData == null)
            {
                MessageBox.Show("请先从列表中选择一个主数据条目进行编辑。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                Debug.WriteLine($"DEBUG: PpeMasterMgmtVM.ExecuteEditMasterItem: _selectedMasterItemData is null.");
                return;
            }

            Debug.WriteLine($"DEBUG: PpeMasterMgmtVM.ExecuteEditMasterItem: Initiating edit for master item ID: {_selectedMasterItemData.ItemMasterID}, Code: '{_selectedMasterItemData.ItemMasterCode}'");

            if (AllCategories == null || !AllCategories.Any(c => c.CategoryID > 0))
            {
                MessageBox.Show("无法编辑主数据条目，因为没有可用的劳保用品类别。请先添加类别。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine("DEBUG: PpeMasterMgmtVM.ExecuteEditMasterItem: No real categories available.");
                return;
            }

            var categoriesForDialog = new ObservableCollection<PpeCategory>(AllCategories.Where(c => c.CategoryID > 0).OrderBy(c => c.CategoryName));
            var dialogViewModel = new EditPpeMasterItemDialogViewModel(_selectedMasterItemData, categoriesForDialog, false);

            var dialog = new EditPpeMasterItemDialog
            {
                DataContext = dialogViewModel,
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                PpeMasterItem itemToUpdate = dialogViewModel.CurrentMasterItem;
                Debug.WriteLine($"DEBUG: PpeMasterMgmtVM.ExecuteEditMasterItem: Dialog confirmed save for ItemMasterID: {itemToUpdate.ItemMasterID}, New ItemCode: '{itemToUpdate.ItemMasterCode}', Name: '{itemToUpdate.ItemName}'");

                if (SQLiteDataAccess.UpdateMasterItem(itemToUpdate))
                {
                    LoggingService.LogAction("主数据管理", $"修改主数据条目: '{itemToUpdate.ItemName}' ({itemToUpdate.ItemMasterCode}), ID: {itemToUpdate.ItemMasterID}");
                    ExecuteLoadMasterItems(null);
                    MessageBox.Show($"主数据条目 “{itemToUpdate.ItemName}” 更新成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"更新主数据条目 “{itemToUpdate.ItemName}” 失败。\n可能原因：用品主代码与其他条目重复、未选择所属类别，或必填项为空。", "更新失败", MessageBoxButton.OK, MessageBoxImage.Error);
                    Debug.WriteLine($"DEBUG: PpeMasterMgmtVM.ExecuteEditMasterItem: UpdateMasterItem returned failure for ItemMasterID: {itemToUpdate.ItemMasterID}");
                }
            }
            else { Debug.WriteLine($"DEBUG: PpeMasterMgmtVM.ExecuteEditMasterItem: Edit master item dialog was cancelled for ID: {_selectedMasterItemData.ItemMasterID}"); }
        }

        private void ExecuteDeleteMasterItem(object parameter)
        {
            if (_selectedMasterItemData == null)
            {
                MessageBox.Show("请先从列表中选择一个主数据条目进行删除。", "提示");
                Debug.WriteLine($"DEBUG: PpeMasterMgmtVM.ExecuteDeleteMasterItem: _selectedMasterItemData is null.");
                return;
            }

            Debug.WriteLine($"DEBUG: PpeMasterMgmtVM.ExecuteDeleteMasterItem: Action to delete master item ID: {_selectedMasterItemData.ItemMasterID}, Name: '{_selectedMasterItemData.ItemName}'");
            if (MessageBox.Show($"确定要删除主数据项 “{_selectedMasterItemData.ItemName} ({_selectedMasterItemData.ItemMasterCode})” 吗？\n注意：此操作会将其在所有已发放记录中的关联置空。", "确认删除主数据", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                bool success = false;
                string itemNameForLog = _selectedMasterItemData.ItemName;
                int itemIdForLog = _selectedMasterItemData.ItemMasterID;
                try
                {
                    success = SQLiteDataAccess.DeleteMasterItem(_selectedMasterItemData.ItemMasterID);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"DEBUG: PpeMasterMgmtVM.ExecuteDeleteMasterItem: EXCEPTION: {ex.Message}");
                    MessageBox.Show($"删除主数据项时发生错误: {ex.Message}", "删除失败", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (success)
                {
                    LoggingService.LogAction("主数据管理", $"删除主数据项: '{itemNameForLog}' (ID: {itemIdForLog})");
                    ExecuteLoadMasterItems(null);
                    // SelectedMasterItemDisplay will be cleared if the item is no longer in DisplayMasterItems after refresh
                    MessageBox.Show($"主数据项 “{itemNameForLog}” 删除成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    Debug.WriteLine($"DEBUG: PpeMasterMgmtVM.ExecuteDeleteMasterItem: Item ID {itemIdForLog} deleted successfully.");
                }
                else
                {
                    MessageBox.Show($"删除主数据项 “{itemNameForLog}” 失败。", "删除失败", MessageBoxButton.OK, MessageBoxImage.Error);
                    Debug.WriteLine($"DEBUG: PpeMasterMgmtVM.ExecuteDeleteMasterItem: DeleteMasterItem returned false for ID: {itemIdForLog}");
                }
            }
            else { Debug.WriteLine($"DEBUG: PpeMasterMgmtVM.ExecuteDeleteMasterItem: Deletion cancelled by user for Item ID: {_selectedMasterItemData.ItemMasterID}"); }
        }
        #endregion
    }
}