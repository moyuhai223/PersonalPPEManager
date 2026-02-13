// App.xaml.cs
using PersonalPPEManager.DataAccess; // 确保引用了DataAccess命名空间
using System.Windows;

namespace PersonalPPEManager // 确保这个命名空间与你的项目匹配
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // 调用基类的 OnStartup 方法
            base.OnStartup(e);

            // 初始化数据库和表
            // 这应该在应用程序的任何其他数据库操作之前调用
            // 并且在创建主窗口（主窗口的ViewModel可能立即尝试加载数据）之前调用
            try
            {
                SQLiteDataAccess.InitializeDatabase();
                System.Diagnostics.Debug.WriteLine("DEBUG: App.OnStartup - Database initialized successfully.");
            }
            catch (System.Exception ex) // 捕获初始化数据库时可能发生的任何严重错误
            {
                System.Diagnostics.Debug.WriteLine($"DEBUG: App.OnStartup - CRITICAL ERROR during Database Initialization: {ex.Message}\n{ex.StackTrace}");
                // 在实际发布的应用中，这里可能需要一个更友好的错误提示，并可能决定是否终止应用
                MessageBox.Show($"应用程序启动失败：无法初始化数据库。\n错误: {ex.Message}\n\n请检查日志或联系支持。",
                                "启动错误", MessageBoxButton.OK, MessageBoxImage.Error);
                // 退出应用程序，因为数据库是核心依赖
                Current.Shutdown(-1);
                return;
            }

            // 创建并显示主窗口
            MainWindow mainWindow = new MainWindow();

            // 如果你的 MainWindow 有一个 MainViewModel，并且你想在这里设置它：
            // var mainViewModel = new ViewModels.MainViewModel(); // 假设你有一个MainViewModel
            // mainWindow.DataContext = mainViewModel;

            mainWindow.Show();
            System.Diagnostics.Debug.WriteLine("DEBUG: App.OnStartup - MainWindow shown.");
        }
    }
}