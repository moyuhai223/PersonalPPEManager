// ViewModels/DataUtilityViewModel.cs
using PersonalPPEManager.Models;
using PersonalPPEManager.DataAccess;
using PersonalPPEManager.Services;
using Microsoft.Win32; // For OpenFileDialog and SaveFileDialog
using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.IO; // For Path, File, FileInfo
using System.Linq; // For Any, FirstOrDefault, Take
using System.Threading.Tasks; // For Task
using System.Windows; // For MessageBox, MessageBoxButton, MessageBoxImage
using System.Data.SQLite; // For SQLiteConnection.ClearAllPools()

namespace PersonalPPEManager.ViewModels
{
    public class DataUtilityViewModel : BaseViewModel
    {
        private readonly CsvService _csvService;
        private static readonly string DbFileName = "ppe_database.sqlite"; // 数据库文件名

        #region Properties
        private string _selectedCsvFilePath;
        public string SelectedCsvFilePath
        {
            get => _selectedCsvFilePath;
            set
            {
                if (SetProperty(ref _selectedCsvFilePath, value))
                {
                    if (StartImportCommand is RelayCommand command) command.RaiseCanExecuteChanged();
                    ImportStatusMessage = string.Empty;
                    System.Diagnostics.Debug.WriteLine($"DEBUG: DataUtilityVM.SelectedCsvFilePath_set: Path set to '{_selectedCsvFilePath}'");
                }
            }
        }

        private string _importStatusMessage;
        public string ImportStatusMessage
        {
            get => _importStatusMessage;
            set => SetProperty(ref _importStatusMessage, value);
        }

        private string _backupStatusMessage;
        public string BackupStatusMessage
        {
            get => _backupStatusMessage;
            set => SetProperty(ref _backupStatusMessage, value);
        }

        private string _restoreStatusMessage;
        public string RestoreStatusMessage
        {
            get => _restoreStatusMessage;
            set => SetProperty(ref _restoreStatusMessage, value);
        }
        private string _exportStatusMessage;
        public string ExportStatusMessage
        {
            get => _exportStatusMessage;
            set => SetProperty(ref _exportStatusMessage, value);
        }
        private bool _isProcessingFile; // 用于控制导入、备份和恢复按钮的可用性
        public bool IsProcessingFile
        {
            get => _isProcessingFile;
            set
            {
                if (SetProperty(ref _isProcessingFile, value))
                {
                    if (SelectCsvFileCommand is RelayCommand selectCmd) selectCmd.RaiseCanExecuteChanged();
                    if (StartImportCommand is RelayCommand startCmd) startCmd.RaiseCanExecuteChanged();
                    if (BackupDatabaseCommand is RelayCommand backupCmd) backupCmd.RaiseCanExecuteChanged();
                    if (RestoreDatabaseCommand is RelayCommand restoreCmd) restoreCmd.RaiseCanExecuteChanged();
                    System.Diagnostics.Debug.WriteLine($"DEBUG: DataUtilityVM.IsProcessingFile_set: Value set to '{_isProcessingFile}'");
                }
            }
        }
        #endregion

        #region Commands
        public ICommand SelectCsvFileCommand { get; }
        public ICommand StartImportCommand { get; }
        public ICommand BackupDatabaseCommand { get; }
        public ICommand RestoreDatabaseCommand { get; }
        public ICommand ExportEmployeesCsvCommand { get; } // <<--- 新增导出命令
        #endregion

        public DataUtilityViewModel()
        {
            System.Diagnostics.Debug.WriteLine("DEBUG: DataUtilityVM_ctor: Constructor started.");
            _csvService = new CsvService();

            SelectCsvFileCommand = new RelayCommand(ExecuteSelectCsvFile, _ => !IsProcessingFile);
            StartImportCommand = new RelayCommand(ExecuteStartImport,
                _ => !string.IsNullOrWhiteSpace(SelectedCsvFilePath) && !IsProcessingFile);
            BackupDatabaseCommand = new RelayCommand(ExecuteBackupDatabase, _ => !IsProcessingFile);
            RestoreDatabaseCommand = new RelayCommand(ExecuteRestoreDatabase, _ => !IsProcessingFile);
            ExportEmployeesCsvCommand = new RelayCommand(ExecuteExportEmployeesCsv, _ => !IsProcessingFile);

            ImportStatusMessage = "请选择一个CSV文件开始导入员工信息。";
            BackupStatusMessage = "点击按钮选择备份位置并开始备份数据库。";
            RestoreStatusMessage = "警告：恢复操作将覆盖当前数据！";
            ExportStatusMessage = "点击按钮选择导出位置并开始导出所有员工信息。";
            System.Diagnostics.Debug.WriteLine("DEBUG: DataUtilityVM_ctor: Constructor finished.");
        }

        #region Command Execute Methods
        private void ExecuteSelectCsvFile(object parameter)
        {
            System.Diagnostics.Debug.WriteLine("DEBUG: DataUtilityVM.ExecuteSelectCsvFile: Method started.");
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "CSV 文件 (*.csv)|*.csv|所有文件 (*.*)|*.*",
                Title = "选择员工信息CSV文件",
                CheckFileExists = true,
                CheckPathExists = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                SelectedCsvFilePath = openFileDialog.FileName;
                // ImportStatusMessage 更新和 RaiseCanExecuteChanged 已经移到 SelectedCsvFilePath 的 setter 中
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("DEBUG: DataUtilityVM.ExecuteSelectCsvFile: No file selected.");
            }
            System.Diagnostics.Debug.WriteLine("DEBUG: DataUtilityVM.ExecuteSelectCsvFile: Method finished.");
        }

        private async void ExecuteStartImport(object parameter)
        {
            if (IsProcessingFile || string.IsNullOrWhiteSpace(SelectedCsvFilePath)) return;

            IsProcessingFile = true;
            System.Diagnostics.Debug.WriteLine($"DEBUG: DataUtilityVM.ExecuteStartImport: Import process started for file: {SelectedCsvFilePath}");
            ImportStatusMessage = "正在解析CSV文件，请稍候...";

            CsvParseResult parseResult = null;
            string filePathCopy = SelectedCsvFilePath; // 避免在 Task.Run 中访问可能被UI线程修改的属性

            try
            {
                parseResult = await Task.Run(() => _csvService.ParseEmployeeCsv(filePathCopy));
                System.Diagnostics.Debug.WriteLine($"DEBUG: DataUtilityVM.ExecuteStartImport (Task.Run-Parse): Parsing finished. Found {parseResult.Employees?.Count ?? 0} employees. Errors/Warnings: {parseResult.ErrorMessages.Count}");

                if (parseResult.HasFatalErrors || parseResult.Employees == null || !parseResult.Employees.Any())
                {
                    string errorMsg = "CSV文件解析失败或没有找到有效的员工数据可供导入。\n";
                    if (parseResult.ErrorMessages.Any())
                    {
                        errorMsg += "详细信息:\n" + string.Join("\n", parseResult.ErrorMessages.Take(5));
                    }
                    ImportStatusMessage = errorMsg;
                    System.Diagnostics.Debug.WriteLine("DEBUG: DataUtilityVM.ExecuteStartImport: No valid employees found in CSV or fatal parsing error.");
                    LoggingService.LogAction("员工CSV导入", $"文件 '{Path.GetFileName(filePathCopy)}' 未导入任何数据 (无有效记录或解析错误)。");
                    IsProcessingFile = false;
                    return;
                }
                ImportStatusMessage = $"已解析 {parseResult.Employees.Count} 条记录，准备导入数据库...";
            }
            catch (Exception ex)
            {
                ImportStatusMessage = $"解析CSV文件时发生严重错误: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"DEBUG: DataUtilityVM.ExecuteStartImport: EXCEPTION during CSV parsing Task: {ex.Message}\n{ex.StackTrace}");
                LoggingService.LogAction("员工CSV导入失败", $"文件 '{Path.GetFileName(filePathCopy)}' 解析失败: {ex.Message}");
                IsProcessingFile = false;
                return;
            }

            ImportStatusMessage = $"正在向数据库导入 {parseResult.Employees.Count} 条记录...";
            int initialDbCount = 0;
            int finalDbCount = 0;
            List<Employee> employeesToImportCopy = new List<Employee>(parseResult.Employees);

            try
            {
                (initialDbCount, finalDbCount) = await Task.Run(() => {
                    int initCount = SQLiteDataAccess.GetAllEmployees().Count;
                    SQLiteDataAccess.ImportEmployees(employeesToImportCopy);
                    int finCount = SQLiteDataAccess.GetAllEmployees().Count;
                    return (initCount, finCount);
                });
                System.Diagnostics.Debug.WriteLine($"DEBUG: DataUtilityVM.ExecuteStartImport (Task.Run-DB): DB import operations finished. Initial count: {initialDbCount}, Final count: {finalDbCount}");
            }
            catch (Exception ex)
            {
                ImportStatusMessage = $"向数据库导入员工时发生严重错误: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"DEBUG: DataUtilityVM.ExecuteStartImport: EXCEPTION during DB import Task: {ex.Message}\n{ex.StackTrace}");
                LoggingService.LogAction("员工CSV导入失败", $"文件 '{Path.GetFileName(filePathCopy)}' 数据库导入失败: {ex.Message}");
                IsProcessingFile = false;
                return;
            }

            int actuallyAddedCount = finalDbCount - initialDbCount;
            string summaryMessage = $"导入操作完成。\n从文件 '{Path.GetFileName(filePathCopy)}' 共解析到 {parseResult.Employees.Count} 条记录。\n";
            summaryMessage += $"数据库中新增了: {actuallyAddedCount} 条记录。\n";
            int skippedInDb = parseResult.Employees.Count - actuallyAddedCount;
            if (skippedInDb > 0)
            {
                summaryMessage += $"{skippedInDb} 条记录可能因工号重复而已存在于数据库中，或在导入数据库时因其他原因未计入新增。\n";
            }
            if (parseResult.ErrorMessages.Any())
            {
                summaryMessage += $"此外，CSV文件解析过程中有 {parseResult.ErrorMessages.Count} 条警告/错误信息。\n（仅显示前几条）例如: {string.Join("; ", parseResult.ErrorMessages.Take(3))}";
            }

            ImportStatusMessage = summaryMessage;
            LoggingService.LogAction("员工CSV导入完成", summaryMessage.Replace("\n", " ").Replace("\r", " "));

            IsProcessingFile = false;
            MessageBox.Show("员工导入操作已完成。\n" + summaryMessage + "\n\n如果当前已打开员工管理界面，你可能需要手动点击“刷新列表”以查看最新数据。",
                            "导入完成", MessageBoxButton.OK, MessageBoxImage.Information);
            System.Diagnostics.Debug.WriteLine("DEBUG: DataUtilityVM.ExecuteStartImport: Import process finished.");
        }

        private async void ExecuteBackupDatabase(object parameter)
        {
            if (IsProcessingFile) return;
            IsProcessingFile = true;
            System.Diagnostics.Debug.WriteLine("DEBUG: DataUtilityVM.ExecuteBackupDatabase: Method started.");
            BackupStatusMessage = "准备备份...";

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "SQLite 数据库备份 (*.sqlite)|*.sqlite|所有文件 (*.*)|*.*",
                Title = "选择数据库备份保存位置",
                FileName = $"ppe_database_backup_{DateTime.Now:yyyyMMdd_HHmmss}.sqlite"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                string backupFilePath = saveFileDialog.FileName;
                BackupStatusMessage = $"正在备份数据库到: {Path.GetFileName(backupFilePath)} ...";
                System.Diagnostics.Debug.WriteLine($"DEBUG: DataUtilityVM.ExecuteBackupDatabase: Backup destination selected: {backupFilePath}");
                string sourceDbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DbFileName);

                try
                {
                    if (!File.Exists(sourceDbPath))
                    {
                        BackupStatusMessage = "错误：源数据库文件未找到！无法备份。";
                        System.Diagnostics.Debug.WriteLine($"DEBUG: DataUtilityVM.ExecuteBackupDatabase: Source DB file not found at '{sourceDbPath}'");
                        IsProcessingFile = false;
                        return;
                    }
                    await Task.Run(() => File.Copy(sourceDbPath, backupFilePath, true));

                    BackupStatusMessage = $"数据库成功备份到: {Path.GetFileName(backupFilePath)}";
                    System.Diagnostics.Debug.WriteLine($"DEBUG: DataUtilityVM.ExecuteBackupDatabase: Database backup successful to '{backupFilePath}'.");
                    LoggingService.LogAction("数据库备份", $"数据库成功备份到: {backupFilePath}");
                    MessageBox.Show($"数据库成功备份到:\n{backupFilePath}", "备份完成", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    BackupStatusMessage = $"备份数据库时发生错误: {ex.Message}";
                    System.Diagnostics.Debug.WriteLine($"DEBUG: DataUtilityVM.ExecuteBackupDatabase: EXCEPTION during backup: {ex.Message}\n{ex.StackTrace}");
                    LoggingService.LogAction("数据库备份失败", $"尝试备份到 '{backupFilePath}' 时失败: {ex.Message}");
                    MessageBox.Show($"数据库备份失败: {ex.Message}", "备份错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                BackupStatusMessage = "数据库备份操作已取消。";
                System.Diagnostics.Debug.WriteLine("DEBUG: DataUtilityVM.ExecuteBackupDatabase: Backup cancelled by user.");
            }

            IsProcessingFile = false;
            System.Diagnostics.Debug.WriteLine("DEBUG: DataUtilityVM.ExecuteBackupDatabase: Method finished.");
        }

        private async void ExecuteRestoreDatabase(object parameter)
        {
            if (IsProcessingFile) return;
            IsProcessingFile = true;
            System.Diagnostics.Debug.WriteLine("DEBUG: DataUtilityVM.ExecuteRestoreDatabase: Method started.");
            RestoreStatusMessage = "准备恢复数据...";

            MessageBoxResult confirmation = MessageBox.Show(
                "警告：此操作将使用所选备份文件完全覆盖当前所有数据，且此操作无法撤销！\n\n强烈建议在继续之前已备份当前数据。\n\n确定要继续恢复吗？",
                "恢复数据确认", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirmation != MessageBoxResult.Yes)
            {
                RestoreStatusMessage = "数据恢复操作已取消。";
                System.Diagnostics.Debug.WriteLine("DEBUG: DataUtilityVM.ExecuteRestoreDatabase: Restore cancelled by user confirmation.");
                IsProcessingFile = false;
                return;
            }

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "SQLite 数据库备份 (*.sqlite)|*.sqlite|所有文件 (*.*)|*.*",
                Title = "选择要恢复的数据库备份文件",
                CheckFileExists = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string backupFilePath = openFileDialog.FileName;
                RestoreStatusMessage = $"准备从 '{Path.GetFileName(backupFilePath)}' 恢复数据...";
                System.Diagnostics.Debug.WriteLine($"DEBUG: DataUtilityVM.ExecuteRestoreDatabase: Backup file selected: {backupFilePath}");
                string currentDbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DbFileName);

                try
                {
                    System.Diagnostics.Debug.WriteLine("DEBUG: DataUtilityVM.ExecuteRestoreDatabase: Attempting to clear all SQLite connection pools.");
                    SQLiteConnection.ClearAllPools();
                    System.Diagnostics.Debug.WriteLine("DEBUG: DataUtilityVM.ExecuteRestoreDatabase: SQLite connection pools cleared.");
                    await Task.Delay(500);

                    await Task.Run(() => File.Copy(backupFilePath, currentDbPath, true));

                    RestoreStatusMessage = $"数据已成功从 '{Path.GetFileName(backupFilePath)}' 恢复。\n请重新启动应用程序以加载新数据！";
                    System.Diagnostics.Debug.WriteLine($"DEBUG: DataUtilityVM.ExecuteRestoreDatabase: Database restore successful from '{backupFilePath}' to '{currentDbPath}'.");
                    LoggingService.LogAction("数据库恢复", $"数据成功从 '{Path.GetFileName(backupFilePath)}' 恢复。");
                    MessageBox.Show(RestoreStatusMessage, "恢复完成", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (IOException ioEx)
                {
                    RestoreStatusMessage = $"恢复数据库时发生文件操作错误: {ioEx.Message}\n可能是文件被占用或权限不足。请尝试关闭程序后手动替换文件，或确保程序有足够权限。";
                    System.Diagnostics.Debug.WriteLine($"DEBUG: DataUtilityVM.ExecuteRestoreDatabase: IOException during restore: {ioEx.Message}\n{ioEx.StackTrace}");
                    LoggingService.LogAction("数据库恢复失败", $"尝试从 '{Path.GetFileName(backupFilePath)}' 恢复时失败: {ioEx.Message}");
                    MessageBox.Show(RestoreStatusMessage, "恢复错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    RestoreStatusMessage = $"恢复数据库时发生未知错误: {ex.Message}";
                    System.Diagnostics.Debug.WriteLine($"DEBUG: DataUtilityVM.ExecuteRestoreDatabase: EXCEPTION during restore: {ex.Message}\n{ex.StackTrace}");
                    LoggingService.LogAction("数据库恢复失败", $"尝试从 '{Path.GetFileName(backupFilePath)}' 恢复时失败: {ex.Message}");
                    MessageBox.Show(RestoreStatusMessage, "恢复错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                RestoreStatusMessage = "数据恢复操作已取消（未选择备份文件）。";
                System.Diagnostics.Debug.WriteLine("DEBUG: DataUtilityVM.ExecuteRestoreDatabase: Restore cancelled by user (no file selected).");
            }

            IsProcessingFile = false;
            System.Diagnostics.Debug.WriteLine("DEBUG: DataUtilityVM.ExecuteRestoreDatabase: Method finished.");
        }
        private async void ExecuteExportEmployeesCsv(object parameter)
        {
            if (IsProcessingFile) return;
            IsProcessingFile = true;
            System.Diagnostics.Debug.WriteLine("DEBUG: DataUtilityVM.ExecuteExportEmployeesCsv: Method started.");
            ExportStatusMessage = "准备导出员工数据...";

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV 文件 (*.csv)|*.csv|所有文件 (*.*)|*.*",
                Title = "选择员工信息CSV导出位置",
                FileName = $"employees_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv" // 建议的导出文件名
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                string exportFilePath = saveFileDialog.FileName;
                ExportStatusMessage = $"正在导出员工数据到: {Path.GetFileName(exportFilePath)} ...";
                System.Diagnostics.Debug.WriteLine($"DEBUG: DataUtilityVM.ExecuteExportEmployeesCsv: Export destination selected: {exportFilePath}");

                try
                {
                    // 1. 从数据库获取所有员工数据
                    List<Employee> allEmployees = null;
                    await Task.Run(() => // 获取数据也可能耗时，放到Task.Run
                    {
                        allEmployees = SQLiteDataAccess.GetAllEmployees();
                    });

                    System.Diagnostics.Debug.WriteLine($"DEBUG: DataUtilityVM.ExecuteExportEmployeesCsv: Fetched {allEmployees?.Count ?? 0} employees from database.");

                    if (allEmployees == null || !allEmployees.Any())
                    {
                        ExportStatusMessage = "数据库中没有员工数据可供导出。";
                        System.Diagnostics.Debug.WriteLine("DEBUG: DataUtilityVM.ExecuteExportEmployeesCsv: No employees to export.");
                        IsProcessingFile = false;
                        return;
                    }

                    // 2. 调用 CsvService 将数据写入文件 (也放到Task.Run)
                    bool success = false;
                    string csvWriteError = null;
                    await Task.Run(() =>
                    {
                        success = _csvService.WriteEmployeesToCsv(allEmployees, exportFilePath, out csvWriteError);
                    });

                    if (success)
                    {
                        ExportStatusMessage = $"成功将 {allEmployees.Count} 条员工数据导出到: {Path.GetFileName(exportFilePath)}";
                        System.Diagnostics.Debug.WriteLine($"DEBUG: DataUtilityVM.ExecuteExportEmployeesCsv: Employee data export successful to '{exportFilePath}'.");
                        LoggingService.LogAction("员工数据导出", $"成功导出 {allEmployees.Count} 条员工数据到: {Path.GetFileName(exportFilePath)}");
                        MessageBox.Show($"员工数据成功导出到:\n{exportFilePath}", "导出完成", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        ExportStatusMessage = $"导出员工数据失败: {(string.IsNullOrEmpty(csvWriteError) ? "未知错误。" : csvWriteError)}";
                        System.Diagnostics.Debug.WriteLine($"DEBUG: DataUtilityVM.ExecuteExportEmployeesCsv: Failed to write CSV. Error: {csvWriteError}");
                        LoggingService.LogAction("员工数据导出失败", $"尝试导出到 '{Path.GetFileName(exportFilePath)}' 时失败: {csvWriteError}");
                        MessageBox.Show(ExportStatusMessage, "导出错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex) // 捕获获取员工数据或调用CsvService期间的意外错误
                {
                    ExportStatusMessage = $"导出员工数据时发生意外错误: {ex.Message}";
                    System.Diagnostics.Debug.WriteLine($"DEBUG: DataUtilityVM.ExecuteExportEmployeesCsv: EXCEPTION during export: {ex.Message}\n{ex.StackTrace}");
                    LoggingService.LogAction("员工数据导出失败", $"尝试导出到 '{Path.GetFileName(exportFilePath)}' 时发生意外错误: {ex.Message}");
                    MessageBox.Show(ExportStatusMessage, "导出错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                ExportStatusMessage = "员工数据导出操作已取消。";
                System.Diagnostics.Debug.WriteLine("DEBUG: DataUtilityVM.ExecuteExportEmployeesCsv: Export cancelled by user.");
            }

            IsProcessingFile = false;
            System.Diagnostics.Debug.WriteLine("DEBUG: DataUtilityVM.ExecuteExportEmployeesCsv: Method finished.");
        }
    }
    #endregion
}
