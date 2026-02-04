using Ink_Canvas.Helpers;
using Ink_Canvas.ViewModels;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace Ink_Canvas
{
    /// <summary>
    /// Interaction logic for StopwatchWindow.xaml
    /// </summary>
    public partial class CountdownTimerWindow : Window
    {
        private bool _isInCompact = false;

        /// <summary>
        /// ViewModel 实例
        /// </summary>
        public CountdownTimerWindowViewModel ViewModel { get; private set; }

        public CountdownTimerWindow()
        {
            InitializeComponent();

            // 创建并设置 ViewModel
            ViewModel = new CountdownTimerWindowViewModel();
            DataContext = ViewModel;

            // 订阅 ViewModel 事件
            ViewModel.RequestClose += OnRequestClose;
            ViewModel.ToggleFullscreenRequested += OnToggleFullscreenRequested;
            ViewModel.ToggleCompactModeRequested += OnToggleCompactModeRequested;

            AnimationsHelper.ShowWithSlideFromBottomAndFade(this, 0.25);
        }

        #region 事件处理

        /// <summary>
        /// 请求关闭窗口
        /// </summary>
        private void OnRequestClose(object sender, EventArgs e)
        {
            Close();
        }

        /// <summary>
        /// 请求切换全屏
        /// </summary>
        private void OnToggleFullscreenRequested(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Normal)
            {
                WindowState = WindowState.Maximized;
                ViewModel.FullscreenButtonIcon = "\uE73F";
            }
            else
            {
                WindowState = WindowState.Normal;
                ViewModel.FullscreenButtonIcon = "\uE740";
            }
        }

        /// <summary>
        /// 请求切换紧凑模式
        /// </summary>
        private void OnToggleCompactModeRequested(object sender, EventArgs e)
        {
            if (_isInCompact)
            {
                Width = 1100;
                Height = 700;
                BigViewController.Visibility = Visibility.Visible;
                TbCurrentTime.Visibility = Visibility.Collapsed;

                // Set to center
                double dpiScaleX = 1, dpiScaleY = 1;
                PresentationSource source = PresentationSource.FromVisual(this);
                if (source != null)
                {
                    dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
                    dpiScaleY = source.CompositionTarget.TransformToDevice.M22;
                }
                IntPtr windowHandle = new WindowInteropHelper(this).Handle;
                System.Windows.Forms.Screen screen = System.Windows.Forms.Screen.FromHandle(windowHandle);
                double screenWidth = screen.Bounds.Width / dpiScaleX, screenHeight = screen.Bounds.Height / dpiScaleY;
                Left = (screenWidth / 2) - (Width / 2);
                Top = (screenHeight / 2) - (Height / 2);
                Left = (screenWidth / 2) - (Width / 2);
                Top = (screenHeight / 2) - (Height / 2);
            }
            else
            {
                if (WindowState == WindowState.Maximized) WindowState = WindowState.Normal;
                Width = 400;
                Height = 250;
                BigViewController.Visibility = Visibility.Collapsed;
                TbCurrentTime.Visibility = Visibility.Visible;
            }

            _isInCompact = !_isInCompact;
        }

        #endregion

        #region UI 事件处理

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 初始化由 ViewModel 处理
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ViewModel?.CloseCommand.Execute(null);
        }

        private void GridTime_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel?.ToggleSettingModeCommand.CanExecute(null) == true)
            {
                ViewModel.ToggleSettingModeCommand.Execute(null);
            }
        }

        private void BtnStart_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel?.StartPauseResumeCommand.CanExecute(null) == true)
            {
                ViewModel.StartPauseResumeCommand.Execute(null);
            }
        }

        private void BtnReset_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel?.ResetCommand.CanExecute(null) == true)
            {
                ViewModel.ResetCommand.Execute(null);
            }
        }

        private void BtnFullscreen_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel?.ToggleFullscreenCommand.CanExecute(null) == true)
            {
                ViewModel.ToggleFullscreenCommand.Execute(null);
            }
        }

        private void BtnMinimal_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel?.ToggleCompactModeCommand.CanExecute(null) == true)
            {
                ViewModel.ToggleCompactModeCommand.Execute(null);
            }
        }

        private void BtnClose_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel?.CloseCommand.CanExecute(null) == true)
            {
                ViewModel.CloseCommand.Execute(null);
            }
        }

        private void WindowDragMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        #endregion

        #region 时间调整按钮事件

        private void ButtonHourPlus1_Click(object sender, RoutedEventArgs e)
        {
            ViewModel?.IncrementHourCommand.Execute(1);
        }

        private void ButtonHourMinus1_Click(object sender, RoutedEventArgs e)
        {
            ViewModel?.DecrementHourCommand.Execute(1);
        }

        private void ButtonMinutePlus1_Click(object sender, RoutedEventArgs e)
        {
            ViewModel?.IncrementMinuteCommand.Execute(1);
        }

        private void ButtonMinuteMinus1_Click(object sender, RoutedEventArgs e)
        {
            ViewModel?.DecrementMinuteCommand.Execute(1);
        }

        private void ButtonSecondPlus1_Click(object sender, RoutedEventArgs e)
        {
            ViewModel?.IncrementSecondCommand.Execute(1);
        }

        private void ButtonSecondMinus1_Click(object sender, RoutedEventArgs e)
        {
            ViewModel?.DecrementSecondCommand.Execute(1);
        }

        private void ButtonSecondPlus5_Click(object sender, RoutedEventArgs e)
        {
            ViewModel?.IncrementSecondCommand.Execute(5);
        }

        private void ButtonSecondMinus5_Click(object sender, RoutedEventArgs e)
        {
            ViewModel?.DecrementSecondCommand.Execute(5);
        }

        #endregion

        /// <summary>
        /// 窗口关闭时清理资源
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            // 取消订阅事件
            if (ViewModel != null)
            {
                ViewModel.RequestClose -= OnRequestClose;
                ViewModel.ToggleFullscreenRequested -= OnToggleFullscreenRequested;
                ViewModel.ToggleCompactModeRequested -= OnToggleCompactModeRequested;
                ViewModel.Cleanup();
            }

            base.OnClosed(e);
        }
    }
}
