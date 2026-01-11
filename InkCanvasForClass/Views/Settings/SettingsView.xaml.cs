using Ink_Canvas.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Ink_Canvas.Views.Settings
{
    /// <summary>
    /// SettingsView.xaml 的交互逻辑
    /// 设置面板 UserControl，提供设置分类导航和内容显示
    /// </summary>
    public partial class SettingsView : UserControl
    {
        private Border _currentSelectedNavButton;

        /// <summary>
        /// 构造函数
        /// </summary>
        public SettingsView()
        {
            InitializeComponent();
            Loaded += SettingsView_Loaded;
        }

        /// <summary>
        /// ViewModel 属性
        /// </summary>
        public SettingsViewModel ViewModel
        {
            get => DataContext as SettingsViewModel;
            set => DataContext = value;
        }

        /// <summary>
        /// 控件加载完成事件
        /// </summary>
        private void SettingsView_Loaded(object sender, RoutedEventArgs e)
        {
            // 默认选中启动设置
            if (_currentSelectedNavButton == null)
            {
                SelectNavButton(SettingsStartupNavButton);
            }
        }

        /// <summary>
        /// 导航按钮点击事件
        /// </summary>
        private void SettingsNavButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border)
            {
                SelectNavButton(border);
                
                // 触发导航事件
                var category = border.Tag?.ToString();
                if (!string.IsNullOrEmpty(category))
                {
                    NavigateToCategory?.Invoke(this, new SettingsNavigationEventArgs(category));
                }
            }
        }

        /// <summary>
        /// 选中导航按钮
        /// </summary>
        private void SelectNavButton(Border button)
        {
            // 取消之前选中的按钮
            if (_currentSelectedNavButton != null)
            {
                _currentSelectedNavButton.BorderThickness = new Thickness(0, 0, 0, 0);
            }

            // 选中新按钮
            _currentSelectedNavButton = button;
            if (_currentSelectedNavButton != null)
            {
                _currentSelectedNavButton.BorderThickness = new Thickness(0, 0, 4, 0);
            }
        }

        /// <summary>
        /// 滚动到指定分类
        /// </summary>
        /// <param name="categoryName">分类名称</param>
        public void ScrollToCategory(string categoryName)
        {
            // 根据分类名称选中对应的导航按钮
            Border targetButton = categoryName switch
            {
                "Startup" => SettingsStartupNavButton,
                "Canvas" => SettingsCanvasNavButton,
                "Gesture" => SettingsGestureNavButton,
                "Appearance" => SettingsAppearanceNavButton,
                "PowerPoint" => SettingsPPTNavButton,
                "Advanced" => SettingsAdvancedNavButton,
                "Automation" => SettingsAutomationNavButton,
                "About" => SettingsAboutNavButton,
                _ => null
            };

            if (targetButton != null)
            {
                SelectNavButton(targetButton);
            }
        }

        /// <summary>
        /// 获取滚动查看器
        /// </summary>
        public ScrollViewer GetScrollViewer() => SettingsPanelScrollViewer;

        /// <summary>
        /// 获取内容面板
        /// </summary>
        public StackPanel GetContentPanel() => SettingsContentPanel;

        /// <summary>
        /// 导航事件
        /// </summary>
        public event EventHandler<SettingsNavigationEventArgs> NavigateToCategory;

        /// <summary>
        /// 滚动变化事件
        /// </summary>
        public event EventHandler<ScrollChangedEventArgs> ScrollChanged;

        /// <summary>
        /// 触发滚动变化事件（供外部调用）
        /// </summary>
        internal void OnScrollChanged(ScrollChangedEventArgs e)
        {
            ScrollChanged?.Invoke(this, e);
        }
    }

    /// <summary>
    /// 设置导航事件参数
    /// </summary>
    public class SettingsNavigationEventArgs : EventArgs
    {
        /// <summary>
        /// 目标分类名称
        /// </summary>
        public string CategoryName { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="categoryName">分类名称</param>
        public SettingsNavigationEventArgs(string categoryName)
        {
            CategoryName = categoryName;
        }
    }
}
