using Ink_Canvas.ViewModels;
using System;
using System.Windows;

namespace Ink_Canvas.Dialogs
{
    /// <summary>
    /// YesOrNoNotificationWindow.xaml 的交互逻辑
    /// </summary>
    public partial class YesOrNoNotificationWindow : Window
    {
        private readonly Action _yesAction;
        private readonly Action _noAction;
        private readonly Action _windowClose;

        /// <summary>
        /// ViewModel 实例
        /// </summary>
        public YesOrNoNotificationWindowViewModel ViewModel { get; private set; }

        public YesOrNoNotificationWindow(string text, Action yesAction = null, Action noAction = null, Action windowClose = null)
        {
            _yesAction = yesAction;
            _noAction = noAction;
            _windowClose = windowClose;

            InitializeComponent();

            // 创建并设置 ViewModel
            ViewModel = new YesOrNoNotificationWindowViewModel();
            ViewModel.Initialize(text);
            DataContext = ViewModel;

            // 订阅 ViewModel 事件
            ViewModel.RequestClose += OnRequestClose;
            ViewModel.YesClicked += OnYesClicked;
            ViewModel.NoClicked += OnNoClicked;
            ViewModel.WindowClosing += OnWindowClosing;
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
        /// 点击"是"按钮
        /// </summary>
        private void OnYesClicked(object sender, EventArgs e)
        {
            _yesAction?.Invoke();
        }

        /// <summary>
        /// 点击"否"按钮
        /// </summary>
        private void OnNoClicked(object sender, EventArgs e)
        {
            _noAction?.Invoke();
        }

        /// <summary>
        /// 窗口关闭中
        /// </summary>
        private void OnWindowClosing(object sender, EventArgs e)
        {
            _windowClose?.Invoke();
        }

        /// <summary>
        /// "是"按钮点击事件
        /// </summary>
        private void ButtonYes_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel?.YesCommand.CanExecute(null) == true)
            {
                ViewModel.YesCommand.Execute(null);
            }
        }

        /// <summary>
        /// "否"按钮点击事件
        /// </summary>
        private void ButtonNo_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel?.NoCommand.CanExecute(null) == true)
            {
                ViewModel.NoCommand.Execute(null);
            }
        }

        /// <summary>
        /// 窗口已关闭事件
        /// </summary>
        private void Window_Closed(object sender, EventArgs e)
        {
            // 触发关闭回调
            _windowClose?.Invoke();

            // 清理资源
            if (ViewModel != null)
            {
                ViewModel.RequestClose -= OnRequestClose;
                ViewModel.YesClicked -= OnYesClicked;
                ViewModel.NoClicked -= OnNoClicked;
                ViewModel.WindowClosing -= OnWindowClosing;
                ViewModel.Cleanup();
            }
        }

        #endregion

        /// <summary>
        /// 获取对话框结果
        /// </summary>
        public bool? DialogResultValue => ViewModel?.DialogResult;
    }
}
