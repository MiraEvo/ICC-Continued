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
                    // 异步保存，但关闭事件是同步的，所以直接调用同步方法
                    try
                    {
                        string namesPath = System.IO.Path.Combine(App.RootPath, "Names.txt");
                        System.IO.File.WriteAllText(namesPath, ViewModel.NamesText);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        e.Cancel = true; // 保存失败，取消关闭
                    }
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true; // 取消关闭
                }
            }
        }

        /// <summary>
        /// 关闭按钮点击事件
        /// </summary>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
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
