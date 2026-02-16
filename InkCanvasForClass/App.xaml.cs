using Ink_Canvas.Core;
using Ink_Canvas.Helpers;
using Ink_Canvas.Services;
using Ink_Canvas.ViewModels;
using Ink_Canvas.ViewModels.Settings;
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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sentry;
using System.Runtime.CompilerServices;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Ink_Canvas
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static System.Threading.Mutex _mutex;

        public static string[] StartArgs { get; private set; }
        public static string RootPath { get; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data") + "\\";

        /// <summary>
        /// 依赖注入服务提供者
        /// </summary>
        public IServiceProvider Services { get; private set; }

        public App() {
            // 尽早初始化日志服务
            InitializeLogging();

            // 尽早初始化 Sentry，以便捕获所有异常
            InitializeSentry();

            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Startup += App_Startup;
            Exit += App_Exit;
            DispatcherUnhandledException += App_DispatcherUnhandledException;

            // 初始化依赖注入
            ConfigureServices();
        }

        /// <summary>
        /// 初始化日志服务
        /// </summary>
        private static void InitializeLogging()
        {
            try
            {
                // 仅在 Debug 构建时启用控制台输出
#if DEBUG
                bool enableConsole = true;
#else
                bool enableConsole = false;
#endif

                // 初始化新的日志服务
                LogHelper.Initialize(
                    sentryDsn: null, // Sentry 通过 SentryHelper 单独管理
                    enableConsole: enableConsole,
                    enableFile: true
                );

                LogHelper.NewLog("日志服务初始化完成");
            }
            catch (Exception ex)
            {
                // 如果初始化失败，回退到旧版日志
                LogHelper.UseModernLogger = false;
                LogHelper.NewLog($"现代化日志服务初始化失败，使用回退模式: {ex.Message}");
            }
        }

        /// <summary>
        /// 初始化 Sentry SDK
        /// </summary>
        private static void InitializeSentry()
        {
            // 使用 SentryHelper 进行初始化，包含更多功能
            SentryHelper.Initialize(enablePerformanceMonitoring: true, tracesSampleRate: 1.0);

            // 设置匿名用户标识
            SentryHelper.SetAnonymousUser();

            // 添加应用启动面包屑
            SentryHelper.AddBreadcrumb(
                message: "Application starting",
                category: SentryHelper.BreadcrumbCategory.System,
                level: BreadcrumbLevel.Info
            );
        }

        /// <summary>
        /// 处理 AppDomain 未处理异常（非 UI 线程异常）
        /// </summary>
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception exception)
            {
                // 使用 SentryHelper 捕获异常，包含更多上下文
                SentryHelper.CaptureException(exception,
                    tags: new System.Collections.Generic.Dictionary<string, string>
                    {
                        { "exception_type", "unhandled" },
                        { "is_terminating", e.IsTerminating.ToString() }
                    });

                LogHelper.NewLog($"[AppDomain UnhandledException] {exception}");

                // 如果是终止性异常，确保 Sentry 有时间发送
                if (e.IsTerminating)
                {
                    SentryHelper.Flush(2000);
                }
            }
        }

        /// <summary>
        /// 应用程序退出事件处理 - 释放所有资源防止进程残留
        /// </summary>
        private void App_Exit(object sender, ExitEventArgs e) {
            try {
                LogHelper.WriteLogToFile("应用退出：开始清理", LogHelper.LogType.Event);

                // 添加应用退出面包屑
                SentryHelper.AddBreadcrumb(
                    message: "Application exiting",
                    category: SentryHelper.BreadcrumbCategory.System,
                    level: BreadcrumbLevel.Info
                );

                // 释放 mutex
                if (_mutex != null) {
                    try {
                        _mutex.ReleaseMutex();
                        _mutex.Dispose();
                        _mutex = null;
                        LogHelper.WriteLogToFile("应用退出：互斥锁已释放", LogHelper.LogType.Info);
                    }
                    catch (ObjectDisposedException ex) {
                        LogHelper.WriteLogToFile("应用退出：释放互斥锁失败（对象已释放） - " + ex.Message, LogHelper.LogType.Error);
                    }
                    catch (InvalidOperationException ex) {
                        LogHelper.WriteLogToFile("应用退出：释放互斥锁失败（无效操作） - " + ex.Message, LogHelper.LogType.Error);
                    }
                    catch (UnauthorizedAccessException ex) {
                        LogHelper.WriteLogToFile("应用退出：释放互斥锁失败（访问被拒绝） - " + ex.Message, LogHelper.LogType.Error);
                    }
                }

                if (Application.Current.MainWindow is MainWindow mainWindow)
                {
                    mainWindow.DisposeTrayIcon();
                }

                LogHelper.WriteLogToFile("应用退出：清理完成，准备强制退出", LogHelper.LogType.Event);
            }
            catch (IOException ex) {
                LogHelper.WriteLogToFile("应用退出：清理过程发生IO错误 - " + ex.Message, LogHelper.LogType.Error);
            }
            catch (UnauthorizedAccessException ex) {
                LogHelper.WriteLogToFile("应用退出：清理过程发生访问权限错误 - " + ex.Message, LogHelper.LogType.Error);
            }
            catch (InvalidOperationException ex) {
                LogHelper.WriteLogToFile("应用退出：清理过程发生无效操作错误 - " + ex.Message, LogHelper.LogType.Error);
            }
            finally {
                // 关闭 Sentry，使用更短的超时避免卡住
                try {
                    SentryHelper.Close(500);
                }
                catch (System.TimeoutException ex) {
                    LogHelper.WriteLogToFile("应用退出：关闭 Sentry 失败（超时） - " + ex.Message, LogHelper.LogType.Warning);
                }
                catch (ObjectDisposedException ex) {
                    LogHelper.WriteLogToFile("应用退出：关闭 Sentry 失败（对象已释放） - " + ex.Message, LogHelper.LogType.Warning);
                }

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

            // 墨迹画布服务 - 管理墨迹画布相关功能
            services.AddSingleton<IInkCanvasService, InkCanvasService>();

            // 截图服务 - 管理截图功能
            services.AddSingleton<IScreenshotService, ScreenshotService>();

            // 通知服务 - 管理通知功能
            services.AddSingleton<INotificationService, NotificationService>();

            // ========================================
            // ViewModels (Singleton)
            // ========================================

            // 设置 ViewModel
            // 依赖: ISettingsService
            services.AddSingleton<SettingsViewModel>();
            services.AddSingleton<AboutSettingsViewModel>();

            // 外观设置 ViewModel
            // 依赖: ISettingsService
            services.AddSingleton<AppearanceSettingsViewModel>(sp =>
            {
                var settingsService = sp.GetRequiredService<ISettingsService>();
                return new AppearanceSettingsViewModel(settingsService.Appearance, () => settingsService.Save());
            });

            // 画布设置 ViewModel
            // 依赖: ISettingsService
            services.AddSingleton<CanvasSettingsViewModel>();

            // 存储设置 ViewModel
            // 依赖: ISettingsService
            services.AddSingleton<ViewModels.Settings.StorageSettingsViewModel>();

            // 手势设置 ViewModel
            services.AddSingleton<ViewModels.GestureSettingsViewModel>();

            // 随机选择设置 ViewModel
            services.AddSingleton<ViewModels.Settings.RandomPickSettingsViewModel>();

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

            // 黑板/白板 ViewModel
            // 依赖: ISettingsService, ITimeMachineService
            services.AddSingleton<BlackboardViewModel>();

            // 触摸事件 ViewModel
            // 依赖: ISettingsService, ITimeMachineService
            services.AddSingleton<TouchEventsViewModel>();

            // ========================================
            // 构建服务提供者
            // ========================================
            Services = services.BuildServiceProvider();

            // 设置全局服务定位器（仅用于无法使用构造函数注入的场景）
            ServiceLocator.ServiceProvider = Services;

            // 注意: 不在此处预加载设置服务
            // 设置加载已在 MainWindow.LoadSettings() 中处理
            // 这样可以确保 App.RootPath 在 App_Startup 中被正确设置后再加载设置

            LogHelper.WriteLogToFile("依赖注入配置完成", LogHelper.LogType.Info);
        }

        private Assembly? OnAssemblyResolve(object sender, ResolveEventArgs args) {
            // 旧版 IA 库 (IAWinFX, IACore, IALoader) 已移除
            // 现在使用 Windows.UI.Input.Inking.Analysis API，不再需要处理这些程序集
            return null;
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            // 使用 SentryHelper 捕获异常，包含更多上下文
            SentryHelper.CaptureException(e.Exception,
                tags: new System.Collections.Generic.Dictionary<string, string>
                {
                    { "exception_type", "dispatcher_unhandled" },
                    { "thread", "ui" }
                });

            Ink_Canvas.MainWindow.ShowNewMessage("抱歉，出现未预期的异常，可能导致 InkCanvasForClass 运行不稳定。\n建议保存墨迹后重启应用。");
            LogHelper.NewLog(e.Exception.ToString());
            e.Handled = true;
        }

        private MainWindow mainWin = null;

        [RequiresUnmanagedCode("Uses DWM APIs for WindowChrome configuration.")]
        void App_Startup(object sender, StartupEventArgs e)
        {
            if (!Directory.Exists(RootPath))
            {
                Directory.CreateDirectory(RootPath);
            }

            // 开始应用启动事务
            SentryHelper.StartTransaction("app.startup", "application");
            SentryHelper.StartSpan("initialization", "Initializing application");

            LogHelper.NewLog(string.Format("Ink Canvas Starting (Version: {0})", Assembly.GetExecutingAssembly().GetName().Version.ToString()));

            _mutex = new System.Threading.Mutex(true, "InkCanvasForClass", out bool ret);

            if (!ret && !(e.Args.Contains("-m")||e.Args.Contains("--multiple"))) //-m multiple
            {
                LogHelper.NewLog("检测到已有实例");

                if (TaskDialog.OSSupportsTaskDialogs) {
                    using (TaskDialog dialog = new())
                    {
                        dialog.WindowTitle = "InkCanvasForClass";
                        dialog.MainIcon = TaskDialogIcon.Warning;
                        dialog.MainInstruction = "已有一个实例正在运行";
                        dialog.Content = "这意味着 InkCanvasForClass 正在运行，而您又运行了主程序一遍。如果频繁出现该弹窗且ICC无法正常启动时，请尝试 “以多开模式启动”。";
                        TaskDialogButton customButton = new("以多开模式启动")
                        {
                            Default = false
                        };
                        dialog.ButtonClicked += (s, _e) => {
                            if (_e.Item == customButton)
                            {
                                Process.Start(System.Windows.Forms.Application.ExecutablePath, "-m");
                            }
                        };
                        dialog.Buttons.Add(customButton);
                        dialog.Buttons.Add(new TaskDialogButton(ButtonType.Ok) { Default = true });
                        dialog.ShowDialog();
                    }
                }

                LogHelper.NewLog("Ink Canvas 已自动关闭");
                Environment.Exit(0);
            }


            var isUsingWindowChrome = false;
            try {
                // 优先尝试读取 YAML 格式，如果不存在则尝试 JSON
                string yamlPath = Path.Combine(App.RootPath, "Settings.yml");
                string jsonPath = Path.Combine(App.RootPath, "Settings.json");

                if (File.Exists(yamlPath)) {
                    try {
                        string yaml = File.ReadAllText(yamlPath);
                        var deserializer = new DeserializerBuilder()
                            .WithNamingConvention(CamelCaseNamingConvention.Instance)
                            .IgnoreUnmatchedProperties()
                            .Build();
                        dynamic obj = deserializer.Deserialize<dynamic>(yaml);
                        if (obj != null && obj.ContainsKey("startup"))
                        {
                            var startup = obj["startup"] as Dictionary<object, object>;
                            if (startup != null && startup.ContainsKey("enableWindowChromeRendering"))
                            {
                                isUsingWindowChrome = Convert.ToBoolean(startup["enableWindowChromeRendering"]);
                            }
                        }
                    }
                    catch (Exception ex) {
                        LogHelper.WriteLogToFile("解析 Settings.yml 的 WindowChrome 配置失败:" + ex.Message, LogHelper.LogType.Error);
                    }
                }
                else if (File.Exists(jsonPath)) {
                    try {
                        string text = File.ReadAllText(jsonPath);
                        var obj = JObject.Parse(text);
                        isUsingWindowChrome = (bool)obj.SelectToken("startup.enableWindowChromeRendering");
                    }
                    catch (JsonReaderException ex) {
                        LogHelper.WriteLogToFile("解析 Settings.json 的 WindowChrome 配置失败（JSON格式错误）:" + ex.Message, LogHelper.LogType.Error);
                    }
                    catch (FileNotFoundException ex) {
                        LogHelper.WriteLogToFile("解析 Settings.json 的 WindowChrome 配置失败（文件未找到）:" + ex.Message, LogHelper.LogType.Error);
                    }
                    catch (UnauthorizedAccessException ex) {
                        LogHelper.WriteLogToFile("读取 Settings.json 失败（访问被拒绝）:" + ex.Message, LogHelper.LogType.Error);
                    }
                    catch (IOException ex) {
                        LogHelper.WriteLogToFile("读取 Settings.json 失败（IO错误）:" + ex.Message, LogHelper.LogType.Error);
                    }
                }
            } catch (System.Security.SecurityException ex) {
                LogHelper.WriteLogToFile("读取设置文件失败（安全权限错误）:" + ex.Message, LogHelper.LogType.Error);
            }

            mainWin = new();

            // MainWindow is a desktop-annotation overlay and must stay transparent.
            // Enabling WindowChrome here forces AllowsTransparency=false and breaks the transparent background.
            mainWin.AllowsTransparency = true;
            WindowChrome.SetWindowChrome(mainWin, null);
            mainWin.Show();

            LierdaCracker cracker = new();
            cracker.Cracker();

            StartArgs = e.Args;

            // 结束启动 Span
            SentryHelper.EndSpan();

            // 添加启动完成面包屑
            SentryHelper.AddBreadcrumb(
                message: "Application startup completed",
                category: SentryHelper.BreadcrumbCategory.System,
                level: BreadcrumbLevel.Info,
                data: new System.Collections.Generic.Dictionary<string, string>
                {
                    { "args_count", e.Args.Length.ToString() },
                    { "version", Assembly.GetExecutingAssembly().GetName().Version.ToString() }
                }
            );

            // 结束启动事务
            SentryHelper.EndTransaction();
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
                        var senderScrollViewer = (System.Windows.Controls.ScrollViewer)sender;
                        senderScrollViewer.ScrollToVerticalOffset(senderScrollViewer.VerticalOffset - e.Delta * Constants.MouseWheelScrollMultiplier * System.Windows.Forms.SystemInformation.MouseWheelScrollLines / Constants.MouseWheelDeltaStandard);
                        e.Handled = true;
                    }
                    catch (InvalidOperationException ex) {
                        LogHelper.WriteLogToFile("滚轮偏移计算失败（无效操作）：" + ex.Message, LogHelper.LogType.Trace);
                    }
                    catch (OverflowException ex) {
                        LogHelper.WriteLogToFile("滚轮偏移计算失败（数值溢出）：" + ex.Message, LogHelper.LogType.Trace);
                    }
            }
            catch (InvalidOperationException ex) {
                LogHelper.WriteLogToFile("滚轮事件处理失败（无效操作）：" + ex.Message, LogHelper.LogType.Trace);
            }
            catch (OverflowException ex) {
                LogHelper.WriteLogToFile("滚轮事件处理失败（数值溢出）：" + ex.Message, LogHelper.LogType.Trace);
            }
        }
    }
}
