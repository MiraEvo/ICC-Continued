using Ink_Canvas.Core;
using Ink_Canvas.Helpers;
using Ink_Canvas.Services;
using Ink_Canvas.ViewModels;
using Ink_Canvas.Views.Settings;
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
    /// <summary>
    /// MainWindow 主窗口类
    ///
    /// 架构说明：
    /// - 此类采用 MVVM 模式，通过 ViewModel 管理 UI 状态和命令
    /// - 业务逻辑已迁移到服务层（Services）
    /// - 事件处理程序作为 View 回调，委托给 ViewModel 或服务层
    /// - 使用 partial class 将功能分散到多个文件中（MainWindow_cs 目录）
    ///
    /// 迁移状态：
    /// - ViewModel 初始化和事件订阅已完成
    /// - 工具按钮命令已绑定到 ViewModel
    /// - 设置面板导航已通过事件绑定
    /// - 白板页面管理已通过 ViewModel 事件处理
    /// </summary>
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
        /// 浮动工具栏 ViewModel
        /// </summary>
        private FloatingBarViewModel _floatingBarViewModel;

        /// <summary>
        /// 热键服务
        /// </summary>
        private IHotkeyService _hotkeyService;

        /// <summary>
        /// 初始化 ViewModels，从依赖注入容器中获取实例
        ///
        /// 注意：MainWindow 在 XAML 中实例化，无法使用构造函数注入，
        /// 因此使用 ServiceLocator 是合理的。这是 ServiceLocator 的合法使用场景之一。
        /// </summary>
        private void InitializeViewModels()
        {
            try
            {
                ViewModel = ServiceLocator.GetRequiredService<MainWindowViewModel>();
                ToolbarVM = ServiceLocator.GetRequiredService<ToolbarViewModel>();
                SettingsVM = ServiceLocator.GetRequiredService<SettingsViewModel>();
                _hotkeyService = ServiceLocator.GetRequiredService<IHotkeyService>();

                // 统一设置系统：MainWindow.Settings 通过 getter 从 SettingsService 获取设置
                // 如果 SettingsService 还没有加载设置，先加载
                var settingsService = ServiceLocator.GetRequiredService<ISettingsService>();
                if (settingsService != null && !settingsService.IsLoaded)
                {
                    settingsService.Load();
                }

                // 订阅 ViewModel 事件
                SubscribeViewModelEvents();

                // 注册默认热键
                RegisterDefaultHotkeys();

                LogHelper.WriteLogToFile("ViewModels initialized successfully", LogHelper.LogType.Info);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile("Failed to initialize ViewModels: " + ex.Message, LogHelper.LogType.Error);
                throw;
            }
        }

        /// <summary>
        /// 注册默认热键
        /// </summary>
        private void RegisterDefaultHotkeys()
        {
            try
            {
                // 注册撤销热键 (Ctrl+Z)
                _hotkeyService.RegisterHotkey("Undo", new KeyGesture(Key.Z, ModifierKeys.Control), () =>
                {
                    if (ViewModel?.UndoCommand?.CanExecute(null) == true)
                        ViewModel.UndoCommand.Execute(null);
                });

                // 注册重做热键 (Ctrl+Y)
                _hotkeyService.RegisterHotkey("Redo", new KeyGesture(Key.Y, ModifierKeys.Control), () =>
                {
                    if (ViewModel?.RedoCommand?.CanExecute(null) == true)
                        ViewModel.RedoCommand.Execute(null);
                });

                // 注册清空热键 (Ctrl+E)
                _hotkeyService.RegisterHotkey("Clear", new KeyGesture(Key.E, ModifierKeys.Control), () =>
                {
                    if (ViewModel?.ClearCanvasCommand?.CanExecute(null) == true)
                        ViewModel.ClearCanvasCommand.Execute(null);
                });

                // 注册切换到绘图工具热键 (Alt+D)
                _hotkeyService.RegisterHotkey("DrawTool", new KeyGesture(Key.D, ModifierKeys.Alt), () =>
                {
                    if (ViewModel?.ChangeToDrawToolCommand?.CanExecute(null) == true)
                    {
                        ViewModel.ChangeToDrawToolCommand.Execute(null);
                        PenIcon_Click(lastBorderMouseDownObject, null);
                    }
                });

                // 注册退出绘图工具热键 (Alt+Q)
                _hotkeyService.RegisterHotkey("QuitDrawTool", new KeyGesture(Key.Q, ModifierKeys.Alt), () =>
                {
                    if (currentMode != 0) ImageBlackboard_MouseUp(lastBorderMouseDownObject, null);
                    if (ViewModel?.QuitDrawToolCommand?.CanExecute(null) == true)
                    {
                        ViewModel.QuitDrawToolCommand.Execute(null);
                        CursorIcon_Click(lastBorderMouseDownObject, null);
                    }
                });

                // 注册切换到选择工具热键 (Alt+S)
                _hotkeyService.RegisterHotkey("SelectTool", new KeyGesture(Key.S, ModifierKeys.Alt), () =>
                {
                    if (StackPanelCanvasControls.Visibility == Visibility.Visible)
                    {
                        if (ViewModel?.ChangeToSelectCommand?.CanExecute(null) == true)
                        {
                            ViewModel.ChangeToSelectCommand.Execute(null);
                            SymbolIconSelect_MouseUp(lastBorderMouseDownObject, null);
                        }
                    }
                });

                // 注册切换到橡皮擦热键 (Alt+E)
                _hotkeyService.RegisterHotkey("EraserTool", new KeyGesture(Key.E, ModifierKeys.Alt), () =>
                {
                    if (StackPanelCanvasControls.Visibility == Visibility.Visible)
                    {
                        if (ViewModel?.ChangeToEraserCommand?.CanExecute(null) == true)
                        {
                            ViewModel.ChangeToEraserCommand.Execute(null);
                            if (Eraser_Icon.Background != null)
                                EraserIconByStrokes_Click(lastBorderMouseDownObject, null);
                            else
                                EraserIcon_Click(lastBorderMouseDownObject, null);
                        }
                    }
                });

                // 注册切换到画板热键 (Alt+B)
                _hotkeyService.RegisterHotkey("BoardTool", new KeyGesture(Key.B, ModifierKeys.Alt), () =>
                {
                    if (ViewModel?.ChangeToBoardCommand?.CanExecute(null) == true)
                    {
                        ViewModel.ChangeToBoardCommand.Execute(null);
                        ImageBlackboard_MouseUp(lastBorderMouseDownObject, null);
                    }
                });

                // 注册截图热键 (Alt+C)
                _hotkeyService.RegisterHotkey("Capture", new KeyGesture(Key.C, ModifierKeys.Alt), () =>
                {
                    if (ViewModel?.CaptureCommand?.CanExecute(null) == true)
                        ViewModel.CaptureCommand.Execute(null);
                });

                // 注册绘制直线热键 (Alt+L)
                _hotkeyService.RegisterHotkey("DrawLine", new KeyGesture(Key.L, ModifierKeys.Alt), () =>
                {
                    if (StackPanelCanvasControls.Visibility == Visibility.Visible)
                    {
                        if (ViewModel?.DrawLineCommand?.CanExecute(null) == true)
                        {
                            ViewModel.DrawLineCommand.Execute(null);
                            BtnDrawLine_Click(lastMouseDownSender, null);
                        }
                    }
                });

                // 注册隐藏/显示热键 (Alt+V)
                _hotkeyService.RegisterHotkey("ToggleHide", new KeyGesture(Key.V, ModifierKeys.Alt), () =>
                {
                    if (ViewModel?.ToggleHideCommand?.CanExecute(null) == true)
                        ViewModel.ToggleHideCommand.Execute(null);
                });

                LogHelper.WriteLogToFile("Default hotkeys registered successfully", LogHelper.LogType.Info);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile("Failed to register default hotkeys: " + ex.Message, LogHelper.LogType.Error);
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

            // 白板页面导航请求
            ViewModel.PreviousWhiteboardPageRequested += OnPreviousWhiteboardPageRequested;
            ViewModel.NextWhiteboardPageRequested += OnNextWhiteboardPageRequested;
            ViewModel.AddWhiteboardPageRequested += OnAddWhiteboardPageRequested;
            ViewModel.DeleteWhiteboardPageRequested += OnDeleteWhiteboardPageRequested;
            ViewModel.ShowWhiteboardPageListRequested += OnShowWhiteboardPageListRequested;

            // 白板背景设置请求
            ViewModel.SetBoardBackgroundColorRequested += OnSetBoardBackgroundColorRequested;
            ViewModel.SetBoardBackgroundPatternRequested += OnSetBoardBackgroundPatternRequested;
            ViewModel.ToggleBoardBackgroundPanelRequested += OnToggleBoardBackgroundPanelRequested;

            // 设置面板事件订阅
            SubscribeSettingsViewEvents();
        }

        /// <summary>
        /// 订阅 SettingsView 事件
        /// </summary>
        private void SubscribeSettingsViewEvents()
        {
            // SettingsView 的导航事件已在 XAML 中绑定到 SettingsView_NavigateToCategory
            // 这里可以添加其他需要在代码中订阅的事件

            // 如果 SettingsViewControl 存在，设置其 ViewModel
            if (SettingsViewControl != null && SettingsVM != null)
            {
                SettingsViewControl.ViewModel = SettingsVM;
            }

            // 订阅 SettingsViewModel 的事件
            if (SettingsVM != null)
            {
                SettingsVM.RestartRequested += OnSettingsRestartRequested;
                SettingsVM.ExitRequested += OnSettingsExitRequested;
            }
        }

        /// <summary>
        /// 处理设置面板重启请求
        /// </summary>
        private void OnSettingsRestartRequested(object sender, EventArgs e)
        {
            BtnRestart_Click(null, null);
        }

        /// <summary>
        /// 处理设置面板退出请求
        /// </summary>
        private void OnSettingsExitRequested(object sender, EventArgs e)
        {
            BtnExit_Click(null, null);
        }

        /// <summary>
        /// 处理 SettingsView 导航事件
        /// </summary>
        private void SettingsView_NavigateToCategory(object sender, Views.Settings.SettingsNavigationEventArgs e)
        {
            // 处理设置分类导航
            // 当用户点击设置面板左侧导航按钮时触发
            try
            {
                var categoryName = e.CategoryName;
                LogHelper.WriteLogToFile($"Settings navigation to category: {categoryName}", LogHelper.LogType.Info);

                // 调用现有的设置导航逻辑
                // 这里可以根据分类名称滚动到对应的设置组
                ScrollToSettingsCategory(categoryName);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"SettingsView_NavigateToCategory failed: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 滚动到指定的设置分类
        /// </summary>
        private void ScrollToSettingsCategory(string categoryName)
        {
            // 根据分类名称找到对应的 GroupBox 并滚动到该位置
            // 这里复用现有的设置导航逻辑
            try
            {
                FrameworkElement targetElement = categoryName switch
                {
                    "Startup" => SettingsStartupGroupBox,
                    "Canvas" => SettingsCanvasGroupBox,
                    "Gesture" => SettingsGestureGroupBox,
                    "Appearance" => SettingsAppearanceGroupBox,
                    "PowerPoint" => SettingsPPTGroupBox,
                    "Advanced" => SettingsAdvancedGroupBox,
                    "Automation" => SettingsAutomationGroupBox,
                    "About" => SettingsAboutGroupBox,
                    "Storage" => SettingsStorageGroupBox,
                    "Snapshot" => SettingsSnapshotGroupBox,
                    "ShapeDrawing" => SettingsShapeDrawingGroupBox,
                    "InkRecognition" => SettingsInkRecognitionGroupBox,
                    "RandWindow" => SettingsRandWindowGroupBox,
                    "Donation" => SettingsDonationGroupBox,
                    _ => null
                };

                if (targetElement != null && SettingsPanelScrollViewer != null)
                {
                    // 获取目标元素相对于 ScrollViewer 的位置
                    var transform = targetElement.TransformToAncestor(SettingsPanelScrollViewer);
                    var position = transform.Transform(new System.Windows.Point(0, 0));

                    // 滚动到目标位置
                    SettingsPanelScrollViewer.ScrollToVerticalOffset(
                        SettingsPanelScrollViewer.VerticalOffset + position.Y - 20);
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"ScrollToSettingsCategory failed: {ex.Message}", LogHelper.LogType.Error);
            }
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

                // 取消订阅白板相关事件
                ViewModel.PreviousWhiteboardPageRequested -= OnPreviousWhiteboardPageRequested;
                ViewModel.NextWhiteboardPageRequested -= OnNextWhiteboardPageRequested;
                ViewModel.AddWhiteboardPageRequested -= OnAddWhiteboardPageRequested;
                ViewModel.DeleteWhiteboardPageRequested -= OnDeleteWhiteboardPageRequested;
                ViewModel.ShowWhiteboardPageListRequested -= OnShowWhiteboardPageListRequested;
                ViewModel.SetBoardBackgroundColorRequested -= OnSetBoardBackgroundColorRequested;
                ViewModel.SetBoardBackgroundPatternRequested -= OnSetBoardBackgroundPatternRequested;
                ViewModel.ToggleBoardBackgroundPanelRequested -= OnToggleBoardBackgroundPanelRequested;
            }
        }

        #region ViewModel 事件处理

        // ============================================================
        // View 回调方法
        // ============================================================
        // 这些方法作为 View 层的回调，响应 ViewModel 发出的请求事件。
        // 它们负责调用原有的 UI 操作方法，实现 ViewModel 与 View 的解耦。
        //
        // 设计原则：
        // 1. 事件处理方法应保持简洁，只做委托调用
        // 2. 复杂的业务逻辑应在 ViewModel 或服务层中处理
        // 3. UI 操作（如动画、弹窗）保留在 View 层
        // ============================================================

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

        /// <summary>
        /// 处理白板上一页请求
        /// </summary>
        private void OnPreviousWhiteboardPageRequested(object sender, EventArgs e)
        {
            try
            {
                BtnWhiteBoardSwitchPrevious_Click(sender, e);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile("OnPreviousWhiteboardPageRequested failed: " + ex.Message, LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 处理白板下一页请求
        /// </summary>
        private void OnNextWhiteboardPageRequested(object sender, EventArgs e)
        {
            try
            {
                BtnWhiteBoardSwitchNext_Click(sender, e);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile("OnNextWhiteboardPageRequested failed: " + ex.Message, LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 处理添加新白板页请求
        /// </summary>
        private void OnAddWhiteboardPageRequested(object sender, EventArgs e)
        {
            try
            {
                BtnWhiteBoardAdd_Click(sender, e);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile("OnAddWhiteboardPageRequested failed: " + ex.Message, LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 处理删除当前白板页请求
        /// </summary>
        private void OnDeleteWhiteboardPageRequested(object sender, EventArgs e)
        {
            try
            {
                BtnWhiteBoardDelete_Click(sender, null);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile("OnDeleteWhiteboardPageRequested failed: " + ex.Message, LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 处理显示白板页面列表请求
        /// </summary>
        private void OnShowWhiteboardPageListRequested(object sender, EventArgs e)
        {
            try
            {
                BtnWhiteBoardPageIndex_Click(sender, e);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile("OnShowWhiteboardPageListRequested failed: " + ex.Message, LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 处理设置白板背景颜色请求
        /// </summary>
        private void OnSetBoardBackgroundColorRequested(object sender, int colorIndex)
        {
            try
            {
                UpdateBoardBackground(colorIndex);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile("OnSetBoardBackgroundColorRequested failed: " + ex.Message, LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 处理设置白板背景图案请求
        /// </summary>
        private void OnSetBoardBackgroundPatternRequested(object sender, int patternIndex)
        {
            try
            {
                // 根据索引设置背景图案
                BoardPagesSettingsList[CurrentWhiteboardIndex - 1].BackgroundPattern = (BlackboardBackgroundPatternEnum)patternIndex;
                ApplyBackgroundPattern();
                UpdateBoardBackgroundPanelDisplayStatus();
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile("OnSetBoardBackgroundPatternRequested failed: " + ex.Message, LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 处理显示/隐藏白板背景设置面板请求
        /// </summary>
        private void OnToggleBoardBackgroundPanelRequested(object sender, EventArgs e)
        {
            try
            {
                // 切换白板背景设置面板的显示状态
                if (BoardBackgroundPopup.Visibility == Visibility.Visible)
                {
                    BoardBackgroundPopup.Visibility = Visibility.Collapsed;
                }
                else
                {
                    BoardBackgroundPopup.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile("OnToggleBoardBackgroundPanelRequested failed: " + ex.Message, LogHelper.LogType.Error);
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

            // 初始化 FloatingBarView
            InitializeFloatingBarView();

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

        // 使用 SettingsService 管理的 Settings 实例
        public Settings Settings => ServiceLocator.GetService<ISettingsService>()?.Settings ?? new Settings();
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
                // 使用新的 Windows.UI.Input.Inking.Analysis API 预热
                _ = InkRecognizeHelper.PreloadAsync();
                LogHelper.WriteLogToFile("Ink Analysis API preload initiated", LogHelper.LogType.Info);
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
            // 注意：旧版 IA 库不支持 64 位，但新的 Windows.UI.Input.Inking.Analysis API 支持 x64
            // 因此移除了 64 位进程检查

            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
            SystemEvents_UserPreferenceChanged(null, null);

            //TextBlockVersion.Text = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            LogHelper.WriteLogToFile("Ink Canvas Loaded", LogHelper.LogType.Event);

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
                if (isFloatingBarOutsideScreen) dpiChangedDelayAction.DebounceAction(Constants.DpiChangeDelayMilliseconds, null, () => {
                    if (!isFloatingBarFolded)
                    {
                        if (isInPPTPresentationMode) ViewboxFloatingBarMarginAnimation(Constants.FloatingBarBottomMarginPPT);
                        else ViewboxFloatingBarMarginAnimation(Constants.FloatingBarBottomMarginNormal, true);
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
                    try {
                        if (timerCheckPPT != null) {
                            timerCheckPPT.Stop();
                            timerCheckPPT.Elapsed -= TimerCheckPPT_Elapsed;
                            timerCheckPPT.Dispose();
                        }
                    }
                    catch (Exception ex) {
                        LogHelper.WriteLogToFile("Error stopping timerCheckPPT: " + ex.Message, LogHelper.LogType.Error);
                    }

                    try {
                        if (timerKillProcess != null) {
                            timerKillProcess.Stop();
                            timerKillProcess.Elapsed -= TimerKillProcess_Elapsed;
                            timerKillProcess.Dispose();
                        }
                    }
                    catch (Exception ex) {
                        LogHelper.WriteLogToFile("Error stopping timerKillProcess: " + ex.Message, LogHelper.LogType.Error);
                    }

                    try {
                        if (timerCheckAutoFold != null) {
                            timerCheckAutoFold.Stop();
                            timerCheckAutoFold.Elapsed -= TimerCheckAutoFold_Elapsed;
                            timerCheckAutoFold.Dispose();
                        }
                    }
                    catch (Exception ex) {
                        LogHelper.WriteLogToFile("Error stopping timerCheckAutoFold: " + ex.Message, LogHelper.LogType.Error);
                    }

                    try {
                        if (timerCheckAutoUpdateWithSilence != null) {
                            timerCheckAutoUpdateWithSilence.Stop();
                            timerCheckAutoUpdateWithSilence.Elapsed -= TimerCheckAutoUpdateWithSilence_Elapsed;
                            timerCheckAutoUpdateWithSilence.Dispose();
                        }
                    }
                    catch (Exception ex) {
                        LogHelper.WriteLogToFile("Error stopping timerCheckAutoUpdateWithSilence: " + ex.Message, LogHelper.LogType.Error);
                    }

                    try {
                        if (timerDisplayTime != null) {
                            timerDisplayTime.Stop();
                            timerDisplayTime.Elapsed -= TimerDisplayTime_Elapsed;
                            timerDisplayTime.Dispose();
                        }
                    }
                    catch (Exception ex) {
                        LogHelper.WriteLogToFile("Error stopping timerDisplayTime: " + ex.Message, LogHelper.LogType.Error);
                    }

                    try {
                        if (timerDisplayDate != null) {
                            timerDisplayDate.Stop();
                            timerDisplayDate.Elapsed -= TimerDisplayDate_Elapsed;
                            timerDisplayDate.Dispose();
                        }
                    }
                    catch (Exception ex) {
                        LogHelper.WriteLogToFile("Error stopping timerDisplayDate: " + ex.Message, LogHelper.LogType.Error);
                    }

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

                    try {
                        SystemEvents.DisplaySettingsChanged -= SystemEventsOnDisplaySettingsChanged;
                    }
                    catch (Exception ex) {
                        LogHelper.WriteLogToFile("Error unsubscribing DisplaySettingsChanged: " + ex.Message, LogHelper.LogType.Error);
                    }

                    LogHelper.WriteLogToFile("Ink Canvas closing: Unsubscribing InkCanvas and TimeMachine events");
                    // 取消 InkCanvas 和 TimeMachine 事件订阅
                    try {
                        if (timeMachine != null) {
                            timeMachine.OnRedoStateChanged -= TimeMachine_OnRedoStateChanged;
                            timeMachine.OnUndoStateChanged -= TimeMachine_OnUndoStateChanged;
                        }
                    }
                    catch (Exception ex) {
                        LogHelper.WriteLogToFile("Error unsubscribing TimeMachine events: " + ex.Message, LogHelper.LogType.Error);
                    }

                    try {
                        if (inkCanvas != null && inkCanvas.Strokes != null) {
                            inkCanvas.Strokes.StrokesChanged -= StrokesOnStrokesChanged;
                        }
                    }
                    catch (Exception ex) {
                        LogHelper.WriteLogToFile("Error unsubscribing InkCanvas.Strokes events: " + ex.Message, LogHelper.LogType.Error);
                    }

                    LogHelper.WriteLogToFile("Ink Canvas closing: Releasing PPT COM objects");
                    // 释放 PPT COM 对象，防止进程残留
                    try {
                        if (pptApplication != null) {
                            try {
                                // 取消订阅所有 PPT 事件
                                pptApplication.PresentationOpen -= PptApplication_PresentationOpen;
                                pptApplication.PresentationClose -= PptApplication_PresentationClose;
                                pptApplication.SlideShowBegin -= PptApplication_SlideShowBegin;
                                pptApplication.SlideShowNextSlide -= PptApplication_SlideShowNextSlide;
                                pptApplication.SlideShowEnd -= PptApplication_SlideShowEnd;
                            }
                            catch (Exception ex) {
                                LogHelper.WriteLogToFile("Error unsubscribing PPT events: " + ex.Message, LogHelper.LogType.Warning);
                            }

                            // 释放 COM 对象引用
                            try {
                                System.Runtime.InteropServices.Marshal.ReleaseComObject(pptApplication);
                                LogHelper.WriteLogToFile("PPT Application COM object released", LogHelper.LogType.Info);
                            }
                            catch (Exception ex) {
                                LogHelper.WriteLogToFile("Error releasing pptApplication COM object: " + ex.Message, LogHelper.LogType.Error);
                            }
                            pptApplication = null;
                        }

                        if (presentation != null) {
                            try {
                                System.Runtime.InteropServices.Marshal.ReleaseComObject(presentation);
                                LogHelper.WriteLogToFile("Presentation COM object released", LogHelper.LogType.Info);
                            }
                            catch (Exception ex) {
                                LogHelper.WriteLogToFile("Error releasing presentation COM object: " + ex.Message, LogHelper.LogType.Error);
                            }
                            presentation = null;
                        }

                        if (slides != null) {
                            try {
                                System.Runtime.InteropServices.Marshal.ReleaseComObject(slides);
                                LogHelper.WriteLogToFile("Slides COM object released", LogHelper.LogType.Info);
                            }
                            catch (Exception ex) {
                                LogHelper.WriteLogToFile("Error releasing slides COM object: " + ex.Message, LogHelper.LogType.Error);
                            }
                            slides = null;
                        }

                        if (slide != null) {
                            try {
                                System.Runtime.InteropServices.Marshal.ReleaseComObject(slide);
                                LogHelper.WriteLogToFile("Slide COM object released", LogHelper.LogType.Info);
                            }
                            catch (Exception ex) {
                                LogHelper.WriteLogToFile("Error releasing slide COM object: " + ex.Message, LogHelper.LogType.Error);
                            }
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
            LogHelper.WriteLogToFile("Ink Canvas closed", LogHelper.LogType.Event);
        }

        private async void AutoUpdate() {
            AvailableLatestVersion = await AutoUpdateHelper.CheckForUpdates();

            if (AvailableLatestVersion != null) {
                var isDownloadSuccessful = false;
                isDownloadSuccessful = await AutoUpdateHelper.DownloadSetupFileAndSaveStatus(AvailableLatestVersion);

                if (isDownloadSuccessful) {
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
