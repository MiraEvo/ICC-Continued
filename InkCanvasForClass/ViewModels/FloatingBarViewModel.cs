using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ink_Canvas.Core;
using Ink_Canvas.Services;
using System;

namespace Ink_Canvas.ViewModels
{
    /// <summary>
    /// FloatingBar ViewModel - 管理浮动工具栏状态和命令
    /// </summary>
    public partial class FloatingBarViewModel : ViewModelBase
    {
        private readonly ISettingsService _settingsService;
        private readonly ITimeMachineService _timeMachineService;

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="settingsService">设置服务</param>
        /// <param name="timeMachineService">时光机服务</param>
        public FloatingBarViewModel(
            ISettingsService settingsService,
            ITimeMachineService timeMachineService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _timeMachineService = timeMachineService ?? throw new ArgumentNullException(nameof(timeMachineService));

            // 订阅服务事件
            _timeMachineService.UndoStateChanged += OnUndoStateChanged;
            _timeMachineService.RedoStateChanged += OnRedoStateChanged;
        }

        #endregion

        #region 工具状态

        /// <summary>
        /// 当前选中的工具
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsCursorMode))]
        [NotifyPropertyChangedFor(nameof(IsPenMode))]
        [NotifyPropertyChangedFor(nameof(IsEraseByStrokeMode))]
        [NotifyPropertyChangedFor(nameof(IsEraseByGeometryMode))]
        [NotifyPropertyChangedFor(nameof(IsLassoMode))]
        private ICCToolsEnum _selectedTool = ICCToolsEnum.CursorMode;

        /// <summary>
        /// 是否为光标模式
        /// </summary>
        public bool IsCursorMode => SelectedTool == ICCToolsEnum.CursorMode;

        /// <summary>
        /// 是否为画笔模式
        /// </summary>
        public bool IsPenMode => SelectedTool == ICCToolsEnum.PenMode;

        /// <summary>
        /// 是否为线擦模式
        /// </summary>
        public bool IsEraseByStrokeMode => SelectedTool == ICCToolsEnum.EraseByStrokeMode;

        /// <summary>
        /// 是否为板擦模式
        /// </summary>
        public bool IsEraseByGeometryMode => SelectedTool == ICCToolsEnum.EraseByGeometryMode;

        /// <summary>
        /// 是否为套索选择模式
        /// </summary>
        public bool IsLassoMode => SelectedTool == ICCToolsEnum.LassoMode;

        #endregion

        #region 显示状态

        /// <summary>
        /// 浮动工具栏是否最小化
        /// </summary>
        [ObservableProperty]
        private bool _isMinimized;

        /// <summary>
        /// 是否在PPT模式
        /// </summary>
        [ObservableProperty]
        private bool _isPPTMode;

        /// <summary>
        /// 浮动工具栏是否可见
        /// </summary>
        [ObservableProperty]
        private bool _isVisible = true;

        /// <summary>
        /// 浮动工具栏透明度
        /// </summary>
        [ObservableProperty]
        private double _opacity = 1.0;

        /// <summary>
        /// 浮动工具栏缩放比例
        /// </summary>
        [ObservableProperty]
        private double _scale = 1.0;

        #endregion

        #region 撤销/重做状态

        /// <summary>
        /// 是否可以撤销
        /// </summary>
        [ObservableProperty]
        private bool _canUndo;

        /// <summary>
        /// 是否可以重做
        /// </summary>
        [ObservableProperty]
        private bool _canRedo;

        private void OnUndoStateChanged(bool canUndo)
        {
            CanUndo = canUndo;
        }

        private void OnRedoStateChanged(bool canRedo)
        {
            CanRedo = canRedo;
        }

        #endregion

        #region 工具选择命令

        /// <summary>
        /// 工具变更事件
        /// </summary>
        public event EventHandler<ToolChangedEventArgs> ToolChanged;

        /// <summary>
        /// 选择工具命令
        /// </summary>
        [RelayCommand]
        private void SelectTool(ICCToolsEnum tool)
        {
            var oldTool = SelectedTool;
            SelectedTool = tool;
            ToolChanged?.Invoke(this, new ToolChangedEventArgs(oldTool, tool));
        }

        /// <summary>
        /// 切换到光标模式命令
        /// </summary>
        [RelayCommand]
        private void SwitchToCursor()
        {
            SelectTool(ICCToolsEnum.CursorMode);
        }

        /// <summary>
        /// 切换到画笔模式命令
        /// </summary>
        [RelayCommand]
        private void SwitchToPen()
        {
            SelectTool(ICCToolsEnum.PenMode);
        }

        /// <summary>
        /// 切换到线擦模式命令
        /// </summary>
        [RelayCommand]
        private void SwitchToEraseByStroke()
        {
            SelectTool(ICCToolsEnum.EraseByStrokeMode);
        }

        /// <summary>
        /// 切换到板擦模式命令
        /// </summary>
        [RelayCommand]
        private void SwitchToEraseByGeometry()
        {
            SelectTool(ICCToolsEnum.EraseByGeometryMode);
        }

        /// <summary>
        /// 切换到套索选择模式命令
        /// </summary>
        [RelayCommand]
        private void SwitchToLasso()
        {
            SelectTool(ICCToolsEnum.LassoMode);
        }

        #endregion

        #region 撤销/重做命令

        /// <summary>
        /// 撤销请求事件
        /// </summary>
        public event EventHandler UndoRequested;

        /// <summary>
        /// 重做请求事件
        /// </summary>
        public event EventHandler RedoRequested;

        /// <summary>
        /// 撤销命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanUndo))]
        private void Undo()
        {
            UndoRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 重做命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanRedo))]
        private void Redo()
        {
            RedoRequested?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region 清空命令

        /// <summary>
        /// 清空画布请求事件
        /// </summary>
        public event EventHandler ClearRequested;

        /// <summary>
        /// 清空画布命令
        /// </summary>
        [RelayCommand]
        private void Clear()
        {
            ClearRequested?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region 截图命令

        /// <summary>
        /// 截图请求事件
        /// </summary>
        public event EventHandler ScreenshotRequested;

        /// <summary>
        /// 截图命令
        /// </summary>
        [RelayCommand]
        private void Screenshot()
        {
            ScreenshotRequested?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region 设置命令

        /// <summary>
        /// 设置请求事件
        /// </summary>
        public event EventHandler SettingsRequested;

        /// <summary>
        /// 打开设置命令
        /// </summary>
        [RelayCommand]
        private void Settings()
        {
            SettingsRequested?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region 退出命令

        /// <summary>
        /// 退出请求事件
        /// </summary>
        public event EventHandler ExitRequested;

        /// <summary>
        /// 退出应用命令
        /// </summary>
        [RelayCommand]
        private void Exit()
        {
            ExitRequested?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region 最小化命令

        /// <summary>
        /// 切换最小化命令
        /// </summary>
        [RelayCommand]
        private void ToggleMinimize()
        {
            IsMinimized = !IsMinimized;
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 更新撤销/重做状态（由外部调用）
        /// </summary>
        public void UpdateUndoRedoState(bool canUndo, bool canRedo)
        {
            CanUndo = canUndo;
            CanRedo = canRedo;
            
            // 通知命令状态变化
            UndoCommand.NotifyCanExecuteChanged();
            RedoCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// 更新PPT模式状态（由外部调用）
        /// </summary>
        public void UpdatePPTMode(bool isPPTMode)
        {
            IsPPTMode = isPPTMode;
            
            // 根据PPT模式调整透明度
            if (isPPTMode && _settingsService?.Settings?.Appearance != null)
            {
                // 使用PPT模式下的透明度设置
                Opacity = _settingsService.Settings.Appearance.ViewboxFloatingBarOpacityInPPTValue;
            }
            else if (_settingsService?.Settings?.Appearance != null)
            {
                // 使用正常模式下的透明度设置
                Opacity = _settingsService.Settings.Appearance.ViewboxFloatingBarOpacityValue;
            }
        }

        /// <summary>
        /// 更新缩放比例（由外部调用）
        /// </summary>
        public void UpdateScale(double scale)
        {
            Scale = scale;
        }

        /// <summary>
        /// 更新透明度（由外部调用）
        /// </summary>
        public void UpdateOpacity(double opacity)
        {
            Opacity = opacity;
        }

        #endregion

        #region 清理

        /// <summary>
        /// 清理资源
        /// </summary>
        public override void Cleanup()
        {
            _timeMachineService.UndoStateChanged -= OnUndoStateChanged;
            _timeMachineService.RedoStateChanged -= OnRedoStateChanged;
            base.Cleanup();
        }

        #endregion
    }

    /// <summary>
    /// 工具变更事件参数
    /// </summary>
    public class ToolChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 旧工具
        /// </summary>
        public ICCToolsEnum OldTool { get; }

        /// <summary>
        /// 新工具
        /// </summary>
        public ICCToolsEnum NewTool { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public ToolChangedEventArgs(ICCToolsEnum oldTool, ICCToolsEnum newTool)
        {
            OldTool = oldTool;
            NewTool = newTool;
        }
    }
}
