using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ink_Canvas.Core;
using System;

namespace Ink_Canvas.ViewModels
{
    /// <summary>
    /// OperatingGuideWindow ViewModel - 管理操作指南窗口状态和逻辑
    /// </summary>
    public partial class OperatingGuideWindowViewModel : ViewModelBase
    {
        #region 属性

        /// <summary>
        /// 窗口状态
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNormalState))]
        private System.Windows.WindowState _windowState = System.Windows.WindowState.Normal;

        /// <summary>
        /// 是否为正常状态（非最大化）
        /// </summary>
        public bool IsNormalState => WindowState == System.Windows.WindowState.Normal;

        /// <summary>
        /// 全屏按钮图标
        /// </summary>
        [ObservableProperty]
        private string _fullscreenButtonIcon = "Fullscreen24";

        #endregion

        #region 命令

        /// <summary>
        /// 关闭窗口命令
        /// </summary>
        [RelayCommand]
        private void Close()
        {
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 切换全屏命令
        /// </summary>
        [RelayCommand]
        private void ToggleFullscreen()
        {
            if (WindowState == System.Windows.WindowState.Normal)
            {
                WindowState = System.Windows.WindowState.Maximized;
                FullscreenButtonIcon = "Contract24";
            }
            else
            {
                WindowState = System.Windows.WindowState.Normal;
                FullscreenButtonIcon = "Fullscreen24";
            }
        }

        /// <summary>
        /// 窗口拖动命令
        /// </summary>
        [RelayCommand]
        private void DragMove()
        {
            DragMoveRequested?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region 事件

        /// <summary>
        /// 请求关闭窗口事件
        /// </summary>
        public event EventHandler RequestClose;

        /// <summary>
        /// 请求拖动窗口事件
        /// </summary>
        public event EventHandler DragMoveRequested;

        #endregion
    }
}
