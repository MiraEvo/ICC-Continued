using Ink_Canvas.Helpers;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.PowerPoint;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms; // For Screen class in WPS detection

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

        // 设置服务，用于访问应用程序配置
        private readonly ISettingsService _settingsService;

        #region 优先级判断相关

        /// <summary>
        /// PPT 绑定优先级枚举
        /// 用于判断应该绑定到哪个 PowerPoint 实例
        /// </summary>
        public enum PPTBindingPriority
        {
            /// <summary>
            /// 级别 0: 无效或无法访问
            /// </summary>
            None = 0,

            /// <summary>
            /// 级别 1: 基础 - 有 Application 和 ActivePresentation
            /// </summary>
            Level1_Basic = 1,

            /// <summary>
            /// 级别 2: 有放映窗口 - 有 SlideShowWindow
            /// </summary>
            Level2_HasSlideShow = 2,

            /// <summary>
            /// 级别 3: 窗口激活 - 放映窗口是激活状态或为焦点
            /// </summary>
            Level3_WindowActivated = 3
        }

        /// <summary>
        /// PPT 绑定目标信息
        /// 存储一个 PowerPoint 实例的绑定信息和优先级
        /// </summary>
        public class PPTTarget
        {
            /// <summary>
            /// PowerPoint Application 对象（dynamic 类型）
            /// </summary>
            public dynamic Application { get; set; }

            /// <summary>
            /// 当前活动的演示文稿对象（dynamic 类型）
            /// </summary>
            public dynamic Presentation { get; set; }

            /// <summary>
            /// 幻灯片放映窗口对象（dynamic 类型）
            /// </summary>
            public dynamic SlideShowWindow { get; set; }

            /// <summary>
            /// 绑定优先级
            /// </summary>
            public PPTBindingPriority Priority { get; set; }

            /// <summary>
            /// 演示文稿名称（用于日志和调试）
            /// </summary>
            public string PresentationName { get; set; }

            /// <summary>
            /// 应用程序名称（PowerPoint 或 WPS，用于特殊处理）
            /// </summary>
            public string ApplicationName { get; set; }

            /// <summary>
            /// 放映窗口句柄（如果可获取）
            /// </summary>
            public IntPtr SlideShowWindowHWND { get; set; }

            /// <summary>
            /// 构造函数
            /// </summary>
            public PPTTarget()
            {
                Priority = PPTBindingPriority.None;
                SlideShowWindowHWND = IntPtr.Zero;
            }
        }

        /// <summary>
        /// 当前绑定的 PPT 目标
        /// </summary>
        private PPTTarget _currentTarget = null;

        #endregion

        #region 轮询和事件注册双模式

        /// <summary>
        /// 事件注册是否成功
        /// </summary>
        private bool _eventRegistrationSucceeded = false;

        /// <summary>
        /// 轮询定时器
        /// </summary>
        private System.Threading.Timer _pollingTimer = null;

        /// <summary>
        /// 轮询间隔（毫秒）
        /// 事件注册失败时使用 500ms 快速轮询
        /// 事件注册成功时使用 3000ms 慢速轮询作为备份
        /// </summary>
        private int _pollingInterval = 500;

        /// <summary>
        /// 上一次轮询获取的幻灯片索引
        /// 用于检测幻灯片切换
        /// </summary>
        private int _lastPolledSlideIndex = -1;

        /// <summary>
        /// 轮询是否正在运行
        /// </summary>
        private bool _isPollingActive = false;

        #endregion

        #region ROT 扫描频率优化

        /// <summary>
        /// ROT 扫描定时器
        /// 用于定期扫描 ROT 以检测新的 PowerPoint 实例或窗口切换
        /// </summary>
        private System.Threading.Timer _rotScanTimer = null;

        /// <summary>
        /// ROT 扫描间隔（毫秒）
        /// 默认 5000ms (5秒)，可根据活动情况动态调整
        /// </summary>
        private int _rotScanInterval = 5000;

        /// <summary>
        /// 最小 ROT 扫描间隔（毫秒）
        /// 防止扫描过于频繁
        /// </summary>
        private const int MIN_ROT_SCAN_INTERVAL = 2000; // 2秒

        /// <summary>
        /// 最大 ROT 扫描间隔（毫秒）
        /// 在稳定状态下使用较长的扫描间隔
        /// </summary>
        private const int MAX_ROT_SCAN_INTERVAL = 15000; // 15秒

        /// <summary>
        /// 上一次 ROT 扫描时间
        /// 用于性能监控
        /// </summary>
        private DateTime _lastRotScanTime = DateTime.MinValue;

        /// <summary>
        /// 上一次 ROT 扫描耗时（毫秒）
        /// 用于性能监控和自适应调整
        /// </summary>
        private long _lastRotScanDuration = 0;

        /// <summary>
        /// ROT 扫描次数统计
        /// 用于性能监控
        /// </summary>
        private int _rotScanCount = 0;

        /// <summary>
        /// ROT 扫描总耗时（毫秒）
        /// 用于计算平均扫描时间
        /// </summary>
        private long _rotScanTotalDuration = 0;

        /// <summary>
        /// 连续未发现变化的 ROT 扫描次数
        /// 用于自适应调整扫描间隔
        /// </summary>
        private int _consecutiveUnchangedScans = 0;

        /// <summary>
        /// ROT 扫描是否正在运行
        /// 防止并发扫描
        /// </summary>
        private bool _isRotScanRunning = false;

        /// <summary>
        /// 上一次扫描到的 PowerPoint 实例数量
        /// 用于检测实例数量变化
        /// </summary>
        private int _lastPptInstanceCount = 0;

        /// <summary>
        /// 是否启用调试模式
        /// 调试模式下会输出更详细的日志
        /// </summary>
        private bool _debugMode = false;

        #endregion

        #region COM 对象生命周期管理

        /// <summary>
        /// COM 对象跟踪器
        /// 用于跟踪和管理活动的 COM 对象，确保正确释放
        /// </summary>
        private class ComObjectTracker : IDisposable
        {
            private readonly List<WeakReference> _trackedObjects = new List<WeakReference>();
            private readonly object _lock = new object();
            private bool _disposed = false;

            /// <summary>
            /// 跟踪一个 COM 对象
            /// </summary>
            /// <param name="comObject">要跟踪的 COM 对象</param>
            public void Track(object comObject)
            {
                if (comObject == null || !Marshal.IsComObject(comObject))
                {
                    return;
                }

                lock (_lock)
                {
                    // 使用 WeakReference 避免阻止垃圾回收
                    _trackedObjects.Add(new WeakReference(comObject));
                    LogHelper.WriteLogToFile($"ComObjectTracker: Now tracking {_trackedObjects.Count} COM objects", LogHelper.LogType.Trace);
                }
            }

            /// <summary>
            /// 停止跟踪一个 COM 对象（不释放）
            /// </summary>
            /// <param name="comObject">要停止跟踪的 COM 对象</param>
            public void Untrack(object comObject)
            {
                if (comObject == null)
                {
                    return;
                }

                lock (_lock)
                {
                    _trackedObjects.RemoveAll(wr => !wr.IsAlive || ReferenceEquals(wr.Target, comObject));
                    LogHelper.WriteLogToFile($"ComObjectTracker: Now tracking {_trackedObjects.Count} COM objects", LogHelper.LogType.Trace);
                }
            }

            /// <summary>
            /// 释放所有跟踪的 COM 对象
            /// </summary>
            public void ReleaseAll()
            {
                lock (_lock)
                {
                    int releasedCount = 0;
                    int failedCount = 0;

                    foreach (var weakRef in _trackedObjects)
                    {
                        if (weakRef.IsAlive)
                        {
                            var comObject = weakRef.Target;
                            if (comObject != null && Marshal.IsComObject(comObject))
                            {
                                try
                                {
                                    Marshal.ReleaseComObject(comObject);
                                    releasedCount++;
                                }
                                catch (Exception ex)
                                {
                                    LogHelper.WriteLogToFile($"ComObjectTracker: Failed to release COM object: {ex.Message}", LogHelper.LogType.Warning);
                                    failedCount++;
                                }
                            }
                        }
                    }

                    _trackedObjects.Clear();
                    LogHelper.WriteLogToFile($"ComObjectTracker: Released {releasedCount} COM objects, {failedCount} failed", LogHelper.LogType.Info);
                }
            }

            /// <summary>
            /// 获取当前跟踪的 COM 对象数量
            /// </summary>
            public int TrackedCount
            {
                get
                {
                    lock (_lock)
                    {
                        // 清理已被垃圾回收的对象
                        _trackedObjects.RemoveAll(wr => !wr.IsAlive);
                        return _trackedObjects.Count;
                    }
                }
            }

            /// <summary>
            /// 验证引用计数（用于调试）
            /// </summary>
            public void ValidateReferenceCounts()
            {
                lock (_lock)
                {
                    int aliveCount = 0;
                    int deadCount = 0;

                    foreach (var weakRef in _trackedObjects)
                    {
                        if (weakRef.IsAlive)
                        {
                            aliveCount++;
                        }
                        else
                        {
                            deadCount++;
                        }
                    }

                    if (deadCount > 0)
                    {
                        LogHelper.WriteLogToFile($"ComObjectTracker: Validation - {aliveCount} alive, {deadCount} garbage collected", LogHelper.LogType.Trace);
                    }

                    if (aliveCount > 10)
                    {
                        LogHelper.WriteLogToFile($"ComObjectTracker: WARNING - High number of tracked COM objects: {aliveCount}", LogHelper.LogType.Warning);
                    }
                }
            }

            /// <summary>
            /// 释放资源
            /// </summary>
            public void Dispose()
            {
                if (!_disposed)
                {
                    ReleaseAll();
                    _disposed = true;
                }
            }
        }

        /// <summary>
        /// COM 对象包装器，实现自动释放
        /// 使用 using 语句确保 COM 对象在使用后被释放
        /// </summary>
        private class ComObjectWrapper : IDisposable
        {
            private object _comObject;
            private bool _disposed = false;
            private readonly string _objectName;

            public ComObjectWrapper(object comObject, string objectName = "Unknown")
            {
                _comObject = comObject;
                _objectName = objectName;
                
                if (_comObject != null && Marshal.IsComObject(_comObject))
                {
                    LogHelper.WriteLogToFile($"ComObjectWrapper: Created wrapper for {_objectName}", LogHelper.LogType.Trace);
                }
            }

            /// <summary>
            /// 获取包装的 COM 对象
            /// </summary>
            public object Object => _comObject;

            /// <summary>
            /// 获取包装的 COM 对象（dynamic 类型）
            /// </summary>
            public dynamic DynamicObject => _comObject;

            /// <summary>
            /// 释放 COM 对象
            /// </summary>
            public void Dispose()
            {
                if (!_disposed && _comObject != null)
                {
                    if (Marshal.IsComObject(_comObject))
                    {
                        try
                        {
                            int refCount = Marshal.ReleaseComObject(_comObject);
                            LogHelper.WriteLogToFile($"ComObjectWrapper: Released {_objectName}, remaining ref count = {refCount}", LogHelper.LogType.Trace);
                        }
                        catch (Exception ex)
                        {
                            LogHelper.WriteLogToFile($"ComObjectWrapper: Error releasing {_objectName}: {ex.Message}", LogHelper.LogType.Warning);
                        }
                    }
                    
                    _comObject = null;
                    _disposed = true;
                }
            }
        }

        /// <summary>
        /// 全局 COM 对象跟踪器实例
        /// </summary>
        private readonly ComObjectTracker _comObjectTracker = new ComObjectTracker();

        /// <summary>
        /// 创建一个 COM 对象包装器，用于自动释放
        /// </summary>
        /// <param name="comObject">COM 对象</param>
        /// <param name="objectName">对象名称（用于日志）</param>
        /// <returns>COM 对象包装器</returns>
        private ComObjectWrapper WrapComObject(object comObject, string objectName = "Unknown")
        {
            return new ComObjectWrapper(comObject, objectName);
        }

        /// <summary>
        /// 跟踪一个 COM 对象
        /// </summary>
        /// <param name="comObject">要跟踪的 COM 对象</param>
        private void TrackComObject(object comObject)
        {
            _comObjectTracker.Track(comObject);
        }

        /// <summary>
        /// 停止跟踪一个 COM 对象
        /// </summary>
        /// <param name="comObject">要停止跟踪的 COM 对象</param>
        private void UntrackComObject(object comObject)
        {
            _comObjectTracker.Untrack(comObject);
        }

        /// <summary>
        /// 释放所有跟踪的 COM 对象
        /// </summary>
        private void ReleaseAllTrackedComObjects()
        {
            _comObjectTracker.ReleaseAll();
        }

        /// <summary>
        /// 验证 COM 对象引用计数（用于调试）
        /// </summary>
        private void ValidateComObjectReferenceCounts()
        {
            _comObjectTracker.ValidateReferenceCounts();
        }

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="settingsService">设置服务（可选，用于访问 PPT 联动增强设置）</param>
        public PPTService(ISettingsService settingsService = null)
        {
            _settingsService = settingsService;
            
            if (_settingsService == null)
            {
                LogHelper.WriteLogToFile("PPTService：未注入 SettingsService，增强模式将被禁用", LogHelper.LogType.Warning);
            }
            else
            {
                LogHelper.WriteLogToFile("PPTService：已注入 SettingsService", LogHelper.LogType.Info);
            }
        }

        #endregion

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

        [RequiresUnmanagedCode("Uses ole32/oleaut32 COM interop to fetch running PowerPoint instance.")]
        private static object GetActiveObject(string progId)
        {
            Guid clsid;
            CLSIDFromProgIDEx(progId, out clsid);
            GetActiveObject(ref clsid, IntPtr.Zero, out object obj);
            return obj;
        }

        #region ROT (Running Object Table) 相关

        /// <summary>
        /// 获取运行对象表 (Running Object Table)
        /// </summary>
        [DllImport("ole32.dll")]
        private static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable prot);

        /// <summary>
        /// 创建绑定上下文
        /// </summary>
        [DllImport("ole32.dll")]
        private static extern int CreateBindCtx(int reserved, out IBindCtx ppbc);

        /// <summary>
        /// 运行对象表接口
        /// 用于枚举系统中所有正在运行的 COM 对象
        /// </summary>
        [ComImport]
        [Guid("00000010-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IRunningObjectTable
        {
            int Register(int grfFlags, [MarshalAs(UnmanagedType.IUnknown)] object punkObject, IMoniker pmkObjectName);
            int Revoke(int dwRegister);
            int IsRunning(IMoniker pmkObjectName);
            int GetObject(IMoniker pmkObjectName, [MarshalAs(UnmanagedType.IUnknown)] out object ppunkObject);
            int NoteChangeTime(int dwRegister, ref System.Runtime.InteropServices.ComTypes.FILETIME pfiletime);
            int GetTimeOfLastChange(IMoniker pmkObjectName, out System.Runtime.InteropServices.ComTypes.FILETIME pfiletime);
            int EnumRunning(out IEnumMoniker ppenumMoniker);
        }

        /// <summary>
        /// Moniker 枚举器接口
        /// 用于遍历 ROT 中的所有 Moniker
        /// </summary>
        [ComImport]
        [Guid("00000102-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IEnumMoniker
        {
            [PreserveSig]
            int Next(int celt, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.Interface)] IMoniker[] rgelt, IntPtr pceltFetched);
            [PreserveSig]
            int Skip(int celt);
            void Reset();
            void Clone(out IEnumMoniker ppenum);
        }

        /// <summary>
        /// Moniker 接口
        /// 表示 COM 对象的名称
        /// </summary>
        [ComImport]
        [Guid("0000000f-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMoniker
        {
            void GetClassID(out Guid pClassID);
            [PreserveSig]
            int IsDirty();
            void Load(System.Runtime.InteropServices.ComTypes.IStream pStm);
            void Save(System.Runtime.InteropServices.ComTypes.IStream pStm, [MarshalAs(UnmanagedType.Bool)] bool fClearDirty);
            void GetSizeMax(out long pcbSize);
            void BindToObject(IBindCtx pbc, IMoniker pmkToLeft, [In] ref Guid riidResult, [MarshalAs(UnmanagedType.IUnknown)] out object ppvResult);
            void BindToStorage(IBindCtx pbc, IMoniker pmkToLeft, [In] ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppvObj);
            void Reduce(IBindCtx pbc, int dwReduceHowFar, ref IMoniker ppmkToLeft, out IMoniker ppmkReduced);
            void ComposeWith(IMoniker pmkRight, [MarshalAs(UnmanagedType.Bool)] bool fOnlyIfNotGeneric, out IMoniker ppmkComposite);
            void Enum([MarshalAs(UnmanagedType.Bool)] bool fForward, out IEnumMoniker ppenumMoniker);
            [PreserveSig]
            int IsEqual(IMoniker pmkOtherMoniker);
            void Hash(out int pdwHash);
            [PreserveSig]
            int IsRunning(IBindCtx pbc, IMoniker pmkToLeft, IMoniker pmkNewlyRunning);
            void GetTimeOfLastChange(IBindCtx pbc, IMoniker pmkToLeft, out System.Runtime.InteropServices.ComTypes.FILETIME pFileTime);
            void Inverse(out IMoniker ppmk);
            void CommonPrefixWith(IMoniker pmkOther, out IMoniker ppmkPrefix);
            void RelativePathTo(IMoniker pmkOther, out IMoniker ppmkRelPath);
            void GetDisplayName(IBindCtx pbc, IMoniker pmkToLeft, [MarshalAs(UnmanagedType.LPWStr)] out string ppszDisplayName);
            void ParseDisplayName(IBindCtx pbc, IMoniker pmkToLeft, [MarshalAs(UnmanagedType.LPWStr)] string pszDisplayName, out int pchEaten, out IMoniker ppmkOut);
            [PreserveSig]
            int IsSystemMoniker(out int pdwMonikerType);
        }

        /// <summary>
        /// 绑定上下文接口
        /// 用于 Moniker 绑定操作
        /// </summary>
        [ComImport]
        [Guid("0000000e-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IBindCtx
        {
            void RegisterObjectBound([MarshalAs(UnmanagedType.IUnknown)] object punk);
            void RevokeObjectBound([MarshalAs(UnmanagedType.IUnknown)] object punk);
            void ReleaseBoundObjects();
            void SetBindOptions([In] ref BIND_OPTS pbindopts);
            void GetBindOptions(ref BIND_OPTS pbindopts);
            void GetRunningObjectTable(out IRunningObjectTable pprot);
            void RegisterObjectParam([MarshalAs(UnmanagedType.LPWStr)] string pszKey, [MarshalAs(UnmanagedType.IUnknown)] object punk);
            void GetObjectParam([MarshalAs(UnmanagedType.LPWStr)] string pszKey, [MarshalAs(UnmanagedType.IUnknown)] out object ppunk);
            void EnumObjectParam(out IEnumString ppenum);
            [PreserveSig]
            int RevokeObjectParam([MarshalAs(UnmanagedType.LPWStr)] string pszKey);
        }

        /// <summary>
        /// 字符串枚举器接口
        /// </summary>
        [ComImport]
        [Guid("00000101-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IEnumString
        {
            [PreserveSig]
            int Next(int celt, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 0)] string[] rgelt, IntPtr pceltFetched);
            [PreserveSig]
            int Skip(int celt);
            void Reset();
            void Clone(out IEnumString ppenum);
        }

        /// <summary>
        /// 绑定选项结构
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct BIND_OPTS
        {
            public int cbStruct;
            public int grfFlags;
            public int grfMode;
            public int dwTickCountDeadline;
        }

        /// <summary>
        /// PowerPoint.Application 的经典 GUID
        /// 用于在 ROT 中识别 PowerPoint 应用程序
        /// </summary>
        private static readonly Guid PowerPointApplicationGuid = new Guid("91493441-5A91-11CF-8700-00AA0060263B");

        /// <summary>
        /// PowerPoint 支持的文件扩展名列表
        /// 用于在 ROT 中通过文件名匹配 PowerPoint 演示文稿
        /// </summary>
        private static readonly string[] PowerPointExtensions = new[]
        {
            ".ppt", ".pptx", ".pptm", ".ppsx", ".ppsm", ".pps",
            ".pot", ".potx", ".potm", ".odp", ".otp", ".thmx"
        };

        #endregion

        #region ROT 查找和枚举

        /// <summary>
        /// 枚举运行对象表 (ROT) 中的所有 PowerPoint 应用程序
        /// 通过 GUID 匹配和文件扩展名匹配来识别 PowerPoint 实例
        /// </summary>
        /// <returns>匹配的 COM 对象列表</returns>
        [RequiresUnmanagedCode("Uses ole32 COM interop to enumerate running object table.")]
        private List<object> EnumerateRunningObjectTable()
        {
            var results = new List<object>();

            try
            {
                // 获取运行对象表
                int hr = GetRunningObjectTable(0, out IRunningObjectTable rot);
                if (hr != 0)
                {
                    LogHelper.WriteLogToFile($"获取 ROT 失败，HRESULT：0x{hr:X8}", LogHelper.LogType.Error);
                    return results;
                }

                // 枚举 ROT 中的所有 Moniker
                rot.EnumRunning(out IEnumMoniker enumMoniker);
                if (enumMoniker == null)
                {
                    LogHelper.WriteLogToFile("ROT 中枚举 Moniker 失败", LogHelper.LogType.Error);
                    Marshal.ReleaseComObject(rot);
                    return results;
                }

                // 创建绑定上下文
                hr = CreateBindCtx(0, out IBindCtx bindCtx);
                if (hr != 0)
                {
                    LogHelper.WriteLogToFile($"创建绑定上下文失败，HRESULT：0x{hr:X8}", LogHelper.LogType.Error);
                    Marshal.ReleaseComObject(enumMoniker);
                    Marshal.ReleaseComObject(rot);
                    return results;
                }

                IMoniker[] monikers = new IMoniker[1];
                IntPtr fetched = IntPtr.Zero;

                // 遍历所有 Moniker
                while (enumMoniker.Next(1, monikers, fetched) == 0)
                {
                    IMoniker moniker = monikers[0];
                    if (moniker == null) continue;

                    try
                    {
                        // 获取 Moniker 的显示名称
                        moniker.GetDisplayName(bindCtx, null, out string displayName);

                        if (!string.IsNullOrEmpty(displayName))
                        {
                            LogHelper.WriteLogToFile($"ROT：发现 Moniker：{displayName}", LogHelper.LogType.Trace);

                            bool isMatch = false;

                            // 方法 1: 通过 PowerPoint.Application GUID 匹配
                            if (displayName.Contains(PowerPointApplicationGuid.ToString("B").ToUpper()))
                            {
                                isMatch = true;
                                LogHelper.WriteLogToFile($"ROT：通过 PowerPoint GUID 匹配：{displayName}", LogHelper.LogType.Info);
                            }

                            // 方法 2: 通过文件扩展名匹配
                            if (!isMatch)
                            {
                                foreach (var ext in PowerPointExtensions)
                                {
                                    if (displayName.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                                    {
                                        isMatch = true;
                                        LogHelper.WriteLogToFile($"ROT：通过扩展名 {ext} 匹配：{displayName}", LogHelper.LogType.Info);
                                        break;
                                    }
                                }
                            }

                            // 如果匹配成功，获取 COM 对象
                            if (isMatch)
                            {
                                try
                                {
                                    rot.GetObject(moniker, out object comObject);
                                    if (comObject != null)
                                    {
                                        results.Add(comObject);
                                        LogHelper.WriteLogToFile($"ROT：成功获取 COM 对象：{displayName}", LogHelper.LogType.Info);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    LogHelper.WriteLogToFile($"ROT：获取 COM 对象失败 {displayName}：{ex.Message}", LogHelper.LogType.Warning);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.WriteLogToFile($"ROT：处理 Moniker 失败：{ex.Message}", LogHelper.LogType.Warning);
                    }
                    finally
                    {
                        // 释放 Moniker
                        try
                        {
                            Marshal.ReleaseComObject(moniker);
                        }
                        catch (Exception ex)
                        {
                            LogHelper.WriteLogToFile($"ROT：释放 Moniker 失败：{ex.Message}", LogHelper.LogType.Warning);
                        }
                    }
                }

                // 释放资源 - 精确管理引用计数
                try
                {
                    SafeReleaseComObject(bindCtx);
                    SafeReleaseComObject(enumMoniker);
                    SafeReleaseComObject(rot);
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"ROT：释放 COM 对象失败：{ex.Message}", LogHelper.LogType.Warning);
                }

                LogHelper.WriteLogToFile($"ROT：发现 {results.Count} 个 PowerPoint 实例", LogHelper.LogType.Info);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"ROT：枚举失败：{ex.Message}", LogHelper.LogType.Error);
                LogHelper.NewLog(ex);
            }

            return results;
        }

        /// <summary>
        /// 启动 ROT 扫描定时器
        /// 定期扫描 ROT 以检测新的 PowerPoint 实例或窗口切换
        /// </summary>
        private void StartRotScanning()
        {
            try
            {
                if (_rotScanTimer != null)
                {
                    LogHelper.WriteLogToFile("StartRotScanning：ROT 扫描定时器已在运行", LogHelper.LogType.Trace);
                    return;
                }

                LogHelper.WriteLogToFile($"StartRotScanning：启动 ROT 扫描定时器，间隔 {_rotScanInterval}ms", LogHelper.LogType.Info);
                
                _rotScanTimer = new System.Threading.Timer(
                    RotScanTimerCallback,
                    null,
                    _rotScanInterval,
                    _rotScanInterval);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"StartRotScanning：启动 ROT 扫描定时器失败：{ex.Message}", LogHelper.LogType.Error);
                LogHelper.NewLog(ex);
            }
        }

        /// <summary>
        /// 停止 ROT 扫描定时器
        /// </summary>
        private void StopRotScanning()
        {
            try
            {
                if (_rotScanTimer != null)
                {
                    LogHelper.WriteLogToFile("StopRotScanning：停止 ROT 扫描定时器", LogHelper.LogType.Info);
                    _rotScanTimer.Dispose();
                    _rotScanTimer = null;
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"StopRotScanning：停止 ROT 扫描定时器失败：{ex.Message}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// ROT 扫描定时器回调
        /// 执行 ROT 扫描并根据结果调整扫描间隔
        /// </summary>
        /// <param name="state">状态对象（未使用）</param>
        private void RotScanTimerCallback(object state)
        {
            // 防止并发扫描
            if (_isRotScanRunning)
            {
                if (_debugMode)
                {
                    LogHelper.WriteLogToFile("RotScanTimerCallback：上次扫描仍在进行，跳过本次", LogHelper.LogType.Trace);
                }
                return;
            }

            _isRotScanRunning = true;

            try
            {
                // 记录扫描开始时间
                var scanStartTime = DateTime.Now;
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                if (_debugMode)
                {
                    LogHelper.WriteLogToFile($"RotScanTimerCallback：开始 ROT 扫描 #{_rotScanCount + 1}", LogHelper.LogType.Trace);
                }

                // 执行 ROT 扫描
                List<object> pptInstances = EnumerateRunningObjectTable();
                
                stopwatch.Stop();
                _lastRotScanDuration = stopwatch.ElapsedMilliseconds;
                _lastRotScanTime = scanStartTime;
                _rotScanCount++;
                _rotScanTotalDuration += _lastRotScanDuration;

                // 性能监控日志
                LogHelper.WriteLogToFile(
                    $"RotScan 性能：第 #{_rotScanCount} 次扫描耗时 {_lastRotScanDuration}ms，" +
                    $"发现 {pptInstances?.Count ?? 0} 个实例，" +
                    $"平均耗时：{(_rotScanTotalDuration / _rotScanCount)}ms",
                    LogHelper.LogType.Info);

                // 检测实例数量变化
                int currentInstanceCount = pptInstances?.Count ?? 0;
                bool instanceCountChanged = currentInstanceCount != _lastPptInstanceCount;

                if (instanceCountChanged)
                {
                    LogHelper.WriteLogToFile(
                        $"RotScanTimerCallback：PowerPoint 实例数量从 {_lastPptInstanceCount} 变为 {currentInstanceCount}",
                        LogHelper.LogType.Info);
                    
                    _lastPptInstanceCount = currentInstanceCount;
                    _consecutiveUnchangedScans = 0;

                    // 实例数量变化，可能需要重新计算优先级和切换绑定
                    HandleRotScanInstanceChange(pptInstances);
                }
                else
                {
                    _consecutiveUnchangedScans++;
                    
                    if (_debugMode)
                    {
                        LogHelper.WriteLogToFile(
                            $"RotScanTimerCallback：未检测到变化，连续未变化次数：{_consecutiveUnchangedScans}",
                            LogHelper.LogType.Trace);
                    }
                }

                // 自适应调整扫描间隔
                AdjustRotScanInterval(instanceCountChanged, _lastRotScanDuration);

                // 释放扫描到的 COM 对象（如果不需要使用）
                if (pptInstances != null && !instanceCountChanged)
                {
                    foreach (var instance in pptInstances)
                    {
                        SafeReleaseComObject(instance);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"RotScanTimerCallback：ROT 扫描时发生错误：{ex.Message}", LogHelper.LogType.Error);
                LogHelper.NewLog(ex);
            }
            finally
            {
                _isRotScanRunning = false;
            }
        }

        /// <summary>
        /// 自适应调整 ROT 扫描间隔
        /// 根据扫描结果和性能指标动态调整扫描频率
        /// </summary>
        /// <param name="instanceCountChanged">实例数量是否发生变化</param>
        /// <param name="scanDuration">本次扫描耗时（毫秒）</param>
        private void AdjustRotScanInterval(bool instanceCountChanged, long scanDuration)
        {
            int oldInterval = _rotScanInterval;
            int newInterval = _rotScanInterval;

            try
            {
                // 策略 1: 如果检测到变化，缩短扫描间隔以快速响应
                if (instanceCountChanged)
                {
                    newInterval = Math.Max(MIN_ROT_SCAN_INTERVAL, _rotScanInterval / 2);
                    LogHelper.WriteLogToFile(
                        $"AdjustRotScanInterval：实例发生变化，扫描间隔从 {oldInterval}ms 降至 {newInterval}ms",
                        LogHelper.LogType.Info);
                }
                // 策略 2: 如果连续多次未检测到变化，逐渐延长扫描间隔
                else if (_consecutiveUnchangedScans >= 3)
                {
                    // 每 3 次未变化，增加 2 秒扫描间隔
                    int increment = (_consecutiveUnchangedScans / 3) * 2000;
                    newInterval = Math.Min(MAX_ROT_SCAN_INTERVAL, _rotScanInterval + increment);
                    
                    if (newInterval != oldInterval)
                    {
                        LogHelper.WriteLogToFile(
                            $"AdjustRotScanInterval：连续 {_consecutiveUnchangedScans} 次未变化，" +
                            $"扫描间隔从 {oldInterval}ms 增至 {newInterval}ms",
                            LogHelper.LogType.Info);
                    }
                }

                // 策略 3: 如果扫描耗时过长，延长扫描间隔以减少性能影响
                if (scanDuration > 1000) // 扫描超过 1 秒
                {
                    newInterval = Math.Min(MAX_ROT_SCAN_INTERVAL, Math.Max(newInterval, (int)(scanDuration * 3)));
                    LogHelper.WriteLogToFile(
                        $"AdjustRotScanInterval：扫描耗时 {scanDuration}ms（过长），" +
                        $"调整间隔至 {newInterval}ms",
                        LogHelper.LogType.Warning);
                }

                // 应用新的扫描间隔
                if (newInterval != oldInterval)
                {
                    _rotScanInterval = newInterval;
                    
                    // 重新配置定时器
                    if (_rotScanTimer != null)
                    {
                        _rotScanTimer.Change(_rotScanInterval, _rotScanInterval);
                        LogHelper.WriteLogToFile(
                            $"AdjustRotScanInterval：ROT 扫描间隔已调整为 {_rotScanInterval}ms",
                            LogHelper.LogType.Info);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"AdjustRotScanInterval：调整扫描间隔失败：{ex.Message}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 处理 ROT 扫描检测到的实例变化
        /// 重新计算优先级并可能切换绑定
        /// </summary>
        /// <param name="pptInstances">扫描到的 PowerPoint 实例列表</param>
        private void HandleRotScanInstanceChange(List<object> pptInstances)
        {
            if (pptInstances == null || pptInstances.Count == 0)
            {
                LogHelper.WriteLogToFile("HandleRotScanInstanceChange：未发现 PowerPoint 实例", LogHelper.LogType.Info);
                return;
            }

            try
            {
                LogHelper.WriteLogToFile($"HandleRotScanInstanceChange：正在处理 {pptInstances.Count} 个 PowerPoint 实例", LogHelper.LogType.Info);

                // 为每个实例计算优先级
                List<PPTTarget> targets = new List<PPTTarget>();
                
                foreach (var instance in pptInstances)
                {
                    try
                    {
                        dynamic app = GetDynamicComObject(instance);
                        if (app == null)
                        {
                            SafeReleaseComObject(instance);
                            continue;
                        }
                        
                        PPTTarget target = CalculatePriority(app);
                        
                        if (target != null && target.Priority != PPTBindingPriority.None)
                        {
                            targets.Add(target);
                            
                            if (_debugMode)
                            {
                                LogHelper.WriteLogToFile(
                                    $"HandleRotScanInstanceChange：发现优先级 {target.Priority} 的目标，" +
                                    $"演示文稿：{target.PresentationName}",
                                    LogHelper.LogType.Trace);
                            }
                        }
                        else
                        {
                            SafeReleaseComObject(app);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.WriteLogToFile($"HandleRotScanInstanceChange：处理实例失败：{ex.Message}", LogHelper.LogType.Warning);
                        SafeReleaseComObject(instance);
                    }
                }

                if (targets.Count == 0)
                {
                    LogHelper.WriteLogToFile("HandleRotScanInstanceChange：未找到有效目标", LogHelper.LogType.Warning);
                    return;
                }

                // 选择优先级最高的目标
                PPTTarget bestTarget = targets.OrderByDescending(t => t.Priority).First();
                
                LogHelper.WriteLogToFile(
                    $"HandleRotScanInstanceChange：最佳目标优先级 {bestTarget.Priority}，" +
                    $"演示文稿：{bestTarget.PresentationName}",
                    LogHelper.LogType.Info);

                // 判断是否需要切换绑定
                if (ShouldSwitchBinding(_currentTarget, bestTarget))
                {
                    LogHelper.WriteLogToFile("HandleRotScanInstanceChange：切换绑定到新目标", LogHelper.LogType.Info);
                    
                    bool switchSuccess = SwitchBinding(bestTarget);
                    
                    if (switchSuccess)
                    {
                        LogHelper.WriteLogToFile("HandleRotScanInstanceChange：绑定切换成功", LogHelper.LogType.Info);
                        
                        // 重新启动轮询或事件监听
                        StopPollingOrEventMonitoring();
                        StartPollingOrEventMonitoring();
                    }
                    else
                    {
                        LogHelper.WriteLogToFile("HandleRotScanInstanceChange：绑定切换失败", LogHelper.LogType.Error);
                    }
                }
                else
                {
                    if (_debugMode)
                    {
                        LogHelper.WriteLogToFile("HandleRotScanInstanceChange：无需切换绑定", LogHelper.LogType.Trace);
                    }
                }

                // 释放未选中的目标
                foreach (var target in targets)
                {
                    if (target != bestTarget && target != _currentTarget)
                    {
                        ReleaseTargetComObjects(target);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"HandleRotScanInstanceChange：处理实例变化失败：{ex.Message}", LogHelper.LogType.Error);
                LogHelper.NewLog(ex);
            }
        }

        /// <summary>
        /// 获取 ROT 扫描性能统计信息
        /// 用于调试和性能监控
        /// </summary>
        /// <returns>性能统计信息字符串</returns>
        private string GetRotScanPerformanceStats()
        {
            if (_rotScanCount == 0)
            {
                return "尚未执行 ROT 扫描";
            }

            long avgDuration = _rotScanTotalDuration / _rotScanCount;
            TimeSpan timeSinceLastScan = DateTime.Now - _lastRotScanTime;

            return $"ROT 扫描统计：" +
                   $"总次数：{_rotScanCount}，" +
                   $"平均耗时：{avgDuration}ms，" +
                   $"上次耗时：{_lastRotScanDuration}ms，" +
                   $"当前间隔：{_rotScanInterval}ms，" +
                   $"距上次扫描：{timeSinceLastScan.TotalSeconds:F1}s，" +
                   $"连续未变化：{_consecutiveUnchangedScans}";
        }

        /// <summary>
        /// 设置调试模式
        /// 调试模式下会输出更详细的日志
        /// </summary>
        /// <param name="enabled">是否启用调试模式</param>
        public void SetDebugMode(bool enabled)
        {
            _debugMode = enabled;
            LogHelper.WriteLogToFile($"PPTService：调试模式{(enabled ? "已启用" : "已关闭")}", LogHelper.LogType.Info);
        }

        /// <summary>
        /// 输出当前 PPT 服务状态的详细日志
        /// 用于调试和问题诊断
        /// </summary>
        public void LogCurrentState()
        {
            try
            {
                LogHelper.WriteLogToFile("=== PPT 服务当前状态 ===", LogHelper.LogType.Info);
                
                // 基本状态
                LogHelper.WriteLogToFile($"SettingsService：{(_settingsService != null ? "可用" : "不可用")}", LogHelper.LogType.Info);
                LogHelper.WriteLogToFile($"调试模式：{_debugMode}", LogHelper.LogType.Info);
                
                // 当前绑定状态
                if (_currentTarget != null)
                {
                    LogHelper.WriteLogToFile("当前绑定：", LogHelper.LogType.Info);
                    LogHelper.WriteLogToFile($"  - 优先级：{_currentTarget.Priority}", LogHelper.LogType.Info);
                    LogHelper.WriteLogToFile($"  - 应用：{_currentTarget.ApplicationName}", LogHelper.LogType.Info);
                    LogHelper.WriteLogToFile($"  - 演示文稿：{_currentTarget.PresentationName}", LogHelper.LogType.Info);
                    LogHelper.WriteLogToFile($"  - HWND：0x{_currentTarget.SlideShowWindowHWND:X}", LogHelper.LogType.Info);
                }
                else
                {
                    LogHelper.WriteLogToFile("当前绑定：无", LogHelper.LogType.Info);
                }
                
                // 事件和轮询状态
                LogHelper.WriteLogToFile($"事件注册：{(_eventRegistrationSucceeded ? "成功" : "失败")}", LogHelper.LogType.Info);
                LogHelper.WriteLogToFile($"轮询状态：{_isPollingActive}", LogHelper.LogType.Info);
                LogHelper.WriteLogToFile($"轮询间隔：{_pollingInterval}ms", LogHelper.LogType.Info);
                LogHelper.WriteLogToFile($"最近轮询页码：{_lastPolledSlideIndex}", LogHelper.LogType.Info);
                
                // ROT 扫描状态
                LogHelper.WriteLogToFile($"ROT 扫描启用：{(_rotScanTimer != null)}", LogHelper.LogType.Info);
                LogHelper.WriteLogToFile($"ROT 扫描间隔：{_rotScanInterval}ms", LogHelper.LogType.Info);
                LogHelper.WriteLogToFile($"ROT 扫描中：{_isRotScanRunning}", LogHelper.LogType.Info);
                LogHelper.WriteLogToFile($"最近实例数量：{_lastPptInstanceCount}", LogHelper.LogType.Info);
                LogHelper.WriteLogToFile($"连续未变化扫描次数：{_consecutiveUnchangedScans}", LogHelper.LogType.Info);
                
                // ROT 扫描性能统计
                if (_rotScanCount > 0)
                {
                    LogHelper.WriteLogToFile(GetRotScanPerformanceStats(), LogHelper.LogType.Info);
                }
                
                // COM 对象跟踪
                LogHelper.WriteLogToFile($"跟踪的 COM 对象数：{_comObjectTracker.TrackedCount}", LogHelper.LogType.Info);
                
                // 演示文稿信息
                if (_presentation != null)
                {
                    LogHelper.WriteLogToFile("演示文稿已加载：是", LogHelper.LogType.Info);
                    LogHelper.WriteLogToFile($"幻灯片数量：{_slidesCount}", LogHelper.LogType.Info);
                }
                else
                {
                    LogHelper.WriteLogToFile("演示文稿已加载：否", LogHelper.LogType.Info);
                }
                
                LogHelper.WriteLogToFile("=== PPT 服务状态结束 ===", LogHelper.LogType.Info);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"LogCurrentState：记录状态失败：{ex.Message}", LogHelper.LogType.Error);
            }
        }

        #endregion

        #region Dynamic COM 对象处理

        /// <summary>
        /// 将 COM 对象包装为 dynamic 类型
        /// 用于处理 COM 注册损坏导致的类型不匹配问题
        /// </summary>
        /// <param name="comObject">原始 COM 对象</param>
        /// <returns>dynamic 类型的 COM 对象，如果失败则返回 null</returns>
        private dynamic GetDynamicComObject(object comObject)
        {
            if (comObject == null)
            {
                LogHelper.WriteLogToFile("GetDynamicComObject：输入对象为空", LogHelper.LogType.Warning);
                return null;
            }

            try
            {
                // 将 RCW 转换为 dynamic 类型
                dynamic dynamicObject = comObject;
                
                LogHelper.WriteLogToFile($"GetDynamicComObject：已将 COM 对象包装为 dynamic，类型：{comObject.GetType().Name}", LogHelper.LogType.Trace);
                
                return dynamicObject;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"GetDynamicComObject：包装 COM 对象失败：{ex.Message}", LogHelper.LogType.Error);
                return null;
            }
        }

        /// <summary>
        /// 检查 dynamic 对象是否具有指定的属性
        /// </summary>
        /// <param name="dynamicObject">dynamic 对象</param>
        /// <param name="propertyName">属性名称</param>
        /// <returns>如果属性存在且可访问则返回 true</returns>
        private bool HasProperty(dynamic dynamicObject, string propertyName)
        {
            if (dynamicObject == null)
            {
                return false;
            }

            try
            {
                // 尝试通过反射检查属性是否存在
                var type = dynamicObject.GetType();
                var property = type.GetProperty(propertyName);
                
                if (property != null)
                {
                    return true;
                }

                // 对于 COM 对象，还需要检查是否可以通过 IDispatch 访问
                try
                {
                    dynamicObject.GetType().InvokeMember(
                        propertyName,
                        System.Reflection.BindingFlags.GetProperty,
                        null,
                        dynamicObject,
                        null);
                    return true;
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"HasProperty: 无法访问属性 '{propertyName}': {ex.Message}", LogHelper.LogType.Trace);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"HasProperty: 检查属性 '{propertyName}' 出错：{ex.Message}", LogHelper.LogType.Trace);
                return false;
            }
        }

        /// <summary>
        /// 安全地获取 dynamic 对象的属性值
        /// </summary>
        /// <param name="dynamicObject">dynamic 对象</param>
        /// <param name="propertyName">属性名称</param>
        /// <param name="defaultValue">默认值（如果获取失败）</param>
        /// <returns>属性值或默认值</returns>
        private dynamic SafeGetProperty(dynamic dynamicObject, string propertyName, dynamic defaultValue = null)
        {
            if (dynamicObject == null)
            {
                return defaultValue;
            }

            try
            {
                // 尝试访问属性
                var value = dynamicObject.GetType().InvokeMember(
                    propertyName,
                    System.Reflection.BindingFlags.GetProperty,
                    null,
                    dynamicObject,
                    null);
                
                return value;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"SafeGetProperty：获取属性 '{propertyName}' 失败：{ex.Message}", LogHelper.LogType.Trace);
                return defaultValue;
            }
        }

        /// <summary>
        /// 尝试访问 dynamic 对象的属性，并处理可能的异常
        /// </summary>
        /// <typeparam name="T">期望的返回类型</typeparam>
        /// <param name="action">访问属性的操作</param>
        /// <param name="defaultValue">默认值（如果访问失败）</param>
        /// <param name="propertyName">属性名称（用于日志）</param>
        /// <returns>属性值或默认值</returns>
        private T TryGetDynamicProperty<T>(Func<T> action, T defaultValue, string propertyName = "")
        {
            try
            {
                return action();
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                LogHelper.WriteLogToFile($"尝试获取属性：'{propertyName}' 不存在或不可访问：{ex.Message}", LogHelper.LogType.Trace);
                return defaultValue;
            }
            catch (System.Runtime.InteropServices.COMException ex)
            {
                LogHelper.WriteLogToFile($"尝试获取属性：访问 '{propertyName}' 的 COM 错误：{ex.Message}", LogHelper.LogType.Warning);
                return defaultValue;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"尝试获取属性：访问 '{propertyName}' 发生异常：{ex.Message}", LogHelper.LogType.Warning);
                return defaultValue;
            }
        }

        /// <summary>
        /// 安全地释放 dynamic COM 对象
        /// 处理引用计数管理
        /// </summary>
        /// <param name="comObject">要释放的 COM 对象</param>
        /// <returns>释放的引用计数，如果失败则返回 0</returns>
        private int SafeReleaseComObject(object comObject)
        {
            if (comObject != null && Marshal.IsComObject(comObject))
            {
                try
                {
                    int refCount = Marshal.ReleaseComObject(comObject);
                    LogHelper.WriteLogToFile($"释放 COM 对象：已释放（{comObject.GetType().Name}），剩余引用计数 = {refCount}", LogHelper.LogType.Trace);
                    return refCount;
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"释放 COM 对象失败：{ex.Message}", LogHelper.LogType.Warning);
                    return 0;
                }
            }
            return 0;
        }

        /// <summary>
        /// 完全释放 COM 对象，将引用计数降为 0
        /// 警告：仅在确定不再需要该对象时使用
        /// </summary>
        /// <param name="comObject">要释放的 COM 对象</param>
        private void FinalReleaseComObject(object comObject)
        {
            if (comObject != null && Marshal.IsComObject(comObject))
            {
                try
                {
                    int refCount = Marshal.FinalReleaseComObject(comObject);
                    LogHelper.WriteLogToFile($"完全释放 COM 对象：已释放（{comObject.GetType().Name}），最终引用计数 = {refCount}", LogHelper.LogType.Trace);
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"完全释放 COM 对象失败：{ex.Message}", LogHelper.LogType.Warning);
                }
            }
        }

        /// <summary>
        /// 安全地释放 COM 对象并将引用设为 null
        /// </summary>
        /// <typeparam name="T">COM 对象类型</typeparam>
        /// <param name="comObject">COM 对象引用</param>
        private void SafeReleaseAndNull<T>(ref T comObject) where T : class
        {
            if (comObject != null)
            {
                SafeReleaseComObject(comObject);
                comObject = null;
            }
        }

        #endregion

        #region 重试和错误恢复机制

        /// <summary>
        /// 使用重试机制获取 ActivePresentation
        /// 在进入放映瞬间 ActivePresentation 可能瞬时失效，需要重试
        /// </summary>
        /// <param name="app">PowerPoint Application 对象（dynamic 类型）</param>
        /// <param name="maxRetries">最大重试次数（默认 3 次）</param>
        /// <param name="retryDelayMs">重试间隔（毫秒，默认 100ms）</param>
        /// <returns>ActivePresentation 对象，如果失败则返回 null</returns>
        private dynamic GetActivePresentationWithRetry(dynamic app, int maxRetries = 3, int retryDelayMs = 100)
        {
            if (app == null)
            {
                LogHelper.WriteLogToFile("GetActivePresentationWithRetry：Application 为空", LogHelper.LogType.Warning);
                return null;
            }

            int attemptCount = 0;
            Exception lastException = null;

            while (attemptCount < maxRetries)
            {
                attemptCount++;

                try
                {
                    LogHelper.WriteLogToFile($"GetActivePresentationWithRetry：第 {attemptCount}/{maxRetries} 次尝试", LogHelper.LogType.Trace);

                    dynamic activePresentation = app.ActivePresentation;

                    if (activePresentation != null)
                    {
                        LogHelper.WriteLogToFile($"GetActivePresentationWithRetry：第 {attemptCount} 次成功获取 ActivePresentation", LogHelper.LogType.Info);
                        return activePresentation;
                    }
                    else
                    {
                        LogHelper.WriteLogToFile($"GetActivePresentationWithRetry：第 {attemptCount} 次 ActivePresentation 为空", LogHelper.LogType.Trace);
                        lastException = new InvalidOperationException("ActivePresentation is null");
                    }
                }
                catch (System.Runtime.InteropServices.COMException comEx)
                {
                    lastException = comEx;
                    LogHelper.WriteLogToFile($"GetActivePresentationWithRetry：第 {attemptCount} 次 COM 异常：HRESULT=0x{comEx.HResult:X8}，Message={comEx.Message}", LogHelper.LogType.Warning);
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException binderEx)
                {
                    lastException = binderEx;
                    LogHelper.WriteLogToFile($"GetActivePresentationWithRetry：第 {attemptCount} 次 RuntimeBinder 异常：{binderEx.Message}", LogHelper.LogType.Warning);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    LogHelper.WriteLogToFile($"GetActivePresentationWithRetry：第 {attemptCount} 次发生异常：{ex.Message}", LogHelper.LogType.Warning);
                }

                // 如果还有重试机会，等待后重试
                if (attemptCount < maxRetries)
                {
                    LogHelper.WriteLogToFile($"GetActivePresentationWithRetry：等待 {retryDelayMs}ms 后重试...", LogHelper.LogType.Trace);
                    System.Threading.Thread.Sleep(retryDelayMs);
                }
            }

            // 所有重试都失败
            LogHelper.WriteLogToFile($"GetActivePresentationWithRetry：重试 {maxRetries} 次后仍失败。最后错误：{lastException?.Message}", LogHelper.LogType.Error);
            if (lastException != null)
            {
                LogHelper.NewLog(lastException);
            }

            return null;
        }

        /// <summary>
        /// 使用重试机制执行 dynamic COM 操作
        /// 通用的重试包装器，可用于任何可能瞬时失败的 COM 操作
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="operation">要执行的操作</param>
        /// <param name="operationName">操作名称（用于日志）</param>
        /// <param name="defaultValue">失败时的默认返回值</param>
        /// <param name="maxRetries">最大重试次数（默认 3 次）</param>
        /// <param name="retryDelayMs">重试间隔（毫秒，默认 100ms）</param>
        /// <returns>操作结果，如果失败则返回默认值</returns>
        private T ExecuteWithRetry<T>(Func<T> operation, string operationName, T defaultValue, int maxRetries = 3, int retryDelayMs = 100)
        {
            if (operation == null)
            {
                LogHelper.WriteLogToFile($"ExecuteWithRetry：操作为空 '{operationName}'", LogHelper.LogType.Warning);
                return defaultValue;
            }

            int attemptCount = 0;
            Exception lastException = null;

            while (attemptCount < maxRetries)
            {
                attemptCount++;

                try
                {
                    LogHelper.WriteLogToFile($"ExecuteWithRetry[{operationName}]：第 {attemptCount}/{maxRetries} 次尝试", LogHelper.LogType.Trace);

                    T result = operation();

                    LogHelper.WriteLogToFile($"ExecuteWithRetry[{operationName}]：第 {attemptCount} 次执行成功", LogHelper.LogType.Trace);
                    return result;
                }
                catch (System.Runtime.InteropServices.COMException comEx)
                {
                    lastException = comEx;
                    LogHelper.WriteLogToFile($"ExecuteWithRetry[{operationName}]：第 {attemptCount} 次 COM 异常：HRESULT=0x{comEx.HResult:X8}，Message={comEx.Message}", LogHelper.LogType.Warning);
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException binderEx)
                {
                    lastException = binderEx;
                    LogHelper.WriteLogToFile($"ExecuteWithRetry[{operationName}]：第 {attemptCount} 次 RuntimeBinder 异常：{binderEx.Message}", LogHelper.LogType.Warning);
                }
                catch (InvalidOperationException invalidOpEx)
                {
                    lastException = invalidOpEx;
                    LogHelper.WriteLogToFile($"ExecuteWithRetry[{operationName}]：第 {attemptCount} 次 InvalidOperation 异常：{invalidOpEx.Message}", LogHelper.LogType.Warning);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    LogHelper.WriteLogToFile($"ExecuteWithRetry[{operationName}]：第 {attemptCount} 次发生异常：{ex.Message}", LogHelper.LogType.Warning);
                }

                // 如果还有重试机会，等待后重试
                if (attemptCount < maxRetries)
                {
                    LogHelper.WriteLogToFile($"ExecuteWithRetry[{operationName}]：等待 {retryDelayMs}ms 后重试...", LogHelper.LogType.Trace);
                    System.Threading.Thread.Sleep(retryDelayMs);
                }
            }

            // 所有重试都失败
            LogHelper.WriteLogToFile($"ExecuteWithRetry[{operationName}]：重试 {maxRetries} 次后仍失败。最后错误：{lastException?.Message}", LogHelper.LogType.Error);
            if (lastException != null)
            {
                LogHelper.NewLog(lastException);
            }

            return defaultValue;
        }

        /// <summary>
        /// 使用重试机制执行无返回值的 dynamic COM 操作
        /// </summary>
        /// <param name="operation">要执行的操作</param>
        /// <param name="operationName">操作名称（用于日志）</param>
        /// <param name="maxRetries">最大重试次数（默认 3 次）</param>
        /// <param name="retryDelayMs">重试间隔（毫秒，默认 100ms）</param>
        /// <returns>如果操作成功则返回 true，否则返回 false</returns>
        private bool ExecuteWithRetry(Action operation, string operationName, int maxRetries = 3, int retryDelayMs = 100)
        {
            if (operation == null)
            {
                LogHelper.WriteLogToFile($"ExecuteWithRetry：操作为空 '{operationName}'", LogHelper.LogType.Warning);
                return false;
            }

            int attemptCount = 0;
            Exception lastException = null;

            while (attemptCount < maxRetries)
            {
                attemptCount++;

                try
                {
                    LogHelper.WriteLogToFile($"ExecuteWithRetry[{operationName}]：第 {attemptCount}/{maxRetries} 次尝试", LogHelper.LogType.Trace);

                    operation();

                    LogHelper.WriteLogToFile($"ExecuteWithRetry[{operationName}]：第 {attemptCount} 次执行成功", LogHelper.LogType.Trace);
                    return true;
                }
                catch (System.Runtime.InteropServices.COMException comEx)
                {
                    lastException = comEx;
                    LogHelper.WriteLogToFile($"ExecuteWithRetry[{operationName}]：第 {attemptCount} 次 COM 异常：HRESULT=0x{comEx.HResult:X8}，Message={comEx.Message}", LogHelper.LogType.Warning);
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException binderEx)
                {
                    lastException = binderEx;
                    LogHelper.WriteLogToFile($"ExecuteWithRetry[{operationName}]：第 {attemptCount} 次 RuntimeBinder 异常：{binderEx.Message}", LogHelper.LogType.Warning);
                }
                catch (InvalidOperationException invalidOpEx)
                {
                    lastException = invalidOpEx;
                    LogHelper.WriteLogToFile($"ExecuteWithRetry[{operationName}]：第 {attemptCount} 次 InvalidOperation 异常：{invalidOpEx.Message}", LogHelper.LogType.Warning);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    LogHelper.WriteLogToFile($"ExecuteWithRetry[{operationName}]：第 {attemptCount} 次发生异常：{ex.Message}", LogHelper.LogType.Warning);
                }

                // 如果还有重试机会，等待后重试
                if (attemptCount < maxRetries)
                {
                    LogHelper.WriteLogToFile($"ExecuteWithRetry[{operationName}]：等待 {retryDelayMs}ms 后重试...", LogHelper.LogType.Trace);
                    System.Threading.Thread.Sleep(retryDelayMs);
                }
            }

            // 所有重试都失败
            LogHelper.WriteLogToFile($"ExecuteWithRetry[{operationName}]：重试 {maxRetries} 次后仍失败。最后错误：{lastException?.Message}", LogHelper.LogType.Error);
            if (lastException != null)
            {
                LogHelper.NewLog(lastException);
            }

            return false;
        }

        /// <summary>
        /// 安全地访问 dynamic COM 对象的属性，带有全局异常处理和降级处理
        /// 这是 TryGetDynamicProperty 的增强版本，提供更详细的错误日志和降级策略
        /// </summary>
        /// <typeparam name="T">期望的返回类型</typeparam>
        /// <param name="action">访问属性的操作</param>
        /// <param name="defaultValue">默认值（如果访问失败）</param>
        /// <param name="propertyName">属性名称（用于日志）</param>
        /// <param name="enableRetry">是否启用重试（默认 false）</param>
        /// <param name="maxRetries">最大重试次数（默认 2 次）</param>
        /// <returns>属性值或默认值</returns>
        private T SafeAccessDynamicProperty<T>(Func<T> action, T defaultValue, string propertyName = "", bool enableRetry = false, int maxRetries = 2)
        {
            if (action == null)
            {
                LogHelper.WriteLogToFile($"SafeAccessDynamicProperty：属性 '{propertyName}' 的访问操作为空", LogHelper.LogType.Warning);
                return defaultValue;
            }

            // 如果启用重试，使用 ExecuteWithRetry
            if (enableRetry)
            {
                return ExecuteWithRetry(action, $"访问属性 '{propertyName}'", defaultValue, maxRetries, 50);
            }

            // 否则，单次尝试
            try
            {
                return action();
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                LogHelper.WriteLogToFile($"SafeAccessDynamicProperty：属性 '{propertyName}' 不存在或不可访问：{ex.Message}", LogHelper.LogType.Trace);
                LogHelper.WriteLogToFile($"SafeAccessDynamicProperty：返回默认值 '{propertyName}'", LogHelper.LogType.Trace);
                return defaultValue;
            }
            catch (System.Runtime.InteropServices.COMException ex)
            {
                LogHelper.WriteLogToFile($"SafeAccessDynamicProperty：访问属性 '{propertyName}' 的 COM 错误：HRESULT=0x{ex.HResult:X8}，Message={ex.Message}", LogHelper.LogType.Warning);
                LogHelper.WriteLogToFile($"SafeAccessDynamicProperty：返回默认值 '{propertyName}'", LogHelper.LogType.Trace);
                
                // 记录详细的 COM 错误信息
                if (ex.HResult == unchecked((int)0x800706BA)) // RPC_S_SERVER_UNAVAILABLE
                {
                    LogHelper.WriteLogToFile($"SafeAccessDynamicProperty：COM 服务器不可用 '{propertyName}'", LogHelper.LogType.Error);
                }
                else if (ex.HResult == unchecked((int)0x80010001)) // RPC_E_CALL_REJECTED
                {
                    LogHelper.WriteLogToFile($"SafeAccessDynamicProperty：COM 调用被拒绝 '{propertyName}'", LogHelper.LogType.Error);
                }
                else if (ex.HResult == unchecked((int)0x8001010A)) // RPC_E_SERVERCALL_RETRYLATER
                {
                    LogHelper.WriteLogToFile($"SafeAccessDynamicProperty：COM 服务器繁忙 '{propertyName}'，建议重试", LogHelper.LogType.Warning);
                }
                
                return defaultValue;
            }
            catch (InvalidOperationException ex)
            {
                LogHelper.WriteLogToFile($"SafeAccessDynamicProperty：访问属性 '{propertyName}' 时发生无效操作：{ex.Message}", LogHelper.LogType.Warning);
                LogHelper.WriteLogToFile($"SafeAccessDynamicProperty：返回默认值 '{propertyName}'", LogHelper.LogType.Trace);
                return defaultValue;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"SafeAccessDynamicProperty：访问属性 '{propertyName}' 发生异常：{ex.GetType().Name} - {ex.Message}", LogHelper.LogType.Error);
                LogHelper.WriteLogToFile($"SafeAccessDynamicProperty：堆栈：{ex.StackTrace}", LogHelper.LogType.Trace);
                LogHelper.WriteLogToFile($"SafeAccessDynamicProperty：返回默认值 '{propertyName}'", LogHelper.LogType.Trace);
                LogHelper.NewLog(ex);
                return defaultValue;
            }
        }

        /// <summary>
        /// 安全地执行 dynamic COM 方法调用，带有全局异常处理
        /// </summary>
        /// <param name="action">要执行的方法调用</param>
        /// <param name="methodName">方法名称（用于日志）</param>
        /// <param name="enableRetry">是否启用重试（默认 false）</param>
        /// <param name="maxRetries">最大重试次数（默认 2 次）</param>
        /// <returns>如果方法执行成功则返回 true，否则返回 false</returns>
        private bool SafeInvokeDynamicMethod(Action action, string methodName = "", bool enableRetry = false, int maxRetries = 2)
        {
            if (action == null)
            {
                LogHelper.WriteLogToFile($"SafeInvokeDynamicMethod：方法 '{methodName}' 的调用为空", LogHelper.LogType.Warning);
                return false;
            }

            // 如果启用重试，使用 ExecuteWithRetry
            if (enableRetry)
            {
                return ExecuteWithRetry(action, $"调用方法 '{methodName}'", maxRetries, 50);
            }

            // 否则，单次尝试
            try
            {
                action();
                return true;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                LogHelper.WriteLogToFile($"SafeInvokeDynamicMethod：方法 '{methodName}' 不存在或不可访问：{ex.Message}", LogHelper.LogType.Warning);
                return false;
            }
            catch (System.Runtime.InteropServices.COMException ex)
            {
                LogHelper.WriteLogToFile($"SafeInvokeDynamicMethod：调用方法 '{methodName}' 时发生 COM 错误：HRESULT=0x{ex.HResult:X8}，Message={ex.Message}", LogHelper.LogType.Error);
                
                // 记录详细的 COM 错误信息
                if (ex.HResult == unchecked((int)0x800706BA)) // RPC_S_SERVER_UNAVAILABLE
                {
                    LogHelper.WriteLogToFile($"SafeInvokeDynamicMethod：COM 服务器不可用 '{methodName}'", LogHelper.LogType.Error);
                }
                else if (ex.HResult == unchecked((int)0x80010001)) // RPC_E_CALL_REJECTED
                {
                    LogHelper.WriteLogToFile($"SafeInvokeDynamicMethod：COM 调用被拒绝 '{methodName}'", LogHelper.LogType.Error);
                }
                else if (ex.HResult == unchecked((int)0x8001010A)) // RPC_E_SERVERCALL_RETRYLATER
                {
                    LogHelper.WriteLogToFile($"SafeInvokeDynamicMethod：COM 服务器繁忙 '{methodName}'，建议重试", LogHelper.LogType.Warning);
                }
                return false;
            }
            catch (InvalidOperationException ex)
            {
                LogHelper.WriteLogToFile($"SafeInvokeDynamicMethod：调用方法 '{methodName}' 时发生无效操作：{ex.Message}", LogHelper.LogType.Error);
                LogHelper.NewLog(ex);
                return false;
            }
        }

        /// <summary>
        /// 检查 COM 异常是否为瞬时错误（可重试）
        /// </summary>
        /// <param name="ex">COM 异常</param>
        /// <returns>如果是瞬时错误则返回 true</returns>
        private bool IsTransientCOMError(System.Runtime.InteropServices.COMException ex)
        {
            if (ex == null)
            {
                return false;
            }

            // 常见的瞬时 COM 错误 HRESULT
            int[] transientErrors = new int[]
            {
                unchecked((int)0x8001010A), // RPC_E_SERVERCALL_RETRYLATER - 服务器忙，稍后重试
                unchecked((int)0x80010001), // RPC_E_CALL_REJECTED - 调用被拒绝
                unchecked((int)0x80010007), // RPC_E_SERVER_DIED_DNE - 服务器不存在或已终止
                unchecked((int)0x800706BA), // RPC_S_SERVER_UNAVAILABLE - 服务器不可用
                unchecked((int)0x800706BE), // RPC_S_CALL_FAILED - 调用失败
                unchecked((int)0x80004005), // E_FAIL - 未指定的错误（可能是瞬时的）
            };

            foreach (int errorCode in transientErrors)
            {
                if (ex.HResult == errorCode)
                {
                    LogHelper.WriteLogToFile($"可重试 COM 错误：HRESULT=0x{ex.HResult:X8}", LogHelper.LogType.Info);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 获取 COM 异常的友好错误消息
        /// </summary>
        /// <param name="ex">COM 异常</param>
        /// <returns>友好的错误消息</returns>
        private string GetCOMErrorMessage(System.Runtime.InteropServices.COMException ex)
        {
            if (ex == null)
            {
                return "未知 COM 错误";
            }

            switch (ex.HResult)
            {
                case unchecked((int)0x8001010A):
                    return "COM 服务器繁忙，请稍后重试 (RPC_E_SERVERCALL_RETRYLATER)";
                case unchecked((int)0x80010001):
                    return "COM 调用被拒绝 (RPC_E_CALL_REJECTED)";
                case unchecked((int)0x80010007):
                    return "COM 服务器已终止或不存在 (RPC_E_SERVER_DIED_DNE)";
                case unchecked((int)0x800706BA):
                    return "COM 服务器不可用 (RPC_S_SERVER_UNAVAILABLE)";
                case unchecked((int)0x800706BE):
                    return "COM 调用失败 (RPC_S_CALL_FAILED)";
                case unchecked((int)0x80004005):
                    return "未指定的 COM 错误 (E_FAIL)";
                case unchecked((int)0x80020009):
                    return "发生 COM 异常 (DISP_E_EXCEPTION)";
                case unchecked((int)0x8002000E):
                    return "参数数量无效 (DISP_E_BADPARAMCOUNT)";
                case unchecked((int)0x80020003):
                    return "成员未找到 (DISP_E_MEMBERNOTFOUND)";
                default:
                    return $"COM 错误：HRESULT=0x{ex.HResult:X8}，Message={ex.Message}";
            }
        }

        #endregion

        #region 优先级计算

        /// <summary>
        /// 计算 PowerPoint 应用程序的绑定优先级
        /// </summary>
        /// <param name="app">PowerPoint Application 对象（dynamic 类型）</param>
        /// <returns>包含优先级信息的 PPTTarget 对象</returns>
        private PPTTarget CalculatePriority(dynamic app)
        {
            var target = new PPTTarget
            {
                Application = app
            };

            if (app == null)
            {
                LogHelper.WriteLogToFile("CalculatePriority：Application 为空", LogHelper.LogType.Warning);
                return target;
            }

            try
            {
                // 尝试获取应用程序名称（用于 WPS 特殊处理）
                target.ApplicationName = TryGetDynamicProperty(() => app.Name, "未知", "Name");
                LogHelper.WriteLogToFile($"CalculatePriority：应用名称 = {target.ApplicationName}", LogHelper.LogType.Trace);

                // 检查 ActivePresentation 是否存在
                // 使用重试机制处理瞬时失效（在进入放映瞬间可能失效）
                dynamic activePresentation = null;
                try
                {
                    activePresentation = GetActivePresentationWithRetry(app);
                    if (activePresentation != null)
                    {
                        target.Presentation = activePresentation;
                        target.Priority = PPTBindingPriority.Level1_Basic;

                        // 尝试获取演示文稿名称
                        target.PresentationName = TryGetDynamicProperty(() => activePresentation.Name, "未知", "Presentation.Name");
                        LogHelper.WriteLogToFile($"CalculatePriority：发现 ActivePresentation：{target.PresentationName}，优先级 = Level1_Basic", LogHelper.LogType.Info);
                    }
                    else
                    {
                        LogHelper.WriteLogToFile("CalculatePriority：重试后 ActivePresentation 仍为空", LogHelper.LogType.Trace);
                        return target;
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"CalculatePriority：重试后获取 ActivePresentation 失败：{ex.Message}", LogHelper.LogType.Trace);
                    return target;
                }

                // 检查 SlideShowWindow 是否存在
                dynamic slideShowWindows = null;
                try
                {
                    slideShowWindows = app.SlideShowWindows;
                    if (slideShowWindows != null && slideShowWindows.Count > 0)
                    {
                        dynamic slideShowWindow = slideShowWindows[1]; // COM 集合从 1 开始
                        if (slideShowWindow != null)
                        {
                            target.SlideShowWindow = slideShowWindow;
                            target.Priority = PPTBindingPriority.Level2_HasSlideShow;
                            LogHelper.WriteLogToFile("CalculatePriority：发现 SlideShowWindow，优先级 = Level2_HasSlideShow", LogHelper.LogType.Info);

                            // 尝试获取窗口句柄
                            try
                            {
                                IntPtr hwnd = GetSlideShowWindowHWND(slideShowWindow);
                                if (hwnd != IntPtr.Zero)
                                {
                                    target.SlideShowWindowHWND = hwnd;
                                    LogHelper.WriteLogToFile($"CalculatePriority：获取 SlideShowWindow HWND = 0x{hwnd:X}", LogHelper.LogType.Trace);
                                }
                                else
                                {
                                    LogHelper.WriteLogToFile("CalculatePriority：获取 SlideShowWindow HWND 失败", LogHelper.LogType.Trace);
                                }
                            }
                            catch (Exception ex)
                            {
                                LogHelper.WriteLogToFile($"CalculatePriority：获取 SlideShowWindow HWND 失败：{ex.Message}", LogHelper.LogType.Trace);
                            }

                            // WPS 特殊处理: 修正窗口尺寸
                            // WPS 返回幻灯片尺寸而非窗口尺寸，需要通过窗口句柄获取真实尺寸
                            try
                            {
                                CorrectWPSSlideShowWindowSize(target);
                            }
                            catch (Exception ex)
                            {
                                LogHelper.WriteLogToFile($"CalculatePriority：修正 WPS 窗口尺寸失败：{ex.Message}", LogHelper.LogType.Trace);
                            }

                            // 检查放映窗口是否激活或为焦点
                            bool isActivated = CheckSlideShowWindowActivated(slideShowWindow, target.ApplicationName, target.SlideShowWindowHWND);
                            if (isActivated)
                            {
                                target.Priority = PPTBindingPriority.Level3_WindowActivated;
                                LogHelper.WriteLogToFile("CalculatePriority：放映窗口已激活，优先级 = Level3_WindowActivated", LogHelper.LogType.Info);
                            }
                        }
                        else
                        {
                            // slideShowWindow 为 null，释放 slideShowWindows
                            SafeReleaseComObject(slideShowWindows);
                        }
                    }
                    else
                    {
                        LogHelper.WriteLogToFile("CalculatePriority：未找到 SlideShowWindows", LogHelper.LogType.Trace);
                        // 释放 slideShowWindows（即使 Count 为 0）
                        if (slideShowWindows != null)
                        {
                            SafeReleaseComObject(slideShowWindows);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"CalculatePriority：检查 SlideShowWindows 失败：{ex.Message}", LogHelper.LogType.Trace);
                    // 确保释放 slideShowWindows
                    if (slideShowWindows != null)
                    {
                        SafeReleaseComObject(slideShowWindows);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"CalculatePriority：发生异常：{ex.Message}", LogHelper.LogType.Error);
                LogHelper.NewLog(ex);
            }

            return target;
        }

        /// <summary>
        /// 检查幻灯片放映窗口是否激活或为焦点
        /// 处理 WPS 非全屏放映的特殊情况（焦点在框架窗口）
        /// </summary>
        /// <param name="slideShowWindow">幻灯片放映窗口对象</param>
        /// <param name="applicationName">应用程序名称（用于 WPS 特殊处理）</param>
        /// <param name="hwnd">窗口句柄</param>
        /// <returns>如果窗口激活或为焦点则返回 true</returns>
        [RequiresUnmanagedCode("Uses Win32 API GetForegroundWindow to check if slide show window is activated.")]
        private bool CheckSlideShowWindowActivated(dynamic slideShowWindow, string applicationName, IntPtr hwnd)
        {
            if (slideShowWindow == null)
            {
                return false;
            }

            try
            {
                // 方法 1: 检查 Active 属性
                bool isActive = TryGetDynamicProperty(() => slideShowWindow.Active == Microsoft.Office.Core.MsoTriState.msoTrue, false, "SlideShowWindow.Active");
                if (isActive)
                {
                    LogHelper.WriteLogToFile("CheckSlideShowWindowActivated：窗口已激活（Active 属性）", LogHelper.LogType.Trace);
                    return true;
                }

                // 方法 2: 通过窗口句柄检查是否为前台窗口
                if (hwnd != IntPtr.Zero)
                {
                    IntPtr foregroundWindow = GetForegroundWindow();
                    if (foregroundWindow == hwnd)
                    {
                        LogHelper.WriteLogToFile("CheckSlideShowWindowActivated：窗口为前台窗口", LogHelper.LogType.Trace);
                        return true;
                    }

                    // WPS 特殊处理: 非全屏放映时，焦点可能在框架窗口而非放映窗口
                    if (applicationName != null && applicationName.Contains("WPS"))
                    {
                        LogHelper.WriteLogToFile("CheckSlideShowWindowActivated：检测到 WPS，启用特殊焦点判断", LogHelper.LogType.Trace);
                        
                        // 检查是否为全屏放映
                        bool isFullScreen = IsWPSFullScreenMode(slideShowWindow);
                        LogHelper.WriteLogToFile($"CheckSlideShowWindowActivated：WPS 全屏模式 = {isFullScreen}", LogHelper.LogType.Trace);
                        
                        if (!isFullScreen)
                        {
                            // 非全屏时，焦点可能在框架窗口（父窗口）而非放映窗口
                            IntPtr frameWindow = GetWPSFrameWindow(hwnd);
                            
                            if (frameWindow != IntPtr.Zero)
                            {
                                LogHelper.WriteLogToFile($"CheckSlideShowWindowActivated：找到 WPS 框架窗口：0x{frameWindow:X}", LogHelper.LogType.Trace);
                                
                                // 检查框架窗口是否为前台窗口
                                if (foregroundWindow == frameWindow)
                                {
                                    LogHelper.WriteLogToFile("CheckSlideShowWindowActivated：WPS 框架窗口为前台（非全屏）", LogHelper.LogType.Info);
                                    
                                    // 进一步验证：检查 ActivePresentation 是否与当前放映窗口的演示文稿匹配
                                    if (VerifyWPSActivePresentationMatch(slideShowWindow))
                                    {
                                        LogHelper.WriteLogToFile("CheckSlideShowWindowActivated：ActivePresentation 匹配，确认 WPS 窗口为活动", LogHelper.LogType.Info);
                                        return true;
                                    }
                                    else
                                    {
                                        LogHelper.WriteLogToFile("CheckSlideShowWindowActivated：ActivePresentation 不匹配，WPS 窗口可能未激活", LogHelper.LogType.Warning);
                                    }
                                }
                                else
                                {
                                    LogHelper.WriteLogToFile($"CheckSlideShowWindowActivated：WPS 框架窗口非前台。前台：0x{foregroundWindow:X}，框架：0x{frameWindow:X}", LogHelper.LogType.Trace);
                                }
                            }
                            else
                            {
                                LogHelper.WriteLogToFile("CheckSlideShowWindowActivated：未找到 WPS 框架窗口，直接检查放映窗口", LogHelper.LogType.Trace);
                                
                                // 如果找不到框架窗口，可能是特殊情况，直接检查放映窗口
                                // 结合 ActivePresentation 判断
                                if (VerifyWPSActivePresentationMatch(slideShowWindow))
                                {
                                    LogHelper.WriteLogToFile("CheckSlideShowWindowActivated：ActivePresentation 匹配，视为激活", LogHelper.LogType.Info);
                                    return true;
                                }
                            }
                        }
                        else
                        {
                            // 全屏模式下，如果放映窗口不是前台窗口，但 ActivePresentation 匹配，也可能是活动的
                            if (VerifyWPSActivePresentationMatch(slideShowWindow))
                            {
                                LogHelper.WriteLogToFile("CheckSlideShowWindowActivated：WPS 全屏模式且 ActivePresentation 匹配，视为激活", LogHelper.LogType.Info);
                                return true;
                            }
                        }
                    }
                }

                // 方法 3: 检查 View 是否可访问（间接判断窗口是否活动）
                dynamic view = null;
                try
                {
                    view = slideShowWindow.View;
                    if (view != null)
                    {
                        // 如果能成功访问 View，说明窗口可能是活动的
                        // 这是一个较弱的判断，但在某些情况下有用
                        LogHelper.WriteLogToFile("CheckSlideShowWindowActivated：View 可访问", LogHelper.LogType.Trace);
                        
                        // 释放 View 对象
                        SafeReleaseComObject(view);
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"CheckSlideShowWindowActivated：View 不可访问：{ex.Message}", LogHelper.LogType.Trace);
                    // 确保释放 view（如果已创建）
                    if (view != null)
                    {
                        SafeReleaseComObject(view);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"CheckSlideShowWindowActivated：检查激活状态失败：{ex.Message}", LogHelper.LogType.Warning);
            }

            return false;
        }

        /// <summary>
        /// 验证 WPS 的 ActivePresentation 是否与当前放映窗口的演示文稿匹配
        /// 用于确认焦点窗口是否真正对应当前放映
        /// </summary>
        /// <param name="slideShowWindow">幻灯片放映窗口对象</param>
        /// <returns>如果匹配则返回 true</returns>
        private bool VerifyWPSActivePresentationMatch(dynamic slideShowWindow)
        {
            if (slideShowWindow == null)
            {
                return false;
            }

            dynamic slideShowPresentation = null;
            dynamic app = null;
            dynamic activePresentation = null;

            try
            {
                // 获取放映窗口关联的演示文稿
                slideShowPresentation = slideShowWindow.Presentation;
                if (slideShowPresentation == null)
                {
                    LogHelper.WriteLogToFile("VerifyWPSActivePresentationMatch: SlideShowWindow.Presentation is null", LogHelper.LogType.Trace);
                    return false;
                }

                // 获取演示文稿的完整路径（如果有）
                string slideShowPresentationPath = TryGetDynamicProperty(() => slideShowPresentation.FullName, null, "Presentation.FullName");
                string slideShowPresentationName = TryGetDynamicProperty(() => slideShowPresentation.Name, null, "Presentation.Name");
                
                LogHelper.WriteLogToFile($"VerifyWPSActivePresentationMatch: SlideShow Presentation = '{slideShowPresentationName}' (Path: '{slideShowPresentationPath}')", LogHelper.LogType.Trace);

                // 获取 Application 的 ActivePresentation
                // 使用重试机制处理瞬时失效
                app = slideShowPresentation.Application;
                if (app == null)
                {
                    LogHelper.WriteLogToFile("VerifyWPSActivePresentationMatch: Cannot get Application from Presentation", LogHelper.LogType.Trace);
                    SafeReleaseComObject(slideShowPresentation);
                    return false;
                }

                activePresentation = GetActivePresentationWithRetry(app);
                if (activePresentation == null)
                {
                    LogHelper.WriteLogToFile("VerifyWPSActivePresentationMatch: ActivePresentation is null after retry", LogHelper.LogType.Trace);
                    SafeReleaseComObject(app);
                    SafeReleaseComObject(slideShowPresentation);
                    return false;
                }

                // 获取 ActivePresentation 的完整路径
                string activePresentationPath = TryGetDynamicProperty(() => activePresentation.FullName, null, "ActivePresentation.FullName");
                string activePresentationName = TryGetDynamicProperty(() => activePresentation.Name, null, "ActivePresentation.Name");
                
                LogHelper.WriteLogToFile($"VerifyWPSActivePresentationMatch: Active Presentation = '{activePresentationName}' (Path: '{activePresentationPath}')", LogHelper.LogType.Trace);

                // 比较演示文稿
                bool pathMatches = false;
                bool nameMatches = false;

                // 优先比较完整路径（更准确）
                if (!string.IsNullOrEmpty(slideShowPresentationPath) && !string.IsNullOrEmpty(activePresentationPath))
                {
                    pathMatches = string.Equals(slideShowPresentationPath, activePresentationPath, StringComparison.OrdinalIgnoreCase);
                    LogHelper.WriteLogToFile($"VerifyWPSActivePresentationMatch: Path comparison = {pathMatches}", LogHelper.LogType.Trace);
                }

                // 如果路径不可用或不匹配，比较名称
                if (!pathMatches && !string.IsNullOrEmpty(slideShowPresentationName) && !string.IsNullOrEmpty(activePresentationName))
                {
                    nameMatches = string.Equals(slideShowPresentationName, activePresentationName, StringComparison.OrdinalIgnoreCase);
                    LogHelper.WriteLogToFile($"VerifyWPSActivePresentationMatch: Name comparison = {nameMatches}", LogHelper.LogType.Trace);
                }

                // 释放 COM 对象
                SafeReleaseComObject(activePresentation);
                SafeReleaseComObject(app);
                SafeReleaseComObject(slideShowPresentation);

                bool matches = pathMatches || nameMatches;
                LogHelper.WriteLogToFile($"VerifyWPSActivePresentationMatch: Final result = {matches}", LogHelper.LogType.Info);
                
                return matches;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"VerifyWPSActivePresentationMatch: Error verifying presentation match: {ex.Message}", LogHelper.LogType.Warning);
                
                // 确保释放所有 COM 对象
                if (activePresentation != null)
                {
                    SafeReleaseComObject(activePresentation);
                }
                if (app != null)
                {
                    SafeReleaseComObject(app);
                }
                if (slideShowPresentation != null)
                {
                    SafeReleaseComObject(slideShowPresentation);
                }
                
                return false;
            }
        }

        #endregion

        #region Win32 API for Window Checking

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public int Width => Right - Left;
            public int Height => Bottom - Top;
        }

        #endregion

        #region SlideShowWindow HWND 获取

        /// <summary>
        /// 简单的 IDispatch 接口定义，用于通过 DispID 访问属性
        /// 这是一个早期绑定接口，用于在 COM 注册损坏时访问 SlideShowWindow 的 HWND
        /// </summary>
        [ComImport]
        [Guid("00020400-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
        private interface IDispatchEx
        {
            // IDispatch 方法
            void GetTypeInfoCount(out uint pctinfo);
            void GetTypeInfo(uint iTInfo, uint lcid, out IntPtr ppTInfo);
            void GetIDsOfNames(ref Guid riid, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr)] string[] rgszNames, uint cNames, uint lcid, [MarshalAs(UnmanagedType.LPArray)] int[] rgDispId);
            void Invoke(int dispIdMember, ref Guid riid, uint lcid, ushort wFlags, ref System.Runtime.InteropServices.ComTypes.DISPPARAMS pDispParams, out object pVarResult, out System.Runtime.InteropServices.ComTypes.EXCEPINFO pExcepInfo, out uint puArgErr);
        }

        /// <summary>
        /// 通过 IDispatch 属性扫描获取 SlideShowWindow 的 HWND
        /// 尝试通过 DispID 2010 获取 HWND 属性
        /// </summary>
        /// <param name="slideShowWindow">SlideShowWindow 对象（dynamic 类型）</param>
        /// <returns>窗口句柄，如果获取失败则返回 IntPtr.Zero</returns>
        private IntPtr GetSlideShowWindowHWND(dynamic slideShowWindow)
        {
            if (slideShowWindow == null)
            {
                LogHelper.WriteLogToFile("GetSlideShowWindowHWND: slideShowWindow is null", LogHelper.LogType.Warning);
                return IntPtr.Zero;
            }

            try
            {
                // 方法 1: 直接尝试访问 HWND 属性
                try
                {
                    int hwndInt = (int)slideShowWindow.HWND;
                    if (hwndInt != 0)
                    {
                        LogHelper.WriteLogToFile($"GetSlideShowWindowHWND: Got HWND via direct property access: 0x{hwndInt:X}", LogHelper.LogType.Info);
                        return new IntPtr(hwndInt);
                    }
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
                {
                    LogHelper.WriteLogToFile("GetSlideShowWindowHWND: HWND property not accessible via direct access", LogHelper.LogType.Trace);
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"GetSlideShowWindowHWND: Error accessing HWND property directly: {ex.Message}", LogHelper.LogType.Trace);
                }

                // 方法 2: 通过 IDispatch 接口和 DispID 访问
                // SlideShowWindow.HWND 的 DispID 通常是 2010
                try
                {
                    object comObject = slideShowWindow;
                    if (Marshal.IsComObject(comObject))
                    {
                        // 尝试通过反射调用 IDispatch.Invoke
                        Type type = comObject.GetType();
                        
                        // 尝试多个可能的 DispID (2001-2010)
                        int[] possibleDispIds = { 2010, 2009, 2008, 2007, 2006, 2005, 2004, 2003, 2002, 2001 };
                        
                        foreach (int dispId in possibleDispIds)
                        {
                            try
                            {
                                object result = type.InvokeMember(
                                    "[DispID=" + dispId + "]",
                                    System.Reflection.BindingFlags.GetProperty | System.Reflection.BindingFlags.InvokeMethod,
                                    null,
                                    comObject,
                                    null);
                                
                                if (result != null)
                                {
                                    int hwndInt = Convert.ToInt32(result);
                                    if (hwndInt != 0)
                                    {
                                        LogHelper.WriteLogToFile($"GetSlideShowWindowHWND: Got HWND via DispID {dispId}: 0x{hwndInt:X}", LogHelper.LogType.Info);
                                        return new IntPtr(hwndInt);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                LogHelper.WriteLogToFile($"GetSlideShowWindowHWND: DispID {dispId} failed: {ex.Message}", LogHelper.LogType.Trace);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"GetSlideShowWindowHWND: IDispatch method failed: {ex.Message}", LogHelper.LogType.Trace);
                }

                // 方法 3: 通过窗口标题和位置匹配（fallback）
                LogHelper.WriteLogToFile("GetSlideShowWindowHWND: Falling back to window title/position matching", LogHelper.LogType.Info);
                return GetSlideShowWindowHWNDByMatching(slideShowWindow);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"GetSlideShowWindowHWND: Unexpected error: {ex.Message}", LogHelper.LogType.Error);
                LogHelper.NewLog(ex);
                return IntPtr.Zero;
            }
        }

        #endregion

        /// <summary>
        /// 通过窗口标题和位置匹配获取 SlideShowWindow 的 HWND
        /// 这是一个 fallback 方法，当无法通过 IDispatch 获取 HWND 时使用
        /// </summary>
        /// <param name="slideShowWindow">SlideShowWindow 对象（dynamic 类型）</param>
        /// <returns>窗口句柄，如果无法确定则返回 IntPtr.Zero</returns>
        [RequiresUnmanagedCode("Uses Win32 API calls to enumerate windows and get window information.")]
        private IntPtr GetSlideShowWindowHWNDByMatching(dynamic slideShowWindow)
        {
            if (slideShowWindow == null)
            {
                LogHelper.WriteLogToFile("GetSlideShowWindowHWNDByMatching: slideShowWindow is null", LogHelper.LogType.Warning);
                return IntPtr.Zero;
            }

            try
            {
                // 获取演示文稿名称（用于窗口标题匹配）
                string presentationName = null;
                dynamic presentation = null;
                try
                {
                    presentation = slideShowWindow.Presentation;
                    if (presentation != null)
                    {
                        presentationName = TryGetDynamicProperty(() => presentation.Name, null, "Presentation.Name");
                        LogHelper.WriteLogToFile($"GetSlideShowWindowHWNDByMatching: Presentation name = '{presentationName}'", LogHelper.LogType.Trace);
                        
                        // 释放 Presentation 对象
                        SafeReleaseComObject(presentation);
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"GetSlideShowWindowHWNDByMatching: Failed to get presentation name: {ex.Message}", LogHelper.LogType.Trace);
                    // 确保释放 presentation
                    if (presentation != null)
                    {
                        SafeReleaseComObject(presentation);
                    }
                }

                // 获取窗口位置和尺寸（用于位置匹配）
                int expectedLeft = 0, expectedTop = 0, expectedWidth = 0, expectedHeight = 0;
                bool hasPositionInfo = false;
                
                try
                {
                    expectedLeft = TryGetDynamicProperty(() => (int)slideShowWindow.Left, 0, "SlideShowWindow.Left");
                    expectedTop = TryGetDynamicProperty(() => (int)slideShowWindow.Top, 0, "SlideShowWindow.Top");
                    expectedWidth = TryGetDynamicProperty(() => (int)slideShowWindow.Width, 0, "SlideShowWindow.Width");
                    expectedHeight = TryGetDynamicProperty(() => (int)slideShowWindow.Height, 0, "SlideShowWindow.Height");
                    
                    if (expectedWidth > 0 && expectedHeight > 0)
                    {
                        hasPositionInfo = true;
                        LogHelper.WriteLogToFile($"GetSlideShowWindowHWNDByMatching: Expected position = ({expectedLeft}, {expectedTop}), size = ({expectedWidth}x{expectedHeight})", LogHelper.LogType.Trace);
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"GetSlideShowWindowHWNDByMatching: Failed to get window position: {ex.Message}", LogHelper.LogType.Trace);
                }

                // 如果既没有演示文稿名称也没有位置信息，无法匹配
                if (string.IsNullOrEmpty(presentationName) && !hasPositionInfo)
                {
                    LogHelper.WriteLogToFile("GetSlideShowWindowHWNDByMatching: No presentation name or position info available for matching", LogHelper.LogType.Warning);
                    return IntPtr.Zero;
                }

                // 枚举所有窗口并查找匹配的窗口
                var matchingWindows = new List<IntPtr>();
                
                EnumWindows((hWnd, lParam) =>
                {
                    try
                    {
                        // 只检查可见窗口
                        if (!IsWindowVisible(hWnd))
                        {
                            return true; // 继续枚举
                        }

                        // 获取窗口标题
                        var titleBuilder = new System.Text.StringBuilder(256);
                        GetWindowText(hWnd, titleBuilder, titleBuilder.Capacity);
                        string windowTitle = titleBuilder.ToString();

                        bool titleMatches = false;
                        bool positionMatches = false;

                        // 检查标题是否匹配
                        if (!string.IsNullOrEmpty(presentationName) && !string.IsNullOrEmpty(windowTitle))
                        {
                            // 窗口标题通常包含演示文稿名称
                            // 例如: "演示文稿1.pptx - PowerPoint 幻灯片放映"
                            if (windowTitle.Contains(presentationName) || 
                                windowTitle.Contains(Path.GetFileNameWithoutExtension(presentationName)))
                            {
                                titleMatches = true;
                                LogHelper.WriteLogToFile($"GetSlideShowWindowHWNDByMatching: Title match found: '{windowTitle}' contains '{presentationName}'", LogHelper.LogType.Trace);
                            }
                        }

                        // 检查位置是否匹配
                        if (hasPositionInfo)
                        {
                            if (GetWindowRect(hWnd, out RECT rect))
                            {
                                // 允许一定的误差范围（±10 像素）
                                int tolerance = 10;
                                
                                bool leftMatches = Math.Abs(rect.Left - expectedLeft) <= tolerance;
                                bool topMatches = Math.Abs(rect.Top - expectedTop) <= tolerance;
                                bool widthMatches = Math.Abs(rect.Width - expectedWidth) <= tolerance;
                                bool heightMatches = Math.Abs(rect.Height - expectedHeight) <= tolerance;
                                
                                if (leftMatches && topMatches && widthMatches && heightMatches)
                                {
                                    positionMatches = true;
                                    LogHelper.WriteLogToFile($"GetSlideShowWindowHWNDByMatching: Position match found: HWND=0x{hWnd:X}, Rect=({rect.Left},{rect.Top},{rect.Width}x{rect.Height})", LogHelper.LogType.Trace);
                                }
                            }
                        }

                        // 如果标题和位置都匹配（或只有一个条件可用且匹配），则认为找到了窗口
                        bool isMatch = false;
                        if (!string.IsNullOrEmpty(presentationName) && hasPositionInfo)
                        {
                            // 两个条件都可用，必须都匹配
                            isMatch = titleMatches && positionMatches;
                        }
                        else if (!string.IsNullOrEmpty(presentationName))
                        {
                            // 只有标题可用
                            isMatch = titleMatches;
                        }
                        else if (hasPositionInfo)
                        {
                            // 只有位置可用
                            isMatch = positionMatches;
                        }

                        if (isMatch)
                        {
                            matchingWindows.Add(hWnd);
                            LogHelper.WriteLogToFile($"GetSlideShowWindowHWNDByMatching: Found matching window: HWND=0x{hWnd:X}, Title='{windowTitle}'", LogHelper.LogType.Info);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.WriteLogToFile($"GetSlideShowWindowHWNDByMatching: Error checking window 0x{hWnd:X}: {ex.Message}", LogHelper.LogType.Trace);
                    }

                    return true; // 继续枚举
                }, IntPtr.Zero);

                // 处理匹配结果
                if (matchingWindows.Count == 0)
                {
                    LogHelper.WriteLogToFile("GetSlideShowWindowHWNDByMatching: No matching windows found", LogHelper.LogType.Warning);
                    return IntPtr.Zero;
                }
                else if (matchingWindows.Count == 1)
                {
                    LogHelper.WriteLogToFile($"GetSlideShowWindowHWNDByMatching: Found exactly one matching window: 0x{matchingWindows[0]:X}", LogHelper.LogType.Info);
                    return matchingWindows[0];
                }
                else
                {
                    // 多个匹配，无法确定哪个是正确的窗口
                    // 为了确保准确性，返回 null
                    LogHelper.WriteLogToFile($"GetSlideShowWindowHWNDByMatching: Found {matchingWindows.Count} matching windows, cannot determine which is correct", LogHelper.LogType.Warning);
                    foreach (var hwnd in matchingWindows)
                    {
                        LogHelper.WriteLogToFile($"  - Candidate HWND: 0x{hwnd:X}", LogHelper.LogType.Trace);
                    }
                    return IntPtr.Zero;
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"GetSlideShowWindowHWNDByMatching: Unexpected error: {ex.Message}", LogHelper.LogType.Error);
                LogHelper.NewLog(ex);
                return IntPtr.Zero;
            }
        }

        #endregion

        #region WPS 特殊兼容性处理

        /// <summary>
        /// 检测是否为 WPS 应用程序
        /// 通过 Application.Name 或其他特征判断
        /// </summary>
        /// <param name="app">PowerPoint Application 对象（dynamic 类型）</param>
        /// <returns>如果是 WPS 则返回 true</returns>
        private bool IsWPSApplication(dynamic app)
        {
            if (app == null)
            {
                return false;
            }

            try
            {
                // 方法 1: 检查 Application.Name 属性
                string appName = TryGetDynamicProperty(() => app.Name, "", "Application.Name");
                
                if (!string.IsNullOrEmpty(appName))
                {
                    // WPS 的 Application.Name 可能包含 "WPS" 或 "Kingsoft"
                    // 注意: WPS2013 可能返回 "Microsoft PowerPoint"，需要其他方法判断
                    if (appName.Contains("WPS") || appName.Contains("Kingsoft"))
                    {
                        LogHelper.WriteLogToFile($"IsWPSApplication: Detected WPS by Application.Name = '{appName}'", LogHelper.LogType.Info);
                        return true;
                    }
                }

                // 方法 2: 检查 Application.Version 属性
                // WPS 的版本号格式与 PowerPoint 不同
                string version = TryGetDynamicProperty(() => app.Version, "", "Application.Version");
                if (!string.IsNullOrEmpty(version))
                {
                    // WPS 版本号通常以 "11." 开头（WPS2019）或其他特殊格式
                    // 这是一个较弱的判断，但可以作为辅助
                    LogHelper.WriteLogToFile($"IsWPSApplication: Application.Version = '{version}'", LogHelper.LogType.Trace);
                }

                // 方法 3: 检查 Application.Build 属性
                // WPS 的 Build 号与 PowerPoint 不同
                try
                {
                    string build = TryGetDynamicProperty(() => app.Build, "", "Application.Build");
                    if (!string.IsNullOrEmpty(build))
                    {
                        LogHelper.WriteLogToFile($"IsWPSApplication: Application.Build = '{build}'", LogHelper.LogType.Trace);
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"IsWPSApplication: Failed to get Build: {ex.Message}", LogHelper.LogType.Trace);
                }

                // 方法 4: 检查 ProgID
                // 通过 COM 对象的 ProgID 判断
                try
                {
                    var type = app.GetType();
                    string progId = type.FullName;
                    
                    if (!string.IsNullOrEmpty(progId))
                    {
                        // WPS 的 ProgID 通常包含 "kwpp" 或 "WPS"
                        if (progId.Contains("kwpp") || progId.Contains("WPS"))
                        {
                            LogHelper.WriteLogToFile($"IsWPSApplication: Detected WPS by ProgID = '{progId}'", LogHelper.LogType.Info);
                            return true;
                        }
                        
                        LogHelper.WriteLogToFile($"IsWPSApplication: ProgID = '{progId}'", LogHelper.LogType.Trace);
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"IsWPSApplication: Failed to get ProgID: {ex.Message}", LogHelper.LogType.Trace);
                }

                // 如果所有方法都无法确定，假设不是 WPS
                LogHelper.WriteLogToFile("IsWPSApplication: Cannot determine if WPS, assuming PowerPoint", LogHelper.LogType.Trace);
                return false;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"IsWPSApplication: Error detecting WPS: {ex.Message}", LogHelper.LogType.Warning);
                return false;
            }
        }

        /// <summary>
        /// 获取 WPS SlideShowWindow 的实际窗口尺寸
        /// WPS 返回幻灯片尺寸而非窗口尺寸，需要修正
        /// </summary>
        /// <param name="slideShowWindow">SlideShowWindow 对象（dynamic 类型）</param>
        /// <param name="hwnd">窗口句柄（如果已获取）</param>
        /// <returns>修正后的窗口尺寸 (Left, Top, Width, Height)，如果失败则返回 null</returns>
        [RequiresUnmanagedCode("Uses Win32 API GetWindowRect to get WPS slide show window size.")]
        private (int Left, int Top, int Width, int Height)? GetWPSSlideShowWindowSize(dynamic slideShowWindow, IntPtr hwnd)
        {
            if (slideShowWindow == null)
            {
                LogHelper.WriteLogToFile("GetWPSSlideShowWindowSize: slideShowWindow is null", LogHelper.LogType.Warning);
                return null;
            }

            try
            {
                // 方法 1: 如果有窗口句柄，直接通过 Win32 API 获取窗口尺寸
                if (hwnd != IntPtr.Zero)
                {
                    if (GetWindowRect(hwnd, out RECT rect))
                    {
                        LogHelper.WriteLogToFile($"GetWPSSlideShowWindowSize: Got window size from HWND: ({rect.Left}, {rect.Top}), {rect.Width}x{rect.Height}", LogHelper.LogType.Info);
                        return (rect.Left, rect.Top, rect.Width, rect.Height);
                    }
                    else
                    {
                        LogHelper.WriteLogToFile("GetWPSSlideShowWindowSize: Failed to get window rect from HWND", LogHelper.LogType.Warning);
                    }
                }

                // 方法 2: 尝试通过 SlideShowWindow 属性获取（可能返回幻灯片尺寸）
                int left = TryGetDynamicProperty(() => (int)slideShowWindow.Left, 0, "SlideShowWindow.Left");
                int top = TryGetDynamicProperty(() => (int)slideShowWindow.Top, 0, "SlideShowWindow.Top");
                int width = TryGetDynamicProperty(() => (int)slideShowWindow.Width, 0, "SlideShowWindow.Width");
                int height = TryGetDynamicProperty(() => (int)slideShowWindow.Height, 0, "SlideShowWindow.Height");

                if (width > 0 && height > 0)
                {
                    LogHelper.WriteLogToFile($"GetWPSSlideShowWindowSize: Got size from SlideShowWindow properties: ({left}, {top}), {width}x{height}", LogHelper.LogType.Trace);
                    LogHelper.WriteLogToFile("GetWPSSlideShowWindowSize: WARNING - WPS may return slide size instead of window size", LogHelper.LogType.Warning);
                    
                    // WPS 返回的可能是幻灯片尺寸（如 960x720），而非窗口尺寸
                    // 如果没有窗口句柄，我们只能返回这个值，但需要标记为可能不准确
                    return (left, top, width, height);
                }

                // 方法 3: 尝试通过窗口标题匹配获取窗口句柄，然后获取尺寸
                if (hwnd == IntPtr.Zero)
                {
                    LogHelper.WriteLogToFile("GetWPSSlideShowWindowSize: Attempting to get HWND by matching", LogHelper.LogType.Trace);
                    hwnd = GetSlideShowWindowHWNDByMatching(slideShowWindow);
                    
                    if (hwnd != IntPtr.Zero)
                    {
                        if (GetWindowRect(hwnd, out RECT rect))
                        {
                            LogHelper.WriteLogToFile($"GetWPSSlideShowWindowSize: Got window size from matched HWND: ({rect.Left}, {rect.Top}), {rect.Width}x{rect.Height}", LogHelper.LogType.Info);
                            return (rect.Left, rect.Top, rect.Width, rect.Height);
                        }
                    }
                }

                LogHelper.WriteLogToFile("GetWPSSlideShowWindowSize: Failed to get window size", LogHelper.LogType.Warning);
                return null;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"GetWPSSlideShowWindowSize: Error getting window size: {ex.Message}", LogHelper.LogType.Error);
                LogHelper.NewLog(ex);
                return null;
            }
        }

        /// <summary>
        /// 修正 WPS SlideShowWindow 的尺寸信息
        /// WPS 返回幻灯片尺寸而非窗口尺寸，需要通过窗口句柄获取真实尺寸
        /// </summary>
        /// <param name="target">PPT 绑定目标</param>
        private void CorrectWPSSlideShowWindowSize(PPTTarget target)
        {
            if (target == null || target.SlideShowWindow == null)
            {
                return;
            }

            try
            {
                // 检查是否为 WPS
                bool isWPS = IsWPSApplication(target.Application);
                if (!isWPS)
                {
                    LogHelper.WriteLogToFile("CorrectWPSSlideShowWindowSize: Not WPS, no correction needed", LogHelper.LogType.Trace);
                    return;
                }

                LogHelper.WriteLogToFile("CorrectWPSSlideShowWindowSize: Detected WPS, correcting window size", LogHelper.LogType.Info);

                // 获取修正后的窗口尺寸
                var correctedSize = GetWPSSlideShowWindowSize(target.SlideShowWindow, target.SlideShowWindowHWND);
                
                if (correctedSize.HasValue)
                {
                    LogHelper.WriteLogToFile($"CorrectWPSSlideShowWindowSize: Corrected size = ({correctedSize.Value.Left}, {correctedSize.Value.Top}), {correctedSize.Value.Width}x{correctedSize.Value.Height}", LogHelper.LogType.Info);
                    
                    // 这里可以将修正后的尺寸存储到 target 中，供后续使用
                    // 例如：target.CorrectedLeft = correctedSize.Value.Left;
                    // 但由于 PPTTarget 类目前没有这些字段，我们只记录日志
                    
                    // 如果需要，可以在 PPTTarget 类中添加以下字段：
                    // public int? CorrectedLeft { get; set; }
                    // public int? CorrectedTop { get; set; }
                    // public int? CorrectedWidth { get; set; }
                    // public int? CorrectedHeight { get; set; }
                }
                else
                {
                    LogHelper.WriteLogToFile("CorrectWPSSlideShowWindowSize: Failed to get corrected size", LogHelper.LogType.Warning);
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"CorrectWPSSlideShowWindowSize: Error correcting window size: {ex.Message}", LogHelper.LogType.Error);
                LogHelper.NewLog(ex);
            }
        }

        /// <summary>
        /// 处理 WPS 非全屏放映的边框窗口
        /// WPS 非全屏放映时，放映窗口外有一个边框窗口（框架窗口）
        /// </summary>
        /// <param name="slideShowWindowHWND">放映窗口句柄</param>
        /// <returns>边框窗口句柄，如果不存在则返回 IntPtr.Zero</returns>
        [RequiresUnmanagedCode("Uses Win32 API GetParent and IsWindowVisible to get WPS frame window.")]
        private IntPtr GetWPSFrameWindow(IntPtr slideShowWindowHWND)
        {
            if (slideShowWindowHWND == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            try
            {
                // WPS 非全屏放映时，放映窗口的父窗口就是边框窗口
                IntPtr parentWindow = GetParent(slideShowWindowHWND);
                
                if (parentWindow != IntPtr.Zero)
                {
                    LogHelper.WriteLogToFile($"GetWPSFrameWindow: Found parent window (frame): 0x{parentWindow:X}", LogHelper.LogType.Trace);
                    
                    // 验证父窗口是否可见
                    if (IsWindowVisible(parentWindow))
                    {
                        LogHelper.WriteLogToFile($"GetWPSFrameWindow: Frame window is visible", LogHelper.LogType.Trace);
                        return parentWindow;
                    }
                    else
                    {
                        LogHelper.WriteLogToFile($"GetWPSFrameWindow: Frame window is not visible", LogHelper.LogType.Trace);
                    }
                }
                else
                {
                    LogHelper.WriteLogToFile("GetWPSFrameWindow: No parent window found", LogHelper.LogType.Trace);
                }

                return IntPtr.Zero;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"GetWPSFrameWindow: Error getting frame window: {ex.Message}", LogHelper.LogType.Warning);
                return IntPtr.Zero;
            }
        }

        /// <summary>
        /// 检查 WPS 是否为全屏放映模式
        /// </summary>
        /// <param name="slideShowWindow">SlideShowWindow 对象（dynamic 类型）</param>
        /// <returns>如果是全屏放映则返回 true</returns>
        [RequiresUnmanagedCode("Uses Win32 API calls to check WPS full screen mode.")]
        private bool IsWPSFullScreenMode(dynamic slideShowWindow)
        {
            if (slideShowWindow == null)
            {
                return false;
            }

            try
            {
                // 方法 1: 检查 View.State 属性
                bool isFullScreen = TryGetDynamicProperty(
                    () => slideShowWindow.View.State == PpSlideShowState.ppSlideShowRunning,
                    false,
                    "SlideShowWindow.View.State");
                
                if (isFullScreen)
                {
                    LogHelper.WriteLogToFile("IsWPSFullScreenMode: Detected full screen mode via View.State", LogHelper.LogType.Trace);
                    return true;
                }

                // 方法 2: 通过窗口尺寸判断
                // 全屏窗口的尺寸通常等于屏幕尺寸
                int width = TryGetDynamicProperty(() => (int)slideShowWindow.Width, 0, "SlideShowWindow.Width");
                int height = TryGetDynamicProperty(() => (int)slideShowWindow.Height, 0, "SlideShowWindow.Height");
                
                if (width > 0 && height > 0)
                {
                    // 获取主屏幕尺寸
                    int screenWidth = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;
                    int screenHeight = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;
                    
                    // 允许一定的误差范围（±50 像素）
                    int tolerance = 50;
                    bool widthMatches = Math.Abs(width - screenWidth) <= tolerance;
                    bool heightMatches = Math.Abs(height - screenHeight) <= tolerance;
                    
                    if (widthMatches && heightMatches)
                    {
                        LogHelper.WriteLogToFile($"IsWPSFullScreenMode: Detected full screen mode via window size: {width}x{height} ≈ {screenWidth}x{screenHeight}", LogHelper.LogType.Trace);
                        return true;
                    }
                    else
                    {
                        LogHelper.WriteLogToFile($"IsWPSFullScreenMode: Not full screen mode: {width}x{height} vs {screenWidth}x{screenHeight}", LogHelper.LogType.Trace);
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"IsWPSFullScreenMode: Error checking full screen mode: {ex.Message}", LogHelper.LogType.Warning);
                return false;
            }
        }

        /// <summary>
        /// WPS 版本枚举
        /// 用于识别不同版本的 WPS 并应用相应的降级处理
        /// </summary>
        private enum WPSVersion
        {
            /// <summary>
            /// 未知版本或非 WPS
            /// </summary>
            Unknown = 0,

            /// <summary>
            /// WPS 2007 或更早版本
            /// 问题：无法获取当前页，只能获取总页数
            /// </summary>
            WPS2007 = 2007,

            /// <summary>
            /// WPS 2013
            /// 问题：Application.Name 返回 "Microsoft PowerPoint"
            /// </summary>
            WPS2013 = 2013,

            /// <summary>
            /// WPS 2019 或更新版本
            /// 问题：SlideShowWindow 只返回 View 属性，其他属性不可访问
            /// </summary>
            WPS2019 = 2019
        }

        /// <summary>
        /// 检测 WPS 版本
        /// 根据不同版本的特征判断 WPS 版本号
        /// </summary>
        /// <param name="app">PowerPoint Application 对象（dynamic 类型）</param>
        /// <returns>WPS 版本枚举值</returns>
        private WPSVersion DetectWPSVersion(dynamic app)
        {
            if (app == null)
            {
                return WPSVersion.Unknown;
            }

            try
            {
                // 首先确认是否为 WPS
                if (!IsWPSApplication(app))
                {
                    LogHelper.WriteLogToFile("DetectWPSVersion: Not a WPS application", LogHelper.LogType.Trace);
                    return WPSVersion.Unknown;
                }

                // 方法 1: 通过 Application.Version 属性判断
                string version = TryGetDynamicProperty(() => app.Version, "", "Application.Version");
                if (!string.IsNullOrEmpty(version))
                {
                    LogHelper.WriteLogToFile($"DetectWPSVersion: Application.Version = '{version}'", LogHelper.LogType.Info);

                    // WPS 版本号格式分析：
                    // WPS2019: 通常以 "11." 开头
                    // WPS2013: 通常以 "9." 或 "10." 开头
                    // WPS2007: 通常以 "8." 或更早版本开头
                    
                    if (version.StartsWith("11.") || version.StartsWith("12."))
                    {
                        LogHelper.WriteLogToFile("DetectWPSVersion: Detected WPS2019 or later by version number", LogHelper.LogType.Info);
                        return WPSVersion.WPS2019;
                    }
                    else if (version.StartsWith("9.") || version.StartsWith("10."))
                    {
                        LogHelper.WriteLogToFile("DetectWPSVersion: Detected WPS2013 by version number", LogHelper.LogType.Info);
                        return WPSVersion.WPS2013;
                    }
                    else if (version.StartsWith("8.") || version.StartsWith("7."))
                    {
                        LogHelper.WriteLogToFile("DetectWPSVersion: Detected WPS2007 or earlier by version number", LogHelper.LogType.Info);
                        return WPSVersion.WPS2007;
                    }
                }

                // 方法 2: 通过 Application.Name 判断
                // WPS2013 的 Application.Name 返回 "Microsoft PowerPoint"，这是一个特殊标识
                string appName = TryGetDynamicProperty(() => app.Name, "", "Application.Name");
                if (!string.IsNullOrEmpty(appName))
                {
                    LogHelper.WriteLogToFile($"DetectWPSVersion: Application.Name = '{appName}'", LogHelper.LogType.Trace);

                    if (appName.Contains("Microsoft PowerPoint") && IsWPSApplication(app))
                    {
                        // 如果 Name 是 "Microsoft PowerPoint" 但我们已经确认是 WPS，那很可能是 WPS2013
                        LogHelper.WriteLogToFile("DetectWPSVersion: Detected WPS2013 by Application.Name (Microsoft PowerPoint)", LogHelper.LogType.Info);
                        return WPSVersion.WPS2013;
                    }
                }

                // 方法 3: 通过功能特征判断
                // 尝试访问 SlideShowWindow 的属性来判断版本
                dynamic slideShowWindows = null;
                dynamic slideShowWindow = null;
                
                try
                {
                    slideShowWindows = app.SlideShowWindows;
                    if (slideShowWindows != null && slideShowWindows.Count > 0)
                    {
                        slideShowWindow = slideShowWindows[1];
                        if (slideShowWindow != null)
                        {
                            // 尝试访问 Width 属性
                            // WPS2019 无法访问 Width 等属性（除了 View）
                            bool canAccessWidth = false;
                            try
                            {
                                _ = (int)slideShowWindow.Width;
                                canAccessWidth = true;
                                LogHelper.WriteLogToFile("DetectWPSVersion: 可以访问 SlideShowWindow.Width", LogHelper.LogType.Trace);
                            }
                            catch (Exception ex)
                            {
                                LogHelper.WriteLogToFile($"DetectWPSVersion: 无法访问 SlideShowWindow.Width: {ex.Message}", LogHelper.LogType.Trace);
                            }

                            // 尝试访问 View 属性
                            bool canAccessView = false;
                            dynamic view = null;
                            try
                            {
                                view = slideShowWindow.View;
                                if (view != null)
                                {
                                    canAccessView = true;
                                    LogHelper.WriteLogToFile("DetectWPSVersion: 可以访问 SlideShowWindow.View", LogHelper.LogType.Trace);
                                    SafeReleaseComObject(view);
                                }
                            }
                            catch (Exception ex)
                            {
                                LogHelper.WriteLogToFile($"DetectWPSVersion: 无法访问 SlideShowWindow.View: {ex.Message}", LogHelper.LogType.Trace);
                                if (view != null)
                                {
                                    SafeReleaseComObject(view);
                                }
                            }

                            // 如果只能访问 View 而不能访问 Width，很可能是 WPS2019
                            if (canAccessView && !canAccessWidth)
                            {
                                LogHelper.WriteLogToFile("DetectWPSVersion: Detected WPS2019 by property access pattern (View only)", LogHelper.LogType.Info);
                                SafeReleaseComObject(slideShowWindow);
                                SafeReleaseComObject(slideShowWindows);
                                return WPSVersion.WPS2019;
                            }

                            SafeReleaseComObject(slideShowWindow);
                        }
                        SafeReleaseComObject(slideShowWindows);
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"DetectWPSVersion: Error checking SlideShowWindow properties: {ex.Message}", LogHelper.LogType.Trace);
                    // 确保释放 COM 对象
                    if (slideShowWindow != null)
                    {
                        SafeReleaseComObject(slideShowWindow);
                    }
                    if (slideShowWindows != null)
                    {
                        SafeReleaseComObject(slideShowWindows);
                    }
                }

                // 如果无法确定具体版本，但确认是 WPS，返回 Unknown
                LogHelper.WriteLogToFile("DetectWPSVersion: Cannot determine specific WPS version, returning Unknown", LogHelper.LogType.Warning);
                return WPSVersion.Unknown;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"DetectWPSVersion: Error detecting WPS version: {ex.Message}", LogHelper.LogType.Error);
                return WPSVersion.Unknown;
            }
        }

        /// <summary>
        /// 获取当前幻灯片索引（支持 WPS 版本降级处理）
        /// 根据不同的 WPS 版本应用不同的获取策略
        /// </summary>
        /// <param name="slideShowWindow">SlideShowWindow 对象（dynamic 类型）</param>
        /// <param name="app">PowerPoint Application 对象（dynamic 类型，用于版本检测）</param>
        /// <returns>当前幻灯片索引，如果获取失败则返回 -1</returns>
        private int GetCurrentSlideIndexWithWPSSupport(dynamic slideShowWindow, dynamic app)
        {
            if (slideShowWindow == null)
            {
                LogHelper.WriteLogToFile("GetCurrentSlideIndexWithWPSSupport: slideShowWindow is null", LogHelper.LogType.Warning);
                return -1;
            }

            dynamic view = null;
            dynamic slide = null;

            try
            {
                // 检测 WPS 版本
                WPSVersion wpsVersion = DetectWPSVersion(app);
                LogHelper.WriteLogToFile($"GetCurrentSlideIndexWithWPSSupport: Detected WPS version = {wpsVersion}", LogHelper.LogType.Trace);

                // 获取 View 对象
                view = slideShowWindow.View;
                if (view == null)
                {
                    LogHelper.WriteLogToFile("GetCurrentSlideIndexWithWPSSupport: View is null", LogHelper.LogType.Warning);
                    return -1;
                }

                // WPS2007 特殊处理：无法获取当前页，只能获取总页数
                if (wpsVersion == WPSVersion.WPS2007)
                {
                    LogHelper.WriteLogToFile("GetCurrentSlideIndexWithWPSSupport: WPS2007 detected - cannot get current slide index, only total count", LogHelper.LogType.Warning);
                    
                    // 尝试获取总页数（作为降级方案）
                    try
                    {
                        dynamic presentation = view.Presentation;
                        if (presentation != null)
                        {
                            dynamic slides = presentation.Slides;
                            if (slides != null)
                            {
                                int totalCount = slides.Count;
                                LogHelper.WriteLogToFile($"GetCurrentSlideIndexWithWPSSupport: WPS2007 - Total slides = {totalCount}", LogHelper.LogType.Info);
                                
                                // 释放 COM 对象
                                SafeReleaseComObject(slides);
                                SafeReleaseComObject(presentation);
                                SafeReleaseComObject(view);
                                
                                // 返回 -1 表示无法获取当前页
                                return -1;
                            }
                            SafeReleaseComObject(presentation);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.WriteLogToFile($"GetCurrentSlideIndexWithWPSSupport: WPS2007 - Error getting total count: {ex.Message}", LogHelper.LogType.Warning);
                    }
                    
                    SafeReleaseComObject(view);
                    return -1;
                }

                // 标准方法：通过 View.Slide.SlideIndex 获取
                // 这适用于 PowerPoint 和大多数 WPS 版本
                try
                {
                    slide = view.Slide;
                    if (slide == null)
                    {
                        LogHelper.WriteLogToFile("GetCurrentSlideIndexWithWPSSupport: Slide is null", LogHelper.LogType.Warning);
                        SafeReleaseComObject(view);
                        return -1;
                    }

                    // 获取 SlideIndex
                    int slideIndex = TryGetDynamicProperty(() => slide.SlideIndex, -1, "Slide.SlideIndex");
                    
                    if (slideIndex != -1)
                    {
                        LogHelper.WriteLogToFile($"GetCurrentSlideIndexWithWPSSupport: Current slide index = {slideIndex}", LogHelper.LogType.Trace);
                    }
                    else
                    {
                        LogHelper.WriteLogToFile("GetCurrentSlideIndexWithWPSSupport: Failed to get SlideIndex", LogHelper.LogType.Warning);
                    }

                    // 释放 COM 对象
                    SafeReleaseComObject(slide);
                    SafeReleaseComObject(view);

                    return slideIndex;
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"GetCurrentSlideIndexWithWPSSupport: Error getting slide index: {ex.Message}", LogHelper.LogType.Warning);
                    
                    // 确保释放 COM 对象
                    if (slide != null)
                    {
                        SafeReleaseComObject(slide);
                    }
                    SafeReleaseComObject(view);
                    
                    return -1;
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"GetCurrentSlideIndexWithWPSSupport: Unexpected error: {ex.Message}", LogHelper.LogType.Error);
                
                // 确保释放 COM 对象
                if (slide != null)
                {
                    SafeReleaseComObject(slide);
                }
                if (view != null)
                {
                    SafeReleaseComObject(view);
                }
                
                return -1;
            }
        }

        /// <summary>
        /// 获取幻灯片总数（支持 WPS 版本降级处理）
        /// </summary>
        /// <param name="presentation">Presentation 对象（dynamic 类型）</param>
        /// <returns>幻灯片总数，如果获取失败则返回 0</returns>
        private int GetSlidesCountWithWPSSupport(dynamic presentation)
        {
            if (presentation == null)
            {
                LogHelper.WriteLogToFile("GetSlidesCountWithWPSSupport: presentation is null", LogHelper.LogType.Warning);
                return 0;
            }

            dynamic slides = null;

            try
            {
                slides = presentation.Slides;
                if (slides == null)
                {
                    LogHelper.WriteLogToFile("GetSlidesCountWithWPSSupport: Slides collection is null", LogHelper.LogType.Warning);
                    return 0;
                }

                int count = TryGetDynamicProperty(() => slides.Count, 0, "Slides.Count");
                LogHelper.WriteLogToFile($"GetSlidesCountWithWPSSupport: Slides count = {count}", LogHelper.LogType.Trace);

                // 释放 COM 对象
                SafeReleaseComObject(slides);

                return count;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"GetSlidesCountWithWPSSupport: Error getting slides count: {ex.Message}", LogHelper.LogType.Error);
                
                // 确保释放 COM 对象
                if (slides != null)
                {
                    SafeReleaseComObject(slides);
                }
                
                return 0;
            }
        }

        /// <summary>
        /// 检查 SlideShowWindow 是否有效（支持 WPS 版本降级处理）
        /// WPS2019 只能访问 View 属性，其他属性不可访问
        /// </summary>
        /// <param name="slideShowWindow">SlideShowWindow 对象（dynamic 类型）</param>
        /// <param name="wpsVersion">WPS 版本（如果已知）</param>
        /// <returns>如果窗口有效则返回 true</returns>
        private bool IsSlideShowWindowValidWithWPSSupport(dynamic slideShowWindow, WPSVersion wpsVersion = WPSVersion.Unknown)
        {
            if (slideShowWindow == null)
            {
                return false;
            }

            dynamic view = null;

            try
            {
                // WPS2019 特殊处理：只检查 View 属性
                if (wpsVersion == WPSVersion.WPS2019)
                {
                    LogHelper.WriteLogToFile("IsSlideShowWindowValidWithWPSSupport: WPS2019 detected - checking View only", LogHelper.LogType.Trace);
                    
                    try
                    {
                        view = slideShowWindow.View;
                        if (view != null)
                        {
                            SafeReleaseComObject(view);
                            return true;
                        }
                        return false;
                    }
                    catch (System.Runtime.InteropServices.COMException ex)
                    {
                        LogHelper.WriteLogToFile($"IsSlideShowWindowValidWithWPSSupport: WPS2019 - View access failed: {ex.Message}", LogHelper.LogType.Trace);
                        if (view != null)
                        {
                            SafeReleaseComObject(view);
                        }
                        return false;
                    }
                }

                // 标准检查：尝试访问 View 属性
                try
                {
                    view = slideShowWindow.View;
                    if (view != null)
                    {
                        SafeReleaseComObject(view);
                        return true;
                    }
                    return false;
                }
                catch (System.Runtime.InteropServices.COMException ex)
                {
                    LogHelper.WriteLogToFile($"IsSlideShowWindowValidWithWPSSupport: View access failed: {ex.Message}", LogHelper.LogType.Trace);
                    if (view != null)
                    {
                        SafeReleaseComObject(view);
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"IsSlideShowWindowValidWithWPSSupport: Error checking window validity: {ex.Message}", LogHelper.LogType.Warning);
                if (view != null)
                {
                    SafeReleaseComObject(view);
                }
                return false;
            }
        }

        #endregion

        #region 动态绑定切换

        /// <summary>
        /// 判断是否应该切换绑定到新的 PPT 目标
        /// </summary>
        /// <param name="current">当前绑定的目标</param>
        /// <param name="candidate">候选目标</param>
        /// <returns>如果应该切换则返回 true</returns>
        private bool ShouldSwitchBinding(PPTTarget current, PPTTarget candidate)
        {
            // 如果候选目标无效，不切换
            if (candidate == null || candidate.Priority == PPTBindingPriority.None)
            {
                LogHelper.WriteLogToFile("ShouldSwitchBinding: Candidate is invalid, not switching", LogHelper.LogType.Trace);
                return false;
            }

            // 如果当前没有绑定，切换到候选目标
            if (current == null || current.Priority == PPTBindingPriority.None)
            {
                LogHelper.WriteLogToFile($"ShouldSwitchBinding: No current binding, switching to candidate (Priority={candidate.Priority})", LogHelper.LogType.Info);
                return true;
            }

            // 规则 1: 优先级更高的优先
            if (candidate.Priority > current.Priority)
            {
                LogHelper.WriteLogToFile($"ShouldSwitchBinding: Candidate has higher priority ({candidate.Priority} > {current.Priority}), switching", LogHelper.LogType.Info);
                return true;
            }

            // 规则 2: 优先级相同时，检查是否为不同的演示文稿
            if (candidate.Priority == current.Priority)
            {
                // 比较演示文稿对象
                bool isDifferentPresentation = !AreSamePresentations(current.Presentation, candidate.Presentation);
                
                if (isDifferentPresentation)
                {
                    LogHelper.WriteLogToFile($"ShouldSwitchBinding: Same priority but different presentation, switching from '{current.PresentationName}' to '{candidate.PresentationName}'", LogHelper.LogType.Info);
                    return true;
                }
                else
                {
                    LogHelper.WriteLogToFile($"ShouldSwitchBinding: Same priority and same presentation, not switching", LogHelper.LogType.Trace);
                    return false;
                }
            }

            // 规则 3: 候选优先级更低，不切换
            LogHelper.WriteLogToFile($"ShouldSwitchBinding: Candidate has lower priority ({candidate.Priority} < {current.Priority}), not switching", LogHelper.LogType.Trace);
            return false;
        }

        /// <summary>
        /// 比较两个演示文稿对象是否相同
        /// </summary>
        /// <param name="pres1">演示文稿 1</param>
        /// <param name="pres2">演示文稿 2</param>
        /// <returns>如果是同一个演示文稿则返回 true</returns>
        private bool AreSamePresentations(dynamic pres1, dynamic pres2)
        {
            if (pres1 == null && pres2 == null)
            {
                return true;
            }

            if (pres1 == null || pres2 == null)
            {
                return false;
            }

            try
            {
                // 方法 1: 比较对象引用
                if (ReferenceEquals(pres1, pres2))
                {
                    return true;
                }

                // 方法 2: 比较演示文稿名称
                string name1 = TryGetDynamicProperty(() => pres1.Name, "", "Presentation1.Name");
                string name2 = TryGetDynamicProperty(() => pres2.Name, "", "Presentation2.Name");
                
                if (!string.IsNullOrEmpty(name1) && !string.IsNullOrEmpty(name2))
                {
                    bool sameByName = string.Equals(name1, name2, StringComparison.OrdinalIgnoreCase);
                    LogHelper.WriteLogToFile($"AreSamePresentations: Comparing by name: '{name1}' vs '{name2}' = {sameByName}", LogHelper.LogType.Trace);
                    return sameByName;
                }

                // 方法 3: 比较完整路径
                string path1 = TryGetDynamicProperty(() => pres1.FullName, "", "Presentation1.FullName");
                string path2 = TryGetDynamicProperty(() => pres2.FullName, "", "Presentation2.FullName");
                
                if (!string.IsNullOrEmpty(path1) && !string.IsNullOrEmpty(path2))
                {
                    bool sameByPath = string.Equals(path1, path2, StringComparison.OrdinalIgnoreCase);
                    LogHelper.WriteLogToFile($"AreSamePresentations: Comparing by path: '{path1}' vs '{path2}' = {sameByPath}", LogHelper.LogType.Trace);
                    return sameByPath;
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"AreSamePresentations: Error comparing presentations: {ex.Message}", LogHelper.LogType.Warning);
            }

            // 无法确定，假设不同
            return false;
        }

        /// <summary>
        /// 切换绑定到新的 PPT 目标
        /// 释放旧绑定的 RCW 引用
        /// </summary>
        /// <param name="newTarget">新的绑定目标</param>
        /// <returns>是否成功切换</returns>
        private bool SwitchBinding(PPTTarget newTarget)
        {
            if (newTarget == null)
            {
                LogHelper.WriteLogToFile("SwitchBinding: New target is null, cannot switch", LogHelper.LogType.Warning);
                return false;
            }

            try
            {
                LogHelper.WriteLogToFile($"SwitchBinding: Switching to new target (Priority={newTarget.Priority}, Presentation='{newTarget.PresentationName}')", LogHelper.LogType.Info);

                // 释放旧绑定的 RCW
                if (_currentTarget != null)
                {
                    ReleaseTargetComObjects(_currentTarget);
                }

                // 更新当前绑定
                _currentTarget = newTarget;

                // 更新服务的内部状态（如果需要）
                UpdateInternalStateFromTarget(newTarget);

                LogHelper.WriteLogToFile("SwitchBinding: Binding switched successfully", LogHelper.LogType.Info);
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"SwitchBinding: Error switching binding: {ex.Message}", LogHelper.LogType.Error);
                LogHelper.NewLog(ex);
                return false;
            }
        }

        /// <summary>
        /// 释放 PPTTarget 中的 COM 对象引用
        /// 精确管理 RCW 引用计数
        /// </summary>
        /// <param name="target">要释放的目标</param>
        private void ReleaseTargetComObjects(PPTTarget target)
        {
            if (target == null)
            {
                return;
            }

            try
            {
                LogHelper.WriteLogToFile($"ReleaseTargetComObjects: Releasing COM objects for target (Presentation='{target.PresentationName}')", LogHelper.LogType.Trace);

                // 释放 SlideShowWindow
                if (target.SlideShowWindow != null)
                {
                    SafeReleaseComObject(target.SlideShowWindow);
                    target.SlideShowWindow = null;
                }

                // 释放 Presentation
                if (target.Presentation != null)
                {
                    SafeReleaseComObject(target.Presentation);
                    target.Presentation = null;
                }

                // 释放 Application
                // 注意: Application 对象通常不应该释放，因为它可能被其他地方引用
                // 但在增强模式下，我们从 ROT 获取的是新的引用，需要释放
                if (target.Application != null)
                {
                    SafeReleaseComObject(target.Application);
                    target.Application = null;
                }

                LogHelper.WriteLogToFile("ReleaseTargetComObjects: COM objects released", LogHelper.LogType.Trace);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"ReleaseTargetComObjects: Error releasing COM objects: {ex.Message}", LogHelper.LogType.Warning);
            }
        }

        /// <summary>
        /// 从 PPTTarget 更新服务的内部状态
        /// 将 dynamic 对象转换为强类型对象（如果可能）
        /// </summary>
        /// <param name="target">绑定目标</param>
        private void UpdateInternalStateFromTarget(PPTTarget target)
        {
            if (target == null)
            {
                return;
            }

            try
            {
                // 尝试将 dynamic 对象转换为强类型对象
                // 这在 COM 注册正常的情况下可以工作
                // 在 COM 注册损坏的情况下，我们保持使用 dynamic 对象

                // 更新 Application
                if (target.Application != null)
                {
                    try
                    {
                        _pptApplication = (Microsoft.Office.Interop.PowerPoint.Application)target.Application;
                        LogHelper.WriteLogToFile("UpdateInternalStateFromTarget: Successfully cast Application to strong type", LogHelper.LogType.Trace);
                    }
                    catch (InvalidCastException)
                    {
                        // COM 注册损坏，无法转换，保持 null
                        _pptApplication = null;
                        LogHelper.WriteLogToFile("UpdateInternalStateFromTarget: Cannot cast Application to strong type (COM registration issue)", LogHelper.LogType.Trace);
                    }
                }

                // 更新 Presentation
                if (target.Presentation != null)
                {
                    try
                    {
                        _presentation = (Presentation)target.Presentation;
                        
                        // 更新 Slides
                        _slides = _presentation.Slides;
                        _slidesCount = _slides.Count;
                        _memoryStreams = new MemoryStream[_slidesCount + 2];
                        
                        LogHelper.WriteLogToFile($"UpdateInternalStateFromTarget: Successfully cast Presentation to strong type, Slides={_slidesCount}", LogHelper.LogType.Trace);
                    }
                    catch (InvalidCastException)
                    {
                        // COM 注册损坏，无法转换
                        _presentation = null;
                        _slides = null;
                        LogHelper.WriteLogToFile("UpdateInternalStateFromTarget: Cannot cast Presentation to strong type (COM registration issue)", LogHelper.LogType.Trace);
                    }
                }

                LogHelper.WriteLogToFile("UpdateInternalStateFromTarget: Internal state updated", LogHelper.LogType.Trace);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"UpdateInternalStateFromTarget: Error updating internal state: {ex.Message}", LogHelper.LogType.Warning);
            }
        }

        #endregion

        #region 事件注册和轮询

        /// <summary>
        /// 尝试注册 PPT 事件
        /// 包括 SlideShowNextSlide、SlideShowBegin、SlideShowEnd 等
        /// </summary>
        /// <param name="app">PowerPoint Application 对象（dynamic 类型）</param>
        /// <returns>如果事件注册成功则返回 true</returns>
        private bool TryRegisterPPTEvents(dynamic app)
        {
            if (app == null)
            {
                LogHelper.WriteLogToFile("TryRegisterPPTEvents: Application is null", LogHelper.LogType.Warning);
                return false;
            }

            try
            {
                LogHelper.WriteLogToFile("TryRegisterPPTEvents: Attempting to register PPT events", LogHelper.LogType.Info);

                // 尝试注册 SlideShowNextSlide 事件
                try
                {
                    // 使用 dynamic 类型注册事件
                    // 注意：在 COM 注册损坏的情况下，事件注册可能会失败
                    app.SlideShowNextSlide += new EApplication_SlideShowNextSlideEventHandler(OnSlideShowNextSlide);
                    LogHelper.WriteLogToFile("TryRegisterPPTEvents: Successfully registered SlideShowNextSlide event", LogHelper.LogType.Info);
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"TryRegisterPPTEvents: Failed to register SlideShowNextSlide event: {ex.Message}", LogHelper.LogType.Warning);
                    return false;
                }

                // 尝试注册 SlideShowBegin 事件
                try
                {
                    app.SlideShowBegin += new EApplication_SlideShowBeginEventHandler(OnSlideShowBegin);
                    LogHelper.WriteLogToFile("TryRegisterPPTEvents: Successfully registered SlideShowBegin event", LogHelper.LogType.Info);
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"TryRegisterPPTEvents: Failed to register SlideShowBegin event: {ex.Message}", LogHelper.LogType.Warning);
                    // SlideShowBegin 失败不影响整体成功，因为 SlideShowNextSlide 是主要的
                }

                // 尝试注册 SlideShowEnd 事件
                try
                {
                    app.SlideShowEnd += new EApplication_SlideShowEndEventHandler(OnSlideShowEnd);
                    LogHelper.WriteLogToFile("TryRegisterPPTEvents: Successfully registered SlideShowEnd event", LogHelper.LogType.Info);
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"TryRegisterPPTEvents: Failed to register SlideShowEnd event: {ex.Message}", LogHelper.LogType.Warning);
                    // SlideShowEnd 失败不影响整体成功
                }

                // 如果至少 SlideShowNextSlide 注册成功，则认为事件注册成功
                _eventRegistrationSucceeded = true;
                LogHelper.WriteLogToFile("TryRegisterPPTEvents: Event registration succeeded", LogHelper.LogType.Info);
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"TryRegisterPPTEvents: Event registration failed: {ex.Message}", LogHelper.LogType.Error);
                LogHelper.NewLog(ex);
                _eventRegistrationSucceeded = false;
                return false;
            }
        }

        /// <summary>
        /// 取消注册 PPT 事件
        /// </summary>
        /// <param name="app">PowerPoint Application 对象（dynamic 类型）</param>
        private void UnregisterPPTEvents(dynamic app)
        {
            if (app == null)
            {
                return;
            }

            try
            {
                LogHelper.WriteLogToFile("UnregisterPPTEvents: Attempting to unregister PPT events", LogHelper.LogType.Info);

                // 取消注册 SlideShowNextSlide 事件
                try
                {
                    app.SlideShowNextSlide -= new EApplication_SlideShowNextSlideEventHandler(OnSlideShowNextSlide);
                    LogHelper.WriteLogToFile("UnregisterPPTEvents: Successfully unregistered SlideShowNextSlide event", LogHelper.LogType.Trace);
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"UnregisterPPTEvents: Failed to unregister SlideShowNextSlide event: {ex.Message}", LogHelper.LogType.Trace);
                }

                // 取消注册 SlideShowBegin 事件
                try
                {
                    app.SlideShowBegin -= new EApplication_SlideShowBeginEventHandler(OnSlideShowBegin);
                    LogHelper.WriteLogToFile("UnregisterPPTEvents: Successfully unregistered SlideShowBegin event", LogHelper.LogType.Trace);
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"UnregisterPPTEvents: Failed to unregister SlideShowBegin event: {ex.Message}", LogHelper.LogType.Trace);
                }

                // 取消注册 SlideShowEnd 事件
                try
                {
                    app.SlideShowEnd -= new EApplication_SlideShowEndEventHandler(OnSlideShowEnd);
                    LogHelper.WriteLogToFile("UnregisterPPTEvents: Successfully unregistered SlideShowEnd event", LogHelper.LogType.Trace);
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"UnregisterPPTEvents: Failed to unregister SlideShowEnd event: {ex.Message}", LogHelper.LogType.Trace);
                }

                _eventRegistrationSucceeded = false;
                LogHelper.WriteLogToFile("UnregisterPPTEvents: Event unregistration completed", LogHelper.LogType.Info);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"UnregisterPPTEvents: Error during event unregistration: {ex.Message}", LogHelper.LogType.Warning);
            }
        }

        /// <summary>
        /// 启动轮询
        /// 根据事件注册状态选择轮询间隔：
        /// - 事件注册失败：500ms 快速轮询
        /// - 事件注册成功：3000ms 慢速轮询（作为备份）
        /// </summary>
        private void StartPolling()
        {
            try
            {
                // 如果轮询已经在运行，先停止
                if (_isPollingActive)
                {
                    StopPolling();
                }

                // 根据事件注册状态设置轮询间隔
                if (_eventRegistrationSucceeded)
                {
                    _pollingInterval = 3000; // 3 秒慢速轮询（事件成功时作为备份）
                    LogHelper.WriteLogToFile("StartPolling: Event registration succeeded, using slow polling (3000ms) as backup", LogHelper.LogType.Info);
                }
                else
                {
                    _pollingInterval = 500; // 500 毫秒快速轮询（事件失败时）
                    LogHelper.WriteLogToFile("StartPolling: Event registration failed, using fast polling (500ms)", LogHelper.LogType.Info);
                }

                // 创建并启动轮询定时器
                _pollingTimer = new System.Threading.Timer(
                    PollingCallback,
                    null,
                    _pollingInterval,
                    _pollingInterval);

                _isPollingActive = true;
                LogHelper.WriteLogToFile($"StartPolling: Polling started with interval {_pollingInterval}ms", LogHelper.LogType.Info);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"StartPolling: Failed to start polling: {ex.Message}", LogHelper.LogType.Error);
                LogHelper.NewLog(ex);
            }
        }

        /// <summary>
        /// 停止轮询
        /// </summary>
        private void StopPolling()
        {
            try
            {
                if (_pollingTimer != null)
                {
                    _pollingTimer.Dispose();
                    _pollingTimer = null;
                    LogHelper.WriteLogToFile("StopPolling: Polling stopped", LogHelper.LogType.Info);
                }

                _isPollingActive = false;
                _lastPolledSlideIndex = -1;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"StopPolling: Error stopping polling: {ex.Message}", LogHelper.LogType.Warning);
            }
        }

        /// <summary>
        /// 启动轮询或事件监听
        /// 根据事件注册是否成功选择合适的模式
        /// </summary>
        private void StartPollingOrEventMonitoring()
        {
            try
            {
                if (_eventRegistrationSucceeded)
                {
                    // 事件注册成功，使用慢速轮询作为备份
                    _pollingInterval = 3000; // 3 秒
                    LogHelper.WriteLogToFile("StartPollingOrEventMonitoring: Event registration succeeded, using slow polling (3s) as backup", LogHelper.LogType.Info);
                }
                else
                {
                    // 事件注册失败，使用快速轮询
                    _pollingInterval = 500; // 500 毫秒
                    LogHelper.WriteLogToFile("StartPollingOrEventMonitoring: Event registration failed, using fast polling (500ms)", LogHelper.LogType.Info);
                }
                
                // 启动轮询定时器
                StartPolling();
                
                // 启动 ROT 扫描定时器（用于检测新实例和窗口切换）
                StartRotScanning();
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"StartPollingOrEventMonitoring: Error starting polling/event monitoring: {ex.Message}", LogHelper.LogType.Error);
                LogHelper.NewLog(ex);
            }
        }

        /// <summary>
        /// 停止轮询或事件监听
        /// </summary>
        private void StopPollingOrEventMonitoring()
        {
            try
            {
                LogHelper.WriteLogToFile("StopPollingOrEventMonitoring: Stopping polling and event monitoring", LogHelper.LogType.Info);
                
                // 停止轮询定时器
                StopPolling();
                
                // 停止 ROT 扫描定时器
                StopRotScanning();
                
                LogHelper.WriteLogToFile("StopPollingOrEventMonitoring: Stopped successfully", LogHelper.LogType.Info);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"StopPollingOrEventMonitoring: Error stopping polling/event monitoring: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 轮询回调方法
        /// 定期检查当前幻灯片页数
        /// </summary>
        /// <param name="state">状态对象（未使用）</param>
        private void PollingCallback(object state)
        {
            try
            {
                // 首先进行定期检查和重新绑定
                PeriodicCheckAndRebind();

                // 检查当前绑定是否有效
                if (_currentTarget == null || _currentTarget.SlideShowWindow == null)
                {
                    // 没有有效的绑定，尝试重新绑定
                    LogHelper.WriteLogToFile("PollingCallback: No valid binding, attempting to rebind", LogHelper.LogType.Trace);
                    return;
                }

                // 轮询当前幻灯片
                int currentSlideIndex = PollCurrentSlide();

                // 检查幻灯片是否切换
                if (currentSlideIndex != -1 && currentSlideIndex != _lastPolledSlideIndex)
                {
                    LogHelper.WriteLogToFile($"PollingCallback: Slide changed from {_lastPolledSlideIndex} to {currentSlideIndex}", LogHelper.LogType.Info);
                    
                    // 更新上一次轮询的索引
                    _lastPolledSlideIndex = currentSlideIndex;

                    // 触发幻灯片切换事件（如果需要）
                    // 注意：这里可以调用相应的事件处理逻辑
                    OnSlideChangedByPolling(currentSlideIndex);
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"PollingCallback: Error during polling: {ex.Message}", LogHelper.LogType.Warning);
            }
        }

        /// <summary>
        /// 轮询当前幻灯片页数
        /// 从 SlideShowWindow.View.Slide.SlideIndex 获取
        /// 支持 WPS 版本降级处理
        /// </summary>
        /// <returns>当前幻灯片索引，如果获取失败则返回 -1</returns>
        private int PollCurrentSlide()
        {
            if (_currentTarget == null || _currentTarget.SlideShowWindow == null)
            {
                return -1;
            }

            try
            {
                // 使用支持 WPS 版本的方法获取当前幻灯片索引
                int slideIndex = GetCurrentSlideIndexWithWPSSupport(_currentTarget.SlideShowWindow, _currentTarget.Application);
                
                if (slideIndex != -1)
                {
                    LogHelper.WriteLogToFile($"PollCurrentSlide: Current slide index = {slideIndex}", LogHelper.LogType.Trace);
                }
                else
                {
                    LogHelper.WriteLogToFile("PollCurrentSlide: Failed to get current slide index", LogHelper.LogType.Trace);
                }

                return slideIndex;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"PollCurrentSlide: Error getting current slide: {ex.Message}", LogHelper.LogType.Warning);
                return -1;
            }
        }

        /// <summary>
        /// 轮询检测到幻灯片切换时的处理方法
        /// </summary>
        /// <param name="slideIndex">新的幻灯片索引</param>
        private void OnSlideChangedByPolling(int slideIndex)
        {
            try
            {
                LogHelper.WriteLogToFile($"OnSlideChangedByPolling: Slide changed to {slideIndex}", LogHelper.LogType.Info);

                // 更新当前位置
                _currentShowPosition = slideIndex;

                // 这里可以触发相应的事件或回调
                // 例如：通知 UI 更新、保存墨迹等
                // 注意：由于这是在定时器线程中调用，可能需要使用 Dispatcher 切换到 UI 线程
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"OnSlideChangedByPolling: Error handling slide change: {ex.Message}", LogHelper.LogType.Warning);
            }
        }

        /// <summary>
        /// 切换轮询间隔
        /// 根据事件注册状态动态调整轮询频率
        /// </summary>
        /// <param name="eventSucceeded">事件注册是否成功</param>
        private void SwitchPollingInterval(bool eventSucceeded)
        {
            try
            {
                int newInterval = eventSucceeded ? 3000 : 500;

                if (newInterval != _pollingInterval)
                {
                    _pollingInterval = newInterval;
                    LogHelper.WriteLogToFile($"SwitchPollingInterval: Switching polling interval to {_pollingInterval}ms", LogHelper.LogType.Info);

                    // 如果轮询正在运行，重启以应用新的间隔
                    if (_isPollingActive)
                    {
                        StopPolling();
                        StartPolling();
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"SwitchPollingInterval: Error switching polling interval: {ex.Message}", LogHelper.LogType.Warning);
            }
        }

        /// <summary>
        /// 检查 SlideShowWindow 是否有效
        /// 用于检测放映是否已结束或窗口是否已关闭
        /// </summary>
        /// <param name="slideShowWindow">SlideShowWindow 对象（dynamic 类型）</param>
        /// <returns>如果窗口有效则返回 true</returns>
        private bool IsSlideShowWindowValid(dynamic slideShowWindow)
        {
            if (slideShowWindow == null)
            {
                return false;
            }

            try
            {
                // 方法 1: 尝试访问 View 属性
                // 如果放映已结束，访问 View 会抛出异常
                dynamic view = null;
                try
                {
                    view = slideShowWindow.View;
                    if (view != null)
                    {
                        // View 可访问，窗口有效
                        SafeReleaseComObject(view);
                        return true;
                    }
                    else
                    {
                        // View 为 null，窗口可能无效
                        return false;
                    }
                }
                catch (System.Runtime.InteropServices.COMException ex)
                {
                    // COM 异常通常表示窗口已关闭或放映已结束
                    LogHelper.WriteLogToFile($"IsSlideShowWindowValid: View access failed (COM exception): {ex.Message}", LogHelper.LogType.Trace);
                    
                    // 确保释放 view
                    if (view != null)
                    {
                        SafeReleaseComObject(view);
                    }
                    
                    return false;
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"IsSlideShowWindowValid: View access failed: {ex.Message}", LogHelper.LogType.Trace);
                    
                    // 确保释放 view
                    if (view != null)
                    {
                        SafeReleaseComObject(view);
                    }
                    
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"IsSlideShowWindowValid: Error checking window validity: {ex.Message}", LogHelper.LogType.Warning);
                return false;
            }
        }

        /// <summary>
        /// 检测放映是否结束后重新开始
        /// 通过比较当前 SlideShowWindow 和之前的 SlideShowWindow 来判断
        /// </summary>
        /// <returns>如果检测到放映重新开始则返回 true</returns>
        private bool DetectSlideShowRestart()
        {
            if (_currentTarget == null || _currentTarget.Application == null)
            {
                return false;
            }

            dynamic slideShowWindows = null;
            dynamic slideShowWindow = null;

            try
            {
                // 获取当前的 SlideShowWindows 集合
                slideShowWindows = _currentTarget.Application.SlideShowWindows;
                if (slideShowWindows == null || slideShowWindows.Count == 0)
                {
                    // 没有放映窗口，说明放映已结束
                    LogHelper.WriteLogToFile("DetectSlideShowRestart: No slide show windows found", LogHelper.LogType.Trace);
                    
                    if (slideShowWindows != null)
                    {
                        SafeReleaseComObject(slideShowWindows);
                    }
                    
                    return false;
                }

                // 获取第一个放映窗口
                slideShowWindow = slideShowWindows[1]; // COM 集合从 1 开始
                if (slideShowWindow == null)
                {
                    SafeReleaseComObject(slideShowWindows);
                    return false;
                }

                // 检查是否与当前绑定的窗口不同
                bool isDifferent = !ReferenceEquals(slideShowWindow, _currentTarget.SlideShowWindow);
                
                if (isDifferent)
                {
                    LogHelper.WriteLogToFile("DetectSlideShowRestart: Detected new slide show window (restart)", LogHelper.LogType.Info);
                    
                    // 释放 COM 对象
                    SafeReleaseComObject(slideShowWindow);
                    SafeReleaseComObject(slideShowWindows);
                    
                    return true;
                }

                // 释放 COM 对象
                SafeReleaseComObject(slideShowWindow);
                SafeReleaseComObject(slideShowWindows);
                
                return false;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"DetectSlideShowRestart: Error detecting restart: {ex.Message}", LogHelper.LogType.Warning);
                
                // 确保释放 COM 对象
                if (slideShowWindow != null)
                {
                    SafeReleaseComObject(slideShowWindow);
                }
                if (slideShowWindows != null)
                {
                    SafeReleaseComObject(slideShowWindows);
                }
                
                return false;
            }
        }

        /// <summary>
        /// 自动重新绑定
        /// 当检测到放映结束或窗口无效时，尝试重新绑定到新的放映窗口
        /// </summary>
        private void AutoRebind()
        {
            try
            {
                LogHelper.WriteLogToFile("AutoRebind: Attempting to rebind to PowerPoint", LogHelper.LogType.Info);

                // 检查当前绑定是否有效
                if (_currentTarget != null && _currentTarget.SlideShowWindow != null)
                {
                    bool isValid = IsSlideShowWindowValid(_currentTarget.SlideShowWindow);
                    if (isValid)
                    {
                        LogHelper.WriteLogToFile("AutoRebind: Current binding is still valid, no rebind needed", LogHelper.LogType.Trace);
                        return;
                    }
                    else
                    {
                        LogHelper.WriteLogToFile("AutoRebind: Current binding is invalid, rebinding", LogHelper.LogType.Info);
                    }
                }

                // 检测是否有新的放映窗口（放映重新开始）
                bool hasRestarted = DetectSlideShowRestart();
                if (hasRestarted)
                {
                    LogHelper.WriteLogToFile("AutoRebind: Slide show restarted, rebinding to new window", LogHelper.LogType.Info);
                }

                // 重新计算优先级并绑定
                if (_currentTarget != null && _currentTarget.Application != null)
                {
                    PPTTarget newTarget = CalculatePriority(_currentTarget.Application);
                    
                    if (newTarget != null && newTarget.Priority != PPTBindingPriority.None)
                    {
                        // 切换到新的绑定
                        SwitchBinding(newTarget);
                        
                        // 重置轮询状态
                        _lastPolledSlideIndex = -1;
                        
                        LogHelper.WriteLogToFile("AutoRebind: Successfully rebound to PowerPoint", LogHelper.LogType.Info);
                    }
                    else
                    {
                        LogHelper.WriteLogToFile("AutoRebind: No valid target found for rebinding", LogHelper.LogType.Warning);
                    }
                }
                else
                {
                    LogHelper.WriteLogToFile("AutoRebind: No current application to rebind to", LogHelper.LogType.Warning);
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"AutoRebind: Error during rebinding: {ex.Message}", LogHelper.LogType.Error);
                LogHelper.NewLog(ex);
            }
        }

        /// <summary>
        /// 定期检查并重新绑定（在轮询回调中调用）
        /// 检测放映结束后重新开始的情况
        /// </summary>
        private void PeriodicCheckAndRebind()
        {
            try
            {
                // 检查当前绑定是否有效
                if (_currentTarget == null || _currentTarget.SlideShowWindow == null)
                {
                    LogHelper.WriteLogToFile("PeriodicCheckAndRebind: No current binding, attempting auto rebind", LogHelper.LogType.Trace);
                    AutoRebind();
                    return;
                }

                // 检查 SlideShowWindow 是否有效
                bool isValid = IsSlideShowWindowValid(_currentTarget.SlideShowWindow);
                if (!isValid)
                {
                    LogHelper.WriteLogToFile("PeriodicCheckAndRebind: SlideShowWindow is invalid, attempting auto rebind", LogHelper.LogType.Info);
                    AutoRebind();
                    return;
                }

                // 检测放映是否重新开始
                bool hasRestarted = DetectSlideShowRestart();
                if (hasRestarted)
                {
                    LogHelper.WriteLogToFile("PeriodicCheckAndRebind: Slide show restarted, attempting auto rebind", LogHelper.LogType.Info);
                    AutoRebind();
                    return;
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"PeriodicCheckAndRebind: Error during periodic check: {ex.Message}", LogHelper.LogType.Warning);
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 检查并连接到 PowerPoint 应用程序
        /// 根据设置选择使用传统模式或增强模式
        /// </summary>
        public bool CheckAndConnectPPT(string progId = "PowerPoint.Application")
        {
            try
            {
                // 检查是否启用 PPT 联动增强功能
                bool isEnhancedModeEnabled = false;
                
                if (_settingsService != null && _settingsService.IsLoaded)
                {
                    isEnhancedModeEnabled = _settingsService.Settings.PowerPointSettings.IsEnablePPTEnhancedSupport;
                    LogHelper.WriteLogToFile($"CheckAndConnectPPT: Enhanced mode setting = {isEnhancedModeEnabled}", LogHelper.LogType.Info);
                }
                else
                {
                    LogHelper.WriteLogToFile("CheckAndConnectPPT: SettingsService not available, using traditional mode", LogHelper.LogType.Warning);
                }

                // 根据设置选择模式
                if (isEnhancedModeEnabled)
                {
                    LogHelper.WriteLogToFile("CheckAndConnectPPT: Using Enhanced Mode (ROT + dynamic COM)", LogHelper.LogType.Info);
                    return CheckAndConnectPPT_EnhancedMode(progId);
                }
                else
                {
                    LogHelper.WriteLogToFile("CheckAndConnectPPT: Using Traditional Mode (GetActiveObject)", LogHelper.LogType.Info);
                    return CheckAndConnectPPT_TraditionalMode(progId);
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"CheckAndConnectPPT: Unexpected error in mode selection: {ex.Message}", LogHelper.LogType.Error);
                LogHelper.NewLog(ex);
                
                // 如果模式选择失败，回退到传统模式
                LogHelper.WriteLogToFile("CheckAndConnectPPT: Falling back to traditional mode", LogHelper.LogType.Warning);
                return CheckAndConnectPPT_TraditionalMode(progId);
            }
        }

        /// <summary>
        /// 传统模式：使用 GetActiveObject 连接到 PowerPoint
        /// 这是原有的实现方式，依赖 COM 注册正常
        /// </summary>
        /// <param name="progId">程序 ID</param>
        /// <returns>是否成功连接</returns>
        private bool CheckAndConnectPPT_TraditionalMode(string progId)
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
                    dynamic activeWindow = null;
                    dynamic selection = null;
                    dynamic slideRange = null;
                    dynamic slideShowWindows = null;
                    dynamic slideShowWindow = null;
                    dynamic view = null;
                    
                    try
                    {
                        // 在普通视图下这种方式可以获得当前选中的幻灯片对象
                        activeWindow = _pptApplication.ActiveWindow;
                        if (activeWindow != null)
                        {
                            selection = activeWindow.Selection;
                            if (selection != null)
                            {
                                slideRange = selection.SlideRange;
                                if (slideRange != null)
                                {
                                    int slideNumber = slideRange.SlideNumber;
                                    _slide = _slides[slideNumber];
                                    
                                    // 释放中间对象
                                    SafeReleaseComObject(slideRange);
                                }
                                SafeReleaseComObject(selection);
                            }
                            SafeReleaseComObject(activeWindow);
                        }
                    }
                    catch (Exception ex)
                    {
                        // 在阅读模式下出现异常时，通过下面的方式来获得当前选中的幻灯片对象
                        LogHelper.WriteLogToFile("普通视图获取幻灯片失败：" + ex.Message, LogHelper.LogType.Trace);
                        
                        // 释放可能已创建的对象
                        SafeReleaseComObject(slideRange);
                        SafeReleaseComObject(selection);
                        SafeReleaseComObject(activeWindow);
                        
                        try
                        {
                            slideShowWindows = _pptApplication.SlideShowWindows;
                            if (slideShowWindows != null && slideShowWindows.Count > 0)
                            {
                                slideShowWindow = slideShowWindows[1];
                                if (slideShowWindow != null)
                                {
                                    view = slideShowWindow.View;
                                    if (view != null)
                                    {
                                        _slide = view.Slide;
                                        SafeReleaseComObject(view);
                                    }
                                    SafeReleaseComObject(slideShowWindow);
                                }
                                SafeReleaseComObject(slideShowWindows);
                            }
                        }
                        catch (Exception innerEx)
                        {
                            LogHelper.WriteLogToFile("Error getting slide in slide show view: " + innerEx.Message, LogHelper.LogType.Trace);
                            // 确保释放所有对象
                            SafeReleaseComObject(view);
                            SafeReleaseComObject(slideShowWindow);
                            SafeReleaseComObject(slideShowWindows);
                        }
                    }

                    LogHelper.WriteLogToFile("Successfully connected to PowerPoint (Traditional Mode)", LogHelper.LogType.Info);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile("Failed to connect to PowerPoint (Traditional Mode): " + ex.Message, LogHelper.LogType.Error);
                return false;
            }
        }

        /// <summary>
        /// 增强模式：使用 ROT 查找和 dynamic COM 连接到 PowerPoint
        /// 这是新的实现方式，可以处理 COM 注册损坏的情况
        /// </summary>
        /// <param name="progId">程序 ID（在增强模式下主要用于日志）</param>
        /// <returns>是否成功连接</returns>
        private bool CheckAndConnectPPT_EnhancedMode(string progId)
        {
            try
            {
                LogHelper.WriteLogToFile("CheckAndConnectPPT_EnhancedMode: Starting enhanced mode connection", LogHelper.LogType.Info);
                
                // 启动增强模式主循环
                // 这将扫描 ROT、计算优先级、动态绑定，并启动轮询/事件监听
                bool success = EnhancedModeMainLoop();
                
                if (success)
                {
                    LogHelper.WriteLogToFile("CheckAndConnectPPT_EnhancedMode: Successfully connected in enhanced mode", LogHelper.LogType.Info);
                    return true;
                }
                else
                {
                    LogHelper.WriteLogToFile("CheckAndConnectPPT_EnhancedMode: Failed to connect in enhanced mode", LogHelper.LogType.Warning);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"CheckAndConnectPPT_EnhancedMode: Error in enhanced mode: {ex.Message}", LogHelper.LogType.Error);
                LogHelper.NewLog(ex);
                return false;
            }
        }

        #region 增强模式主循环和绑定管理

        /// <summary>
        /// 增强模式主循环
        /// 扫描 ROT 查找 PowerPoint 实例，计算优先级，动态绑定，并启动轮询/事件监听
        /// </summary>
        /// <returns>是否成功建立连接</returns>
        private bool EnhancedModeMainLoop()
        {
            try
            {
                LogHelper.WriteLogToFile("EnhancedModeMainLoop: Starting main loop", LogHelper.LogType.Info);
                
                // 步骤 1: 扫描 ROT 查找所有 PowerPoint 实例
                LogHelper.WriteLogToFile("EnhancedModeMainLoop: Step 1 - Scanning ROT for PowerPoint instances", LogHelper.LogType.Info);
                List<object> pptInstances = EnumerateRunningObjectTable();
                
                if (pptInstances == null || pptInstances.Count == 0)
                {
                    LogHelper.WriteLogToFile("EnhancedModeMainLoop: No PowerPoint instances found in ROT", LogHelper.LogType.Warning);
                    return false;
                }
                
                LogHelper.WriteLogToFile($"EnhancedModeMainLoop: Found {pptInstances.Count} PowerPoint instance(s)", LogHelper.LogType.Info);
                
                // 步骤 2: 为每个实例计算优先级
                LogHelper.WriteLogToFile("EnhancedModeMainLoop: Step 2 - Calculating priorities for all instances", LogHelper.LogType.Info);
                List<PPTTarget> targets = new List<PPTTarget>();
                
                foreach (var instance in pptInstances)
                {
                    try
                    {
                        // 将 COM 对象包装为 dynamic
                        dynamic app = GetDynamicComObject(instance);
                        if (app == null)
                        {
                            LogHelper.WriteLogToFile("EnhancedModeMainLoop: Failed to wrap COM object as dynamic, skipping", LogHelper.LogType.Warning);
                            SafeReleaseComObject(instance);
                            continue;
                        }
                        
                        // 计算优先级
                        PPTTarget target = CalculatePriority(app);
                        
                        if (target != null && target.Priority != PPTBindingPriority.None)
                        {
                            targets.Add(target);
                            LogHelper.WriteLogToFile($"EnhancedModeMainLoop: Added target with priority {target.Priority}, Presentation: {target.PresentationName}", LogHelper.LogType.Info);
                        }
                        else
                        {
                            LogHelper.WriteLogToFile("EnhancedModeMainLoop: Target has no valid priority, skipping", LogHelper.LogType.Trace);
                            // 释放无效的 app 对象
                            SafeReleaseComObject(app);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.WriteLogToFile($"EnhancedModeMainLoop: Error processing instance: {ex.Message}", LogHelper.LogType.Warning);
                        SafeReleaseComObject(instance);
                    }
                }
                
                if (targets.Count == 0)
                {
                    LogHelper.WriteLogToFile("EnhancedModeMainLoop: No valid targets found after priority calculation", LogHelper.LogType.Warning);
                    return false;
                }
                
                // 步骤 3: 选择优先级最高的目标
                LogHelper.WriteLogToFile("EnhancedModeMainLoop: Step 3 - Selecting highest priority target", LogHelper.LogType.Info);
                PPTTarget bestTarget = targets.OrderByDescending(t => t.Priority).First();
                
                LogHelper.WriteLogToFile($"EnhancedModeMainLoop: Selected target with priority {bestTarget.Priority}, Presentation: {bestTarget.PresentationName}", LogHelper.LogType.Info);
                
                // 步骤 4: 绑定到选中的目标
                LogHelper.WriteLogToFile("EnhancedModeMainLoop: Step 4 - Binding to selected target", LogHelper.LogType.Info);
                bool bindingSuccess = SwitchBinding(bestTarget);
                
                if (!bindingSuccess)
                {
                    LogHelper.WriteLogToFile("EnhancedModeMainLoop: Failed to bind to target", LogHelper.LogType.Error);
                    
                    // 释放所有目标的 COM 对象
                    foreach (var target in targets)
                    {
                        ReleaseTargetComObjects(target);
                    }
                    
                    return false;
                }
                
                // 步骤 5: 启动轮询或事件监听
                LogHelper.WriteLogToFile("EnhancedModeMainLoop: Step 5 - Starting polling/event monitoring", LogHelper.LogType.Info);
                StartPollingOrEventMonitoring();
                
                // 释放未选中的目标的 COM 对象
                foreach (var target in targets)
                {
                    if (target != bestTarget)
                    {
                        ReleaseTargetComObjects(target);
                    }
                }
                
                LogHelper.WriteLogToFile("EnhancedModeMainLoop: Main loop completed successfully", LogHelper.LogType.Info);
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"EnhancedModeMainLoop: Unexpected error in main loop: {ex.Message}", LogHelper.LogType.Error);
                LogHelper.NewLog(ex);
                return false;
            }
        }


        /// 轮询当前幻灯片
        /// 定期检查当前幻灯片索引，检测幻灯片切换
        /// </summary>
        /// <param name="state">定时器状态（未使用）</param>
        private void PollCurrentSlide(object state)
        {
            try
            {
                // 检查当前目标是否有效
                if (_currentTarget == null || _currentTarget.SlideShowWindow == null)
                {
                    LogHelper.WriteLogToFile("PollCurrentSlide: No valid target or SlideShowWindow", LogHelper.LogType.Trace);
                    return;
                }
                
                // 获取当前幻灯片索引
                int currentSlideIndex = GetCurrentSlideIndex_Dynamic(_currentTarget.SlideShowWindow);
                
                if (currentSlideIndex <= 0)
                {
                    LogHelper.WriteLogToFile("PollCurrentSlide: Failed to get current slide index", LogHelper.LogType.Trace);
                    return;
                }
                
                // 检查是否发生了幻灯片切换
                if (currentSlideIndex != _lastPolledSlideIndex)
                {
                    LogHelper.WriteLogToFile($"PollCurrentSlide: Slide changed from {_lastPolledSlideIndex} to {currentSlideIndex}", LogHelper.LogType.Info);
                    _lastPolledSlideIndex = currentSlideIndex;
                    _currentShowPosition = currentSlideIndex;
                    
                    // 触发幻灯片切换事件（如果需要）
                    // 这里可以添加自定义的幻灯片切换处理逻辑
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"PollCurrentSlide: Error polling current slide: {ex.Message}", LogHelper.LogType.Warning);
            }
        }

        /// <summary>
        /// 使用 dynamic 对象获取当前幻灯片索引
        /// </summary>
        /// <param name="slideShowWindow">SlideShowWindow 对象（dynamic 类型）</param>
        /// <returns>当前幻灯片索引，如果失败则返回 -1</returns>
        private int GetCurrentSlideIndex_Dynamic(dynamic slideShowWindow)
        {
            if (slideShowWindow == null)
            {
                return -1;
            }
            
            dynamic view = null;
            dynamic slide = null;
            
            try
            {
                view = slideShowWindow.View;
                if (view == null)
                {
                    return -1;
                }
                
                slide = view.Slide;
                if (slide == null)
                {
                    return -1;
                }
                
                int slideIndex = TryGetDynamicProperty(() => (int)slide.SlideIndex, -1, "Slide.SlideIndex");
                
                // 释放中间对象
                SafeReleaseComObject(slide);
                SafeReleaseComObject(view);
                
                return slideIndex;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"GetCurrentSlideIndex_Dynamic: Error getting slide index: {ex.Message}", LogHelper.LogType.Warning);
                
                // 确保释放对象
                if (slide != null)
                {
                    SafeReleaseComObject(slide);
                }
                if (view != null)
                {
                    SafeReleaseComObject(view);
                }
                
                return -1;
            }
        }

        /// <summary>
        /// 获取 Application 对象
        /// 根据当前模式返回传统模式的 _pptApplication 或增强模式的 _currentTarget.Application
        /// </summary>
        /// <returns>Application 对象（dynamic 类型），如果不可用则返回 null</returns>
        private dynamic GetApplicationObject()
        {
            // 优先使用传统模式
            if (_pptApplication != null)
            {
                return _pptApplication;
            }
            
            // 如果传统模式不可用，使用增强模式
            if (_currentTarget != null && _currentTarget.Application != null)
            {
                return _currentTarget.Application;
            }
            
            LogHelper.WriteLogToFile("GetApplicationObject: No valid Application object available in either mode", LogHelper.LogType.Warning);
            return null;
        }

        #endregion

        /// <summary>
        /// 启动幻灯片放映
        /// </summary>
        public void StartSlideShow()
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    if (_presentation != null)
                    {
                        // 使用 ComObjectWrapper 自动管理 SlideShowSettings 的生命周期
                        using (var settingsWrapper = WrapComObject(_presentation.SlideShowSettings, "SlideShowSettings"))
                        {
                            var settings = settingsWrapper.DynamicObject;
                            if (settings != null)
                            {
                                settings.Run();
                            }
                        }
                    }
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
                dynamic slideShowWindows = null;
                dynamic slideShowWindow = null;
                dynamic view = null;
                
                try
                {
                    if (IsSlideShowRunning())
                    {
                        slideShowWindows = _pptApplication.SlideShowWindows;
                        if (slideShowWindows != null && slideShowWindows.Count > 0)
                        {
                            slideShowWindow = slideShowWindows[1];
                            if (slideShowWindow != null)
                            {
                                view = slideShowWindow.View;
                                if (view != null)
                                {
                                    view.Exit();
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile("Failed to end slide show: " + ex.Message, LogHelper.LogType.Error);
                }
                finally
                {
                    // 释放所有 COM 对象
                    SafeReleaseComObject(view);
                    SafeReleaseComObject(slideShowWindow);
                    SafeReleaseComObject(slideShowWindows);
                }
            });
        }

        /// <summary>
        /// 切换到上一张幻灯片
        /// 支持传统模式和增强模式
        /// </summary>
        public void PreviousSlide()
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                lock (_pptOperationLock)
                {
                    dynamic slideShowWindows = null;
                    dynamic slideShowWindow = null;
                    dynamic view = null;
                    try
                    {
                        if (IsSlideShowRunning())
                        {
                            // 获取 Application 对象（支持两种模式）
                            dynamic app = GetApplicationObject();
                            if (app == null)
                            {
                                LogHelper.WriteLogToFile("PreviousSlide: No valid Application object", LogHelper.LogType.Warning);
                                return;
                            }
                            
                            slideShowWindows = SafeAccessDynamicProperty(
                                () => app.SlideShowWindows,
                                null,
                                "Application.SlideShowWindows");
                            
                            if (slideShowWindows != null)
                            {
                                int count = TryGetDynamicProperty(() => (int)slideShowWindows.Count, 0, "SlideShowWindows.Count");
                                if (count > 0)
                                {
                                    slideShowWindow = slideShowWindows[1];
                                    if (slideShowWindow != null)
                                    {
                                        SafeInvokeDynamicMethod(() => slideShowWindow.Activate(), "SlideShowWindow.Activate");
                                        
                                        view = SafeAccessDynamicProperty(
                                            () => slideShowWindow.View,
                                            null,
                                            "SlideShowWindow.View");
                                        
                                        if (view != null)
                                        {
                                            SafeInvokeDynamicMethod(() => view.Previous(), "View.Previous");
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.WriteLogToFile("Failed to go to previous slide: " + ex.Message, LogHelper.LogType.Error);
                    }
                    finally
                    {
                        // 释放所有 COM 对象
                        SafeReleaseComObject(view);
                        SafeReleaseComObject(slideShowWindow);
                        SafeReleaseComObject(slideShowWindows);
                    }
                }
            });
        }

        /// <summary>
        /// 切换到下一张幻灯片
        /// 支持传统模式和增强模式
        /// </summary>
        public void NextSlide()
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                lock (_pptOperationLock)
                {
                    dynamic slideShowWindows = null;
                    dynamic slideShowWindow = null;
                    dynamic view = null;
                    try
                    {
                        if (IsSlideShowRunning())
                        {
                            // 获取 Application 对象（支持两种模式）
                            dynamic app = GetApplicationObject();
                            if (app == null)
                            {
                                LogHelper.WriteLogToFile("NextSlide: No valid Application object", LogHelper.LogType.Warning);
                                return;
                            }
                            
                            slideShowWindows = SafeAccessDynamicProperty(
                                () => app.SlideShowWindows,
                                null,
                                "Application.SlideShowWindows");
                            
                            if (slideShowWindows != null)
                            {
                                int count = TryGetDynamicProperty(() => (int)slideShowWindows.Count, 0, "SlideShowWindows.Count");
                                if (count > 0)
                                {
                                    slideShowWindow = slideShowWindows[1];
                                    if (slideShowWindow != null)
                                    {
                                        SafeInvokeDynamicMethod(() => slideShowWindow.Activate(), "SlideShowWindow.Activate");
                                        
                                        view = SafeAccessDynamicProperty(
                                            () => slideShowWindow.View,
                                            null,
                                            "SlideShowWindow.View");
                                        
                                        if (view != null)
                                        {
                                            SafeInvokeDynamicMethod(() => view.Next(), "View.Next");
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.WriteLogToFile("Failed to go to next slide: " + ex.Message, LogHelper.LogType.Error);
                    }
                    finally
                    {
                        // 释放所有 COM 对象
                        SafeReleaseComObject(view);
                        SafeReleaseComObject(slideShowWindow);
                        SafeReleaseComObject(slideShowWindows);
                    }
                }
            });
        }

        /// <summary>
        /// 跳转到指定幻灯片
        /// </summary>
        public void GoToSlide(int slideIndex)
        {
            dynamic slideShowWindows = null;
            dynamic slideShowWindow = null;
            dynamic view = null;
            dynamic windows = null;
            dynamic window = null;
            dynamic windowView = null;
            
            try
            {
                if (IsSlideShowRunning())
                {
                    slideShowWindows = _pptApplication.SlideShowWindows;
                    if (slideShowWindows != null && slideShowWindows.Count > 0)
                    {
                        slideShowWindow = slideShowWindows[1];
                        if (slideShowWindow != null)
                        {
                            view = slideShowWindow.View;
                            if (view != null)
                            {
                                view.GotoSlide(slideIndex);
                            }
                        }
                    }
                }
                else if (_presentation != null)
                {
                    windows = _presentation.Windows;
                    if (windows != null && windows.Count > 0)
                    {
                        window = windows[1];
                        if (window != null)
                        {
                            windowView = window.View;
                            if (windowView != null)
                            {
                                windowView.GotoSlide(slideIndex);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"Failed to go to slide {slideIndex}: " + ex.Message, LogHelper.LogType.Error);
            }
            finally
            {
                // 释放所有 COM 对象
                SafeReleaseComObject(windowView);
                SafeReleaseComObject(window);
                SafeReleaseComObject(windows);
                SafeReleaseComObject(view);
                SafeReleaseComObject(slideShowWindow);
                SafeReleaseComObject(slideShowWindows);
            }
        }

        /// <summary>
        /// 显示幻灯片导航
        /// </summary>
        public void ShowSlideNavigation()
        {
            dynamic slideShowWindows = null;
            dynamic slideShowWindow = null;
            dynamic slideNavigation = null;
            
            try
            {
                if (IsSlideShowRunning())
                {
                    slideShowWindows = _pptApplication.SlideShowWindows;
                    if (slideShowWindows != null && slideShowWindows.Count > 0)
                    {
                        slideShowWindow = slideShowWindows[1];
                        if (slideShowWindow != null)
                        {
                            slideNavigation = slideShowWindow.SlideNavigation;
                            if (slideNavigation != null)
                            {
                                slideNavigation.Visible = true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile("Failed to show slide navigation: " + ex.Message, LogHelper.LogType.Error);
            }
            finally
            {
                // 释放所有 COM 对象
                SafeReleaseComObject(slideNavigation);
                SafeReleaseComObject(slideShowWindow);
                SafeReleaseComObject(slideShowWindows);
            }
        }

        /// <summary>
        /// 判断幻灯片放映是否正在运行
        /// 支持传统模式和增强模式
        /// </summary>
        public bool IsSlideShowRunning()
        {
            dynamic slideShowWindows = null;
            try
            {
                // 优先使用传统模式的 _pptApplication
                if (_pptApplication != null)
                {
                    slideShowWindows = _pptApplication.SlideShowWindows;
                    if (slideShowWindows != null)
                    {
                        int count = slideShowWindows.Count;
                        SafeReleaseComObject(slideShowWindows);
                        return count > 0;
                    }
                    
                    return false;
                }
                
                // 如果传统模式不可用，尝试使用增强模式的 _currentTarget
                if (_currentTarget != null && _currentTarget.Application != null)
                {
                    LogHelper.WriteLogToFile("IsSlideShowRunning: Using enhanced mode (_currentTarget)", LogHelper.LogType.Trace);
                    
                    slideShowWindows = SafeAccessDynamicProperty(
                        () => _currentTarget.Application.SlideShowWindows,
                        null,
                        "Application.SlideShowWindows");
                    
                    if (slideShowWindows != null)
                    {
                        int count = TryGetDynamicProperty(() => (int)slideShowWindows.Count, 0, "SlideShowWindows.Count");
                        SafeReleaseComObject(slideShowWindows);
                        return count > 0;
                    }
                    
                    return false;
                }
                
                // 两种模式都不可用
                LogHelper.WriteLogToFile("IsSlideShowRunning: No valid Application object available", LogHelper.LogType.Warning);
                return false;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"IsSlideShowRunning: Error checking slide show status: {ex.Message}", LogHelper.LogType.Warning);
                
                // 确保释放 slideShowWindows
                if (slideShowWindows != null)
                {
                    SafeReleaseComObject(slideShowWindows);
                }
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
                                _memoryStreams[i]?.Dispose();
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
                    // 使用 ComObjectWrapper 自动管理 SlideShowTransition 的生命周期
                    using (var transitionWrapper = WrapComObject(slide.SlideShowTransition, "SlideShowTransition"))
                    {
                        var transition = transitionWrapper.DynamicObject;
                        if (transition != null && transition.Hidden == MsoTriState.msoTrue)
                        {
                            return true;
                        }
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
                    // 使用 ComObjectWrapper 自动管理 SlideShowTransition 的生命周期
                    using (var transitionWrapper = WrapComObject(slide.SlideShowTransition, "SlideShowTransition"))
                    {
                        var transition = transitionWrapper.DynamicObject;
                        if (transition != null && transition.Hidden == MsoTriState.msoTrue)
                        {
                            transition.Hidden = MsoTriState.msoFalse;
                        }
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
                    // 使用 ComObjectWrapper 自动管理 SlideShowTransition 的生命周期
                    using (var transitionWrapper = WrapComObject(slide.SlideShowTransition, "SlideShowTransition"))
                    {
                        var transition = transitionWrapper.DynamicObject;
                        if (transition != null && 
                            transition.AdvanceOnTime == MsoTriState.msoTrue &&
                            transition.AdvanceTime > 0)
                        {
                            return true;
                        }
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
                    // 使用 ComObjectWrapper 自动管理 SlideShowSettings 的生命周期
                    using (var settingsWrapper = WrapComObject(_presentation.SlideShowSettings, "SlideShowSettings"))
                    {
                        var settings = settingsWrapper.DynamicObject;
                        if (settings != null)
                        {
                            settings.AdvanceMode = PpSlideShowAdvanceMode.ppSlideShowManualAdvance;
                            LogHelper.WriteLogToFile("Disabled auto play", LogHelper.LogType.Info);
                        }
                    }
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
                // 停止轮询
                if (_isPollingActive)
                {
                    LogHelper.WriteLogToFile("ReleaseComObjects: Stopping polling", LogHelper.LogType.Info);
                    StopPolling();
                }

                // 首先释放所有跟踪的 COM 对象
                LogHelper.WriteLogToFile("ReleaseComObjects: Releasing all tracked COM objects", LogHelper.LogType.Info);
                ReleaseAllTrackedComObjects();

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

                // 释放当前目标的 COM 对象
                if (_currentTarget != null)
                {
                    ReleaseTargetComObjects(_currentTarget);
                    _currentTarget = null;
                }

                // 最后验证是否还有未释放的对象
                ValidateComObjectReferenceCounts();
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
