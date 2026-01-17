using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ink_Canvas.Core;
using Ink_Canvas.Helpers;
using Ink_Canvas.Services;
using Ink_Canvas.Services.Events;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

using Ink_Canvas.ViewModels.Settings;

namespace Ink_Canvas.ViewModels
{
    /// <summary>
    /// Settings ViewModel - 提供设置的可绑定访问
    /// 使用 CommunityToolkit.Mvvm 源代码生成器
    /// </summary>
    public partial class SettingsViewModel : ViewModelBase
    {
        private readonly ISettingsService _settingsService;

        /// <summary>
        /// 获取内部设置对象（用于兼容现有代码）
        /// </summary>
        public Ink_Canvas.Settings Settings => _settingsService.Settings;

        #region 事件

        /// <summary>
        /// 重启应用请求事件
        /// </summary>
        public event EventHandler RestartRequested;

        /// <summary>
        /// 退出应用请求事件
        /// </summary>
        public event EventHandler ExitRequested;

        #endregion

        #region 子 ViewModel

        /// <summary>
        /// Whiteboard/Canvas Settings ViewModel (Renamed from Canvas)
        /// </summary>
        [ObservableProperty]
        private CanvasSettingsViewModel _whiteboard;

        /// <summary>
        /// Appearance Settings ViewModel
        /// </summary>
        [ObservableProperty]
        private AppearanceSettingsViewModel _appearance;

        /// <summary>
        /// Gesture Settings ViewModel
        /// </summary>
        [ObservableProperty]
        private GestureSettingsViewModel _gesture;

        /// <summary>
        /// Startup Settings ViewModel
        /// </summary>
        [ObservableProperty]
        private StartupSettingsViewModel _startup;

        /// <summary>
        /// Advanced Settings ViewModel
        /// </summary>
        [ObservableProperty]
        private AdvancedSettingsViewModel _advanced;

        /// <summary>
        /// Automation Settings ViewModel
        /// </summary>
        [ObservableProperty]
        private AutomationSettingsViewModel _automation;

        /// <summary>
        /// PowerPoint Settings ViewModel
        /// </summary>
        [ObservableProperty]
        private PowerPointSettingsViewModel _powerPoint;

        /// <summary>
        /// Snapshot Settings ViewModel
        /// </summary>
        [ObservableProperty]
        private SnapshotSettingsViewModel _snapshot;

        /// <summary>
        /// InkToShape Settings ViewModel
        /// </summary>
        [ObservableProperty]
        private InkToShapeSettingsViewModel _inkToShape;

        /// <summary>
        /// Storage Settings ViewModel
        /// </summary>
        [ObservableProperty]
        private StorageSettingsViewModel _storage;

        /// <summary>
        /// Random Pick Settings ViewModel
        /// </summary>
        [ObservableProperty]
        private RandomPickSettingsViewModel _randomPick;

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="settingsService">Settings Service</param>
        public SettingsViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

            // 注意：子 ViewModel 的初始化延迟到设置加载完成后
            // 这是因为在 DI 容器创建 SettingsViewModel 时，SettingsService.Load() 可能还未调用
            // 如果此时初始化子 ViewModel，它们会引用一个空的默认设置对象
            // 而当 Load() 被调用后，Settings 对象会被替换，导致子 ViewModel 与实际设置不同步

            // Subscribe to Settings Loaded Event - 当设置（重新）加载时重新初始化子 ViewModel
            _settingsService.SettingsLoaded += OnSettingsLoaded;

            // Subscribe to Settings Changed Event
            _settingsService.SettingChanged += OnSettingChanged;

            // 如果设置已经加载，则立即初始化子 ViewModel
            if (_settingsService.IsLoaded)
            {
                InitializeSubViewModels();
            }
        }

        /// <summary>
        /// 设置加载完成事件处理
        /// 当 SettingsService.Load() 被调用时，需要重新初始化子 ViewModel
        /// 以确保它们引用的是最新加载的设置对象
        /// </summary>
        private void OnSettingsLoaded(object sender, SettingsLoadedEventArgs e)
        {
            try
            {
                LogHelper.WriteLogToFile($"SettingsViewModel: Settings loaded, reinitializing sub-ViewModels (loadedFromFile={e.LoadedFromFile}, isDefault={e.IsDefault})", LogHelper.LogType.Info);
                InitializeSubViewModels();
                OnPropertyChanged(string.Empty); // 通知所有属性已更改
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"SettingsViewModel: Error reinitializing sub-ViewModels: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// Initialize Sub-ViewModels
        /// </summary>
        private void InitializeSubViewModels()
        {
            Whiteboard = new CanvasSettingsViewModel(Settings.Canvas, SaveSettings);
            Appearance = new AppearanceSettingsViewModel(Settings.Appearance, SaveSettings);
            Gesture = new GestureSettingsViewModel(Settings.Gesture, SaveSettings);
            Startup = new StartupSettingsViewModel(Settings.Startup, SaveSettings);
            Advanced = new AdvancedSettingsViewModel(Settings.Advanced, SaveSettings);
            Automation = new AutomationSettingsViewModel(Settings.Automation, SaveSettings);
            PowerPoint = new PowerPointSettingsViewModel(Settings.PowerPointSettings, SaveSettings);
            Snapshot = new SnapshotSettingsViewModel(Settings.Snapshot, SaveSettings);
            InkToShape = new InkToShapeSettingsViewModel(Settings.InkToShape, SaveSettings);
            Storage = new StorageSettingsViewModel(Settings.Storage, SaveSettings);
            RandomPick = new RandomPickSettingsViewModel(Settings.RandSettings, SaveSettings);
        }

        /// <summary>
        /// 设置变更事件处理
        /// </summary>
        private void OnSettingChanged(object sender, SettingChangedEventArgs e)
        {
            // 通知 UI 设置已更改
            OnPropertyChanged(nameof(Settings));
        }

        /// <summary>
        /// 保存设置命令
        /// </summary>
        [RelayCommand]
        private void SaveSettings()
        {
            _settingsService.Save();
        }

        /// <summary>
        /// 重置设置命令
        /// </summary>
        [RelayCommand]
        public void ResetSettings()
        {
            _settingsService.ResetToDefaults();
            InitializeSubViewModels();
            OnPropertyChanged(string.Empty); // 通知所有属性已更改
        }

        /// <summary>
        /// 重新加载设置命令
        /// </summary>
        [RelayCommand]
        private void ReloadSettings()
        {
            _settingsService.Load();
            InitializeSubViewModels();
            OnPropertyChanged(string.Empty);
        }

        /// <summary>
        /// 重启应用命令
        /// </summary>
        [RelayCommand]
        private void Restart()
        {
            // 触发重启请求事件，由 View 处理实际的重启逻辑
            RestartRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 退出应用命令
        /// </summary>
        [RelayCommand]
        private void Exit()
        {
            // 触发退出请求事件，由 View 处理实际的退出逻辑
            ExitRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public override void Cleanup()
        {
            _settingsService.SettingsLoaded -= OnSettingsLoaded;
            _settingsService.SettingChanged -= OnSettingChanged;
            base.Cleanup();
        }
    }
}
