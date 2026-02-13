// ViewModels/OperationLogViewModel.cs
using PersonalPPEManager.Models;
using PersonalPPEManager.DataAccess;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Linq; // For FirstOrDefault, if needed for other things
using System.Diagnostics; // For Debug.WriteLine

namespace PersonalPPEManager.ViewModels
{
    public class OperationLogViewModel : BaseViewModel
    {
        private DateTime? _fromDate;
        public DateTime? FromDate
        {
            get => _fromDate;
            set => SetProperty(ref _fromDate, value);
        }

        private DateTime? _toDate;
        public DateTime? ToDate
        {
            get => _toDate;
            set => SetProperty(ref _toDate, value);
        }

        private string _operationTypeFilter;
        public string OperationTypeFilter
        {
            get => _operationTypeFilter;
            set => SetProperty(ref _operationTypeFilter, value);
        }

        private ObservableCollection<LogEntry> _logEntries;
        public ObservableCollection<LogEntry> LogEntries
        {
            get => _logEntries;
            set => SetProperty(ref _logEntries, value);
        }

        public ICommand LoadLogsCommand { get; }
        public ICommand ClearFiltersCommand { get; }

        public OperationLogViewModel()
        {
            Debug.WriteLine("DEBUG: OperationLogViewModel_ctor: Constructor started.");
            LogEntries = new ObservableCollection<LogEntry>();

            LoadLogsCommand = new RelayCommand(ExecuteLoadLogs);
            ClearFiltersCommand = new RelayCommand(ExecuteClearFilters);

            // 初始加载所有日志或最近的日志
            ExecuteLoadLogs(null);
            Debug.WriteLine("DEBUG: OperationLogViewModel_ctor: Initial logs loaded. Constructor finished.");
        }

        private void ExecuteLoadLogs(object parameter = null) // parameter is not used here but can be
        {
            Debug.WriteLine($"DEBUG: OperationLogVM.ExecuteLoadLogs: Loading logs. From: {FromDate}, To: {ToDate}, TypeFilter: '{OperationTypeFilter}'");
            try
            {
                // 如果 ToDate 被选中，确保它包含当天结束的时间
                DateTime? effectiveToDate = ToDate.HasValue ? ToDate.Value.Date.AddDays(1).AddTicks(-1) : (DateTime?)null;

                var logs = SQLiteDataAccess.GetLogEntries(FromDate, effectiveToDate, OperationTypeFilter);
                LogEntries = new ObservableCollection<LogEntry>(logs);
                Debug.WriteLine($"DEBUG: OperationLogVM.ExecuteLoadLogs: Loaded {LogEntries.Count} log entries.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DEBUG: OperationLogVM.ExecuteLoadLogs: EXCEPTION - {ex.Message}\n{ex.StackTrace}");
                // 可以考虑在UI上显示错误信息
                System.Windows.MessageBox.Show($"加载操作日志时发生错误: {ex.Message}", "加载错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                LogEntries.Clear(); // 出错时清空列表
            }
        }

        private void ExecuteClearFilters(object parameter)
        {
            Debug.WriteLine("DEBUG: OperationLogVM.ExecuteClearFilters: Clearing filters.");
            FromDate = null;
            ToDate = null;
            OperationTypeFilter = string.Empty;
            ExecuteLoadLogs(null); // 重新加载所有日志
            Debug.WriteLine("DEBUG: OperationLogVM.ExecuteClearFilters: Filters cleared and logs reloaded.");
        }
    }
}