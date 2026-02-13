// ViewModels/SettingsViewModel.cs
using PersonalPPEManager.Services; // 为了 ConfigurationService
using System.Windows.Input;
using System.Windows; // 为了 MessageBox
using System.Diagnostics; // 为了 Debug.WriteLine

namespace PersonalPPEManager.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private readonly ConfigurationService _configService;

        #region Backing Fields for UI-Bound Properties
        private int _maxActiveSuits;
        private int _maxActiveHats;
        private int _maxActiveSafetyShoes;
        private int _maxActiveCanvasShoes;
        #endregion

        #region Public Properties for UI Binding
        public int MaxActiveSuits
        {
            get => _maxActiveSuits;
            set
            {
                if (value >= 0) // 基本校验：不允许负数
                    SetProperty(ref _maxActiveSuits, value);
                else
                    MessageBox.Show("数量不能为负数。", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public int MaxActiveHats
        {
            get => _maxActiveHats;
            set
            {
                if (value >= 0)
                    SetProperty(ref _maxActiveHats, value);
                else
                    MessageBox.Show("数量不能为负数。", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public int MaxActiveSafetyShoes
        {
            get => _maxActiveSafetyShoes;
            set
            {
                if (value >= 0)
                    SetProperty(ref _maxActiveSafetyShoes, value);
                else
                    MessageBox.Show("数量不能为负数。", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public int MaxActiveCanvasShoes
        {
            get => _maxActiveCanvasShoes;
            set
            {
                if (value >= 0)
                    SetProperty(ref _maxActiveCanvasShoes, value);
                else
                    MessageBox.Show("数量不能为负数。", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        #endregion

        #region Commands
        public ICommand SaveSettingsCommand { get; }
        public ICommand RestoreDefaultsCommand { get; }
        public ICommand LoadCurrentSettingsCommand { get; }
        #endregion

        public SettingsViewModel()
        {
            Debug.WriteLine("DEBUG: SettingsViewModel_ctor: Constructor started.");
            _configService = ConfigurationService.Instance; // 使用单例

            LoadSettingsFromService(); // 从 ConfigurationService 加载当前值到VM属性

            SaveSettingsCommand = new RelayCommand(ExecuteSaveSettings, CanExecuteSaveSettings);
            RestoreDefaultsCommand = new RelayCommand(ExecuteRestoreDefaults);
            LoadCurrentSettingsCommand = new RelayCommand(param => LoadSettingsFromService()); // 用于从UI刷新
            Debug.WriteLine("DEBUG: SettingsViewModel_ctor: Constructor finished.");
        }

        /// <summary>
        /// 从 ConfigurationService 的单例实例加载设置到 ViewModel 的属性中，以供UI绑定。
        /// </summary>
        private void LoadSettingsFromService()
        {
            Debug.WriteLine("DEBUG: SettingsViewModel.LoadSettingsFromService: Loading settings from ConfigurationService instance.");

            // 可选：如果希望每次“加载”都强制从数据库刷新 ConfigurationService 实例的状态。
            // _configService.LoadAllSettingsFromDb(); 
            // 一般情况下，ConfigurationService 在应用启动时加载一次，之后由 SettingsViewModel 保存时更新。
            // 如果其他地方也可能修改数据库中的配置（不太可能在这个应用中），则上面的行是必要的。

            MaxActiveSuits = _configService.MaxActiveSuits;
            MaxActiveHats = _configService.MaxActiveHats;
            MaxActiveSafetyShoes = _configService.MaxActiveSafetyShoes;
            MaxActiveCanvasShoes = _configService.MaxActiveCanvasShoes;
            Debug.WriteLine($"DEBUG: SettingsViewModel.LoadSettingsFromService: ViewModel properties updated. " +
                              $"Suits={MaxActiveSuits}, Hats={MaxActiveHats}, " +
                              $"SafetyShoes={MaxActiveSafetyShoes}, CanvasShoes={MaxActiveCanvasShoes}");
        }

        private bool CanExecuteSaveSettings(object parameter)
        {
            // 确保所有值都是非负的（虽然setter已经做了基本校验）
            bool isValid = MaxActiveSuits >= 0 && MaxActiveHats >= 0 &&
                           MaxActiveSafetyShoes >= 0 && MaxActiveCanvasShoes >= 0;

            // 检查是否有更改，只有在值发生变化时才允许保存 (可选)
            bool hasChanges = MaxActiveSuits != _configService.MaxActiveSuits ||
                              MaxActiveHats != _configService.MaxActiveHats ||
                              MaxActiveSafetyShoes != _configService.MaxActiveSafetyShoes ||
                              MaxActiveCanvasShoes != _configService.MaxActiveCanvasShoes;

            // Debug.WriteLine($"DEBUG: SettingsViewModel.CanExecuteSaveSettings: IsValid={isValid}, HasChanges={hasChanges}");
            return isValid; // 暂时不要求必须有更改才允许保存，让用户可以主动保存当前值（即使未变）
        }

        private void ExecuteSaveSettings(object parameter)
        {
            Debug.WriteLine("DEBUG: SettingsViewModel.ExecuteSaveSettings: Attempting to save settings.");
            if (!CanExecuteSaveSettings(null)) // 再次校验
            {
                MessageBox.Show("设置值无效，无法保存。", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                Debug.WriteLine("DEBUG: SettingsViewModel.ExecuteSaveSettings: Save aborted due to invalid settings.");
                return;
            }

            // 1. 将ViewModel中的值更新到ConfigurationService的单例实例中
            _configService.MaxActiveSuits = this.MaxActiveSuits;
            _configService.MaxActiveHats = this.MaxActiveHats;
            _configService.MaxActiveSafetyShoes = this.MaxActiveSafetyShoes;
            _configService.MaxActiveCanvasShoes = this.MaxActiveCanvasShoes;
            Debug.WriteLine("DEBUG: SettingsViewModel.ExecuteSaveSettings: ConfigurationService instance properties updated from ViewModel.");

            // 2. 调用ConfigurationService的方法将这些值保存到数据库
            if (_configService.SaveAllSettingsToDb())
            {
                MessageBox.Show("设置已成功保存！\n部分设置（如最大持有数量）的更改将在下次相关操作（如劳保发放）时生效，或在重新加载模块/重启应用后完全生效。", "保存成功", MessageBoxButton.OK, MessageBoxImage.Information);
                Debug.WriteLine("DEBUG: SettingsViewModel.ExecuteSaveSettings: Settings saved successfully to DB via service.");
                LoggingService.LogAction("系统设置修改",
                    $"最大洁净服数:{MaxActiveSuits}, 最大帽子数:{MaxActiveHats}, " +
                    $"最大劳保鞋数:{MaxActiveSafetyShoes}, 最大帆布鞋数:{MaxActiveCanvasShoes}");
            }
            else
            {
                MessageBox.Show("保存设置到数据库失败。请查看调试日志获取更多信息。", "保存失败", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine("DEBUG: SettingsViewModel.ExecuteSaveSettings: Failed to save settings to DB via service.");
            }
            // 保存后，ViewModel 中的值与 Service 中的值应该是一致的，不需要再次 LoadSettingsFromService
            // 但如果 CanExecuteSaveSettings 依赖于比较 ViewModel 和 Service 的值，则可能需要更新 CanExecute 状态
            if (SaveSettingsCommand is RelayCommand cmd) cmd.RaiseCanExecuteChanged();
        }

        private void ExecuteRestoreDefaults(object parameter)
        {
            Debug.WriteLine("DEBUG: SettingsViewModel.ExecuteRestoreDefaults: Restoring default settings to ViewModel.");
            if (MessageBox.Show("确定要将所有显示的设置恢复为系统默认值吗？\n此操作仅更新界面上的值，您仍需点击“保存设置”才能将这些默认值持久化到数据库。",
                                "确认恢复默认",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                // 调用服务层的方法来重置其内部属性为默认值
                // _configService.RestoreDefaultSettingsToInstance(); // 服务实例的值不需要在这里改，因为我们是从常量加载到VM

                // 直接将ViewModel的属性设置为常量默认值
                MaxActiveSuits = ConfigurationService.DefaultMaxActiveSuits;
                MaxActiveHats = ConfigurationService.DefaultMaxActiveHats;
                MaxActiveSafetyShoes = ConfigurationService.DefaultMaxActiveSafetyShoes;
                MaxActiveCanvasShoes = ConfigurationService.DefaultMaxActiveCanvasShoes;

                Debug.WriteLine("DEBUG: SettingsViewModel.ExecuteRestoreDefaults: ViewModel properties reset to defaults. User needs to click Save to persist.");
                MessageBox.Show("界面上的设置已恢复为默认值。请点击“保存设置”以应用并持久化这些更改。", "已恢复", MessageBoxButton.OK, MessageBoxImage.Information);

                // 更新保存按钮的可执行状态，因为值可能已更改
                if (SaveSettingsCommand is RelayCommand cmd) cmd.RaiseCanExecuteChanged();
            }
        }
    }
}