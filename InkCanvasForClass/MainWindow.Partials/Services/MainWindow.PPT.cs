// ============================================================================
// MW_PPT.cs - PowerPoint 集成逻辑
// ============================================================================
//
// 功能说明:
//   - PowerPoint 演示文稿检测和监控
//   - PPT 翻页控制
//   - PPT 模式下的 UI 状态管理
//   - PPT 墨迹保存和恢复
//
// 迁移状态 (渐进式迁移):
//   - PPTService 已创建，提供 PPT 相关服务接口
//   - PPTNavigationView UserControl 已创建
//   - 此文件中的核心逻辑仍在使用
//
// 相关文件:
//   - Services/PPTService.cs
//   - Services/IPPTService.cs
//   - Views/PPT/PPTNavigationView.xaml
//   - MainWindow.xaml (PPT 导航按钮区域)
//
// ============================================================================

using Ink_Canvas.Helpers;
using Ink_Canvas.Dialogs;
using Microsoft.Office.Interop.PowerPoint;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Application = System.Windows.Application;
using File = System.IO.File;
using MessageBox = System.Windows.MessageBox;
using iNKORE.UI.WPF.Modern;
using Microsoft.Office.Core;

using System.Collections.Concurrent;

namespace Ink_Canvas {
    public partial class MainWindow {
        public static Microsoft.Office.Interop.PowerPoint.Application? pptApplication = null;
        public static Presentation? presentation = null;
        public static Slides? slides = null;
        public static Slide? slide = null;
        public static int slidescount = 0;

        // PPT联动优化：缓存的画刷对象，避免重复创建
        private static readonly SolidColorBrush _darkBgBrush = new SolidColorBrush(Color.FromRgb(39, 39, 42));
        private static readonly SolidColorBrush _darkBorderBrush = new SolidColorBrush(Color.FromRgb(82, 82, 91));
        private static readonly SolidColorBrush _lightBgBrush = new SolidColorBrush(Color.FromRgb(244, 244, 245));
        private static readonly SolidColorBrush _lightBorderBrush = new SolidColorBrush(Color.FromRgb(161, 161, 170));
        private static readonly SolidColorBrush _whiteBrush = new SolidColorBrush(Colors.White);
        private static readonly SolidColorBrush _darkTextBrush = new SolidColorBrush(Color.FromRgb(24, 24, 27));
        private static readonly SolidColorBrush _darkGeometryBrush = new SolidColorBrush(Color.FromRgb(39, 39, 42));

        // PPT联动优化：使用线程池代替每次new Thread
        private static readonly object _pptOperationLock = new object();

        static MainWindow() {
            // 冻结画刷以提高性能
            _darkBgBrush.Freeze();
            _darkBorderBrush.Freeze();
            _lightBgBrush.Freeze();
            _lightBorderBrush.Freeze();
            _whiteBrush.Freeze();
            _darkTextBrush.Freeze();
            _darkGeometryBrush.Freeze();
        }

        #pragma warning disable CA1420
        [DllImport("ole32.dll")]
        private static extern void CLSIDFromProgIDEx([MarshalAs(UnmanagedType.LPWStr)] string lpszProgID, out Guid lpclsid);

        [DllImport("oleaut32.dll", PreserveSig = false)]
        private static extern void GetActiveObject(ref Guid rclsid, IntPtr pvReserved, [MarshalAs(UnmanagedType.IUnknown)] out object ppunk);

        [RequiresUnmanagedCode("Uses ole32/oleaut32 COM interop to fetch running PowerPoint instance.")]
        public static object GetActiveObject(string progId) {
            Guid clsid;
            CLSIDFromProgIDEx(progId, out clsid);
            GetActiveObject(ref clsid, IntPtr.Zero, out object obj);
            return obj;
        }
        #pragma warning restore CA1420

        private void BtnCheckPPT_Click(object sender, RoutedEventArgs e) {
            try {
                pptApplication =
                    (Microsoft.Office.Interop.PowerPoint.Application)GetActiveObject("kwpp.Application");
                //pptApplication.SlideShowWindows[1].View.Next();
                if (pptApplication != null) {
                    //获得演示文稿对象
                    presentation = pptApplication.ActivePresentation;
                    pptApplication.SlideShowBegin += PptApplication_SlideShowBegin;
                    pptApplication.SlideShowNextSlide += PptApplication_SlideShowNextSlide;
                    pptApplication.SlideShowEnd += PptApplication_SlideShowEnd;
                    // 获得幻灯片对象集合
                    slides = presentation.Slides;
                    // 获得幻灯片的数量
                    slidescount = slides.Count;
                    memoryStreams = new MemoryStream[slidescount + 2];
                    // 获得当前选中的幻灯片
                    try {
                        // 在普通视图下这种方式可以获得当前选中的幻灯片对象
                        // 然而在阅读模式下，这种方式会出现异常
                        slide = slides[pptApplication.ActiveWindow.Selection.SlideRange.SlideNumber];
                    }
                    catch (Exception ex) {
                        // 在阅读模式下出现异常时，通过下面的方式来获得当前选中的幻灯片对象
                        LogHelper.WriteLogToFile("普通视图获取幻灯片失败：" + ex.Message, LogHelper.LogType.Trace);
                        slide = pptApplication.SlideShowWindows[1].View.Slide;
                    }
                }

                if (pptApplication == null) throw new Exception();
                //BtnCheckPPT.Visibility = Visibility.Collapsed;
                BorderFloatingBarExitPPTBtn.Visibility = Visibility.Visible;
            }
            catch (COMException ex) {
                LogHelper.WriteLogToFile("检查 PPT 时发生 COM 错误：" + ex.Message, LogHelper.LogType.Warning);
                BorderFloatingBarExitPPTBtn.Visibility = Visibility.Collapsed;
                LeftBottomPanelForPPTNavigation.Visibility = Visibility.Collapsed;
                RightBottomPanelForPPTNavigation.Visibility = Visibility.Collapsed;
                LeftSidePanelForPPTNavigation.Visibility = Visibility.Collapsed;
                RightSidePanelForPPTNavigation.Visibility = Visibility.Collapsed;
                MessageBox.Show("未找到幻灯片");
            }
            catch (Exception ex) {
                LogHelper.WriteLogToFile("检查 PPT 失败：" + ex.Message, LogHelper.LogType.Warning);
                //BtnCheckPPT.Visibility = Visibility.Visible;
                BorderFloatingBarExitPPTBtn.Visibility = Visibility.Collapsed;
                LeftBottomPanelForPPTNavigation.Visibility = Visibility.Collapsed;
                RightBottomPanelForPPTNavigation.Visibility = Visibility.Collapsed;
                LeftSidePanelForPPTNavigation.Visibility = Visibility.Collapsed;
                RightSidePanelForPPTNavigation.Visibility = Visibility.Collapsed;
            }
        }

        public static bool IsShowingRestoreHiddenSlidesWindow = false;
        private static bool IsShowingAutoplaySlidesWindow = false;


        private const int MkEUnavailable = unchecked((int)0x800401E3);

        private void TimerCheckPPT_Elapsed(object? sender, ElapsedEventArgs e) {
            if (IsShowingRestoreHiddenSlidesWindow || IsShowingAutoplaySlidesWindow) return;
            try {
                var isPowerPointRunning = Process.GetProcessesByName("POWERPNT").Length > 0;
                var isWpsRunning = Process.GetProcessesByName("wpp").Length > 0;
                if (!isPowerPointRunning && !isWpsRunning) return;

                //使用下方提前创建 PowerPoint 实例，将导致 PowerPoint 不再有启动界面
                //pptApplication = (Microsoft.Office.Interop.PowerPoint.Application)Activator.CreateInstance(Marshal.GetTypeFromCLSID(new Guid("91493441-5A91-11CF-8700-00AA0060263B")));
                //new ComAwareEventInfo(typeof(EApplication_Event), "SlideShowBegin").AddEventHandler(pptApplication, new EApplication_SlideShowBeginEventHandler(this.PptApplication_SlideShowBegin));
                //new ComAwareEventInfo(typeof(EApplication_Event), "SlideShowEnd").AddEventHandler(pptApplication, new EApplication_SlideShowEndEventHandler(this.PptApplication_SlideShowEnd));
                //new ComAwareEventInfo(typeof(EApplication_Event), "SlideShowNextSlide").AddEventHandler(pptApplication, new EApplication_SlideShowNextSlideEventHandler(this.PptApplication_SlideShowNextSlide));
                //ConfigHelper.Instance.IsInitApplicationSuccessful = true;

                pptApplication =
                    (Microsoft.Office.Interop.PowerPoint.Application)GetActiveObject("PowerPoint.Application");

                if (pptApplication != null) {
                    timerCheckPPT.Stop();
                    //获得演示文稿对象
                    presentation = pptApplication.ActivePresentation;

                    // 获得幻灯片对象集合
                    slides = presentation.Slides;

                    // 获得幻灯片的数量
                    slidescount = slides.Count;
                    memoryStreams = new MemoryStream[slidescount + 2];
                    // 获得当前选中的幻灯片
                    try {
                        // 在普通视图下这种方式可以获得当前选中的幻灯片对象
                        // 然而在阅读模式下，这种方式会出现异常
                        slide = slides[pptApplication.ActiveWindow.Selection.SlideRange.SlideNumber];
                    }
                    catch (Exception ex) {
                        // 在阅读模式下出现异常时，通过下面的方式来获得当前选中的幻灯片对象
                        LogHelper.WriteLogToFile("普通视图获取幻灯片失败：" + ex.Message, LogHelper.LogType.Trace);
                        slide = pptApplication.SlideShowWindows[1].View.Slide;
                    }

                    pptApplication.PresentationOpen += PptApplication_PresentationOpen;
                    pptApplication.PresentationClose += PptApplication_PresentationClose;
                    pptApplication.SlideShowBegin += PptApplication_SlideShowBegin;
                    pptApplication.SlideShowNextSlide += PptApplication_SlideShowNextSlide;
                    pptApplication.SlideShowEnd += PptApplication_SlideShowEnd;
                }

                if (pptApplication == null) return;
                //BtnCheckPPT.Visibility = Visibility.Collapsed;

                // 此处是已经开启了
                PptApplication_PresentationOpen(null);

                //如果检测到已经开始放映，则立即进入画板模式
                if (pptApplication.SlideShowWindows.Count >= 1)
                    PptApplication_SlideShowBegin(pptApplication.SlideShowWindows[1]);
            }
            catch (COMException ex) {
                if (ex.HResult == MkEUnavailable) {
                    LogHelper.WriteLogToFile("轮询 PPT 时 COM 对象暂不可用，将在下次轮询重试。", LogHelper.LogType.Trace);
                } else {
                    LogHelper.WriteLogToFile("轮询 PPT 时发生 COM 错误：" + ex.Message, LogHelper.LogType.Warning);
                }
                Application.Current.Dispatcher.Invoke(() => { BorderFloatingBarExitPPTBtn.Visibility = Visibility.Collapsed; });
                timerCheckPPT.Start();
            }
            catch (Exception ex) {
                LogHelper.WriteLogToFile("轮询 PPT 失败：" + ex.Message, LogHelper.LogType.Warning);
                //StackPanelPPTControls.Visibility = Visibility.Collapsed;
                Application.Current.Dispatcher.Invoke(() => { BorderFloatingBarExitPPTBtn.Visibility = Visibility.Collapsed; });
                timerCheckPPT.Start();
            }
        }

        private void PptApplication_PresentationOpen(Presentation Pres) {
            // 跳转到上次播放页
            if (Settings.PowerPointSettings.IsNotifyPreviousPage)
                Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                    var folderPath = Settings.Automation.AutoSavedStrokesLocation +
                                     @"\Auto Saved - Presentations\" + presentation.Name + "_" +
                                     presentation.Slides.Count;
                    try {
                        if (!File.Exists(folderPath + "/Position")) return;
                        if (!int.TryParse(File.ReadAllText(folderPath + "/Position"), out var page)) return;
                        if (page <= 0) return;
                        new YesOrNoNotificationWindow($"上次播放到了第 {page} 页, 是否立即跳转", () => {
                            if (pptApplication.SlideShowWindows.Count >= 1)
                                // 如果已经播放了的话, 跳转
                                presentation.SlideShowWindow.View.GotoSlide(page);
                            else
                                presentation.Windows[1].View.GotoSlide(page);
                        }).ShowDialog();
                    }
                    catch (Exception ex) {
                        LogHelper.WriteLogToFile(ex.ToString(), LogHelper.LogType.Error);
                    }
                }), DispatcherPriority.Normal);


            //检查是否有隐藏幻灯片
            if (Settings.PowerPointSettings.IsNotifyHiddenPage) {
                var isHaveHiddenSlide = false;
                foreach (Slide slide in slides)
                    if (slide.SlideShowTransition.Hidden == Microsoft.Office.Core.MsoTriState.msoTrue) {
                        isHaveHiddenSlide = true;
                        break;
                    }

                Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                    if (isHaveHiddenSlide && !IsShowingRestoreHiddenSlidesWindow) {
                        IsShowingRestoreHiddenSlidesWindow = true;
                        new YesOrNoNotificationWindow("检测到此演示文档中包含隐藏的幻灯片，是否取消隐藏？",
                            () => {
                                foreach (Slide slide in slides)
                                    if (slide.SlideShowTransition.Hidden ==
                                        Microsoft.Office.Core.MsoTriState.msoTrue)
                                        slide.SlideShowTransition.Hidden =
                                            Microsoft.Office.Core.MsoTriState.msoFalse;
                                IsShowingRestoreHiddenSlidesWindow = false;
                            }, () => { IsShowingRestoreHiddenSlidesWindow = false; },
                            () => { IsShowingRestoreHiddenSlidesWindow = false; }).ShowDialog();
                    }
                }), DispatcherPriority.Normal);
            }

            //检测是否有自动播放
            if (Settings.PowerPointSettings.IsNotifyAutoPlayPresentation
                // && presentation.SlideShowSettings.AdvanceMode == PpSlideShowAdvanceMode.ppSlideShowUseSlideTimings
                && BorderFloatingBarExitPPTBtn.Visibility != Visibility.Visible) {
                bool hasSlideTimings = false;
                foreach (Slide slide in presentation.Slides) {
                    if (slide.SlideShowTransition.AdvanceOnTime == MsoTriState.msoTrue &&
                        slide.SlideShowTransition.AdvanceTime > 0) {
                        hasSlideTimings = true;
                        break;
                    }
                }

                if (hasSlideTimings) {
                    Application.Current.Dispatcher.BeginInvoke((Action)(() => {
                        if (hasSlideTimings && !IsShowingAutoplaySlidesWindow) {
                            IsShowingAutoplaySlidesWindow = true;
                            new YesOrNoNotificationWindow("检测到此演示文档中自动播放或排练计时已经启用，可能导致幻灯片自动翻页，是否取消？",
                                () => {
                                    presentation.SlideShowSettings.AdvanceMode =
                                        PpSlideShowAdvanceMode.ppSlideShowManualAdvance;
                                    IsShowingAutoplaySlidesWindow = false;
                                }, () => { IsShowingAutoplaySlidesWindow = false; },
                                () => { IsShowingAutoplaySlidesWindow = false; }).ShowDialog();
                        }
                    }));
                    presentation.SlideShowSettings.AdvanceMode = PpSlideShowAdvanceMode.ppSlideShowManualAdvance;
                }
            }
        }

        private void PptApplication_PresentationClose(Presentation Pres) {
            pptApplication.PresentationOpen -= PptApplication_PresentationOpen;
            pptApplication.PresentationClose -= PptApplication_PresentationClose;
            pptApplication.SlideShowBegin -= PptApplication_SlideShowBegin;
            pptApplication.SlideShowNextSlide -= PptApplication_SlideShowNextSlide;
            pptApplication.SlideShowEnd -= PptApplication_SlideShowEnd;
            pptApplication = null;
            timerCheckPPT.Start();
            Application.Current.Dispatcher.Invoke(() => {
                BorderFloatingBarExitPPTBtn.Visibility = Visibility.Collapsed;
            });
        }





        private string pptName = null;
        int currentShowPosition = -1;

        private void UpdatePPTBtnStyleSettingsStatus() {
            var sopt = Settings.PowerPointSettings.PPTSButtonsOption.ToString();
            char[] soptc = sopt.ToCharArray();
            if (soptc[0] == '2')
            {
                PPTLSPageButton.Visibility = Visibility.Visible;
                PPTRSPageButton.Visibility = Visibility.Visible;
            }
            else
            {
                PPTLSPageButton.Visibility = Visibility.Collapsed;
                PPTRSPageButton.Visibility = Visibility.Collapsed;
            }
            if (soptc[2] == '2')
            {
                // 优化：使用缓存的画刷对象
                PPTBtnLSBorder.Background = _darkBgBrush;
                PPTBtnRSBorder.Background = _darkBgBrush;
                PPTBtnLSBorder.BorderBrush = _darkBorderBrush;
                PPTBtnRSBorder.BorderBrush = _darkBorderBrush;
                PPTLSPreviousButtonGeometry.Brush = _whiteBrush;
                PPTRSPreviousButtonGeometry.Brush = _whiteBrush;
                PPTLSNextButtonGeometry.Brush = _whiteBrush;
                PPTRSNextButtonGeometry.Brush = _whiteBrush;
                PPTLSPreviousButtonFeedbackBorder.Background = _whiteBrush;
                PPTRSPreviousButtonFeedbackBorder.Background = _whiteBrush;
                PPTLSPageButtonFeedbackBorder.Background = _whiteBrush;
                PPTRSPageButtonFeedbackBorder.Background = _whiteBrush;
                PPTLSNextButtonFeedbackBorder.Background = _whiteBrush;
                PPTRSNextButtonFeedbackBorder.Background = _whiteBrush;
                TextBlock.SetForeground(PPTLSPageButton, _whiteBrush);
                TextBlock.SetForeground(PPTRSPageButton, _whiteBrush);
            }
            else
            {
                PPTBtnLSBorder.Background = _lightBgBrush;
                PPTBtnRSBorder.Background = _lightBgBrush;
                PPTBtnLSBorder.BorderBrush = _lightBorderBrush;
                PPTBtnRSBorder.BorderBrush = _lightBorderBrush;
                PPTLSPreviousButtonGeometry.Brush = _darkGeometryBrush;
                PPTRSPreviousButtonGeometry.Brush = _darkGeometryBrush;
                PPTLSNextButtonGeometry.Brush = _darkGeometryBrush;
                PPTRSNextButtonGeometry.Brush = _darkGeometryBrush;
                PPTLSPreviousButtonFeedbackBorder.Background = _darkTextBrush;
                PPTRSPreviousButtonFeedbackBorder.Background = _darkTextBrush;
                PPTLSPageButtonFeedbackBorder.Background = _darkTextBrush;
                PPTRSPageButtonFeedbackBorder.Background = _darkTextBrush;
                PPTLSNextButtonFeedbackBorder.Background = _darkTextBrush;
                PPTRSNextButtonFeedbackBorder.Background = _darkTextBrush;
                TextBlock.SetForeground(PPTLSPageButton, _darkTextBrush);
                TextBlock.SetForeground(PPTRSPageButton, _darkTextBrush);
            }
            if (soptc[1] == '2')
            {
                PPTBtnLSBorder.Opacity = 0.5;
                PPTBtnRSBorder.Opacity = 0.5;
            }
            else
            {
                PPTBtnLSBorder.Opacity = 1;
                PPTBtnRSBorder.Opacity = 1;
            }

            var bopt = Settings.PowerPointSettings.PPTBButtonsOption.ToString();
            char[] boptc = bopt.ToCharArray();
            if (boptc[0] == '2')
            {
                PPTLBPageButton.Visibility = Visibility.Visible;
                PPTRBPageButton.Visibility = Visibility.Visible;
            }
            else
            {
                PPTLBPageButton.Visibility = Visibility.Collapsed;
                PPTRBPageButton.Visibility = Visibility.Collapsed;
            }
            if (boptc[2] == '2')
            {
                // 优化：使用缓存的画刷对象
                PPTBtnLBBorder.Background = _darkBgBrush;
                PPTBtnRBBorder.Background = _darkBgBrush;
                PPTBtnLBBorder.BorderBrush = _darkBorderBrush;
                PPTBtnRBBorder.BorderBrush = _darkBorderBrush;
                PPTLBPreviousButtonGeometry.Brush = _whiteBrush;
                PPTRBPreviousButtonGeometry.Brush = _whiteBrush;
                PPTLBNextButtonGeometry.Brush = _whiteBrush;
                PPTRBNextButtonGeometry.Brush = _whiteBrush;
                PPTLBPreviousButtonFeedbackBorder.Background = _whiteBrush;
                PPTRBPreviousButtonFeedbackBorder.Background = _whiteBrush;
                PPTLBPageButtonFeedbackBorder.Background = _whiteBrush;
                PPTRBPageButtonFeedbackBorder.Background = _whiteBrush;
                PPTLBNextButtonFeedbackBorder.Background = _whiteBrush;
                PPTRBNextButtonFeedbackBorder.Background = _whiteBrush;
                TextBlock.SetForeground(PPTLBPageButton, _whiteBrush);
                TextBlock.SetForeground(PPTRBPageButton, _whiteBrush);
            }
            else
            {
                PPTBtnLBBorder.Background = _lightBgBrush;
                PPTBtnRBBorder.Background = _lightBgBrush;
                PPTBtnLBBorder.BorderBrush = _lightBorderBrush;
                PPTBtnRBBorder.BorderBrush = _lightBorderBrush;
                PPTLBPreviousButtonGeometry.Brush = _darkGeometryBrush;
                PPTRBPreviousButtonGeometry.Brush = _darkGeometryBrush;
                PPTLBNextButtonGeometry.Brush = _darkGeometryBrush;
                PPTRBNextButtonGeometry.Brush = _darkGeometryBrush;
                PPTLBPreviousButtonFeedbackBorder.Background = _darkTextBrush;
                PPTRBPreviousButtonFeedbackBorder.Background = _darkTextBrush;
                PPTLBPageButtonFeedbackBorder.Background = _darkTextBrush;
                PPTRBPageButtonFeedbackBorder.Background = _darkTextBrush;
                PPTLBNextButtonFeedbackBorder.Background = _darkTextBrush;
                PPTRBNextButtonFeedbackBorder.Background = _darkTextBrush;
                TextBlock.SetForeground(PPTLBPageButton, _darkTextBrush);
                TextBlock.SetForeground(PPTRBPageButton, _darkTextBrush);
            }
            if (boptc[1] == '2')
            {
                PPTBtnLBBorder.Opacity = 0.5;
                PPTBtnRBBorder.Opacity = 0.5;
            }
            else
            {
                PPTBtnLBBorder.Opacity = 1;
                PPTBtnRBBorder.Opacity = 1;
            }
        }

        private void UpdatePPTBtnDisplaySettingsStatus() {

            if (!Settings.PowerPointSettings.ShowPPTButton) {
                LeftBottomPanelForPPTNavigation.Visibility = Visibility.Collapsed;
                RightBottomPanelForPPTNavigation.Visibility = Visibility.Collapsed;
                LeftSidePanelForPPTNavigation.Visibility = Visibility.Collapsed;
                RightSidePanelForPPTNavigation.Visibility = Visibility.Collapsed;
                return;
            }

            var lsp = Settings.PowerPointSettings.PPTLSButtonPosition;
            LeftSidePanelForPPTNavigation.Margin = new Thickness(0, 0, 0, (double)lsp * 2);
            var rsp = Settings.PowerPointSettings.PPTRSButtonPosition;
            RightSidePanelForPPTNavigation.Margin = new Thickness(0, 0, 0, (double)rsp * 2);

            var dopt = Settings.PowerPointSettings.PPTButtonsDisplayOption.ToString();
            char[] doptc = dopt.ToCharArray();

            if (doptc[0] == '2') AnimationsHelper.ShowWithFadeIn(LeftBottomPanelForPPTNavigation);
            else LeftBottomPanelForPPTNavigation.Visibility = Visibility.Collapsed;
            if (doptc[1] == '2') AnimationsHelper.ShowWithFadeIn(RightBottomPanelForPPTNavigation);
            else RightBottomPanelForPPTNavigation.Visibility = Visibility.Collapsed;
            if (doptc[2] == '2') AnimationsHelper.ShowWithFadeIn(LeftSidePanelForPPTNavigation);
            else LeftSidePanelForPPTNavigation.Visibility = Visibility.Collapsed;
            if (doptc[3] == '2') AnimationsHelper.ShowWithFadeIn(RightSidePanelForPPTNavigation);
            else RightSidePanelForPPTNavigation.Visibility = Visibility.Collapsed;
        }

        // COM错误处理：RPC_E_CALL_REJECTED 重试常量
        private const int RPC_E_CALL_REJECTED = unchecked((int)0x80010001);
        private const int COM_RETRY_COUNT = 3;
        private const int COM_RETRY_DELAY_MS = 100;

        /// <summary>
        /// 带重试机制的COM操作执行器，用于处理RPC_E_CALL_REJECTED错误
        /// </summary>
        private T ExecuteComOperationWithRetry<T>(Func<T> operation, string operationName, T defaultValue = default) {
            for (int attempt = 1; attempt <= COM_RETRY_COUNT; attempt++) {
                try {
                    return operation();
                }
                catch (COMException ex) when (ex.ErrorCode == RPC_E_CALL_REJECTED) {
                    LogHelper.WriteLogToFile($"COM 操作 '{operationName}' 被拒绝（第 {attempt}/{COM_RETRY_COUNT} 次），正在重试...", LogHelper.LogType.Warning);
                    if (attempt < COM_RETRY_COUNT) {
                        Thread.Sleep(COM_RETRY_DELAY_MS * attempt); // 递增延迟
                    }
                    else {
                        LogHelper.WriteLogToFile($"COM 操作 '{operationName}' 在重试 {COM_RETRY_COUNT} 次后失败：{ex.Message}", LogHelper.LogType.Error);
                        return defaultValue;
                    }
                }
                catch (Exception ex) {
                    LogHelper.WriteLogToFile($"COM 操作 '{operationName}' 失败：{ex.Message}", LogHelper.LogType.Error);
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// 带重试机制的COM操作执行器（无返回值版本）
        /// </summary>
        private bool ExecuteComOperationWithRetry(Action operation, string operationName) {
            for (int attempt = 1; attempt <= COM_RETRY_COUNT; attempt++) {
                try {
                    operation();
                    return true;
                }
                catch (COMException ex) when (ex.ErrorCode == RPC_E_CALL_REJECTED) {
                    LogHelper.WriteLogToFile($"COM operation '{operationName}' rejected (attempt {attempt}/{COM_RETRY_COUNT}), retrying...", LogHelper.LogType.Warning);
                    if (attempt < COM_RETRY_COUNT) {
                        Thread.Sleep(COM_RETRY_DELAY_MS * attempt);
                    }
                    else {
                        LogHelper.WriteLogToFile($"COM operation '{operationName}' failed after {COM_RETRY_COUNT} attempts: {ex.Message}", LogHelper.LogType.Error);
                        return false;
                    }
                }
                catch (Exception ex) {
                    LogHelper.WriteLogToFile($"COM operation '{operationName}' failed: {ex.Message}", LogHelper.LogType.Error);
                    return false;
                }
            }
            return false;
        }

        private async void PptApplication_SlideShowBegin(SlideShowWindow Wn) {
            if (Settings.Automation.IsAutoFoldInPPTSlideShow && !isFloatingBarFolded)
                await FoldFloatingBar(new object());
            else if (isFloatingBarFolded) await UnFoldFloatingBar(new object());

            isStopInkReplay = true;

            LogHelper.WriteLogToFile("PowerPoint 放映开始", LogHelper.LogType.Event);

            // 等待COM对象准备就绪
            await Task.Delay(50);

            await Application.Current.Dispatcher.InvokeAsync(() => {
                try {
                    // 使用重试机制获取Presentation对象
                    var presentationObj = ExecuteComOperationWithRetry(() => Wn.Presentation, "GetPresentation");
                    if (presentationObj == null) {
                        LogHelper.WriteLogToFile("获取 Presentation 对象失败，终止放映开始处理", LogHelper.LogType.Error);
                        return;
                    }

                    //调整颜色
                    var screenRatio = SystemParameters.PrimaryScreenWidth / SystemParameters.PrimaryScreenHeight;
                    var slideWidth = ExecuteComOperationWithRetry(() => presentationObj.PageSetup.SlideWidth, "GetSlideWidth", 0f);
                    var slideHeight = ExecuteComOperationWithRetry(() => presentationObj.PageSetup.SlideHeight, "GetSlideHeight", 1f);
                    
                    // 检查屏幕比例和幻灯片比例是否匹配
                    const double targetRatio16_9 = 16.0 / 9;
                    const double tolerance = 0.01;
                    
                    if (Math.Abs(screenRatio - targetRatio16_9) <= tolerance) {
                        // 16:9 屏幕比例，检查幻灯片比例
                        if (slideWidth / slideHeight < 1.65) {
                            // 幻灯片比例接近4:3，可能需要调整颜色设置
                            lastDesktopInkColor = 0;
                        }
                    } else if (Math.Abs(screenRatio - (256.0 / 135.0)) <= tolerance) {
                        // 接近19:9或其他宽屏比例
                        lastDesktopInkColor = 2;
                    } else {
                        // 默认颜色设置
                        lastDesktopInkColor = 1;
                    }

                    slidescount = ExecuteComOperationWithRetry(() => presentationObj.Slides.Count, "GetSlidesCount", 0);
                    previousSlideID = 0;
                    memoryStreams = new MemoryStream[slidescount + 2];

                    pptName = ExecuteComOperationWithRetry(() => presentationObj.Name, "GetPresentationName", "未知");
                    LogHelper.NewLog("课件名称：" + pptName);
                    LogHelper.NewLog("幻灯片数量：" + slidescount.ToString());

                //检查是否有已有墨迹，并加载
                if (Settings.PowerPointSettings.IsAutoSaveStrokesInPowerPoint)
                    if (Directory.Exists(Settings.Automation.AutoSavedStrokesLocation +
                                         @"\Auto Saved - Presentations\" + pptName + "_" +
                                         slidescount)) {
                        LogHelper.WriteLogToFile("检测到已保存的墨迹", LogHelper.LogType.Trace);
                        var files = new DirectoryInfo(Settings.Automation.AutoSavedStrokesLocation +
                                                      @"\Auto Saved - Presentations\" + pptName + "_" +
                                                      slidescount).GetFiles();
                        var count = 0;
                        foreach (var file in files)
                            if (file.Name != "Position") {
                                var i = -1;
                                try {
                                    i = int.Parse(Path.GetFileNameWithoutExtension(file.Name));
                                    memoryStreams[i]?.Dispose();
                                    memoryStreams[i] = new MemoryStream(File.ReadAllBytes(file.FullName));
                                    memoryStreams[i].Position = 0;
                                    count++;
                                }
                                catch (Exception ex) {
                                    LogHelper.WriteLogToFile(
                                        $"加载第 {i} 张幻灯片墨迹失败\n{ex}",
                                        LogHelper.LogType.Error);
                                }
                            }

                        LogHelper.WriteLogToFile($"已加载保存的墨迹：{count.ToString()} 份");
                    }

                BorderFloatingBarExitPPTBtn.Visibility = Visibility.Visible;

                // -- old --
                //if (Settings.PowerPointSettings.IsShowBottomPPTNavigationPanel && !isFloatingBarFolded)
                //    AnimationsHelper.ShowWithSlideFromBottomAndFade(BottomViewboxPPTSidesControl);
                //else
                //    BottomViewboxPPTSidesControl.Visibility = Visibility.Collapsed;

                //if (Settings.PowerPointSettings.IsShowSidePPTNavigationPanel && !isFloatingBarFolded) {

                //    AnimationsHelper.ShowWithScaleFromRight(RightSidePanelForPPTNavigation);
                //} else {
                //    LeftSidePanelForPPTNavigation.Visibility = Visibility.Collapsed;
                //    RightSidePanelForPPTNavigation.Visibility = Visibility.Collapsed;
                //}
                // -- old --

                // -- new --
                if (!isFloatingBarFolded) {
                    UpdatePPTBtnDisplaySettingsStatus();
                    UpdatePPTBtnStyleSettingsStatus();
                }

                BorderFloatingBarExitPPTBtn.Visibility = Visibility.Visible;
                ViewboxFloatingBar.Opacity = Settings.Appearance.ViewboxFloatingBarOpacityInPPTValue;

                if (Settings.PowerPointSettings.IsShowCanvasAtNewSlideShow &&
                    !Settings.Automation.IsAutoFoldInPPTSlideShow &&
                    GridTransparencyFakeBackground.Background == Brushes.Transparent && !isFloatingBarFolded) {
                    BtnHideInkCanvas_Click(null, null);
                }

                if (currentMode != 0)
                {
                    //currentMode = 0;
                    //GridBackgroundCover.Visibility = Visibility.Collapsed;
                    //AnimationsHelper.HideWithSlideAndFade(BlackboardLeftSide);
                    //AnimationsHelper.HideWithSlideAndFade(BlackboardCenterSide);
                    //AnimationsHelper.HideWithSlideAndFade(BlackboardRightSide);

                    //SaveStrokes();
                    //ClearStrokes(true);

                    //BtnSwitch.Content = BtnSwitchTheme.Content.ToString() == "浅色" ? "黑板" : "白板";
                    //StackPanelPPTButtons.Visibility = Visibility.Visible;
                    ImageBlackboard_MouseUp(null,null);
                    BtnHideInkCanvas_Click(null, null);
                }

                //ClearStrokes(true);

                BorderFloatingBarMainControls.Visibility = Visibility.Visible;

                if (Settings.PowerPointSettings.IsShowCanvasAtNewSlideShow &&
                    !Settings.Automation.IsAutoFoldInPPTSlideShow)
                    BtnColorRed_Click(null, null);

                if (Settings.PowerPointSettings.IsAutoEnterAnnotationMode && !isFloatingBarFolded) {
                    SelectedMode = ICCToolsEnum.PenMode;
                    ForceUpdateToolSelection(null);
                }

                isEnteredSlideShowEndEvent = false;
                var currentPos = ExecuteComOperationWithRetry(() => Wn.View.CurrentShowPosition, "GetCurrentShowPosition", 1);
                PPTBtnPageNow.Text = $"{currentPos}";
                PPTBtnPageTotal.Text = $"/ {slidescount}";
                LogHelper.NewLog("PowerPoint 放映加载流程完成");

                if (!isFloatingBarFolded) {
                    // 优化：使用Task.Delay代替Thread.Sleep
                    Task.Run(async () => {
                        await Task.Delay(100);
                        Application.Current.Dispatcher.Invoke(() => {
                            ViewboxFloatingBarMarginAnimation(60);
                        });
                    });
                }
                }
                catch (COMException ex) {
                    LogHelper.WriteLogToFile($"SlideShowBegin 中的 COM 异常：{ex.Message}（错误码：0x{ex.ErrorCode:X8}）", LogHelper.LogType.Error);
                }
                catch (Exception ex) {
                    LogHelper.WriteLogToFile($"SlideShowBegin 异常：{ex.Message}", LogHelper.LogType.Error);
                }
            });
        }

        private bool isEnteredSlideShowEndEvent = false; //防止重复调用本函数导致墨迹保存失效

        private async void PptApplication_SlideShowEnd(Presentation Pres) {
            if (isFloatingBarFolded) await UnFoldFloatingBar(new object());

            LogHelper.WriteLogToFile(string.Format("PowerPoint 放映结束"), LogHelper.LogType.Event);
            if (isEnteredSlideShowEndEvent) {
                LogHelper.WriteLogToFile("检测到已进入放映结束流程，直接返回");
                return;
            }

            isEnteredSlideShowEndEvent = true;

            // 优化：异步保存墨迹文件，不阻塞UI
            if (Settings.PowerPointSettings.IsAutoSaveStrokesInPowerPoint) {
                var rootPath = Settings.Automation.AutoSavedStrokesLocation;
                var folderName = Pres.Name + "_" + Pres.Slides.Count;
                var folderPath = Path.Combine(rootPath, "Auto Saved - Presentations", folderName);
                var slidesCount = Pres.Slides.Count;
                var currentPos = currentShowPosition;
                var prevSlideId = previousSlideID;

                // 先在UI线程保存当前墨迹到内存
                MemoryStream[] streamsToSave = null;
                Application.Current.Dispatcher.Invoke(() => {
                    try {
                        memoryStreams[currentPos]?.Dispose();
                        using (MemoryStream ms = new MemoryStream()) {
                            inkCanvas.Strokes.Save(ms);
                            ms.Position = 0;
                            memoryStreams[currentPos] = new MemoryStream(ms.ToArray());
                        }
                        // 复制引用以便异步保存
                        streamsToSave = memoryStreams;
                    }
                    catch (Exception ex) {
                        LogHelper.WriteLogToFile("保存墨迹到内存流失败：" + ex.Message, LogHelper.LogType.Error);
                    }
                });

                // 异步保存文件
                _ = Task.Run(() => {
                    try {
                        string drive = Path.GetPathRoot(folderPath);
                        var actualFolderPath = folderPath;
                        if (!string.IsNullOrEmpty(drive) && !Directory.Exists(drive)) {
                            string fallbackRoot = Path.Combine(App.RootPath, "FallbackPresentations");
                            actualFolderPath = Path.Combine(fallbackRoot, folderName);
                            LogHelper.WriteLogToFile($"未找到驱动器 {drive}，已回退到 {actualFolderPath}", LogHelper.LogType.Warning);
                        }

                        if (!Directory.Exists(actualFolderPath)) Directory.CreateDirectory(actualFolderPath);
                        File.WriteAllText(actualFolderPath + "/Position", prevSlideId.ToString());

                        if (streamsToSave != null) {
                            for (var i = 1; i <= slidesCount; i++) {
                                if (streamsToSave[i] != null) {
                                    try {
                                        if (streamsToSave[i].Length > 8) {
                                            streamsToSave[i].Position = 0;
                                            var srcBuf = new byte[streamsToSave[i].Length];
                                            var byteLength = streamsToSave[i].Read(srcBuf, 0, srcBuf.Length);
                                            File.WriteAllBytes(actualFolderPath + @"\" + i.ToString("0000") + ".icstk", srcBuf);
                                            LogHelper.WriteLogToFile(string.Format(
                                                "已保存第 {0} 张幻灯片墨迹，大小={1}, 读取字节={2}", i.ToString(),
                                                streamsToSave[i].Length, byteLength));
                                        } else {
                                            var filePath = actualFolderPath + @"\" + i.ToString("0000") + ".icstk";
                                            if (File.Exists(filePath)) File.Delete(filePath);
                                        }
                                    }
                                    catch (Exception ex) {
                                        LogHelper.WriteLogToFile(
                                            $"保存第 {i} 张幻灯片墨迹失败\n{ex}",
                                            LogHelper.LogType.Error);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex) {
                        LogHelper.WriteLogToFile("保存墨迹文件过程中发生错误：" + ex.Message, LogHelper.LogType.Error);
                    }
                });
            }

            await Application.Current.Dispatcher.InvokeAsync(() => {


                BorderFloatingBarExitPPTBtn.Visibility = Visibility.Collapsed;
                LeftBottomPanelForPPTNavigation.Visibility = Visibility.Collapsed;
                RightBottomPanelForPPTNavigation.Visibility = Visibility.Collapsed;
                LeftSidePanelForPPTNavigation.Visibility = Visibility.Collapsed;
                RightSidePanelForPPTNavigation.Visibility = Visibility.Collapsed;


                if (currentMode != 0) {
                    ImageBlackboard_MouseUp(null,null);
                }

                ClearStrokes(true);

                if (GridTransparencyFakeBackground.Background != Brushes.Transparent)
                    BtnHideInkCanvas_Click(null, null);

                ViewboxFloatingBar.Opacity = Settings.Appearance.ViewboxFloatingBarOpacityValue;
            });

            await Task.Delay(150);

            _ = Application.Current.Dispatcher.InvokeAsync(() => {
                ViewboxFloatingBarMarginAnimation(100, true);
            });

        }

        private int previousSlideID = 0;
        private MemoryStream[] memoryStreams = new MemoryStream[50];

        private void PptApplication_SlideShowNextSlide(SlideShowWindow Wn) {
            LogHelper.WriteLogToFile($"PowerPoint Next Slide (Slide {Wn.View.CurrentShowPosition})",
                LogHelper.LogType.Event);
            if (Wn.View.CurrentShowPosition == previousSlideID) return;

            // 优化：异步保存墨迹，不阻塞UI
            var prevSlideId = previousSlideID;
            Application.Current.Dispatcher.Invoke(() => {
                memoryStreams[prevSlideId]?.Dispose();
                using (var ms = new MemoryStream()) {
                    inkCanvas.Strokes.Save(ms);
                    ms.Position = 0;
                    memoryStreams[prevSlideId] = new MemoryStream(ms.ToArray());
                }

                // 优化：异步保存截图
                if (inkCanvas.Strokes.Count > Settings.Automation.MinimumAutomationStrokeNumber &&
                    Settings.PowerPointSettings.IsAutoSaveScreenShotInPowerPoint && !_isPptClickingBtnTurned) {
                    var presName = Wn.Presentation.Name;
                    var slidePos = Wn.View.CurrentShowPosition;
                    Task.Run(() => SavePPTScreenshot(presName + "/" + slidePos));
                }
                _isPptClickingBtnTurned = false;

                ClearStrokes(true);
                timeMachine.ClearStrokeHistory();

                try {
                    if (memoryStreams[Wn.View.CurrentShowPosition] != null &&
                        memoryStreams[Wn.View.CurrentShowPosition].Length > 0)
                        inkCanvas.Strokes.Add(new StrokeCollection(memoryStreams[Wn.View.CurrentShowPosition]));

                    currentShowPosition = Wn.View.CurrentShowPosition;
                }
                catch (Exception ex) {
                    LogHelper.WriteLogToFile("MW_PPT.cs 异常（此前忽略）：" + ex.Message, LogHelper.LogType.Error);
                }

                PPTBtnPageNow.Text = $"{Wn.View.CurrentShowPosition}";
                PPTBtnPageTotal.Text = $"/ {Wn.Presentation.Slides.Count}";
            });
            previousSlideID = Wn.View.CurrentShowPosition;
        }

        private bool _isPptClickingBtnTurned = false;


        private bool IsSlideShowRunning() {
            try {
                return pptApplication != null && pptApplication.SlideShowWindows != null && pptApplication.SlideShowWindows.Count > 0;
            }
            catch (COMException ex) {
                LogHelper.WriteLogToFile("检查放映状态时发生 COM 错误：" + ex.Message, LogHelper.LogType.Trace);
                return false;
            }
            catch (Exception ex) {
                LogHelper.WriteLogToFile("检查放映状态失败：" + ex.Message, LogHelper.LogType.Trace);
                return false;
            }
        }

        private void BtnPPTSlidesUp_Click(object sender, RoutedEventArgs e) {
            if (currentMode == 1) {
                ImageBlackboard_MouseUp(null, null);
            }

            _isPptClickingBtnTurned = true;

            if (!IsSlideShowRunning()) return;

            try {
                if (inkCanvas.Strokes.Count > Settings.Automation.MinimumAutomationStrokeNumber &&
                    Settings.PowerPointSettings.IsAutoSaveScreenShotInPowerPoint)
                    SavePPTScreenshot(pptApplication.SlideShowWindows[1].Presentation.Name + "/" + pptApplication.SlideShowWindows[1].View.CurrentShowPosition);
            }
            catch (Exception ex) {
                LogHelper.WriteLogToFile("保存 PPT 截图失败（上一张）：" + ex.Message, LogHelper.LogType.Error);
            }

            // 优化：使用线程池代替new Thread
            ThreadPool.QueueUserWorkItem(_ => {
                lock (_pptOperationLock) {
                    try {
                        pptApplication.SlideShowWindows[1].Activate();
                    }
                    catch (COMException) {
                        // ignored
                    }
                    catch (Exception ex) {
                        LogHelper.WriteLogToFile("激活放映窗口失败：" + ex.Message, LogHelper.LogType.Trace);
                    }

                    try {
                        pptApplication.SlideShowWindows[1].View.Previous();
                    }
                    catch (Exception ex) {
                        LogHelper.WriteLogToFile("切换到上一张幻灯片失败：" + ex.Message, LogHelper.LogType.Error);
                    }
                }
            });
        }

        private void BtnPPTSlidesDown_Click(object sender, RoutedEventArgs e) {
            if (currentMode == 1) {
                ImageBlackboard_MouseUp(null, null);
            }

            _isPptClickingBtnTurned = true;
            if (!IsSlideShowRunning()) return;

            try {
                if (inkCanvas.Strokes.Count > Settings.Automation.MinimumAutomationStrokeNumber &&
                    Settings.PowerPointSettings.IsAutoSaveScreenShotInPowerPoint)
                    SavePPTScreenshot(pptApplication.SlideShowWindows[1].Presentation.Name + "/" + pptApplication.SlideShowWindows[1].View.CurrentShowPosition);
            }
            catch (Exception ex) {
                LogHelper.WriteLogToFile("保存 PPT 截图失败（下一张）：" + ex.Message, LogHelper.LogType.Error);
            }

            // 优化：使用线程池代替new Thread
            ThreadPool.QueueUserWorkItem(_ => {
                lock (_pptOperationLock) {
                    try {
                        pptApplication.SlideShowWindows[1].Activate();
                    }
                    catch (COMException) {
                        // ignored
                    }
                    catch (Exception ex) {
                        LogHelper.WriteLogToFile("激活放映窗口失败：" + ex.Message, LogHelper.LogType.Trace);
                    }

                    try {
                        pptApplication.SlideShowWindows[1].View.Next();
                    }
                    catch (COMException) {
                        // ignored
                    }
                    catch (Exception ex) {
                        LogHelper.WriteLogToFile("切换到下一张幻灯片失败：" + ex.Message, LogHelper.LogType.Trace);
                    }
                }
            });
        }

        private void PPTNavigationBtn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            lastBorderMouseDownObject = sender;
            if (!Settings.PowerPointSettings.EnablePPTButtonPageClickable) return;
            if (sender == PPTLSPageButton)
            {
                PPTLSPageButtonFeedbackBorder.Opacity = 0.15;
            }
            else if (sender == PPTRSPageButton)
            {
                PPTRSPageButtonFeedbackBorder.Opacity = 0.15;
            }
            else if (sender == PPTLBPageButton)
            {
                PPTLBPageButtonFeedbackBorder.Opacity = 0.15;
            }
            else if (sender == PPTRBPageButton)
            {
                PPTRBPageButtonFeedbackBorder.Opacity = 0.15;
            }
        }

        private void PPTNavigationBtn_MouseLeave(object sender, MouseEventArgs e)
        {
            lastBorderMouseDownObject = null;
            if (sender == PPTLSPageButton)
            {
                PPTLSPageButtonFeedbackBorder.Opacity = 0;
            }
            else if (sender == PPTRSPageButton)
            {
                PPTRSPageButtonFeedbackBorder.Opacity = 0;
            }
            else if (sender == PPTLBPageButton)
            {
                PPTLBPageButtonFeedbackBorder.Opacity = 0;
            }
            else if (sender == PPTRBPageButton)
            {
                PPTRBPageButtonFeedbackBorder.Opacity = 0;
            }
        }

        private async void PPTNavigationBtn_MouseUp(object sender, MouseButtonEventArgs e) {
            if (lastBorderMouseDownObject != sender) return;

            if (sender == PPTLSPageButton)
            {
                PPTLSPageButtonFeedbackBorder.Opacity = 0;
            }
            else if (sender == PPTRSPageButton)
            {
                PPTRSPageButtonFeedbackBorder.Opacity = 0;
            }
            else if (sender == PPTLBPageButton)
            {
                PPTLBPageButtonFeedbackBorder.Opacity = 0;
            }
            else if (sender == PPTRBPageButton)
            {
                PPTRBPageButtonFeedbackBorder.Opacity = 0;
            }

            if (!Settings.PowerPointSettings.EnablePPTButtonPageClickable) return;

            GridTransparencyFakeBackground.Opacity = 1;
            GridTransparencyFakeBackground.Background = new SolidColorBrush(StringToColor("#01FFFFFF"));
            CursorIcon_Click(null, null);
            try {
                pptApplication.SlideShowWindows[1].SlideNavigation.Visible = true;
            }
            catch (Exception ex) {
                LogHelper.WriteLogToFile("显示幻灯片导航失败：" + ex.Message, LogHelper.LogType.Error);
            }

            // 控制居中
            if (!isFloatingBarFolded) {
                await Task.Delay(100);
                ViewboxFloatingBarMarginAnimation(60);
            }
        }

        private void BtnPPTSlideShow_Click(object sender, RoutedEventArgs e) {
            // 优化：使用线程池代替new Thread
            ThreadPool.QueueUserWorkItem(_ => {
                try {
                    presentation.SlideShowSettings.Run();
                }
                catch (Exception ex) {
                    LogHelper.WriteLogToFile("MW_PPT.cs 异常：" + ex.Message, LogHelper.LogType.Error);
                }
            });
        }

        private async void BtnPPTSlideShowEnd_Click(object sender, RoutedEventArgs e) {
            if (!IsSlideShowRunning()) return;
            Application.Current.Dispatcher.Invoke(() => {
                try {
                    var currentPos = pptApplication.SlideShowWindows[1].View.CurrentShowPosition;
                    memoryStreams[currentPos]?.Dispose();
                    using (var ms = new MemoryStream()) {
                        inkCanvas.Strokes.Save(ms);
                        ms.Position = 0;
                        memoryStreams[currentPos] = new MemoryStream(ms.ToArray());
                    }
                    timeMachine.ClearStrokeHistory();
                }
                catch (Exception ex) {
                    LogHelper.WriteLogToFile("MW_PPT.cs 异常（此前忽略）：" + ex.Message, LogHelper.LogType.Error);
                }
            });

            // 优化：使用线程池代替new Thread
            ThreadPool.QueueUserWorkItem(_ => {
                try {
                    pptApplication.SlideShowWindows[1].View.Exit();
                }
                catch (Exception ex) {
                    LogHelper.WriteLogToFile("MW_PPT.cs 异常（此前忽略）：" + ex.Message, LogHelper.LogType.Error);
                }
            });

            HideSubPanels("cursor");
            // update tool selection
            SelectedMode = ICCToolsEnum.CursorMode;
            ForceUpdateToolSelection(null);
            await Task.Delay(150);
            ViewboxFloatingBarMarginAnimation(100, true);
        }

        private void GridPPTControlPrevious_MouseDown(object sender, MouseButtonEventArgs e)
        {
            lastBorderMouseDownObject = sender;
            if (sender == PPTLSPreviousButtonBorder) {
                PPTLSPreviousButtonFeedbackBorder.Opacity = 0.15;
            } else if (sender == PPTRSPreviousButtonBorder) {
                PPTRSPreviousButtonFeedbackBorder.Opacity = 0.15;
            } else if (sender == PPTLBPreviousButtonBorder)
            {
                PPTLBPreviousButtonFeedbackBorder.Opacity = 0.15;
            }
            else if (sender == PPTRBPreviousButtonBorder)
            {
                PPTRBPreviousButtonFeedbackBorder.Opacity = 0.15;
            }
        }
        private void GridPPTControlPrevious_MouseLeave(object sender, MouseEventArgs e)
        {
            lastBorderMouseDownObject = null;
            if (sender == PPTLSPreviousButtonBorder) {
                PPTLSPreviousButtonFeedbackBorder.Opacity = 0;
            } else if (sender == PPTRSPreviousButtonBorder) {
                PPTRSPreviousButtonFeedbackBorder.Opacity = 0;
            } else if (sender == PPTLBPreviousButtonBorder)
            {
                PPTLBPreviousButtonFeedbackBorder.Opacity = 0;
            }
            else if (sender == PPTRBPreviousButtonBorder)
            {
                PPTRBPreviousButtonFeedbackBorder.Opacity = 0;
            }
        }
        private void GridPPTControlPrevious_MouseUp(object sender, MouseButtonEventArgs e) {
            if (lastBorderMouseDownObject != sender) return;
            if (sender == PPTLSPreviousButtonBorder) {
                PPTLSPreviousButtonFeedbackBorder.Opacity = 0;
            } else if (sender == PPTRSPreviousButtonBorder) {
                PPTRSPreviousButtonFeedbackBorder.Opacity = 0;
            } else if (sender == PPTLBPreviousButtonBorder)
            {
                PPTLBPreviousButtonFeedbackBorder.Opacity = 0;
            }
            else if (sender == PPTRBPreviousButtonBorder)
            {
                PPTRBPreviousButtonFeedbackBorder.Opacity = 0;
            }
            BtnPPTSlidesUp_Click(null, null);
        }


        private void GridPPTControlNext_MouseDown(object sender, MouseButtonEventArgs e) {
            lastBorderMouseDownObject = sender;
            if (sender == PPTLSNextButtonBorder) {
                PPTLSNextButtonFeedbackBorder.Opacity = 0.15;
            } else if (sender == PPTRSNextButtonBorder) {
                PPTRSNextButtonFeedbackBorder.Opacity = 0.15;
            } else if (sender == PPTLBNextButtonBorder)
            {
                PPTLBNextButtonFeedbackBorder.Opacity = 0.15;
            }
            else if (sender == PPTRBNextButtonBorder)
            {
                PPTRBNextButtonFeedbackBorder.Opacity = 0.15;
            }
        }
        private void GridPPTControlNext_MouseLeave(object sender, MouseEventArgs e)
        {
            lastBorderMouseDownObject = null;
            if (sender == PPTLSNextButtonBorder) {
                PPTLSNextButtonFeedbackBorder.Opacity = 0;
            } else if (sender == PPTRSNextButtonBorder) {
                PPTRSNextButtonFeedbackBorder.Opacity = 0;
            } else if (sender == PPTLBNextButtonBorder)
            {
                PPTLBNextButtonFeedbackBorder.Opacity = 0;
            }
            else if (sender == PPTRBNextButtonBorder)
            {
                PPTRBNextButtonFeedbackBorder.Opacity = 0;
            }
        }
        private void GridPPTControlNext_MouseUp(object sender, MouseButtonEventArgs e) {
            if (lastBorderMouseDownObject != sender) return;
            if (sender == PPTLSNextButtonBorder) {
                PPTLSNextButtonFeedbackBorder.Opacity = 0;
            } else if (sender == PPTRSNextButtonBorder) {
                PPTRSNextButtonFeedbackBorder.Opacity = 0;
            } else if (sender == PPTLBNextButtonBorder)
            {
                PPTLBNextButtonFeedbackBorder.Opacity = 0;
            }
            else if (sender == PPTRBNextButtonBorder)
            {
                PPTRBNextButtonFeedbackBorder.Opacity = 0;
            }
            BtnPPTSlidesDown_Click(null, null);
        }

        private void ImagePPTControlEnd_MouseUp(object sender, MouseButtonEventArgs e) {
            BtnPPTSlideShowEnd_Click(null, null);
        }
    }
}
