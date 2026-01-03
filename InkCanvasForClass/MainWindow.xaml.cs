using Ink_Canvas.Core;
using Ink_Canvas.Helpers;
using Ink_Canvas.ViewModels;
using iNKORE.UI.WPF.Modern;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media;
using System.Diagnostics;
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
using iNKORE.UI.WPF.Modern.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using Vanara.PInvoke;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using TextBox = System.Windows.Controls.TextBox;

namespace Ink_Canvas {
    public partial class MainWindow : Window {

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
        /// 初始化 ViewModels，从 ServiceLocator 中获取实例
        /// </summary>
        private void InitializeViewModels()
        {
            try
            {
                ViewModel = ServiceLocator.GetRequiredService<MainWindowViewModel>();
                ToolbarVM = ServiceLocator.GetRequiredService<ToolbarViewModel>();
                SettingsVM = ServiceLocator.GetRequiredService<SettingsViewModel>();
                
                // 订阅 ViewModel 事件
                SubscribeViewModelEvents();
                
                LogHelper.WriteLogToFile("ViewModels initialized successfully", LogHelper.LogType.Info);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile("Failed to initialize ViewModels: " + ex.Message, LogHelper.LogType.Error);
                throw;
            }
        }

        /// <summary>
        /// 订阅 ViewModel 事件
        /// </summary>
        private void SubscribeViewModelEvents()
        {
            // 清空画布请求
            ViewModel.ClearCanvasRequested += OnClearCanvasRequested;
            
            // 截图请求
            ViewModel.CaptureRequested += OnCaptureRequested;
            
            // 隐藏/显示请求
            ViewModel.ToggleHideRequested += OnToggleHideRequested;
            
            // 退出应用请求
            ViewModel.ExitAppRequested += OnExitAppRequested;
            
            // 画笔切换请求
            ViewModel.ChangeToPenRequested += OnChangeToPenRequested;
            
            // 绘制直线请求
            ViewModel.DrawLineRequested += OnDrawLineRequested;
            
            // 隐藏子面板请求
            ViewModel.HideSubPanelsRequested += OnHideSubPanelsRequested;
            
            // 工具按钮点击事件 - 每次点击都会触发，用于处理弹窗切换
            ViewModel.ToolButtonClicked += OnToolButtonClicked;
            
            // 撤销/重做请求 - 由 View 执行实际操作
            ViewModel.UndoRequested += OnUndoRequested;
            ViewModel.RedoRequested += OnRedoRequested;
        }
        
        /// <summary>
        /// 处理工具按钮点击 - 每次点击都会调用对应的 Click 方法
        /// 这样可以正确处理弹窗的显示/隐藏逻辑
        /// </summary>
        private void OnToolButtonClicked(object sender, ViewModels.ICCToolsEnum tool)
        {
            // 根据工具类型调用原有的工具切换方法
            // 这些方法内部已经有逻辑来处理：
            // 1. 从其他工具切换到此工具时，进入该工具模式
            // 2. 再次点击已选中的工具时，切换弹窗的显示/隐藏
            switch (tool)
            {
                case ViewModels.ICCToolsEnum.CursorMode:
                    CursorIcon_Click(null, null);
                    break;
                case ViewModels.ICCToolsEnum.PenMode:
                    PenIcon_Click(null, null);
                    break;
                case ViewModels.ICCToolsEnum.EraseByStrokeMode:
                    EraserIconByStrokes_Click(null, null);
                    break;
                case ViewModels.ICCToolsEnum.EraseByGeometryMode:
                    EraserIcon_Click(null, null);
                    break;
                case ViewModels.ICCToolsEnum.LassoMode:
                    SymbolIconSelect_MouseUp(null, null);
                    break;
            }
        }
        
        /// <summary>
        /// 处理隐藏子面板请求
        /// </summary>
        private void OnHideSubPanelsRequested(object sender, EventArgs e)
        {
            HideSubPanels();
        }
        
        /// <summary>
        /// 处理撤销请求 - 调用原有的撤销方法
        /// </summary>
        private void OnUndoRequested(object sender, EventArgs e)
        {
            BtnUndo_Click(null, null);
        }
        
        /// <summary>
        /// 处理重做请求 - 调用原有的重做方法
        /// </summary>
        private void OnRedoRequested(object sender, EventArgs e)
        {
            BtnRedo_Click(null, null);
        }

        /// <summary>
        /// 取消订阅 ViewModel 事件
        /// </summary>
        private void UnsubscribeViewModelEvents()
        {
            if (ViewModel != null)
            {
                ViewModel.ClearCanvasRequested -= OnClearCanvasRequested;
                ViewModel.CaptureRequested -= OnCaptureRequested;
                ViewModel.ToggleHideRequested -= OnToggleHideRequested;
                ViewModel.ExitAppRequested -= OnExitAppRequested;
                ViewModel.ChangeToPenRequested -= OnChangeToPenRequested;
                ViewModel.DrawLineRequested -= OnDrawLineRequested;
                ViewModel.HideSubPanelsRequested -= OnHideSubPanelsRequested;
                ViewModel.ToolButtonClicked -= OnToolButtonClicked;
                ViewModel.UndoRequested -= OnUndoRequested;
                ViewModel.RedoRequested -= OnRedoRequested;
            }
        }

        #region ViewModel 事件处理

        /// <summary>
        /// 处理清空画布请求
        /// </summary>
        private void OnClearCanvasRequested(object sender, EventArgs e)
        {
            // 调用现有的清空逻辑
            try
            {
                // 这里调用现有的清空画布方法
                // 在后续重构中会将此方法迁移到服务层
                if (inkCanvas.Strokes.Count > 0)
                {
                    timeMachine.CommitStrokeEraseHistory(inkCanvas.Strokes);
                    inkCanvas.Strokes.Clear();
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile("OnClearCanvasRequested failed: " + ex.Message, LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 处理截图请求
        /// </summary>
        private void OnCaptureRequested(object sender, EventArgs e)
        {
            // 调用现有的截图逻辑
            try
            {
                // 这里调用现有的截图方法
                // KeyCapture 或相关方法
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile("OnCaptureRequested failed: " + ex.Message, LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 处理隐藏/显示请求
        /// </summary>
        private void OnToggleHideRequested(object sender, EventArgs e)
        {
            // 调用现有的隐藏/显示逻辑
            try
            {
                // 这里调用现有的隐藏/显示方法
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile("OnToggleHideRequested failed: " + ex.Message, LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 处理退出应用请求
        /// </summary>
        private void OnExitAppRequested(object sender, EventArgs e)
        {
            // 调用现有的退出逻辑
            try
            {
                CloseIsFromButton = true;
                Close();
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile("OnExitAppRequested failed: " + ex.Message, LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 处理画笔切换请求
        /// </summary>
        private void OnChangeToPenRequested(object sender, int penIndex)
        {
            try
            {
                // 这里调用现有的画笔切换方法
                // 例如：SwitchToPen(penIndex)
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile("OnChangeToPenRequested failed: " + ex.Message, LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 处理绘制直线请求
        /// </summary>
        private void OnDrawLineRequested(object sender, EventArgs e)
        {
            try
            {
                // 这里调用现有的绘制直线方法
                // 例如：SwitchToShapeDrawing(ShapeType.Line)
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile("OnDrawLineRequested failed: " + ex.Message, LogHelper.LogType.Error);
            }
        }

        #endregion

        #endregion

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

            BlackboardLeftSide.Visibility = Visibility.Collapsed;
            BlackboardCenterSide.Visibility = Visibility.Collapsed;
            BlackboardRightSide.Visibility = Visibility.Collapsed;
            BorderTools.Visibility = Visibility.Collapsed;
            BorderSettings.Visibility = Visibility.Collapsed;
            LeftSidePanelForPPTNavigation.Visibility = Visibility.Collapsed;
            RightSidePanelForPPTNavigation.Visibility = Visibility.Collapsed;
            BorderSettings.Margin = new Thickness(0, 0, 0, 0);
            TwoFingerGestureBorder.Visibility = Visibility.Collapsed;
            BoardTwoFingerGestureBorder.Visibility = Visibility.Collapsed;
            BorderDrawShape.Visibility = Visibility.Collapsed;
            BoardBorderDrawShape.Visibility = Visibility.Collapsed;
            GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;

            ViewboxFloatingBar.Margin = new Thickness((SystemParameters.WorkArea.Width - 284) / 2,
                SystemParameters.WorkArea.Height - 60, -2000, -200);
            ViewboxFloatingBarMarginAnimation(100, true);

            try {
                if (File.Exists("debug.ini")) Label.Visibility = Visibility.Visible;
            }
            catch (Exception ex) {
                LogHelper.WriteLogToFile(ex.ToString(), LogHelper.LogType.Error);
            }

            try {
                if (File.Exists("Log.txt")) {
                    var fileInfo = new FileInfo("Log.txt");
                    var fileSizeInKB = fileInfo.Length / 1024;
                    if (fileSizeInKB > 512)
                        try {
                            File.Delete("Log.txt");
                            LogHelper.WriteLogToFile(
                                "The Log.txt file has been successfully deleted. Original file size: " + fileSizeInKB +
                                " KB", LogHelper.LogType.Info);
                        }
                        catch (Exception ex) {
                            LogHelper.WriteLogToFile(
                                ex + " | Can not delete the Log.txt file. File size: " + fileSizeInKB + " KB",
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
                if (File.Exists("SpecialVersion.ini")) SpecialVersionResetToSuggestion_Click();
            }
            catch (Exception ex) {
                LogHelper.WriteLogToFile(ex.ToString(), LogHelper.LogType.Error);
            }
            
            // 注意：CheckColorTheme和CheckPenTypeUIState已移至Window_Loaded中，
            // 在LoadSettings之后调用，以确保设置已加载
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


                drawingAttributes.Height = 2.5;
                drawingAttributes.Width = 2.5;
                drawingAttributes.IsHighlighter = false;
                drawingAttributes.FitToCurve = Settings.Canvas.FitToCurve;

                inkCanvas.EditingMode = InkCanvasEditingMode.Ink;


                //inkCanvas.Gesture += InkCanvas_Gesture;
            }
            catch (Exception ex) {
                LogHelper.WriteLogToFile("Failed to initialize pen canvas: " + ex.Message, LogHelper.LogType.Error);
            }
        }

        private void inkCanvas_EditingModeChanged(object sender, RoutedEventArgs e) {
            var inkCanvas1 = sender as InkCanvas;
            if (inkCanvas1 == null) return;
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

        public static Settings Settings = new Settings();
        public static string settingsFileName = "Settings.json";
        public bool isLoaded = false;

        [DllImport("user32.dll")]
        static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll")]
        static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, UInt32 uFlags);

        const uint MF_BYCOMMAND = 0x00000000;
        const uint MF_GRAYED = 0x00000001;
        const uint SC_CLOSE = 0xF060;

        private static void PreloadIALibrary() {
            try {
                GC.KeepAlive(typeof(InkAnalyzer));
                GC.KeepAlive(typeof(AnalysisAlternate));
                GC.KeepAlive(typeof(InkDrawingNode));
                var analyzer = new InkAnalyzer();
                analyzer.AddStrokes(new StrokeCollection() {
                    new Stroke(new StylusPointCollection() {
                        new StylusPoint(114,514),
                        new StylusPoint(191,9810),
                        new StylusPoint(7,21),
                        new StylusPoint(123,789),
                    })
                });
                analyzer.Analyze();
            }
            catch (Exception ex) {
                LogHelper.WriteLogToFile("PreloadIALibrary failed: " + ex.Message, LogHelper.LogType.Error);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            loadPenCanvas();
            //加载设置
            LoadSettings(true);
            
            // 在设置加载后更新UI状态
            CheckColorTheme(true);
            CheckPenTypeUIState();
            
            // HasNewUpdateWindow hasNewUpdateWindow = new HasNewUpdateWindow();
            if (Environment.Is64BitProcess) SettingsInkRecognitionGroupBox.Visibility = Visibility.Collapsed;

            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
            SystemEvents_UserPreferenceChanged(null, null);

            //TextBlockVersion.Text = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            LogHelper.WriteLogToFile("Ink Canvas Loaded", LogHelper.LogType.Event);

            isLoaded = true;

            BlackBoardLeftSidePageListView.ItemsSource = blackBoardSidePageListViewObservableCollection;
            BlackBoardRightSidePageListView.ItemsSource = blackBoardSidePageListViewObservableCollection;

            BtnLeftWhiteBoardSwitchPreviousGeometry.Brush =
                new SolidColorBrush(System.Windows.Media.Color.FromArgb(127, 24, 24, 27));
            BtnLeftWhiteBoardSwitchPreviousLabel.Opacity = 0.5;
            BtnRightWhiteBoardSwitchPreviousGeometry.Brush =
                new SolidColorBrush(System.Windows.Media.Color.FromArgb(127, 24, 24, 27));
            BtnRightWhiteBoardSwitchPreviousLabel.Opacity = 0.5;

            BorderInkReplayToolBox.Visibility = Visibility.Collapsed;
            BoardBackgroundPopup.Visibility = Visibility.Collapsed;

            // 提前加载IA库，优化第一笔等待时间
            Task.Run(() => {
                try {
                    PreloadIALibrary();
                }
                catch (Exception ex) {
                    LogHelper.WriteLogToFile("Failed to preload IA library in background: " + ex.Message, LogHelper.LogType.Error);
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

            StylusInvertedListenerInit();

            PenPaletteV2Init();
            SelectionV2Init();
            ShapeDrawingV2Init();

            InitStorageManagementModule();

            InitFreezeWindow(new HWND[] {
                new HWND(new WindowInteropHelper(this).Handle)
            });

            UpdateIndexInfoDisplay();
        }

        private void SystemEventsOnDisplaySettingsChanged(object sender, EventArgs e) {
            if (!Settings.Advanced.IsEnableResolutionChangeDetection) return;
            ShowNotification($"检测到显示器信息变化，变为{System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width}x{System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height}");
            new Thread(() => {
                var isFloatingBarOutsideScreen = false;
                var isInPPTPresentationMode = false;
                Dispatcher.Invoke(() => {
                    isFloatingBarOutsideScreen = IsOutsideOfScreenHelper.IsOutsideOfScreen(ViewboxFloatingBar);
                    isInPPTPresentationMode = BorderFloatingBarExitPPTBtn.Visibility == Visibility.Visible;
                });
                if (isFloatingBarOutsideScreen) dpiChangedDelayAction.DebounceAction(3000, null, () => {
                    if (!isFloatingBarFolded)
                    {
                        if (isInPPTPresentationMode) ViewboxFloatingBarMarginAnimation(60);
                        else ViewboxFloatingBarMarginAnimation(100, true);
                    }
                });
            }).Start();
        }

        public DelayAction dpiChangedDelayAction = new DelayAction();

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
                    if (isFloatingBarOutsideScreen) dpiChangedDelayAction.DebounceAction(3000,null, () => {
                        if (!isFloatingBarFolded)
                        {
                            if (isInPPTPresentationMode) ViewboxFloatingBarMarginAnimation(60);
                            else ViewboxFloatingBarMarginAnimation(100, true);
                        }
                    });
                }).Start();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            LogHelper.WriteLogToFile("Ink Canvas closing", LogHelper.LogType.Event);
            if (!CloseIsFromButton) {
                e.Cancel = true;
            }

            if (e.Cancel) LogHelper.WriteLogToFile("Ink Canvas closing cancelled", LogHelper.LogType.Event);
            else {
                try {
                    LogHelper.WriteLogToFile("Ink Canvas closing: Stopping background tasks");
                    // 停止墨迹回放线程
                    isStopInkReplay = true;
                    
                    LogHelper.WriteLogToFile("Ink Canvas closing: Stopping timers");
                    // 停止所有定时器，防止进程残留
                    timerCheckPPT.Stop();
                    timerCheckPPT.Dispose();
                    timerKillProcess.Stop();
                    timerKillProcess.Dispose();
                    timerCheckAutoFold.Stop();
                    timerCheckAutoFold.Dispose();
                    timerCheckAutoUpdateWithSilence.Stop();
                    timerCheckAutoUpdateWithSilence.Dispose();
                    timerDisplayTime.Stop();
                    timerDisplayTime.Dispose();
                    timerDisplayDate.Stop();
                    timerDisplayDate.Dispose();
                    
                    LogHelper.WriteLogToFile("Ink Canvas closing: Disposing freeze frame");
                    DisposeFreezeFrame();
                    
                    LogHelper.WriteLogToFile("Ink Canvas closing: Unsubscribing system events");
                    // 取消系统事件订阅，防止进程残留
                    try {
                        Microsoft.Win32.SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
                    }
                    catch (Exception ex) {
                        LogHelper.WriteLogToFile("Error unsubscribing UserPreferenceChanged: " + ex.Message, LogHelper.LogType.Error);
                    }
                    
                    LogHelper.WriteLogToFile("Ink Canvas closing: Releasing PPT COM objects");
                    // 释放 PPT COM 对象，防止进程残留
                    try {
                        if (pptApplication != null) {
                            try {
                                pptApplication.PresentationOpen -= PptApplication_PresentationOpen;
                                pptApplication.PresentationClose -= PptApplication_PresentationClose;
                                pptApplication.SlideShowBegin -= PptApplication_SlideShowBegin;
                                pptApplication.SlideShowNextSlide -= PptApplication_SlideShowNextSlide;
                                pptApplication.SlideShowEnd -= PptApplication_SlideShowEnd;
                            }
                            catch { /* 忽略事件解绑错误 */ }
                            
                            // 释放 COM 对象引用
                            try {
                                System.Runtime.InteropServices.Marshal.ReleaseComObject(pptApplication);
                            }
                            catch { /* 忽略 COM 释放错误 */ }
                            pptApplication = null;
                        }
                        
                        if (presentation != null) {
                            try {
                                System.Runtime.InteropServices.Marshal.ReleaseComObject(presentation);
                            }
                            catch { /* 忽略 COM 释放错误 */ }
                            presentation = null;
                        }
                        
                        if (slides != null) {
                            try {
                                System.Runtime.InteropServices.Marshal.ReleaseComObject(slides);
                            }
                            catch { /* 忽略 COM 释放错误 */ }
                            slides = null;
                        }
                        
                        if (slide != null) {
                            try {
                                System.Runtime.InteropServices.Marshal.ReleaseComObject(slide);
                            }
                            catch { /* 忽略 COM 释放错误 */ }
                            slide = null;
                        }
                    }
                    catch (Exception ex) {
                        LogHelper.WriteLogToFile("Error releasing PPT COM objects: " + ex.Message, LogHelper.LogType.Error);
                    }
                    
                    LogHelper.WriteLogToFile("Ink Canvas closing: Unsubscribing ViewModel events");
                    // 取消 ViewModel 事件订阅
                    try {
                        UnsubscribeViewModelEvents();
                    }
                    catch (Exception ex) {
                        LogHelper.WriteLogToFile("Error unsubscribing ViewModel events: " + ex.Message, LogHelper.LogType.Error);
                    }
                    
                    LogHelper.WriteLogToFile("Ink Canvas closing: Disposing TaskbarIcon");
                    // 释放托盘图标，防止进程残留
                    try {
                        var taskbar = (Hardcodet.Wpf.TaskbarNotification.TaskbarIcon)Application.Current.FindResource("TaskbarTrayIcon");
                        if (taskbar != null) {
                            taskbar.Dispose();
                        }
                    }
                    catch (Exception ex) {
                        LogHelper.WriteLogToFile("Error disposing TaskbarIcon: " + ex.Message, LogHelper.LogType.Error);
                    }
                    
                    LogHelper.WriteLogToFile("Ink Canvas closing: Finished cleanup");
                }
                catch (Exception ex) {
                    LogHelper.WriteLogToFile("Error during window closing: " + ex.Message, LogHelper.LogType.Error);
                }
                
                // 强制进行垃圾回收，确保 COM 对象被释放
                try {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
                catch { /* 忽略 GC 错误 */ }
                
                Application.Current.Shutdown();
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

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
            SystemEvents.DisplaySettingsChanged -= SystemEventsOnDisplaySettingsChanged;

            LogHelper.WriteLogToFile("Ink Canvas closed", LogHelper.LogType.Event);
        }

        private async void AutoUpdate() {
            AvailableLatestVersion = await AutoUpdateHelper.CheckForUpdates();

            if (AvailableLatestVersion != null) {
                var IsDownloadSuccessful = false;
                IsDownloadSuccessful = await AutoUpdateHelper.DownloadSetupFileAndSaveStatus(AvailableLatestVersion);

                if (IsDownloadSuccessful) {
                    if (!Settings.Startup.IsAutoUpdateWithSilence) {
                        if (MessageBox.Show("InkCanvasForClass 新版本安装包已下载完成，是否立即更新？",
                                "InkCanvasForClass New Version Available", MessageBoxButton.YesNo,
                                MessageBoxImage.Question) ==
                            MessageBoxResult.Yes) AutoUpdateHelper.InstallNewVersionApp(AvailableLatestVersion, false);
                    } else {
                        timerCheckAutoUpdateWithSilence.Start();
                    }
                }
            } else {
                AutoUpdateHelper.DeleteUpdatesFolder();
            }
        }

        #endregion Definations and Loading
    }
}