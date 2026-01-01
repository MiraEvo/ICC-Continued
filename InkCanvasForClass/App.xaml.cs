using Hardcodet.Wpf.TaskbarNotification;
using Ink_Canvas.Helpers;
using iNKORE.UI.WPF.Modern.Controls;
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

        public App() {
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
            Startup += App_Startup;
            DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        private Assembly OnAssemblyResolve(object sender, ResolveEventArgs args) {
            if (args.Name.Contains("IAWinFX") || args.Name.Contains("IACore") || args.Name.Contains("IALoader")) {
                try {
                    string assemblyName = new AssemblyName(args.Name).Name + ".dll";
                    string assemblyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, assemblyName);
                    if (File.Exists(assemblyPath)) {
                        return Assembly.LoadFrom(assemblyPath);
                    }
                }
                catch (Exception ex) {
                    LogHelper.WriteLogToFile("AssemblyResolve failed for " + args.Name + ": " + ex.Message, LogHelper.LogType.Error);
                }
            }
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
                        SenderScrollViewer.ScrollToVerticalOffset(SenderScrollViewer.VerticalOffset - e.Delta * 10 * System.Windows.Forms.SystemInformation.MouseWheelScrollLines / (double)120);
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
