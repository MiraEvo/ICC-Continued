using Ink_Canvas.Helpers;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.PowerPoint;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Ink_Canvas.Services
{
    /// <summary>
    /// 保存墨迹到文件的参数对象
    /// </summary>
    public class SaveStrokesParameters
    {
        /// <summary>
        /// 演示文稿名称
        /// </summary>
        public string PresentationName { get; set; }

        /// <summary>
        /// 幻灯片总数
        /// </summary>
        public int SlidesCount { get; set; }

        /// <summary>
        /// 当前位置
        /// </summary>
        public int CurrentPosition { get; set; }

        /// <summary>
        /// 上一张幻灯片 ID
        /// </summary>
        public int PreviousSlideId { get; set; }

        /// <summary>
        /// 根路径
        /// </summary>
        public string RootPath { get; set; }
    }

    /// <summary>
    /// PowerPoint 集成服务实现
    /// 负责处理与 PowerPoint 的 COM 互操作和幻灯片放映管理
    /// </summary>
    public class PPTService : IPPTService
    {
        private Microsoft.Office.Interop.PowerPoint.Application _pptApplication;
        private Presentation _presentation;
        private Slides _slides;
        private Slide _slide;
        private int _slidesCount;
        private int _currentShowPosition = -1;
        private MemoryStream[] _memoryStreams = new MemoryStream[50];

        // PPT 操作锁，防止并发问题
        private static readonly object _pptOperationLock = new object();

        // 异步墨迹保存队列
        private readonly ConcurrentQueue<(int slideId, MemoryStream stream)> _strokeSaveQueue = new ConcurrentQueue<(int, MemoryStream)>();

        #region 属性

        public Microsoft.Office.Interop.PowerPoint.Application PptApplication => _pptApplication;
        public Presentation Presentation => _presentation;
        public Slides Slides => _slides;
        public Slide Slide => _slide;
        public int SlidesCount => _slidesCount;
        public int CurrentShowPosition
        {
            get => _currentShowPosition;
            set => _currentShowPosition = value;
        }
        public MemoryStream[] MemoryStreams
        {
            get => _memoryStreams;
            set => _memoryStreams = value;
        }

        #endregion

        #region 事件

        public event Action<Presentation> PresentationOpened;
        public event Action<Presentation> PresentationClosed;
        public event Action<SlideShowWindow> SlideShowBegin;
        public event Action<SlideShowWindow> SlideShowNextSlide;
        public event Action<Presentation> SlideShowEnd;

        #endregion

        #region COM 互操作

        [DllImport("ole32.dll")]
        private static extern void CLSIDFromProgIDEx([MarshalAs(UnmanagedType.LPWStr)] string lpszProgID, out Guid lpclsid);

        [DllImport("oleaut32.dll", PreserveSig = false)]
        private static extern void GetActiveObject(ref Guid rclsid, IntPtr pvReserved, [MarshalAs(UnmanagedType.IUnknown)] out object ppunk);

        private static object GetActiveObject(string progId)
        {
            Guid clsid;
            CLSIDFromProgIDEx(progId, out clsid);
            GetActiveObject(ref clsid, IntPtr.Zero, out object obj);
            return obj;
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 检查并连接到 PowerPoint 应用程序
        /// </summary>
        public bool CheckAndConnectPPT(string progId = "PowerPoint.Application")
        {
            try
            {
                _pptApplication = (Microsoft.Office.Interop.PowerPoint.Application)GetActiveObject(progId);

                if (_pptApplication != null)
                {
                    // 获得演示文稿对象
                    _presentation = _pptApplication.ActivePresentation;

                    // 订阅事件
                    _pptApplication.PresentationOpen += OnPresentationOpen;
                    _pptApplication.PresentationClose += OnPresentationClose;
                    _pptApplication.SlideShowBegin += OnSlideShowBegin;
                    _pptApplication.SlideShowNextSlide += OnSlideShowNextSlide;
                    _pptApplication.SlideShowEnd += OnSlideShowEnd;

                    // 获得幻灯片对象集合
                    _slides = _presentation.Slides;

                    // 获得幻灯片的数量
                    _slidesCount = _slides.Count;
                    _memoryStreams = new MemoryStream[_slidesCount + 2];

                    // 获得当前选中的幻灯片
                    try
                    {
                        // 在普通视图下这种方式可以获得当前选中的幻灯片对象
                        _slide = _slides[_pptApplication.ActiveWindow.Selection.SlideRange.SlideNumber];
                    }
                    catch (Exception ex)
                    {
                        // 在阅读模式下出现异常时，通过下面的方式来获得当前选中的幻灯片对象
                        LogHelper.WriteLogToFile("Error getting slide in normal view: " + ex.Message, LogHelper.LogType.Trace);
                        if (_pptApplication.SlideShowWindows.Count > 0)
                        {
                            _slide = _pptApplication.SlideShowWindows[1].View.Slide;
                        }
                    }

                    LogHelper.WriteLogToFile("Successfully connected to PowerPoint", LogHelper.LogType.Info);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile("Failed to connect to PowerPoint: " + ex.Message, LogHelper.LogType.Error);
                return false;
            }
        }

        /// <summary>
        /// 启动幻灯片放映
        /// </summary>
        public void StartSlideShow()
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    _presentation?.SlideShowSettings.Run();
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile("Failed to start slide show: " + ex.Message, LogHelper.LogType.Error);
                }
            });
        }

        /// <summary>
        /// 结束幻灯片放映
        /// </summary>
        public void EndSlideShow()
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    if (IsSlideShowRunning())
                    {
                        _pptApplication.SlideShowWindows[1].View.Exit();
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile("Failed to end slide show: " + ex.Message, LogHelper.LogType.Error);
                }
            });
        }

        /// <summary>
        /// 切换到上一张幻灯片
        /// </summary>
        public void PreviousSlide()
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                lock (_pptOperationLock)
                {
                    try
                    {
                        if (IsSlideShowRunning())
                        {
                            _pptApplication.SlideShowWindows[1].Activate();
                            _pptApplication.SlideShowWindows[1].View.Previous();
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.WriteLogToFile("Failed to go to previous slide: " + ex.Message, LogHelper.LogType.Error);
                    }
                }
            });
        }

        /// <summary>
        /// 切换到下一张幻灯片
        /// </summary>
        public void NextSlide()
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                lock (_pptOperationLock)
                {
                    try
                    {
                        if (IsSlideShowRunning())
                        {
                            _pptApplication.SlideShowWindows[1].Activate();
                            _pptApplication.SlideShowWindows[1].View.Next();
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.WriteLogToFile("Failed to go to next slide: " + ex.Message, LogHelper.LogType.Error);
                    }
                }
            });
        }

        /// <summary>
        /// 跳转到指定幻灯片
        /// </summary>
        public void GoToSlide(int slideIndex)
        {
            try
            {
                if (IsSlideShowRunning())
                {
                    _pptApplication.SlideShowWindows[1].View.GotoSlide(slideIndex);
                }
                else if (_presentation != null)
                {
                    _presentation.Windows[1].View.GotoSlide(slideIndex);
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"Failed to go to slide {slideIndex}: " + ex.Message, LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 显示幻灯片导航
        /// </summary>
        public void ShowSlideNavigation()
        {
            try
            {
                if (IsSlideShowRunning())
                {
                    _pptApplication.SlideShowWindows[1].SlideNavigation.Visible = true;
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile("Failed to show slide navigation: " + ex.Message, LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 判断幻灯片放映是否正在运行
        /// </summary>
        public bool IsSlideShowRunning()
        {
            try
            {
                return _pptApplication != null && _pptApplication.SlideShowWindows != null && _pptApplication.SlideShowWindows.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 保存墨迹到内存流
        /// </summary>
        public void SaveStrokesToMemory(int slideIndex, MemoryStream stream)
        {
            if (slideIndex >= 0 && slideIndex < _memoryStreams.Length)
            {
                _memoryStreams[slideIndex] = stream;
            }
        }

        /// <summary>
        /// 从内存流加载墨迹
        /// </summary>
        public MemoryStream LoadStrokesFromMemory(int slideIndex)
        {
            if (slideIndex >= 0 && slideIndex < _memoryStreams.Length)
            {
                return _memoryStreams[slideIndex];
            }
            return null;
        }

        /// <summary>
        /// 异步保存墨迹到文件
        /// </summary>
        /// <param name="parameters">保存参数对象</param>
        public async Task SaveStrokesToFileAsync(SaveStrokesParameters parameters)
        {
            try
            {
                await Task.Run(async () =>
                {
                    try
                    {
                        var folderName = parameters.PresentationName + "_" + parameters.SlidesCount;
                        var folderPath = Path.Combine(parameters.RootPath, "Auto Saved - Presentations", folderName);

                        string drive = Path.GetPathRoot(folderPath);
                        var actualFolderPath = folderPath;
                        if (!string.IsNullOrEmpty(drive) && !Directory.Exists(drive))
                        {
                            string fallbackRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FallbackPresentations");
                            actualFolderPath = Path.Combine(fallbackRoot, folderName);
                            LogHelper.WriteLogToFile($"Drive {drive} not found. Falling back to {actualFolderPath}", LogHelper.LogType.Warning);
                        }

                        if (!Directory.Exists(actualFolderPath))
                        {
                            Directory.CreateDirectory(actualFolderPath);
                        }

                        await File.WriteAllTextAsync(Path.Combine(actualFolderPath, "Position"), parameters.PreviousSlideId.ToString());

                        for (var i = 1; i <= parameters.SlidesCount; i++)
                        {
                            if (_memoryStreams[i] != null)
                            {
                                try
                                {
                                    if (_memoryStreams[i].Length > 8)
                                    {
                                        _memoryStreams[i].Position = 0;
                                        var srcBuf = new byte[_memoryStreams[i].Length];
                                        var byteLength = _memoryStreams[i].Read(srcBuf, 0, srcBuf.Length);
                                        await File.WriteAllBytesAsync(Path.Combine(actualFolderPath, i.ToString("0000") + ".icstk"), srcBuf);
                                        LogHelper.WriteLogToFile($"Saved strokes for Slide {i}, size={_memoryStreams[i].Length}, byteLength={byteLength}", LogHelper.LogType.Trace);
                                    }
                                    else
                                    {
                                        var filePath = Path.Combine(actualFolderPath, i.ToString("0000") + ".icstk");
                                        if (File.Exists(filePath))
                                        {
                                            File.Delete(filePath);
                                        }
                                    }
                                }
                                catch (IOException ex)
                                {
                                    LogHelper.WriteLogToFile($"I/O error saving strokes for Slide {i}: {ex.Message}", LogHelper.LogType.Error);
                                    LogHelper.NewLog(ex);
                                }
                                catch (UnauthorizedAccessException ex)
                                {
                                    LogHelper.WriteLogToFile($"Access denied saving strokes for Slide {i}: {ex.Message}", LogHelper.LogType.Error);
                                    LogHelper.NewLog(ex);
                                }
                                catch (Exception ex)
                                {
                                    LogHelper.WriteLogToFile($"Unexpected error saving strokes for Slide {i}: {ex.Message}", LogHelper.LogType.Error);
                                    LogHelper.NewLog(ex);
                                }
                            }
                        }
                    }
                    catch (IOException ex)
                    {
                        LogHelper.WriteLogToFile($"I/O error during stroke saving process: {ex.Message}", LogHelper.LogType.Error);
                        LogHelper.NewLog(ex);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        LogHelper.WriteLogToFile($"Access denied during stroke saving process: {ex.Message}", LogHelper.LogType.Error);
                        LogHelper.NewLog(ex);
                    }
                    catch (Exception ex)
                    {
                        LogHelper.WriteLogToFile($"Unexpected error during stroke saving process: {ex.Message}", LogHelper.LogType.Error);
                        LogHelper.NewLog(ex);
                    }
                });
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"Failed to start async stroke saving task: {ex.Message}", LogHelper.LogType.Error);
                LogHelper.NewLog(ex);
            }
        }

        /// <summary>
        /// 从文件加载墨迹
        /// </summary>
        public async Task<int> LoadStrokesFromFileAsync(string presentationName, int slidesCount, string rootPath)
        {
            try
            {
                var folderPath = Path.Combine(rootPath, "Auto Saved - Presentations", presentationName + "_" + slidesCount);

                if (Directory.Exists(folderPath))
                {
                    LogHelper.WriteLogToFile("Found saved strokes", LogHelper.LogType.Trace);
                    var files = new DirectoryInfo(folderPath).GetFiles();
                    var count = 0;

                    foreach (var file in files)
                    {
                        if (file.Name != "Position")
                        {
                            try
                            {
                                var i = int.Parse(Path.GetFileNameWithoutExtension(file.Name));
                                _memoryStreams[i] = new MemoryStream(await File.ReadAllBytesAsync(file.FullName));
                                _memoryStreams[i].Position = 0;
                                count++;
                            }
                            catch (Exception ex)
                            {
                                LogHelper.WriteLogToFile($"Failed to load strokes: {ex.Message}", LogHelper.LogType.Error);
                            }
                        }
                    }

                    LogHelper.WriteLogToFile($"Loaded {count} saved strokes", LogHelper.LogType.Info);
                    return count;
                }

                return 0;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile("Error loading strokes from file: " + ex.Message, LogHelper.LogType.Error);
                return 0;
            }
        }

        /// <summary>
        /// 检查是否有隐藏的幻灯片
        /// </summary>
        public bool HasHiddenSlides()
        {
            try
            {
                if (_slides == null) return false;

                foreach (Slide slide in _slides)
                {
                    if (slide.SlideShowTransition.Hidden == MsoTriState.msoTrue)
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile("Error checking for hidden slides: " + ex.Message, LogHelper.LogType.Error);
                return false;
            }
        }

        /// <summary>
        /// 取消隐藏所有幻灯片
        /// </summary>
        public void UnhideAllSlides()
        {
            try
            {
                if (_slides == null) return;

                foreach (Slide slide in _slides)
                {
                    if (slide.SlideShowTransition.Hidden == MsoTriState.msoTrue)
                    {
                        slide.SlideShowTransition.Hidden = MsoTriState.msoFalse;
                    }
                }

                LogHelper.WriteLogToFile("Unhid all slides", LogHelper.LogType.Info);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile("Error unhiding slides: " + ex.Message, LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 检查是否启用了自动播放
        /// </summary>
        public bool HasAutoPlayEnabled()
        {
            try
            {
                if (_presentation == null || _presentation.Slides == null) return false;

                foreach (Slide slide in _presentation.Slides)
                {
                    if (slide.SlideShowTransition.AdvanceOnTime == MsoTriState.msoTrue &&
                        slide.SlideShowTransition.AdvanceTime > 0)
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile("Error checking for auto play: " + ex.Message, LogHelper.LogType.Error);
                return false;
            }
        }

        /// <summary>
        /// 禁用自动播放
        /// </summary>
        public void DisableAutoPlay()
        {
            try
            {
                if (_presentation != null)
                {
                    _presentation.SlideShowSettings.AdvanceMode = PpSlideShowAdvanceMode.ppSlideShowManualAdvance;
                    LogHelper.WriteLogToFile("Disabled auto play", LogHelper.LogType.Info);
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile("Error disabling auto play: " + ex.Message, LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 释放 COM 对象资源
        /// </summary>
        public void ReleaseComObjects()
        {
            try
            {
                if (_pptApplication != null)
                {
                    try
                    {
                        // 取消订阅所有 PPT 事件
                        _pptApplication.PresentationOpen -= OnPresentationOpen;
                        _pptApplication.PresentationClose -= OnPresentationClose;
                        _pptApplication.SlideShowBegin -= OnSlideShowBegin;
                        _pptApplication.SlideShowNextSlide -= OnSlideShowNextSlide;
                        _pptApplication.SlideShowEnd -= OnSlideShowEnd;
                    }
                    catch (Exception ex)
                    {
                        LogHelper.WriteLogToFile("Error unsubscribing PPT events: " + ex.Message, LogHelper.LogType.Warning);
                    }

                    // 释放 COM 对象引用
                    try
                    {
                        Marshal.ReleaseComObject(_pptApplication);
                        LogHelper.WriteLogToFile("PPT Application COM object released", LogHelper.LogType.Info);
                    }
                    catch (Exception ex)
                    {
                        LogHelper.WriteLogToFile("Error releasing pptApplication COM object: " + ex.Message, LogHelper.LogType.Error);
                    }
                    _pptApplication = null;
                }

                if (_presentation != null)
                {
                    try
                    {
                        Marshal.ReleaseComObject(_presentation);
                        LogHelper.WriteLogToFile("Presentation COM object released", LogHelper.LogType.Info);
                    }
                    catch (Exception ex)
                    {
                        LogHelper.WriteLogToFile("Error releasing presentation COM object: " + ex.Message, LogHelper.LogType.Error);
                    }
                    _presentation = null;
                }

                if (_slides != null)
                {
                    try
                    {
                        Marshal.ReleaseComObject(_slides);
                        LogHelper.WriteLogToFile("Slides COM object released", LogHelper.LogType.Info);
                    }
                    catch (Exception ex)
                    {
                        LogHelper.WriteLogToFile("Error releasing slides COM object: " + ex.Message, LogHelper.LogType.Error);
                    }
                    _slides = null;
                }

                if (_slide != null)
                {
                    try
                    {
                        Marshal.ReleaseComObject(_slide);
                        LogHelper.WriteLogToFile("Slide COM object released", LogHelper.LogType.Info);
                    }
                    catch (Exception ex)
                    {
                        LogHelper.WriteLogToFile("Error releasing slide COM object: " + ex.Message, LogHelper.LogType.Error);
                    }
                    _slide = null;
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile("Error releasing PPT COM objects: " + ex.Message, LogHelper.LogType.Error);
            }
        }

        #endregion

        #region 事件处理

        private void OnPresentationOpen(Presentation pres)
        {
            PresentationOpened?.Invoke(pres);
        }

        private void OnPresentationClose(Presentation pres)
        {
            PresentationClosed?.Invoke(pres);
        }

        private void OnSlideShowBegin(SlideShowWindow wn)
        {
            SlideShowBegin?.Invoke(wn);
        }

        private void OnSlideShowNextSlide(SlideShowWindow wn)
        {
            SlideShowNextSlide?.Invoke(wn);
        }

        private void OnSlideShowEnd(Presentation pres)
        {
            SlideShowEnd?.Invoke(pres);
        }

        #endregion
    }
}
