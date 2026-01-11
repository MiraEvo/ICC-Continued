using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Ink_Canvas.Views.Selection
{
    /// <summary>
    /// SelectionControlsView.xaml 的交互逻辑
    /// 提供笔画选择后的操作控件，包括克隆、旋转、翻转、锁定、删除等功能
    /// </summary>
    public partial class SelectionControlsView : UserControl
    {
        #region Events

        /// <summary>
        /// 克隆选中笔画事件
        /// </summary>
        public event EventHandler CloneRequested;

        /// <summary>
        /// 克隆到新页面事件
        /// </summary>
        public event EventHandler CloneToNewBoardRequested;

        /// <summary>
        /// 顺时针旋转事件
        /// </summary>
        public event EventHandler RotateClockwiseRequested;

        /// <summary>
        /// 旋转指定角度事件（45度或90度）
        /// </summary>
        public event EventHandler<RotateEventArgs> RotateRequested;

        /// <summary>
        /// 翻转事件
        /// </summary>
        public event EventHandler<FlipEventArgs> FlipRequested;

        /// <summary>
        /// 锁定/解锁事件
        /// </summary>
        public event EventHandler LockToggleRequested;

        /// <summary>
        /// 打开调色板事件
        /// </summary>
        public event EventHandler PaletteRequested;

        /// <summary>
        /// 删除选中笔画事件
        /// </summary>
        public event EventHandler DeleteRequested;

        /// <summary>
        /// 更多菜单事件
        /// </summary>
        public event EventHandler MoreMenuRequested;

        #endregion

        public SelectionControlsView()
        {
            InitializeComponent();
        }

        #region Dependency Properties

        public static readonly DependencyProperty IsLockedProperty =
            DependencyProperty.Register(nameof(IsLocked), typeof(bool), typeof(SelectionControlsView),
                new PropertyMetadata(false, OnIsLockedChanged));

        public bool IsLocked
        {
            get => (bool)GetValue(IsLockedProperty);
            set => SetValue(IsLockedProperty, value);
        }

        private static void OnIsLockedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SelectionControlsView view)
            {
                var isLocked = (bool)e.NewValue;
                view.BorderStrokeSelectionLock_LockClose.Visibility = isLocked ? Visibility.Visible : Visibility.Collapsed;
                view.BorderStrokeSelectionLock_LockOpen.Visibility = isLocked ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        #endregion

        #region Event Handlers

        private void BorderStrokeSelectionToolButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border)
            {
                border.Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(0x18, 0x09, 0x09, 0x0b));
            }
        }

        private void BorderStrokeSelectionToolButton_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Background = System.Windows.Media.Brushes.Transparent;
            }
        }

        private void BorderStrokeSelectionClone_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ResetButtonBackground(sender);
            CloneRequested?.Invoke(this, EventArgs.Empty);
        }

        private void BorderStrokeSelectionCloneToNewBoard_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ResetButtonBackground(sender);
            CloneToNewBoardRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ImageRotateClockwise_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ResetButtonBackground(sender);
            RotateClockwiseRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ImageRotate_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ResetButtonBackground(sender);
            if (sender is Border border)
            {
                int angle = border.Name == "BorderImageRotate45" ? 45 : 90;
                RotateRequested?.Invoke(this, new RotateEventArgs(angle));
            }
        }

        private void ImageFlip_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ResetButtonBackground(sender);
            if (sender is Border border)
            {
                bool isHorizontal = border.Name == "BorderImageFlipHorizontal";
                FlipRequested?.Invoke(this, new FlipEventArgs(isHorizontal));
            }
        }

        private void BorderStrokeSelectionLock_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ResetButtonBackground(sender);
            LockToggleRequested?.Invoke(this, EventArgs.Empty);
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            PaletteRequested?.Invoke(this, EventArgs.Empty);
        }

        private void BorderStrokeSelectionDelete_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ResetButtonBackground(sender);
            DeleteRequested?.Invoke(this, EventArgs.Empty);
        }

        private void BorderStrokeMoreMenuButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ResetButtonBackground(sender);
            MoreMenuRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ResetButtonBackground(object sender)
        {
            if (sender is Border border)
            {
                border.Background = System.Windows.Media.Brushes.Transparent;
            }
        }

        #endregion
    }

    #region Event Args

    public class RotateEventArgs : EventArgs
    {
        public int Angle { get; }

        public RotateEventArgs(int angle)
        {
            Angle = angle;
        }
    }

    public class FlipEventArgs : EventArgs
    {
        public bool IsHorizontal { get; }

        public FlipEventArgs(bool isHorizontal)
        {
            IsHorizontal = isHorizontal;
        }
    }

    #endregion
}
