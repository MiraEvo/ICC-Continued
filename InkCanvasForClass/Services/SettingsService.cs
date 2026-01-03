using Ink_Canvas.Helpers;
using Newtonsoft.Json;
using System;
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
                    _settings = value;
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

        /// <summary>
        /// 设置变更事件
        /// </summary>
        public event EventHandler<SettingsChangedEventArgs> SettingsChanged;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="settingsFilePath">设置文件路径，默认为 App.RootPath + "Settings.json"</param>
        public SettingsService(string settingsFilePath = null)
        {
            SettingsFilePath = settingsFilePath ?? Path.Combine(App.RootPath, "Settings.json");
            _settings = new Settings();
        }

        /// <summary>
        /// 从文件加载设置
        /// </summary>
        /// <returns>是否加载成功</returns>
        public bool Load()
        {
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
                        LogHelper.WriteLogToFile($"Settings loaded successfully from {SettingsFilePath}", LogHelper.LogType.Info);
                        return true;
                    }
                }
                else
                {
                    // 文件不存在，使用默认设置
                    Settings = new Settings();
                    IsLoaded = true;
                    LogHelper.WriteLogToFile($"Settings file not found, using defaults: {SettingsFilePath}", LogHelper.LogType.Info);
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
            return false;
        }

        /// <summary>
        /// 异步从文件加载设置
        /// </summary>
        /// <returns>是否加载成功</returns>
        public async Task<bool> LoadAsync()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = await Task.Run(() => File.ReadAllText(SettingsFilePath));
                    var loadedSettings = JsonConvert.DeserializeObject<Settings>(json);
                    
                    if (loadedSettings != null)
                    {
                        Settings = loadedSettings;
                        IsLoaded = true;
                        LogHelper.WriteLogToFile($"Settings loaded successfully from {SettingsFilePath}", LogHelper.LogType.Info);
                        return true;
                    }
                }
                else
                {
                    Settings = new Settings();
                    IsLoaded = true;
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"Failed to load settings async: {ex.Message}", LogHelper.LogType.Error);
            }

            Settings = new Settings();
            IsLoaded = true;
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
                return true;
            }
            catch (IOException ex)
            {
                LogHelper.WriteLogToFile($"Failed to write settings file: {ex.Message}", LogHelper.LogType.Error);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"Unexpected error saving settings: {ex.Message}", LogHelper.LogType.Error);
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
                await Task.Run(() => File.WriteAllText(SettingsFilePath, json));
                
                LogHelper.WriteLogToFile($"Settings saved successfully to {SettingsFilePath}", LogHelper.LogType.Info);
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"Failed to save settings async: {ex.Message}", LogHelper.LogType.Error);
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
        }

        /// <summary>
        /// 触发设置变更事件
        /// </summary>
        /// <param name="categoryName">设置分类名称</param>
        /// <param name="propertyName">属性名称</param>
        /// <param name="oldValue">旧值</param>
        /// <param name="newValue">新值</param>
        protected virtual void OnSettingsChanged(string categoryName, string propertyName, object oldValue, object newValue)
        {
            SettingsChanged?.Invoke(this, new SettingsChangedEventArgs(categoryName, propertyName, oldValue, newValue));
        }
    }
}