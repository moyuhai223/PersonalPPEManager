// MainWindow.xaml.cs
using PersonalPPEManager.ViewModels; // 确保引用了ViewModels命名空间
using PersonalPPEManager.Views;     // 确保引用了Views命名空间
using System.Windows;
using System.Windows.Controls;      // 为了 Button 类型
using System.Diagnostics;           // 为了 Debug.WriteLine

namespace PersonalPPEManager // 确保这个命名空间与你的项目匹配
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        // 缓存UserControl实例以避免重复创建，提高切换效率
        private UserControl _employeeManagementView;
        private UserControl _ppeIssuanceView;
        private UserControl _queryView;
        private UserControl _dataUtilityView;
        private UserControl _operationLogView;
        private UserControl _settingsView;
        private UserControl _ppeMasterManagementView; // 新增：劳保用品主数据管理视图

        public MainWindow()
        {
            InitializeComponent();
            // 默认显示员工管理界面 (或你希望的任何其他默认视图)
            // 确保XAML中存在名为 MainContentArea 的 ContentControl
            if (MainContentArea != null)
            {
                ShowEmployeeManagementView();
            }
            else
            {
                Debug.WriteLine("CRITICAL DEBUG: MainWindow_ctor - MainContentArea (ContentControl) not found in XAML.");
            }
        }

        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button clickedButton)
            {
                switch (clickedButton.Name)
                {
                    case "BtnEmployeeManagement":
                        ShowEmployeeManagementView();
                        break;
                    case "BtnPPEIssuance":
                        ShowPPEIssuanceView();
                        break;
                    case "BtnQuery":
                        ShowQueryView();
                        break;
                    case "BtnDataUtility":
                        ShowDataUtilityView();
                        break;
                    case "BtnOperationLog":
                        ShowOperationLogView();
                        break;
                    case "BtnSettings":
                        ShowSettingsView();
                        break;
                    case "BtnPpeMasterManagement": // 新增：处理主数据管理按钮点击
                        ShowPpeMasterManagementView();
                        break;
                    default:
                        Debug.WriteLine($"DEBUG: MainWindow.NavButton_Click - Unknown button name: {clickedButton.Name}");
                        break;
                }
            }
        }

        private void ShowEmployeeManagementView()
        {
            if (_employeeManagementView == null)
            {
                _employeeManagementView = new EmployeeManagementView
                {
                    DataContext = new EmployeeManagementViewModel()
                };
                Debug.WriteLine("DEBUG: MainWindow - EmployeeManagementView and ViewModel created.");
            }
            MainContentArea.Content = _employeeManagementView;
            Debug.WriteLine("DEBUG: MainWindow - EmployeeManagementView displayed.");
        }

        private void ShowPPEIssuanceView()
        {
            if (_ppeIssuanceView == null)
            {
                _ppeIssuanceView = new PPEIssuanceView
                {
                    DataContext = new PPEIssuanceViewModel()
                };
                Debug.WriteLine("DEBUG: MainWindow - PPEIssuanceView and ViewModel created.");
            }
            MainContentArea.Content = _ppeIssuanceView;
            Debug.WriteLine("DEBUG: MainWindow - PPEIssuanceView displayed.");
        }

        private void ShowQueryView()
        {
            if (_queryView == null)
            {
                _queryView = new QueryView
                {
                    DataContext = new QueryViewModel()
                };
                Debug.WriteLine("DEBUG: MainWindow - QueryView and ViewModel created.");
            }
            MainContentArea.Content = _queryView;
            Debug.WriteLine("DEBUG: MainWindow - QueryView displayed.");
        }

        private void ShowDataUtilityView()
        {
            if (_dataUtilityView == null)
            {
                _dataUtilityView = new DataUtilityView
                {
                    DataContext = new DataUtilityViewModel()
                };
                Debug.WriteLine("DEBUG: MainWindow - DataUtilityView and ViewModel created.");
            }
            MainContentArea.Content = _dataUtilityView;
            Debug.WriteLine("DEBUG: MainWindow - DataUtilityView displayed.");
        }

        private void ShowOperationLogView()
        {
            if (_operationLogView == null)
            {
                _operationLogView = new OperationLogView
                {
                    DataContext = new OperationLogViewModel()
                };
                Debug.WriteLine("DEBUG: MainWindow - OperationLogView and ViewModel created.");
            }
            MainContentArea.Content = _operationLogView;
            Debug.WriteLine("DEBUG: MainWindow - OperationLogView displayed.");
        }

        private void ShowSettingsView()
        {
            if (_settingsView == null)
            {
                _settingsView = new SettingsView // 创建 SettingsView 实例
                {
                    // SettingsViewModel 的无参构造函数会获取 ConfigurationService.Instance
                    DataContext = new SettingsViewModel() // 创建并设置其 ViewModel
                };
                Debug.WriteLine("DEBUG: MainWindow - SettingsView and ViewModel created.");
            }
            MainContentArea.Content = _settingsView; // 将视图显示在主内容区
            Debug.WriteLine("DEBUG: MainWindow - SettingsView displayed.");
        }

        // 新增：显示劳保用品主数据管理视图的方法
        private void ShowPpeMasterManagementView()
        {
            if (_ppeMasterManagementView == null)
            {
                _ppeMasterManagementView = new PpeMasterManagementView
                {
                    DataContext = new PpeMasterManagementViewModel()
                };
                Debug.WriteLine("DEBUG: MainWindow - PpeMasterManagementView and ViewModel created.");
            }
            MainContentArea.Content = _ppeMasterManagementView;
            Debug.WriteLine("DEBUG: MainWindow - PpeMasterManagementView displayed.");
        }
    }
}