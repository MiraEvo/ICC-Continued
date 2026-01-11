using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ink_Canvas.Core;
using Ink_Canvas.Services;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

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
        public Settings Settings => _settingsService.Settings;

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
        /// Canvas 设置 ViewModel
        /// </summary>
        [ObservableProperty]
        private CanvasSettingsViewModel _canvas;

        /// <summary>
        /// Appearance 设置 ViewModel
        /// </summary>
        [ObservableProperty]
        private AppearanceSettingsViewModel _appearance;

        /// <summary>
        /// Gesture 设置 ViewModel
        /// </summary>
        [ObservableProperty]
        private GestureSettingsViewModel _gesture;

        /// <summary>
        /// Startup 设置 ViewModel
        /// </summary>
        [ObservableProperty]
        private StartupSettingsViewModel _startup;

        /// <summary>
        /// Advanced 设置 ViewModel
        /// </summary>
        [ObservableProperty]
        private AdvancedSettingsViewModel _advanced;

        /// <summary>
        /// Automation 设置 ViewModel
        /// </summary>
        [ObservableProperty]
        private AutomationSettingsViewModel _automation;

        /// <summary>
        /// PowerPoint 设置 ViewModel
        /// </summary>
        [ObservableProperty]
        private PowerPointSettingsViewModel _powerPoint;

        /// <summary>
        /// Snapshot 设置 ViewModel
        /// </summary>
        [ObservableProperty]
        private SnapshotSettingsViewModel _snapshot;

        /// <summary>
        /// InkToShape 设置 ViewModel
        /// </summary>
        [ObservableProperty]
        private InkToShapeSettingsViewModel _inkToShape;

        #endregion

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="settingsService">设置服务</param>
        public SettingsViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            
            // 初始化子 ViewModel
            InitializeSubViewModels();
            
            // 订阅设置变更事件
            _settingsService.SettingsChanged += OnSettingsChanged;
        }

        /// <summary>
        /// 初始化子 ViewModel
        /// </summary>
        private void InitializeSubViewModels()
        {
            Canvas = new CanvasSettingsViewModel(Settings.Canvas, SaveSettings);
            Appearance = new AppearanceSettingsViewModel(Settings.Appearance, SaveSettings);
            Gesture = new GestureSettingsViewModel(Settings.Gesture, SaveSettings);
            Startup = new StartupSettingsViewModel(Settings.Startup, SaveSettings);
            Advanced = new AdvancedSettingsViewModel(Settings.Advanced, SaveSettings);
            Automation = new AutomationSettingsViewModel(Settings.Automation, SaveSettings);
            PowerPoint = new PowerPointSettingsViewModel(Settings.PowerPointSettings, SaveSettings);
            Snapshot = new SnapshotSettingsViewModel(Settings.Snapshot, SaveSettings);
            InkToShape = new InkToShapeSettingsViewModel(Settings.InkToShape, SaveSettings);
        }

        /// <summary>
        /// 设置变更事件处理
        /// </summary>
        private void OnSettingsChanged(object sender, SettingsChangedEventArgs e)
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
        private void ResetSettings()
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
            _settingsService.SettingsChanged -= OnSettingsChanged;
            base.Cleanup();
        }
    }
}