using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ink_Canvas.Core;
using System;

namespace Ink_Canvas.ViewModels
{
    /// <summary>
    /// YesOrNoNotificationWindow ViewModel - 管理是/否确认对话框状态和逻辑
    /// </summary>
    public partial class YesOrNoNotificationWindowViewModel : ViewModelBase
    {
        #region 构造函数

        public YesOrNoNotificationWindowViewModel()
        {
        }

        #endregion

        #region 属性

        /// <summary>
        /// 对话框标题
        /// </summary>
        [ObservableProperty]
        private string _windowTitle = "演示文档设置 - Ink Canvas 画板";

        /// <summary>
        /// 提示消息内容
        /// </summary>
        [ObservableProperty]
        private string _messageText = string.Empty;

        /// <summary>
        /// 图标前景色
        /// </summary>
        [ObservableProperty]
        private string _iconForeground = "#15803d";

        /// <summary>
        /// "是"按钮文本
        /// </summary>
        [ObservableProperty]
        private string _yesButtonText = "是";

        /// <summary>
        /// "否"按钮文本
        /// </summary>
        [ObservableProperty]
        private string _noButtonText = "否";

        /// <summary>
        /// 是否已选择（用于返回结果）
        /// </summary>
        [ObservableProperty]
        private bool? _dialogResult;

        #endregion

        #region 命令

        /// <summary>
        /// 点击"是"按钮命令
        /// </summary>
        [RelayCommand]
        private void Yes()
        {
            DialogResult = true;
            YesClicked?.Invoke(this, EventArgs.Empty);
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 点击"否"按钮命令
        /// </summary>
        [RelayCommand]
        private void No()
        {
            DialogResult = false;
            NoClicked?.Invoke(this, EventArgs.Empty);
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 关闭窗口命令
        /// </summary>
        [RelayCommand]
        private void Close()
        {
            WindowClosing?.Invoke(this, EventArgs.Empty);
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region 事件

        /// <summary>
        /// 请求关闭窗口事件
        /// </summary>
        public event EventHandler RequestClose;

        /// <summary>
        /// 点击"是"按钮事件
        /// </summary>
        public event EventHandler YesClicked;

        /// <summary>
        /// 点击"否"按钮事件
        /// </summary>
        public event EventHandler NoClicked;

        /// <summary>
        /// 窗口关闭事件
        /// </summary>
        public event EventHandler WindowClosing;

        #endregion

        #region 公共方法

        /// <summary>
        /// 初始化对话框内容
        /// </summary>
        /// <param name="message">提示消息</param>
        /// <param name="title">窗口标题（可选）</param>
        public void Initialize(string message, string title = null)
        {
            MessageText = message;
            if (!string.IsNullOrEmpty(title))
            {
                WindowTitle = title;
            }
        }

        #endregion
    }
}
