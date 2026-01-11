using Ink_Canvas.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Ink_Canvas.Views.Blackboard
{
    /// <summary>
    /// BlackboardView.xaml 的交互逻辑
    /// </summary>
    public partial class BlackboardView : UserControl
    {
        #region 依赖属性

        /// <summary>
        /// ViewModel 依赖属性
        /// </summary>
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(
                nameof(ViewModel),
                typeof(BlackboardViewModel),
                typeof(BlackboardView),
                new PropertyMetadata(null, OnViewModelChanged));

        /// <summary>
        /// ViewModel
        /// </summary>
        public BlackboardViewModel ViewModel
        {
            get => (BlackboardViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BlackboardView view && e.NewValue is BlackboardViewModel vm)
            {
                view.DataContext = vm;
            }
        }

        #endregion

        #region 事件

        /// <summary>
        /// 上一页请求事件
        /// </summary>
        public event EventHandler PreviousPageRequested;

        /// <summary>
        /// 下一页请求事件
        /// </summary>
        public event EventHandler NextPageRequested;

        /// <summary>
        /// 添加页面请求事件
        /// </summary>
        public event EventHandler AddPageRequested;

        /// <summary>
        /// 页面列表点击事件
        /// </summary>
        public event EventHandler<int> PageListItemClicked;

        /// <summary>
        /// 页面列表切换事件
        /// </summary>
        public event EventHandler PageListToggled;

        /// <summary>
        /// 手势设置点击事件
        /// </summary>
        public event EventHandler GestureSettingsRequested;

        /// <summary>
        /// 背景设置点击事件
        /// </summary>
        public event EventHandler BackgroundSettingsRequested;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        public BlackboardView()
        {
            InitializeComponent();
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 上一页按钮点击
        /// </summary>
        private void BtnPreviousPage_Click(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel?.PreviousPageCommand?.CanExecute(null) == true)
            {
                ViewModel.PreviousPageCommand.Execute(null);
            }
            PreviousPageRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 下一页按钮点击
        /// </summary>
        private void BtnNextPage_Click(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel?.NextPageCommand?.CanExecute(null) == true)
            {
                ViewModel.NextPageCommand.Execute(null);
            }
            NextPageRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 添加页面按钮点击
        /// </summary>
        private void BtnAddPage_Click(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel?.AddPageCommand?.CanExecute(null) == true)
            {
                ViewModel.AddPageCommand.Execute(null);
            }
            AddPageRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 页面列表按钮点击
        /// </summary>
        private void BtnPageList_Click(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel?.TogglePageListCommand?.CanExecute(null) == true)
            {
                ViewModel.TogglePageListCommand.Execute(null);
            }
            PageListToggled?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 页面列表项点击
        /// </summary>
        private void PageListItem_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is PageThumbnail thumbnail)
            {
                int pageIndex = thumbnail.Index - 1; // 转换为0-based索引
                if (ViewModel?.NavigateToPageCommand?.CanExecute(pageIndex) == true)
                {
                    ViewModel.NavigateToPageCommand.Execute(pageIndex);
                }
                PageListItemClicked?.Invoke(this, pageIndex);
            }
        }

        /// <summary>
        /// 手势按钮点击
        /// </summary>
        private void BtnGesture_Click(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel?.ToggleGesturePanelCommand?.CanExecute(null) == true)
            {
                ViewModel.ToggleGesturePanelCommand.Execute(null);
            }
            GestureSettingsRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 背景按钮点击
        /// </summary>
        private void BtnBackground_Click(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel?.ToggleBackgroundPanelCommand?.CanExecute(null) == true)
            {
                ViewModel.ToggleBackgroundPanelCommand.Execute(null);
            }
            BackgroundSettingsRequested?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置左侧面板缩放
        /// </summary>
        public void SetLeftSideScale(double scaleX, double scaleY)
        {
            ViewboxBlackboardLeftSideScaleTransform.ScaleX = scaleX;
            ViewboxBlackboardLeftSideScaleTransform.ScaleY = scaleY;
        }

        /// <summary>
        /// 设置中间面板缩放
        /// </summary>
        public void SetCenterSideScale(double scaleX, double scaleY)
        {
            ViewboxBlackboardCenterSideScaleTransform.ScaleX = scaleX;
            ViewboxBlackboardCenterSideScaleTransform.ScaleY = scaleY;
        }

        /// <summary>
        /// 设置右侧面板缩放
        /// </summary>
        public void SetRightSideScale(double scaleX, double scaleY)
        {
            ViewboxBlackboardRightSideScaleTransform.ScaleX = scaleX;
            ViewboxBlackboardRightSideScaleTransform.ScaleY = scaleY;
        }

        /// <summary>
        /// 设置左侧面板可见性
        /// </summary>
        public void SetLeftSideVisibility(Visibility visibility)
        {
            BlackboardLeftSide.Visibility = visibility;
        }

        /// <summary>
        /// 设置中间面板可见性
        /// </summary>
        public void SetCenterSideVisibility(Visibility visibility)
        {
            BlackboardCenterSide.Visibility = visibility;
        }

        /// <summary>
        /// 设置右侧面板可见性
        /// </summary>
        public void SetRightSideVisibility(Visibility visibility)
        {
            BlackboardRightSide.Visibility = visibility;
        }

        /// <summary>
        /// 刷新页面缩略图
        /// </summary>
        public void RefreshThumbnails()
        {
            ViewModel?.RefreshPageThumbnails();
        }

        #endregion
    }
}
