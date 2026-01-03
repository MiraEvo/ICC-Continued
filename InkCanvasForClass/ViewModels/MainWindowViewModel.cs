using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ink_Canvas.Core;
using Ink_Canvas.Services;
using System;
using System.Windows;

namespace Ink_Canvas.ViewModels
{
    /// <summary>
    /// ICC 工具枚举
    /// </summary>
    public enum ICCToolsEnum
    {
        CursorMode,
        PenMode,
        EraseByStrokeMode,
        EraseByGeometryMode,
        LassoMode,
    }

    /// <summary>
    /// 当前应用模式
    /// </summary>
    public enum AppMode
    {
        /// <summary>
        /// 桌面模式（透明批注）
        /// </summary>
        Desktop = 0,

        /// <summary>
        /// 白板/黑板模式
        /// </summary>
        Whiteboard = 1
    }

    /// <summary>
    /// MainWindow ViewModel - 管理主窗口状态和命令
    /// </summary>
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly ISettingsService _settingsService;
        private readonly ITimeMachineService _timeMachineService;
        private readonly IPageService _pageService;

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        public MainWindowViewModel(
            ISettingsService settingsService,
            ITimeMachineService timeMachineService,
            IPageService pageService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _timeMachineService = timeMachineService ?? throw new ArgumentNullException(nameof(timeMachineService));
            _pageService = pageService ?? throw new ArgumentNullException(nameof(pageService));

            // 订阅服务事件
            _timeMachineService.UndoStateChanged += OnUndoStateChanged;
            _timeMachineService.RedoStateChanged += OnRedoStateChanged;
            _pageService.PageChanged += OnPageChanged;
        }

        /// <summary>
        /// 默认构造函数（用于设计时或从 ServiceLocator 获取服务）
        /// </summary>
        public MainWindowViewModel() : this(
            ServiceLocator.GetRequiredService<ISettingsService>(),
            ServiceLocator.GetRequiredService<ITimeMachineService>(),
            ServiceLocator.GetRequiredService<IPageService>())
        {
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
        [NotifyPropertyChangedFor(nameof(IsAnyEraserMode))]
        [NotifyPropertyChangedFor(nameof(IsInkingMode))]
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

        /// <summary>
        /// 是否为任意擦除模式
        /// </summary>
        public bool IsAnyEraserMode => IsEraseByStrokeMode || IsEraseByGeometryMode;

        /// <summary>
        /// 是否在书写模式（非光标模式）
        /// </summary>
        public bool IsInkingMode => SelectedTool != ICCToolsEnum.CursorMode;

        #endregion

        #region 应用模式状态

        /// <summary>
        /// 当前应用模式
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsDesktopMode))]
        [NotifyPropertyChangedFor(nameof(IsWhiteboardMode))]
        private AppMode _currentAppMode = AppMode.Desktop;

        /// <summary>
        /// 是否为桌面模式
        /// </summary>
        public bool IsDesktopMode => CurrentAppMode == AppMode.Desktop;

        /// <summary>
        /// 是否为白板模式
        /// </summary>
        public bool IsWhiteboardMode => CurrentAppMode == AppMode.Whiteboard;

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

        #region 页面状态

        /// <summary>
        /// 当前页面索引
        /// </summary>
        [ObservableProperty]
        private int _currentPageIndex;

        /// <summary>
        /// 页面总数
        /// </summary>
        [ObservableProperty]
        private int _totalPages;

        /// <summary>
        /// 页面信息显示文本
        /// </summary>
        public string PageInfoText => $"{CurrentPageIndex + 1} / {TotalPages}";

        private void OnPageChanged(object sender, PageChangedEventArgs e)
        {
            CurrentPageIndex = e.NewIndex;
            TotalPages = e.TotalPages;
            OnPropertyChanged(nameof(PageInfoText));
        }

        #endregion

        #region 特殊功能状态

        /// <summary>
        /// 是否开启画面定格
        /// </summary>
        [ObservableProperty]
        private bool _isFreezeEnabled;

        /// <summary>
        /// 是否开启单指漫游
        /// </summary>
        [ObservableProperty]
        private bool _isSingleFingerDragMode;

        /// <summary>
        /// 是否开启双指手势
        /// </summary>
        [ObservableProperty]
        private bool _isTwoFingerGestureEnabled;

        /// <summary>
        /// 是否在形状绘制模式
        /// </summary>
        [ObservableProperty]
        private bool _isInShapeDrawingMode;

        /// <summary>
        /// 是否显示浮动工具栏
        /// </summary>
        [ObservableProperty]
        private bool _isFloatingBarVisible = true;

        /// <summary>
        /// 浮动工具栏是否折叠
        /// </summary>
        [ObservableProperty]
        private bool _isFloatingBarFolded;

        /// <summary>
        /// 是否显示画布控件区
        /// </summary>
        [ObservableProperty]
        private bool _isCanvasControlsVisible;

        /// <summary>
        /// 是否在PPT演示模式
        /// </summary>
        [ObservableProperty]
        private bool _isInPPTMode;

        #endregion

        #region UI 可见性

        /// <summary>
        /// 设置面板是否可见
        /// </summary>
        [ObservableProperty]
        private bool _isSettingsPanelVisible;

        /// <summary>
        /// 工具面板是否可见
        /// </summary>
        [ObservableProperty]
        private bool _isToolsPanelVisible;

        /// <summary>
        /// 画笔调色盘是否可见
        /// </summary>
        [ObservableProperty]
        private bool _isPenPaletteVisible;

        /// <summary>
        /// 橡皮大小面板是否可见
        /// </summary>
        [ObservableProperty]
        private bool _isEraserSizePanelVisible;

        /// <summary>
        /// 形状绘制面板是否可见
        /// </summary>
        [ObservableProperty]
        private bool _isShapeDrawPanelVisible;

        /// <summary>
        /// 手势设置面板是否可见
        /// </summary>
        [ObservableProperty]
        private bool _isGesturePanelVisible;

        #endregion

        #region 工具切换命令

        /// <summary>
        /// 切换到光标模式
        /// </summary>
        [RelayCommand]
        private void SwitchToCursor()
        {
            SelectedTool = ICCToolsEnum.CursorMode;
            HideAllSubPanels();
        }

        /// <summary>
        /// 切换到画笔模式
        /// </summary>
        [RelayCommand]
        private void SwitchToPen()
        {
            SelectedTool = ICCToolsEnum.PenMode;
            HideAllSubPanels();
        }

        /// <summary>
        /// 切换到线擦模式
        /// </summary>
        [RelayCommand]
        private void SwitchToEraseByStroke()
        {
            SelectedTool = ICCToolsEnum.EraseByStrokeMode;
            HideAllSubPanels();
        }

        /// <summary>
        /// 切换到板擦模式
        /// </summary>
        [RelayCommand]
        private void SwitchToEraseByGeometry()
        {
            SelectedTool = ICCToolsEnum.EraseByGeometryMode;
            HideAllSubPanels();
        }

        /// <summary>
        /// 切换到套索选择模式
        /// </summary>
        [RelayCommand]
        private void SwitchToLasso()
        {
            SelectedTool = ICCToolsEnum.LassoMode;
            HideAllSubPanels();
        }

        #endregion

        #region 撤销/重做命令

        /// <summary>
        /// 撤销命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanUndo))]
        private void Undo()
        {
            _timeMachineService.Undo();
            HideSubPanelsRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 重做命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanRedo))]
        private void Redo()
        {
            _timeMachineService.Redo();
            HideSubPanelsRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 隐藏子面板请求事件
        /// </summary>
        public event EventHandler HideSubPanelsRequested;

        #endregion

        #region 页面命令

        /// <summary>
        /// 上一页命令
        /// </summary>
        [RelayCommand]
        private void PreviousPage()
        {
            _pageService.GoPrevious();
        }

        /// <summary>
        /// 下一页命令
        /// </summary>
        [RelayCommand]
        private void NextPage()
        {
            _pageService.GoNext();
        }

        /// <summary>
        /// 添加新页面命令
        /// </summary>
        [RelayCommand]
        private void AddPage()
        {
            _pageService.AddPage();
        }

        /// <summary>
        /// 删除当前页面命令
        /// </summary>
        [RelayCommand]
        private void DeleteCurrentPage()
        {
            _pageService.DeleteCurrentPage();
        }

        #endregion

        #region 模式切换命令

        /// <summary>
        /// 切换白板/桌面模式命令
        /// </summary>
        [RelayCommand]
        private void ToggleWhiteboardMode()
        {
            CurrentAppMode = CurrentAppMode == AppMode.Desktop ? AppMode.Whiteboard : AppMode.Desktop;
        }

        /// <summary>
        /// 进入白板模式命令
        /// </summary>
        [RelayCommand]
        private void EnterWhiteboardMode()
        {
            CurrentAppMode = AppMode.Whiteboard;
        }

        /// <summary>
        /// 退出白板模式命令
        /// </summary>
        [RelayCommand]
        private void ExitWhiteboardMode()
        {
            CurrentAppMode = AppMode.Desktop;
        }

        #endregion

        #region 特殊功能命令

        /// <summary>
        /// 切换画面定格命令
        /// </summary>
        [RelayCommand]
        private void ToggleFreeze()
        {
            IsFreezeEnabled = !IsFreezeEnabled;
        }

        /// <summary>
        /// 切换单指漫游命令
        /// </summary>
        [RelayCommand]
        private void ToggleSingleFingerDrag()
        {
            IsSingleFingerDragMode = !IsSingleFingerDragMode;
        }

        /// <summary>
        /// 切换双指手势命令
        /// </summary>
        [RelayCommand]
        private void ToggleTwoFingerGesture()
        {
            IsTwoFingerGestureEnabled = !IsTwoFingerGestureEnabled;
        }

        #endregion

        #region UI 命令

        /// <summary>
        /// 显示/隐藏设置面板命令
        /// </summary>
        [RelayCommand]
        private void ToggleSettingsPanel()
        {
            IsSettingsPanelVisible = !IsSettingsPanelVisible;
            if (IsSettingsPanelVisible)
            {
                HideAllSubPanels();
                IsSettingsPanelVisible = true;
            }
        }

        /// <summary>
        /// 显示/隐藏工具面板命令
        /// </summary>
        [RelayCommand]
        private void ToggleToolsPanel()
        {
            IsToolsPanelVisible = !IsToolsPanelVisible;
        }

        /// <summary>
        /// 显示/隐藏画笔调色盘命令
        /// </summary>
        [RelayCommand]
        private void TogglePenPalette()
        {
            IsPenPaletteVisible = !IsPenPaletteVisible;
        }

        /// <summary>
        /// 显示/隐藏橡皮大小面板命令
        /// </summary>
        [RelayCommand]
        private void ToggleEraserSizePanel()
        {
            IsEraserSizePanelVisible = !IsEraserSizePanelVisible;
        }

        /// <summary>
        /// 显示/隐藏形状绘制面板命令
        /// </summary>
        [RelayCommand]
        private void ToggleShapeDrawPanel()
        {
            IsShapeDrawPanelVisible = !IsShapeDrawPanelVisible;
        }

        /// <summary>
        /// 隐藏所有子面板
        /// </summary>
        [RelayCommand]
        private void HideAllSubPanels()
        {
            IsSettingsPanelVisible = false;
            IsToolsPanelVisible = false;
            IsPenPaletteVisible = false;
            IsEraserSizePanelVisible = false;
            IsShapeDrawPanelVisible = false;
            IsGesturePanelVisible = false;
        }

        /// <summary>
        /// 折叠/展开浮动工具栏命令
        /// </summary>
        [RelayCommand]
        private void ToggleFloatingBarFold()
        {
            IsFloatingBarFolded = !IsFloatingBarFolded;
        }

        #endregion

        #region 热键命令

        /// <summary>
        /// 清空画布命令
        /// </summary>
        [RelayCommand]
        private void ClearCanvas()
        {
            // 此命令将触发 ClearCanvasRequested 事件，由 View 处理实际清空逻辑
            ClearCanvasRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 清空画布请求事件（用于与 View 交互）
        /// </summary>
        public event EventHandler ClearCanvasRequested;

        /// <summary>
        /// 截图命令
        /// </summary>
        [RelayCommand]
        private void Capture()
        {
            CaptureRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 截图请求事件
        /// </summary>
        public event EventHandler CaptureRequested;

        /// <summary>
        /// 隐藏/显示窗口命令
        /// </summary>
        [RelayCommand]
        private void ToggleHide()
        {
            IsFloatingBarVisible = !IsFloatingBarVisible;
            ToggleHideRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 隐藏请求事件
        /// </summary>
        public event EventHandler ToggleHideRequested;

        /// <summary>
        /// 退出应用命令
        /// </summary>
        [RelayCommand]
        private void ExitApp()
        {
            ExitAppRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 退出应用请求事件
        /// </summary>
        public event EventHandler ExitAppRequested;

        /// <summary>
        /// 进入绘图工具模式命令（从光标模式切换到画笔模式）
        /// </summary>
        [RelayCommand]
        private void ChangeToDrawTool()
        {
            if (SelectedTool == ICCToolsEnum.CursorMode)
            {
                SelectedTool = ICCToolsEnum.PenMode;
            }
        }

        /// <summary>
        /// 退出绘图工具模式命令（从画笔模式切换到光标模式）
        /// </summary>
        [RelayCommand]
        private void QuitDrawTool()
        {
            SelectedTool = ICCToolsEnum.CursorMode;
        }

        /// <summary>
        /// 切换到选择模式命令
        /// </summary>
        [RelayCommand]
        private void ChangeToSelect()
        {
            SelectedTool = ICCToolsEnum.LassoMode;
        }

        /// <summary>
        /// 切换到橡皮模式命令
        /// </summary>
        [RelayCommand]
        private void ChangeToEraser()
        {
            // 根据设置决定使用线擦还是板擦
            if (SelectedTool == ICCToolsEnum.EraseByStrokeMode)
            {
                SelectedTool = ICCToolsEnum.EraseByGeometryMode;
            }
            else
            {
                SelectedTool = ICCToolsEnum.EraseByStrokeMode;
            }
        }

        /// <summary>
        /// 进入画板模式命令
        /// </summary>
        [RelayCommand]
        private void ChangeToBoard()
        {
            ToggleWhiteboardMode();
        }

        /// <summary>
        /// 切换到画笔1命令
        /// </summary>
        [RelayCommand]
        private void ChangeToPen1()
        {
            SelectedTool = ICCToolsEnum.PenMode;
            ChangeToPenRequested?.Invoke(this, 0);
        }

        /// <summary>
        /// 切换到画笔2命令
        /// </summary>
        [RelayCommand]
        private void ChangeToPen2()
        {
            SelectedTool = ICCToolsEnum.PenMode;
            ChangeToPenRequested?.Invoke(this, 1);
        }

        /// <summary>
        /// 切换到画笔3命令
        /// </summary>
        [RelayCommand]
        private void ChangeToPen3()
        {
            SelectedTool = ICCToolsEnum.PenMode;
            ChangeToPenRequested?.Invoke(this, 2);
        }

        /// <summary>
        /// 切换到画笔4命令
        /// </summary>
        [RelayCommand]
        private void ChangeToPen4()
        {
            SelectedTool = ICCToolsEnum.PenMode;
            ChangeToPenRequested?.Invoke(this, 3);
        }

        /// <summary>
        /// 切换到画笔5命令
        /// </summary>
        [RelayCommand]
        private void ChangeToPen5()
        {
            SelectedTool = ICCToolsEnum.PenMode;
            ChangeToPenRequested?.Invoke(this, 4);
        }

        /// <summary>
        /// 切换画笔请求事件，参数为画笔索引
        /// </summary>
        public event EventHandler<int> ChangeToPenRequested;

        /// <summary>
        /// 绘制直线命令
        /// </summary>
        [RelayCommand]
        private void DrawLine()
        {
            IsInShapeDrawingMode = true;
            DrawLineRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 绘制直线请求事件
        /// </summary>
        public event EventHandler DrawLineRequested;

        #endregion

        #region 清理

        /// <summary>
        /// 清理资源
        /// </summary>
        public override void Cleanup()
        {
            _timeMachineService.UndoStateChanged -= OnUndoStateChanged;
            _timeMachineService.RedoStateChanged -= OnRedoStateChanged;
            _pageService.PageChanged -= OnPageChanged;
            base.Cleanup();
        }

        #endregion
    }
}