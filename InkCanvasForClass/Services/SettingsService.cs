using Ink_Canvas.Helpers;
using Ink_Canvas.Models.Settings;
using Ink_Canvas.Services.Events;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Ink_Canvas.Services
{
    /// <summary>
    /// 设置服务实现，提供设置的加载、保存和访问功能
    /// 使用 YAML 格式存储设置，提供更好的可读性和注释支持
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

        /// <summary>
        /// 旧版 JSON 设置文件路径（用于迁移）
        /// </summary>
        public string LegacyJsonSettingsPath { get; }

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
        /// <param name="settingsFilePath">设置文件路径，默认为 App.RootPath + "Settings.yml"</param>
        public SettingsService(string settingsFilePath = null)
        {
            SettingsFilePath = settingsFilePath ?? Path.Combine(App.RootPath, "Settings.yml");
            LegacyJsonSettingsPath = Path.Combine(App.RootPath, "Settings.json");
            _settings = new Settings();
            SubscribeToSettingsChanges(_settings);
        }

        #region YAML 序列化器

        private static IDeserializer CreateYamlDeserializer()
        {
            return new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();
        }

        private static ISerializer CreateYamlSerializer()
        {
            return new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
                .WithIndentedSequences()
                .Build();
        }

        #endregion

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
                // 优先尝试加载 YAML 格式
                if (File.Exists(SettingsFilePath))
                {
                    string yaml = File.ReadAllText(SettingsFilePath);
                    var deserializer = CreateYamlDeserializer();
                    var loadedSettings = deserializer.Deserialize<Settings>(yaml);

                    if (loadedSettings != null)
                    {
                        Settings = loadedSettings;
                        IsLoaded = true;
                        loadedFromFile = true;

                        // 检查并迁移路径
                        CheckAndMigratePaths();

                        LogHelper.WriteLogToFile($"Settings loaded successfully from YAML: {SettingsFilePath}", LogHelper.LogType.Info);
                        OnSettingsLoaded(SettingsFilePath, loadedFromFile, isDefault);
                        return true;
                    }
                }
                // 如果 YAML 不存在，尝试迁移旧版 JSON
                else if (File.Exists(LegacyJsonSettingsPath))
                {
                    string json = File.ReadAllText(LegacyJsonSettingsPath);
                    var loadedSettings = JsonConvert.DeserializeObject<Settings>(json);

                    if (loadedSettings != null)
                    {
                        Settings = loadedSettings;
                        IsLoaded = true;
                        loadedFromFile = true;

                        // 检查并迁移路径
                        CheckAndMigratePaths();

                        // 迁移到 YAML 格式
                        Save();

                        // 可选：删除旧版 JSON 文件
                        try
                        {
                            File.Delete(LegacyJsonSettingsPath);
                            LogHelper.WriteLogToFile($"Migrated and deleted legacy JSON settings: {LegacyJsonSettingsPath}", LogHelper.LogType.Info);
                        }
                        catch (Exception ex)
                        {
                            LogHelper.WriteLogToFile($"Failed to delete legacy JSON settings: {ex.Message}", LogHelper.LogType.Warning);
                        }

                        LogHelper.WriteLogToFile($"Settings migrated from JSON to YAML: {SettingsFilePath}", LogHelper.LogType.Info);
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

                    // 检查并迁移路径 (即使是默认设置也需要检查路径是否正确)
                    CheckAndMigratePaths();

                    LogHelper.WriteLogToFile($"Settings file not found, using defaults: {SettingsFilePath}", LogHelper.LogType.Info);
                    OnSettingsLoaded(SettingsFilePath, loadedFromFile, isDefault);
                    return true;
                }
            }
            catch (Exception ex) when (ex is YamlDotNet.Core.YamlException || ex is JsonException)
            {
                LogHelper.WriteLogToFile($"Failed to parse settings file: {ex.Message}", LogHelper.LogType.Error);
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
        /// 检查并迁移旧的路径设置
        /// </summary>
        private void CheckAndMigratePaths()
        {
            try
            {
                // Migration: 确保 AutoSavedStrokesLocation 和 StorageLocation 同步
                string programDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
                string oldDefaultPath1 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Ink Canvas");
                string oldDefaultPath2 = Path.Combine(programDir, "InkCanvasForClass");
                string newDefaultPath = Path.Combine(programDir, "Data");

                bool needSave = false;

                // 确保对象不为空
                if (Settings.Automation == null) Settings.Automation = new AutomationSettings();
                if (Settings.Storage == null) Settings.Storage = new StorageSettings();

                // 检查并迁移旧的默认路径
                string currentPath = Settings.Automation.AutoSavedStrokesLocation?.TrimEnd('\\') ?? "";
                string storageLocation = Settings.Storage.StorageLocation ?? "fr";

                // 处理默认的自动选择标识 "a-"，将其设置为 "fr"（icc安装目录）
                if (storageLocation == "a-")
                {
                    storageLocation = "fr";
                    Settings.Storage.StorageLocation = "fr";
                    needSave = true;
                }

                // 获取 StorageLocation 对应的预期路径
                string expectedPath = GetExpectedPathFromStorageLocation(storageLocation, programDir);

                // 如果是自定义存储位置，使用 UserStorageLocation
                if (storageLocation == "c-" && !string.IsNullOrEmpty(Settings.Storage.UserStorageLocation))
                {
                    expectedPath = Settings.Storage.UserStorageLocation.TrimEnd('\\');
                }

                // 只迁移旧的默认路径（Ink Canvas 或 InkCanvasForClass）
                if (currentPath.Equals(oldDefaultPath2, StringComparison.OrdinalIgnoreCase) ||
                    currentPath.Equals(oldDefaultPath1, StringComparison.OrdinalIgnoreCase))
                {
                    Settings.Automation.AutoSavedStrokesLocation = newDefaultPath;
                    Settings.Storage.StorageLocation = "fr";
                    needSave = true;
                    LogHelper.WriteLogToFile($"Migrated old default path from '{currentPath}' to '{newDefaultPath}'", LogHelper.LogType.Info);
                }
                // 特殊处理：如果当前路径包含 "InkCanvasForClass" 且存储位置是 "fr"，强制更新为 Data 目录
                // 这解决了用户反馈的路径卡在旧默认值的问题，即使路径不完全匹配 oldDefaultPath2
                else if (storageLocation == "fr" && currentPath.IndexOf("InkCanvasForClass", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Settings.Automation.AutoSavedStrokesLocation = newDefaultPath;
                    needSave = true;
                    LogHelper.WriteLogToFile($"Forced migration from '{currentPath}' to '{newDefaultPath}' because it contains 'InkCanvasForClass'", LogHelper.LogType.Info);
                }
                // 强制同步 AutoSavedStrokesLocation 与 StorageLocation
                // 始终以 StorageLocation 为准
                else if (!string.IsNullOrEmpty(expectedPath) &&
                        !string.Equals(currentPath, expectedPath.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                {
                    Settings.Automation.AutoSavedStrokesLocation = expectedPath;
                    needSave = true;
                    LogHelper.WriteLogToFile($"Synced AutoSavedStrokesLocation from '{currentPath}' to '{expectedPath}' based on StorageLocation '{storageLocation}'", LogHelper.LogType.Info);
                }

                if (needSave)
                {
                    Save();
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"Error in CheckAndMigratePaths: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 根据 StorageLocation 获取期望的存储路径
        /// </summary>
        private static string GetExpectedPathFromStorageLocation(string storageLocation, string programDir)
        {
            if (string.IsNullOrEmpty(storageLocation))
            {
                return Path.Combine(programDir, "Data");
            }

            if (storageLocation == "c-")
            {
                // 自定义存储位置，由 UserStorageLocation 决定，不在此处处理
                return null;
            }
            else if (storageLocation.StartsWith("d"))
            {
                // 磁盘驱动器存储
                var driveLetter = storageLocation.Substring(1).ToUpper();
                return driveLetter + ":\\InkCanvasForClass";
            }
            else if (storageLocation == "fw")
            {
                // 文档文件夹
                var docfolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                return Path.Combine(docfolder, "InkCanvasForClass");
            }
            else if (storageLocation == "fr")
            {
                // icc安装目录
                return Path.Combine(programDir, "Data");
            }
            else if (storageLocation == "fu")
            {
                // 当前用户目录
                var usrfolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                return Path.Combine(usrfolder, "InkCanvasForClass");
            }
            else if (storageLocation == "fd")
            {
                // 桌面文件夹
                var dskfolder = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                return Path.Combine(dskfolder, "InkCanvasForClass");
            }
            else if (storageLocation == "a-")
            {
                // 自动选择，默认使用安装目录
                return Path.Combine(programDir, "Data");
            }

            // 默认使用安装目录
            return Path.Combine(programDir, "Data");
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
                // 优先尝试加载 YAML 格式
                if (File.Exists(SettingsFilePath))
                {
                    string yaml = await File.ReadAllTextAsync(SettingsFilePath);
                    var deserializer = CreateYamlDeserializer();
                    var loadedSettings = deserializer.Deserialize<Settings>(yaml);

                    if (loadedSettings != null)
                    {
                        Settings = loadedSettings;
                        IsLoaded = true;
                        loadedFromFile = true;

                        // 检查并迁移路径
                        CheckAndMigratePaths();

                        LogHelper.WriteLogToFile($"Settings loaded successfully from YAML: {SettingsFilePath}", LogHelper.LogType.Info);
                        OnSettingsLoaded(SettingsFilePath, loadedFromFile, isDefault);
                        return true;
                    }
                }
                // 如果 YAML 不存在，尝试迁移旧版 JSON
                else if (File.Exists(LegacyJsonSettingsPath))
                {
                    string json = await File.ReadAllTextAsync(LegacyJsonSettingsPath);
                    var loadedSettings = JsonConvert.DeserializeObject<Settings>(json);

                    if (loadedSettings != null)
                    {
                        Settings = loadedSettings;
                        IsLoaded = true;
                        loadedFromFile = true;

                        // 检查并迁移路径
                        CheckAndMigratePaths();

                        // 迁移到 YAML 格式
                        await SaveAsync();

                        // 可选：删除旧版 JSON 文件
                        try
                        {
                            File.Delete(LegacyJsonSettingsPath);
                            LogHelper.WriteLogToFile($"Migrated and deleted legacy JSON settings: {LegacyJsonSettingsPath}", LogHelper.LogType.Info);
                        }
                        catch (Exception ex)
                        {
                            LogHelper.WriteLogToFile($"Failed to delete legacy JSON settings: {ex.Message}", LogHelper.LogType.Warning);
                        }

                        LogHelper.WriteLogToFile($"Settings migrated from JSON to YAML: {SettingsFilePath}", LogHelper.LogType.Info);
                        OnSettingsLoaded(SettingsFilePath, loadedFromFile, isDefault);
                        return true;
                    }
                }
                else
                {
                    Settings = new Settings();
                    IsLoaded = true;
                    isDefault = true;

                    // 检查并迁移路径
                    CheckAndMigratePaths();

                    LogHelper.WriteLogToFile($"Settings file not found, using defaults: {SettingsFilePath}", LogHelper.LogType.Info);
                    OnSettingsLoaded(SettingsFilePath, loadedFromFile, isDefault);
                    return true;
                }
            }
            catch (Exception ex) when (ex is YamlDotNet.Core.YamlException || ex is JsonException)
            {
                LogHelper.WriteLogToFile($"Failed to parse settings file: {ex.Message}", LogHelper.LogType.Error);
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
        /// 保存设置到文件（YAML 格式）
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

                // 使用 YAML 序列化
                var serializer = CreateYamlSerializer();
                string yaml = serializer.Serialize(Settings);

                // 添加文件头注释
                string yamlWithHeader = $"# InkCanvasForClass 设置文件\n" +
                                       $"# 生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                                       $"# 警告: 手动修改时请确保 YAML 格式正确\n\n" +
                                       yaml;

                File.WriteAllText(SettingsFilePath, yamlWithHeader);

                LogHelper.WriteLogToFile($"Settings saved successfully to YAML: {SettingsFilePath}", LogHelper.LogType.Info);
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
        /// 异步保存设置到文件（YAML 格式）
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

                // 使用 YAML 序列化
                var serializer = CreateYamlSerializer();
                string yaml = serializer.Serialize(Settings);

                // 添加文件头注释
                string yamlWithHeader = $"# InkCanvasForClass 设置文件\n" +
                                       $"# 生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                                       $"# 警告: 手动修改时请确保 YAML 格式正确\n\n" +
                                       yaml;

                await File.WriteAllTextAsync(SettingsFilePath, yamlWithHeader);

                LogHelper.WriteLogToFile($"Settings saved successfully to YAML: {SettingsFilePath}", LogHelper.LogType.Info);
                OnSettingsSaved(SettingsFilePath, true);
                return true;
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
            LogHelper.WriteLogToFile("设置已重置为默认值", LogHelper.LogType.Info);
            OnSettingsLoaded(SettingsFilePath, false, true);
        }

        /// <summary>
        /// 从外部设置对象同步设置
        /// 用于与 MainWindow.Settings 保持同步
        /// </summary>
        /// <param name="externalSettings">外部设置对象</param>
        public void SyncFrom(Settings externalSettings)
        {
            if (externalSettings == null)
            {
                LogHelper.WriteLogToFile("SyncFrom 调用时 settings 为空，已忽略", LogHelper.LogType.Warning);
                return;
            }

            lock (_lock)
            {
                var oldSettings = _settings;
                if (oldSettings != null)
                {
                    UnsubscribeFromSettingsChanges(oldSettings);
                }
                _settings = externalSettings;
                if (_settings != null)
                {
                    SubscribeToSettingsChanges(_settings);
                }
                IsLoaded = true;
            }

            LogHelper.WriteLogToFile("设置已从外部来源同步", LogHelper.LogType.Info);
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
