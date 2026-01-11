using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Ink_Canvas.Views.PPT
{
    /// <summary>
    /// PPTNavigationView.xaml 的交互逻辑
    /// 提供 PPT 演示模式下的页面导航控件
    /// </summary>
    public partial class PPTNavigationView : UserControl
    {
        #region Events

        /// <summary>
        /// 上一页事件
        /// </summary>
        public event EventHandler PreviousPageRequested;

        /// <summary>
        /// 下一页事件
        /// </summary>
        public event EventHandler NextPageRequested;

        /// <summary>
        /// 页面导航按钮点击事件（显示页面列表）
        /// </summary>
        public event EventHandler PageNavigationRequested;

        #endregion

        public PPTNavigationView()
        {
            InitializeComponent();
        }

        #region Dependency Properties

        public static readonly DependencyProperty CurrentPageProperty =
            DependencyProperty.Register(nameof(CurrentPage), typeof(int), typeof(PPTNavigationView),
                new PropertyMetadata(1));

        public int CurrentPage
        {
            get => (int)GetValue(CurrentPageProperty);
            set => SetValue(CurrentPageProperty, value);
        }

        public static readonly DependencyProperty TotalPagesProperty =
            DependencyProperty.Register(nameof(TotalPages), typeof(int), typeof(PPTNavigationView),
                new PropertyMetadata(1, OnTotalPagesChanged));

        public int TotalPages
        {
            get => (int)GetValue(TotalPagesProperty);
            set => SetValue(TotalPagesProperty, value);
        }

        private static void OnTotalPagesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PPTNavigationView view)
            {
                view.PPTBtnPageTotal.Text = $"/ {e.NewValue}";
            }
        }

        public static readonly DependencyProperty LeftPanelVisibilityProperty =
            DependencyProperty.Register(nameof(LeftPanelVisibility), typeof(Visibility), typeof(PPTNavigationView),
                new PropertyMetadata(Visibility.Visible));

        public Visibility LeftPanelVisibility
        {
            get => (Visibility)GetValue(LeftPanelVisibilityProperty);
            set => SetValue(LeftPanelVisibilityProperty, value);
        }

        public static readonly DependencyProperty RightPanelVisibilityProperty =
            DependencyProperty.Register(nameof(RightPanelVisibility), typeof(Visibility), typeof(PPTNavigationView),
                new PropertyMetadata(Visibility.Visible));

        public Visibility RightPanelVisibility
        {
            get => (Visibility)GetValue(RightPanelVisibilityProperty);
            set => SetValue(RightPanelVisibilityProperty, value);
        }

        #endregion

        #region Event Handlers

        private void GridPPTControlPrevious_MouseDown(object sender, MouseButtonEventArgs e)
        {
            AnimateFeedbackBorder(sender, 0.1);
        }

        private void GridPPTControlPrevious_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimateFeedbackBorder(sender, 0);
        }

        private void GridPPTControlPrevious_MouseUp(object sender, MouseButtonEventArgs e)
        {
            AnimateFeedbackBorder(sender, 0);
            PreviousPageRequested?.Invoke(this, EventArgs.Empty);
        }

        private void GridPPTControlNext_MouseDown(object sender, MouseButtonEventArgs e)
        {
            AnimateFeedbackBorder(sender, 0.1);
        }

        private void GridPPTControlNext_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimateFeedbackBorder(sender, 0);
        }

        private void GridPPTControlNext_MouseUp(object sender, MouseButtonEventArgs e)
        {
            AnimateFeedbackBorder(sender, 0);
            NextPageRequested?.Invoke(this, EventArgs.Empty);
        }

        private void PPTNavigationBtn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            AnimatePageButtonFeedback(sender, 0.1);
        }

        private void PPTNavigationBtn_MouseLeave(object sender, MouseEventArgs e)
        {
            AnimatePageButtonFeedback(sender, 0);
        }

        private void PPTNavigationBtn_MouseUp(object sender, MouseButtonEventArgs e)
        {
            AnimatePageButtonFeedback(sender, 0);
            PageNavigationRequested?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Animation Helpers

        private void AnimateFeedbackBorder(object sender, double targetOpacity)
        {
            if (sender is Border border)
            {
                string feedbackBorderName = border.Name.Replace("ButtonBorder", "ButtonFeedbackBorder");
                if (FindName(feedbackBorderName) is Border feedbackBorder)
                {
                    var animation = new DoubleAnimation(targetOpacity, TimeSpan.FromMilliseconds(100));
                    feedbackBorder.BeginAnimation(OpacityProperty, animation);
                }
            }
        }

        private void AnimatePageButtonFeedback(object sender, double targetOpacity)
        {
            if (sender is Border border)
            {
                string feedbackBorderName = border.Name.Replace("PageButton", "PageButtonFeedbackBorder");
                if (FindName(feedbackBorderName) is Border feedbackBorder)
                {
                    var animation = new DoubleAnimation(targetOpacity, TimeSpan.FromMilliseconds(100));
                    feedbackBorder.BeginAnimation(OpacityProperty, animation);
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 更新页面显示
        /// </summary>
        public void UpdatePageDisplay(int currentPage, int totalPages)
        {
            CurrentPage = currentPage;
            TotalPages = totalPages;
            PPTBtnPageNow.Text = currentPage.ToString();
            PPTBtnPageTotal.Text = $"/ {totalPages}";
        }

        /// <summary>
        /// 设置左侧面板可见性
        /// </summary>
        public void SetLeftPanelVisible(bool visible)
        {
            LeftPanelVisibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// 设置右侧面板可见性
        /// </summary>
        public void SetRightPanelVisible(bool visible)
        {
            RightPanelVisibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }

        #endregion
    }
}
