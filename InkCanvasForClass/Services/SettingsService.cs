using Ink_Canvas.Helpers;
using Ink_Canvas.Models.Settings;
using Ink_Canvas.Services.Events;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;

namespace Ink_Canvas.Services
{
    /// <summary>
    /// 设置服务实现，提供设置的加载、保存和访问功能
    /// </summary>
    public class SettingsService : ISettingsService
    {
        private Settings _settings;
        private readonly object _lock = new object();

        #region 属性

        /// <summary>
        /// 获取当前设置对象
        /// </summary>
        public Settings Settings
        {
            get
            {
                lock (_lock)
                {
                    return _settings ??= new Settings();
                }
            }
            private set
            {
                lock (_lock)
                {
                    var oldSettings = _settings;
                    if (oldSettings != null)
                    {
                        UnsubscribeFromSettingsChanges(oldSettings);
                    }
                    _settings = value;
                    if (_settings != null)
                    {
                        SubscribeToSettingsChanges(_settings);
                    }
                }
            }
        }

        /// <summary>
        /// 设置是否已加载
        /// </summary>
        public bool IsLoaded { get; private set; }

        /// <summary>
        /// 设置文件路径
        /// </summary>
        public string SettingsFilePath { get; }

        #endregion

        #region 分类访问属性

        /// <summary>
        /// 画布设置
        /// </summary>
        public CanvasSettings Canvas => Settings?.Canvas;

        /// <summary>
        /// 外观设置
        /// </summary>
        public AppearanceSettings Appearance => Settings?.Appearance;

        /// <summary>
        /// 手势设置
        /// </summary>
        public GestureSettings Gesture => Settings?.Gesture;

        /// <summary>
        /// PowerPoint 设置
        /// </summary>
        public PowerPointSettings PowerPoint => Settings?.PowerPointSettings;

        /// <summary>
        /// 自动化设置
        /// </summary>
        public AutomationSettings Automation => Settings?.Automation;

        /// <summary>
        /// 高级设置
        /// </summary>
        public AdvancedSettings Advanced => Settings?.Advanced;

        /// <summary>
        /// 墨迹转形状设置
        /// </summary>
        public InkToShapeSettings InkToShape => Settings?.InkToShape;

        /// <summary>
        /// 启动设置
        /// </summary>
        public StartupSettings Startup => Settings?.Startup;

        /// <summary>
        /// 截图设置
        /// </summary>
        public SnapshotSettings Snapshot => Settings?.Snapshot;

        /// <summary>
        /// 存储设置
        /// </summary>
        public StorageSettings Storage => Settings?.Storage;

        /// <summary>
        /// 随机点名设置
        /// </summary>
        public RandSettings RandSettings => Settings?.RandSettings;

        #endregion

        #region 事件

        /// <summary>
        /// 设置加载完成事件
        /// </summary>
        public event EventHandler<SettingsLoadedEventArgs> SettingsLoaded;

        /// <summary>
        /// 设置保存完成事件
        /// </summary>
        public event EventHandler<SettingsSavedEventArgs> SettingsSaved;

        /// <summary>
        /// 单个设置项变更事件
        /// </summary>
        public event EventHandler<SettingChangedEventArgs> SettingChanged;

        /// <summary>
        /// 设置变更事件（向后兼容）
        /// </summary>
        [Obsolete("请使用 SettingChanged 事件代替")]
        public event EventHandler<SettingsChangedEventArgs> SettingsChanged;

        #endregion

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="settingsFilePath">设置文件路径，默认为 App.RootPath + "Settings.json"</param>
        public SettingsService(string settingsFilePath = null)
        {
            SettingsFilePath = settingsFilePath ?? Path.Combine(App.RootPath, "Settings.json");
            _settings = new Settings();
            SubscribeToSettingsChanges(_settings);
        }

        /// <summary>
        /// 从文件加载设置
        /// </summary>
        /// <returns>是否加载成功</returns>
        public bool Load()
        {
            bool loadedFromFile = false;
            bool isDefault = false;

            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    var loadedSettings = JsonConvert.DeserializeObject<Settings>(json);
                    
                    if (loadedSettings != null)
                    {
                        Settings = loadedSettings;
                        IsLoaded = true;
                        loadedFromFile = true;
                        LogHelper.WriteLogToFile($"Settings loaded successfully from {SettingsFilePath}", LogHelper.LogType.Info);
                        OnSettingsLoaded(SettingsFilePath, loadedFromFile, isDefault);
                        return true;
                    }
                }
                else
                {
                    // 文件不存在，使用默认设置
                    Settings = new Settings();
                    IsLoaded = true;
                    isDefault = true;
                    LogHelper.WriteLogToFile($"Settings file not found, using defaults: {SettingsFilePath}", LogHelper.LogType.Info);
                    OnSettingsLoaded(SettingsFilePath, loadedFromFile, isDefault);
                    return true;
                }
            }
            catch (JsonException ex)
            {
                LogHelper.WriteLogToFile($"Failed to parse settings JSON: {ex.Message}", LogHelper.LogType.Error);
            }
            catch (IOException ex)
            {
                LogHelper.WriteLogToFile($"Failed to read settings file: {ex.Message}", LogHelper.LogType.Error);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"Unexpected error loading settings: {ex.Message}", LogHelper.LogType.Error);
            }

            // 加载失败，使用默认设置
            Settings = new Settings();
            IsLoaded = true;
            isDefault = true;
            OnSettingsLoaded(SettingsFilePath, loadedFromFile, isDefault);
            return false;
        }

        /// <summary>
        /// 异步从文件加载设置
        /// </summary>
        /// <returns>是否加载成功</returns>
        public async Task<bool> LoadAsync()
        {
            bool loadedFromFile = false;
            bool isDefault = false;

            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = await File.ReadAllTextAsync(SettingsFilePath);
                    var loadedSettings = JsonConvert.DeserializeObject<Settings>(json);
                    
                    if (loadedSettings != null)
                    {
                        Settings = loadedSettings;
                        IsLoaded = true;
                        loadedFromFile = true;
                        LogHelper.WriteLogToFile($"Settings loaded successfully from {SettingsFilePath}", LogHelper.LogType.Info);
                        OnSettingsLoaded(SettingsFilePath, loadedFromFile, isDefault);
                        return true;
                    }
                }
                else
                {
                    Settings = new Settings();
                    IsLoaded = true;
                    isDefault = true;
                    LogHelper.WriteLogToFile($"Settings file not found, using defaults: {SettingsFilePath}", LogHelper.LogType.Info);
                    OnSettingsLoaded(SettingsFilePath, loadedFromFile, isDefault);
                    return true;
                }
            }
            catch (JsonException ex)
            {
                LogHelper.WriteLogToFile($"Failed to parse settings JSON: {ex.Message}", LogHelper.LogType.Error);
                LogHelper.NewLog(ex);
            }
            catch (IOException ex)
            {
                LogHelper.WriteLogToFile($"Failed to read settings file: {ex.Message}", LogHelper.LogType.Error);
                LogHelper.NewLog(ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                LogHelper.WriteLogToFile($"Access denied reading settings file: {ex.Message}", LogHelper.LogType.Error);
                LogHelper.NewLog(ex);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"Unexpected error loading settings async: {ex.Message}", LogHelper.LogType.Error);
                LogHelper.NewLog(ex);
            }

            Settings = new Settings();
            IsLoaded = true;
            isDefault = true;
            OnSettingsLoaded(SettingsFilePath, loadedFromFile, isDefault);
            return false;
        }

        /// <summary>
        /// 保存设置到文件
        /// </summary>
        /// <returns>是否保存成功</returns>
        public bool Save()
        {
            try
            {
                // 确保目录存在
                string directory = Path.GetDirectoryName(SettingsFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonConvert.SerializeObject(Settings, Formatting.Indented);
                File.WriteAllText(SettingsFilePath, json);
                
                LogHelper.WriteLogToFile($"Settings saved successfully to {SettingsFilePath}", LogHelper.LogType.Info);
                OnSettingsSaved(SettingsFilePath, true);
                return true;
            }
            catch (IOException ex)
            {
                LogHelper.WriteLogToFile($"Failed to write settings file: {ex.Message}", LogHelper.LogType.Error);
                OnSettingsSaved(SettingsFilePath, false, ex.Message);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"Unexpected error saving settings: {ex.Message}", LogHelper.LogType.Error);
                OnSettingsSaved(SettingsFilePath, false, ex.Message);
            }

            return false;
        }

        /// <summary>
        /// 异步保存设置到文件
        /// </summary>
        /// <returns>是否保存成功</returns>
        public async Task<bool> SaveAsync()
        {
            try
            {
                string directory = Path.GetDirectoryName(SettingsFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonConvert.SerializeObject(Settings, Formatting.Indented);
                await File.WriteAllTextAsync(SettingsFilePath, json);
                
                LogHelper.WriteLogToFile($"Settings saved successfully to {SettingsFilePath}", LogHelper.LogType.Info);
                OnSettingsSaved(SettingsFilePath, true);
                return true;
            }
            catch (JsonException ex)
            {
                LogHelper.WriteLogToFile($"Failed to serialize settings to JSON: {ex.Message}", LogHelper.LogType.Error);
                LogHelper.NewLog(ex);
                OnSettingsSaved(SettingsFilePath, false, ex.Message);
            }
            catch (IOException ex)
            {
                LogHelper.WriteLogToFile($"Failed to write settings file: {ex.Message}", LogHelper.LogType.Error);
                LogHelper.NewLog(ex);
                OnSettingsSaved(SettingsFilePath, false, ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                LogHelper.WriteLogToFile($"Access denied writing settings file: {ex.Message}", LogHelper.LogType.Error);
                LogHelper.NewLog(ex);
                OnSettingsSaved(SettingsFilePath, false, ex.Message);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"Unexpected error saving settings async: {ex.Message}", LogHelper.LogType.Error);
                LogHelper.NewLog(ex);
                OnSettingsSaved(SettingsFilePath, false, ex.Message);
            }

            return false;
        }

        /// <summary>
        /// 重置设置为默认值
        /// </summary>
        public void ResetToDefaults()
        {
            Settings = new Settings();
            LogHelper.WriteLogToFile("Settings reset to defaults", LogHelper.LogType.Info);
            OnSettingsLoaded(SettingsFilePath, false, true);
        }

        #region 事件触发方法

        /// <summary>
        /// 触发设置加载完成事件
        /// </summary>
        protected virtual void OnSettingsLoaded(string filePath, bool loadedFromFile, bool isDefault)
        {
            SettingsLoaded?.Invoke(this, new SettingsLoadedEventArgs(filePath, loadedFromFile, isDefault));
        }

        /// <summary>
        /// 触发设置保存完成事件
        /// </summary>
        protected virtual void OnSettingsSaved(string filePath, bool success, string errorMessage = null)
        {
            SettingsSaved?.Invoke(this, new SettingsSavedEventArgs(filePath, success, errorMessage));
        }

        /// <summary>
        /// 触发单个设置项变更事件
        /// </summary>
        protected virtual void OnSettingChanged(string categoryName, string propertyName, object oldValue, object newValue)
        {
            SettingChanged?.Invoke(this, new SettingChangedEventArgs(categoryName, propertyName, oldValue, newValue));
            
            // 同时触发向后兼容的事件
#pragma warning disable CS0618 // 类型或成员已过时
            SettingsChanged?.Invoke(this, new SettingsChangedEventArgs(categoryName, propertyName, oldValue, newValue));
#pragma warning restore CS0618
        }

        #endregion

        #region 设置变更订阅

        /// <summary>
        /// 订阅设置对象的属性变更事件
        /// </summary>
        private void SubscribeToSettingsChanges(Settings settings)
        {
            if (settings == null) return;

            // 订阅各分类的 ExtendedPropertyChanged 事件以获取旧值和新值
            if (settings.Canvas != null)
                settings.Canvas.ExtendedPropertyChanged += (s, e) => HandleExtendedPropertyChanged("Canvas", e);
            
            if (settings.Appearance != null)
                settings.Appearance.ExtendedPropertyChanged += (s, e) => HandleExtendedPropertyChanged("Appearance", e);
            
            if (settings.Gesture != null)
                settings.Gesture.ExtendedPropertyChanged += (s, e) => HandleExtendedPropertyChanged("Gesture", e);
            
            if (settings.PowerPointSettings != null)
                settings.PowerPointSettings.ExtendedPropertyChanged += (s, e) => HandleExtendedPropertyChanged("PowerPoint", e);
            
            if (settings.Automation != null)
                settings.Automation.ExtendedPropertyChanged += (s, e) => HandleExtendedPropertyChanged("Automation", e);
            
            if (settings.Advanced != null)
                settings.Advanced.ExtendedPropertyChanged += (s, e) => HandleExtendedPropertyChanged("Advanced", e);
            
            if (settings.InkToShape != null)
                settings.InkToShape.ExtendedPropertyChanged += (s, e) => HandleExtendedPropertyChanged("InkToShape", e);
            
            if (settings.Startup != null)
                settings.Startup.ExtendedPropertyChanged += (s, e) => HandleExtendedPropertyChanged("Startup", e);
            
            if (settings.Snapshot != null)
                settings.Snapshot.ExtendedPropertyChanged += (s, e) => HandleExtendedPropertyChanged("Snapshot", e);
            
            if (settings.Storage != null)
                settings.Storage.ExtendedPropertyChanged += (s, e) => HandleExtendedPropertyChanged("Storage", e);
            
            if (settings.RandSettings != null)
                settings.RandSettings.ExtendedPropertyChanged += (s, e) => HandleExtendedPropertyChanged("RandSettings", e);
        }

        /// <summary>
        /// 取消订阅设置对象的属性变更事件
        /// </summary>
        private void UnsubscribeFromSettingsChanges(Settings settings)
        {
            // 由于我们使用的是匿名委托，无法直接取消订阅
            // 但由于旧的 Settings 对象会被垃圾回收，这不会造成内存泄漏
            // 如果需要更精确的控制，可以使用命名方法或存储委托引用
        }

        /// <summary>
        /// 处理扩展属性变更事件
        /// </summary>
        private void HandleExtendedPropertyChanged(string categoryName, ExtendedPropertyChangedEventArgs e)
        {
            OnSettingChanged(categoryName, e.PropertyName, e.OldValue, e.NewValue);
        }

        #endregion
    }
}