using Ink_Canvas.Helpers;
using Ink_Canvas.Models.Settings;
using Ink_Canvas.ViewModels;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Ink_Canvas
{
    /// <summary>
    /// RandWindow.xaml 的交互逻辑
    /// </summary>
    public partial class RandWindow : Window
    {
        private readonly Settings _settings;

        public static int randSeed;

        /// <summary>
        /// ViewModel 实例
        /// </summary>
        public RandWindowViewModel ViewModel { get; private set; }

        public RandWindow(Settings settings)
        {
            InitializeComponent();
            _settings = settings;

            // 创建并设置 ViewModel
            ViewModel = new RandWindowViewModel(settings.RandSettings);
            DataContext = ViewModel;

            // 订阅 ViewModel 事件
            ViewModel.RequestClose += OnRequestClose;
            ViewModel.OpenNamesInputRequested += OnOpenNamesInputRequested;
            ViewModel.ErrorOccurred += OnErrorOccurred;

            // 显示动画
            AnimationsHelper.ShowWithSlideFromBottomAndFade(this, 0.25);
        }

        public RandWindow(Settings settings, bool isAutoClose)
        {
            InitializeComponent();
            _settings = settings;

            // 创建并设置 ViewModel
            ViewModel = new RandWindowViewModel(settings.RandSettings);
            ViewModel.SetAutoCloseMode(isAutoClose);
            DataContext = ViewModel;

            // 订阅 ViewModel 事件
            ViewModel.RequestClose += OnRequestClose;
            ViewModel.OpenNamesInputRequested += OnOpenNamesInputRequested;
            ViewModel.ErrorOccurred += OnErrorOccurred;

            // 自动开始逻辑
            Loaded += async (s, e) =>
            {
                await Task.Delay(100);
                if (ViewModel.StartRandomSelectionCommand.CanExecute(null))
                {
                    await ViewModel.StartRandomSelectionCommand.ExecuteAsync(null);
                }
            };
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
        /// 请求打开名单输入窗口
        /// </summary>
        private void OnOpenNamesInputRequested(object sender, EventArgs e)
        {
            new NamesInputWindow().ShowDialog();
            ViewModel?.LoadNames();
        }

        /// <summary>
        /// 发生错误
        /// </summary>
        private void OnErrorOccurred(object sender, string errorMessage)
        {
            MessageBox.Show($"发生错误: {errorMessage}");
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
                ViewModel.OpenNamesInputRequested -= OnOpenNamesInputRequested;
                ViewModel.ErrorOccurred -= OnErrorOccurred;
                ViewModel.Cleanup();
            }

            base.OnClosed(e);
        }
    }
}
