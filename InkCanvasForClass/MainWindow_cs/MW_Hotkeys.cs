// ============================================================================
// MW_Hotkeys.cs - 热键和键盘事件处理
// ============================================================================
// 
// 功能说明:
//   - 键盘快捷键处理（方向键、Page Up/Down、Ctrl+Z/Y 等）
//   - 鼠标滚轮事件处理（PPT 翻页）
//   - 热键到 ViewModel 命令的委托
//
// 迁移状态 (渐进式迁移):
//   - HotkeyService 已创建，提供热键注册和事件
//   - 此文件中的事件处理程序委托到 ViewModel 命令
//   - 完全迁移后，热键处理将完全由 HotkeyService 管理
//
// 相关文件:
//   - Services/HotkeyService.cs
//   - Services/IHotkeyService.cs
//   - ViewModels/MainWindowViewModel.cs
//
// ============================================================================

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using Ink_Canvas.Helpers;
using static Ink_Canvas.Popups.ColorPalette;

namespace Ink_Canvas {
    public partial class MainWindow : Window {
        private void Window_MouseWheel(object sender, MouseWheelEventArgs e) {
            if (BorderFloatingBarExitPPTBtn.Visibility != Visibility.Visible || currentMode != 0) return;
            if (e.Delta >= 120)
                BtnPPTSlidesUp_Click(null, null);
            else if (e.Delta <= -120) BtnPPTSlidesDown_Click(null, null);
        }

        private void Main_Grid_PreviewKeyDown(object sender, KeyEventArgs e) {
            if (currentMode != 0) {
                if (e.Key == Key.Down || e.Key == Key.PageDown || e.Key == Key.Right || e.Key == Key.N)
                    BtnWhiteBoardSwitchNext_Click(null, null);
                if (e.Key == Key.Up || e.Key == Key.PageUp || e.Key == Key.Left || e.Key == Key.P)
                    BtnWhiteBoardSwitchPrevious_Click(null, null);
            } else if (BorderFloatingBarExitPPTBtn.Visibility == Visibility.Visible || currentMode == 0) {
                if (e.Key == Key.Down || e.Key == Key.PageDown || e.Key == Key.Right || e.Key == Key.N ||
                    e.Key == Key.Space) BtnPPTSlidesDown_Click(null, null);
                if (e.Key == Key.Up || e.Key == Key.PageUp || e.Key == Key.Left || e.Key == Key.P)
                    BtnPPTSlidesUp_Click(null, null);
            };
            if (e.Key == Key.LeftCtrl) {
                Trace.WriteLine("KeyDown");
                isControlKeyDown = true;
                ControlKeyDownEvent?.Invoke(this,e);
            }
            if (e.Key == Key.LeftShift) {
                Trace.WriteLine("KeyDown");
                isShiftKeyDown = true;
                ShiftKeyDownEvent?.Invoke(this,e);
            }
            if (isControlKeyDown && e.Key == Key.A) {
                SelectionV2.InvokeSelectAll();
                e.Handled = true;
            }
        }

        public bool isControlKeyDown = false;
        public bool isShiftKeyDown = false;

        public event EventHandler<KeyEventArgs> ControlKeyDownEvent;
        public event EventHandler<KeyEventArgs> ShiftKeyDownEvent;
        public event EventHandler<KeyEventArgs> ControlKeyUpEvent;
        public event EventHandler<KeyEventArgs> ShiftKeyUpEvent;

        private void Main_Grid_PreviewKeyUp(object sender, KeyEventArgs e) {
            if (e.Key == Key.LeftCtrl) {
                isControlKeyDown = false;
                ControlKeyUpEvent?.Invoke(this,e);
            };
            if (e.Key == Key.LeftShift) {
                isShiftKeyDown = false;
                ShiftKeyUpEvent?.Invoke(this,e);
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Escape) KeyExit(null, null);
        }

        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = true;
        }

        #region 热键处理 - 委托到 ViewModel 命令

        /// <summary>
        /// 撤销热键 (Ctrl+Z) - 委托到 ViewModel.UndoCommand
        /// </summary>
        private void HotKey_Undo(object sender, ExecutedRoutedEventArgs e) {
            try {
                if (ViewModel?.UndoCommand?.CanExecute(null) == true) {
                    ViewModel.UndoCommand.Execute(null);
                }
            }
            catch (Exception ex) {
                LogHelper.WriteLogToFile("Exception in MW_Hotkeys.cs HotKey_Undo: " + ex.Message, LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 重做热键 (Ctrl+Y) - 委托到 ViewModel.RedoCommand
        /// </summary>
        private void HotKey_Redo(object sender, ExecutedRoutedEventArgs e) {
            try {
                if (ViewModel?.RedoCommand?.CanExecute(null) == true) {
                    ViewModel.RedoCommand.Execute(null);
                }
            }
            catch (Exception ex) {
                LogHelper.WriteLogToFile("Exception in MW_Hotkeys.cs HotKey_Redo: " + ex.Message, LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 清空热键 (Ctrl+E) - 委托到 ViewModel.ClearCanvasCommand
        /// </summary>
        private void HotKey_Clear(object sender, ExecutedRoutedEventArgs e) {
            try {
                if (ViewModel?.ClearCanvasCommand?.CanExecute(null) == true) {
                    ViewModel.ClearCanvasCommand.Execute(null);
                }
            }
            catch (Exception ex) {
                LogHelper.WriteLogToFile("Exception in MW_Hotkeys.cs HotKey_Clear: " + ex.Message, LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 退出热键 (Escape) - 退出PPT演示模式
        /// </summary>
        private void KeyExit(object sender, ExecutedRoutedEventArgs e) {
            if (BorderFloatingBarExitPPTBtn.Visibility == Visibility.Visible) BtnPPTSlideShowEnd_Click(null, null);
        }

        /// <summary>
        /// 切换到绘图工具热键 (Alt+D) - 委托到 ViewModel.ChangeToDrawToolCommand
        /// </summary>
        private void KeyChangeToDrawTool(object sender, ExecutedRoutedEventArgs e) {
            try {
                if (ViewModel?.ChangeToDrawToolCommand?.CanExecute(null) == true) {
                    ViewModel.ChangeToDrawToolCommand.Execute(null);
                }
                // 同时触发 UI 更新（保持兼容性）
                PenIcon_Click(lastBorderMouseDownObject, null);
            }
            catch (Exception ex) {
                LogHelper.WriteLogToFile("Exception in MW_Hotkeys.cs KeyChangeToDrawTool: " + ex.Message, LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 退出绘图工具热键 (Alt+Q) - 委托到 ViewModel.QuitDrawToolCommand
        /// </summary>
        private void KeyChangeToQuitDrawTool(object sender, ExecutedRoutedEventArgs e) {
            try {
                if (currentMode != 0) ImageBlackboard_MouseUp(lastBorderMouseDownObject, null);
                if (ViewModel?.QuitDrawToolCommand?.CanExecute(null) == true) {
                    ViewModel.QuitDrawToolCommand.Execute(null);
                }
                // 同时触发 UI 更新（保持兼容性）
                CursorIcon_Click(lastBorderMouseDownObject, null);
            }
            catch (Exception ex) {
                LogHelper.WriteLogToFile("Exception in MW_Hotkeys.cs KeyChangeToQuitDrawTool: " + ex.Message, LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 切换到选择工具热键 (Alt+S) - 委托到 ViewModel.ChangeToSelectCommand
        /// </summary>
        private void KeyChangeToSelect(object sender, ExecutedRoutedEventArgs e) {
            try {
                if (StackPanelCanvasControls.Visibility == Visibility.Visible) {
                    if (ViewModel?.ChangeToSelectCommand?.CanExecute(null) == true) {
                        ViewModel.ChangeToSelectCommand.Execute(null);
                    }
                    // 同时触发 UI 更新（保持兼容性）
                    SymbolIconSelect_MouseUp(lastBorderMouseDownObject, null);
                }
            }
            catch (Exception ex) {
                LogHelper.WriteLogToFile("Exception in MW_Hotkeys.cs KeyChangeToSelect: " + ex.Message, LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 切换到橡皮擦热键 (Alt+E) - 委托到 ViewModel.ChangeToEraserCommand
        /// </summary>
        private void KeyChangeToEraser(object sender, ExecutedRoutedEventArgs e) {
            try {
                if (StackPanelCanvasControls.Visibility == Visibility.Visible) {
                    if (ViewModel?.ChangeToEraserCommand?.CanExecute(null) == true) {
                        ViewModel.ChangeToEraserCommand.Execute(null);
                    }
                    // 保持原有 UI 逻辑（根据当前状态切换）
                    if (Eraser_Icon.Background != null)
                        EraserIconByStrokes_Click(lastBorderMouseDownObject, null);
                    else
                        EraserIcon_Click(lastBorderMouseDownObject, null);
                }
            }
            catch (Exception ex) {
                LogHelper.WriteLogToFile("Exception in MW_Hotkeys.cs KeyChangeToEraser: " + ex.Message, LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 切换到画板热键 (Alt+B) - 委托到 ViewModel.ChangeToBoardCommand
        /// </summary>
        private void KeyChangeToBoard(object sender, ExecutedRoutedEventArgs e) {
            try {
                if (ViewModel?.ChangeToBoardCommand?.CanExecute(null) == true) {
                    ViewModel.ChangeToBoardCommand.Execute(null);
                }
                // 同时触发 UI 更新（保持兼容性）
                ImageBlackboard_MouseUp(lastBorderMouseDownObject, null);
            }
            catch (Exception ex) {
                LogHelper.WriteLogToFile("Exception in MW_Hotkeys.cs KeyChangeToBoard: " + ex.Message, LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 截图热键 (Alt+C) - 委托到 ViewModel.CaptureCommand
        /// </summary>
        private void KeyCapture(object sender, ExecutedRoutedEventArgs e) {
            try {
                if (ViewModel?.CaptureCommand?.CanExecute(null) == true) {
                    ViewModel.CaptureCommand.Execute(null);
                }
            }
            catch (Exception ex) {
                LogHelper.WriteLogToFile("Exception in MW_Hotkeys.cs KeyCapture: " + ex.Message, LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 绘制直线热键 (Alt+L) - 委托到 ViewModel.DrawLineCommand
        /// </summary>
        private void KeyDrawLine(object sender, ExecutedRoutedEventArgs e) {
            try {
                if (StackPanelCanvasControls.Visibility == Visibility.Visible) {
                    if (ViewModel?.DrawLineCommand?.CanExecute(null) == true) {
                        ViewModel.DrawLineCommand.Execute(null);
                    }
                    // 保持原有 UI 触发
                    BtnDrawLine_Click(lastMouseDownSender, null);
                }
            }
            catch (Exception ex) {
                LogHelper.WriteLogToFile("Exception in MW_Hotkeys.cs KeyDrawLine: " + ex.Message, LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 隐藏/显示热键 (Alt+V) - 委托到 ViewModel.ToggleHideCommand
        /// </summary>
        private void KeyHide(object sender, ExecutedRoutedEventArgs e) {
            try {
                if (ViewModel?.ToggleHideCommand?.CanExecute(null) == true) {
                    ViewModel.ToggleHideCommand.Execute(null);
                }
            }
            catch (Exception ex) {
                LogHelper.WriteLogToFile("Exception in MW_Hotkeys.cs KeyHide: " + ex.Message, LogHelper.LogType.Error);
            }
        }

        #endregion
    }
}