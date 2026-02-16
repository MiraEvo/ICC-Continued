// ============================================================================
// MW_FloatingBarIcons.cs - 浮动工具栏图标和交互逻辑
// ============================================================================
//
// 功能说明:
//   - 浮动工具栏的拖动实现
//   - 工具按钮的点击事件处理（光标、画笔、橡皮、套索等）
//   - 工具栏状态管理和按钮高亮
//   - 子面板的显示/隐藏控制
//   - 撤销/重做按钮逻辑
//   - 白板模式切换
//
// 迁移状态 (渐进式迁移):
//   - 工具按钮命令已通过 behaviors:FloatingBarButton.Command 绑定到 FloatingBarViewModel
//   - FloatingBarView UserControl 已创建但暂时隐藏 (Visibility="Collapsed")
//   - 此文件中的事件处理程序仍在使用，作为 ViewModel 命令的实际执行逻辑
//   - 完全迁移后，此文件中的 UI 交互代码将移至 FloatingBarView.xaml.cs
//
// 相关文件:
//   - Views/FloatingBar/FloatingBarView.xaml
//   - Views/FloatingBar/FloatingBarView.xaml.cs
//   - ViewModels/FloatingBarViewModel.cs
//   - MainWindow.xaml (ViewboxFloatingBar 区域)
//
// ============================================================================

using Ink_Canvas.Dialogs;
using Ink_Canvas.Helpers;
using Ink_Canvas.Popups;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Threading;
using Application = System.Windows.Application;
using Point = System.Windows.Point;
using System.Diagnostics;
using System.IO;
using System.Windows.Media.Effects;
using System.Text;
using System.Globalization;
using System.Windows.Data;
using System.Xml.Linq;
using Image = System.Windows.Controls.Image;

namespace Ink_Canvas {
    public partial class MainWindow : System.Windows.Window {
        #region 常用颜色画刷缓存

        private static readonly SolidColorBrush CachedTransparentBrush = new SolidColorBrush(Colors.Transparent);
        private static readonly SolidColorBrush CachedZincGray100Brush = new SolidColorBrush(Color.FromRgb(248, 250, 252)); // Slate-50
        private static readonly SolidColorBrush CachedZincGray400Brush = new SolidColorBrush(Color.FromRgb(209, 213, 219)); // Gray-300
        private static readonly SolidColorBrush CachedZincGray900Brush = new SolidColorBrush(Color.FromRgb(31, 41, 55)); // Gray-800
        private static readonly SolidColorBrush CachedBlue600Brush = new SolidColorBrush(Color.FromRgb(37, 99, 235));
        private static readonly SolidColorBrush CachedGhostWhiteBrush = new SolidColorBrush(Colors.GhostWhite);
        private static readonly SolidColorBrush CachedInkReplayButtonPressedBrush = new SolidColorBrush(Color.FromArgb(34, 9, 9, 11));

        private int _lastUndoTick;
        private int _lastRedoTick;
        private const int UndoRedoMinIntervalMs = 120;

        #endregion

        #region "手勢"按鈕

        /// <summary>
        /// 隐藏所有常用子面板（用于切换面板时先隐藏其他面板）
        /// </summary>
        private void HideCommonSubPanels() {
            AnimationsHelper.HideWithSlideAndFade(EraserSizePanel);
            AnimationsHelper.HideWithSlideAndFade(BorderTools);
            AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);
            // AnimationsHelper.HideWithSlideAndFade(PenPalette);
            // AnimationsHelper.HideWithSlideAndFade(BoardPenPalette);
            AnimationsHelper.HideWithSlideAndFade(BorderDrawShape);
            AnimationsHelper.HideWithSlideAndFade(BoardBorderDrawShape);
            AnimationsHelper.HideWithSlideAndFade(BoardEraserSizePanel);
            AnimationsHelper.HideWithSlideAndFade(BoardBackgroundPopup);
        }

        /// <summary>
        /// 用於浮動工具欄的"手勢"按鈕和白板工具欄的"手勢"按鈕的點擊事件
        /// </summary>
        private void TwoFingerGestureBorder_MouseUp(object sender, RoutedEventArgs e) {
            HideCommonSubPanels();

            if (TwoFingerGestureBorder.Visibility == Visibility.Visible) {
                AnimationsHelper.HideWithSlideAndFade(TwoFingerGestureBorder);
                AnimationsHelper.HideWithSlideAndFade(BoardTwoFingerGestureBorder);
            } else {
                AnimationsHelper.ShowWithSlideFromBottomAndFade(TwoFingerGestureBorder);
                AnimationsHelper.ShowWithSlideFromBottomAndFade(BoardTwoFingerGestureBorder);
            }
        }

        /// <summary>
        /// 鐢ㄦ柤鏇存柊娴嫊宸ュ叿娆勭殑"鎵嬪嫝"鎸夐垥鍜岀櫧鏉垮伐鍏锋瑒鐨?鎵嬪嫝"鎸夐垥鐨勬ǎ寮忥紙闁嬪暉鍜岄棞闁夌媭鎱嬶級
        /// </summary>
        private void CheckEnableTwoFingerGestureBtnColorPrompt() {
            if (Settings.Gesture.DefaultMultiPointHandWritingMode == 0) {
                SetBoardGestureDisabledStyle();
            } else {
                if (Settings.Gesture.IsEnableTwoFingerGesture) {
                    SetBoardGestureEnabledStyle();
                } else {
                    SetBoardGestureDisabledStyle();
                }
            }
        }

        /// <summary>
        /// 璁剧疆鐧芥澘鎵嬪娍鎸夐挳涓虹鐢ㄦ牱寮?
        /// </summary>
        private void SetBoardGestureDisabledStyle() {
            BoardGesture.Background = CachedZincGray100Brush;
            BoardGestureGeometry.Brush = CachedZincGray900Brush;
            BoardGestureGeometry2.Brush = CachedZincGray900Brush;
            BoardGestureLabel.Foreground = CachedZincGray900Brush;
            BoardGesture.BorderBrush = CachedZincGray400Brush;
            BoardGestureGeometry.Geometry = Geometry.Parse(XamlGraphicsIconGeometries.DisabledGestureIcon);
            BoardGestureGeometry2.Geometry = Geometry.Parse("F0 M24,24z M0,0z");
        }

        /// <summary>
        /// 设置白板手势按钮为启用样式
        /// </summary>
        private void SetBoardGestureEnabledStyle() {
            BoardGesture.Background = CachedBlue600Brush;
            BoardGestureGeometry.Brush = CachedGhostWhiteBrush;
            BoardGestureGeometry2.Brush = CachedGhostWhiteBrush;
            BoardGestureLabel.Foreground = CachedGhostWhiteBrush;
            BoardGesture.BorderBrush = CachedBlue600Brush;
            BoardGestureGeometry.Geometry = Geometry.Parse(XamlGraphicsIconGeometries.EnabledGestureIcon);
            BoardGestureGeometry2.Geometry =
                Geometry.Parse("F0 M24,24z M0,0z " + XamlGraphicsIconGeometries.EnabledGestureIconBadgeCheck);
        }

        /// <summary>
        /// 控制是否顯示浮動工具欄的“手勢”按鈕
        /// </summary>
        private void CheckEnableTwoFingerGestureBtnVisibility(bool isVisible) {
            // EnableTwoFingerGestureBorder has been removed
        }

        #endregion “手勢”按鈕

        #region 浮動工具欄的拖動實現

        private bool isDragDropInEffect = false;
        private Point pos = new Point();
        private Point downPos = new Point();
        private Point pointDesktop = new Point(-1, -1); //用于记录上次在桌面时的坐标
        private Point pointPPT = new Point(-1, -1); //用于记录上次在PPT中的坐标

        private void SymbolIconEmoji_MouseMove(object sender, MouseEventArgs e) {
            if (isDragDropInEffect) {
                var xPos = e.GetPosition(null).X - pos.X + ViewboxFloatingBar.Margin.Left;
                var yPos = e.GetPosition(null).Y - pos.Y + ViewboxFloatingBar.Margin.Top;
                ViewboxFloatingBar.Margin = new Thickness(xPos, yPos, -2000, -200);

                pos = e.GetPosition(null);
                if (BorderFloatingBarExitPPTBtn.Visibility == Visibility.Visible)
                    pointPPT = new Point(xPos, yPos);
                else
                    pointDesktop = new Point(xPos, yPos);
            }
        }

        private void SymbolIconEmoji_MouseDown(object sender, MouseButtonEventArgs e) {
            if (isViewboxFloatingBarMarginAnimationRunning) {
                ViewboxFloatingBar.BeginAnimation(FrameworkElement.MarginProperty, null);
                isViewboxFloatingBarMarginAnimationRunning = false;
            }

            isDragDropInEffect = true;
            pos = e.GetPosition(null);
            downPos = e.GetPosition(null);
            GridForFloatingBarDraging.Visibility = Visibility.Visible;
        }

        private void SymbolIconEmoji_MouseUp(object sender, MouseButtonEventArgs e) {
            isDragDropInEffect = false;

            if (e is null || (Math.Abs(downPos.X - e.GetPosition(null).X) <= 10 &&
                              Math.Abs(downPos.Y - e.GetPosition(null).Y) <= 10)) {
                if (BorderFloatingBarMainControls.Visibility == Visibility.Visible) {
                    BorderFloatingBarMainControls.Visibility = Visibility.Collapsed;
                    CheckEnableTwoFingerGestureBtnVisibility(false);
                } else {
                    BorderFloatingBarMainControls.Visibility = Visibility.Visible;
                    CheckEnableTwoFingerGestureBtnVisibility(true);
                }
            }

            GridForFloatingBarDraging.Visibility = Visibility.Collapsed;
        }

        #endregion 浮動工具欄的拖動實現

        #region 隱藏子面板和按鈕背景高亮

        /// <summary>
        /// 隱藏形狀繪製面板
        /// </summary>
        private void CollapseBorderDrawShape() {
            AnimationsHelper.HideWithSlideAndFade(BorderDrawShape);
            AnimationsHelper.HideWithSlideAndFade(BoardBorderDrawShape);
        }

        /// <summary>
        ///     <c>HideSubPanels</c>的青春版。目前需要修改<c>BorderSettings</c>的關閉機制（改為動畫關閉）。
        /// </summary>
        private void HideSubPanelsImmediately() {
            BorderTools.Visibility = Visibility.Collapsed;
            BoardBorderTools.Visibility = Visibility.Collapsed;
            // PenPalette.Visibility = Visibility.Collapsed;
            // BoardPenPalette.Visibility = Visibility.Collapsed;
            BoardEraserSizePanel.Visibility = Visibility.Collapsed;
            EraserSizePanel.Visibility = Visibility.Collapsed;
            // BorderSettings.Visibility = Visibility.Collapsed; // Removed
            //BorderSettingsMask.Visibility = Visibility.Collapsed;
            //SettingsOverlay.Visibility = Visibility.Collapsed;
            BoardBorderLeftPageListView.Visibility = Visibility.Collapsed;
            BoardBorderRightPageListView.Visibility = Visibility.Collapsed;
            BoardBackgroundPopup.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        ///     <para>
        ///         易嚴定真，這個多功能函數包括了以下的內容：
        ///     </para>
        ///     <list type="number">
        ///         <item>
        ///             隱藏浮動工具欄和白板模式下的“更多功能”面板
        ///         </item>
        ///         <item>
        ///             隱藏白板模式下和浮動工具欄的畫筆調色盤
        ///         </item>
        ///         <item>
        ///             隱藏白板模式下的“清屏”按鈕（已作廢）
        ///         </item>
        ///         <item>
        ///             負責給Settings設置面板做隱藏動畫
        ///         </item>
        ///         <item>
        ///             隱藏白板模式下和浮動工具欄的“手勢”面板
        ///         </item>
        ///         <item>
        ///             當<c>ToggleSwitchDrawShapeBorderAutoHide</c>開啟時，會自動隱藏白板模式下和浮動工具欄的“形狀”面板
        ///         </item>
        ///         <item>
        ///             按需高亮指定的浮動工具欄和白板工具欄中的按鈕，通過param：<paramref name="mode"/> 來指定
        ///         </item>
        ///         <item>
        ///             將浮動工具欄自動居中，通過param：<paramref name="autoAlignCenter"/>
        ///         </item>
        ///     </list>
        /// </summary>
        /// <param name="mode">
        ///     <para>
        ///         按需高亮指定的浮動工具欄和白板工具欄中的按鈕，有下面幾種情況：
        ///     </para>
        ///     <list type="number">
        ///         <item>
        ///             當<c><paramref name="mode"/>==null</c>時，不會執行任何有關操作
        ///         </item>
        ///         <item>
        ///             當<c><paramref name="mode"/>!="clear"</c>時，會先取消高亮所有工具欄按鈕，然後根據下面的情況進行高亮處理
        ///         </item>
        ///         <item>
        ///             當<c><paramref name="mode"/>=="color" || <paramref name="mode"/>=="pen"</c>時，會高亮浮動工具欄和白板工具欄中的“批註”，“筆”按鈕
        ///         </item>
        ///         <item>
        ///             當<c><paramref name="mode"/>=="eraser"</c>時，會高亮白板工具欄中的“橡皮”和浮動工具欄中的“面積擦”按鈕
        ///         </item>
        ///         <item>
        ///             當<c><paramref name="mode"/>=="eraserByStrokes"</c>時，會高亮白板工具欄中的“橡皮”和浮動工具欄中的“墨跡擦”按鈕
        ///         </item>
        ///         <item>
        ///             當<c><paramref name="mode"/>=="select"</c>時，會高亮浮動工具欄和白板工具欄中的“選擇”，“套索選”按鈕
        ///         </item>
        ///     </list>
        /// </param>
        /// <param name="autoAlignCenter">
        ///     是否自動居中浮動工具欄
        /// </param>
        private async Task HideSubPanelsAsync(string mode = null, bool autoAlignCenter = false) {
            AnimationsHelper.HideWithSlideAndFade(BorderTools);
            AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);
            // AnimationsHelper.HideWithSlideAndFade(PenPalette);
            // AnimationsHelper.HideWithSlideAndFade(BoardPenPalette);
            AnimationsHelper.HideWithSlideAndFade(BoardEraserSizePanel);
            AnimationsHelper.HideWithSlideAndFade(EraserSizePanel);
            AnimationsHelper.HideWithSlideAndFade(BorderDrawShape);
            AnimationsHelper.HideWithSlideAndFade(BoardBorderLeftPageListView);
            AnimationsHelper.HideWithSlideAndFade(BoardBorderRightPageListView);
            AnimationsHelper.HideWithSlideAndFade(BoardBackgroundPopup);

            // Removed BorderSettings logic
            //BorderSettingsMask.Visibility = Visibility.Collapsed;
            //SettingsOverlay.Visibility = Visibility.Collapsed;

            AnimationsHelper.HideWithSlideAndFade(TwoFingerGestureBorder);
            AnimationsHelper.HideWithSlideAndFade(EraserSizePanel);
            AnimationsHelper.HideWithSlideAndFade(BoardTwoFingerGestureBorder);
            if (IsDrawShapeBorderAutoHide) {
                AnimationsHelper.HideWithSlideAndFade(BorderDrawShape);
                AnimationsHelper.HideWithSlideAndFade(BoardBorderDrawShape);
            }

            if (mode != null && autoAlignCenter) {
                if (BorderFloatingBarExitPPTBtn.Visibility == Visibility.Visible) {
                    await Task.Delay(10);
                    ViewboxFloatingBarMarginAnimation(60);
                } else if (Topmost == true) //非黑板
                {
                    await Task.Delay(10);
                    ViewboxFloatingBarMarginAnimation(100, true);
                } else //黑板
                {
                    await Task.Delay(10);
                    ViewboxFloatingBarMarginAnimation(60);
                }
            }

            // new popup
            PenPaletteV2Popup.IsOpen = false;
            SelectionPopupV2.IsOpen = false;
            ShapeDrawingPopupV2.IsOpen = false;

            await Task.Delay(20);
            isHidingSubPanelsWhenInking = false;
        }

        /// <summary>
        /// 隐藏子面板（事件处理器包装）
        /// </summary>
        private async void HideSubPanels(string mode = null, bool autoAlignCenter = false) {
            try {
                await HideSubPanelsAsync(mode, autoAlignCenter);
            }
            catch (Exception ex) {
                LogHelper.WriteLogToFile($"隐藏子面板失败: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        #endregion

        #region 撤銷重做按鈕

        private void SymbolIconUndo_MouseUp(object sender, MouseButtonEventArgs e) {
            //if (lastBorderMouseDownObject != sender) return;

            if (lastBorderMouseDownObject is Panel panel)
                panel.Background = CachedTransparentBrush;
            if (sender == SymbolIconUndo && lastBorderMouseDownObject != SymbolIconUndo) {
                if (e?.StylusDevice?.TabletDevice.Type != TabletDeviceType.Touch) return;
            }

            if (!SymbolIconUndo.IsEnabled) return;
            BtnUndo_Click(null, null);
            HideSubPanels();
        }

        private void SymbolIconRedo_MouseUp(object sender, MouseButtonEventArgs e) {
            //if (lastBorderMouseDownObject != sender) return;

            if (lastBorderMouseDownObject is Panel panel)
                panel.Background = CachedTransparentBrush;
            if (sender == SymbolIconRedo && lastBorderMouseDownObject != SymbolIconRedo) {
                if (e?.StylusDevice?.TabletDevice.Type != TabletDeviceType.Touch) return;
            }

            if (!SymbolIconRedo.IsEnabled) return;
            BtnRedo_Click(null, null);
            HideSubPanels();
        }

        #endregion

        #region 白板按鈕和退出白板模式按鈕

        //private bool Not_Enter_Blackboard_fir_Mouse_Click = true;
        private bool isDisplayingOrHidingBlackboard = false;

        private void ImageBlackboard_MouseUp(object sender, MouseButtonEventArgs e) {

            if (lastBorderMouseDownObject is Panel panel)
                panel.Background = CachedTransparentBrush;
            if (sender == WhiteboardFloatingBarBtn && lastBorderMouseDownObject != WhiteboardFloatingBarBtn) return;

            LeftUnFoldButtonQuickPanel.Visibility = Visibility.Collapsed;
            RightUnFoldButtonQuickPanel.Visibility = Visibility.Collapsed;
            if (isDisplayingOrHidingBlackboard) return;
            isDisplayingOrHidingBlackboard = true;

            UnFoldFloatingBar_MouseUp(null, null);

            if (SelectedMode == ICCToolsEnum.CursorMode || SelectedMode == ICCToolsEnum.LassoMode) PenIcon_Click(null, null);

            if (currentMode == 0) {
                LeftBottomPanelForPPTNavigation.Visibility = Visibility.Collapsed;
                RightBottomPanelForPPTNavigation.Visibility = Visibility.Collapsed;
                LeftSidePanelForPPTNavigation.Visibility = Visibility.Collapsed;
                RightSidePanelForPPTNavigation.Visibility = Visibility.Collapsed;
                //进入黑板

                new Thread(new ThreadStart(() => {
                    Thread.Sleep(100);
                    Application.Current.Dispatcher.Invoke(() => { ViewboxFloatingBarMarginAnimation(60); });
                })).Start();

                HideSubPanels();
                if (GridTransparencyFakeBackground.Background == Brushes.Transparent) {
                    GridBackgroundCover.Visibility = Visibility.Collapsed;
                    AnimationsHelper.HideWithSlideAndFade(BlackboardLeftSide);
                    AnimationsHelper.HideWithSlideAndFade(BlackboardCenterSide);
                    AnimationsHelper.HideWithSlideAndFade(BlackboardRightSide);
                }

                BtnHideInkCanvas_Click(null, null);

                if (Settings.Gesture.AutoSwitchTwoFingerGesture) // 自动关闭多指书写、开启双指移动
                {
                    Settings.Gesture.IsEnableTwoFingerTranslate = true;
                    if (isInMultiTouchMode) Settings.Gesture.DefaultMultiPointHandWritingMode = 1;
                    SaveSettings();
                }

                if (Settings.Appearance.EnableTimeDisplayInWhiteboardMode == true) {
                    WaterMarkTime.Visibility = Visibility.Visible;
                    WaterMarkDate.Visibility = Visibility.Visible;
                } else {
                    WaterMarkTime.Visibility = Visibility.Collapsed;
                    WaterMarkDate.Visibility = Visibility.Collapsed;
                }

                if (Settings.Appearance.EnableChickenSoupInWhiteboardMode == true) {
                    BlackBoardWaterMark.Visibility = Visibility.Visible;
                } else {
                    BlackBoardWaterMark.Visibility = Visibility.Collapsed;
                }

                if (Settings.Appearance.ChickenSoupSource == 0) {
                    int randChickenSoupIndex = Random.Shared.Next(ChickenSoup.OSUPlayerYuLu.Length);
                    BlackBoardWaterMark.Text = ChickenSoup.OSUPlayerYuLu[randChickenSoupIndex];
                } else if (Settings.Appearance.ChickenSoupSource == 1) {
                    int randChickenSoupIndex = Random.Shared.Next(ChickenSoup.MingYanJingJu.Length);
                    BlackBoardWaterMark.Text = ChickenSoup.MingYanJingJu[randChickenSoupIndex];
                } else if (Settings.Appearance.ChickenSoupSource == 2) {
                    int randChickenSoupIndex = Random.Shared.Next(ChickenSoup.GaoKaoPhrases.Length);
                    BlackBoardWaterMark.Text = ChickenSoup.GaoKaoPhrases[randChickenSoupIndex];
                }

            } else {
                //关闭黑板
                HideSubPanelsImmediately();

                if (BorderFloatingBarExitPPTBtn.Visibility == Visibility.Visible) {
                    var dops = Settings.PowerPointSettings.PPTButtonsDisplayOption.ToString();
                    var dopsc = dops.ToCharArray();
                    if (dopsc[0] == '2' && isDisplayingOrHidingBlackboard == false)
                        AnimationsHelper.ShowWithFadeIn(LeftBottomPanelForPPTNavigation);
                    if (dopsc[1] == '2' && isDisplayingOrHidingBlackboard == false)
                        AnimationsHelper.ShowWithFadeIn(RightBottomPanelForPPTNavigation);
                    if (dopsc[2] == '2' && isDisplayingOrHidingBlackboard == false)
                        AnimationsHelper.ShowWithFadeIn(LeftSidePanelForPPTNavigation);
                    if (dopsc[3] == '2' && isDisplayingOrHidingBlackboard == false)
                        AnimationsHelper.ShowWithFadeIn(RightSidePanelForPPTNavigation);
                }

                if (Settings.Automation.IsAutoSaveStrokesAtClear &&
                    inkCanvas.Strokes.Count > Settings.Automation.MinimumAutomationStrokeNumber) SaveScreenshot(true);

                if (BorderFloatingBarExitPPTBtn.Visibility == Visibility.Collapsed)
                    new Thread(new ThreadStart(() => {
                        Thread.Sleep(300);
                        Application.Current.Dispatcher.Invoke(() => { ViewboxFloatingBarMarginAnimation(100, true); });
                    })).Start();
                else
                    new Thread(new ThreadStart(() => {
                        Thread.Sleep(300);
                        Application.Current.Dispatcher.Invoke(() => { ViewboxFloatingBarMarginAnimation(60); });
                    })).Start();

                if (SelectedMode != ICCToolsEnum.PenMode) PenIcon_Click(null, null);

                if (Settings.Gesture.AutoSwitchTwoFingerGesture) // 自动启用多指书写
                {
                    Settings.Gesture.IsEnableTwoFingerTranslate = false;
                    SaveSettings();
                }
                // 2024.5.2 need to be tested
                // if (!isInMultiTouchMode) ToggleSwitchEnableMultiTouchMode.IsOn = true;
                WaterMarkTime.Visibility = Visibility.Collapsed;
                WaterMarkDate.Visibility = Visibility.Collapsed;
                BlackBoardWaterMark.Visibility = Visibility.Collapsed;
            }

            // 修复：进入白板模式前确保背景状态和容器可见性正确
            // 当从光标模式点击白板按钮时：
            // 1. PenIcon_Click 设置背景为非透明
            // 2. BtnHideInkCanvas_Click 会把背景设置回透明，并隐藏 GridBackgroundCoverHolder
            // 3. 这导致 BtnSwitch_Click 进入错误的分支，或者即使进入正确分支，白板背景也不可见
            if (currentMode == 0) {
                GridTransparencyFakeBackground.Opacity = 1;
                GridTransparencyFakeBackground.Background = new SolidColorBrush(StringToColor("#01FFFFFF"));
                GridBackgroundCoverHolder.Visibility = Visibility.Visible;
            }

            BtnSwitch_Click(null, null);

            if (currentMode == 0)
            {
                if (Settings.Canvas.HideInkAfterExitBoardMode && BorderFloatingBarExitPPTBtn.Visibility == Visibility.Visible)
                {
                    CursorIcon_Click(null, null);
                }
                else if (inkCanvas.Strokes.Count == 0 && BorderFloatingBarExitPPTBtn.Visibility != Visibility.Visible)
                {
                    CursorIcon_Click(null, null);
                }
            }

            // Legacy theme switch removed; ApplicationThemeManager is used now.

            new Thread(new ThreadStart(() => {
                Thread.Sleep(200);
                Application.Current.Dispatcher.Invoke(() => { isDisplayingOrHidingBlackboard = false; });
            })).Start();

            SwitchToDefaultPen(null, null);
            CheckColorTheme(true);
        }

        #endregion

        #region 清空畫布按鈕

        private void SymbolIconDelete_MouseUp(object sender, MouseButtonEventArgs e) {

            if (lastBorderMouseDownObject is Panel panel)
                panel.Background = CachedTransparentBrush;
            if (sender == SymbolIconDelete && lastBorderMouseDownObject != SymbolIconDelete) return;

            if (inkCanvas.GetSelectedStrokes().Count > 0) {
                inkCanvas.Strokes.Remove(inkCanvas.GetSelectedStrokes());
                // cancel
                GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
                inkCanvas.Opacity = 1;
                InkSelectionStrokesOverlay.Visibility = Visibility.Collapsed;
                InkSelectionStrokesBackgroundInkCanvas.Visibility = Visibility.Collapsed;
                InkSelectionStrokesOverlay.DrawStrokes(new StrokeCollection(), new Matrix());
                UpdateStrokeSelectionBorder(false, null);
                RectangleSelectionHitTestBorder.Visibility = Visibility.Visible;
            } else if (inkCanvas.Strokes.Count > 0) {
                if (Settings.Automation.IsAutoSaveStrokesAtClear &&
                    inkCanvas.Strokes.Count > Settings.Automation.MinimumAutomationStrokeNumber) {
                    if (BorderFloatingBarExitPPTBtn.Visibility == Visibility.Visible)
                        SavePPTScreenshot($"{pptName}/{previousSlideID}_{DateTime.Now:HH-mm-ss}");
                    else
                        SaveScreenshot(true);
                }

                BtnClear_Click(null, null);
            }
        }

        #endregion

        #region 工具栏状态管理和高亮相关

        public enum ICCToolsEnum {
            CursorMode,
            PenMode,
            EraseByStrokeMode,
            EraseByGeometryMode,
            LassoMode,
        }

        public ICCToolsEnum SelectedMode = ICCToolsEnum.CursorMode;

        /// <summary>
        ///     设置当前正在使用的工具和浮动工具栏、白板按钮的状态和样式管理
        /// </summary>
        /// <param name="customMode">设置后不遵守SelectedMode，直接使用该变量传入的Mode进行设置</param>
        public void ForceUpdateToolSelection(ICCToolsEnum? customMode) {
            ICCToolsEnum mode = customMode ?? SelectedMode;

            GeometryDrawing[] iconGeometryDrawingsFloatingBar = new GeometryDrawing[] {
                CursorIconGeometry,
                PenIconGeometry,
                StrokeEraserIconGeometry,
                CircleEraserIconGeometry,
                LassoSelectIconGeometry
            };

            StackPanel[] floatingBarIconPanels = new StackPanel[] {
                Cursor_Icon,
                Pen_Icon,
                EraserByStrokes_Icon,
                Eraser_Icon,
                SymbolIconSelect,
                HandFloatingBarBtn,
            };

            TextBlock[] iconTextBlocksFloatingBar = new TextBlock[] {
                SelectionToolBarTextBlock,
                PenToolbarTextBlock,
                InkEraserToolbarTextBlock,
                CircleEraserToolbarTextBlock,
                LassoToolToolbarTextBlock
            };

            string[] iconGeometryPathStringsFloatingBar = new string[] {
                XamlGraphicsIconGeometries.LinedCursorIcon,
                XamlGraphicsIconGeometries.LinedPenIcon,
                XamlGraphicsIconGeometries.LinedEraserStrokeIcon,
                XamlGraphicsIconGeometries.LinedEraserCircleIcon,
                XamlGraphicsIconGeometries.LinedLassoSelectIcon,
                XamlGraphicsIconGeometries.SolidCursorIcon,
                XamlGraphicsIconGeometries.SolidPenIcon,
                XamlGraphicsIconGeometries.SolidEraserStrokeIcon,
                XamlGraphicsIconGeometries.SolidEraserCircleIcon,
                XamlGraphicsIconGeometries.SolidLassoSelectIcon
            };

            Border[] iconBordersWhiteboard = new Border[] {
                BoardSelect,
                BoardPen,
                BoardEraser
            };
            GeometryDrawing[] iconGeometryDrawingsWhiteboard = new GeometryDrawing[] {
                BoardSelectGeometry,
                BoardPenGeometry,
                BoardEraserGeometry
            };
            TextBlock[] iconLabelsWhiteboard = new TextBlock[] {
                BoardSelectLabel,
                BoardPenLabel,
                BoardEraserLabel
            };

            foreach (var tb in iconTextBlocksFloatingBar) {
                tb.Foreground = UIStyleHelper.BlackBrush;
            }

            foreach (var gd in iconGeometryDrawingsFloatingBar) {
                gd.Brush = UIStyleHelper.DarkGrayBrush;
                gd.Geometry = Geometry.Parse(iconGeometryPathStringsFloatingBar[Array.IndexOf(iconGeometryDrawingsFloatingBar, gd)]);
            }

            foreach (var ib in iconBordersWhiteboard) {
                ib.Background = CachedZincGray100Brush;
                ib.BorderBrush = CachedZincGray400Brush;
            }

            foreach (var gd in iconGeometryDrawingsWhiteboard) gd.Brush = CachedZincGray900Brush;
            foreach (var txt in iconLabelsWhiteboard) txt.Foreground = CachedZincGray900Brush;

            FloatingbarSelectionBG.Visibility = Visibility.Hidden;
            System.Windows.Controls.Canvas.SetLeft(FloatingbarSelectionBG, 0);

            var highlightStepWidth = Settings.Appearance.FloatingBarButtonLabelVisibility ? 28 : 21;

            var FloatingBarItemsCalc = new List<FrameworkElement>() {
                Cursor_Icon,
                Pen_Icon,
                SymbolIconDelete,
                EraserByStrokes_Icon,
                Eraser_Icon,
                SymbolIconSelect,
                ShapeDrawFloatingBarBtn,
                FreezeFloatingBarBtn,
                HandFloatingBarBtn,
                SymbolIconUndo,
                SymbolIconRedo,
                CursorWithDelFloatingBarBtn,
            };

            var final_items = new List<FrameworkElement>();
            foreach (var fe in FloatingBarItemsCalc) {
                if (fe.Visibility != Visibility.Collapsed) final_items.Add(fe);
            }

            if (mode != ICCToolsEnum.CursorMode) {
                // floating bar
                var ngdf = iconGeometryDrawingsFloatingBar[(int)mode];
                ngdf.Brush = UIStyleHelper.WhiteBrush;
                iconTextBlocksFloatingBar[(int)mode].Foreground = UIStyleHelper.WhiteBrush;
                ngdf.Geometry = Geometry.Parse(iconGeometryPathStringsFloatingBar[(int)mode+5]);
                FloatingbarSelectionBG.Visibility = Visibility.Visible;
                var iconPosI = final_items.IndexOf(floatingBarIconPanels[(int)mode])*highlightStepWidth;
                System.Windows.Controls.Canvas.SetLeft(FloatingbarSelectionBG, iconPosI);

                // whiteboard
                var wmi = mode == ICCToolsEnum.LassoMode ? 0 :
                    mode == ICCToolsEnum.PenMode ? 1 :
                    mode == ICCToolsEnum.EraseByGeometryMode || mode == ICCToolsEnum.EraseByStrokeMode ? 2 : 0;
                iconBordersWhiteboard[wmi].Background = CachedBlue600Brush;
                iconBordersWhiteboard[wmi].BorderBrush = CachedBlue600Brush;
                iconGeometryDrawingsWhiteboard[wmi].Brush = CachedGhostWhiteBrush;
                iconLabelsWhiteboard[wmi].Foreground = CachedGhostWhiteBrush;
            }

            FloatingbarFreezeBtnBGCanvas.Visibility = mode != ICCToolsEnum.CursorMode ? Visibility.Visible : Visibility.Collapsed;
            if (mode == ICCToolsEnum.CursorMode) IsAnnotationFreezeOn = false;

            // 切换模式时关闭漫游模式
            if (isSingleFingerDragMode && mode != ICCToolsEnum.CursorMode) {
                isSingleFingerDragMode = false;
                UpdateFloatingBarHandIconCheckedStatus();
            }
        }

        #endregion

        #region 画面定格

        private bool _isAnnotationFreezeOn { get; set; } = false;

        private bool IsAnnotationFreezeOn {
            get => _isAnnotationFreezeOn;
            set {
                _isAnnotationFreezeOn = value;
                UpdateFloatingBarFreezeIconCheckedStatus();
                var t = new Thread(() => {
                    ApplyFreezeFrame();
                });
                t.Start();
            }
        }

        private async void ApplyFreezeFrame() {
            if (!isFreezeFrameLoaded) return;
            if (_isAnnotationFreezeOn) {
                var bmp = await GetFreezedFrameAsync();
                _ = Application.Current.Dispatcher.InvokeAsync(() => {
                    FreezeFrameBackgroundImage.Source = BitmapToImageSource(bmp);
                    FreezeFrameBackgroundImage.Visibility = Visibility.Visible;
                });
            } else {
                _ = Application.Current.Dispatcher.InvokeAsync(() => {
                    FreezeFrameBackgroundImage.Source = null;
                    FreezeFrameBackgroundImage.Visibility = Visibility.Collapsed;
                });
            }
        }

        private void UpdateFloatingBarFreezeIconCheckedStatus() {
            if (IsAnnotationFreezeOn) {
                FreezeIconGeometry.Brush = UIStyleHelper.WhiteBrush;
                FreezeToolbarTextBlock.Foreground = UIStyleHelper.WhiteBrush;
                FloatingbarFreezeBtnBG.Visibility = Visibility.Visible;
                var transform = FloatingbarFreezeBtnBG.TransformToVisual(BorderFloatingBarMainControls);
                var pt = transform.Transform(new Point(0, 0));
                System.Windows.Controls.Canvas.SetLeft(FloatingbarFreezeBtnBG, pt.X-5);
            } else {
                FreezeIconGeometry.Brush = UIStyleHelper.DarkGrayBrush;
                FreezeToolbarTextBlock.Foreground = UIStyleHelper.BlackBrush;
                FloatingbarFreezeBtnBG.Visibility = Visibility.Hidden;
            }
        }

        private void UpdateFloatingBarHandIconCheckedStatus() {
            if (isSingleFingerDragMode) {
                HandIconGeometry.Brush = UIStyleHelper.WhiteBrush;
                HandToolbarTextBlock.Foreground = UIStyleHelper.WhiteBrush;
                FloatingbarHandBtnBG.Visibility = Visibility.Visible;
                var transform = FloatingbarHandBtnBG.TransformToVisual(BorderFloatingBarMainControls);
                var pt = transform.Transform(new Point(0, 0));
                System.Windows.Controls.Canvas.SetLeft(FloatingbarHandBtnBG, pt.X-5);
            } else {
                HandIconGeometry.Brush = UIStyleHelper.DarkGrayBrush;
                HandToolbarTextBlock.Foreground = UIStyleHelper.BlackBrush;
                FloatingbarHandBtnBG.Visibility = Visibility.Hidden;
            }
        }

        #endregion

        #region 主要的工具按鈕事件

        private async void SymbolIconCursor_Click(object sender, RoutedEventArgs e) {
            if (ShapeDrawingV2Layer.IsInShapeDrawingMode) ShapeDrawingV2Layer.EndShapeDrawing();
            if (currentMode != 0) {
                ImageBlackboard_MouseUp(null, null);
            } else {
                BtnHideInkCanvas_Click(null, null);

                if (BorderFloatingBarExitPPTBtn.Visibility == Visibility.Visible) {
                    await Task.Delay(100);
                    ViewboxFloatingBarMarginAnimation(60);
                }
            }
        }

        private void FreezeFloatingBarBtn_MouseUp(object sender, MouseButtonEventArgs e) {
            if (lastBorderMouseDownObject is Panel panel)
                panel.Background = CachedTransparentBrush;
            if (sender == FreezeFloatingBarBtn && lastBorderMouseDownObject != FreezeFloatingBarBtn) return;

            IsAnnotationFreezeOn = !IsAnnotationFreezeOn;
        }

        private async void CursorIcon_Click(object sender, RoutedEventArgs e)
        {
            if (lastBorderMouseDownObject is Panel panel)
                panel.Background = CachedTransparentBrush;
            if (sender == Cursor_Icon && lastBorderMouseDownObject != Cursor_Icon) return;

            if (ShapeDrawingV2Layer.IsInShapeDrawingMode) ShapeDrawingV2Layer.EndShapeDrawing();

            // 切换前自动截图保存墨迹
            if (inkCanvas.Strokes.Count > 0 &&
                inkCanvas.Strokes.Count > Settings.Automation.MinimumAutomationStrokeNumber)
            {
                if (BorderFloatingBarExitPPTBtn.Visibility == Visibility.Visible) SavePPTScreenshot($"{pptName}/{previousSlideID}_{DateTime.Now:HH-mm-ss}");
                else SaveScreenshot(true);
            }

            if (BorderFloatingBarExitPPTBtn.Visibility != Visibility.Visible)
            {
                if (Settings.Canvas.HideStrokeWhenSelecting)
                {
                    inkCanvas.Visibility = Visibility.Collapsed;
                }
                else
                {
                    inkCanvas.IsHitTestVisible = false;
                    inkCanvas.Visibility = Visibility.Visible;
                }
            }
            else
            {
                if (Settings.PowerPointSettings.IsShowStrokeOnSelectInPowerPoint)
                {
                    inkCanvas.Visibility = Visibility.Visible;
                    inkCanvas.IsHitTestVisible = true;
                }
                else
                {
                    if (Settings.Canvas.HideStrokeWhenSelecting)
                    {
                        inkCanvas.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        inkCanvas.IsHitTestVisible = false;
                        inkCanvas.Visibility = Visibility.Visible;
                    }
                }
            }

            GridTransparencyFakeBackground.Opacity = 0;
            GridTransparencyFakeBackground.Background = Brushes.Transparent;

            GridBackgroundCoverHolder.Visibility = Visibility.Collapsed;
            inkCanvas.Select(new StrokeCollection());
            GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;

            if (currentMode != 0)
            {
                SaveStrokes();
                RestoreStrokes(true);
            }

            CheckEnableTwoFingerGestureBtnVisibility(false);


            StackPanelCanvasControls.Visibility = Visibility.Collapsed;

            if (isFloatingBarFolded) return;
            HideSubPanels("cursor", true);

            // update tool selection
            SelectedMode = ICCToolsEnum.CursorMode;
            ForceUpdateToolSelection(null);

            await Task.Delay(5);

            if (BorderFloatingBarExitPPTBtn.Visibility == Visibility.Visible)
                ViewboxFloatingBarMarginAnimation(60);
            else
                ViewboxFloatingBarMarginAnimation(100, true);

            RectangleSelectionHitTestBorder.Visibility = Visibility.Collapsed;
        }

        private bool ____isHideSubPanel = true;

        private void PenIconFakeClickForToolBarSettings() {
            ____isHideSubPanel = false;
            PenIcon_Click(null, null);
            ____isHideSubPanel = true;
        }

        private void PenIcon_Click(object sender, RoutedEventArgs e) {

            if (lastBorderMouseDownObject is Panel panel)
                panel.Background = CachedTransparentBrush;
            if (sender == Pen_Icon && lastBorderMouseDownObject != Pen_Icon) return;

            if (ShapeDrawingV2Layer.IsInShapeDrawingMode) ShapeDrawingV2Layer.EndShapeDrawing();

            if (Pen_Icon.Background == null || StackPanelCanvasControls.Visibility == Visibility.Collapsed) {

                inkCanvas.EditingMode = InkCanvasEditingMode.Ink;

                GridTransparencyFakeBackground.Opacity = 1;
                GridTransparencyFakeBackground.Background = new SolidColorBrush(StringToColor("#01FFFFFF"));

                inkCanvas.IsHitTestVisible = true;
                inkCanvas.Visibility = Visibility.Visible;

                GridBackgroundCoverHolder.Visibility = Visibility.Visible;
                GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;

                /*if (forceEraser && currentMode == 0)
                    BtnColorRed_Click(sender, null);*/

                StackPanelCanvasControls.Visibility = Visibility.Visible;
                CheckEnableTwoFingerGestureBtnVisibility(true);
                inkCanvas.EditingMode = InkCanvasEditingMode.Ink;

                // 恢复颜色 - 根据当前模式恢复对应的颜色
                // Requirements: 3.1, 3.2, 3.3
                if (currentMode == 0) {
                    inkColor = ValidateAndCorrectColorIndex(lastDesktopInkColor);
                } else {
                    inkColor = ValidateAndCorrectColorIndex(lastBoardInkColor);
                }

                // 恢复笔类型 - Requirements: 2.5
                penType = lastPenType;

                // 恢复笔触粗细 - Requirements: 1.3
                if (penType == 0) {
                    // 签字笔 - 验证并修正笔触粗细
                    double validatedPenWidth = ValidateAndCorrectPenWidth(lastPenWidth, false);
                    drawingAttributes.Width = validatedPenWidth;
                    drawingAttributes.Height = validatedPenWidth;
                    drawingAttributes.StylusTip = StylusTip.Ellipse;
                    drawingAttributes.IsHighlighter = false;
                } else {
                    // 荧光笔 - 验证并修正笔触粗细
                    double validatedHighlighterWidth = ValidateAndCorrectPenWidth(lastHighlighterWidth, true);
                    drawingAttributes.Width = validatedHighlighterWidth / 2;
                    drawingAttributes.Height = validatedHighlighterWidth;
                    drawingAttributes.StylusTip = StylusTip.Rectangle;
                    drawingAttributes.IsHighlighter = true;
                }

                ColorSwitchCheck();
                CheckPenTypeUIState();

                // 同步PenPaletteV2的选中颜色 - Requirements: 3.1, 3.5
                PenPaletteV2.SelectedColor = InkColorToColorPaletteColor(inkColor);

                if (____isHideSubPanel) HideSubPanels("pen", true);

                // update tool selection
                SelectedMode = ICCToolsEnum.PenMode;
                ForceUpdateToolSelection(null);
            }
            else
            {
                if (inkCanvas.EditingMode == InkCanvasEditingMode.Ink)
                {
                    //if (PenPalette.Visibility == Visibility.Visible)
                    //{
                    //    AnimationsHelper.HideWithSlideAndFade(EraserSizePanel);
                    //    AnimationsHelper.HideWithSlideAndFade(BorderTools);
                    //    AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);
                    //    AnimationsHelper.HideWithSlideAndFade(PenPalette);
                    //    AnimationsHelper.HideWithSlideAndFade(BoardPenPalette);
                    //    AnimationsHelper.HideWithSlideAndFade(BorderDrawShape);
                    //    AnimationsHelper.HideWithSlideAndFade(BoardBorderDrawShape);
                    //    AnimationsHelper.HideWithSlideAndFade(BoardEraserSizePanel);
                    //    AnimationsHelper.HideWithSlideAndFade(BorderTools);
                    //    AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);
                    //    AnimationsHelper.HideWithSlideAndFade(TwoFingerGestureBorder);
                    //    AnimationsHelper.HideWithSlideAndFade(BoardTwoFingerGestureBorder);
                    //}
                    //else
                    //{
                    //    AnimationsHelper.HideWithSlideAndFade(EraserSizePanel);
                    //    AnimationsHelper.HideWithSlideAndFade(BorderTools);
                    //    AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);
                    //    AnimationsHelper.HideWithSlideAndFade(BorderDrawShape);
                    //    AnimationsHelper.HideWithSlideAndFade(BoardBorderDrawShape);
                    //    AnimationsHelper.HideWithSlideAndFade(BoardEraserSizePanel);
                    //    AnimationsHelper.HideWithSlideAndFade(BorderTools);
                    //    AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);
                    //    AnimationsHelper.HideWithSlideAndFade(TwoFingerGestureBorder);
                    //    AnimationsHelper.HideWithSlideAndFade(BoardTwoFingerGestureBorder);
                    //    AnimationsHelper.ShowWithSlideFromBottomAndFade(PenPalette);
                    //    AnimationsHelper.ShowWithSlideFromBottomAndFade(BoardPenPalette);
                    //}

                    if (PenPaletteV2Popup.IsOpen == false) {
                        var transform = Pen_Icon.TransformToVisual(Main_Grid);
                        var pt = transform.Transform(new Point(0, 0));
                        PenPaletteV2Popup.VerticalOffset = pt.Y;
                        PenPaletteV2Popup.HorizontalOffset = pt.X - 32;

                        // 同步当前颜色到PenPaletteV2 - Requirements: 3.1, 3.5
                        PenPaletteV2.SelectedColor = InkColorToColorPaletteColor(inkColor);
                    }
                    PenPaletteV2Popup.IsOpen = !PenPaletteV2Popup.IsOpen;
                }
                else
                {
                    inkCanvas.EditingMode = InkCanvasEditingMode.Ink;

                    // 恢复颜色 - 根据当前模式恢复对应的颜色
                    // Requirements: 3.1, 3.2, 3.3
                    if (currentMode == 0) {
                        inkColor = ValidateAndCorrectColorIndex(lastDesktopInkColor);
                    } else {
                        inkColor = ValidateAndCorrectColorIndex(lastBoardInkColor);
                    }

                    // 恢复笔类型 - Requirements: 2.5
                    penType = lastPenType;

                    // 恢复笔触粗细 - Requirements: 1.3
                    if (penType == 0) {
                        // 签字笔 - 验证并修正笔触粗细
                        double validatedPenWidth = ValidateAndCorrectPenWidth(lastPenWidth, false);
                        drawingAttributes.Width = validatedPenWidth;
                        drawingAttributes.Height = validatedPenWidth;
                        drawingAttributes.StylusTip = StylusTip.Ellipse;
                        drawingAttributes.IsHighlighter = false;
                    } else {
                        // 荧光笔 - 验证并修正笔触粗细
                        double validatedHighlighterWidth = ValidateAndCorrectPenWidth(lastHighlighterWidth, true);
                        drawingAttributes.Width = validatedHighlighterWidth / 2;
                        drawingAttributes.Height = validatedHighlighterWidth;
                        drawingAttributes.StylusTip = StylusTip.Rectangle;
                        drawingAttributes.IsHighlighter = true;
                    }

                    ColorSwitchCheck();
                    CheckPenTypeUIState();
                    if (____isHideSubPanel) HideSubPanels("pen", true);

                    // update tool selection
                    SelectedMode = ICCToolsEnum.PenMode;
                    ForceUpdateToolSelection(null);
                }
            }
        }


        /// <summary>
        ///     浮動工具欄的“套索選”按鈕事件，重定向到舊UI的<c>BtnSelect_Click</c>方法
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">MouseButtonEventArgs</param>
        private void SymbolIconSelect_MouseUp(object sender, MouseButtonEventArgs e) {
            if (lastBorderMouseDownObject is Panel panel)
                panel.Background = CachedTransparentBrush;
            if (sender == SymbolIconSelect && lastBorderMouseDownObject != SymbolIconSelect) return;

            if (ShapeDrawingV2Layer.IsInShapeDrawingMode) ShapeDrawingV2Layer.EndShapeDrawing();

            if (SelectedMode == ICCToolsEnum.LassoMode) {
                if (SelectionPopupV2.IsOpen == false) {
                    var transform = SymbolIconSelect.TransformToVisual(Main_Grid);
                    var pt = transform.Transform(new Point(0, 0));
                    SelectionPopupV2.VerticalOffset = pt.Y;
                    SelectionPopupV2.HorizontalOffset = pt.X - 32;
                }
                SelectionPopupV2.IsOpen = !SelectionPopupV2.IsOpen;
            } else HideSubPanels("select");

            forceEraser = true;
            drawingShapeMode = 0;
            inkCanvas.IsManipulationEnabled = false;
            if (inkCanvas.EditingMode != InkCanvasEditingMode.Select) {
                inkCanvas.EditingMode = InkCanvasEditingMode.Select;
            }

            // update tool selection
            SelectedMode = ICCToolsEnum.LassoMode;
            ForceUpdateToolSelection(null);
        }

        private void EraserIcon_Click(object sender, RoutedEventArgs e)
        {

            if (lastBorderMouseDownObject is Panel panel)
                panel.Background = CachedTransparentBrush;
            if (sender == Eraser_Icon && lastBorderMouseDownObject != Eraser_Icon) return;

            if (ShapeDrawingV2Layer.IsInShapeDrawingMode) ShapeDrawingV2Layer.EndShapeDrawing();

            forceEraser = true;
            forcePointEraser = true;

            double width = 24;
            switch (Settings.Canvas.EraserSize)
            {
                case 0:
                    width = 24;
                    break;
                case 1:
                    width = 38;
                    break;
                case 2:
                    width = 46;
                    break;
                case 3:
                    width = 62;
                    break;
                case 4:
                    width = 78;
                    break;
            }

            eraserWidth = width;
            isEraserCircleShape = Settings.Canvas.EraserShapeType == 0;
            isUsingStrokesEraser = false;

            if (inkCanvas.EditingMode == InkCanvasEditingMode.EraseByPoint)
            {
                if (EraserSizePanel.Visibility == Visibility.Collapsed)
                {
                    AnimationsHelper.HideWithSlideAndFade(BorderTools);
                    AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);
                    // AnimationsHelper.HideWithSlideAndFade(PenPalette);
                    // AnimationsHelper.HideWithSlideAndFade(BoardPenPalette);
                    AnimationsHelper.HideWithSlideAndFade(BorderDrawShape);
                    AnimationsHelper.HideWithSlideAndFade(BoardBorderDrawShape);
                    AnimationsHelper.HideWithSlideAndFade(BorderTools);
                    AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);
                    AnimationsHelper.ShowWithSlideFromBottomAndFade(EraserSizePanel);
                    AnimationsHelper.ShowWithSlideFromBottomAndFade(BoardEraserSizePanel);
                }
                else
                {
                    AnimationsHelper.HideWithSlideAndFade(EraserSizePanel);
                    AnimationsHelper.HideWithSlideAndFade(BorderTools);
                    AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);
                    // AnimationsHelper.HideWithSlideAndFade(PenPalette);
                    // AnimationsHelper.HideWithSlideAndFade(BoardPenPalette);
                    AnimationsHelper.HideWithSlideAndFade(BorderDrawShape);
                    AnimationsHelper.HideWithSlideAndFade(BoardBorderDrawShape);
                    AnimationsHelper.HideWithSlideAndFade(BoardEraserSizePanel);
                    AnimationsHelper.HideWithSlideAndFade(BorderTools);
                    AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);
                    AnimationsHelper.HideWithSlideAndFade(TwoFingerGestureBorder);
                    AnimationsHelper.HideWithSlideAndFade(BoardTwoFingerGestureBorder);
                }
            }
            else
            {
                HideSubPanels("eraser");

                // update tool selection
                SelectedMode = ICCToolsEnum.EraseByGeometryMode;
                ForceUpdateToolSelection(null);
            }

            inkCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint;
            drawingShapeMode = 0;

            inkCanvas_EditingModeChanged(inkCanvas, null);
            CancelSingleFingerDragMode();
        }

        private void EraserIconByStrokes_Click(object sender, RoutedEventArgs e)
        {

            if (lastBorderMouseDownObject is Panel panel)
                panel.Background = CachedTransparentBrush;
            if (sender == EraserByStrokes_Icon && lastBorderMouseDownObject != EraserByStrokes_Icon) return;

            if (ShapeDrawingV2Layer.IsInShapeDrawingMode) ShapeDrawingV2Layer.EndShapeDrawing();

            forceEraser = true;
            forcePointEraser = false;

            isUsingStrokesEraser = true;

            // update tool selection
            SelectedMode = ICCToolsEnum.EraseByStrokeMode;
            ForceUpdateToolSelection(null);

            inkCanvas.EraserShape = new EllipseStylusShape(5, 5);
            inkCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
            drawingShapeMode = 0;

            inkCanvas_EditingModeChanged(inkCanvas, null);
            CancelSingleFingerDragMode();

            HideSubPanels("eraserByStrokes");
        }

        #endregion

        #region 按钮布局更新

        public void UpdateFloatingBarIconsVisibility() {

            var items = new FrameworkElement[] {
                ShapeDrawFloatingBarBtn,
                FreezeFloatingBarBtn,
                HandFloatingBarBtn,
                SymbolIconUndo,
                SymbolIconRedo,
                CursorWithDelFloatingBarBtn,
                SymbolIconSelect,
                WhiteboardFloatingBarBtn,
                Fold_Icon,
                EnableTwoFingerGestureBorder
            };

            var floatingBarIconsVisibilityValue = Settings.Appearance.FloatingBarIconsVisibility;
            var fbivca = floatingBarIconsVisibilityValue.ToCharArray();
            for (var i = 0; i < fbivca.Length; i++) {
                if (items[i] == EnableTwoFingerGestureBorder) continue;
                items[i].Visibility = fbivca[i] == '1' ? Visibility.Visible : Visibility.Collapsed;
                if (!isLoaded) continue;
                if (items[i] == FreezeFloatingBarBtn && IsAnnotationFreezeOn && fbivca[i] == '0') IsAnnotationFreezeOn = false;
                if (items[i] == HandFloatingBarBtn && isSingleFingerDragMode && fbivca[i] == '0') {
                    isSingleFingerDragMode = false;
                    UpdateFloatingBarHandIconCheckedStatus();
                }
                if ((items[i] == HandFloatingBarBtn || items[i] == SymbolIconSelect) && fbivca[i] == '0' &&
                    SelectedMode != ICCToolsEnum.PenMode &&
                    SelectedMode != ICCToolsEnum.CursorMode) PenIconFakeClickForToolBarSettings();
            }

            if (StackPanelCanvasControls.Visibility == Visibility.Visible) {
                EnableTwoFingerGestureBorder.Visibility = fbivca[9] == '1' ? Visibility.Visible : Visibility.Collapsed;
            }

            Eraser_Icon.Visibility = Visibility.Visible;
            EraserByStrokes_Icon.Visibility = Visibility.Visible;

            if (Settings.Appearance.EraserButtonsVisibility == 1) EraserByStrokes_Icon.Visibility = Visibility.Collapsed;
                else if (Settings.Appearance.EraserButtonsVisibility == 2) Eraser_Icon.Visibility = Visibility.Collapsed;

            if (((SelectedMode == ICCToolsEnum.EraseByStrokeMode && Settings.Appearance.EraserButtonsVisibility == 1)
                 || (SelectedMode == ICCToolsEnum.EraseByGeometryMode &&
                     Settings.Appearance.EraserButtonsVisibility == 2)) && isLoaded) PenIconFakeClickForToolBarSettings();



            if (Settings.Appearance.OnlyDisplayEraserBtn && Settings.Appearance.EraserButtonsVisibility != 0) {
                InkEraserToolbarTextBlock.Text = "橡皮";
                CircleEraserToolbarTextBlock.Text = "橡皮";
            } else {
                InkEraserToolbarTextBlock.Text = "线擦";
                CircleEraserToolbarTextBlock.Text = "板擦";
            }
        }

        public void UpdateFloatingBarIconsLayout() {
            FrameworkElement[] IconsArray = new FrameworkElement[13] {
                Cursor_Icon,
                Pen_Icon,
                SymbolIconDelete,
                Eraser_Icon,
                EraserByStrokes_Icon,
                SymbolIconSelect,
                ShapeDrawFloatingBarBtn,
                SymbolIconUndo,
                SymbolIconRedo,
                CursorWithDelFloatingBarBtn,
                WhiteboardFloatingBarBtn,
                ToolsFloatingBarBtn,
                Fold_Icon
            };
            var barHeight = Settings.Appearance.FloatingBarButtonLabelVisibility ? 36 : 28;
            foreach (StackPanel elem in IconsArray.Cast<StackPanel>()) {
                if (elem.Children[0] is Image) {
                    ((Image)elem.Children[0]).Margin = new Thickness(0, Settings.Appearance.FloatingBarButtonLabelVisibility ? 3 : 5, 0, 0);
                    ((Image)elem.Children[0]).Height = Settings.Appearance.FloatingBarButtonLabelVisibility ? 17 : 15;
                }
                elem.Width = Settings.Appearance.FloatingBarButtonLabelVisibility ? 28 : 21;
                elem.Height = Settings.Appearance.FloatingBarButtonLabelVisibility ? 34 : 28;
                FloatingbarSelectionBG.Height = Settings.Appearance.FloatingBarButtonLabelVisibility ? 34 : 28;
            }
            BorderFloatingBarMoveControls.Width = barHeight;
            BorderFloatingBarMoveControls.Height = barHeight;
            BorderFloatingBarMainControls.Height = barHeight;
            EnableTwoFingerGestureBorder.Height = barHeight;
            EnableTwoFingerGestureBorder.Width = barHeight;
            ViewboxFloatingBar.Height = Settings.Appearance.FloatingBarButtonLabelVisibility ? 58 : 46;
        }

        #endregion

        private void FloatingBarToolBtnMouseDownFeedback_Panel(object sender, MouseButtonEventArgs e) {
            var s = (Panel)sender;
            lastBorderMouseDownObject = sender;
            if (s == SymbolIconDelete) s.Background = UIStyleHelper.DeleteButtonPressedBrush;
            else s.Background = UIStyleHelper.ButtonPressedBrush;
        }

        private void FloatingBarToolBtnMouseLeaveFeedback_Panel(object sender, MouseEventArgs e) {
            var s = (Panel)sender;
            lastBorderMouseDownObject = null;
            s.Background = CachedTransparentBrush;
        }

        private void SymbolIconSettings_Click(object sender, RoutedEventArgs e) {
            HideSubPanels();

            // 检查是否已经打开了设置窗口
            foreach (Window window in Application.Current.Windows)
            {
                if (window is Views.Settings.SettingsWindow)
                {
                    window.Activate();
                    if (window.WindowState == WindowState.Minimized)
                    {
                        window.WindowState = WindowState.Normal;
                    }
                    return;
                }
            }

            // 打开新的设置窗口
            var settingsWindow = new Views.Settings.SettingsWindow();
            settingsWindow.Show();
        }

        private async void SymbolIconScreenshot_MouseUp(object sender, MouseButtonEventArgs e) {
            HideSubPanelsImmediately();
            await Task.Delay(50);
            //SaveScreenShotToDesktop();
            var scrwin = new ScreenshotWindow(this,Settings);
            scrwin.Show();
        }

        private void ImageCountdownTimer_MouseUp(object sender, MouseButtonEventArgs e) {
            LeftUnFoldButtonQuickPanel.Visibility = Visibility.Collapsed;
            RightUnFoldButtonQuickPanel.Visibility = Visibility.Collapsed;
            AnimationsHelper.HideWithSlideAndFade(BorderTools);
            AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);

            new CountdownTimerWindow().Show();
        }

        private void OperatingGuideWindowIcon_MouseUp(object sender, MouseButtonEventArgs e) {
            AnimationsHelper.HideWithSlideAndFade(BorderTools);
            AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);

            new OperatingGuideWindow().Show();
        }

        private void SymbolIconRand_MouseUp(object sender, MouseButtonEventArgs e) {
            LeftUnFoldButtonQuickPanel.Visibility = Visibility.Collapsed;
            RightUnFoldButtonQuickPanel.Visibility = Visibility.Collapsed;
            if (lastBorderMouseDownObject != sender) return;

            AnimationsHelper.HideWithSlideAndFade(BorderTools);
            AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);

            new RandWindow(Settings).Show();
        }

        public void CheckEraserTypeTab() {
            // Eraser tabs have been removed from legacy UI
        }

        private void SymbolIconRandOne_MouseUp(object sender, MouseButtonEventArgs e) {
            LeftUnFoldButtonQuickPanel.Visibility = Visibility.Collapsed;
            RightUnFoldButtonQuickPanel.Visibility = Visibility.Collapsed;
            if (lastBorderMouseDownObject != sender) return;

            AnimationsHelper.HideWithSlideAndFade(BorderTools);
            AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);

            new RandWindow(Settings,true).ShowDialog();
        }

        private void GridInkReplayButton_MouseUp(object sender, MouseButtonEventArgs e) {
            if (lastBorderMouseDownObject != sender) return;
            if (inkCanvas.Strokes.Count == 0) {
                HideSubPanels();
                return;
            };

            AnimationsHelper.HideWithSlideAndFade(BorderTools);
            AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);

            CollapseBorderDrawShape();

            InkCanvasForInkReplay.Visibility = Visibility.Visible;
            InkCanvasGridForInkReplay.Visibility = Visibility.Hidden;
            InkCanvasGridForInkReplay.IsHitTestVisible = false;
            FloatingbarUIForInkReplay.Visibility = Visibility.Hidden;
            FloatingbarUIForInkReplay.IsHitTestVisible = false;
            BlackboardUIGridForInkReplay.Visibility = Visibility.Hidden;
            BlackboardUIGridForInkReplay.IsHitTestVisible = false;

            AnimationsHelper.ShowWithFadeIn(BorderInkReplayToolBox);
            InkReplayPanelStatusText.Text = "正在重播墨迹...";
            InkReplayPlayPauseBorder.Background = CachedTransparentBrush;
            InkReplayPlayButtonImage.Visibility = Visibility.Collapsed;
            InkReplayPauseButtonImage.Visibility = Visibility.Visible;

            isStopInkReplay = false;
            isPauseInkReplay = false;
            isRestartInkReplay = false;
            inkReplaySpeed = 1;
            InkCanvasForInkReplay.Strokes.Clear();
            var strokes = inkCanvas.Strokes.Clone();
            if (inkCanvas.GetSelectedStrokes().Count != 0) strokes = inkCanvas.GetSelectedStrokes().Clone();
            int k = 1, i = 0;
            new Thread(() => {
                isRestartInkReplay = true;
                while (isRestartInkReplay) {
                    isRestartInkReplay = false;
                    Application.Current.Dispatcher.Invoke(() => {
                        InkCanvasForInkReplay.Strokes.Clear();
                    });
                    foreach (var stroke in strokes) {

                        if (isRestartInkReplay) break;

                        var stylusPoints = new StylusPointCollection();
                        if (stroke.StylusPoints.Count == 629) //圆或椭圆
                        {
                            Stroke s = null;
                            foreach (var stylusPoint in stroke.StylusPoints) {

                                var timeoutCount = 0;
                                while (isPauseInkReplay && timeoutCount < 1000) {
                                    Thread.Sleep(10);
                                    timeoutCount++;
                                }
                                if (timeoutCount >= 1000) break;

                                if (i++ >= 50) {
                                    i = 0;
                                    Thread.Sleep((int)(10 / inkReplaySpeed));
                                    if (isStopInkReplay) return;
                                }

                                Application.Current.Dispatcher.Invoke(() => {
                                    try {
                                        InkCanvasForInkReplay.Strokes.Remove(s);
                                    }
                                    catch (Exception ex) {
                                        LogHelper.WriteLogToFile("浮动工具栏回放异常：" + ex.Message, LogHelper.LogType.Error);
                                    }

                                    stylusPoints.Add(stylusPoint);
                                    s = new Stroke(stylusPoints.Clone());
                                    s.DrawingAttributes = stroke.DrawingAttributes;
                                    InkCanvasForInkReplay.Strokes.Add(s);
                                });
                            }
                        } else {
                            Stroke s = null;
                            foreach (var stylusPoint in stroke.StylusPoints) {

                                var timeoutCount = 0;
                                while (isPauseInkReplay && timeoutCount < 1000) {
                                    Thread.Sleep(10);
                                    timeoutCount++;
                                }
                                if (timeoutCount >= 1000) break;

                                if (i++ >= k) {
                                    i = 0;
                                    Thread.Sleep((int)(10 / inkReplaySpeed));
                                    if (isStopInkReplay) return;
                                }

                                Application.Current.Dispatcher.Invoke(() => {
                                    try {
                                        InkCanvasForInkReplay.Strokes.Remove(s);
                                    }
                                    catch (Exception ex) {
                                        LogHelper.WriteLogToFile("浮动工具栏回放异常：" + ex.Message, LogHelper.LogType.Error);
                                    }

                                    stylusPoints.Add(stylusPoint);
                                    s = new Stroke(stylusPoints.Clone());
                                    s.DrawingAttributes = stroke.DrawingAttributes;
                                    InkCanvasForInkReplay.Strokes.Add(s);
                                });
                            }
                        }
                    }
                }

                Thread.Sleep(100);
                Application.Current.Dispatcher.Invoke(() => {
                    InkCanvasForInkReplay.Visibility = Visibility.Collapsed;
                    InkCanvasGridForInkReplay.Visibility = Visibility.Visible;
                    InkCanvasGridForInkReplay.IsHitTestVisible = true;
                    AnimationsHelper.HideWithFadeOut(BorderInkReplayToolBox);
                    FloatingbarUIForInkReplay.Visibility = Visibility.Visible;
                    FloatingbarUIForInkReplay.IsHitTestVisible = true;
                    BlackboardUIGridForInkReplay.Visibility = Visibility.Visible;
                    BlackboardUIGridForInkReplay.IsHitTestVisible = true;
                });
            }).Start();
        }

        private bool isStopInkReplay = false;
        private bool isPauseInkReplay = false;
        private bool isRestartInkReplay = false;
        private double inkReplaySpeed = 1;

        private void InkCanvasForInkReplay_MouseDown(object sender, MouseButtonEventArgs e) {
            if (e.ClickCount == 2) {
                InkCanvasForInkReplay.Visibility = Visibility.Collapsed;
                InkCanvasGridForInkReplay.Visibility = Visibility.Visible;
                InkCanvasGridForInkReplay.IsHitTestVisible = true;
                FloatingbarUIForInkReplay.Visibility = Visibility.Visible;
                FloatingbarUIForInkReplay.IsHitTestVisible = true;
                BlackboardUIGridForInkReplay.Visibility = Visibility.Visible;
                BlackboardUIGridForInkReplay.IsHitTestVisible = true;
                AnimationsHelper.HideWithFadeOut(BorderInkReplayToolBox);
                isStopInkReplay = true;
            }
        }

        private void InkReplayPlayPauseBorder_OnMouseDown(object sender, MouseButtonEventArgs e) {
            InkReplayPlayPauseBorder.Background = CachedInkReplayButtonPressedBrush;
        }

        private void InkReplayPlayPauseBorder_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            InkReplayPlayPauseBorder.Background = CachedTransparentBrush;
            isPauseInkReplay = !isPauseInkReplay;
            InkReplayPanelStatusText.Text = isPauseInkReplay?"已暂停！":"正在重播墨迹...";
            InkReplayPlayButtonImage.Visibility = isPauseInkReplay ? Visibility.Visible: Visibility.Collapsed;
            InkReplayPauseButtonImage.Visibility = !isPauseInkReplay ? Visibility.Visible : Visibility.Collapsed;
        }

        private void InkReplayStopButtonBorder_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            InkReplayStopButtonBorder.Background = CachedInkReplayButtonPressedBrush;
        }

        private void InkReplayStopButtonBorder_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            InkReplayStopButtonBorder.Background = CachedTransparentBrush;
            InkCanvasForInkReplay.Visibility = Visibility.Collapsed;
            InkCanvasGridForInkReplay.Visibility = Visibility.Visible;
            InkCanvasGridForInkReplay.IsHitTestVisible = true;
            FloatingbarUIForInkReplay.Visibility = Visibility.Visible;
            FloatingbarUIForInkReplay.IsHitTestVisible = true;
            BlackboardUIGridForInkReplay.Visibility = Visibility.Visible;
            BlackboardUIGridForInkReplay.IsHitTestVisible = true;
            AnimationsHelper.HideWithFadeOut(BorderInkReplayToolBox);
            isStopInkReplay = true;
        }

        private void InkReplayReplayButtonBorder_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            InkReplayReplayButtonBorder.Background = CachedInkReplayButtonPressedBrush;
        }

        private void InkReplayReplayButtonBorder_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            InkReplayReplayButtonBorder.Background = CachedTransparentBrush;
            isRestartInkReplay = true;
            isPauseInkReplay = false;
            InkReplayPanelStatusText.Text = "正在重播墨迹...";
            InkReplayPlayButtonImage.Visibility = Visibility.Collapsed;
            InkReplayPauseButtonImage.Visibility = Visibility.Visible;
        }

        private void InkReplaySpeedButtonBorder_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            InkReplaySpeedButtonBorder.Background = CachedInkReplayButtonPressedBrush;
        }

        private void InkReplaySpeedButtonBorder_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            InkReplaySpeedButtonBorder.Background = CachedTransparentBrush;
            inkReplaySpeed = inkReplaySpeed == 0.5 ? 1 :
                inkReplaySpeed == 1 ? 2 :
                inkReplaySpeed == 2 ? 4 :
                inkReplaySpeed == 4 ? 8 : 0.5;
            InkReplaySpeedTextBlock.Text = inkReplaySpeed + "x";
        }

        private void SymbolIconTools_MouseUp(object sender, MouseButtonEventArgs e) {

            if (lastBorderMouseDownObject is Panel panel)
                panel.Background = CachedTransparentBrush;
            if (sender == ToolsFloatingBarBtn && lastBorderMouseDownObject != ToolsFloatingBarBtn) return;

            if (BorderTools.Visibility == Visibility.Visible) {
                AnimationsHelper.HideWithSlideAndFade(EraserSizePanel);
                AnimationsHelper.HideWithSlideAndFade(BorderTools);
                AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);
                // AnimationsHelper.HideWithSlideAndFade(PenPalette);
                // AnimationsHelper.HideWithSlideAndFade(BoardPenPalette);
                AnimationsHelper.HideWithSlideAndFade(BorderDrawShape);
                AnimationsHelper.HideWithSlideAndFade(BoardBorderDrawShape);
                AnimationsHelper.HideWithSlideAndFade(BoardEraserSizePanel);
                AnimationsHelper.HideWithSlideAndFade(BorderTools);
                AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);
                AnimationsHelper.HideWithSlideAndFade(TwoFingerGestureBorder);
                AnimationsHelper.HideWithSlideAndFade(BoardTwoFingerGestureBorder);
            }
            else {
                AnimationsHelper.HideWithSlideAndFade(EraserSizePanel);
                AnimationsHelper.HideWithSlideAndFade(BorderTools);
                AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);
                // AnimationsHelper.HideWithSlideAndFade(PenPalette);
                // AnimationsHelper.HideWithSlideAndFade(BoardPenPalette);
                AnimationsHelper.HideWithSlideAndFade(BorderDrawShape);
                AnimationsHelper.HideWithSlideAndFade(BoardBorderDrawShape);
                AnimationsHelper.HideWithSlideAndFade(BoardEraserSizePanel);
                AnimationsHelper.HideWithSlideAndFade(TwoFingerGestureBorder);
                AnimationsHelper.HideWithSlideAndFade(BoardTwoFingerGestureBorder);
                AnimationsHelper.ShowWithSlideFromBottomAndFade(BorderTools);
                AnimationsHelper.ShowWithSlideFromBottomAndFade(BoardBorderTools);
            }
        }

        private bool isViewboxFloatingBarMarginAnimationRunning = false;

        public void ViewboxFloatingBarMarginAnimation(int MarginFromEdge,
            bool PosXCaculatedWithTaskbarHeight = false) {
            if (MarginFromEdge == 60) MarginFromEdge = 55;
            Dispatcher.InvokeAsync(() => {
                if (Topmost == false)
                    MarginFromEdge = -60;
                else
                    ViewboxFloatingBar.Visibility = Visibility.Visible;
                isViewboxFloatingBarMarginAnimationRunning = true;

                double dpiScaleX = 1, dpiScaleY = 1;
                var source = PresentationSource.FromVisual(this);
                if (source != null) {
                    dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
                    dpiScaleY = source.CompositionTarget.TransformToDevice.M22;
                }

                var screen = System.Windows.Forms.Screen.FromHandle(new WindowInteropHelper(this).Handle);
                double screenWidth = screen.Bounds.Width / dpiScaleX, screenHeight = screen.Bounds.Height / dpiScaleY;
                var toolbarHeight = SystemParameters.PrimaryScreenHeight - SystemParameters.FullPrimaryScreenHeight -
                                    SystemParameters.WindowCaptionHeight;
                var scale = _floatingBarViewModel?.Scale ?? 1.0;
                pos.X = (screenWidth - ViewboxFloatingBar.ActualWidth * scale) / 2;

                if (PosXCaculatedWithTaskbarHeight == false)
                    pos.Y = screenHeight - MarginFromEdge * scale;
                else
                    pos.Y = screenHeight - ViewboxFloatingBar.ActualHeight * scale -
                            toolbarHeight - scale * 3;

                if (MarginFromEdge != -60) {
                    if (BorderFloatingBarExitPPTBtn.Visibility == Visibility.Visible) {
                        if (pointPPT.X != -1 || pointPPT.Y != -1) {
                            if (Math.Abs(pointPPT.Y - pos.Y) > 50)
                                pos = pointPPT;
                            else
                                pointPPT = pos;
                        }
                    }
                    else {
                        if (pointDesktop.X != -1 || pointDesktop.Y != -1) {
                            if (Math.Abs(pointDesktop.Y - pos.Y) > 50)
                                pos = pointDesktop;
                            else
                                pointDesktop = pos;
                        }
                    }
                }

                var sb = new Storyboard();
                var marginAnimation = new ThicknessAnimation {
                    Duration = TimeSpan.FromSeconds(0.20),
                    From = ViewboxFloatingBar.Margin,
                    To = new Thickness(pos.X, pos.Y, 0, -20)
                };
                marginAnimation.EasingFunction = new CircleEase();
                sb.Children.Add(marginAnimation);
                Storyboard.SetTargetProperty(sb, new PropertyPath(FrameworkElement.MarginProperty));
                sb.Completed += (sender, args) => {
                    ViewboxFloatingBar.Margin = new Thickness(pos.X, pos.Y, 0, -20);
                    if (Topmost == false) ViewboxFloatingBar.Visibility = Visibility.Hidden;
                };
                sb.Begin(ViewboxFloatingBar);
            });
        }

        public async void PureViewboxFloatingBarMarginAnimationInDesktopMode()
        {
            await Dispatcher.InvokeAsync(() => {
                ViewboxFloatingBar.Visibility = Visibility.Visible;
                isViewboxFloatingBarMarginAnimationRunning = true;

                double dpiScaleX = 1, dpiScaleY = 1;
                var source = PresentationSource.FromVisual(this);
                if (source != null)
                {
                    dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
                    dpiScaleY = source.CompositionTarget.TransformToDevice.M22;
                }

                var screen = System.Windows.Forms.Screen.FromHandle(new WindowInteropHelper(this).Handle);
                double screenWidth = screen.Bounds.Width / dpiScaleX, screenHeight = screen.Bounds.Height / dpiScaleY;
                var toolbarHeight = SystemParameters.PrimaryScreenHeight - SystemParameters.FullPrimaryScreenHeight -
                                    SystemParameters.WindowCaptionHeight;
                var scale = _floatingBarViewModel?.Scale ?? 1.0;
                pos.X = (screenWidth - ViewboxFloatingBar.ActualWidth * scale) / 2;

                pos.Y = screenHeight - ViewboxFloatingBar.ActualHeight * scale -
                        toolbarHeight - scale * 3;

                if (pointDesktop.X != -1 || pointDesktop.Y != -1) pointDesktop = pos;

                var marginAnimation = new ThicknessAnimation
                {
                    Duration = TimeSpan.FromSeconds(0.35),
                    From = ViewboxFloatingBar.Margin,
                    To = new Thickness(pos.X, pos.Y, 0, -20)
                };
                marginAnimation.EasingFunction = new CircleEase();
                ViewboxFloatingBar.BeginAnimation(MarginProperty, marginAnimation);
            });

            await Task.Delay(349);

            await Dispatcher.InvokeAsync(() => {
                ViewboxFloatingBar.Margin = new Thickness(pos.X, pos.Y, -2000, -200);
            });
        }

        public async void PureViewboxFloatingBarMarginAnimationInPPTMode()
        {
            await Application.Current.Dispatcher.InvokeAsync(() => {
                ViewboxFloatingBar.Visibility = Visibility.Visible;
                isViewboxFloatingBarMarginAnimationRunning = true;

                double dpiScaleX = 1, dpiScaleY = 1;
                var source = PresentationSource.FromVisual(this);
                if (source != null)
                {
                    dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
                    dpiScaleY = source.CompositionTarget.TransformToDevice.M22;
                }

                var screen = System.Windows.Forms.Screen.FromHandle(new WindowInteropHelper((System.Windows.Window)this).Handle);
                double screenWidth = screen.Bounds.Width / dpiScaleX, screenHeight = screen.Bounds.Height / dpiScaleY;
                var scale = _floatingBarViewModel?.Scale ?? 1.0;
                pos.X = (screenWidth - ViewboxFloatingBar.ActualWidth * scale) / 2;

                pos.Y = screenHeight - 55 * scale;

                if (pointPPT.X != -1 || pointPPT.Y != -1)
                {
                    pointPPT = pos;
                }

                var marginAnimation = new ThicknessAnimation
                {
                    Duration = TimeSpan.FromSeconds(0.35),
                    From = ViewboxFloatingBar.Margin,
                    To = new Thickness(pos.X, pos.Y, 0, -20)
                };
                marginAnimation.EasingFunction = new CircleEase();
                ViewboxFloatingBar.BeginAnimation(FrameworkElement.MarginProperty, marginAnimation);
            });

            await Task.Delay(349);

            await Application.Current.Dispatcher.InvokeAsync(() => {
                ViewboxFloatingBar.Margin = new Thickness(pos.X, pos.Y, -2000, -200);
            });
        }

        private void ColorThemeSwitch_MouseUp(object sender, RoutedEventArgs e) {
            isUselightThemeColor = !isUselightThemeColor;
            if (currentMode == 0) isDesktopUselightThemeColor = isUselightThemeColor;
            CheckColorTheme();
        }

        private void CursorWithDelIcon_Click(object sender, RoutedEventArgs e) {

            if (lastBorderMouseDownObject is Panel panel)
                panel.Background = CachedTransparentBrush;
            if (sender == CursorWithDelFloatingBarBtn && lastBorderMouseDownObject != CursorWithDelFloatingBarBtn) return;

            SymbolIconDelete_MouseUp(null, null);
            CursorIcon_Click(null, null);
        }

        private void SelectIcon_MouseUp(object sender, RoutedEvent e) {
            forceEraser = true;
            drawingShapeMode = 0;
            inkCanvas.IsManipulationEnabled = false;
            if (inkCanvas.EditingMode == InkCanvasEditingMode.Select) {
                //var selectedStrokes = new StrokeCollection();
                //foreach (var stroke in inkCanvas.Strokes)
                //    if (stroke.GetBounds().Width > 0 && stroke.GetBounds().Height > 0)
                //        selectedStrokes.Add(stroke);
                //inkCanvas.Select(selectedStrokes);
                inkCanvas.Select(inkCanvas.Strokes);
            }
            else {
                inkCanvas.EditingMode = InkCanvasEditingMode.Select;
            }
        }

        private void DrawShapePromptToPen() {
            if (isLongPressSelected == true) {
                HideSubPanels("pen");
                // update tool selection
                SelectedMode = ICCToolsEnum.PenMode;
                ForceUpdateToolSelection(null);
            }
            else {
                if (StackPanelCanvasControls.Visibility == Visibility.Visible) {
                    HideSubPanels("pen");
                    // update tool selection
                    SelectedMode = ICCToolsEnum.PenMode;
                    ForceUpdateToolSelection(null);
                } else {
                    HideSubPanels("cursor");
                    // update tool selection
                    SelectedMode = ICCToolsEnum.CursorMode;
                    ForceUpdateToolSelection(null);
                }

            }
        }

        private void CloseBordertools_MouseUp(object sender, MouseButtonEventArgs e) {
            HideSubPanels();
        }

        #region Left Side Panel

        private void BtnFingerDragMode_Click(object sender, RoutedEventArgs e) {
            if (lastBorderMouseDownObject is Panel panel)
                panel.Background = CachedTransparentBrush;
            if (sender == HandFloatingBarBtn && lastBorderMouseDownObject != HandFloatingBarBtn) return;

            isSingleFingerDragMode = !isSingleFingerDragMode;
            UpdateFloatingBarHandIconCheckedStatus();
        }

        public void BtnUndo_Click(object sender, RoutedEventArgs e) {
            var tick = Environment.TickCount;
            if (tick - _lastUndoTick < UndoRedoMinIntervalMs) return;
            _lastUndoTick = tick;

            if (inkCanvas.GetSelectedStrokes().Count != 0) {
                GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
                inkCanvas.Select(new StrokeCollection());
                UpdateStrokeSelectionBorder(false, null);
                RectangleSelectionHitTestBorder.Visibility = Visibility.Visible;
            }

            var item = timeMachine.Undo();
            ApplyHistoryToCanvas(item);
        }

        public void BtnRedo_Click(object sender, RoutedEventArgs e) {
            var tick = Environment.TickCount;
            if (tick - _lastRedoTick < UndoRedoMinIntervalMs) return;
            _lastRedoTick = tick;

            if (inkCanvas.GetSelectedStrokes().Count != 0) {
                GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
                inkCanvas.Select(new StrokeCollection());
                UpdateStrokeSelectionBorder(false, null);
                RectangleSelectionHitTestBorder.Visibility = Visibility.Visible;
            }

            var item = timeMachine.Redo();
            ApplyHistoryToCanvas(item);
        }

        private void Btn_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e) {
            if (!isLoaded) return;
            try {
                if (((Button)sender).IsEnabled)
                    ((UIElement)((Button)sender).Content).Opacity = 1;
                else
                    ((UIElement)((Button)sender).Content).Opacity = 0.25;
            }
            catch (Exception ex) {
                LogHelper.WriteLogToFile("浮动工具栏按钮状态更新异常：" + ex.Message, LogHelper.LogType.Error);
            }
        }

        #endregion Left Side Panel

        #region Right Side Panel

        public static bool CloseIsFromButton = false;

        public void BtnExit_Click(object sender, RoutedEventArgs e) {
            CloseIsFromButton = true;
            Close();
        }

        public void BtnRestart_Click(object sender, RoutedEventArgs e) {
            Process.Start(System.Windows.Forms.Application.ExecutablePath, "-m");

            CloseIsFromButton = true;
            Application.Current.Shutdown();
        }

        private void SettingsOverlayClick(object sender, MouseButtonEventArgs e) {
            if (isOpeningOrHidingSettingsPane == true) return;
            BtnSettings_Click(null, null);
        }

        private bool isOpeningOrHidingSettingsPane = false;

        private void BtnSettings_Click(object sender, RoutedEventArgs e) {
            //new Views.Settings.SettingsWindow().Show();
            SymbolIconSettings_Click(null, null);
        }

        private void BtnThickness_Click(object sender, RoutedEventArgs e) { }

        private bool forceEraser = false;


        public void BtnClear_Click(object sender, RoutedEventArgs e) {
            forceEraser = false;
            //BorderClearInDelete.Visibility = Visibility.Collapsed;

            if (currentMode == 0) {
                // 先回到画笔再清屏，避免 TimeMachine 的相关 bug 影响
                if (Pen_Icon.Background == null && StackPanelCanvasControls.Visibility == Visibility.Visible)
                    PenIcon_Click(null, null);
            }
            else {
                if (Pen_Icon.Background == null) PenIcon_Click(null, null);
            }

            if (inkCanvas.Strokes.Count != 0) {
                var whiteboardIndex = CurrentWhiteboardIndex;
                if (currentMode == 0) whiteboardIndex = 0;
                strokeCollections[whiteboardIndex] = inkCanvas.Strokes.Clone();
            }

            ClearStrokes(false);
            inkCanvas.Children.Clear();

            CancelSingleFingerDragMode();

            if (Settings.Canvas.ClearCanvasAndClearTimeMachine) timeMachine.ClearStrokeHistory();
        }

        private bool lastIsInMultiTouchMode = false;

        private void CancelSingleFingerDragMode() {
            if (IsDrawShapeBorderAutoHide) CollapseBorderDrawShape();

            GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;

            if (isSingleFingerDragMode) BtnFingerDragMode_Click(null, null);
            isLongPressSelected = false;
        }

        private int currentMode = 0;

        private void BtnSwitch_Click(object sender, RoutedEventArgs e) {
            if (GridTransparencyFakeBackground.Background == Brushes.Transparent) {
                if (currentMode == 0) {
                    currentMode++;
                    GridBackgroundCover.Visibility = Visibility.Collapsed;
                    AnimationsHelper.HideWithSlideAndFade(BlackboardLeftSide);
                    AnimationsHelper.HideWithSlideAndFade(BlackboardCenterSide);
                    AnimationsHelper.HideWithSlideAndFade(BlackboardRightSide);

                    SaveStrokes(true);
                    ClearStrokes(true);
                    RestoreStrokes();
                }
                else
                {
                    // 当前处于白板模式但笔层已隐藏时，仍需先正确切回桌面模式。
                    currentMode = 0;
                    GridBackgroundCover.Visibility = Visibility.Collapsed;
                    AnimationsHelper.HideWithSlideAndFade(BlackboardLeftSide);
                    AnimationsHelper.HideWithSlideAndFade(BlackboardCenterSide);
                    AnimationsHelper.HideWithSlideAndFade(BlackboardRightSide);

                    SaveStrokes();
                    ClearStrokes(true);
                    RestoreStrokes(true);
                }

                Topmost = true;
                BtnHideInkCanvas_Click(null, e);
            }
            else {
                switch (++currentMode % 2) {
                    case 0: //屏幕模式
                        currentMode = 0;
                        GridBackgroundCover.Visibility = Visibility.Collapsed;
                        AnimationsHelper.HideWithSlideAndFade(BlackboardLeftSide);
                        AnimationsHelper.HideWithSlideAndFade(BlackboardCenterSide);
                        AnimationsHelper.HideWithSlideAndFade(BlackboardRightSide);

                        SaveStrokes();
                        ClearStrokes(true);
                        RestoreStrokes(true);

                        Topmost = true;
                        break;
                    case 1: //黑板或白板模式
                        currentMode = 1;
                        GridBackgroundCover.Visibility = Visibility.Visible;
                        AnimationsHelper.ShowWithSlideFromBottomAndFade(BlackboardLeftSide);
                        AnimationsHelper.ShowWithSlideFromBottomAndFade(BlackboardCenterSide);
                        AnimationsHelper.ShowWithSlideFromBottomAndFade(BlackboardRightSide);

                        SaveStrokes(true);
                        ClearStrokes(true);
                        RestoreStrokes();


                        var bgC = BoardPagesSettingsList[CurrentWhiteboardIndex - 1].BackgroundColor;
                        if (bgC == BlackboardBackgroundColorEnum.BlackBoardGreen
                            || bgC == BlackboardBackgroundColorEnum.BlueBlack
                            || bgC == BlackboardBackgroundColorEnum.GrayBlack
                            || bgC == BlackboardBackgroundColorEnum.RealBlack)
                            BtnColorWhite_Click(null, null);
                        else BtnColorBlack_Click(null, null);

                        Topmost = false;
                        break;
                }
            }
        }

        private int BoundsWidth = 5;

        private void BtnHideInkCanvas_Click(object sender, RoutedEventArgs e) {
            if (GridTransparencyFakeBackground.Background == Brushes.Transparent) {
                GridTransparencyFakeBackground.Opacity = 1;
                GridTransparencyFakeBackground.Background = new SolidColorBrush(StringToColor("#01FFFFFF"));
                inkCanvas.IsHitTestVisible = true;
                inkCanvas.Visibility = Visibility.Visible;

                GridBackgroundCoverHolder.Visibility = Visibility.Visible;

                GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
            }
            else {
                // Auto-clear Strokes 要等待截图完成再清理笔记
                if (BorderFloatingBarExitPPTBtn.Visibility != Visibility.Visible) {
                    if (isLoaded && Settings.Automation.IsAutoClearWhenExitingWritingMode)
                        if (inkCanvas.Strokes.Count > 0) {
                            if (Settings.Automation.IsAutoSaveStrokesAtClear && inkCanvas.Strokes.Count >
                                Settings.Automation.MinimumAutomationStrokeNumber)
                                SaveScreenshot(true);

                            //BtnClear_Click(null, null);
                        }

                    inkCanvas.IsHitTestVisible = true;
                    inkCanvas.Visibility = Visibility.Visible;
                }
                else {
                    if (isLoaded && Settings.Automation.IsAutoClearWhenExitingWritingMode &&
                        !Settings.PowerPointSettings.IsNoClearStrokeOnSelectWhenInPowerPoint)
                        if (inkCanvas.Strokes.Count > 0) {
                            if (Settings.Automation.IsAutoSaveStrokesAtClear && inkCanvas.Strokes.Count >
                                Settings.Automation.MinimumAutomationStrokeNumber)
                                SaveScreenshot(true);

                            //BtnClear_Click(null, null);
                        }


                    if (Settings.PowerPointSettings.IsShowStrokeOnSelectInPowerPoint) {
                        inkCanvas.Visibility = Visibility.Visible;
                        inkCanvas.IsHitTestVisible = true;
                    }
                    else {
                        inkCanvas.IsHitTestVisible = true;
                        inkCanvas.Visibility = Visibility.Visible;
                    }
                }

                GridTransparencyFakeBackground.Opacity = 0;
                GridTransparencyFakeBackground.Background = Brushes.Transparent;

                GridBackgroundCoverHolder.Visibility = Visibility.Collapsed;

                if (currentMode != 0) {
                    SaveStrokes();
                    RestoreStrokes(true);
                }
            }

            if (GridTransparencyFakeBackground.Background == Brushes.Transparent) {
                StackPanelCanvasControls.Visibility = Visibility.Collapsed;
                CheckEnableTwoFingerGestureBtnVisibility(false);
                HideSubPanels("cursor");
                // update tool selection
                SelectedMode = ICCToolsEnum.CursorMode;
                ForceUpdateToolSelection(null);
            }
            else {
                AnimationsHelper.ShowWithSlideFromLeftAndFade(StackPanelCanvasControls);
                CheckEnableTwoFingerGestureBtnVisibility(true);
            }
        }

        #endregion
    }
}

