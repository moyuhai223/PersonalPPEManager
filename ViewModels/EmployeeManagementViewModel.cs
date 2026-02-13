// ViewModels/EmployeeManagementViewModel.cs
using PersonalPPEManager.Models;
using PersonalPPEManager.DataAccess;
using PersonalPPEManager.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Linq;
using System;
using System.Windows; // 确保这行存在，为 MessageBox 和 MessageBoxImage

namespace PersonalPPEManager.ViewModels
{
    public class EmployeeManagementViewModel : BaseViewModel
    {
        private ObservableCollection<Employee> _employees;
        public ObservableCollection<Employee> Employees
        {
            get => _employees;
            set => SetProperty(ref _employees, value);
        }

        private Employee _selectedEmployee;
        public Employee SelectedEmployee
        {
            get => _selectedEmployee;
            set
            {
                System.Diagnostics.Debug.WriteLine($"DEBUG: SelectedEmployee_set: New value ID: {value?.EmployeeID}, Old value ID: {_selectedEmployee?.EmployeeID}");
                if (SetProperty(ref _selectedEmployee, value) && _selectedEmployee != null)
                {
                    System.Diagnostics.Debug.WriteLine($"DEBUG: SelectedEmployee_set: Property changed and new value is not null. Populating EditingEmployee.");
                    EditingEmployee = new Employee // 创建副本进行编辑
                    {
                        EmployeeID = _selectedEmployee.EmployeeID,
                        Name = _selectedEmployee.Name,
                        EntryDate = _selectedEmployee.EntryDate, // <<--- 直接复制 DateTime?
                        Process = _selectedEmployee.Process,
                        Status = _selectedEmployee.Status,
                        LockerClothes1 = _selectedEmployee.LockerClothes1,
                        LockerShoes1 = _selectedEmployee.LockerShoes1,
                        LockerClothes2 = _selectedEmployee.LockerClothes2,
                        LockerShoes2 = _selectedEmployee.LockerShoes2,
                        Remarks = _selectedEmployee.Remarks // <<--- 新增此行
                    };
                    IsNewEmployeeMode = false;
                    System.Diagnostics.Debug.WriteLine($"DEBUG: SelectedEmployee_set: EditingEmployee populated. IsNewEmployeeMode set to false.");
                }
                else if (_selectedEmployee == null)
                {
                    System.Diagnostics.Debug.WriteLine($"DEBUG: SelectedEmployee_set: New value is null. IsNewEmployeeMode might remain true or be reset by PrepareNewEmployee if called.");
                }
            }
        }

        private Employee _editingEmployee;
        public Employee EditingEmployee
        {
            get => _editingEmployee;
            set => SetProperty(ref _editingEmployee, value);
        }

        private bool _isNewEmployeeMode;
        public bool IsNewEmployeeMode
        {
            get => _isNewEmployeeMode;
            set => SetProperty(ref _isNewEmployeeMode, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand AddNewCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ClearFormCommand { get; }

        public EmployeeManagementViewModel()
        {
            System.Diagnostics.Debug.WriteLine("DEBUG: EmployeeManagementViewModel_ctor: Constructor started.");
            EditingEmployee = new Employee();
            IsNewEmployeeMode = true;
            System.Diagnostics.Debug.WriteLine("DEBUG: EmployeeManagementViewModel_ctor: EditingEmployee initialized, IsNewEmployeeMode set to true.");

            LoadEmployees(); // LoadEmployees 现在内部有 try-catch 和 Debug.WriteLine

            RefreshCommand = new RelayCommand(param => LoadEmployees());
            AddNewCommand = new RelayCommand(ExecuteAddNew);
            SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
            DeleteCommand = new RelayCommand(ExecuteDelete, CanExecuteDelete);
            ClearFormCommand = new RelayCommand(param => PrepareNewEmployee());
            System.Diagnostics.Debug.WriteLine("DEBUG: EmployeeManagementViewModel_ctor: Commands initialized.");
            System.Diagnostics.Debug.WriteLine("DEBUG: EmployeeManagementViewModel_ctor: Constructor finished.");
        }

        private void LoadEmployees()
        {
            System.Diagnostics.Debug.WriteLine("DEBUG: LoadEmployees: Method started.");
            try
            {
                var employeesFromDb = SQLiteDataAccess.GetAllEmployees(); // GetAllEmployees 现在内部有 Debug.WriteLine
                Employees = new ObservableCollection<Employee>(employeesFromDb);
                System.Diagnostics.Debug.WriteLine($"DEBUG: LoadEmployees: Loaded {Employees?.Count ?? 0} employees successfully.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DEBUG: LoadEmployees: EXCEPTION - {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"加载员工列表时发生错误: {ex.Message}", "加载错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            System.Diagnostics.Debug.WriteLine("DEBUG: LoadEmployees: Method finished.");
        }

        private void PrepareNewEmployee()
        {
            System.Diagnostics.Debug.WriteLine("DEBUG: PrepareNewEmployee: Method started.");
            EditingEmployee = new Employee
            {
                Status = "在职", // 默认状态为“在职”
                Remarks = string.Empty, // 备注默认为空
                EntryDate = DateTime.Today // <<--- 新增或修改此行：将入职时间默认为当天日期
            };
            SelectedEmployee = null; // 取消 DataGrid 中的选择
            IsNewEmployeeMode = true; // 设置为新增模式
            System.Diagnostics.Debug.WriteLine($"DEBUG: PrepareNewEmployee: Form cleared, IsNewEmployeeMode set to true, EntryDate defaulted to {EditingEmployee.EntryDate:yyyy-MM-dd}.");
        }

        private void ExecuteAddNew(object parameter)
        {
            System.Diagnostics.Debug.WriteLine("DEBUG: ExecuteAddNew: Method started.");
            PrepareNewEmployee();
            System.Diagnostics.Debug.WriteLine("DEBUG: ExecuteAddNew: Method finished.");
        }

        private bool CanExecuteSave(object parameter)
        {
            bool canSave = EditingEmployee != null &&
                           !string.IsNullOrWhiteSpace(EditingEmployee.EmployeeID) &&
                           !string.IsNullOrWhiteSpace(EditingEmployee.Name);
            System.Diagnostics.Debug.WriteLine($"DEBUG: CanExecuteSave: Result = {canSave}. EmployeeID='{EditingEmployee?.EmployeeID}', Name='{EditingEmployee?.Name}'");
            return canSave;
        }

        private void ExecuteSave(object parameter)
        {
            System.Diagnostics.Debug.WriteLine("DEBUG: ExecuteSave: Method started.");

            if (EditingEmployee == null || string.IsNullOrWhiteSpace(EditingEmployee.EmployeeID) || string.IsNullOrWhiteSpace(EditingEmployee.Name))
            {
                System.Diagnostics.Debug.WriteLine("DEBUG: ExecuteSave: Validation failed - ID or Name is null/whitespace.");
                MessageBox.Show("工号和姓名不能为空！", "验证错误", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine("DEBUG: ExecuteSave: 'ID or Name empty' MessageBox shown.");
                return;
            }

            bool success = false;
            if (IsNewEmployeeMode)
            {
                System.Diagnostics.Debug.WriteLine("DEBUG: ExecuteSave: IsNewEmployeeMode is true (Add mode).");
                if (Employees != null && Employees.Any(emp => emp.EmployeeID.Equals(EditingEmployee.EmployeeID, StringComparison.OrdinalIgnoreCase)))
                {
                    System.Diagnostics.Debug.WriteLine($"DEBUG: ExecuteSave: Duplicate EmployeeID '{EditingEmployee.EmployeeID}' detected.");
                    MessageBox.Show("该工号已存在！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    System.Diagnostics.Debug.WriteLine("DEBUG: ExecuteSave: 'Duplicate ID' MessageBox shown.");
                    return;
                }
                System.Diagnostics.Debug.WriteLine($"DEBUG: ExecuteSave: Calling AddEmployee for ID: {EditingEmployee.EmployeeID}, Name: {EditingEmployee.Name}");
                success = SQLiteDataAccess.AddEmployee(EditingEmployee);
                System.Diagnostics.Debug.WriteLine($"DEBUG: ExecuteSave: AddEmployee returned: {success}");
                if (success)
                {
                    // 修改前: LoggingService.LogAction("员工添加", $"添加员工: {EditingEmployee.EmployeeID} - {EditingEmployee.Name}");
                    LoggingService.LogAction("员工添加", $"添加员工: {EditingEmployee.Name} ({EditingEmployee.EmployeeID})"); // <<--- 修改此处
                    System.Diagnostics.Debug.WriteLine("DEBUG: ExecuteSave: Logged '员工添加'.");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("DEBUG: ExecuteSave: IsNewEmployeeMode is false (Update mode).");
                System.Diagnostics.Debug.WriteLine($"DEBUG: ExecuteSave: Calling UpdateEmployee for ID: {EditingEmployee.EmployeeID}, Name: {EditingEmployee.Name}");
                success = SQLiteDataAccess.UpdateEmployee(EditingEmployee); // 假设 UpdateEmployee 也存在于 SQLiteDataAccess
                System.Diagnostics.Debug.WriteLine($"DEBUG: ExecuteSave: UpdateEmployee returned: {success}");
                if (success)
                {
                    // 修改前: LoggingService.LogAction("员工修改", $"修改员工信息: {EditingEmployee.EmployeeID} - {EditingEmployee.Name}");
                    LoggingService.LogAction("员工修改", $"修改员工信息: {EditingEmployee.Name} ({EditingEmployee.EmployeeID})"); // <<--- 修改此处
                    System.Diagnostics.Debug.WriteLine("DEBUG: ExecuteSave: Logged '员工修改'.");
                }
            }

            System.Diagnostics.Debug.WriteLine($"DEBUG: ExecuteSave: 'success' variable is: {success}");

            if (success)
            {
                System.Diagnostics.Debug.WriteLine("DEBUG: ExecuteSave: Entering success block. Calling LoadEmployees...");
                LoadEmployees();
                System.Diagnostics.Debug.WriteLine("DEBUG: ExecuteSave: LoadEmployees finished. Calling PrepareNewEmployee...");
                PrepareNewEmployee();
                System.Diagnostics.Debug.WriteLine("DEBUG: ExecuteSave: PrepareNewEmployee finished. About to show 'Save Successful' MessageBox.");
                MessageBox.Show("保存成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                System.Diagnostics.Debug.WriteLine("DEBUG: ExecuteSave: 'Save Successful' MessageBox shown.");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("DEBUG: ExecuteSave: Entering failure block (success is false). About to show 'Save Failed' MessageBox.");
                MessageBox.Show("保存失败！请检查数据或查看日志。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine("DEBUG: ExecuteSave: 'Save Failed' MessageBox shown.");
            }
            System.Diagnostics.Debug.WriteLine("DEBUG: ExecuteSave: Method finished.");
        }

        private bool CanExecuteDelete(object parameter)
        {
            bool canDelete = SelectedEmployee != null; // 修改：删除时不需要检查IsNewEmployeeMode，只要有选中即可
            System.Diagnostics.Debug.WriteLine($"DEBUG: CanExecuteDelete: Result = {canDelete}. SelectedEmployee is null? {SelectedEmployee == null}");
            return canDelete;
        }

        private void ExecuteDelete(object parameter)
        {
            System.Diagnostics.Debug.WriteLine($"DEBUG: ExecuteDelete: Method started. SelectedEmployee ID: {SelectedEmployee?.EmployeeID}");
            if (SelectedEmployee != null)
            {
                if (MessageBox.Show($"确定要删除员工: {SelectedEmployee.Name} (工号: {SelectedEmployee.EmployeeID})吗？\n这将同时删除该员工的所有劳保记录！",
                                                  "确认删除",
                                                  MessageBoxButton.YesNo,
                                                  MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    System.Diagnostics.Debug.WriteLine($"DEBUG: ExecuteDelete: User confirmed deletion for {SelectedEmployee.EmployeeID}. Calling DeleteEmployee.");
                    bool success = SQLiteDataAccess.DeleteEmployee(SelectedEmployee.EmployeeID);
                    System.Diagnostics.Debug.WriteLine($"DEBUG: ExecuteDelete: DeleteEmployee returned {success} for {SelectedEmployee.EmployeeID}.");
                    if (success)
                    {
                        // 修改前: LoggingService.LogAction("员工删除", $"删除员工: {SelectedEmployee.EmployeeID} - {SelectedEmployee.Name}");
                        LoggingService.LogAction("员工删除", $"删除员工: {SelectedEmployee.Name} ({SelectedEmployee.EmployeeID})"); // <<--- 修改此处
                        System.Diagnostics.Debug.WriteLine("DEBUG: ExecuteDelete: Logged '员工删除'.");
                        LoadEmployees();
                        PrepareNewEmployee();
                        System.Diagnostics.Debug.WriteLine("DEBUG: ExecuteDelete: Delete successful. Employees reloaded, form prepared for new.");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"DEBUG: ExecuteDelete: DeleteEmployee failed for {SelectedEmployee.EmployeeID}. Showing error message.");
                        MessageBox.Show("删除失败！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        System.Diagnostics.Debug.WriteLine("DEBUG: ExecuteDelete: 'Delete Failed' MessageBox shown.");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"DEBUG: ExecuteDelete: User cancelled deletion for {SelectedEmployee.EmployeeID}.");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("DEBUG: ExecuteDelete: No employee selected to delete.");
            }
            System.Diagnostics.Debug.WriteLine("DEBUG: ExecuteDelete: Method finished.");
        }
    }
}