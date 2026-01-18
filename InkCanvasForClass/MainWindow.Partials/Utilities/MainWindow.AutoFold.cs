using Ink_Canvas.Helpers;
using Ink_Canvas.Core;
using iNKORE.UI.WPF.Modern;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace Ink_Canvas {
    public partial class MainWindow : Window {
        public bool isFloatingBarFolded = false;
        private bool isFloatingBarChangingHideMode = false;

        private void CloseWhiteboardImmediately() {
            if (isDisplayingOrHidingBlackboard) return;
            isDisplayingOrHidingBlackboard = true;
            HideSubPanelsImmediately();
            if (Settings.Gesture.AutoSwitchTwoFingerGesture) // 自动启用多指书写
            {
                Settings.Gesture.IsEnableTwoFingerTranslate = false;
                SaveSettings();
            }
            WaterMarkTime.Visibility = Visibility.Collapsed;
            WaterMarkDate.Visibility = Visibility.Collapsed;
            BlackBoardWaterMark.Visibility = Visibility.Collapsed;
            BtnSwitch_Click(null, null);
            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
            new Thread(new ThreadStart(() => {
                Thread.Sleep(Constants.ThemeSwitchDelayMilliseconds);
                Application.Current.Dispatcher.Invoke(() => { isDisplayingOrHidingBlackboard = false; });
            })).Start();
        }

        public async void FoldFloatingBar_MouseUp(object sender, MouseButtonEventArgs e) {
            await FoldFloatingBar(sender);
        }

        public async Task FoldFloatingBar(object sender)
        {
            var isShouldRejectAction = false;

            await Dispatcher.InvokeAsync(() => {
                if (lastBorderMouseDownObject != null && lastBorderMouseDownObject is Panel)
                    ((Panel)lastBorderMouseDownObject).Background = new SolidColorBrush(Colors.Transparent);
                if (sender == Fold_Icon && lastBorderMouseDownObject != Fold_Icon) isShouldRejectAction = true;
            });

            if (isShouldRejectAction) return;

            pointDesktop = new Point(-1, -1);
            pointPPT = new Point(-1, -1);

            // FloatingBarIcons_MouseUp_New(sender);
            if (sender == null)
                foldFloatingBarByUser = false;
            else
                foldFloatingBarByUser = true;
            unfoldFloatingBarByUser = false;

            if (isFloatingBarChangingHideMode) return;

            await Dispatcher.InvokeAsync(() => {
                InkCanvasForInkReplay.Visibility = Visibility.Collapsed;
                InkCanvasGridForInkReplay.Visibility = Visibility.Visible;
                InkCanvasGridForInkReplay.IsHitTestVisible = true;
                FloatingbarUIForInkReplay.Visibility = Visibility.Visible;
                FloatingbarUIForInkReplay.IsHitTestVisible = true;
                BlackboardUIGridForInkReplay.Visibility = Visibility.Visible;
                BlackboardUIGridForInkReplay.IsHitTestVisible = true;
                AnimationsHelper.HideWithFadeOut(BorderInkReplayToolBox);
                isStopInkReplay = true;
            });

            await Dispatcher.InvokeAsync(() => {
                isFloatingBarChangingHideMode = true;
                isFloatingBarFolded = true;
                if (currentMode != 0) CloseWhiteboardImmediately();
                if (StackPanelCanvasControls.Visibility == Visibility.Visible)
                    if (foldFloatingBarByUser && inkCanvas.Strokes.Count > Constants.StrokeCountThreshold)
                        ShowNewToast("正在清空墨迹并收纳至屏幕两边，可进入批注模式后通过 “撤销” 功能来恢复原先墨迹。",MW_Toast.ToastType.Informative, Constants.ToastDefaultDisplayDuration);
                CursorWithDelIcon_Click(null, null);
                RectangleSelectionHitTestBorder.Visibility = Visibility.Collapsed;
            });

            await Task.Delay(5);

            await Dispatcher.InvokeAsync(() => {
                LeftBottomPanelForPPTNavigation.Visibility = Visibility.Collapsed;
                RightBottomPanelForPPTNavigation.Visibility = Visibility.Collapsed;
                LeftSidePanelForPPTNavigation.Visibility = Visibility.Collapsed;
                RightSidePanelForPPTNavigation.Visibility = Visibility.Collapsed;
                ViewboxFloatingBarMarginAnimation(-60);
                HideSubPanels("cursor");
                // update tool selection
                SelectedMode = ICCToolsEnum.CursorMode;
                ForceUpdateToolSelection(null);
                SidePannelMarginAnimation(-10);

            });
            isFloatingBarChangingHideMode = false;
        }

        private async void LeftUnFoldButtonDisplayQuickPanel_MouseUp(object sender, MouseButtonEventArgs e) {
            if (Settings.Appearance.IsShowQuickPanel == true) {
                HideRightQuickPanel();
                LeftUnFoldButtonQuickPanel.Visibility = Visibility.Visible;
                await Dispatcher.InvokeAsync(() => {
                    var marginAnimation = new ThicknessAnimation {
                        Duration = TimeSpan.FromSeconds(Constants.UnfoldButtonAnimationDuration),
                        From = new Thickness(Constants.SidePanelCollapsedMargin, 0, 0, Constants.SidePanelBottomMargin),
                        To = new Thickness(Constants.UnfoldButtonInitialMargin, 0, 0, Constants.SidePanelBottomMargin)
                    };
                    marginAnimation.EasingFunction = new CubicEase();
                    LeftUnFoldButtonQuickPanel.BeginAnimation(MarginProperty, marginAnimation);
                });
                await Task.Delay(Constants.ShortDelayMilliseconds);

                await Dispatcher.InvokeAsync(() => {
                    LeftUnFoldButtonQuickPanel.Margin = new Thickness(Constants.UnfoldButtonInitialMargin, 0, 0, Constants.SidePanelBottomMargin);
                });
            }
            else {
                UnFoldFloatingBar_MouseUp(sender, e);
            }
        }

        private async void RightUnFoldButtonDisplayQuickPanel_MouseUp(object sender, MouseButtonEventArgs e) {
            if (Settings.Appearance.IsShowQuickPanel == true) {
                HideLeftQuickPanel();
                RightUnFoldButtonQuickPanel.Visibility = Visibility.Visible;
                await Dispatcher.InvokeAsync(() => {
                    var marginAnimation = new ThicknessAnimation {
                        Duration = TimeSpan.FromSeconds(Constants.UnfoldButtonAnimationDuration),
                        From = new Thickness(0, 0, Constants.SidePanelCollapsedMargin, Constants.SidePanelBottomMargin),
                        To = new Thickness(0, 0, Constants.UnfoldButtonInitialMargin, Constants.SidePanelBottomMargin)
                    };
                    marginAnimation.EasingFunction = new CubicEase();
                    RightUnFoldButtonQuickPanel.BeginAnimation(MarginProperty, marginAnimation);
                });
                await Task.Delay(Constants.ShortDelayMilliseconds);

                await Dispatcher.InvokeAsync(() => {
                    RightUnFoldButtonQuickPanel.Margin = new Thickness(0, 0, Constants.UnfoldButtonInitialMargin, Constants.SidePanelBottomMargin);
                });
            }
            else {
                UnFoldFloatingBar_MouseUp(sender, e);
            }
        }

        private async void HideLeftQuickPanel() {
            if (LeftUnFoldButtonQuickPanel.Visibility == Visibility.Visible) {
                await Dispatcher.InvokeAsync(() => {
                    var marginAnimation = new ThicknessAnimation {
                        Duration = TimeSpan.FromSeconds(Constants.UnfoldButtonAnimationDuration),
                        From = new Thickness(Constants.UnfoldButtonInitialMargin, 0, 0, Constants.SidePanelBottomMargin),
                        To = new Thickness(Constants.SidePanelCollapsedMargin, 0, 0, Constants.SidePanelBottomMargin)
                    };
                    marginAnimation.EasingFunction = new CubicEase();
                    LeftUnFoldButtonQuickPanel.BeginAnimation(MarginProperty, marginAnimation);
                });
                await Task.Delay(Constants.ShortDelayMilliseconds);

                await Dispatcher.InvokeAsync(() => {
                    LeftUnFoldButtonQuickPanel.Margin = new Thickness(0, 0, Constants.SidePanelCollapsedMargin, Constants.SidePanelBottomMargin);
                    LeftUnFoldButtonQuickPanel.Visibility = Visibility.Collapsed;
                });
            }
        }

        private async void HideRightQuickPanel() {
            if (RightUnFoldButtonQuickPanel.Visibility == Visibility.Visible) {
                await Dispatcher.InvokeAsync(() => {
                    var marginAnimation = new ThicknessAnimation {
                        Duration = TimeSpan.FromSeconds(Constants.UnfoldButtonAnimationDuration),
                        From = new Thickness(0, 0, Constants.UnfoldButtonInitialMargin, Constants.SidePanelBottomMargin),
                        To = new Thickness(0, 0, Constants.SidePanelCollapsedMargin, Constants.SidePanelBottomMargin)
                    };
                    marginAnimation.EasingFunction = new CubicEase();
                    RightUnFoldButtonQuickPanel.BeginAnimation(MarginProperty, marginAnimation);
                });
                await Task.Delay(Constants.ShortDelayMilliseconds);

                await Dispatcher.InvokeAsync(() => {
                    RightUnFoldButtonQuickPanel.Margin = new Thickness(0, 0, Constants.SidePanelCollapsedMargin, Constants.SidePanelBottomMargin);
                    RightUnFoldButtonQuickPanel.Visibility = Visibility.Collapsed;
                });
            }
        }

        private void HideQuickPanel_MouseUp(object sender, MouseButtonEventArgs e) {
            HideLeftQuickPanel();
            HideRightQuickPanel();
        }

        public async void UnFoldFloatingBar_MouseUp(object sender, MouseButtonEventArgs e) {
            await UnFoldFloatingBar(sender);
        }

        public async Task UnFoldFloatingBar(object sender)
        {
            await Dispatcher.InvokeAsync(() => {
                LeftUnFoldButtonQuickPanel.Visibility = Visibility.Collapsed;
                RightUnFoldButtonQuickPanel.Visibility = Visibility.Collapsed;
            });
            if (sender == null || BorderFloatingBarExitPPTBtn.Visibility == Visibility.Visible)
                unfoldFloatingBarByUser = false;
            else
                unfoldFloatingBarByUser = true;
            foldFloatingBarByUser = false;

            if (isFloatingBarChangingHideMode) return;

            await Dispatcher.InvokeAsync(() => {
                isFloatingBarChangingHideMode = true;
                isFloatingBarFolded = false;
            });

            await Task.Delay(0);

            await Dispatcher.InvokeAsync(() => {
                if (BorderFloatingBarExitPPTBtn.Visibility == Visibility.Visible)
                {
                    var dops = Settings.PowerPointSettings.PPTButtonsDisplayOption.ToString();
                    var dopsc = dops.ToCharArray();
                    if (dopsc[0] == '2' && isDisplayingOrHidingBlackboard == false) AnimationsHelper.ShowWithFadeIn(LeftBottomPanelForPPTNavigation);
                    if (dopsc[1] == '2' && isDisplayingOrHidingBlackboard == false) AnimationsHelper.ShowWithFadeIn(RightBottomPanelForPPTNavigation);
                    if (dopsc[2] == '2' && isDisplayingOrHidingBlackboard == false) AnimationsHelper.ShowWithFadeIn(LeftSidePanelForPPTNavigation);
                    if (dopsc[3] == '2' && isDisplayingOrHidingBlackboard == false) AnimationsHelper.ShowWithFadeIn(RightSidePanelForPPTNavigation);
                }

                if (BorderFloatingBarExitPPTBtn.Visibility == Visibility.Visible)
                    ViewboxFloatingBarMarginAnimation(Constants.FloatingBarBottomMarginPPT);
                else
                    ViewboxFloatingBarMarginAnimation(Constants.FloatingBarBottomMarginNormal, true);
                SidePannelMarginAnimation(Constants.SidePanelCollapsedMargin, !unfoldFloatingBarByUser);
            });

            isFloatingBarChangingHideMode = false;
        }

        private async void SidePannelMarginAnimation(int MarginFromEdge, bool isNoAnimation = false) // Possible value: -50, -10
        {
            await Dispatcher.InvokeAsync(() => {
                if (MarginFromEdge == -10) LeftSidePanel.Visibility = Visibility.Visible;

                var LeftSidePanelmarginAnimation = new ThicknessAnimation {
                    Duration = isNoAnimation == true ? TimeSpan.FromSeconds(0) : TimeSpan.FromSeconds(Constants.SidePanelAnimationDuration),
                    From = LeftSidePanel.Margin,
                    To = new Thickness(MarginFromEdge, 0, 0, Constants.SidePanelBottomMargin)
                };
                LeftSidePanelmarginAnimation.EasingFunction = new CubicEase();
                var RightSidePanelmarginAnimation = new ThicknessAnimation {
                    Duration = isNoAnimation == true ? TimeSpan.FromSeconds(0) : TimeSpan.FromSeconds(Constants.SidePanelAnimationDuration),
                    From = RightSidePanel.Margin,
                    To = new Thickness(0, 0, MarginFromEdge, Constants.SidePanelBottomMargin)
                };
                RightSidePanelmarginAnimation.EasingFunction = new CubicEase();
                LeftSidePanel.BeginAnimation(MarginProperty, LeftSidePanelmarginAnimation);
                RightSidePanel.BeginAnimation(MarginProperty, RightSidePanelmarginAnimation);
            });

            await Task.Delay(Constants.SidePanelAnimationCompleteDelay);

            await Dispatcher.InvokeAsync(() => {
                LeftSidePanel.Margin = new Thickness(MarginFromEdge, 0, 0, Constants.SidePanelBottomMargin);
                RightSidePanel.Margin = new Thickness(0, 0, MarginFromEdge, Constants.SidePanelBottomMargin);

                if (MarginFromEdge == Constants.SidePanelCollapsedMargin) LeftSidePanel.Visibility = Visibility.Collapsed;
            });
            isFloatingBarChangingHideMode = false;
        }
    }
}
