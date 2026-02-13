// ViewModels/PPEIssuanceViewModel.cs
using PersonalPPEManager.Models;
using PersonalPPEManager.DataAccess;
using PersonalPPEManager.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace PersonalPPEManager.ViewModels
{
    public class PPEIssuanceViewModel : BaseViewModel
    {
        #region Constants
        private const string SuitPpeTypeFromCategory = "洁净服";
        private const string HatPpeType = "帽子";
        private const string SafetyShoePpeType = "白色劳保鞋";
        private const string CanvasShoePpeType = "白色帆布鞋";
        #endregion

        #region Services
        private readonly ConfigurationService _configService;
        #endregion

        #region Backing Fields
        private string _searchEmployeeID;
        private Employee _loadedEmployee;
        private bool _isEmployeeLoaded;

        private bool _suit1IsBeingIssued;
        private string _suit1ItemCode;
        private DateTime? _suit1IssueDate;
        // Suit1_Size is now derived from SelectedSuitMasterItem

        private bool _suit2IsBeingIssued;
        private string _suit2ItemCode;
        private DateTime? _suit2IssueDate;

        private bool _suit3IsBeingIssued;
        private string _suit3ItemCode;
        private DateTime? _suit3IssueDate;

        private bool _hat1IsBeingIssued;
        private string _hat1ItemCode;
        private DateTime? _hat1IssueDate;
        private bool _hat2IsBeingIssued;
        private string _hat2ItemCode;
        private DateTime? _hat2IssueDate;
        private bool _hat3IsBeingIssued;
        private string _hat3ItemCode;
        private DateTime? _hat3IssueDate;

        private bool _safetyShoesIsBeingIssued;
        private DateTime? _safetyShoesIssueDate;
        private string _safetyShoesSize;
        private string _safetyShoesCondition;
        private bool _canvasShoesIsBeingIssued;
        private DateTime? _canvasShoesIssueDate;
        private string _canvasShoesSize;
        private string _canvasShoesCondition;

        private ObservableCollection<PpeCategory> _allPpeCategories;
        private PpeCategory _selectedSuitCategory;
        private ObservableCollection<PpeMasterItem> _availableSuitMasterItems;
        private PpeMasterItem _selectedSuitMasterItem;
        
        private bool _isSuitReplacementModeActive;
        private ObservableCollection<PPEAssignment> _activeSuitsForReplacement;
        private PPEAssignment _selectedSuitForReplacement;
        private string _replacementInstructionMessage;

        private bool _isSaving;
        #endregion

        #region Public Properties
        public string SearchEmployeeID { get => _searchEmployeeID; set => SetProperty(ref _searchEmployeeID, value); }
        public Employee LoadedEmployee { get => _loadedEmployee; set { if (SetProperty(ref _loadedEmployee, value)) { IsEmployeeLoaded = _loadedEmployee != null; if (_loadedEmployee != null) { Debug.WriteLine($"DEBUG: PPEIssuanceVM.LoadedEmployee_set: Loaded: {_loadedEmployee.Name}"); ExitSuitReplacementMode(); } else { Debug.WriteLine("DEBUG: PPEIssuanceVM.LoadedEmployee_set: Cleared.");} } } }
        public bool IsEmployeeLoaded { get => _isEmployeeLoaded; set => SetProperty(ref _isEmployeeLoaded, value); }

        public bool Suit1_IsBeingIssued { get => _suit1IsBeingIssued; set => SetProperty(ref _suit1IsBeingIssued, value); }
        public string Suit1_ItemCode { get => _suit1ItemCode; set => SetProperty(ref _suit1ItemCode, value); }
        public DateTime? Suit1_IssueDate { get => _suit1IssueDate; set => SetProperty(ref _suit1IssueDate, value); }

        public bool Suit2_IsBeingIssued { get => _suit2IsBeingIssued; set => SetProperty(ref _suit2IsBeingIssued, value); }
        public string Suit2_ItemCode { get => _suit2ItemCode; set => SetProperty(ref _suit2ItemCode, value); }
        public DateTime? Suit2_IssueDate { get => _suit2IssueDate; set => SetProperty(ref _suit2IssueDate, value); }

        public bool Suit3_IsBeingIssued { get => _suit3IsBeingIssued; set => SetProperty(ref _suit3IsBeingIssued, value); }
        public string Suit3_ItemCode { get => _suit3ItemCode; set => SetProperty(ref _suit3ItemCode, value); }
        public DateTime? Suit3_IssueDate { get => _suit3IssueDate; set => SetProperty(ref _suit3IssueDate, value); }

        public bool Hat1_IsBeingIssued { get => _hat1IsBeingIssued; set => SetProperty(ref _hat1IsBeingIssued, value); }
        public string Hat1_ItemCode { get => _hat1ItemCode; set => SetProperty(ref _hat1ItemCode, value); }
        public DateTime? Hat1_IssueDate { get => _hat1IssueDate; set => SetProperty(ref _hat1IssueDate, value); }

        public bool Hat2_IsBeingIssued { get => _hat2IsBeingIssued; set => SetProperty(ref _hat2IsBeingIssued, value); }
        public string Hat2_ItemCode { get => _hat2ItemCode; set => SetProperty(ref _hat2ItemCode, value); }
        public DateTime? Hat2_IssueDate { get => _hat2IssueDate; set => SetProperty(ref _hat2IssueDate, value); }

        public bool Hat3_IsBeingIssued { get => _hat3IsBeingIssued; set => SetProperty(ref _hat3IsBeingIssued, value); }
        public string Hat3_ItemCode { get => _hat3ItemCode; set => SetProperty(ref _hat3ItemCode, value); }
        public DateTime? Hat3_IssueDate { get => _hat3IssueDate; set => SetProperty(ref _hat3IssueDate, value); }

        public bool SafetyShoes_IsBeingIssued { get => _safetyShoesIsBeingIssued; set => SetProperty(ref _safetyShoesIsBeingIssued, value); }
        public DateTime? SafetyShoes_IssueDate { get => _safetyShoesIssueDate; set => SetProperty(ref _safetyShoesIssueDate, value); }
        public string SafetyShoes_Size { get => _safetyShoesSize; set => SetProperty(ref _safetyShoesSize, value); }
        public string SafetyShoes_Condition { get => _safetyShoesCondition; set => SetProperty(ref _safetyShoesCondition, value); }

        public bool CanvasShoes_IsBeingIssued { get => _canvasShoesIsBeingIssued; set => SetProperty(ref _canvasShoesIsBeingIssued, value); }
        public DateTime? CanvasShoes_IssueDate { get => _canvasShoesIssueDate; set => SetProperty(ref _canvasShoesIssueDate, value); }
        public string CanvasShoes_Size { get => _canvasShoesSize; set => SetProperty(ref _canvasShoesSize, value); }
        public string CanvasShoes_Condition { get => _canvasShoesCondition; set => SetProperty(ref _canvasShoesCondition, value); }

        public ObservableCollection<string> ShoeConditions { get; }

        public ObservableCollection<PpeCategory> AllPpeCategories { get => _allPpeCategories; set => SetProperty(ref _allPpeCategories, value); }
        public PpeCategory SelectedSuitCategory
        {
            get => _selectedSuitCategory;
            set { if (SetProperty(ref _selectedSuitCategory, value)) { Debug.WriteLine($"DEBUG: PPEIssuanceVM.SelectedSuitCategory changed to: {value?.CategoryName}"); LoadAvailableSuitMasterItems(); SelectedSuitMasterItem = null; } }
        }
        public ObservableCollection<PpeMasterItem> AvailableSuitMasterItems { get => _availableSuitMasterItems; set => SetProperty(ref _availableSuitMasterItems, value); }
        public PpeMasterItem SelectedSuitMasterItem
        {
            get => _selectedSuitMasterItem;
            set { if (SetProperty(ref _selectedSuitMasterItem, value)) { Debug.WriteLine($"DEBUG: PPEIssuanceVM.SelectedSuitMasterItem changed to: {value?.DisplayName}. Stock: {value?.CurrentStock}, Size: {value?.Size}"); } }
        }
        
        public bool IsSuitReplacementModeActive { get => _isSuitReplacementModeActive; set { if (SetProperty(ref _isSuitReplacementModeActive, value)) { if (!_isSuitReplacementModeActive) { SelectedSuitForReplacement = null; ActiveSuitsForReplacement?.Clear(); ReplacementInstructionMessage = string.Empty; } if (SaveIssuanceCommand is RelayCommand cmd) cmd.RaiseCanExecuteChanged(); } } }
        public ObservableCollection<PPEAssignment> ActiveSuitsForReplacement { get => _activeSuitsForReplacement; set => SetProperty(ref _activeSuitsForReplacement, value); }
        public PPEAssignment SelectedSuitForReplacement { get => _selectedSuitForReplacement; set => SetProperty(ref _selectedSuitForReplacement, value); }
        public string ReplacementInstructionMessage { get => _replacementInstructionMessage; set => SetProperty(ref _replacementInstructionMessage, value); }

        public bool IsSaving { get => _isSaving; private set { if(SetProperty(ref _isSaving, value)) { if(SaveIssuanceCommand is RelayCommand saveCmd) saveCmd.RaiseCanExecuteChanged(); if(LoadEmployeeCommand is RelayCommand loadCmd) loadCmd.RaiseCanExecuteChanged(); if(ClearFormCommand is RelayCommand clearCmd) clearCmd.RaiseCanExecuteChanged(); } } }
        #endregion

        #region Commands
        public ICommand LoadEmployeeCommand { get; }
        public ICommand SaveIssuanceCommand { get; }
        public ICommand ClearFormCommand { get; }
        #endregion

        public PPEIssuanceViewModel()
        {
            Debug.WriteLine("DEBUG: PPEIssuanceViewModel_ctor: Constructor started.");
            _configService = ConfigurationService.Instance;
            Debug.WriteLine($"DEBUG: PPEIssuanceViewModel_ctor: MaxActiveSuits from config: {_configService.MaxActiveSuits}");

            ShoeConditions = new ObservableCollection<string> { "新", "旧" };
            AllPpeCategories = new ObservableCollection<PpeCategory>();
            AvailableSuitMasterItems = new ObservableCollection<PpeMasterItem>();
            ActiveSuitsForReplacement = new ObservableCollection<PPEAssignment>();

            LoadEmployeeCommand = new RelayCommand(ExecuteLoadEmployee, CanExecuteLoadEmployee);
            SaveIssuanceCommand = new RelayCommand(ExecuteSaveIssuance, CanExecuteSaveIssuance);
            ClearFormCommand = new RelayCommand(ExecuteClearForm, _ => !IsSaving);
            
            LoadInitialDataForIssuance();
            ResetPPEInputs(); 
            Debug.WriteLine("DEBUG: PPEIssuanceViewModel_ctor: Constructor finished.");
        }

        #region Command Methods and Helpers
        private void LoadInitialDataForIssuance()
        {
            Debug.WriteLine("DEBUG: PPEIssuanceVM.LoadInitialDataForIssuance: Loading all PPE categories.");
            try
            {
                var categories = SQLiteDataAccess.GetAllCategories();
                AllPpeCategories = new ObservableCollection<PpeCategory>(categories.OrderBy(c => c.CategoryName));
                Debug.WriteLine($"DEBUG: PPEIssuanceVM.LoadInitialDataForIssuance: Loaded {AllPpeCategories.Count} PPE categories.");
                SelectedSuitCategory = AllPpeCategories.FirstOrDefault(c => c.CategoryName == SuitPpeTypeFromCategory);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DEBUG: PPEIssuanceVM.LoadInitialDataForIssuance: EXCEPTION loading categories - {ex.Message}");
                MessageBox.Show($"加载劳保用品类别失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadAvailableSuitMasterItems()
        {
            AvailableSuitMasterItems.Clear();
            SelectedSuitMasterItem = null; 
            if (SelectedSuitCategory != null && SelectedSuitCategory.CategoryID > 0) 
            {
                Debug.WriteLine($"DEBUG: PPEIssuanceVM.LoadAvailableSuitMasterItems: Loading master items for CategoryID: {SelectedSuitCategory.CategoryID} ('{SelectedSuitCategory.CategoryName}')");
                try
                {
                    var masterItems = SQLiteDataAccess.GetMasterItemsByCategoryId(SelectedSuitCategory.CategoryID);
                    foreach (var item in masterItems.OrderBy(i => i.ItemName))
                    {
                        AvailableSuitMasterItems.Add(item);
                    }
                    Debug.WriteLine($"DEBUG: PPEIssuanceVM.LoadAvailableSuitMasterItems: Loaded {AvailableSuitMasterItems.Count} master items.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"DEBUG: PPEIssuanceVM.LoadAvailableSuitMasterItems: EXCEPTION - {ex.Message}");
                    MessageBox.Show($"加载“{SelectedSuitCategory.CategoryName}”类别下的主数据失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else { Debug.WriteLine("DEBUG: PPEIssuanceVM.LoadAvailableSuitMasterItems: No valid category selected."); }
        }

        private bool CanExecuteLoadEmployee(object parameter) { return !string.IsNullOrWhiteSpace(SearchEmployeeID) && !IsSaving; }
        private void ExecuteLoadEmployee(object parameter) { /* ... 与上一个完整版本相同 ... */ Debug.WriteLine($"DEBUG: PPEIssuanceVM.ExecuteLoadEmployee: Method started. Searching for EmployeeID: '{SearchEmployeeID}'"); if (string.IsNullOrWhiteSpace(SearchEmployeeID)) { Debug.WriteLine("DEBUG: PPEIssuanceVM.ExecuteLoadEmployee: SearchEmployeeID is null or whitespace. Aborting load."); LoadedEmployee = null; MessageBox.Show("请输入有效的员工工号。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning); return; } Employee foundEmployee = null; try { foundEmployee = SQLiteDataAccess.GetEmployeeById(SearchEmployeeID); } catch (Exception ex) { Debug.WriteLine($"DEBUG: PPEIssuanceVM.ExecuteLoadEmployee: Exception during SQLiteDataAccess.GetEmployeeById: {ex.Message}\n{ex.StackTrace}"); MessageBox.Show($"加载员工信息时发生数据库错误: {ex.Message}", "数据库错误", MessageBoxButton.OK, MessageBoxImage.Error); LoadedEmployee = null; return; } LoadedEmployee = foundEmployee; if (LoadedEmployee == null) { Debug.WriteLine($"DEBUG: PPEIssuanceVM.ExecuteLoadEmployee: No employee found for ID: '{SearchEmployeeID}'."); MessageBox.Show($"未找到工号为 '{SearchEmployeeID}' 的员工。", "提示", MessageBoxButton.OK, MessageBoxImage.Information); } else { Debug.WriteLine($"DEBUG: PPEIssuanceVM.ExecuteLoadEmployee: Employee found and set. ID: '{LoadedEmployee.EmployeeID}', Name: '{LoadedEmployee.Name}'."); } Debug.WriteLine("DEBUG: PPEIssuanceVM.ExecuteLoadEmployee: Method finished."); }
        
        private void ResetPPEInputs()
        {
            Debug.WriteLine("DEBUG: PPEIssuanceVM.ResetPPEInputs: Method called.");
            if (AllPpeCategories.Any() && SelectedSuitCategory == null) // 尝试在重置时也默认选中洁净服类别
            {
                SelectedSuitCategory = AllPpeCategories.FirstOrDefault(c => c.CategoryName == SuitPpeTypeFromCategory) ?? AllPpeCategories.FirstOrDefault();
            }
            else if (!AllPpeCategories.Any()) SelectedSuitCategory = null; // 如果没有类别，确保清空
            // SelectedSuitMasterItem 会在 SelectedSuitCategory 的 setter 中被清空或重新加载后清空

            Suit1_IsBeingIssued = false; Suit1_ItemCode = string.Empty; Suit1_IssueDate = DateTime.Today; 
            Suit2_IsBeingIssued = false; Suit2_ItemCode = string.Empty; Suit2_IssueDate = DateTime.Today; 
            Suit3_IsBeingIssued = false; Suit3_ItemCode = string.Empty; Suit3_IssueDate = DateTime.Today; 

            Hat1_IsBeingIssued = false; Hat1_ItemCode = string.Empty; Hat1_IssueDate = DateTime.Today;
            Hat2_IsBeingIssued = false; Hat2_ItemCode = string.Empty; Hat2_IssueDate = DateTime.Today;
            Hat3_IsBeingIssued = false; Hat3_ItemCode = string.Empty; Hat3_IssueDate = DateTime.Today;

            SafetyShoes_IsBeingIssued = false; SafetyShoes_Size = string.Empty; SafetyShoes_Condition = ShoeConditions.FirstOrDefault(); SafetyShoes_IssueDate = DateTime.Today;
            CanvasShoes_IsBeingIssued = false; CanvasShoes_Size = string.Empty; CanvasShoes_Condition = ShoeConditions.FirstOrDefault(); CanvasShoes_IssueDate = DateTime.Today;

            ExitSuitReplacementMode();
        }

        private void ExitSuitReplacementMode()
        {
            if (IsSuitReplacementModeActive || (ActiveSuitsForReplacement != null && ActiveSuitsForReplacement.Any()) || SelectedSuitForReplacement != null || !string.IsNullOrEmpty(ReplacementInstructionMessage))
            {
                IsSuitReplacementModeActive = false; 
                Debug.WriteLine("DEBUG: PPEIssuanceVM.ExitSuitReplacementMode: Suit replacement mode exited and related properties reset via IsSuitReplacementModeActive setter.");
            }
        }

        private void ExecuteClearForm(object parameter)
        {
            Debug.WriteLine("DEBUG: PPEIssuanceVM.ExecuteClearForm: Method started.");
            SearchEmployeeID = string.Empty;
            LoadedEmployee = null; 
            ResetPPEInputs(); 
            Debug.WriteLine("DEBUG: PPEIssuanceVM.ExecuteClearForm: Form cleared.");
        }
        
        private bool IsAnyNewPPEBeingIssued()
        {
            return Suit1_IsBeingIssued || Suit2_IsBeingIssued || Suit3_IsBeingIssued ||
                   Hat1_IsBeingIssued || Hat2_IsBeingIssued || Hat3_IsBeingIssued ||
                   SafetyShoes_IsBeingIssued || CanvasShoes_IsBeingIssued;
        }
        
        private bool ValidatePPEItem(bool isBeingIssued, DateTime? issueDate, string typeForLogDisplay, string itemCode = null, bool codeRequired = true, string size = null, bool sizeRequiredIfIssued = false)
        {
            if (!isBeingIssued) return true; 
            if (issueDate == null) { MessageBox.Show($"{typeForLogDisplay} 的发放日期不能为空。", "验证错误", MessageBoxButton.OK, MessageBoxImage.Warning); return false; }
            if (codeRequired && string.IsNullOrWhiteSpace(itemCode)) { MessageBox.Show($"{typeForLogDisplay} 的物品编号不能为空。", "验证错误", MessageBoxButton.OK, MessageBoxImage.Warning); return false; }
            if (sizeRequiredIfIssued && string.IsNullOrWhiteSpace(size)) { MessageBox.Show($"{typeForLogDisplay} 的尺码不能为空。", "验证错误", MessageBoxButton.OK, MessageBoxImage.Warning); return false; }
            return true;
        }

        private bool CanExecuteSaveIssuance(object parameter)
        {
            bool hasNewItems = IsAnyNewPPEBeingIssued();
            bool canSaveInReplacementMode = IsSuitReplacementModeActive && 
                                            SelectedSuitForReplacement != null &&
                                            SelectedSuitMasterItem != null && 
                                            (Suit1_IsBeingIssued || Suit2_IsBeingIssued || Suit3_IsBeingIssued); 

            bool canSave = IsEmployeeLoaded && (hasNewItems || canSaveInReplacementMode) && !IsSaving;
            return canSave;
        }
        
        private void ExecuteSaveIssuance(object parameter) 
        {
            Debug.WriteLine("DEBUG: PPEIssuanceVM.ExecuteSaveIssuance: Method started.");
            if (!IsEmployeeLoaded) { MessageBox.Show("请先加载员工信息。", "提示"); IsSaving = false; return; }

            IsSaving = true;
            try
            {
                var assignmentsToAdd = new List<PPEAssignment>();
                var assignmentsToUpdate = new List<PPEAssignment>();
                var masterItemsToUpdateStock = new Dictionary<int, int>(); 

                // --- 1. Process Cleanroom Suits ---
                var pendingNewPhysicalSuits = new List<(string SpecificCode, DateTime? IssueDate)>();
                string suitSizeFromMaster = SelectedSuitMasterItem?.Size; // Get once
                string suitCategoryNameFromMaster = SelectedSuitCategory?.CategoryName ?? SuitPpeTypeFromCategory; // Use selected category name

                if (Suit1_IsBeingIssued) { if (!ValidatePPEItem(true, Suit1_IssueDate, $"{SelectedSuitMasterItem?.ItemName ?? SuitPpeTypeFromCategory} (栏1)", Suit1_ItemCode, true, suitSizeFromMaster, SelectedSuitMasterItem == null)) { IsSaving = false; return; } pendingNewPhysicalSuits.Add((Suit1_ItemCode, Suit1_IssueDate)); }
                if (Suit2_IsBeingIssued) { if (!ValidatePPEItem(true, Suit2_IssueDate, $"{SelectedSuitMasterItem?.ItemName ?? SuitPpeTypeFromCategory} (栏2)", Suit2_ItemCode, true, suitSizeFromMaster, SelectedSuitMasterItem == null)) { IsSaving = false; return; } pendingNewPhysicalSuits.Add((Suit2_ItemCode, Suit2_IssueDate)); }
                if (Suit3_IsBeingIssued) { if (!ValidatePPEItem(true, Suit3_IssueDate, $"{SelectedSuitMasterItem?.ItemName ?? SuitPpeTypeFromCategory} (栏3)", Suit3_ItemCode, true, suitSizeFromMaster, SelectedSuitMasterItem == null)) { IsSaving = false; return; } pendingNewPhysicalSuits.Add((Suit3_ItemCode, Suit3_IssueDate)); }

                if (pendingNewPhysicalSuits.Any())
                {
                    if (SelectedSuitMasterItem == null || SelectedSuitCategory == null) { MessageBox.Show("请先为洁净服选择一个有效的类别和规格主数据。", "选择主数据", MessageBoxButton.OK, MessageBoxImage.Warning); IsSaving = false; return; }
                    if (SelectedSuitMasterItem.CurrentStock < pendingNewPhysicalSuits.Count) { MessageBox.Show($"洁净服 “{SelectedSuitMasterItem.ItemName}” 库存不足 (当前库存: {SelectedSuitMasterItem.CurrentStock}, 本次需发放: {pendingNewPhysicalSuits.Count})。", "库存不足", MessageBoxButton.OK, MessageBoxImage.Warning); IsSaving = false; return; }

                    List<PPEAssignment> activeDBSuits = SQLiteDataAccess.GetPPEAssignmentsForEmployeeAndType(LoadedEmployee.EmployeeID, suitCategoryNameFromMaster, activeOnly: true);
                    int currentActiveSuitCount = activeDBSuits.Count;
                    int maxActiveSuitsFromConfig = _configService.MaxActiveSuits;
                    Debug.WriteLine($"DEBUG: PPEIssuanceVM.ExecuteSaveIssuance: Suits - CurrentActive: {currentActiveSuitCount}, PendingNew: {pendingNewPhysicalSuits.Count}, MaxAllowed: {maxActiveSuitsFromConfig}");

                    if (IsSuitReplacementModeActive) 
                    {
                        if (SelectedSuitForReplacement == null) { MessageBox.Show("请选择一件需要替换的旧洁净服。", "选择替换"); IsSaving = false; return; }
                        if (pendingNewPhysicalSuits.Count != 1) { MessageBox.Show("替换操作当前仅支持一次发放一件新的洁净服来替换旧的。", "操作受限"); IsSaving = false; return; }
                        
                        SelectedSuitForReplacement.IsActive = false;
                        assignmentsToUpdate.Add(SelectedSuitForReplacement);
                        Debug.WriteLine($"DEBUG: Old suit ID {SelectedSuitForReplacement.AssignmentID} marked for deactivation.");
                        
                        var newSuitInfo = pendingNewPhysicalSuits.First();
                        assignmentsToAdd.Add(new PPEAssignment { EmployeeID_FK = LoadedEmployee.EmployeeID, PPE_Type = suitCategoryNameFromMaster, ItemSpecificCode = newSuitInfo.SpecificCode, IssueDate = newSuitInfo.IssueDate, Size = suitSizeFromMaster, ItemMasterID_FK = SelectedSuitMasterItem.ItemMasterID, IsActive = true });
                        
                        // Correctly increment count for the specific ItemMasterID
                        masterItemsToUpdateStock.TryGetValue(SelectedSuitMasterItem.ItemMasterID, out int currentDecCountRep);
                        masterItemsToUpdateStock[SelectedSuitMasterItem.ItemMasterID] = currentDecCountRep + 1;
                        Debug.WriteLine($"DEBUG: New suit (Code: {newSuitInfo.SpecificCode}) marked for addition. ItemMasterID {SelectedSuitMasterItem.ItemMasterID} marked for stock decrement.");
                        ExitSuitReplacementMode();
                    }
                    else 
                    {
                        if (currentActiveSuitCount + pendingNewPhysicalSuits.Count <= maxActiveSuitsFromConfig)
                        {
                            foreach (var newSuitInfo in pendingNewPhysicalSuits)
                            {
                                assignmentsToAdd.Add(new PPEAssignment { EmployeeID_FK = LoadedEmployee.EmployeeID, PPE_Type = suitCategoryNameFromMaster, ItemSpecificCode = newSuitInfo.SpecificCode, IssueDate = newSuitInfo.IssueDate, Size = suitSizeFromMaster, ItemMasterID_FK = SelectedSuitMasterItem.ItemMasterID, IsActive = true });
                                masterItemsToUpdateStock.TryGetValue(SelectedSuitMasterItem.ItemMasterID, out int currentDecCountDir); // Corrected
                                masterItemsToUpdateStock[SelectedSuitMasterItem.ItemMasterID] = currentDecCountDir + 1; // Corrected
                            }
                            Debug.WriteLine($"DEBUG: Adding {pendingNewPhysicalSuits.Count} new suit(s) directly.");
                        }
                        else 
                        {
                            int numToDeactivate = (currentActiveSuitCount + pendingNewPhysicalSuits.Count) - maxActiveSuitsFromConfig;
                            if (pendingNewPhysicalSuits.Count == 1 && numToDeactivate == 1)
                            {
                                ActiveSuitsForReplacement = new ObservableCollection<PPEAssignment>(activeDBSuits.OrderBy(s => s.AssignmentID));
                                SelectedSuitForReplacement = null; 
                                IsSuitReplacementModeActive = true;
                                ReplacementInstructionMessage = $"员工已持有 {currentActiveSuitCount} 套有效洁净服，已达上限 ({maxActiveSuitsFromConfig}套)。\n您正尝试发放1套新洁净服。请从下方列表中选择1套旧洁净服进行替换，然后再次点击“保存发放记录”以确认。";
                                MessageBox.Show(ReplacementInstructionMessage, "需要替换洁净服");
                                IsSaving = false; return; 
                            }
                            else
                            {
                                MessageBox.Show($"操作将导致洁净服数量 ({currentActiveSuitCount + pendingNewPhysicalSuits.Count}) 超出最大允许数量 ({maxActiveSuitsFromConfig})。\n当前替换流程仅支持在达到上限时，用“1件新洁净服”替换“1件旧洁净服”。\n请调整本次发放洁净服的数量。", "操作受限");
                                IsSaving = false; return;
                            }
                        }
                    }
                }
                else if (IsSuitReplacementModeActive && !pendingNewPhysicalSuits.Any())
                {
                     MessageBox.Show("您处于洁净服替换模式，但没有勾选新的洁净服进行发放。如果要退出替换模式，请点击“清空表单”或加载其他员工。", "操作提示");
                     IsSaving = false; return;
                }

                // --- 2. Process Hats (Simple Add) ---
                var pendingNewHatsFromForm = new List<(string Code, DateTime? Date)>();
                if (Hat1_IsBeingIssued) { if (!ValidatePPEItem(true, Hat1_IssueDate, "帽子1", Hat1_ItemCode, true)) { IsSaving = false; return; } pendingNewHatsFromForm.Add((Hat1_ItemCode, Hat1_IssueDate)); }
                if (Hat2_IsBeingIssued) { if (!ValidatePPEItem(true, Hat2_IssueDate, "帽子2", Hat2_ItemCode, true)) { IsSaving = false; return; } pendingNewHatsFromForm.Add((Hat2_ItemCode, Hat2_IssueDate)); }
                if (Hat3_IsBeingIssued) { if (!ValidatePPEItem(true, Hat3_IssueDate, "帽子3", Hat3_ItemCode, true)) { IsSaving = false; return; } pendingNewHatsFromForm.Add((Hat3_ItemCode, Hat3_IssueDate)); }
                foreach(var newHat in pendingNewHatsFromForm) { assignmentsToAdd.Add(new PPEAssignment { EmployeeID_FK = LoadedEmployee.EmployeeID, PPE_Type = HatPpeType, ItemSpecificCode = newHat.Code, IssueDate = newHat.Date, IsActive = true }); }
                
                // --- 3. Process Shoes (Simple Add) ---
                if (SafetyShoes_IsBeingIssued) { if (!ValidatePPEItem(true, SafetyShoes_IssueDate, SafetyShoePpeType, codeRequired:false, size: SafetyShoes_Size, sizeRequiredIfIssued:true) || string.IsNullOrWhiteSpace(SafetyShoes_Condition))  { if(string.IsNullOrWhiteSpace(SafetyShoes_Condition)) MessageBox.Show($"{SafetyShoePpeType}的新旧状态不能为空。", "验证错误"); IsSaving = false; return; } assignmentsToAdd.Add(new PPEAssignment { EmployeeID_FK = LoadedEmployee.EmployeeID, PPE_Type = SafetyShoePpeType, IssueDate = SafetyShoes_IssueDate, Size = SafetyShoes_Size, Condition = SafetyShoes_Condition, IsActive = true }); }
                if (CanvasShoes_IsBeingIssued) { if (!ValidatePPEItem(true, CanvasShoes_IssueDate, CanvasShoePpeType, codeRequired:false, size: CanvasShoes_Size, sizeRequiredIfIssued:true) || string.IsNullOrWhiteSpace(CanvasShoes_Condition)) { if(string.IsNullOrWhiteSpace(CanvasShoes_Condition)) MessageBox.Show($"{CanvasShoePpeType}的新旧状态不能为空。", "验证错误"); IsSaving = false; return; } assignmentsToAdd.Add(new PPEAssignment { EmployeeID_FK = LoadedEmployee.EmployeeID, PPE_Type = CanvasShoePpeType, IssueDate = CanvasShoes_IssueDate, Size = CanvasShoes_Size, Condition = CanvasShoes_Condition, IsActive = true }); }
                
                if (!assignmentsToAdd.Any() && !assignmentsToUpdate.Any()){ if (!IsSuitReplacementModeActive) MessageBox.Show("没有需要保存的劳保用品信息。", "提示"); Debug.WriteLine("DEBUG: PPEIssuanceVM.ExecuteSaveIssuance: No assignments to add or update."); IsSaving = false; return; }

                int successDbOpsCount = 0;
                // DB Operations
                foreach (var assignmentToUpdate in assignmentsToUpdate) { if (SQLiteDataAccess.UpdatePPEAssignment(assignmentToUpdate)) { successDbOpsCount++; LoggingService.LogAction("劳保记录更新(设为无效)", $"员工 {LoadedEmployee.Name} ({LoadedEmployee.EmployeeID}) 的劳保记录 {assignmentToUpdate.AssignmentID} ({assignmentToUpdate.PPE_Type} - {assignmentToUpdate.ItemSpecificCode}) 已设为无效。"); } else { Debug.WriteLine($"Failed to update AssignmentID: {assignmentToUpdate.AssignmentID}"); } }
                foreach (var assignmentToAdd in assignmentsToAdd) { if (SQLiteDataAccess.AddPPEAssignment(assignmentToAdd)) { successDbOpsCount++; LoggingService.LogAction("劳保发放", $"为员工 {LoadedEmployee.Name} ({LoadedEmployee.EmployeeID}) 发放 {assignmentToAdd.PPE_Type}" + (string.IsNullOrWhiteSpace(assignmentToAdd.ItemSpecificCode) ? "" : $" (编号: {assignmentToAdd.ItemSpecificCode})") + $"，日期: {assignmentToAdd.IssueDate?.ToString("yyyy-MM-dd")}"); } else { Debug.WriteLine($"Failed to add {assignmentToAdd.PPE_Type} (Code: {assignmentToAdd.ItemSpecificCode})"); } }
                
                int totalAttemptedDbOps = assignmentsToAdd.Count + assignmentsToUpdate.Count;
                bool allAssignmentsSavedOrUpdated = successDbOpsCount == totalAttemptedDbOps;

                if (allAssignmentsSavedOrUpdated && totalAttemptedDbOps > 0 && masterItemsToUpdateStock.Any()) 
                {
                    foreach (var entry in masterItemsToUpdateStock)
                    {
                        Debug.WriteLine($"Updating stock for ItemMasterID: {entry.Key}, QuantityChange: {-entry.Value}");
                        if (!SQLiteDataAccess.UpdateMasterItemStock(entry.Key, -entry.Value)) 
                        {
                            Debug.WriteLine($"FAILED to update stock for ItemMasterID: {entry.Key}.");
                            MessageBox.Show($"警告：劳保用品记录已保存，但更新主数据库存失败 (ID: {entry.Key})。请手动核查库存。", "库存更新失败");
                            LoggingService.LogAction("库存更新失败", $"劳保发放后，更新主数据库存失败 (ID: {entry.Key}) for Employee: {LoadedEmployee.Name} ({LoadedEmployee.EmployeeID})");
                        }
                        else
                        {
                            if (SelectedSuitMasterItem != null && SelectedSuitMasterItem.ItemMasterID == entry.Key)
                            {
                                SelectedSuitMasterItem.CurrentStock -= entry.Value; 
                                OnPropertyChanged(nameof(SelectedSuitMasterItem)); 
                            }
                        }
                    }
                }
                
                // Final User Feedback
                if (allAssignmentsSavedOrUpdated && totalAttemptedDbOps > 0) { MessageBox.Show($"成功为员工 {LoadedEmployee.Name} 处理 {totalAttemptedDbOps} 项劳保用品记录。", "成功"); ExecuteClearForm(null);  }
                else if (successDbOpsCount > 0) { MessageBox.Show($"为员工 {LoadedEmployee.Name} 部分操作成功 ({successDbOpsCount}/{totalAttemptedDbOps} 项)。请检查日志。", "部分成功"); if(IsSuitReplacementModeActive) ExitSuitReplacementMode(); }
                else if (totalAttemptedDbOps > 0)  { MessageBox.Show($"为员工 {LoadedEmployee.Name} 处理劳保用品记录失败。请检查日志。", "失败"); }
            }
            catch (Exception ex)
            {
                 Debug.WriteLine($"DEBUG: PPEIssuanceVM.ExecuteSaveIssuance: UNHANDLED EXCEPTION: {ex.Message}\n{ex.StackTrace}");
                 MessageBox.Show($"保存过程中发生意外错误: {ex.Message}", "严重错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsSaving = false;
                Debug.WriteLine("DEBUG: PPEIssuanceVM.ExecuteSaveIssuance: Method finished (finally block).");
            }
        }
        #endregion
    }
}