using Ink_Canvas.Core;
using Ink_Canvas.Helpers;
using Ink_Canvas.Services;
using Ink_Canvas.ViewModels;
using Ink_Canvas.Views.Settings;
using Ink_Canvas.Services.Events;
using iNKORE.UI.WPF.Modern;
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
using iNKORE.UI.WPF.Modern.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using Vanara.PInvoke;
using Wpf.Ui.Controls;
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
                BlackboardVM = ServiceLocator.GetRequiredService<BlackboardViewModel>();
                TouchEventsVM = ServiceLocator.GetRequiredService<TouchEventsViewModel>();
                _hotkeyService = ServiceLocator.GetRequiredService<IHotkeyService>();

                // 统一设置系统：MainWindow.Settings 通过 getter 从 SettingsService 获取设置
                // 如果 SettingsService 还没有加载设置，先加载
                var settingsService = ServiceLocator.GetRequiredService<ISettingsService>();
                if (settingsService is { IsLoaded: false })
                {
                    settingsService.Load();
                }
                // 订阅设置变更事件，确保设置修改后立即生效
                settingsService.SettingChanged += OnSettingChanged;

                // 订阅 ViewModel 事件
                SubscribeViewModelEvents();

                // 注册默认热键
                RegisterDefaultHotkeys();

                // 初始化外观设置事件监听
                InitializeAppearanceSettingsHandler();

                LogHelper.WriteLogToFile("ViewModel 初始化完成", LogHelper.LogType.Info);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile("ViewModel 初始化失败：" + ex.Message, LogHelper.LogType.Error);
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

                LogHelper.WriteLogToFile("默认热键注册成功", LogHelper.LogType.Info);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile("默认热键注册失败：" + ex.Message, LogHelper.LogType.Error);
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

            // TouchEventsViewModel 事件订阅
            SubscribeTouchEventsViewModelEvents();

            // 设置面板事件订阅
            SubscribeSettingsViewEvents();
        }

        /// <summary>
        /// 订阅 TouchEventsViewModel 事件
        /// </summary>
        private void SubscribeTouchEventsViewModelEvents()
        {
            if (ViewModel?.TouchEventsViewModel == null) return;

            // 编辑模式变更请求
            ViewModel.TouchEventsViewModel.EditingModeChangeRequested += OnEditingModeChangeRequested;

            // 隐藏子面板请求
            ViewModel.TouchEventsViewModel.HideSubPanelsRequested += OnHideSubPanelsRequested;

            // 橡皮擦反馈请求
            ViewModel.TouchEventsViewModel.EraserFeedbackRequested += OnEraserFeedbackRequested;

            // 操作增量请求
            ViewModel.TouchEventsViewModel.ManipulationDeltaRequested += OnManipulationDeltaRequested;

            // 初始化 TouchEventsViewModel 的 UI 引用
            InitializeTouchEventsViewModelUI();
        }

        /// <summary>
        /// 初始化 TouchEventsViewModel 的 UI 引用
        /// </summary>
        private void InitializeTouchEventsViewModelUI()
        {
            if (ViewModel?.TouchEventsViewModel == null) return;

            // 设置 UI 引用
            ViewModel.TouchEventsViewModel.InkCanvas = inkCanvas;
            ViewModel.TouchEventsViewModel.EraserOverlay = GridEraserOverlay;
            ViewModel.TouchEventsViewModel.EraserDrawingVisual = EraserOverlay_DrawingVisual?.DrawingVisual;
            ViewModel.TouchEventsViewModel.FloatingBar = ViewboxFloatingBar;
            ViewModel.TouchEventsViewModel.BlackboardUI = BlackboardUIGridForInkReplay;

            // 设置基准触摸宽度
            ViewModel.TouchEventsViewModel.BoundsWidth = BoundsWidth;
        }

        /// <summary>
        /// 订阅设置相关事件
        /// </summary>
        private void SubscribeSettingsViewEvents()
        {
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
        private void OnHideSubPanelsRequested(object? sender, EventArgs e)
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

                // 取消订阅 TouchEventsViewModel 事件
                UnsubscribeTouchEventsViewModelEvents();
            }
        }

        /// <summary>
        /// 取消订阅 TouchEventsViewModel 事件
        /// </summary>
        private void UnsubscribeTouchEventsViewModelEvents()
        {
            if (ViewModel?.TouchEventsViewModel == null) return;

            ViewModel.TouchEventsViewModel.EditingModeChangeRequested -= OnEditingModeChangeRequested;
            ViewModel.TouchEventsViewModel.HideSubPanelsRequested -= OnHideSubPanelsRequested;
            ViewModel.TouchEventsViewModel.EraserFeedbackRequested -= OnEraserFeedbackRequested;
            ViewModel.TouchEventsViewModel.ManipulationDeltaRequested -= OnManipulationDeltaRequested;
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
                LogHelper.WriteLogToFile("处理清空画布请求失败：" + ex.Message, LogHelper.LogType.Error);
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
                LogHelper.WriteLogToFile("处理截图请求失败：" + ex.Message, LogHelper.LogType.Error);
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
                LogHelper.WriteLogToFile("处理隐藏/显示请求失败：" + ex.Message, LogHelper.LogType.Error);
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
                LogHelper.WriteLogToFile("处理退出应用请求失败：" + ex.Message, LogHelper.LogType.Error);
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
                LogHelper.WriteLogToFile("处理画笔切换请求失败：" + ex.Message, LogHelper.LogType.Error);
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
                LogHelper.WriteLogToFile("处理绘制直线请求失败：" + ex.Message, LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 处理编辑模式变更请求
        /// </summary>
        private void OnEditingModeChangeRequested(object sender, Ink_Canvas.Services.Events.EditingModeChangeRequestedEventArgs e)
        {
            try
            {
                if (inkCanvas != null)
                {
                    inkCanvas.EditingMode = e.NewMode;
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile("处理编辑模式变更请求失败：" + ex.Message, LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 处理橡皮擦反馈请求
        /// </summary>
        private void OnEraserFeedbackRequested(object? sender, Ink_Canvas.Services.Events.EraserFeedbackEventArgs e)
        {
            try
            {
                if (EraserOverlay_DrawingVisual?.DrawingVisual != null)
                {
                    var ct = EraserOverlay_DrawingVisual.DrawingVisual.RenderOpen();
                    
                    // 创建橡皮擦形状
                    Geometry eraserGeometry;
                    if (e.IsCircleShape)
                    {
                        eraserGeometry = new EllipseGeometry(new System.Windows.Point(e.Width / 2, e.Height / 2), e.Width / 2, e.Height / 2);
                    }
                    else
                    {
                        eraserGeometry = new RectangleGeometry(new Rect(0, 0, e.Width, e.Height));
                    }
                    
                    // 应用变换
                    var transform = new TranslateTransform(e.Position.X - e.Width / 2, e.Position.Y - e.Height / 2);
                    eraserGeometry.Transform = transform;
                    
                    // 绘制橡皮擦
                    ct.DrawGeometry(new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(128, 255, 0, 0)), 
                        new System.Windows.Media.Pen(new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(200, 255, 0, 0)), 2), 
                        eraserGeometry);
                    
                    ct.Close();
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile("处理橡皮擦反馈请求失败：" + ex.Message, LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 处理操作增量请求
        /// </summary>
        private void OnManipulationDeltaRequested(object? sender, Ink_Canvas.Services.Events.TouchManipulationDeltaEventArgs e)
        {
            try
            {
                if (inkCanvas?.Strokes != null)
                {
                    // 应用变换到所有笔画
                    var matrix = e.CachedMatrix;
                    matrix.Translate(e.Delta.Translation.X, e.Delta.Translation.Y);
                    matrix.RotateAt(e.Delta.Rotation, e.Center.X, e.Center.Y);
                    matrix.ScaleAt(e.Delta.Scale.X, e.Delta.Scale.Y, e.Center.X, e.Center.Y);

                    foreach (var stroke in inkCanvas.Strokes)
                    {
                        if (!stroke.ContainsPropertyData(Guid.Parse("{D6FCCF9F-6132-4E70-9222-054F05D0BF0E}"))) // 非锁定笔画
                        {
                            stroke.Transform(matrix, false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile("处理操作增量请求失败：" + ex.Message, LogHelper.LogType.Error);
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
                LogHelper.WriteLogToFile("处理白板上一页请求失败：" + ex.Message, LogHelper.LogType.Error);
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
                LogHelper.WriteLogToFile("处理白板下一页请求失败：" + ex.Message, LogHelper.LogType.Error);
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
                LogHelper.WriteLogToFile("处理添加白板页请求失败：" + ex.Message, LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 处理删除当前白板页请求
        /// </summary>
        private void OnDeleteWhiteboardPageRequested(object sender, EventArgs e)
        {
            try
            {
                BtnWhiteBoardDelete_Click(sender, null!);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile("处理删除白板页请求失败：" + ex.Message, LogHelper.LogType.Error);
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
                LogHelper.WriteLogToFile("处理显示白板页列表请求失败：" + ex.Message, LogHelper.LogType.Error);
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
                LogHelper.WriteLogToFile("处理设置白板背景颜色请求失败：" + ex.Message, LogHelper.LogType.Error);
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
                LogHelper.WriteLogToFile("处理设置白板背景图案请求失败：" + ex.Message, LogHelper.LogType.Error);
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
                LogHelper.WriteLogToFile("处理切换白板背景面板失败：" + ex.Message, LogHelper.LogType.Error);
            }
        }

        #endregion

        /// <summary>
        /// 处理设置变更事件
        /// </summary>
        private void OnSettingChanged(object sender, SettingChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    switch (e.CategoryName)
                    {
                        case "Canvas":
                            HandleCanvasSettingChanged(e.PropertyName);
                            break;
                        case "PowerPoint":
                            HandlePowerPointSettingChanged(e.PropertyName);
                            break;
                        case "Automation":
                            HandleAutomationSettingChanged(e.PropertyName);
                            break;
                        case "Gesture":
                            HandleGestureSettingChanged(e.PropertyName);
                            break;
                        case "Advanced":
                            HandleAdvancedSettingChanged(e.PropertyName);
                            break;
                        case "Startup":
                            HandleStartupSettingChanged(e.PropertyName);
                            break;
                        case "InkToShape":
                            HandleInkToShapeSettingChanged(e.PropertyName);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"Error handling setting change: {ex.Message}", LogHelper.LogType.Error);
                }
            });
        }

        private void HandleCanvasSettingChanged(string propertyName)
        {
            switch (propertyName)
            {
                case nameof(CanvasSettings.IsShowCursor):
                    inkCanvas_EditingModeChanged(inkCanvas, null);
                    break;
                case nameof(CanvasSettings.EraserSize):
                    // Update eraser size logic
                    double width = 24;
                    switch (Settings.Canvas.EraserSize)
                    {
                        case 0: width = 24; break;
                        case 1: width = 38; break;
                        case 2: width = 46; break;
                        case 3: width = 62; break;
                        case 4: width = 78; break;
                    }
                    eraserWidth = width;
                    isEraserCircleShape = Settings.Canvas.EraserShapeType == 0;
                    break;
                case nameof(CanvasSettings.EraserShapeType):
                    CheckEraserTypeTab();
                    isEraserCircleShape = Settings.Canvas.EraserShapeType == 0;
                    break;
                case nameof(CanvasSettings.InkWidth):
                    if (drawingAttributes != null)
                    {
                        drawingAttributes.Height = Settings.Canvas.InkWidth;
                        drawingAttributes.Width = Settings.Canvas.InkWidth;
                    }
                    break;
                case nameof(CanvasSettings.HighlighterWidth):
                    if (drawingAttributes != null)
                    {
                        // Note: Logic from HighlighterWidthSlider_ValueChanged
                        // drawingAttributes.Height = Settings.Canvas.HighlighterWidth;
                        // drawingAttributes.Width = Settings.Canvas.HighlighterWidth / 2;
                        // But we need to check current tool mode.
                        // For now, just update if it's highlighter mode or generic update.
                    }
                    break;
                case nameof(CanvasSettings.FitToCurve):
                    if (drawingAttributes != null)
                        drawingAttributes.FitToCurve = Settings.Canvas.FitToCurve;
                    break;
                case nameof(CanvasSettings.ApplyScaleToStylusTip):
                    SelectionV2.ApplyScaleToStylusTip = Settings.Canvas.ApplyScaleToStylusTip;
                    break;
                case nameof(CanvasSettings.OnlyHitTestFullyContainedStrokes):
                    SelectionV2.OnlyHitTestFullyContainedStrokes = Settings.Canvas.OnlyHitTestFullyContainedStrokes;
                    break;
                case nameof(CanvasSettings.AllowClickToSelectLockedStroke):
                    SelectionV2.AllowClickToSelectLockedStroke = Settings.Canvas.AllowClickToSelectLockedStroke;
                    break;
                case nameof(CanvasSettings.SelectionMethod):
                    SelectionV2.SelectionModeSelected = (SelectionPopup.SelectionMode)Settings.Canvas.SelectionMethod;
                    break;
            }
        }

        private void HandlePowerPointSettingChanged(string propertyName)
        {
            switch (propertyName)
            {
                case nameof(PowerPointSettings.PowerPointSupport):
                    if (Settings.PowerPointSettings.PowerPointSupport)
                        timerCheckPPT.Start();
                    else
                        timerCheckPPT.Stop();
                    break;
                case nameof(PowerPointSettings.ShowPPTButton):
                case nameof(PowerPointSettings.PPTButtonsDisplayOption):
                case nameof(PowerPointSettings.PPTLSButtonPosition):
                case nameof(PowerPointSettings.PPTRSButtonPosition):
                    if (BorderFloatingBarExitPPTBtn.Visibility == Visibility.Visible) UpdatePPTBtnDisplaySettingsStatus();
                    break;
                case nameof(PowerPointSettings.PPTSButtonsOption):
                case nameof(PowerPointSettings.PPTBButtonsOption):
                    if (BorderFloatingBarExitPPTBtn.Visibility == Visibility.Visible) UpdatePPTBtnStyleSettingsStatus();
                    break;
            }
        }

        private void HandleAutomationSettingChanged(string propertyName)
        {
            // Check if it's an auto-fold property
            if (propertyName.StartsWith("IsAutoFold"))
            {
                // StartOrStoptimerCheckAutoFold(); // Removed
            }
            // Check if it's an auto-kill property
            else if (propertyName.StartsWith("IsAutoKill"))
            {
                if (Settings.Automation.IsAutoKillEasiNote || Settings.Automation.IsAutoKillPptService ||
                    Settings.Automation.IsAutoKillHiteAnnotation || Settings.Automation.IsAutoKillInkCanvas
                    || Settings.Automation.IsAutoKillICA || Settings.Automation.IsAutoKillIDT || Settings.Automation.IsAutoKillVComYouJiao
                    || Settings.Automation.IsAutoKillSeewoLauncher2DesktopAnnotation)
                    timerKillProcess.Start();
                else
                    timerKillProcess.Stop();
            }
        }

        private void HandleGestureSettingChanged(string propertyName)
        {
            switch (propertyName)
            {
                case nameof(GestureSettings.IsEnableTwoFingerZoom):
                case nameof(GestureSettings.IsEnableTwoFingerTranslate):
                case nameof(GestureSettings.IsEnableTwoFingerRotation):
                    CheckEnableTwoFingerGestureBtnColorPrompt();
                    break;
                case nameof(GestureSettings.IsEnableMultiTouchMode):
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
                    break;
                case nameof(GestureSettings.DisableGestureEraser):
                    break;
            }
        }

        private void HandleAdvancedSettingChanged(string propertyName)
        {
            switch (propertyName)
            {
                case nameof(AdvancedSettings.IsSpecialScreen):
                    break;
                case nameof(AdvancedSettings.IsEnableEdgeGestureUtil):
                    if (OSVersion.GetOperatingSystem() >= OSVersionExtension.OperatingSystem.Windows10)
                        EdgeGestureUtil.DisableEdgeGestures(new WindowInteropHelper(this).Handle, Settings.Advanced.IsEnableEdgeGestureUtil);
                    break;
                case nameof(AdvancedSettings.IsEnableForceFullScreen):
                    if (Settings.Advanced.IsEnableForceFullScreen)
                    {
                        MainWindow_OnSizeChanged(this, null);
                    }
                    break;
            }
        }

        private void HandleStartupSettingChanged(string propertyName)
        {
            switch (propertyName)
            {
                case nameof(StartupSettings.IsEnableNibMode):
                    if (Settings.Startup.IsEnableNibMode)
                        BoundsWidth = Settings.Advanced.NibModeBoundsWidth;
                    else
                        BoundsWidth = Settings.Advanced.FingerModeBoundsWidth;
                    break;
                case nameof(StartupSettings.IsAutoUpdate):
                    break;
                case nameof(StartupSettings.IsAutoUpdateWithSilence):
                    break;
            }
        }

        private void HandleInkToShapeSettingChanged(string propertyName)
        {
            switch (propertyName)
            {
                case nameof(InkToShapeSettings.IsInkToShapeEnabled):
                    PenPaletteV2.InkRecognition = Settings.InkToShape.IsInkToShapeEnabled;
                    break;
            }
        }

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

            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
            SystemEvents_UserPreferenceChanged(null, null);

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
