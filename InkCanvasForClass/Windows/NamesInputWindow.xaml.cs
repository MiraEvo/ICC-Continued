using Ink_Canvas.Helpers;
using Ink_Canvas.ViewModels;
using System;
using System.ComponentModel;
using System.Windows;

namespace Ink_Canvas
{
    /// <summary>
    /// NamesInputWindow.xaml 的交互逻辑
    /// </summary>
    public partial class NamesInputWindow : Window
    {
        /// <summary>
        /// ViewModel 实例
        /// </summary>
        public NamesInputWindowViewModel ViewModel { get; private set; }

        public NamesInputWindow()
        {
            InitializeComponent();

            // 创建并设置 ViewModel
            ViewModel = new NamesInputWindowViewModel();
            DataContext = ViewModel;

            // 订阅 ViewModel 事件
            ViewModel.RequestClose += OnRequestClose;
            ViewModel.ErrorOccurred += OnErrorOccurred;

            // 显示动画
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
        /// 发生错误
        /// </summary>
        private void OnErrorOccurred(object sender, string errorMessage)
        {
            MessageBox.Show(errorMessage, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// 窗口关闭中事件
        /// </summary>
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // 检查是否需要保存
            if (ViewModel?.CheckNeedsSave() == true)
            {
                var result = MessageBox.Show("是否保存？", "名单导入", MessageBoxButton.YesNoCancel);
                if (result == MessageBoxResult.Yes)
                {
                    if (!ViewModel.SaveNamesSync())
                    {
                        e.Cancel = true;
                    }
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true; // 取消关闭
                }
            }
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
                ViewModel.ErrorOccurred -= OnErrorOccurred;
                ViewModel.Cleanup();
            }

            base.OnClosed(e);
        }
    }
}
