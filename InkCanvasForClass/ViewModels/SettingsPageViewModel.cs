using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ink_Canvas.Core;
using Ink_Canvas.Services;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.IO;
using System.Reflection;
using Application = System.Windows.Application;

namespace Ink_Canvas.ViewModels
{
    /// <summary>
    /// SettingsView 页面级 ViewModel
    /// 负责页面级的 UI 逻辑和命令
    /// </summary>
    public partial class SettingsPageViewModel : ViewModelBase
    {
        private readonly ISettingsService _settingsService;
        private readonly MainWindow _mainWindow;

        #region Properties

        /// <summary>
        /// 主设置 ViewModel（包含所有子设置 ViewModel）
        /// </summary>
        public SettingsViewModel MainSettingsVM { get; }

        /// <summary>
        /// 滚动偏移量
        /// </summary>
        [ObservableProperty]
        private double _scrollOffset;

        /// <summary>
        /// 滚动最大高度
        /// </summary>
        [ObservableProperty]
        private double _scrollMaxHeight;

        /// <summary>
        /// 滚动实际高度
        /// </summary>
        [ObservableProperty]
        private double _scrollActualHeight;

        #endregion

        #region Commands

        /// <summary>
        /// 重启应用命令
        /// </summary>
        [RelayCommand]
        private void RestartApp()
        {
            try
            {
                var file = new FileInfo(Assembly.GetExecutingAssembly().Location);
                var exe = Path.Combine(file.DirectoryName, file.Name.Replace(file.Extension, "") + ".exe");

                var proc = new Process
                {
                    StartInfo = {
                        FileName = exe,
                        UseShellExecute = true
                    }
                };
                proc.Start();

                // 关闭当前实例
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                Helpers.LogHelper.WriteLogToFile($"Failed to restart app: {ex.Message}", Helpers.LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 重置设置为推荐值命令
        /// </summary>
        [RelayCommand]
        private void ResetToSuggestion()
        {
            try
            {
                MainSettingsVM.ResetSettingsCommand.Execute(null);
                // 通知 UI 更新
                OnPropertyChanged(string.Empty);
            }
            catch (Exception ex)
            {
                Helpers.LogHelper.WriteLogToFile($"Failed to reset settings: {ex.Message}", Helpers.LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 退出应用命令
        /// </summary>
        [RelayCommand]
        private void ExitApp()
        {
            Application.Current.Shutdown();
        }

        /// <summary>
        /// 以管理员身份重启命令
        /// </summary>
        [RelayCommand]
        private void RunAsAdmin()
        {
            try
            {
                var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                if (!principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator))
                {
                    var file = new FileInfo(Assembly.GetExecutingAssembly().Location);
                    var exe = Path.Combine(file.DirectoryName, file.Name.Replace(file.Extension, "") + ".exe");

                    var proc = new Process
                    {
                        StartInfo = {
                            FileName = exe,
                            Verb = "runas",
                            UseShellExecute = true,
                            Arguments = "-m"
                        }
                    };
                    proc.Start();

                    Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                Helpers.LogHelper.WriteLogToFile($"Failed to run as admin: {ex.Message}", Helpers.LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 以普通用户身份重启命令
        /// </summary>
        [RelayCommand]
        private void RunAsUser()
        {
            try
            {
                var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                if (principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator))
                {
                    Process.Start("explorer.exe", Assembly.GetEntryAssembly().Location);

                    Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                Helpers.LogHelper.WriteLogToFile($"Failed to run as user: {ex.Message}", Helpers.LogHelper.LogType.Error);
            }
        }

        #endregion

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="settingsService">设置服务</param>
        /// <param name="settingsViewModel">设置 ViewModel</param>
        /// <param name="mainWindow">主窗口实例</param>
        public SettingsPageViewModel(
            ISettingsService settingsService, 
            SettingsViewModel settingsViewModel,
            MainWindow mainWindow = null)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            MainSettingsVM = settingsViewModel ?? throw new ArgumentNullException(nameof(settingsViewModel));
            _mainWindow = mainWindow;
        }

        #region Scroll Methods

        /// <summary>
        /// 更新滚动状态
        /// </summary>
        public void UpdateScrollStatus(double offset, double maxHeight, double actualHeight)
        {
            ScrollOffset = offset;
            ScrollMaxHeight = maxHeight;
            ScrollActualHeight = actualHeight;
        }

        /// <summary>
        /// 滚动到指定位置（带动画）
        /// </summary>
        public void ScrollToAnimated(double offset, int animateMs = 155)
        {
            // 这里可以实现滚动动画逻辑
            // 目前只是更新属性，实际滚动由 View 处理
            ScrollOffset = offset;
        }

        #endregion

        #region Navigation Methods

        /// <summary>
        /// 更新设置索引侧边栏显示状态
        /// </summary>
        public void UpdateSettingsIndexSidebarDisplayStatus()
        {
            // 这里可以实现侧边栏导航逻辑
            // 实际实现会根据 UI 元素位置来更新导航状态
        }

        /// <summary>
        /// 跳转到指定设置组
        /// </summary>
        public void JumpToSettingsGroup(int index)
        {
            // 实现跳转到指定设置组的逻辑
        }

        #endregion

        /// <summary>
        /// 清理资源
        /// </summary>
        public override void Cleanup()
        {
            MainSettingsVM?.Cleanup();
            base.Cleanup();
        }
    }
}
