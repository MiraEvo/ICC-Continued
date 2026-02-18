using Ink_Canvas.Core;
using Ink_Canvas.Helpers;
using Ink_Canvas.Services;
using Ink_Canvas.ViewModels;
using Ink_Canvas.Views.Settings;
using Ink_Canvas.Services.Events;
using Ink_Canvas.Models.Settings;
using OSVersionExtension;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using File = System.IO.File;
using MessageBox = System.Windows.MessageBox;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Controls.Primitives;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Text;
using System.Windows.Documents;
using Ink_Canvas.Popups;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using Vanara.PInvoke;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using static Wpf.Ui.Appearance.ApplicationThemeManager;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using TextBox = System.Windows.Controls.TextBox;

namespace Ink_Canvas {
    /// <summary>
    /// MainWindow 主窗口类
    ///
    /// 架构说明：
    /// - 此类采用 MVVM 模式，通过 ViewModel 管理 UI 状态和命令
    /// - 业务逻辑已迁移到服务层（Services）
    /// - 事件处理程序作为 View 回调，委托给 ViewModel 或服务层
    /// - 使用 partial class 将功能分散到多个文件中（MainWindow.Partials 目录）
    ///
    /// 迁移状态：
    /// - ViewModel 初始化和事件订阅已完成
    /// - 工具按钮命令已绑定到 ViewModel
    /// - 设置面板导航已通过事件绑定
    /// - 白板页面管理已通过 ViewModel 事件处理
    /// </summary>
    public partial class MainWindow : System.Windows.Window {
        public Services.ISettingsService SettingsService => (Services.ISettingsService)((App)Application.Current).Services.GetService(typeof(Services.ISettingsService));
        public Settings Settings => SettingsService.Settings;

        public void SaveSettings() {
            SettingsService.Save();
        }

        private void LoadSettings() {
            SettingsService.Load();
        }

        #region ViewModel Properties

        /// <summary>
        /// 主窗口 ViewModel
        /// </summary>
        public MainWindowViewModel ViewModel { get; private set; }

        /// <summary>
        /// 工具栏 ViewModel
        /// </summary>
        public ToolbarViewModel ToolbarVM { get; private set; }

        /// <summary>
        /// 设置 ViewModel
        /// </summary>
        public SettingsViewModel SettingsVM { get; private set; }

        /// <summary>
        /// 浮动工具栏 ViewModel
        /// </summary>
        private FloatingBarViewModel _floatingBarViewModel;

        /// <summary>
        /// 黑板/白板 ViewModel
        /// </summary>
        public BlackboardViewModel BlackboardVM { get; private set; }

        /// <summary>
        /// 触摸事件 ViewModel
        /// </summary>
        public TouchEventsViewModel TouchEventsVM { get; private set; }

        /// <summary>
        /// 热键服务
        /// </summary>
        private IHotkeyService _hotkeyService;

        #endregion

        [RequiresUnmanagedCode("Uses user32 SetWindowPos for forced fullscreen behavior.")]
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property.Name == nameof(Topmost) && isLoaded) {
                if (Topmost && Settings.Advanced.IsEnableForceFullScreen) {
                    Trace.WriteLine("Topmost true");
                    SetWindowPos(new WindowInteropHelper(this).Handle, new IntPtr(-1), 0, 0,
                        System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width,
                        System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height, 0x0040);
                } else if (!Topmost && Settings.Advanced.IsEnableForceFullScreen) {
                    Trace.WriteLine("Topmost false");
                    SetWindowPos(new WindowInteropHelper(this).Handle, new IntPtr(-2), 0, 0,
                        System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width,
                        System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height, 0x0040);
                }
            }
        }

        #region Window Initialization

        public MainWindow() {
            /*
                处于画板模式内：Topmost == false / currentMode != 0
                处于 PPT 放映内：BtnPPTSlideShowEnd.Visibility
            */

            // 初始化 ViewModel
            InitializeViewModels();

            InitializeComponent();

            // 设置 DataContext
            DataContext = ViewModel;

            // 初始化 FloatingBarView
            InitializeFloatingBarView();

            BlackboardLeftSide.Visibility = Visibility.Collapsed;
            BlackboardCenterSide.Visibility = Visibility.Collapsed;
            BlackboardRightSide.Visibility = Visibility.Collapsed;
            BorderTools.Visibility = Visibility.Collapsed;
            //BorderSettings.Visibility = Visibility.Collapsed;
            LeftSidePanelForPPTNavigation.Visibility = Visibility.Collapsed;
            RightSidePanelForPPTNavigation.Visibility = Visibility.Collapsed;
            TwoFingerGestureBorder.Visibility = Visibility.Collapsed;
            BoardTwoFingerGestureBorder.Visibility = Visibility.Collapsed;
            BorderDrawShape.Visibility = Visibility.Collapsed;
            BoardBorderDrawShape.Visibility = Visibility.Collapsed;
            GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;

            ViewboxFloatingBar.Margin = new Thickness((SystemParameters.WorkArea.Width - Constants.FloatingBarWidth) / 2,
                SystemParameters.WorkArea.Height - Constants.FloatingBarBottomMarginPPT,
                Constants.FloatingBarHiddenHorizontalOffset, Constants.FloatingBarHiddenVerticalOffset);
            ViewboxFloatingBarMarginAnimation(Constants.FloatingBarBottomMarginNormal, true);

            try {
                if (File.Exists("debug.ini")) Label.Visibility = Visibility.Visible;
            }
            catch (Exception ex) {
                LogHelper.WriteLogToFile(ex.ToString(), LogHelper.LogType.Error);
            }

            try {
                if (File.Exists("Log.txt")) {
                    var fileInfo = new FileInfo("Log.txt");
                    var fileSizeInKB = fileInfo.Length / Constants.BytesToKilobytes;
                    if (fileSizeInKB > Constants.LogFileSizeThresholdKB)
                        try {
                            File.Delete("Log.txt");
                            LogHelper.WriteLogToFile(
                                "Log.txt 已删除，原文件大小：" + fileSizeInKB +
                                " KB", LogHelper.LogType.Info);
                        }
                        catch (Exception ex) {
                            LogHelper.WriteLogToFile(
                                ex + " | 无法删除 Log.txt，文件大小：" + fileSizeInKB + " KB",
                                LogHelper.LogType.Error);
                        }
                }
            }
            catch (Exception ex) {
                LogHelper.WriteLogToFile(ex.ToString(), LogHelper.LogType.Error);
            }

            InitTimers();
            timeMachine.OnRedoStateChanged += TimeMachine_OnRedoStateChanged;
            timeMachine.OnUndoStateChanged += TimeMachine_OnUndoStateChanged;
            inkCanvas.Strokes.StrokesChanged += StrokesOnStrokesChanged;

            Microsoft.Win32.SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
            try {
                if (File.Exists("SpecialVersion.ini")) ApplySpecialVersionSettings();
            }
            catch (Exception ex) {
                LogHelper.WriteLogToFile(ex.ToString(), LogHelper.LogType.Error);
            }

            // 注意：CheckColorTheme和CheckPenTypeUIState已移至Window_Loaded中，
            // 在LoadSettings之后调用，以确保设置已加载
        }

        /// <summary>
        /// 初始化 FloatingBarView
        /// </summary>
        private void InitializeFloatingBarView()
        {
            if (FloatingBarView != null)
            {
                // 设置 FloatingBarView 的 ViewModel
                _floatingBarViewModel = ServiceLocator.GetService<FloatingBarViewModel>();
                if (_floatingBarViewModel != null)
                {
                    FloatingBarView.ViewModel = _floatingBarViewModel;

                    // 从设置中加载浮动工具栏的缩放和透明度
                    var settingsService = ServiceLocator.GetService<ISettingsService>();
                    if (settingsService != null)
                    {
                        _floatingBarViewModel.Scale = settingsService.Settings.Appearance.ViewboxFloatingBarScaleTransformValue;
                        _floatingBarViewModel.Opacity = settingsService.Settings.Appearance.ViewboxFloatingBarOpacityValue;
                    }
                }
            }
        }

        #endregion

        #region Ink Canvas Functions

        private System.Windows.Media.Color Ink_DefaultColor = Colors.Red;

        private DrawingAttributes drawingAttributes;

        private void loadPenCanvas() {
            try {
                //drawingAttributes = new DrawingAttributes();
                drawingAttributes = inkCanvas.DefaultDrawingAttributes;
                drawingAttributes.Color = Ink_DefaultColor;


                drawingAttributes.Height = 2;
                drawingAttributes.Width = 2;
                drawingAttributes.IsHighlighter = false;
                drawingAttributes.FitToCurve = Settings.Canvas.FitToCurve;

                inkCanvas.EditingMode = InkCanvasEditingMode.Ink;


                //inkCanvas.Gesture += InkCanvas_Gesture;
            }
            catch (Exception ex) {
                LogHelper.WriteLogToFile("初始化画笔画布失败：" + ex.Message, LogHelper.LogType.Error);
            }
        }

        private void inkCanvas_EditingModeChanged(object sender, RoutedEventArgs e) {
            if (sender is not InkCanvas inkCanvas1) return;
            if (Settings.Canvas.IsShowCursor) {
                if (inkCanvas1.EditingMode == InkCanvasEditingMode.Ink || drawingShapeMode != 0)
                    inkCanvas1.ForceCursor = true;
                else
                    inkCanvas1.ForceCursor = false;
            } else {
                inkCanvas1.ForceCursor = false;
            }

            if (inkCanvas1.EditingMode == InkCanvasEditingMode.Ink) forcePointEraser = !forcePointEraser;

            if ((inkCanvas1.EditingMode == InkCanvasEditingMode.EraseByPoint &&
                 SelectedMode == ICCToolsEnum.EraseByGeometryMode) || (inkCanvas1.EditingMode == InkCanvasEditingMode.EraseByStroke &&
                                                                       SelectedMode == ICCToolsEnum.EraseByStrokeMode)) {
                GridEraserOverlay.Visibility = Visibility.Visible;
            } else {
                GridEraserOverlay.Visibility = Visibility.Collapsed;
            }

            inkCanvas1.EditingModeInverted = inkCanvas1.EditingMode;

            RectangleSelectionHitTestBorder.Visibility = inkCanvas1.EditingMode == InkCanvasEditingMode.Select ? Visibility.Visible : Visibility.Collapsed;
        }

        #endregion Ink Canvas

        #region Definitions and Loading

        // 使用 SettingsService 管理的 Settings 实例
        // public Settings Settings => ServiceLocator.GetService<ISettingsService>()?.Settings ?? new Settings();
        public bool isLoaded = false;

        [LibraryImport("user32.dll")]
        public static partial IntPtr GetSystemMenu(IntPtr hWnd, [MarshalAs(UnmanagedType.Bool)] bool bRevert);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);

        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, UInt32 uFlags);

        const uint MF_BYCOMMAND = 0x00000000;
        const uint MF_GRAYED = 0x00000001;
        const uint SC_CLOSE = 0xF060;

        private static void PreloadIALibrary() {
            try {
                // 使用新的 Windows.UI.Input.Inking.Analysis API 预热
                _ = InkRecognizeHelper.PreloadAsync();
                LogHelper.WriteLogToFile("墨迹分析 API 预热已启动", LogHelper.LogType.Info);
            }
            catch (Exception ex) {
                LogHelper.WriteLogToFile("预热 IA 库失败：" + ex.Message, LogHelper.LogType.Error);
            }
        }

        private void ForceDesktopTransparentStartupState(string phase)
        {
            // Startup guard: keep overlay in desktop transparent state even if theme/window managers write background values.
            Background = System.Windows.Media.Brushes.Transparent;
            if (Main_Grid != null)
            {
                Main_Grid.Background = System.Windows.Media.Brushes.Transparent;
            }

            if (GridTransparencyFakeBackground != null)
            {
                GridTransparencyFakeBackground.Opacity = 0;
                GridTransparencyFakeBackground.Background = System.Windows.Media.Brushes.Transparent;
            }

            if (GridBackgroundCover != null)
            {
                GridBackgroundCover.Visibility = Visibility.Collapsed;
            }

            if (GridBackgroundCoverHolder != null)
            {
                GridBackgroundCoverHolder.Visibility = Visibility.Collapsed;
            }

            currentMode = 0;
            Topmost = true;

            LogHelper.WriteLogToFile(
                $"[StartupTransparency:{phase}] AllowsTransparency={AllowsTransparency}, MainWindowBackground={Background}, MainGridBackground={Main_Grid?.Background}, FakeOpacity={GridTransparencyFakeBackground?.Opacity}, FakeBackground={GridTransparencyFakeBackground?.Background}, CoverHolder={GridBackgroundCoverHolder?.Visibility}, Cover={GridBackgroundCover?.Visibility}, Mode={currentMode}",
                LogHelper.LogType.Info);
        }

        [RequiresUnmanagedCode("Uses user32 GetSystemMenu/EnableMenuItem for window system menu adjustments.")]
        private void Window_Loaded(object sender, RoutedEventArgs e) {
            loadPenCanvas();
            //加载设置
            // LoadSettings(true); // 已移除旧版加载逻辑
            var settingsService = ServiceLocator.GetService<ISettingsService>();
            if (settingsService != null)
            {
                settingsService.Load();
            }

            // 在设置加载后更新UI状态
            CheckColorTheme(true);
            CheckPenTypeUIState();

            // 执行启动任务
             PerformStartupTasks();

            // HasNewUpdateWindow hasNewUpdateWindow = new HasNewUpdateWindow();
            // 注意：旧版 IA 库不支持 64 位，但新的 Windows.UI.Input.Inking.Analysis API 支持 x64
            // 因此移除了 64 位进程检查

            EnsureMainWindowTransparentBackground();
            SystemEvents_UserPreferenceChanged(null, null);
            ForceDesktopTransparentStartupState("loaded");
            Dispatcher.BeginInvoke(new Action(() => ForceDesktopTransparentStartupState("context-idle")), DispatcherPriority.ContextIdle);
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(300);
                    await Dispatcher.InvokeAsync(() => ForceDesktopTransparentStartupState("delay-300ms"));
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile("Startup delay task failed: " + ex.Message, LogHelper.LogType.Error);
                }
            });

            //TextBlockVersion.Text = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            LogHelper.WriteLogToFile("Ink Canvas 已加载", LogHelper.LogType.Event);

            isLoaded = true;

            BlackBoardLeftSidePageListView.ItemsSource = blackBoardSidePageListViewObservableCollection;
            BlackBoardRightSidePageListView.ItemsSource = blackBoardSidePageListViewObservableCollection;

            BtnLeftWhiteBoardSwitchPreviousGeometry.Brush =
                new SolidColorBrush(Constants.ButtonDisabledColor);
            BtnLeftWhiteBoardSwitchPreviousLabel.Opacity = Constants.ButtonDisabledOpacity;
            BtnRightWhiteBoardSwitchPreviousGeometry.Brush =
                new SolidColorBrush(Constants.ButtonDisabledColor);
            BtnRightWhiteBoardSwitchPreviousLabel.Opacity = Constants.ButtonDisabledOpacity;

            BorderInkReplayToolBox.Visibility = Visibility.Collapsed;
            BoardBackgroundPopup.Visibility = Visibility.Collapsed;

            // 提前加载IA库，优化第一笔等待时间
            Task.Run(() => {
                try {
                    PreloadIALibrary();
                }
                catch (Exception ex) {
                    LogHelper.WriteLogToFile("后台预热 IA 库失败：" + ex.Message, LogHelper.LogType.Error);
                }
            });

            SystemEvents.DisplaySettingsChanged += SystemEventsOnDisplaySettingsChanged;

            if (Settings.Advanced.IsDisableCloseWindow) {
                // Disable close button
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                IntPtr hMenu = GetSystemMenu(hwnd, false);
                if (hMenu != IntPtr.Zero) {
                    EnableMenuItem(hMenu, SC_CLOSE, MF_BYCOMMAND | MF_GRAYED);
                }
            }

            UpdateFloatingBarIconsLayout();

            PenPaletteV2Init();
            SelectionV2Init();
            ShapeDrawingV2Init();

            InitStorageManagementModule();

            InitFreezeWindow([
                new HWND(new WindowInteropHelper(this).Handle)
            ]);

            UpdateIndexInfoDisplay();

             try {
                if (File.Exists("SpecialVersion.ini")) ApplySpecialVersionSettings();
            }
            catch (Exception ex) {
                LogHelper.WriteLogToFile(ex.ToString(), LogHelper.LogType.Error);
            }

            // 初始化外观设置（浮动工具栏文字和托盘图标）
            ApplyFloatingBarButtonLabelVisibility();
            ApplyTrayIconVisibility();
        }

        /// <summary>
        /// 执行启动时的任务（迁移自 LoadSettings(true)）
        /// </summary>
        private void PerformStartupTasks()
        {
            try
            {
                // 默认切换到光标模式
                CursorIcon_Click(null, null);

                if (Settings.Startup != null)
                {
                    if (Settings.Automation.AutoDelSavedFiles)
                    {
                        // 注意：需确保 DelAutoSavedFiles 类可用
                         DelAutoSavedFiles.DeleteFilesOlder(Settings.Automation.AutoSavedStrokesLocation,
                            Settings.Automation.AutoDelSavedFilesDaysThreshold);
                    }

                    if (Settings.Startup.IsFoldAtStartup)
                    {
                        FoldFloatingBar_MouseUp(Fold_Icon, null);
                    }

                    if (Settings.Startup.IsEnableNibMode)
                    {
                        BoundsWidth = Settings.Advanced.NibModeBoundsWidth;
                    }
                    else
                    {
                        BoundsWidth = Settings.Advanced.FingerModeBoundsWidth;
                    }

                    if (Settings.Startup.IsAutoUpdate)
                    {
                         AutoUpdate();
                    }
                }

                // 应用设置到应用状态
                if (Settings.Canvas != null)
                {
                     // 恢复笔设置
                    lastPenType = Settings.Canvas.LastPenType;
                    penType = lastPenType;
                    lastDesktopInkColor = Settings.Canvas.LastDesktopInkColor;
                    lastBoardInkColor = Settings.Canvas.LastBoardInkColor;
                    highlighterColor = Settings.Canvas.LastHighlighterColor;
                    lastPenWidth = Settings.Canvas.InkWidth;
                    lastHighlighterWidth = Settings.Canvas.HighlighterWidth;

                    // 同步ColorPalette的笔模式和笔粗细
                    if (PenPaletteV2 != null)
                    {
                        PenPaletteV2.PenModeSelected = penType == 1 ? ColorPalette.PenMode.HighlighterMode : ColorPalette.PenMode.PenMode;
                        PenPaletteV2.SelectedPenWidth = penType == 1 ? Settings.Canvas.HighlighterWidth : Settings.Canvas.InkWidth;
                    }

                    // 根据笔类型设置绘图属性
                     if (penType == 1) {
                         // 荧光笔模式
                        drawingAttributes.Width = Settings.Canvas.HighlighterWidth / 2;
                        drawingAttributes.Height = Settings.Canvas.HighlighterWidth;
                        drawingAttributes.StylusTip = StylusTip.Rectangle;
                        drawingAttributes.IsHighlighter = true;
                    }
                    else
                    {
                        drawingAttributes.Width = Settings.Canvas.InkWidth;
                        drawingAttributes.Height = Settings.Canvas.InkWidth;
                    }

                    if (Settings.Canvas.IsShowCursor) {
                        inkCanvas.ForceCursor = true;
                    } else {
                        inkCanvas.ForceCursor = false;
                    }

                     CheckEraserTypeTab();

                    if (Settings.Canvas.FitToCurve) {
                        drawingAttributes.FitToCurve = true;
                    } else {
                        drawingAttributes.FitToCurve = false;
                    }

                    if (SelectionV2 != null)
                    {
                        SelectionV2.SelectionModeSelected = (SelectionPopup.SelectionMode)Settings.Canvas.SelectionMethod;
                        SelectionV2.ApplyScaleToStylusTip = Settings.Canvas.ApplyScaleToStylusTip;
                        SelectionV2.OnlyHitTestFullyContainedStrokes = Settings.Canvas.OnlyHitTestFullyContainedStrokes;
                        SelectionV2.AllowClickToSelectLockedStroke = Settings.Canvas.AllowClickToSelectLockedStroke;
                    }
                }

                 if (Settings.Advanced != null) {
                    if (Settings.Advanced.IsEnableFullScreenHelper) {
                        FullScreenHelper.MarkFullscreenWindowTaskbarList(new WindowInteropHelper(this).Handle, true);
                    }

                    if (Settings.Advanced.IsEnableEdgeGestureUtil) {
                        if (OSVersion.GetOperatingSystem() >= OSVersionExtension.OperatingSystem.Windows10)
                            EdgeGestureUtil.DisableEdgeGestures(new WindowInteropHelper(this).Handle, true);
                    }
                }

                if (Settings.InkToShape != null && PenPaletteV2 != null) {
                    PenPaletteV2.InkRecognition = Settings.InkToShape.IsInkToShapeEnabled;
                }

                // 初始化调色盘的压感模拟状态
                if (PenPaletteV2 != null)
                {
                    switch (Settings.Canvas.InkStyle) {
                        case -1:
                            PenPaletteV2.SimulatePressure = ColorPalette.PressureSimulation.None;
                            break;
                        case 0:
                            PenPaletteV2.SimulatePressure = ColorPalette.PressureSimulation.PointSimulate;
                            break;
                        case 1:
                            PenPaletteV2.SimulatePressure = ColorPalette.PressureSimulation.VelocitySimulate;
                            break;
                        default:
                            PenPaletteV2.SimulatePressure = ColorPalette.PressureSimulation.PointSimulate;
                            Settings.Canvas.InkStyle = 0;
                            break;
                    }
                }

                if (Settings.Automation != null)
                {
                     if (Settings.Automation.IsAutoKillEasiNote || Settings.Automation.IsAutoKillPptService ||
                        Settings.Automation.IsAutoKillHiteAnnotation || Settings.Automation.IsAutoKillInkCanvas
                        || Settings.Automation.IsAutoKillICA || Settings.Automation.IsAutoKillIDT ||
                        Settings.Automation.IsAutoKillVComYouJiao
                        || Settings.Automation.IsAutoKillSeewoLauncher2DesktopAnnotation) {
                        timerKillProcess.Start();
                    } else {
                        timerKillProcess.Stop();
                    }
                }

                if (Settings.PowerPointSettings != null) {
                    if (Settings.PowerPointSettings.PowerPointSupport) {
                        timerCheckPPT.Start();
                    } else {
                        timerCheckPPT.Stop();
                    }
                }

                 if (Settings.Gesture != null) {
                    // 初始化现代化的手掌橡皮擦服务
                    InitializePalmEraserService();

                    if (Settings.Gesture.IsEnableMultiTouchMode) {
                        if (!isInMultiTouchMode) {
                            inkCanvas.StylusDown += MainWindow_StylusDown;
                            inkCanvas.StylusMove += MainWindow_StylusMove;
                            inkCanvas.StylusUp += MainWindow_StylusUp;
                            inkCanvas.TouchDown += MainWindow_TouchDown;
                            inkCanvas.TouchDown -= Main_Grid_TouchDown;
                            inkCanvas.EditingMode = InkCanvasEditingMode.None;
                            inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
                            inkCanvas.Children.Clear();
                            isInMultiTouchMode = true;
                        }
                    } else {
                        if (isInMultiTouchMode) {
                            inkCanvas.StylusDown -= MainWindow_StylusDown;
                            inkCanvas.StylusMove -= MainWindow_StylusMove;
                            inkCanvas.StylusUp -= MainWindow_StylusUp;
                            inkCanvas.TouchDown -= MainWindow_TouchDown;
                            inkCanvas.TouchDown += Main_Grid_TouchDown;
                            inkCanvas.EditingMode = InkCanvasEditingMode.None;
                            inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
                            inkCanvas.Children.Clear();
                            isInMultiTouchMode = false;
                        }
                    }

                    CheckEnableTwoFingerGestureBtnColorPrompt();
                }

                // auto align
                if (BorderFloatingBarExitPPTBtn.Visibility == Visibility.Visible) {
                    ViewboxFloatingBarMarginAnimation(60);
                } else {
                    ViewboxFloatingBarMarginAnimation(100, true);
                }

            }
            catch (Exception ex)
            {
                 LogHelper.WriteLogToFile("执行启动任务失败：" + ex.Message, LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 应用特殊版本推荐设置
        /// </summary>
        private async Task ApplySpecialVersionSettingsAsync()
        {
            await Task.Delay(1000);
            try {
                isLoaded = false;

                var settingsService = ServiceLocator.GetService<ISettingsService>();
                if (settingsService != null)
                {
                    settingsService.ResetToDefaults();

                    // 应用特殊版本设置
                    Settings.Advanced.IsSpecialScreen = true;
                    Settings.Advanced.IsQuadIR = false;
                    Settings.Advanced.TouchMultiplier = 0.3;
                    Settings.Advanced.NibModeBoundsWidth = 5;
                    Settings.Advanced.FingerModeBoundsWidth = 20;
                    Settings.Advanced.EraserBindTouchMultiplier = true;
                    Settings.Advanced.IsLogEnabled = true;
                    Settings.Advanced.IsEnableEdgeGestureUtil = false;
                    Settings.Advanced.EdgeGestureUtilOnlyAffectBlackboardMode = false;
                    Settings.Advanced.IsEnableFullScreenHelper = false;
                    Settings.Advanced.IsEnableForceFullScreen = false;
                    Settings.Advanced.IsEnableDPIChangeDetection = false;
                    Settings.Advanced.IsEnableResolutionChangeDetection = false;
                    Settings.Advanced.IsDisableCloseWindow = true;
                    Settings.Advanced.EnableForceTopMost = false;

                    Settings.Appearance.IsEnableDisPlayNibModeToggler = false;
                    Settings.Appearance.IsColorfulViewboxFloatingBar = false;
                    Settings.Appearance.ViewboxFloatingBarScaleTransformValue = 1;
                    Settings.Appearance.EnableViewboxBlackBoardScaleTransform = false;
                    Settings.Appearance.IsTransparentButtonBackground = true;
                    Settings.Appearance.IsShowExitButton = true;
                    Settings.Appearance.IsShowEraserButton = true;
                    Settings.Appearance.IsShowHideControlButton = false;
                    Settings.Appearance.IsShowLRSwitchButton = false;
                    Settings.Appearance.IsShowModeFingerToggleSwitch = true;
                    Settings.Appearance.IsShowQuickPanel = true;
                    Settings.Appearance.Theme = 0;
                    Settings.Appearance.EnableChickenSoupInWhiteboardMode = true;
                    Settings.Appearance.EnableTimeDisplayInWhiteboardMode = true;
                    Settings.Appearance.ChickenSoupSource = 1;
                    Settings.Appearance.ViewboxFloatingBarOpacityValue = 1.0;
                    Settings.Appearance.ViewboxFloatingBarOpacityInPPTValue = 1.0;
                    Settings.Appearance.EnableTrayIcon = true;
                    Settings.Appearance.FloatingBarButtonLabelVisibility = true;
                    Settings.Appearance.FloatingBarIconsVisibility = "11111111";
                    Settings.Appearance.EraserButtonsVisibility = 0;
                    Settings.Appearance.OnlyDisplayEraserBtn = false;

                    // Automation
                    Settings.Automation.IsAutoFoldInEasiNote = true;
                    Settings.Automation.IsAutoFoldInEasiNoteIgnoreDesktopAnno = true;
                    Settings.Automation.IsAutoFoldInEasiCamera = true;
                    Settings.Automation.IsAutoFoldInEasiNote3C = false;
                    Settings.Automation.IsAutoFoldInEasiNote3 = false;
                    Settings.Automation.IsAutoFoldInEasiNote5C = true;
                    Settings.Automation.IsAutoFoldInSeewoPincoTeacher = false;
                    Settings.Automation.IsAutoFoldInHiteTouchPro = false;
                    Settings.Automation.IsAutoFoldInHiteCamera = false;
                    Settings.Automation.IsAutoFoldInWxBoardMain = false;
                    Settings.Automation.IsAutoFoldInOldZyBoard = false;
                    Settings.Automation.IsAutoFoldInMSWhiteboard = false;
                    Settings.Automation.IsAutoFoldInAdmoxWhiteboard = false;
                    Settings.Automation.IsAutoFoldInAdmoxBooth = false;
                    Settings.Automation.IsAutoFoldInQPoint = false;
                    Settings.Automation.IsAutoFoldInYiYunVisualPresenter = false;
                    Settings.Automation.IsAutoFoldInMaxHubWhiteboard = false;
                    Settings.Automation.IsAutoFoldInPPTSlideShow = false;
                    Settings.Automation.IsAutoKillPptService = false;
                    Settings.Automation.IsAutoKillEasiNote = false;
                    Settings.Automation.IsAutoKillVComYouJiao = false;
                    Settings.Automation.IsAutoKillInkCanvas = false;
                    Settings.Automation.IsAutoKillICA = false;
                    Settings.Automation.IsAutoKillIDT = true;
                    Settings.Automation.IsAutoKillSeewoLauncher2DesktopAnnotation = false;
                    Settings.Automation.IsSaveScreenshotsInDateFolders = false;
                    Settings.Automation.IsAutoSaveStrokesAtScreenshot = true;
                    Settings.Automation.IsAutoSaveStrokesAtClear = true;
                    Settings.Automation.IsAutoClearWhenExitingWritingMode = false;
                    Settings.Automation.MinimumAutomationStrokeNumber = 0;
                    Settings.Automation.IsEnableLimitAutoSaveAmount = false;
                    Settings.Automation.LimitAutoSaveAmount = 3;
                    // Special version overrides
                    Settings.Automation.AutoDelSavedFiles = true;
                    Settings.Automation.AutoDelSavedFilesDaysThreshold = 15;

                    Settings.PowerPointSettings.PowerPointSupport = true;
                    Settings.PowerPointSettings.IsShowCanvasAtNewSlideShow = false;
                    Settings.PowerPointSettings.IsNoClearStrokeOnSelectWhenInPowerPoint = true;
                    Settings.PowerPointSettings.IsShowStrokeOnSelectInPowerPoint = false;
                    Settings.PowerPointSettings.IsAutoSaveStrokesInPowerPoint = true;
                    Settings.PowerPointSettings.IsAutoSaveScreenShotInPowerPoint = true;
                    Settings.PowerPointSettings.IsNotifyPreviousPage = false;
                    Settings.PowerPointSettings.IsNotifyHiddenPage = false;
                    Settings.PowerPointSettings.IsEnableTwoFingerGestureInPresentationMode = false;
                    Settings.PowerPointSettings.IsSupportWPS = true;
                    Settings.PowerPointSettings.RegistryShowBlackScreenLastSlideShow = false;
                    Settings.PowerPointSettings.RegistryShowSlideShowToolbar = false;

                    Settings.Canvas.InkWidth = 2;
                    Settings.Canvas.IsShowCursor = false;
                    Settings.Canvas.InkStyle = 0;
                    Settings.Canvas.HighlighterWidth = 20;
                    Settings.Canvas.EraserSize = 1;
                    Settings.Canvas.EraserType = 0;
                    Settings.Canvas.EraserShapeType = 1;
                    Settings.Canvas.HideStrokeWhenSelecting = false;
                    Settings.Canvas.ClearCanvasAndClearTimeMachine = false;
                    Settings.Canvas.FitToCurve = false;
                    Settings.Canvas.HyperbolaAsymptoteOption = 0;
                    Settings.Canvas.BlackboardBackgroundColor = BlackboardBackgroundColorEnum.White;
                    Settings.Canvas.BlackboardBackgroundPattern = BlackboardBackgroundPatternEnum.None;
                    Settings.Canvas.IsEnableAutoConvertInkColorWhenBackgroundChanged = false;
                    Settings.Canvas.UseDefaultBackgroundColorForEveryNewAddedBlackboardPage = false;
                    Settings.Canvas.UseDefaultBackgroundPatternForEveryNewAddedBlackboardPage = false;
                    Settings.Canvas.SelectionMethod = 0;
                    Settings.Canvas.ApplyScaleToStylusTip = false;
                    Settings.Canvas.OnlyHitTestFullyContainedStrokes = false;
                    Settings.Canvas.AllowClickToSelectLockedStroke = false;

                    Settings.Gesture.AutoSwitchTwoFingerGesture = true;
                    Settings.Gesture.IsEnableTwoFingerTranslate = true;
                    Settings.Gesture.IsEnableTwoFingerZoom = false;
                    Settings.Gesture.IsEnableTwoFingerRotation = false;
                    Settings.Gesture.IsEnableTwoFingerRotationOnSelection = false;
                    Settings.Gesture.DisableGestureEraser = true;
                    Settings.Gesture.DefaultMultiPointHandWritingMode = 2;
                    Settings.Gesture.HideCursorWhenUsingTouchDevice = true;
                    Settings.Gesture.EnableMouseGesture = true;
                    Settings.Gesture.EnableMouseRightBtnGesture = true;
                    Settings.Gesture.EnableMouseWheelGesture = true;

                    Settings.InkToShape.IsInkToShapeEnabled = true;
                    Settings.InkToShape.IsInkToShapeNoFakePressureRectangle = false;
                    Settings.InkToShape.IsInkToShapeNoFakePressureTriangle = false;
                    Settings.InkToShape.IsInkToShapeTriangle = true;
                    Settings.InkToShape.IsInkToShapeRectangle = true;
                    Settings.InkToShape.IsInkToShapeRounded = true;

                    Settings.Startup.IsEnableNibMode = false;
                    Settings.Startup.IsAutoUpdate = false;
                    Settings.Startup.IsAutoUpdateWithSilence = true;
                    Settings.Startup.AutoUpdateWithSilenceStartTime = "18:20";
                    Settings.Startup.AutoUpdateWithSilenceEndTime = "07:40";
                    Settings.Startup.IsFoldAtStartup = false;
                    Settings.Startup.EnableWindowChromeRendering = false;

                    Settings.Snapshot.CopyScreenshotToClipboard = true;
                    Settings.Snapshot.AttachInkWhenScreenshot = true;
                    Settings.Snapshot.OnlySnapshotMaximizeWindow = false;
                    Settings.Snapshot.ScreenshotFileName = "Screenshot-[YYYY]-[MM]-[DD]-[HH]-[mm]-[ss].png";
                    Settings.Snapshot.ScreenshotUsingMagnificationAPI = false;

                    Settings.Storage.StorageLocation = "fr";
                    Settings.Storage.UserStorageLocation = "";

                    // 同步设置 AutoSavedStrokesLocation 为安装目录下的 Data 文件夹
                    var runfolder = AppDomain.CurrentDomain.BaseDirectory;
                    Settings.Automation.AutoSavedStrokesLocation =
                        (runfolder.EndsWith('\\') ? runfolder[..^1] : runfolder) + "\\Data";

                    settingsService.Save();
                }

                isLoaded = true;
            }
            catch (Exception ex) {
                LogHelper.WriteLogToFile("应用特殊版本设置失败：" + ex.Message, LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 应用特殊版本推荐设置（事件处理器包装）
        /// </summary>
        private async void ApplySpecialVersionSettings()
        {
            try {
                await ApplySpecialVersionSettingsAsync();
            }
            catch (Exception ex) {
                LogHelper.WriteLogToFile("应用特殊版本设置失败：" + ex.Message, LogHelper.LogType.Error);
            }
        }

        private void SystemEventsOnDisplaySettingsChanged(object? sender, EventArgs e) {
            if (!Settings.Advanced.IsEnableResolutionChangeDetection) return;
            ShowNotification($"检测到显示器信息变化，变为{System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width}x{System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height}");
            new Thread(() => {
                var isFloatingBarOutsideScreen = false;
                var isInPPTPresentationMode = false;
                Dispatcher.Invoke(() => {
                    isFloatingBarOutsideScreen = IsOutsideOfScreenHelper.IsOutsideOfScreen(ViewboxFloatingBar);
                    isInPPTPresentationMode = BorderFloatingBarExitPPTBtn.Visibility == Visibility.Visible;
                });
                if (isFloatingBarOutsideScreen) dpiChangedDelayAction.DebounceAction(Constants.DpiChangeDelayMilliseconds, null, () => {
                    if (!isFloatingBarFolded)
                    {
                        if (isInPPTPresentationMode) ViewboxFloatingBarMarginAnimation(Constants.FloatingBarBottomMarginPPT);
                        else ViewboxFloatingBarMarginAnimation(Constants.FloatingBarBottomMarginNormal, true);
                    }
                });
            }).Start();
        }

        public DelayAction dpiChangedDelayAction = new();

        private void MainWindow_OnDpiChanged(object sender, System.Windows.DpiChangedEventArgs e)
        {
            if (e.OldDpi.DpiScaleX != e.NewDpi.DpiScaleX && e.OldDpi.DpiScaleY != e.NewDpi.DpiScaleY && Settings.Advanced.IsEnableDPIChangeDetection)
            {
                ShowNotification($"系统DPI发生变化，从 {e.OldDpi.DpiScaleX}x{e.OldDpi.DpiScaleY} 变化为 {e.NewDpi.DpiScaleX}x{e.NewDpi.DpiScaleY}");

                new Thread(() => {
                    var isFloatingBarOutsideScreen = false;
                    var isInPPTPresentationMode = false;
                    Dispatcher.Invoke(() => {
                        isFloatingBarOutsideScreen = IsOutsideOfScreenHelper.IsOutsideOfScreen(ViewboxFloatingBar);
                        isInPPTPresentationMode = BorderFloatingBarExitPPTBtn.Visibility == Visibility.Visible;
                    });
                    if (isFloatingBarOutsideScreen) dpiChangedDelayAction.DebounceAction(Constants.DpiChangeDelayMilliseconds, null, () => {
                        if (!isFloatingBarFolded)
                        {
                            if (isInPPTPresentationMode) ViewboxFloatingBarMarginAnimation(Constants.FloatingBarBottomMarginPPT);
                            else ViewboxFloatingBarMarginAnimation(Constants.FloatingBarBottomMarginNormal, true);
                        }
                    });
                }).Start();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            LogHelper.WriteLogToFile("Ink Canvas 正在关闭", LogHelper.LogType.Event);
            if (!CloseIsFromButton) {
                e.Cancel = true;
            }

            if (e.Cancel) LogHelper.WriteLogToFile("Ink Canvas 关闭已取消", LogHelper.LogType.Event);
            else {
                // 使用 Task 包装清理操作，设置超时以避免卡住
                var cleanupTask = Task.Run(() => {
                    try {
                        // 停止墨迹回放线程
                        isStopInkReplay = true;

                        // 停止所有定时器，防止进程残留
                        StopTimerSafely(timerCheckPPT, TimerCheckPPT_Elapsed);
                        StopTimerSafely(timerKillProcess, TimerKillProcess_Elapsed);
                        StopTimerSafely(timerCheckAutoFold, TimerCheckAutoFold_Elapsed);
                        StopTimerSafely(timerCheckAutoUpdateWithSilence, TimerCheckAutoUpdateWithSilence_Elapsed);
                        StopTimerSafely(timerDisplayTime, TimerDisplayTime_Elapsed);
                        StopTimerSafely(timerDisplayDate, TimerDisplayDate_Elapsed);
                    }
                    catch { /* 忽略错误 */ }
                });

                // 等待清理任务最多 500ms
                cleanupTask.Wait(500);

                // 在 UI 线程上执行必要的清理
                try {
                    DisposeFreezeFrame();
                }
                catch { /* 忽略错误 */ }

                // 取消系统事件订阅
                try {
                    Microsoft.Win32.SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
                }
                catch { /* 忽略错误 */ }

                try {
                    SystemEvents.DisplaySettingsChanged -= SystemEventsOnDisplaySettingsChanged;
                }
                catch { /* 忽略错误 */ }

                // 取消 InkCanvas 和 TimeMachine 事件订阅
                try {
                    if (timeMachine != null) {
                        timeMachine.OnRedoStateChanged -= TimeMachine_OnRedoStateChanged;
                        timeMachine.OnUndoStateChanged -= TimeMachine_OnUndoStateChanged;
                    }
                }
                catch { /* 忽略错误 */ }

                try {
                    if (inkCanvas != null && inkCanvas.Strokes != null) {
                        inkCanvas.Strokes.StrokesChanged -= StrokesOnStrokesChanged;
                    }
                }
                catch { /* 忽略错误 */ }

                // 异步释放 PPT COM 对象（设置超时避免卡住）
                var pptCleanupTask = Task.Run(() => {
                    try {
                        ReleasePptComObjects();
                    }
                    catch { /* 忽略错误 */ }
                });

                // 等待 PPT 清理最多 300ms
                pptCleanupTask.Wait(300);

                // 取消 ViewModel 事件订阅
                try {
                    UnsubscribeViewModelEvents();
                    CleanupAppearanceSettingsHandler();
                }
                catch { /* 忽略错误 */ }

                // 释放托盘图标
                try {
                    TrayIcon?.Dispose();
                }
                catch { /* 忽略错误 */ }

                LogHelper.WriteLogToFile("Ink Canvas 关闭：清理完成", LogHelper.LogType.Event);

                Application.Current.Shutdown();
            }
        }

        /// <summary>
        /// 安全停止定时器
        /// </summary>
        private void StopTimerSafely(System.Timers.Timer timer, System.Timers.ElapsedEventHandler handler) {
            try {
                if (timer != null) {
                    timer.Stop();
                    timer.Elapsed -= handler;
                    timer.Dispose();
                }
            }
            catch { /* 忽略错误 */ }
        }

        public void DisposeTrayIcon()
        {
            TrayIcon?.Dispose();
        }

        /// <summary>
        /// 释放 PPT COM 对象
        /// </summary>
        private void ReleasePptComObjects() {
            try {
                if (pptApplication != null) {
                    try {
                        pptApplication.PresentationOpen -= PptApplication_PresentationOpen;
                        pptApplication.PresentationClose -= PptApplication_PresentationClose;
                        pptApplication.SlideShowBegin -= PptApplication_SlideShowBegin;
                        pptApplication.SlideShowNextSlide -= PptApplication_SlideShowNextSlide;
                        pptApplication.SlideShowEnd -= PptApplication_SlideShowEnd;
                    }
                    catch { /* 忽略错误 */ }

                    try {
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(pptApplication);
                    }
                    catch { /* 忽略错误 */ }
                    pptApplication = null;
                }

                if (presentation != null) {
                    try {
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(presentation);
                    }
                    catch { /* 忽略错误 */ }
                    presentation = null;
                }

                if (slides != null) {
                    try {
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(slides);
                    }
                    catch { /* 忽略错误 */ }
                    slides = null;
                }

                if (slide != null) {
                    try {
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(slide);
                    }
                    catch { /* 忽略错误 */ }
                    slide = null;
                }
            }
            catch { /* 忽略错误 */ }
        }

        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, [MarshalAs(UnmanagedType.Bool)] bool bRepaint);

        [RequiresUnmanagedCode("Uses user32 MoveWindow for forced fullscreen behavior.")]
        private void MainWindow_OnSizeChanged(object sender, SizeChangedEventArgs e) {
            if (Settings.Advanced.IsEnableForceFullScreen) {
                if (isLoaded) ShowNotification(
                    $"检测到窗口大小变化，已自动恢复到全屏：{System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width}x{System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height}（缩放比例为{System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width / SystemParameters.PrimaryScreenWidth}x{System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height / SystemParameters.PrimaryScreenHeight}）");
                WindowState = WindowState.Maximized;
                MoveWindow(new WindowInteropHelper(this).Handle, 0, 0,
                    System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width,
                    System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height, true);
            }
        }

        private void Window_Closed(object sender, EventArgs e) {
            LogHelper.WriteLogToFile("Ink Canvas 已关闭", LogHelper.LogType.Event);
        }

        private void DisplayWelcomePopup() {
            if (Ookii.Dialogs.Wpf.TaskDialog.OSSupportsTaskDialogs) {
                var t = new Thread(() => {
                    try {
                        using var dialog = new Ookii.Dialogs.Wpf.TaskDialog();
                        dialog.WindowTitle = "InkCanvasForClass";
                        dialog.MainInstruction = "已重置为建议设置";
                        dialog.Content = "为了方便在不同环境下获得最佳体验，我们已经将所有选项重置。完成后建议您重启应用。";

                        dialog.Footer = "您以后也可以在 “设置” 界面手动更改这些选项。";

                        dialog.FooterIcon = Ookii.Dialogs.Wpf.TaskDialogIcon.Information;
                        dialog.EnableHyperlinks = true;
                        var okButton = new Ookii.Dialogs.Wpf.TaskDialogButton(Ookii.Dialogs.Wpf.ButtonType.Ok);
                        dialog.Buttons.Add(okButton);
                        dialog.Show();
                    } catch { /* 忽略错误 */ }
                });
                t.Start();
            }
        }

        private async Task AutoUpdateAsync() {
            AvailableLatestVersion = await AutoUpdateHelper.CheckForUpdates();

            if (AvailableLatestVersion != null) {
                var isDownloadSuccessful = await AutoUpdateHelper.DownloadSetupFileAndSaveStatus(AvailableLatestVersion);

                if (isDownloadSuccessful) {
                    if (!Settings.Startup.IsAutoUpdateWithSilence) {
                        if (MessageBox.Show("InkCanvasForClass 新版本安装包已下载完成，是否立即更新？",
                                "InkCanvasForClass New Version Available", System.Windows.MessageBoxButton.YesNo,
                                MessageBoxImage.Question) ==
                            System.Windows.MessageBoxResult.Yes) AutoUpdateHelper.InstallNewVersionApp(AvailableLatestVersion, false);
                    } else {
                        timerCheckAutoUpdateWithSilence.Start();
                    }
                }
            } else {
                AutoUpdateHelper.DeleteUpdatesFolder();
            }
        }

        private async void AutoUpdate() {
            try {
                await AutoUpdateAsync();
            }
            catch (Exception ex) {
                LogHelper.WriteLogToFile($"自动更新检查失败: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        #endregion Definations and Loading
    }
}
