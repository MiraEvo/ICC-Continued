using Hardcodet.Wpf.TaskbarNotification;
using Ink_Canvas.Core;
using Ink_Canvas.Helpers;
using Ink_Canvas.Services;
using Ink_Canvas.ViewModels;
using iNKORE.UI.WPF.Modern.Controls;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using Ookii.Dialogs.Wpf;
using System.Diagnostics;
using Lierda.WPFHelper;
using System.Windows.Shell;
using Newtonsoft.Json.Linq;

namespace Ink_Canvas
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        System.Threading.Mutex mutex;

        public static string[] StartArgs = null;
        public static string RootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data") + "\\";

        /// <summary>
        /// 依赖注入服务提供者
        /// </summary>
        public IServiceProvider Services { get; private set; }

        public App() {
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
            Startup += App_Startup;
            Exit += App_Exit;
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            
            // 初始化依赖注入
            ConfigureServices();
        }
        
        /// <summary>
        /// 应用程序退出事件处理 - 释放所有资源防止进程残留
        /// </summary>
        private void App_Exit(object sender, ExitEventArgs e) {
            try {
                LogHelper.WriteLogToFile("Application Exit: Starting cleanup", LogHelper.LogType.Event);
                
                // 释放 mutex
                if (mutex != null) {
                    try {
                        mutex.ReleaseMutex();
                        mutex.Dispose();
                        mutex = null;
                        LogHelper.WriteLogToFile("Application Exit: Mutex released", LogHelper.LogType.Info);
                    }
                    catch (Exception ex) {
                        LogHelper.WriteLogToFile("Application Exit: Error releasing mutex - " + ex.Message, LogHelper.LogType.Error);
                    }
                }
                
                // 释放托盘图标
                if (_taskbar != null) {
                    try {
                        _taskbar.Dispose();
                        _taskbar = null;
                        LogHelper.WriteLogToFile("Application Exit: TaskbarIcon disposed", LogHelper.LogType.Info);
                    }
                    catch (Exception ex) {
                        LogHelper.WriteLogToFile("Application Exit: Error disposing TaskbarIcon - " + ex.Message, LogHelper.LogType.Error);
                    }
                }
                
                LogHelper.WriteLogToFile("Application Exit: Cleanup completed, forcing exit", LogHelper.LogType.Event);
            }
            catch (Exception ex) {
                LogHelper.WriteLogToFile("Application Exit: Error during cleanup - " + ex.Message, LogHelper.LogType.Error);
            }
            finally {
                // 强制终止进程，确保不会残留
                // 使用 Environment.Exit 确保所有线程（包括前台线程）都被终止
                Environment.Exit(0);
            }
        }

        /// <summary>
        /// 配置依赖注入服务
        /// 
        /// 服务生命周期说明：
        /// - Singleton: 应用程序生命周期内只创建一次，所有请求共享同一实例
        /// - Transient: 每次请求都创建新实例（本应用暂未使用）
        /// 
        /// 服务依赖关系：
        /// - SettingsService: 无依赖
        /// - TimeMachineService: 无依赖
        /// - PageService: 依赖 ITimeMachineService（可选）
        /// - PPTService: 无依赖
        /// - HotkeyService: 无依赖
        /// - FileCleanupService: 无依赖
        /// - CodeAnalyzer: 无依赖
        /// - ResourceManagementChecker: 无依赖
        /// - SettingsViewModel: 依赖 ISettingsService
        /// - MainWindowViewModel: 依赖 ISettingsService, IPageService, ITimeMachineService
        /// - ToolbarViewModel: 依赖 ISettingsService
        /// 
        /// 注意：以下服务需要在 MainWindow 初始化后手动注册，因为它们依赖于 UI 元素：
        /// - IInkCanvasService: 依赖 IccInkCanvas 实例
        /// - INotificationService: 依赖 MainWindow 实例
        /// - IScreenshotService: 依赖 MainWindow 实例
        /// </summary>
        private void ConfigureServices()
        {
            var services = new ServiceCollection();

            // ========================================
            // 核心服务 (Singleton)
            // ========================================
            
            // 设置服务 - 管理应用程序配置
            services.AddSingleton<ISettingsService, SettingsService>();
            
            // 时光机服务 - 管理撤销/重做历史
            services.AddSingleton<ITimeMachineService, TimeMachineService>();
            
            // 页面服务 - 管理画布页面
            // 依赖: ITimeMachineService (通过构造函数注入，可选)
            services.AddSingleton<IPageService>(sp =>
            {
                var timeMachineService = sp.GetService<ITimeMachineService>();
                return new PageService(timeMachineService);
            });
            
            // PPT 服务 - 管理 PowerPoint 集成
            services.AddSingleton<IPPTService, PPTService>();
            
            // 热键服务 - 管理应用程序热键
            services.AddSingleton<IHotkeyService, HotkeyService>();
            
            // 形状绘制服务 - 管理形状绘制功能
            services.AddSingleton<IShapeDrawingService, ShapeDrawingService>();
            
            // ========================================
            // 工具服务 (Singleton)
            // ========================================
            
            // 文件清理服务 - 清理临时文件和冗余资源
            services.AddSingleton<IFileCleanupService, FileCleanupService>();
            
            // 代码分析器 - 静态代码分析
            services.AddSingleton<ICodeAnalyzer, CodeAnalyzer>();
            
            // 资源管理检查器 - 检查资源泄漏
            services.AddSingleton<IResourceManagementChecker, ResourceManagementChecker>();
            
            // ========================================
            // ViewModels (Singleton)
            // ========================================
            
            // 设置 ViewModel
            // 依赖: ISettingsService
            services.AddSingleton<SettingsViewModel>();
            
            // SettingsPageViewModel 已移除，MainWindow 直接使用 SettingsViewModel
            
            // 主窗口 ViewModel
            // 依赖: ISettingsService, IPageService, ITimeMachineService
            services.AddSingleton<MainWindowViewModel>();
            
            // 工具栏 ViewModel
            // 依赖: ISettingsService
            services.AddSingleton<ToolbarViewModel>();
            
            // 浮动工具栏 ViewModel
            // 依赖: ISettingsService, ITimeMachineService
            services.AddSingleton<FloatingBarViewModel>();

            // ========================================
            // 构建服务提供者
            // ========================================
            Services = services.BuildServiceProvider();
            
            // 设置全局服务定位器（仅用于无法使用构造函数注入的场景）
            ServiceLocator.ServiceProvider = Services;
            
            // ========================================
            // 预加载关键服务
            // ========================================
            
            // 预先加载设置服务，确保配置在应用启动时可用
            var settingsService = Services.GetRequiredService<ISettingsService>();
            settingsService.Load();
            
            LogHelper.WriteLogToFile("Dependency injection configured successfully", LogHelper.LogType.Info);
        }

        private Assembly OnAssemblyResolve(object sender, ResolveEventArgs args) {
            // 旧版 IA 库 (IAWinFX, IACore, IALoader) 已移除
            // 现在使用 Windows.UI.Input.Inking.Analysis API，不再需要处理这些程序集
            return null;
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Ink_Canvas.MainWindow.ShowNewMessage("抱歉，出现未预期的异常，可能导致 InkCanvasForClass 运行不稳定。\n建议保存墨迹后重启应用。");
            LogHelper.NewLog(e.Exception.ToString());
            e.Handled = true;
        }

        private TaskbarIcon _taskbar;
        private MainWindow mainWin = null;

        void App_Startup(object sender, StartupEventArgs e)
        {
            RootPath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Data") + "\\";
            if (!Directory.Exists(RootPath))
            {
                Directory.CreateDirectory(RootPath);
            }

            LogHelper.NewLog(string.Format("Ink Canvas Starting (Version: {0})", Assembly.GetExecutingAssembly().GetName().Version.ToString()));

            bool ret;
            mutex = new System.Threading.Mutex(true, "InkCanvasForClass", out ret);

            if (!ret && !(e.Args.Contains("-m")||e.Args.Contains("--multiple"))) //-m multiple
            {
                LogHelper.NewLog("Detected existing instance");

                if (TaskDialog.OSSupportsTaskDialogs) {
                    using (TaskDialog dialog = new())
                    {
                        dialog.WindowTitle = "InkCanvasForClass";
                        dialog.MainIcon = TaskDialogIcon.Warning;
                        dialog.MainInstruction = "已有一个实例正在运行";
                        dialog.Content = "这意味着 InkCanvasForClass 正在运行，而您又运行了主程序一遍。如果频繁出现该弹窗且ICC无法正常启动时，请尝试 “以多开模式启动”。";
                        TaskDialogButton customButton = new("以多开模式启动");
                        customButton.Default = false;
                        dialog.ButtonClicked += (object s, TaskDialogItemClickedEventArgs _e) => {
                            if (_e.Item == customButton)
                            {
                                Process.Start(System.Windows.Forms.Application.ExecutablePath, "-m");
                            }
                        };
                        TaskDialogButton okButton = new(ButtonType.Ok);
                        okButton.Default = true;
                        dialog.Buttons.Add(customButton);
                        dialog.Buttons.Add(okButton);
                        TaskDialogButton button = dialog.ShowDialog();
                    }
                }

                LogHelper.NewLog("Ink Canvas automatically closed");
                Environment.Exit(0);
            }


            var isUsingWindowChrome = false;
            try {
                if (File.Exists(App.RootPath + "Settings.json")) {
                    try {
                        string text = File.ReadAllText(App.RootPath + "Settings.json");
                        var obj = JObject.Parse(text);
                        isUsingWindowChrome = (bool)obj.SelectToken("startup.enableWindowChromeRendering");
                    }
                    catch (Exception ex) {
                        LogHelper.WriteLogToFile("Failed to parse Settings.json for WindowChrome setting: " + ex.Message, LogHelper.LogType.Error);
                    }
                }
            } catch (Exception ex) {
                LogHelper.WriteLogToFile(ex.ToString(), LogHelper.LogType.Error);
            }

            mainWin = new();

            if (isUsingWindowChrome && DwmCompositionHelper.DwmIsCompositionEnabled()) {
                mainWin.AllowsTransparency = false;
                WindowChrome wc = new();
                wc.GlassFrameThickness = new Thickness(-1);
                wc.CaptionHeight = 0;
                wc.CornerRadius = new CornerRadius(0);
                wc.ResizeBorderThickness = new Thickness(0);
                WindowChrome.SetWindowChrome(mainWin, wc);
            } else {
                mainWin.AllowsTransparency = true;
                WindowChrome.SetWindowChrome(mainWin, null);
            }
            mainWin.Show();

            _taskbar = (TaskbarIcon)FindResource("TaskbarTrayIcon");

            LierdaCracker cracker = new();
            cracker.Cracker();

            StartArgs = e.Args;
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            try
            {
                if (System.Windows.Forms.SystemInformation.MouseWheelScrollLines == -1)
                    e.Handled = false;
                else
                    try
                    {
                        ScrollViewerEx SenderScrollViewer = (ScrollViewerEx)sender;
                        SenderScrollViewer.ScrollToVerticalOffset(SenderScrollViewer.VerticalOffset - e.Delta * Constants.MouseWheelScrollMultiplier * System.Windows.Forms.SystemInformation.MouseWheelScrollLines / Constants.MouseWheelDeltaStandard);
                        e.Handled = true;
                    }
                    catch (Exception ex) {
                        LogHelper.WriteLogToFile("ScrollViewer offset calculation failed: " + ex.Message, LogHelper.LogType.Trace);
                    }
            }
            catch (Exception ex) {
                LogHelper.WriteLogToFile("ScrollViewer mouse wheel event failed: " + ex.Message, LogHelper.LogType.Trace);
            }
        }
    }
}
