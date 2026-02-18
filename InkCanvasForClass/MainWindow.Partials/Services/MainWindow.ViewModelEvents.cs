using Ink_Canvas.Core;
using Ink_Canvas.Helpers;
using Ink_Canvas.Services;
using Ink_Canvas.ViewModels;
using Ink_Canvas.Services.Events;
using Ink_Canvas.Models.Settings;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Input;
using Ink_Canvas.Popups;

namespace Ink_Canvas {
    public partial class MainWindow {
        
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
    }
}
