// ============================================================================
// MW_AppearanceSettingsHandler.cs - 外观设置实时更新处理
// ============================================================================
//
// 功能说明:
//   - 监听外观设置变化事件
//   - 实时更新浮动工具栏UI
//   - 处理白板设置变化
//   - 处理托盘图标和收纳模式设置
//
// ============================================================================

using Ink_Canvas.ViewModels;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Ink_Canvas
{
    public partial class MainWindow {
        /// <summary>
        /// 初始化外观设置事件监听
        /// </summary>
        private void InitializeAppearanceSettingsHandler()
        {
            AppearanceSettingsViewModel.AppearanceSettingChanged += OnAppearanceSettingChanged;
        }

        /// <summary>
        /// 清理外观设置事件监听
        /// </summary>
        private void CleanupAppearanceSettingsHandler()
        {
            AppearanceSettingsViewModel.AppearanceSettingChanged -= OnAppearanceSettingChanged;
        }

        /// <summary>
        /// 处理外观设置变化
        /// </summary>
        private void OnAppearanceSettingChanged(object sender, AppearanceSettingChangedEventArgs e)
        {
            // 确保在UI线程上执行
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => OnAppearanceSettingChanged(sender, e));
                return;
            }

            switch (e.PropertyName)
            {
                // 浮动工具栏按钮显示/隐藏
                case nameof(AppearanceSettingsViewModel.IsShowShapeButton):
                case nameof(AppearanceSettingsViewModel.IsShowFreezeButton):
                case nameof(AppearanceSettingsViewModel.IsShowRoamingButton):
                case nameof(AppearanceSettingsViewModel.IsShowUndoButton):
                case nameof(AppearanceSettingsViewModel.IsShowRedoButton):
                case nameof(AppearanceSettingsViewModel.IsShowClearButton):
                case nameof(AppearanceSettingsViewModel.IsShowSelectButton):
                case nameof(AppearanceSettingsViewModel.IsShowWhiteboardButton):
                case nameof(AppearanceSettingsViewModel.IsShowHideButton):
                case nameof(AppearanceSettingsViewModel.IsShowGestureButton):
                case nameof(AppearanceSettingsViewModel.FloatingBarIconsVisibility):
                    UpdateFloatingBarIconsVisibility();
                    UpdateFloatingBarIconsLayout();
                    ForceUpdateToolSelection(null);
                    break;

                // 橡皮按钮显示
                case nameof(AppearanceSettingsViewModel.EraserButtonsVisibility):
                case nameof(AppearanceSettingsViewModel.OnlyDisplayEraserBtn):
                    UpdateFloatingBarIconsVisibility();
                    break;

                // 浮动工具栏按钮标签
                case nameof(AppearanceSettingsViewModel.FloatingBarButtonLabelVisibility):
                    ApplyFloatingBarButtonLabelVisibility();
                    break;

                // 浮动工具栏缩放
                case nameof(AppearanceSettingsViewModel.ViewboxFloatingBarScaleTransformValue):
                    ApplyFloatingBarScale();
                    break;

                // 浮动工具栏透明度
                case nameof(AppearanceSettingsViewModel.ViewboxFloatingBarOpacityValue):
                    ApplyFloatingBarOpacity();
                    break;

                // 浮动工具栏图标
                case nameof(AppearanceSettingsViewModel.FloatingBarImg):
                    ApplyFloatingBarIcon();
                    break;

                // 显示笔尖模式切换按钮
                case nameof(AppearanceSettingsViewModel.IsEnableDisPlayNibModeToggler):
                    ApplyNibModeTogglerVisibility();
                    break;

                // 白板 UI 缩放
                case nameof(AppearanceSettingsViewModel.EnableViewboxBlackBoardScaleTransform):
                    ApplyWhiteboardScaleTransform();
                    break;

                // 托盘图标
                case nameof(AppearanceSettingsViewModel.EnableTrayIcon):
                    ApplyTrayIconVisibility();
                    break;

                // 取消收纳按钮图标
                case nameof(AppearanceSettingsViewModel.UnFoldButtonImageType):
                    ApplyUnFoldButtonImage();
                    break;

                // 鸡汤来源
                case nameof(AppearanceSettingsViewModel.ChickenSoupSource):
                    ApplyChickenSoupSource();
                    break;

                // 白板时间显示
                case nameof(AppearanceSettingsViewModel.EnableTimeDisplayInWhiteboardMode):
                    ApplyTimeDisplayInWhiteboardMode();
                    break;

                // 白板鸡汤显示
                case nameof(AppearanceSettingsViewModel.EnableChickenSoupInWhiteboardMode):
                    ApplyChickenSoupInWhiteboardMode();
                    break;
            }
        }

        /// <summary>
        /// 应用浮动工具栏按钮标签可见性
        /// </summary>
        private void ApplyFloatingBarButtonLabelVisibility()
        {
            FloatingBarTextVisibilityBindingLikeAPieceOfShit.Visibility =
                Settings.Appearance.FloatingBarButtonLabelVisibility ? Visibility.Visible : Visibility.Collapsed;
            UpdateFloatingBarIconsLayout();
        }

        /// <summary>
        /// 应用浮动工具栏缩放
        /// </summary>
        private void ApplyFloatingBarScale()
        {
            double val = Settings.Appearance.ViewboxFloatingBarScaleTransformValue;
            var scale = (val > 0.5 && val < 1.25) ? val : val <= 0.5 ? 0.5 : val >= 1.25 ? 1.25 : 1;

            // 更新 FloatingBarViewModel 的缩放
            _floatingBarViewModel?.UpdateScale(scale);

            // 同时更新 ViewboxFloatingBar 的缩放
            if (ViewboxFloatingBar.LayoutTransform is ScaleTransform st)
            {
                st.ScaleX = scale;
                st.ScaleY = scale;
            }
        }

        /// <summary>
        /// 应用浮动工具栏透明度
        /// </summary>
        private void ApplyFloatingBarOpacity()
        {
            ViewboxFloatingBar.Opacity = Settings.Appearance.ViewboxFloatingBarOpacityValue;
        }

        /// <summary>
        /// 应用浮动工具栏图标
        /// </summary>
        private void ApplyFloatingBarIcon()
        {
            int selectedIndex = Settings.Appearance.FloatingBarImg;

            if (selectedIndex == 0)
            {
                FloatingbarHeadIconImg.Source =
                    new BitmapImage(new Uri("pack://application:,,,/Resources/Icons-png/icc.png"));
                FloatingbarHeadIconImg.Margin = new Thickness(0.5);
            }
            else if (selectedIndex == 1)
            {
                FloatingbarHeadIconImg.Source =
                    new BitmapImage(
                        new Uri("pack://application:,,,/Resources/Icons-png/icc-transparent-dark-small.png"));
                FloatingbarHeadIconImg.Margin = new Thickness(1.2);
            }
        }

        /// <summary>
        /// 应用笔尖模式切换按钮可见性
        /// </summary>
        private void ApplyNibModeTogglerVisibility()
        {
            // 检查控件是否存在，如果不存在则跳过
            var nibModePanel = this.FindName("NibModeSimpleStackPanel") as FrameworkElement;
            var boardNibModePanel = this.FindName("BoardNibModeSimpleStackPanel") as FrameworkElement;
            
            if (nibModePanel == null && boardNibModePanel == null)
            {
                return;
            }

            var visibility = Settings.Appearance.IsEnableDisPlayNibModeToggler
                ? Visibility.Visible
                : Visibility.Collapsed;

            if (nibModePanel != null)
            {
                nibModePanel.Visibility = visibility;
            }

            if (boardNibModePanel != null)
            {
                boardNibModePanel.Visibility = visibility;
            }
        }

        /// <summary>
        /// 应用白板缩放变换
        /// </summary>
        private void ApplyWhiteboardScaleTransform()
        {
            if (Settings.Appearance.EnableViewboxBlackBoardScaleTransform)
            {
                ViewboxBlackboardLeftSideScaleTransform.ScaleX = 0.8;
                ViewboxBlackboardLeftSideScaleTransform.ScaleY = 0.8;
                ViewboxBlackboardCenterSideScaleTransform.ScaleX = 0.8;
                ViewboxBlackboardCenterSideScaleTransform.ScaleY = 0.8;
                ViewboxBlackboardRightSideScaleTransform.ScaleX = 0.8;
                ViewboxBlackboardRightSideScaleTransform.ScaleY = 0.8;
            }
            else
            {
                ViewboxBlackboardLeftSideScaleTransform.ScaleX = 1;
                ViewboxBlackboardLeftSideScaleTransform.ScaleY = 1;
                ViewboxBlackboardCenterSideScaleTransform.ScaleX = 1;
                ViewboxBlackboardCenterSideScaleTransform.ScaleY = 1;
                ViewboxBlackboardRightSideScaleTransform.ScaleX = 1;
                ViewboxBlackboardRightSideScaleTransform.ScaleY = 1;
            }
        }

        /// <summary>
        /// 应用托盘图标可见性
        /// </summary>
        private void ApplyTrayIconVisibility()
        {
            TrayIcon.Visibility = Settings.Appearance.EnableTrayIcon ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// 应用取消收纳按钮图标
        /// </summary>
        private void ApplyUnFoldButtonImage()
        {
            switch (Settings.Appearance.UnFoldButtonImageType)
            {
                case 0:
                    RightUnFoldBtnImgChevron.Source =
                        new BitmapImage(new Uri("pack://application:,,,/Resources/new-icons/unfold-chevron.png"));
                    RightUnFoldBtnImgChevron.Width = 14;
                    RightUnFoldBtnImgChevron.Height = 14;
                    RightUnFoldBtnImgChevron.RenderTransform = new RotateTransform(180);
                    LeftUnFoldBtnImgChevron.Source =
                        new BitmapImage(new Uri("pack://application:,,,/Resources/new-icons/unfold-chevron.png"));
                    LeftUnFoldBtnImgChevron.Width = 14;
                    LeftUnFoldBtnImgChevron.Height = 14;
                    LeftUnFoldBtnImgChevron.RenderTransform = null;
                    break;
                case 1:
                    RightUnFoldBtnImgChevron.Source =
                        new BitmapImage(new Uri("pack://application:,,,/Resources/new-icons/pen-white.png"));
                    RightUnFoldBtnImgChevron.Width = 18;
                    RightUnFoldBtnImgChevron.Height = 18;
                    RightUnFoldBtnImgChevron.RenderTransform = null;
                    LeftUnFoldBtnImgChevron.Source =
                        new BitmapImage(new Uri("pack://application:,,,/Resources/new-icons/pen-white.png"));
                    LeftUnFoldBtnImgChevron.Width = 18;
                    LeftUnFoldBtnImgChevron.Height = 18;
                    LeftUnFoldBtnImgChevron.RenderTransform = null;
                    break;
            }
        }

        /// <summary>
        /// 应用鸡汤来源设置
        /// </summary>
        private void ApplyChickenSoupSource()
        {
            if (Settings.Appearance.ChickenSoupSource == 0)
            {
                int randChickenSoupIndex = Random.Shared.Next(ChickenSoup.OSUPlayerYuLu.Length);
                BlackBoardWaterMark.Text = ChickenSoup.OSUPlayerYuLu[randChickenSoupIndex];
            }
            else if (Settings.Appearance.ChickenSoupSource == 1)
            {
                int randChickenSoupIndex = Random.Shared.Next(ChickenSoup.MingYanJingJu.Length);
                BlackBoardWaterMark.Text = ChickenSoup.MingYanJingJu[randChickenSoupIndex];
            }
            else if (Settings.Appearance.ChickenSoupSource == 2)
            {
                int randChickenSoupIndex = Random.Shared.Next(ChickenSoup.GaoKaoPhrases.Length);
                BlackBoardWaterMark.Text = ChickenSoup.GaoKaoPhrases[randChickenSoupIndex];
            }
        }

        /// <summary>
        /// 应用白板时间显示设置
        /// </summary>
        private void ApplyTimeDisplayInWhiteboardMode()
        {
            if (currentMode == 1)
            {
                if (Settings.Appearance.EnableTimeDisplayInWhiteboardMode)
                {
                    WaterMarkTime.Visibility = Visibility.Visible;
                    WaterMarkDate.Visibility = Visibility.Visible;
                }
                else
                {
                    WaterMarkTime.Visibility = Visibility.Collapsed;
                    WaterMarkDate.Visibility = Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// 应用白板鸡汤显示设置
        /// </summary>
        private void ApplyChickenSoupInWhiteboardMode()
        {
            if (currentMode == 1)
            {
                if (Settings.Appearance.EnableChickenSoupInWhiteboardMode)
                {
                    BlackBoardWaterMark.Visibility = Visibility.Visible;
                }
                else
                {
                    BlackBoardWaterMark.Visibility = Visibility.Collapsed;
                }
            }
        }
    }
}
