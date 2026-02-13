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
        private const string SuitCategoryNameDefault = "洁净服";
        private const string HatCategoryNameDefault = "帽子";
        private const string SafetyShoeCategoryNameDefault = "白色劳保鞋"; // 与模型/数据库中的PPE_Type一致
        private const string CanvasShoeCategoryNameDefault = "白色帆布鞋"; // 与模型/数据库中的PPE_Type一致
        #endregion

        #region Services
        private readonly ConfigurationService _configService;
        #endregion

        #region Backing Fields
        private string _searchEmployeeID;
        private Employee _loadedEmployee;
        private bool _isEmployeeLoaded;

        // PPE Input - Physical Item Specifics
        private bool _suit1IsBeingIssued; private string _suit1ItemCode; private DateTime? _suit1IssueDate;
        private bool _suit2IsBeingIssued; private string _suit2ItemCode; private DateTime? _suit2IssueDate;
        private bool _suit3IsBeingIssued; private string _suit3ItemCode; private DateTime? _suit3IssueDate;

        private bool _hat1IsBeingIssued; private string _hat1ItemCode; private DateTime? _hat1IssueDate;
        private bool _hat2IsBeingIssued; private string _hat2ItemCode; private DateTime? _hat2IssueDate;
        private bool _hat3IsBeingIssued; private string _hat3ItemCode; private DateTime? _hat3IssueDate;

        private bool _safetyShoesIsBeingIssued; private DateTime? _safetyShoesIssueDate; private string _safetyShoesSizeVM; private string _safetyShoesCondition;
        private bool _canvasShoesIsBeingIssued; private DateTime? _canvasShoesIssueDate; private string _canvasShoesSizeVM; private string _canvasShoesCondition;

        // Master Data Selection
        private ObservableCollection<PpeCategory> _allPpeCategories;

        private PpeCategory _selectedSuitCategory;
        private ObservableCollection<PpeMasterItem> _availableSuitMasterItems;
        private PpeMasterItem _selectedSuitMasterItem;

        private PpeCategory _selectedHatCategory;
        private ObservableCollection<PpeMasterItem> _availableHatMasterItems;
        private PpeMasterItem _selectedHatMasterItem;

        private PpeCategory _selectedSafetyShoeCategory;
        private ObservableCollection<PpeMasterItem> _availableSafetyShoeMasterItems;
        private PpeMasterItem _selectedSafetyShoeMasterItem;

        private PpeCategory _selectedCanvasShoeCategory;
        private ObservableCollection<PpeMasterItem> _availableCanvasShoeMasterItems;
        private PpeMasterItem _selectedCanvasShoeMasterItem;

        // Suit Replacement Logic
        private bool _isSuitReplacementModeActive;
        private ObservableCollection<PPEAssignment> _activeSuitsForReplacement;
        private PPEAssignment _selectedSuitForReplacement;
        private string _replacementInstructionMessage;

        private bool _isSaving;
        #endregion

        #region Public Properties
        public string SearchEmployeeID { get => _searchEmployeeID; set => SetProperty(ref _searchEmployeeID, value); }
        public Employee LoadedEmployee { get => _loadedEmployee; set { if (SetProperty(ref _loadedEmployee, value)) { IsEmployeeLoaded = _loadedEmployee != null; if (_loadedEmployee != null) { Debug.WriteLine($"DEBUG: PPEIssuanceVM.LoadedEmployee_set: Loaded: {_loadedEmployee.Name}"); ExitSuitReplacementMode(); } else { Debug.WriteLine("DEBUG: PPEIssuanceVM.LoadedEmployee_set: Cleared."); } } } }
        public bool IsEmployeeLoaded { get => _isEmployeeLoaded; set => SetProperty(ref _isEmployeeLoaded, value); }

        // Suit Physical Item Inputs
        public bool Suit1_IsBeingIssued { get => _suit1IsBeingIssued; set => SetProperty(ref _suit1IsBeingIssued, value); }
        public string Suit1_ItemCode { get => _suit1ItemCode; set => SetProperty(ref _suit1ItemCode, value); }
        public DateTime? Suit1_IssueDate { get => _suit1IssueDate; set => SetProperty(ref _suit1IssueDate, value); }
        // Suit Size is now from SelectedSuitMasterItem.Size

        public bool Suit2_IsBeingIssued { get => _suit2IsBeingIssued; set => SetProperty(ref _suit2IsBeingIssued, value); }
        public string Suit2_ItemCode { get => _suit2ItemCode; set => SetProperty(ref _suit2ItemCode, value); }
        public DateTime? Suit2_IssueDate { get => _suit2IssueDate; set => SetProperty(ref _suit2IssueDate, value); }

        public bool Suit3_IsBeingIssued { get => _suit3IsBeingIssued; set => SetProperty(ref _suit3IsBeingIssued, value); }
        public string Suit3_ItemCode { get => _suit3ItemCode; set => SetProperty(ref _suit3ItemCode, value); }
        public DateTime? Suit3_IssueDate { get => _suit3IssueDate; set => SetProperty(ref _suit3IssueDate, value); }

        // Hat Physical Item Inputs
        public bool Hat1_IsBeingIssued { get => _hat1IsBeingIssued; set => SetProperty(ref _hat1IsBeingIssued, value); }
        public string Hat1_ItemCode { get => _hat1ItemCode; set => SetProperty(ref _hat1ItemCode, value); }
        public DateTime? Hat1_IssueDate { get => _hat1IssueDate; set => SetProperty(ref _hat1IssueDate, value); }

        public bool Hat2_IsBeingIssued { get => _hat2IsBeingIssued; set => SetProperty(ref _hat2IsBeingIssued, value); }
        public string Hat2_ItemCode { get => _hat2ItemCode; set => SetProperty(ref _hat2ItemCode, value); }
        public DateTime? Hat2_IssueDate { get => _hat2IssueDate; set => SetProperty(ref _hat2IssueDate, value); }

        public bool Hat3_IsBeingIssued { get => _hat3IsBeingIssued; set => SetProperty(ref _hat3IsBeingIssued, value); }
        public string Hat3_ItemCode { get => _hat3ItemCode; set => SetProperty(ref _hat3ItemCode, value); }
        public DateTime? Hat3_IssueDate { get => _hat3IssueDate; set => SetProperty(ref _hat3IssueDate, value); }

        // Shoe Physical Item Inputs
        public bool SafetyShoes_IsBeingIssued { get => _safetyShoesIsBeingIssued; set => SetProperty(ref _safetyShoesIsBeingIssued, value); }
        public DateTime? SafetyShoes_IssueDate { get => _safetyShoesIssueDate; set => SetProperty(ref _safetyShoesIssueDate, value); }
        public string SafetyShoes_SizeVM { get => _safetyShoesSizeVM; set => SetProperty(ref _safetyShoesSizeVM, value); } // For UI display, from master
        public string SafetyShoes_Condition { get => _safetyShoesCondition; set => SetProperty(ref _safetyShoesCondition, value); }

        public bool CanvasShoes_IsBeingIssued { get => _canvasShoesIsBeingIssued; set => SetProperty(ref _canvasShoesIsBeingIssued, value); }
        public DateTime? CanvasShoes_IssueDate { get => _canvasShoesIssueDate; set => SetProperty(ref _canvasShoesIssueDate, value); }
        public string CanvasShoes_SizeVM { get => _canvasShoesSizeVM; set => SetProperty(ref _canvasShoesSizeVM, value); } // For UI display, from master
        public string CanvasShoes_Condition { get => _canvasShoesCondition; set => SetProperty(ref _canvasShoesCondition, value); }

        public ObservableCollection<string> ShoeConditions { get; }

        // Master Data Selection Properties
        public ObservableCollection<PpeCategory> AllPpeCategories { get => _allPpeCategories; set => SetProperty(ref _allPpeCategories, value); }

        public PpeCategory SelectedSuitCategory { get => _selectedSuitCategory; set { if (SetProperty(ref _selectedSuitCategory, value)) { Debug.WriteLine($"SelectedSuitCategory: {value?.CategoryName}"); LoadAvailableSuitMasterItems(); SelectedSuitMasterItem = null; } } }
        public ObservableCollection<PpeMasterItem> AvailableSuitMasterItems { get => _availableSuitMasterItems; set => SetProperty(ref _availableSuitMasterItems, value); }
        public PpeMasterItem SelectedSuitMasterItem { get => _selectedSuitMasterItem; set { if (SetProperty(ref _selectedSuitMasterItem, value)) { Debug.WriteLine($"SelectedSuitMasterItem: {value?.DisplayName}"); /* Update related UI if needed */ } } }

        public PpeCategory SelectedHatCategory { get => _selectedHatCategory; set { if (SetProperty(ref _selectedHatCategory, value)) { Debug.WriteLine($"SelectedHatCategory: {value?.CategoryName}"); LoadAvailableHatMasterItems(); SelectedHatMasterItem = null; } } }
        public ObservableCollection<PpeMasterItem> AvailableHatMasterItems { get => _availableHatMasterItems; set => SetProperty(ref _availableHatMasterItems, value); }
        public PpeMasterItem SelectedHatMasterItem { get => _selectedHatMasterItem; set { if (SetProperty(ref _selectedHatMasterItem, value)) { Debug.WriteLine($"SelectedHatMasterItem: {value?.DisplayName}"); } } }

        public PpeCategory SelectedSafetyShoeCategory { get => _selectedSafetyShoeCategory; set { if (SetProperty(ref _selectedSafetyShoeCategory, value)) { Debug.WriteLine($"SelectedSafetyShoeCategory: {value?.CategoryName}"); LoadAvailableSafetyShoeMasterItems(); SelectedSafetyShoeMasterItem = null; } } }
        public ObservableCollection<PpeMasterItem> AvailableSafetyShoeMasterItems { get => _availableSafetyShoeMasterItems; set => SetProperty(ref _availableSafetyShoeMasterItems, value); }
        public PpeMasterItem SelectedSafetyShoeMasterItem { get => _selectedSafetyShoeMasterItem; set { if (SetProperty(ref _selectedSafetyShoeMasterItem, value)) { Debug.WriteLine($"SelectedSafetyShoeMasterItem: {value?.DisplayName}"); if (value != null) SafetyShoes_SizeVM = value.Size; else SafetyShoes_SizeVM = string.Empty; } } }

        public PpeCategory SelectedCanvasShoeCategory { get => _selectedCanvasShoeCategory; set { if (SetProperty(ref _selectedCanvasShoeCategory, value)) { Debug.WriteLine($"SelectedCanvasShoeCategory: {value?.CategoryName}"); LoadAvailableCanvasShoeMasterItems(); SelectedCanvasShoeMasterItem = null; } } }
        public ObservableCollection<PpeMasterItem> AvailableCanvasShoeMasterItems { get => _availableCanvasShoeMasterItems; set => SetProperty(ref _availableCanvasShoeMasterItems, value); }
        public PpeMasterItem SelectedCanvasShoeMasterItem { get => _selectedCanvasShoeMasterItem; set { if (SetProperty(ref _selectedCanvasShoeMasterItem, value)) { Debug.WriteLine($"SelectedCanvasShoeMasterItem: {value?.DisplayName}"); if (value != null) CanvasShoes_SizeVM = value.Size; else CanvasShoes_SizeVM = string.Empty; } } }

        // Suit Replacement Logic Properties
        public bool IsSuitReplacementModeActive { get => _isSuitReplacementModeActive; set { if (SetProperty(ref _isSuitReplacementModeActive, value)) { if (!_isSuitReplacementModeActive) { SelectedSuitForReplacement = null; ActiveSuitsForReplacement?.Clear(); ReplacementInstructionMessage = string.Empty; } if (SaveIssuanceCommand is RelayCommand cmd) cmd.RaiseCanExecuteChanged(); } } }
        public ObservableCollection<PPEAssignment> ActiveSuitsForReplacement { get => _activeSuitsForReplacement; set => SetProperty(ref _activeSuitsForReplacement, value); }
        public PPEAssignment SelectedSuitForReplacement { get => _selectedSuitForReplacement; set => SetProperty(ref _selectedSuitForReplacement, value); }
        public string ReplacementInstructionMessage { get => _replacementInstructionMessage; set => SetProperty(ref _replacementInstructionMessage, value); }

        public bool IsSaving { get => _isSaving; private set { if (SetProperty(ref _isSaving, value)) { if (SaveIssuanceCommand is RelayCommand saveCmd) saveCmd.RaiseCanExecuteChanged(); if (LoadEmployeeCommand is RelayCommand loadCmd) loadCmd.RaiseCanExecuteChanged(); if (ClearFormCommand is RelayCommand clearCmd) clearCmd.RaiseCanExecuteChanged(); } } }
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
            AvailableHatMasterItems = new ObservableCollection<PpeMasterItem>();
            AvailableSafetyShoeMasterItems = new ObservableCollection<PpeMasterItem>();
            AvailableCanvasShoeMasterItems = new ObservableCollection<PpeMasterItem>();

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

                SelectedSuitCategory = AllPpeCategories.FirstOrDefault(c => c.CategoryName == SuitCategoryNameDefault);
                SelectedHatCategory = AllPpeCategories.FirstOrDefault(c => c.CategoryName == HatCategoryNameDefault);
                SelectedSafetyShoeCategory = AllPpeCategories.FirstOrDefault(c => c.CategoryName == SafetyShoeCategoryNameDefault);
                SelectedCanvasShoeCategory = AllPpeCategories.FirstOrDefault(c => c.CategoryName == CanvasShoeCategoryNameDefault);
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
            { /* ... (与之前版本相同) ... */  Debug.WriteLine($"DEBUG: PPEIssuanceVM.LoadAvailableSuitMasterItems for CategoryID: {SelectedSuitCategory.CategoryID}"); try { var mi = SQLiteDataAccess.GetMasterItemsByCategoryId(SelectedSuitCategory.CategoryID); foreach (var i in mi.OrderBy(item => item.ItemName)) AvailableSuitMasterItems.Add(i); Debug.WriteLine($"DEBUG: Loaded {AvailableSuitMasterItems.Count} suit master items."); } catch (Exception ex) { Debug.WriteLine($"EX in LoadAvailableSuitMasterItems: {ex.Message}"); MessageBox.Show($"加载主数据失败: {ex.Message}"); } }
            else { Debug.WriteLine("DEBUG: PPEIssuanceVM.LoadAvailableSuitMasterItems: No valid category selected."); }
        }
        private void LoadAvailableHatMasterItems()
        {
            AvailableHatMasterItems.Clear();
            SelectedHatMasterItem = null;
            if (SelectedHatCategory != null && SelectedHatCategory.CategoryID > 0)
            { Debug.WriteLine($"DEBUG: PPEIssuanceVM.LoadAvailableHatMasterItems for CategoryID: {SelectedHatCategory.CategoryID}"); try { var mi = SQLiteDataAccess.GetMasterItemsByCategoryId(SelectedHatCategory.CategoryID); foreach (var i in mi.OrderBy(item => item.ItemName)) AvailableHatMasterItems.Add(i); Debug.WriteLine($"DEBUG: Loaded {AvailableHatMasterItems.Count} hat master items."); } catch (Exception ex) { Debug.WriteLine($"EX in LoadAvailableHatMasterItems: {ex.Message}"); MessageBox.Show($"加载主数据失败: {ex.Message}"); } }
            else { Debug.WriteLine("DEBUG: PPEIssuanceVM.LoadAvailableHatMasterItems: No valid category selected."); }
        }
        private void LoadAvailableSafetyShoeMasterItems()
        {
            AvailableSafetyShoeMasterItems.Clear();
            SelectedSafetyShoeMasterItem = null;
            if (SelectedSafetyShoeCategory != null && SelectedSafetyShoeCategory.CategoryID > 0)
            { Debug.WriteLine($"DEBUG: PPEIssuanceVM.LoadAvailableSafetyShoeMasterItems for CategoryID: {SelectedSafetyShoeCategory.CategoryID}"); try { var mi = SQLiteDataAccess.GetMasterItemsByCategoryId(SelectedSafetyShoeCategory.CategoryID); foreach (var i in mi.OrderBy(item => item.ItemName)) AvailableSafetyShoeMasterItems.Add(i); Debug.WriteLine($"DEBUG: Loaded {AvailableSafetyShoeMasterItems.Count} safety shoe master items."); } catch (Exception ex) { Debug.WriteLine($"EX in LoadAvailableSafetyShoeMasterItems: {ex.Message}"); MessageBox.Show($"加载主数据失败: {ex.Message}"); } }
            else { Debug.WriteLine("DEBUG: PPEIssuanceVM.LoadAvailableSafetyShoeMasterItems: No valid category selected."); }
        }
        private void LoadAvailableCanvasShoeMasterItems()
        {
            AvailableCanvasShoeMasterItems.Clear();
            SelectedCanvasShoeMasterItem = null;
            if (SelectedCanvasShoeCategory != null && SelectedCanvasShoeCategory.CategoryID > 0)
            { Debug.WriteLine($"DEBUG: PPEIssuanceVM.LoadAvailableCanvasShoeMasterItems for CategoryID: {SelectedCanvasShoeCategory.CategoryID}"); try { var mi = SQLiteDataAccess.GetMasterItemsByCategoryId(SelectedCanvasShoeCategory.CategoryID); foreach (var i in mi.OrderBy(item => item.ItemName)) AvailableCanvasShoeMasterItems.Add(i); Debug.WriteLine($"DEBUG: Loaded {AvailableCanvasShoeMasterItems.Count} canvas shoe master items."); } catch (Exception ex) { Debug.WriteLine($"EX in LoadAvailableCanvasShoeMasterItems: {ex.Message}"); MessageBox.Show($"加载主数据失败: {ex.Message}"); } }
            else { Debug.WriteLine("DEBUG: PPEIssuanceVM.LoadAvailableCanvasShoeMasterItems: No valid category selected."); }
        }

        private bool CanExecuteLoadEmployee(object parameter) { /* ... 与之前版本相同 ... */ return !string.IsNullOrWhiteSpace(SearchEmployeeID) && !IsSaving; }
        private void ExecuteLoadEmployee(object parameter) { /* ... 与之前版本相同 ... */ Debug.WriteLine($"DEBUG: PPEIssuanceVM.ExecuteLoadEmployee: Method started. Searching for EmployeeID: '{SearchEmployeeID}'"); if (string.IsNullOrWhiteSpace(SearchEmployeeID)) { Debug.WriteLine("DEBUG: PPEIssuanceVM.ExecuteLoadEmployee: SearchEmployeeID is null or whitespace. Aborting load."); LoadedEmployee = null; MessageBox.Show("请输入有效的员工工号。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning); return; } Employee foundEmployee = null; try { foundEmployee = SQLiteDataAccess.GetEmployeeById(SearchEmployeeID); } catch (Exception ex) { Debug.WriteLine($"DEBUG: PPEIssuanceVM.ExecuteLoadEmployee: Exception during SQLiteDataAccess.GetEmployeeById: {ex.Message}\n{ex.StackTrace}"); MessageBox.Show($"加载员工信息时发生数据库错误: {ex.Message}", "数据库错误", MessageBoxButton.OK, MessageBoxImage.Error); LoadedEmployee = null; return; } LoadedEmployee = foundEmployee; if (LoadedEmployee == null) { Debug.WriteLine($"DEBUG: PPEIssuanceVM.ExecuteLoadEmployee: No employee found for ID: '{SearchEmployeeID}'."); MessageBox.Show($"未找到工号为 '{SearchEmployeeID}' 的员工。", "提示", MessageBoxButton.OK, MessageBoxImage.Information); } else { Debug.WriteLine($"DEBUG: PPEIssuanceVM.ExecuteLoadEmployee: Employee found and set. ID: '{LoadedEmployee.EmployeeID}', Name: '{LoadedEmployee.Name}'."); } Debug.WriteLine("DEBUG: PPEIssuanceVM.ExecuteLoadEmployee: Method finished."); }

        private void ResetPPEInputs()
        {
            Debug.WriteLine("DEBUG: PPEIssuanceVM.ResetPPEInputs: Method called.");

            if (AllPpeCategories.Any())
            {
                SelectedSuitCategory = AllPpeCategories.FirstOrDefault(c => c.CategoryName == SuitCategoryNameDefault) ?? AllPpeCategories.FirstOrDefault();
                SelectedHatCategory = AllPpeCategories.FirstOrDefault(c => c.CategoryName == HatCategoryNameDefault) ?? AllPpeCategories.FirstOrDefault();
                SelectedSafetyShoeCategory = AllPpeCategories.FirstOrDefault(c => c.CategoryName == SafetyShoeCategoryNameDefault) ?? AllPpeCategories.FirstOrDefault();
                SelectedCanvasShoeCategory = AllPpeCategories.FirstOrDefault(c => c.CategoryName == CanvasShoeCategoryNameDefault) ?? AllPpeCategories.FirstOrDefault();
            }
            else { SelectedSuitCategory = null; SelectedHatCategory = null; SelectedSafetyShoeCategory = null; SelectedCanvasShoeCategory = null; }

            Suit1_IsBeingIssued = false; Suit1_ItemCode = string.Empty; Suit1_IssueDate = DateTime.Today;
            Suit2_IsBeingIssued = false; Suit2_ItemCode = string.Empty; Suit2_IssueDate = DateTime.Today;
            Suit3_IsBeingIssued = false; Suit3_ItemCode = string.Empty; Suit3_IssueDate = DateTime.Today;

            Hat1_IsBeingIssued = false; Hat1_ItemCode = string.Empty; Hat1_IssueDate = DateTime.Today;
            Hat2_IsBeingIssued = false; Hat2_ItemCode = string.Empty; Hat2_IssueDate = DateTime.Today;
            Hat3_IsBeingIssued = false; Hat3_ItemCode = string.Empty; Hat3_IssueDate = DateTime.Today;

            SafetyShoes_IsBeingIssued = false; SafetyShoes_SizeVM = string.Empty; SafetyShoes_Condition = ShoeConditions.FirstOrDefault(); SafetyShoes_IssueDate = DateTime.Today;
            CanvasShoes_IsBeingIssued = false; CanvasShoes_SizeVM = string.Empty; CanvasShoes_Condition = ShoeConditions.FirstOrDefault(); CanvasShoes_IssueDate = DateTime.Today;

            ExitSuitReplacementMode();
        }

        private void ExitSuitReplacementMode() { /* ... 与之前版本相同 ... */ if (IsSuitReplacementModeActive || (ActiveSuitsForReplacement != null && ActiveSuitsForReplacement.Any()) || SelectedSuitForReplacement != null || !string.IsNullOrEmpty(ReplacementInstructionMessage)) { IsSuitReplacementModeActive = false; Debug.WriteLine("DEBUG: PPEIssuanceVM.ExitSuitReplacementMode: Suit replacement mode exited and related properties reset via IsSuitReplacementModeActive setter."); } }
        private void ExecuteClearForm(object parameter) { /* ... 与之前版本相同 ... */ Debug.WriteLine("DEBUG: PPEIssuanceVM.ExecuteClearForm: Method started."); SearchEmployeeID = string.Empty; LoadedEmployee = null; ResetPPEInputs(); Debug.WriteLine("DEBUG: PPEIssuanceVM.ExecuteClearForm: Form cleared."); }
        private bool IsAnyNewPPEBeingIssued() { /* ... 与之前版本相同 ... */ return Suit1_IsBeingIssued || Suit2_IsBeingIssued || Suit3_IsBeingIssued || Hat1_IsBeingIssued || Hat2_IsBeingIssued || Hat3_IsBeingIssued || SafetyShoes_IsBeingIssued || CanvasShoes_IsBeingIssued; }
        private bool ValidatePPEItem(bool isBeingIssued, DateTime? issueDate, string typeForLogDisplay, string itemCode = null, bool codeRequired = true, string size = null, bool sizeRequiredIfIssued = false) { /* ... 与之前版本相同 ... */ if (!isBeingIssued) return true; if (issueDate == null) { MessageBox.Show($"{typeForLogDisplay} 的发放日期不能为空。", "验证错误"); return false; } if (codeRequired && string.IsNullOrWhiteSpace(itemCode)) { MessageBox.Show($"{typeForLogDisplay} 的物品编号不能为空。", "验证错误"); return false; } if (sizeRequiredIfIssued && string.IsNullOrWhiteSpace(size)) { MessageBox.Show($"{typeForLogDisplay} 的尺码不能为空。", "验证错误"); return false; } return true; }
        private bool CanExecuteSaveIssuance(object parameter) { /* ... 与之前版本相同, 已包含IsSaving检查 ... */ bool hasNewItems = IsAnyNewPPEBeingIssued(); bool canSaveInReplacementMode = IsSuitReplacementModeActive && SelectedSuitForReplacement != null && SelectedSuitMasterItem != null && (Suit1_IsBeingIssued || Suit2_IsBeingIssued || Suit3_IsBeingIssued); bool canSave = IsEmployeeLoaded && (hasNewItems || canSaveInReplacementMode) && !IsSaving; return canSave; }

        private void ExecuteSaveIssuance(object parameter)
        {
            Debug.WriteLine("DEBUG: PPEIssuanceVM.ExecuteSaveIssuance: Method started.");
            if (!IsEmployeeLoaded) { MessageBox.Show("请先加载员工信息。", "提示"); return; }

            IsSaving = true;
            try
            {
                var assignmentsToAdd = new List<PPEAssignment>();
                var assignmentsToUpdate = new List<PPEAssignment>();
                var masterItemsToUpdateStock = new Dictionary<int, int>();

                // --- 1. Process Cleanroom Suits (Master Data, Stock, Replacement) ---
                var pendingNewPhysicalSuits = new List<(string SpecificCode, DateTime? IssueDate)>();
                string suitSizeFromMaster = SelectedSuitMasterItem?.Size;
                string suitCategoryName = SelectedSuitCategory?.CategoryName ?? SuitCategoryNameDefault;

                if (Suit1_IsBeingIssued) { if (!ValidatePPEItem(true, Suit1_IssueDate, $"{SelectedSuitMasterItem?.ItemName ?? suitCategoryName} (栏1)", Suit1_ItemCode, true, suitSizeFromMaster, SelectedSuitMasterItem == null && !string.IsNullOrEmpty(suitSizeFromMaster))) { IsSaving = false; return; } pendingNewPhysicalSuits.Add((Suit1_ItemCode, Suit1_IssueDate)); }
                if (Suit2_IsBeingIssued) { if (!ValidatePPEItem(true, Suit2_IssueDate, $"{SelectedSuitMasterItem?.ItemName ?? suitCategoryName} (栏2)", Suit2_ItemCode, true, suitSizeFromMaster, SelectedSuitMasterItem == null && !string.IsNullOrEmpty(suitSizeFromMaster))) { IsSaving = false; return; } pendingNewPhysicalSuits.Add((Suit2_ItemCode, Suit2_IssueDate)); }
                if (Suit3_IsBeingIssued) { if (!ValidatePPEItem(true, Suit3_IssueDate, $"{SelectedSuitMasterItem?.ItemName ?? suitCategoryName} (栏3)", Suit3_ItemCode, true, suitSizeFromMaster, SelectedSuitMasterItem == null && !string.IsNullOrEmpty(suitSizeFromMaster))) { IsSaving = false; return; } pendingNewPhysicalSuits.Add((Suit3_ItemCode, Suit3_IssueDate)); }

                if (pendingNewPhysicalSuits.Any())
                {
                    if (SelectedSuitMasterItem == null || SelectedSuitCategory == null) { MessageBox.Show("请先为洁净服选择一个有效的类别和规格主数据。", "选择主数据"); IsSaving = false; return; }
                    if (SelectedSuitMasterItem.CurrentStock < pendingNewPhysicalSuits.Count) { MessageBox.Show($"洁净服 “{SelectedSuitMasterItem.ItemName}” 库存不足 (当前库存: {SelectedSuitMasterItem.CurrentStock}, 本次需发放: {pendingNewPhysicalSuits.Count})。", "库存不足"); IsSaving = false; return; }

                    List<PPEAssignment> activeDBSuits = SQLiteDataAccess.GetPPEAssignmentsForEmployeeAndType(LoadedEmployee.EmployeeID, suitCategoryName, activeOnly: true);
                    int currentActiveSuitCount = activeDBSuits.Count;
                    int maxActiveSuitsFromConfig = _configService.MaxActiveSuits;
                    Debug.WriteLine($"Suits - CurrentActive: {currentActiveSuitCount}, PendingNew: {pendingNewPhysicalSuits.Count}, MaxAllowed: {maxActiveSuitsFromConfig}");

                    if (IsSuitReplacementModeActive)
                    { /* ... 与上一版本替换逻辑相同，使用 suitCategoryName 和 suitSizeFromMaster, 并更新 masterItemsToUpdateStock ... */ if (SelectedSuitForReplacement == null) { MessageBox.Show("请选择一件需要替换的旧洁净服。"); IsSaving = false; return; } if (pendingNewPhysicalSuits.Count != 1) { MessageBox.Show("替换操作仅支持一对一。"); IsSaving = false; return; } SelectedSuitForReplacement.IsActive = false; assignmentsToUpdate.Add(SelectedSuitForReplacement); var newSuitInfo = pendingNewPhysicalSuits.First(); assignmentsToAdd.Add(new PPEAssignment { EmployeeID_FK = LoadedEmployee.EmployeeID, PPE_Type = suitCategoryName, ItemSpecificCode = newSuitInfo.SpecificCode, IssueDate = newSuitInfo.IssueDate, Size = suitSizeFromMaster, ItemMasterID_FK = SelectedSuitMasterItem.ItemMasterID, IsActive = true }); masterItemsToUpdateStock.TryGetValue(SelectedSuitMasterItem.ItemMasterID, out int currentCountRep); masterItemsToUpdateStock[SelectedSuitMasterItem.ItemMasterID] = currentCountRep + 1; ExitSuitReplacementMode(); }
                    else
                    {
                        if (currentActiveSuitCount + pendingNewPhysicalSuits.Count <= maxActiveSuitsFromConfig)
                        { foreach (var newSuitInfo in pendingNewPhysicalSuits) { assignmentsToAdd.Add(new PPEAssignment { EmployeeID_FK = LoadedEmployee.EmployeeID, PPE_Type = suitCategoryName, ItemSpecificCode = newSuitInfo.SpecificCode, IssueDate = newSuitInfo.IssueDate, Size = suitSizeFromMaster, ItemMasterID_FK = SelectedSuitMasterItem.ItemMasterID, IsActive = true }); masterItemsToUpdateStock.TryGetValue(SelectedSuitMasterItem.ItemMasterID, out int currentCountDir); masterItemsToUpdateStock[SelectedSuitMasterItem.ItemMasterID] = currentCountDir + 1; } }
                        else
                        { int numToDeactivate = (currentActiveSuitCount + pendingNewPhysicalSuits.Count) - maxActiveSuitsFromConfig; if (pendingNewPhysicalSuits.Count == 1 && numToDeactivate == 1) { ActiveSuitsForReplacement = new ObservableCollection<PPEAssignment>(activeDBSuits.OrderBy(s => s.AssignmentID)); SelectedSuitForReplacement = null; IsSuitReplacementModeActive = true; ReplacementInstructionMessage = $"员工已持有 {currentActiveSuitCount} 套有效洁净服，已达上限 ({maxActiveSuitsFromConfig}套)。\n您正尝试发放1套新洁净服。请选择1套旧洁净服替换，然后再次点击“保存”。"; MessageBox.Show(ReplacementInstructionMessage, "需替换"); IsSaving = false; return; } else { MessageBox.Show($"操作将导致洁净服超限 ({maxActiveSuitsFromConfig}套)，且不满足1对1替换条件。\n请调整数量。", "操作受限"); IsSaving = false; return; } }
                    }
                }
                else if (IsSuitReplacementModeActive) { MessageBox.Show("替换模式下，请勾选并发放新的洁净服。", "操作提示"); IsSaving = false; return; }

                // --- 2. Process Hats ---
                var pendingNewHats = new List<(string Code, DateTime? Date)>();
                // ↓↓↓↓↓ 获取正确的类别名称和主数据条目 ↓↓↓↓↓
                string hatCategoryName = SelectedHatCategory?.CategoryName ?? HatCategoryNameDefault;
                PpeMasterItem hatMaster = SelectedHatMasterItem;
                // ↑↑↑↑↑ 获取正确的类别名称和主数据条目 ↑↑↑↑↑

                if (Hat1_IsBeingIssued) { if (!ValidatePPEItem(true, Hat1_IssueDate, $"{hatMaster?.ItemName ?? hatCategoryName} (栏1)", Hat1_ItemCode, true, hatMaster?.Size, false)) { IsSaving = false; return; } pendingNewHats.Add((Hat1_ItemCode, Hat1_IssueDate)); }
                if (Hat2_IsBeingIssued) { if (!ValidatePPEItem(true, Hat2_IssueDate, $"{hatMaster?.ItemName ?? hatCategoryName} (栏2)", Hat2_ItemCode, true, hatMaster?.Size, false)) { IsSaving = false; return; } pendingNewHats.Add((Hat2_ItemCode, Hat2_IssueDate)); }
                if (Hat3_IsBeingIssued) { if (!ValidatePPEItem(true, Hat3_IssueDate, $"{hatMaster?.ItemName ?? hatCategoryName} (栏3)", Hat3_ItemCode, true, hatMaster?.Size, false)) { IsSaving = false; return; } pendingNewHats.Add((Hat3_ItemCode, Hat3_IssueDate)); }

                if (pendingNewHats.Any())
                {
                    if (hatMaster == null) { MessageBox.Show("请为帽子选择类别和规格主数据。", "选择主数据"); IsSaving = false; return; }
                    if (hatMaster.CurrentStock < pendingNewHats.Count) { MessageBox.Show($"帽子“{hatMaster.ItemName}”库存不足。", "库存不足"); IsSaving = false; return; }
                    // TODO: Add Hat replacement logic if MaxActiveHats is configured and exceeded
                    foreach (var newHatInfo in pendingNewHats) { assignmentsToAdd.Add(new PPEAssignment { EmployeeID_FK = LoadedEmployee.EmployeeID, PPE_Type = hatCategoryName, ItemSpecificCode = newHatInfo.Code, IssueDate = newHatInfo.Date, Size = hatMaster.Size, ItemMasterID_FK = hatMaster.ItemMasterID, IsActive = true }); masterItemsToUpdateStock.TryGetValue(hatMaster.ItemMasterID, out int cH); masterItemsToUpdateStock[hatMaster.ItemMasterID] = cH + 1; }
                }

                // --- 3. Process Safety Shoes ---
                if (SafetyShoes_IsBeingIssued)
                {
                    if (SelectedSafetyShoeMasterItem == null || SelectedSafetyShoeCategory == null) { MessageBox.Show("请为白色劳保鞋选择类别和规格主数据。", "选择主数据"); IsSaving = false; return; }
                    if (SelectedSafetyShoeMasterItem.CurrentStock < 1) { MessageBox.Show($"“{SelectedSafetyShoeMasterItem.ItemName}”库存不足。", "库存不足"); IsSaving = false; return; }
                    if (!ValidatePPEItem(true, SafetyShoes_IssueDate, SelectedSafetyShoeMasterItem.ItemName, codeRequired: false, size: SelectedSafetyShoeMasterItem.Size, sizeRequiredIfIssued: true) || string.IsNullOrWhiteSpace(SafetyShoes_Condition)) { if (string.IsNullOrWhiteSpace(SafetyShoes_Condition)) MessageBox.Show($"{SelectedSafetyShoeMasterItem.ItemName}的新旧状态不能为空。", "验证错误"); IsSaving = false; return; }
                    // TODO: Add SafetyShoe replacement logic if MaxActiveSafetyShoes is configured and exceeded
                    assignmentsToAdd.Add(new PPEAssignment { EmployeeID_FK = LoadedEmployee.EmployeeID, PPE_Type = SelectedSafetyShoeCategory.CategoryName, ItemSpecificCode = null, IssueDate = SafetyShoes_IssueDate, Size = SelectedSafetyShoeMasterItem.Size, Condition = SafetyShoes_Condition, ItemMasterID_FK = SelectedSafetyShoeMasterItem.ItemMasterID, IsActive = true });
                    masterItemsToUpdateStock.TryGetValue(SelectedSafetyShoeMasterItem.ItemMasterID, out int cSS); masterItemsToUpdateStock[SelectedSafetyShoeMasterItem.ItemMasterID] = cSS + 1;
                }
                // --- 4. Process Canvas Shoes ---
                if (CanvasShoes_IsBeingIssued)
                {
                    if (SelectedCanvasShoeMasterItem == null || SelectedCanvasShoeCategory == null) { MessageBox.Show("请为白色帆布鞋选择类别和规格主数据。", "选择主数据"); IsSaving = false; return; }
                    if (SelectedCanvasShoeMasterItem.CurrentStock < 1) { MessageBox.Show($"“{SelectedCanvasShoeMasterItem.ItemName}”库存不足。", "库存不足"); IsSaving = false; return; }
                    if (!ValidatePPEItem(true, CanvasShoes_IssueDate, SelectedCanvasShoeMasterItem.ItemName, codeRequired: false, size: SelectedCanvasShoeMasterItem.Size, sizeRequiredIfIssued: true) || string.IsNullOrWhiteSpace(CanvasShoes_Condition)) { if (string.IsNullOrWhiteSpace(CanvasShoes_Condition)) MessageBox.Show($"{SelectedCanvasShoeMasterItem.ItemName}的新旧状态不能为空。", "验证错误"); IsSaving = false; return; }
                    // TODO: Add CanvasShoe replacement logic if MaxActiveCanvasShoes is configured and exceeded
                    assignmentsToAdd.Add(new PPEAssignment { EmployeeID_FK = LoadedEmployee.EmployeeID, PPE_Type = SelectedCanvasShoeCategory.CategoryName, ItemSpecificCode = null, IssueDate = CanvasShoes_IssueDate, Size = SelectedCanvasShoeMasterItem.Size, Condition = CanvasShoes_Condition, ItemMasterID_FK = SelectedCanvasShoeMasterItem.ItemMasterID, IsActive = true });
                    masterItemsToUpdateStock.TryGetValue(SelectedCanvasShoeMasterItem.ItemMasterID, out int cCS); masterItemsToUpdateStock[SelectedCanvasShoeMasterItem.ItemMasterID] = cCS + 1;
                }

                // --- DB Operations ---
                if (!assignmentsToAdd.Any() && !assignmentsToUpdate.Any()) { if (!IsSuitReplacementModeActive) MessageBox.Show("没有需要保存的劳保用品信息。", "提示"); IsSaving = false; return; }
                int successDbOpsCount = 0;
                foreach (var assignmentToUpdate in assignmentsToUpdate) { if (SQLiteDataAccess.UpdatePPEAssignment(assignmentToUpdate)) { successDbOpsCount++; LoggingService.LogAction("劳保记录更新(设为无效)", $"员工 {LoadedEmployee.Name} ({LoadedEmployee.EmployeeID}) 的劳保记录 {assignmentToUpdate.AssignmentID} ({assignmentToUpdate.PPE_Type} - {assignmentToUpdate.ItemSpecificCode}) 已设为无效。"); } else { Debug.WriteLine($"Failed to update AssignmentID: {assignmentToUpdate.AssignmentID}"); } }
                foreach (var assignmentToAdd in assignmentsToAdd) { if (SQLiteDataAccess.AddPPEAssignment(assignmentToAdd)) { successDbOpsCount++; LoggingService.LogAction("劳保发放", $"为员工 {LoadedEmployee.Name} ({LoadedEmployee.EmployeeID}) 发放 {assignmentToAdd.PPE_Type}" + (string.IsNullOrWhiteSpace(assignmentToAdd.ItemSpecificCode) ? "" : $" (编号: {assignmentToAdd.ItemSpecificCode})") + $"，日期: {assignmentToAdd.IssueDate?.ToString("yyyy-MM-dd")}"); } else { Debug.WriteLine($"Failed to add {assignmentToAdd.PPE_Type} (Code: {assignmentToAdd.ItemSpecificCode})"); } }

                int totalAttemptedDbOps = assignmentsToAdd.Count + assignmentsToUpdate.Count;
                bool allAssignmentsSavedOrUpdated = successDbOpsCount == totalAttemptedDbOps;

                if (allAssignmentsSavedOrUpdated && totalAttemptedDbOps > 0 && masterItemsToUpdateStock.Any())
                { /* ... (与上一版本相同: 更新库存, 刷新VM中的SelectedMasterItem.CurrentStock) ... */ foreach (var entry in masterItemsToUpdateStock) { if (!SQLiteDataAccess.UpdateMasterItemStock(entry.Key, -entry.Value)) { MessageBox.Show($"警告：库存更新失败 (ID: {entry.Key})。"); LoggingService.LogAction("库存更新失败", $"劳保发放后，更新库存失败 (ID: {entry.Key})"); } else { if (SelectedSuitMasterItem?.ItemMasterID == entry.Key) { SelectedSuitMasterItem.CurrentStock -= entry.Value; OnPropertyChanged(nameof(SelectedSuitMasterItem)); } else if (SelectedHatMasterItem?.ItemMasterID == entry.Key) { SelectedHatMasterItem.CurrentStock -= entry.Value; OnPropertyChanged(nameof(SelectedHatMasterItem)); } else if (SelectedSafetyShoeMasterItem?.ItemMasterID == entry.Key) { SelectedSafetyShoeMasterItem.CurrentStock -= entry.Value; OnPropertyChanged(nameof(SelectedSafetyShoeMasterItem)); } else if (SelectedCanvasShoeMasterItem?.ItemMasterID == entry.Key) { SelectedCanvasShoeMasterItem.CurrentStock -= entry.Value; OnPropertyChanged(nameof(SelectedCanvasShoeMasterItem)); } } } }

                if (allAssignmentsSavedOrUpdated && totalAttemptedDbOps > 0) { MessageBox.Show($"成功为员工 {LoadedEmployee.Name} 处理 {totalAttemptedDbOps} 项劳保用品记录。", "成功"); ExecuteClearForm(null); }
                else if (successDbOpsCount > 0) { MessageBox.Show($"为员工 {LoadedEmployee.Name} 部分操作成功 ({successDbOpsCount}/{totalAttemptedDbOps} 项)。", "部分成功"); if (IsSuitReplacementModeActive) ExitSuitReplacementMode(); }
                else if (totalAttemptedDbOps > 0) { MessageBox.Show($"为员工 {LoadedEmployee.Name} 处理劳保用品记录失败。", "失败"); }
            }
            catch (Exception ex) { Debug.WriteLine($"UNHANDLED EXCEPTION in ExecuteSaveIssuance: {ex.Message}\n{ex.StackTrace}"); MessageBox.Show($"保存时发生意外错误: {ex.Message}"); }
            finally { IsSaving = false; Debug.WriteLine("DEBUG: PPEIssuanceVM.ExecuteSaveIssuance: Method finished (finally block)."); }
        }
        #endregion
    }
}