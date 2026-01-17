using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Ink_Canvas.ViewModels;
using Ink_Canvas.Views.Settings.Pages;
using iNKORE.UI.WPF.Modern.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace Ink_Canvas.Views.Settings
{
    /// <summary>
    /// SettingsWindow.xaml 的交互逻辑
    /// Fluent Design 风格的设置窗口
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsViewModel ViewModel { get; private set; }

        // 页面缓存
        private readonly Dictionary<string, UserControl> _pages = new Dictionary<string, UserControl>();

        public SettingsWindow()
        {
            InitializeComponent();

            // 获取 ViewModel
            if (App.Current is App app && app.Services != null)
            {
                ViewModel = app.Services.GetService<SettingsViewModel>();
                if (ViewModel != null)
                {
                    DataContext = ViewModel;
                }
            }

            Loaded += SettingsWindow_Loaded;
        }

        private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 默认选中第一项并导航
            if (NavView.MenuItems.Count > 0)
            {
                NavView.SelectedItem = NavView.MenuItems[0];
                // 强制导航一次，以防 SelectionChanged 在 Loaded 之前没有触发
                if (NavView.SelectedItem is NavigationViewItem selectedItem)
                {
                    NavigateToPage(selectedItem.Tag?.ToString());
                }
            }
        }

        private async System.Threading.Tasks.Task<bool> ShowResetConfirmationDialog()
        {
            // 创建确认对话框
            var dialog = new ContentDialog
            {
                Title = "重置设置确认",
                Content = "您确定要重置所有设置到默认值吗？\n\n此操作将清除所有自定义配置，包括：\n• 外观设置\n• 手势设置\n• 书写设置\n• PowerPoint设置\n• 存储设置\n• 其他所有配置\n\n此操作不可撤销！",
                PrimaryButtonText = "确认重置",
                SecondaryButtonText = "取消",
                DefaultButton = ContentDialogButton.Secondary
            };

            // 设置对话框样式
            dialog.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 26, 26));
            dialog.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(250, 250, 250));

            // 显示对话框并等待结果
            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary;
        }

        /// <summary>
        /// 导航选择变更事件处理
        /// </summary>
        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem selectedItem)
            {
                var tag = selectedItem.Tag?.ToString();
                
                // 处理特殊操作
                switch (tag)
                {
                    case "Restart":
                        HandleRestart();
                        // 重置选择到之前的项
                        NavView.SelectedItem = NavView.MenuItems.Count > 0 ? NavView.MenuItems[0] : null;
                        break;
                    case "Reset":
                        _ = HandleResetAsync();
                        // 重置选择到之前的项
                        NavView.SelectedItem = NavView.MenuItems.Count > 0 ? NavView.MenuItems[0] : null;
                        break;
                    case "Exit":
                        HandleExit();
                        break;
                    default:
                        NavigateToPage(tag);
                        break;
                }
            }
        }

        private void HandleRestart()
        {
            try
            {
                // 重启应用
                Process.Start(Process.GetCurrentProcess().MainModule.FileName);
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"重启失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task HandleResetAsync()
        {
            // 显示确认对话框
            var result = await ShowResetConfirmationDialog();
            if (result == true)
            {
                try
                {
                    // 重置设置
                    if (ViewModel != null)
                    {
                        ViewModel.ResetSettings();
                    }
                    
                    System.Windows.MessageBox.Show("设置已重置为默认值", "重置完成", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"重置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void HandleExit()
        {
            // 退出应用
            Application.Current.Shutdown();
        }

        /// <summary>
        /// 根据标签导航到对应页面
        /// </summary>
        /// <param name="pageTag">页面标签</param>
        private void NavigateToPage(string pageTag)
        {
            if (string.IsNullOrEmpty(pageTag)) return;

            UserControl page = null;

            // 检查缓存中是否已有页面
            if (_pages.ContainsKey(pageTag))
            {
                page = _pages[pageTag];
            }
            else
            {
                // 创建新页面
                switch (pageTag)
                {
                    case "QuickSettings":
                        page = new QuickSettingsPage();
                        break;
                    case "Appearance":
                        page = new AppearanceSettingsPage();
                        break;
                    case "Gesture":
                        page = new GestureSettingsPage();
                        break;
                    case "Writing":
                        page = new WritingSettingsPage();
                        break;
                    case "Whiteboard":
                        page = new WhiteboardSettingsPage();
                        break;
                    case "PowerPoint":
                        page = new PowerPointSettingsPage();
                        break;
                    case "Storage":
                        page = new StorageSettingsPage();
                        break;
                    case "Advanced":
                        page = new AdvancedSettingsPage();
                        break;
                    case "Automation":
                        page = new AutomationSettingsPage();
                        break;
                    case "Snapshot":
                        page = new SnapshotSettingsPage();
                        break;
                    case "RandomPick":
                        page = new RandomPickSettingsPage();
                        break;
                    case "About":
                        page = new AboutSettingsPage();
                        break;
                    default:
                        page = new QuickSettingsPage();
                        break;
                }

                // 设置页面的 DataContext 为 SettingsViewModel
                if (page != null)
                {
                    page.DataContext = ViewModel;
                    _pages[pageTag] = page;
                }
            }

            // 导航到页面
            if (page != null)
            {
                ContentFrame.Navigate(page);
                ContentScrollViewer?.ScrollToTop();
            }
        }
    }
}
