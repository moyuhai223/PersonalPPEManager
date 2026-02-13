// ViewModels/QueryViewModel.cs
using PersonalPPEManager.Models;
using PersonalPPEManager.DataAccess;
using PersonalPPEManager.Views;     // 为了 EditPpeAssignmentDialog
using System;
using System.Collections.Generic; // 为了 List<T>
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Linq;      // For FirstOrDefault, Any
using System.Windows;   // 为了 MessageBox, Application.Current
using System.Diagnostics; // For Debug.WriteLine
using PersonalPPEManager.Services; // <<--- 添加或确保这一行存在

namespace PersonalPPEManager.ViewModels
{
    public class QueryViewModel : BaseViewModel
    {
        #region Constants for Search Types
        private const string SearchTypeByIdConst = "按工号";
        private const string SearchByNameConst = "按姓名";
        private const string SearchBySuitCodeConst = "按洁净服编码";
        private const string SuitPpeTypeForSearch = "洁净服"; // 用于按洁净服编码搜索时的用品类型
        #endregion

        #region Backing Fields
        private string _searchTerm;
        private ObservableCollection<string> _searchTypes;
        private string _selectedSearchType;

        private ObservableCollection<Employee> _searchResultsEmployees;
        private Employee _selectedEmployeeFromResults;
        private Employee _displayedEmployee;
        private ObservableCollection<PPEAssignment> _employeeActivePPE;
        private PPEAssignment _selectedPpeAssignmentForEdit;

        private bool _isEmployeeDetailsVisible;
        private bool _isSearchResultsVisible;
        #endregion

        #region Public Properties
        public string SearchTerm
        {
            get => _searchTerm;
            set => SetProperty(ref _searchTerm, value);
        }

        public ObservableCollection<string> SearchTypes
        {
            get => _searchTypes;
            set => SetProperty(ref _searchTypes, value);
        }

        public string SelectedSearchType
        {
            get => _selectedSearchType;
            set => SetProperty(ref _selectedSearchType, value);
        }

        public ObservableCollection<Employee> SearchResultsEmployees
        {
            get => _searchResultsEmployees;
            set => SetProperty(ref _searchResultsEmployees, value);
        }

        public Employee SelectedEmployeeFromResults
        {
            get => _selectedEmployeeFromResults;
            set
            {
                Debug.WriteLine($"DEBUG: QueryVM.SelectedEmployeeFromResults_set: New value ID: {value?.EmployeeID}");
                if (SetProperty(ref _selectedEmployeeFromResults, value) && _selectedEmployeeFromResults != null)
                {
                    DisplayEmployeeDetails(_selectedEmployeeFromResults);
                }
            }
        }

        public Employee DisplayedEmployee
        {
            get => _displayedEmployee;
            set
            {
                Debug.WriteLine($"DEBUG: QueryVM.DisplayedEmployee_set: New value ID: {value?.EmployeeID}");
                if (SetProperty(ref _displayedEmployee, value))
                {
                    IsEmployeeDetailsVisible = _displayedEmployee != null;
                    if (_displayedEmployee != null)
                    {
                        LoadEmployeePPE(_displayedEmployee.EmployeeID);
                    }
                    else
                    {
                        EmployeeActivePPE?.Clear();
                    }
                }
            }
        }

        public ObservableCollection<PPEAssignment> EmployeeActivePPE
        {
            get => _employeeActivePPE;
            set => SetProperty(ref _employeeActivePPE, value);
        }

        public PPEAssignment SelectedPpeAssignmentForEdit
        {
            get => _selectedPpeAssignmentForEdit;
            set
            {
                if (SetProperty(ref _selectedPpeAssignmentForEdit, value))
                {
                    if (EditPpeAssignmentCommand is RelayCommand cmd)
                    {
                        cmd.RaiseCanExecuteChanged();
                    }
                    Debug.WriteLine($"DEBUG: QueryVM.SelectedPpeAssignmentForEdit_set: New selection AssignmentID: {_selectedPpeAssignmentForEdit?.AssignmentID}");
                }
            }
        }

        public bool IsEmployeeDetailsVisible
        {
            get => _isEmployeeDetailsVisible;
            set => SetProperty(ref _isEmployeeDetailsVisible, value);
        }

        public bool IsSearchResultsVisible
        {
            get => _isSearchResultsVisible;
            set => SetProperty(ref _isSearchResultsVisible, value);
        }
        #endregion

        #region Commands
        public ICommand SearchCommand { get; }
        public ICommand EditPpeAssignmentCommand { get; }
        #endregion

        public QueryViewModel()
        {
            Debug.WriteLine("DEBUG: QueryViewModel_ctor: Constructor started.");
            SearchTypes = new ObservableCollection<string>
            {
                SearchTypeByIdConst,
                SearchByNameConst,
                SearchBySuitCodeConst
            };
            SelectedSearchType = SearchTypes.FirstOrDefault();
            Debug.WriteLine($"DEBUG: QueryViewModel_ctor: SearchTypes initialized. Count: {SearchTypes?.Count}. Items: {(SearchTypes != null ? string.Join(", ", SearchTypes) : "null")}. SelectedSearchType: {SelectedSearchType}");

            SearchResultsEmployees = new ObservableCollection<Employee>();
            EmployeeActivePPE = new ObservableCollection<PPEAssignment>();

            SearchCommand = new RelayCommand(ExecuteSearch, CanExecuteSearch);
            EditPpeAssignmentCommand = new RelayCommand(ExecuteEditPpeAssignment, CanExecuteEditPpeAssignment);
            Debug.WriteLine("DEBUG: QueryViewModel_ctor: Constructor and commands initialized.");
        }

        #region Command Execute Methods & Helpers
        private bool CanExecuteSearch(object parameter)
        {
            return !string.IsNullOrWhiteSpace(SearchTerm) && !string.IsNullOrWhiteSpace(SelectedSearchType);
        }

        private void ExecuteSearch(object parameter)
        {
            Debug.WriteLine($"DEBUG: QueryVM.ExecuteSearch: Started. Type: '{SelectedSearchType}', Term: '{SearchTerm}'");
            SearchResultsEmployees.Clear();
            DisplayedEmployee = null; // This will clear PPE and hide details via its setter
            IsSearchResultsVisible = false;

            if (string.IsNullOrWhiteSpace(SearchTerm))
            {
                MessageBox.Show("请输入搜索内容。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                Debug.WriteLine("DEBUG: QueryVM.ExecuteSearch: SearchTerm is empty.");
                return;
            }

            try
            {
                if (SelectedSearchType == SearchTypeByIdConst)
                {
                    SearchByEmployeeId();
                }
                else if (SelectedSearchType == SearchByNameConst)
                {
                    SearchByEmployeeName();
                }
                else if (SelectedSearchType == SearchBySuitCodeConst)
                {
                    SearchBySuitCode();
                }
                else
                {
                    MessageBox.Show("选择了无效的查询类型。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    Debug.WriteLine($"DEBUG: QueryVM.ExecuteSearch: Unknown search type: '{SelectedSearchType}'");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DEBUG: QueryVM.ExecuteSearch: EXCEPTION during search: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"搜索过程中发生错误: {ex.Message}", "搜索错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            Debug.WriteLine("DEBUG: QueryVM.ExecuteSearch: Method finished.");
        }

        private void SearchByEmployeeId()
        {
            var employee = SQLiteDataAccess.GetEmployeeById(SearchTerm);
            if (employee != null)
            {
                DisplayEmployeeDetails(employee);
            }
            else
            {
                MessageBox.Show($"未找到工号为 '{SearchTerm}' 的员工。", "查询结果", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void SearchByEmployeeName()
        {
            var employees = SQLiteDataAccess.SearchEmployeesByName(SearchTerm);
            if (employees == null || !employees.Any())
            {
                MessageBox.Show($"未找到姓名为 '{SearchTerm}' (或相似) 的员工。", "查询结果", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (employees.Count == 1)
            {
                DisplayEmployeeDetails(employees.First());
                return;
            }

            foreach (var emp in employees)
            {
                SearchResultsEmployees.Add(emp);
            }

            IsSearchResultsVisible = true;
            Debug.WriteLine($"DEBUG: QueryVM.ExecuteSearch: Multiple employees found by name. Count: {SearchResultsEmployees.Count}");
        }

        private void SearchBySuitCode()
        {
            var employee = SQLiteDataAccess.GetEmployeeByPPESpecificCode(SearchTerm, SuitPpeTypeForSearch);
            if (employee != null)
            {
                DisplayEmployeeDetails(employee);
            }
            else
            {
                MessageBox.Show($"未找到持有洁净服编码为 '{SearchTerm}' 的员工，或者该洁净服当前并非有效状态。", "查询结果", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DisplayEmployeeDetails(Employee employee)
        {
            Debug.WriteLine($"DEBUG: QueryVM.DisplayEmployeeDetails: Displaying details for EmployeeID: {employee?.EmployeeID}");
            DisplayedEmployee = employee; // This will trigger IsEmployeeDetailsVisible and LoadEmployeePPE via its setter

            IsSearchResultsVisible = false;
            SearchResultsEmployees.Clear();
        }

        private void LoadEmployeePPE(string employeeId)
        {
            if (string.IsNullOrWhiteSpace(employeeId))
            {
                EmployeeActivePPE?.Clear();
                Debug.WriteLine($"DEBUG: QueryVM.LoadEmployeePPE: employeeId is null/whitespace. Cleared PPE list.");
                return;
            }
            Debug.WriteLine($"DEBUG: QueryVM.LoadEmployeePPE: Loading PPE for EmployeeID: {employeeId}");

            var ppeList = new List<PPEAssignment>();
            try
            {
                ppeList = SQLiteDataAccess.GetPPEAssignmentsForEmployee(employeeId, activeOnly: true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DEBUG: QueryVM.LoadEmployeePPE: EXCEPTION during GetPPEAssignmentsForEmployee for EmployeeID {employeeId}: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"加载员工劳保用品列表时发生错误: {ex.Message}", "加载错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            if (ppeList == null)
            {
                Debug.WriteLine($"DEBUG: QueryVM.LoadEmployeePPE: SQLiteDataAccess.GetPPEAssignmentsForEmployee returned NULL for EmployeeID: {employeeId}. Initializing to empty collection.");
                EmployeeActivePPE = new ObservableCollection<PPEAssignment>();
            }
            else
            {
                EmployeeActivePPE = new ObservableCollection<PPEAssignment>(ppeList);
            }
            Debug.WriteLine($"DEBUG: QueryVM.LoadEmployeePPE: Loaded {EmployeeActivePPE?.Count ?? 0} active PPE assignments for EmployeeID: {employeeId}");
        }

        private bool CanExecuteEditPpeAssignment(object parameter)
        {
            bool canEdit = SelectedPpeAssignmentForEdit != null;
            // Debug.WriteLine($"DEBUG: QueryVM.CanExecuteEditPpeAssignment: SelectedPPE is null? {SelectedPpeAssignmentForEdit == null}. Result: {canEdit}");
            return canEdit;
        }

        private void ExecuteEditPpeAssignment(object parameter)
        {
            if (SelectedPpeAssignmentForEdit == null)
            {
                Debug.WriteLine("DEBUG: QueryVM.ExecuteEditPpeAssignment: No PPE Assignment selected for editing.");
                return;
            }

            Debug.WriteLine($"DEBUG: QueryVM.ExecuteEditPpeAssignment: Editing PPE AssignmentID: {SelectedPpeAssignmentForEdit.AssignmentID}, Type: {SelectedPpeAssignmentForEdit.PPE_Type}");

            var editDialogViewModel = new EditPpeAssignmentDialogViewModel(SelectedPpeAssignmentForEdit);
            var editDialog = new EditPpeAssignmentDialog
            {
                DataContext = editDialogViewModel,
                Owner = Application.Current.MainWindow
            };

            bool? dialogResult = editDialog.ShowDialog();

            if (dialogResult == true)
            {
                PPEAssignment updatedPpeAssignment = editDialogViewModel.CurrentPpeAssignment;
                Debug.WriteLine($"DEBUG: QueryVM.ExecuteEditPpeAssignment: Save confirmed from dialog. Attempting to update DB for AssignmentID: {updatedPpeAssignment.AssignmentID}");
                bool success = false;
                try
                {
                    success = SQLiteDataAccess.UpdatePPEAssignment(updatedPpeAssignment);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"DEBUG: QueryVM.ExecuteEditPpeAssignment: EXCEPTION during SQLiteDataAccess.UpdatePPEAssignment: {ex.Message}\n{ex.StackTrace}");
                    MessageBox.Show($"更新劳保用品记录时发生数据库错误: {ex.Message}", "数据库错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                if (success)
                {
                    Debug.WriteLine($"DEBUG: QueryVM.ExecuteEditPpeAssignment: DB Update successful for AssignmentID: {updatedPpeAssignment.AssignmentID}");
                    LoggingService.LogAction("劳保记录修改",
                        $"员工 {DisplayedEmployee?.Name} ({DisplayedEmployee?.EmployeeID}) 的劳保记录 (ID: {updatedPpeAssignment.AssignmentID}, " +
                        $"类型: {updatedPpeAssignment.PPE_Type}, 编码: {updatedPpeAssignment.ItemSpecificCode}) 已修改。");

                    MessageBox.Show("劳保用品记录更新成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);

                    if (DisplayedEmployee != null)
                    {
                        Debug.WriteLine($"DEBUG: QueryVM.ExecuteEditPpeAssignment: Refreshing PPE list for employee {DisplayedEmployee.EmployeeID}");
                        LoadEmployeePPE(DisplayedEmployee.EmployeeID);
                    }
                }
                else
                {
                    Debug.WriteLine($"DEBUG: QueryVM.ExecuteEditPpeAssignment: DB Update FAILED for AssignmentID: {updatedPpeAssignment.AssignmentID}");
                    // MessageBox.Show("更新劳保用品记录失败。请查看日志。", "错误", MessageBoxButton.OK, MessageBoxImage.Error); // 可选，因为底层可能已提示
                }
            }
            else
            {
                Debug.WriteLine($"DEBUG: QueryVM.ExecuteEditPpeAssignment: Edit dialog was cancelled or closed for AssignmentID: {SelectedPpeAssignmentForEdit.AssignmentID}");
            }
            SelectedPpeAssignmentForEdit = null;
        }
        #endregion
    }
}