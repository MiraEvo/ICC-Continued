using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using iNKORE.UI.WPF.Modern.Controls;
using Ink_Canvas.ViewModels;
using Ink_Canvas.Core;
using System.Windows.Media.Animation;
using Ink_Canvas.Popups;

namespace Ink_Canvas.Views
{
    /// <summary>
    /// 设置视图用户控件
    /// 提供应用程序设置的用户界面
    /// </summary>
    public partial class SettingsView : UserControl
    {
        /// <summary>
        /// 初始化 SettingsView 的新实例
        /// </summary>
        public SettingsView()
        {
            InitializeComponent();
            Loaded += SettingsView_Loaded;
        }

        /// <summary>
        /// 视图加载完成时的事件处理器
        /// 负责初始化 DataContext
        /// </summary>
        /// <param name="sender">事件源</param>
        /// <param name="e">事件参数</param>
        private void SettingsView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext == null || !(DataContext is SettingsPageViewModel))
            {
                if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
                {
                    return;
                }
                
                try 
                {
                    // 从服务定位器获取（使用构造函数注入的 ViewModel）
                    var viewModel = ServiceLocator.GetRequiredService<SettingsPageViewModel>();
                    DataContext = viewModel;
                }
                catch (Exception ex)
                {
                    // 记录错误但不创建新实例，因为没有依赖项
                    System.Diagnostics.Debug.WriteLine($"Failed to get SettingsPageViewModel: {ex.Message}");
                }
            }
        }

        private void SCManipulationBoundaryFeedback(object sender, ManipulationBoundaryFeedbackEventArgs e) => e.Handled = true;

        private void SettingsPaneScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scrollViewer = (ScrollViewer)sender;
            var sb = new System.Windows.Media.Animation.Storyboard();
            var ofs = scrollViewer.VerticalOffset;
            var animation = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = ofs,
                To = ofs - e.Delta * 2.5,
                Duration = TimeSpan.FromMilliseconds(155)
            };
            animation.EasingFunction = new System.Windows.Media.Animation.CubicEase() 
            { 
                EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut,
            };
            System.Windows.Media.Animation.Storyboard.SetTargetProperty(animation, new PropertyPath(ColorPalette.ScrollViewerBehavior.VerticalOffsetProperty));
            System.Windows.Media.Animation.Storyboard.SetTargetName(animation, "SettingsPanelScrollViewer");
            sb.Children.Add(animation);
            scrollViewer.ScrollToVerticalOffset(ofs);
            sb.Begin(scrollViewer);
        }

        private void SettingsPane_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (DataContext is SettingsPageViewModel viewModel)
            {
                viewModel.UpdateScrollStatus(e.VerticalOffset, e.ExtentHeight, e.ViewportHeight);
            }
        }

        private void SettingsPaneScrollBarTrack_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is SettingsPageViewModel viewModel)
            {
                var position = e.GetPosition((FrameworkElement)sender);
                var trackHeight = ((FrameworkElement)sender).ActualHeight;
                if (trackHeight > 0)
                {
                    var ratio = position.Y / trackHeight;
                    var offset = ratio * (viewModel.ScrollMaxHeight - viewModel.ScrollActualHeight);
                    SettingsPanelScrollViewer.ScrollToVerticalOffset(offset);
                }
            }
        }

        private void SettingsPaneScrollBarThumb_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SettingsPaneScrollBarThumb.CaptureMouse();
        }

        private void SettingsPaneScrollBarThumb_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && SettingsPaneScrollBarThumb.IsMouseCaptured)
            {
                var position = e.GetPosition(SettingsPaneScrollBarTrack);
                var trackHeight = SettingsPaneScrollBarTrack.ActualHeight;
                
                if (trackHeight > 0)
                {
                    var scrollViewer = SettingsPanelScrollViewer;
                    var ratio = position.Y / trackHeight;
                    var scrollOffset = (scrollViewer.ExtentHeight - scrollViewer.ActualHeight) * ratio;
                    scrollViewer.ScrollToVerticalOffset(scrollOffset);
                }
            }
        }

        private void SettingsPaneScrollBarThumb_MouseUp(object sender, MouseButtonEventArgs e)
        {
            SettingsPaneScrollBarThumb.ReleaseMouseCapture();
        }

        private void SettingsPaneBackBtn_MouseEnter(object sender, MouseEventArgs e)
        {
            var sb = new System.Windows.Media.Animation.Storyboard();
            var fadeAnimation = new System.Windows.Media.Animation.DoubleAnimation 
            { 
                From = 0, 
                To = 1, 
                Duration = TimeSpan.FromSeconds(0.1) 
            };
            fadeAnimation.EasingFunction = new System.Windows.Media.Animation.CubicEase();
            System.Windows.Media.Animation.Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(UIElement.OpacityProperty));
            sb.Children.Add(fadeAnimation);
            sb.Begin((FrameworkElement)SettingsPaneBackBtnHighlight);
            sb.Completed += (o, args) => 
            { 
                SettingsPaneBackBtnHighlight.Opacity = 1; 
            };
        }

        private void SettingsPaneBackBtn_MouseLeave(object sender, MouseEventArgs e)
        {
            var sb = new System.Windows.Media.Animation.Storyboard();
            var fadeAnimation = new System.Windows.Media.Animation.DoubleAnimation 
            { 
                From = 1, 
                To = 0, 
                Duration = TimeSpan.FromSeconds(0.1) 
            };
            fadeAnimation.EasingFunction = new System.Windows.Media.Animation.CubicEase();
            System.Windows.Media.Animation.Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(UIElement.OpacityProperty));
            sb.Children.Add(fadeAnimation);
            sb.Begin((FrameworkElement)SettingsPaneBackBtnHighlight);
            sb.Completed += (o, args) => 
            { 
                SettingsPaneBackBtnHighlight.Opacity = 0; 
            };
        }

        private void BtnSettings_Click(object sender, MouseButtonEventArgs e)
        {
            // 关闭设置面板的逻辑，这里可以通过ViewModel的命令来实现，或者保持原有的关闭逻辑
            // 如果是通过MainWindow控制的可见性，可能需要发送消息或使用事件
            // 这里假设MainWindow会处理这个点击，因为它是Bubble事件
            // 或者我们可以调用ViewModel的关闭命令
            if (DataContext is SettingsPageViewModel viewModel)
            {
                // 如果ViewModel有关闭命令
                // viewModel.CloseCommand.Execute(null);
            }
            
            // 保持向上冒泡，让MainWindow处理关闭
        }
    }
}