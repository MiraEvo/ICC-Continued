using Ink_Canvas.Helpers;
using Ink_Canvas.ViewModels;
using System;
using System.Windows;
using System.Windows.Input;

namespace Ink_Canvas.Dialogs
{
    /// <summary>
    /// Interaction logic for OperatingGuideWindow.xaml
    /// </summary>
    public partial class OperatingGuideWindow : Window
    {
        /// <summary>
        /// ViewModel 实例
        /// </summary>
        public OperatingGuideWindowViewModel ViewModel { get; private set; }

        public OperatingGuideWindow()
        {
            InitializeComponent();

            // 创建并设置 ViewModel
            ViewModel = new OperatingGuideWindowViewModel();
            DataContext = ViewModel;

            // 订阅 ViewModel 事件
            ViewModel.RequestClose += OnRequestClose;
            ViewModel.DragMoveRequested += OnDragMoveRequested;

            AnimationsHelper.ShowWithSlideFromBottomAndFade(this, 0.25);
        }

        /// <summary>
        /// 请求关闭窗口
        /// </summary>
        private void OnRequestClose(object sender, EventArgs e)
        {
            Close();
        }

        /// <summary>
        /// 请求拖动窗口
        /// </summary>
        private void OnDragMoveRequested(object sender, EventArgs e)
        {
            DragMove();
        }

        private void BtnClose_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ViewModel?.CloseCommand.Execute(null);
        }

        private void WindowDragMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                ViewModel?.DragMoveCommand.Execute(null);
            }
        }

        private void BtnFullscreen_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ViewModel?.ToggleFullscreenCommand.Execute(null);
        }

        private void ScrollViewerManipulationBoundaryFeedback(object sender, ManipulationBoundaryFeedbackEventArgs e)
        {
            e.Handled = true;
        }

        /// <summary>
        /// 窗口关闭时清理资源
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            // 取消订阅事件
            if (ViewModel != null)
            {
                ViewModel.RequestClose -= OnRequestClose;
                ViewModel.DragMoveRequested -= OnDragMoveRequested;
                ViewModel.Cleanup();
            }

            base.OnClosed(e);
        }
    }
}
