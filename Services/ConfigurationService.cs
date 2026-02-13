// Services/ConfigurationService.cs
using PersonalPPEManager.DataAccess; // 为了 SQLiteDataAccess
using System;                       // 为了 Lazy<T>
using System.Diagnostics;           // 为了 Debug.WriteLine

namespace PersonalPPEManager.Services
{
    public class ConfigurationService
    {
        // --- 单例模式实现 ---
        private static readonly Lazy<ConfigurationService> _lazyInstance =
            new Lazy<ConfigurationService>(() => new ConfigurationService());

        /// <summary>
        /// 获取 ConfigurationService 的唯一实例。
        /// </summary>
        public static ConfigurationService Instance => _lazyInstance.Value;
        // --- 单例模式实现结束 ---

        #region Setting Keys Constants
        public const string KeyMaxActiveSuits = "MaxActiveSuits";
        public const string KeyMaxActiveHats = "MaxActiveHats";
        public const string KeyMaxActiveSafetyShoes = "MaxActiveSafetyShoes";
        public const string KeyMaxActiveCanvasShoes = "MaxActiveCanvasShoes";
        #endregion

        #region Default Values Constants
        public const int DefaultMaxActiveSuits = 3;
        public const int DefaultMaxActiveHats = 3;
        public const int DefaultMaxActiveSafetyShoes = 1; // 示例默认值，鞋子通常1双有效
        public const int DefaultMaxActiveCanvasShoes = 1; // 示例默认值
        #endregion

        #region Public Properties for Settings
        // 这些属性持有当前加载的或用户通过 SettingsViewModel 修改的配置值
        // SettingsViewModel 会负责将UI上的值同步到这些属性，然后再调用 SaveAllSettingsToDb

        /// <summary>
        /// 允许员工持有的最大有效洁净服套数。
        /// </summary>
        public int MaxActiveSuits { get; set; }

        /// <summary>
        /// 允许员工持有的最大有效帽子顶数。
        /// </summary>
        public int MaxActiveHats { get; set; }

        /// <summary>
        /// 允许员工持有的最大有效白色劳保鞋双数。
        /// </summary>
        public int MaxActiveSafetyShoes { get; set; }

        /// <summary>
        /// 允许员工持有的最大有效白色帆布鞋双数。
        /// </summary>
        public int MaxActiveCanvasShoes { get; set; }
        #endregion

        /// <summary>
        /// 私有构造函数，强制使用单例。
        /// 在实例首次创建时从数据库加载设置。
        /// </summary>
        private ConfigurationService()
        {
            Debug.WriteLine("DEBUG: ConfigurationService_ctor (Singleton): Instance being created. Loading settings from DB.");
            LoadAllSettingsFromDb();
        }

        /// <summary>
        /// 从数据库加载所有设置到当前服务实例的属性中。
        /// 如果数据库中没有特定设置，则使用预定义的默认值。
        /// </summary>
        public void LoadAllSettingsFromDb()
        {
            MaxActiveSuits = SQLiteDataAccess.GetSettingInt(KeyMaxActiveSuits, DefaultMaxActiveSuits);
            MaxActiveHats = SQLiteDataAccess.GetSettingInt(KeyMaxActiveHats, DefaultMaxActiveHats);
            MaxActiveSafetyShoes = SQLiteDataAccess.GetSettingInt(KeyMaxActiveSafetyShoes, DefaultMaxActiveSafetyShoes);
            MaxActiveCanvasShoes = SQLiteDataAccess.GetSettingInt(KeyMaxActiveCanvasShoes, DefaultMaxActiveCanvasShoes);

            Debug.WriteLine($"DEBUG: ConfigurationService.LoadAllSettingsFromDb: Settings loaded into service instance. " +
                              $"Suits={MaxActiveSuits}, Hats={MaxActiveHats}, " +
                              $"SafetyShoes={MaxActiveSafetyShoes}, CanvasShoes={MaxActiveCanvasShoes}");
        }

        /// <summary>
        /// 将当前服务实例属性中的所有设置值保存到数据库。
        /// </summary>
        /// <returns>如果所有设置都成功保存则为true，否则为false。</returns>
        public bool SaveAllSettingsToDb()
        {
            Debug.WriteLine($"DEBUG: ConfigurationService.SaveAllSettingsToDb: Saving current service instance settings to DB. " +
                              $"Suits={this.MaxActiveSuits}, Hats={this.MaxActiveHats}, " +
                              $"SafetyShoes={this.MaxActiveSafetyShoes}, CanvasShoes={this.MaxActiveCanvasShoes}");

            bool successOverall = true;

            // 将当前服务实例的属性值逐个保存到数据库
            if (!SQLiteDataAccess.SaveSettingInt(KeyMaxActiveSuits, this.MaxActiveSuits))
            {
                successOverall = false;
                Debug.WriteLine($"DEBUG: ConfigurationService.SaveAllSettingsToDb: FAILED to save {KeyMaxActiveSuits}.");
            }
            if (!SQLiteDataAccess.SaveSettingInt(KeyMaxActiveHats, this.MaxActiveHats))
            {
                successOverall = false;
                Debug.WriteLine($"DEBUG: ConfigurationService.SaveAllSettingsToDb: FAILED to save {KeyMaxActiveHats}.");
            }
            if (!SQLiteDataAccess.SaveSettingInt(KeyMaxActiveSafetyShoes, this.MaxActiveSafetyShoes))
            {
                successOverall = false;
                Debug.WriteLine($"DEBUG: ConfigurationService.SaveAllSettingsToDb: FAILED to save {KeyMaxActiveSafetyShoes}.");
            }
            if (!SQLiteDataAccess.SaveSettingInt(KeyMaxActiveCanvasShoes, this.MaxActiveCanvasShoes))
            {
                successOverall = false;
                Debug.WriteLine($"DEBUG: ConfigurationService.SaveAllSettingsToDb: FAILED to save {KeyMaxActiveCanvasShoes}.");
            }

            if (successOverall)
                Debug.WriteLine("DEBUG: ConfigurationService.SaveAllSettingsToDb: All settings saved successfully to DB.");
            else
                Debug.WriteLine("DEBUG: ConfigurationService.SaveAllSettingsToDb: One or more settings FAILED to save to DB.");

            return successOverall;
        }

        /// <summary>
        /// 将当前服务实例的属性恢复为预定义的默认值。
        /// 注意：此方法仅修改服务实例中的值，不会自动保存到数据库。
        /// 需要调用 SaveAllSettingsToDb() 来持久化这些默认值。
        /// </summary>
        public void RestoreDefaultSettingsToInstance()
        {
            Debug.WriteLine("DEBUG: ConfigurationService.RestoreDefaultSettingsToInstance: Restoring default settings to service instance properties.");
            MaxActiveSuits = DefaultMaxActiveSuits;
            MaxActiveHats = DefaultMaxActiveHats;
            MaxActiveSafetyShoes = DefaultMaxActiveSafetyShoes;
            MaxActiveCanvasShoes = DefaultMaxActiveCanvasShoes;
            Debug.WriteLine("DEBUG: ConfigurationService.RestoreDefaultSettingsToInstance: Service instance properties reset to defaults. (Not yet saved to DB)");
        }
    }
}