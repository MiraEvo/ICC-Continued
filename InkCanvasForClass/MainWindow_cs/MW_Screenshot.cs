// ============================================================================
// MW_Screenshot.cs - 截图功能
// ============================================================================
// 
// 功能说明:
//   - 屏幕截图捕获
//   - 截图保存（自动保存、手动保存）
//   - 截图格式和质量设置
//   - PPT 截图保存
//
// 迁移状态 (渐进式迁移):
//   - ScreenshotService 已创建，提供截图服务接口
//   - 支持多种截图模式（全屏、选区、窗口）
//   - 此文件中的核心截图逻辑仍在使用
//
// 相关文件:
//   - Services/ScreenshotService.cs
//   - Services/IScreenshotService.cs
//
// ============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using Ink_Canvas.Helpers;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using OSVersionExtension;
using Vanara.PInvoke;
using Encoder = System.Drawing.Imaging.Encoder;
using OperatingSystem = OSVersionExtension.OperatingSystem;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

using System.Management;
using System.Reflection;
using System.Windows.Shapes;
using Path = System.IO.Path;
using Rectangle = System.Drawing.Rectangle;

namespace Ink_Canvas {
    public partial class MainWindow : Window {
        #region MagnificationAPI 获取屏幕截图并过滤ICC窗口

        #region Dubi906w 的轮子

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        #endregion Dubi906w 的轮子

        #region Win32 窗口环境（由 AlanCRL 测试）

        // 感謝 Alan-CRL 造的輪子
        private const int WS_EX_TOPMOST = 0x00000008;
        private const int WS_EX_LAYERED = 0x00080000;
        private const int WS_SIZEBOX = 0x00040000;
        private const int WS_SYSMENU = 0x00080000;
        private const int WS_CLIPCHILDREN = 0x02000000;
        private const int WS_CAPTION = 0x00C00000;
        private const int WS_MAXIMIZEBOX = 0x00010000;
        private const int GWL_STYLE = -16;
        private const int GWL_EXSTYLE = -20;
        private const int WS_THICKFRAME = 0x00040000;
        private const int SWP_NOSIZE = 0x0001;
        private const int SWP_FRAMECHANGED = 0x0020;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_APPWINDOW = 0x00040000;
        private const int SW_SHOW = 5;
        private const int LWA_ALPHA = 0x00000002;
        private const int PW_RENDERFULLCONTENT = 2;
        private static IntPtr windowHostHandle;

        // PInvoke 輪子
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CreateWindowEx(int dwExStyle, string lpClassName, string lpWindowName, int dwStyle,
            int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

        [DllImport("user32.dll")]
        private static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern ushort RegisterClassEx(ref WNDCLASSEX lpwcx);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern short UnregisterClass(string lpClassName, IntPtr hInstance);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);

        [StructLayout(LayoutKind.Sequential)]
        private struct WNDCLASSEX {
            public uint cbSize;
            public uint style;
            [MarshalAs(UnmanagedType.FunctionPtr)] public WndProc lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            public string lpszMenuName;
            public string lpszClassName;
            public IntPtr hIconSm;
        }

        private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private static readonly WndProc StaticWndProcDelegate = WndHostProc;

        private const uint WM_DESTROY = 0x0002;
        private const uint WM_CLOSE = 0x0010;
        private const int CS_HREDRAW = 0x0002;
        private const int CS_VREDRAW = 0x0001;
        private const int IDC_ARROW = 32512;
        private static int COLOR_BTNFACE = 15;
        private const int WS_CHILD = 0x40000000;
        private const int WS_VISIBLE = 0x10000000;
        private const int MS_CLIPAROUNDCURSOR = 0x0002;

        #endregion Win32 窗口环境（由 AlanCRL 测试）

        public void SaveScreenshotToDesktopByMagnificationAPI(HWND[] hwndsList,
            Action<Bitmap> callbackAction, bool isUsingCallback = false) {
            if (OSVersion.GetOperatingSystem() < OperatingSystem.Windows81) return;
            if (!Magnification.MagInitialize()) return;
            // 註冊宿主窗體類名
            var wndClassEx = new WNDCLASSEX {
                cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(), style = CS_HREDRAW | CS_VREDRAW,
                lpfnWndProc = StaticWndProcDelegate, hInstance = IntPtr.Zero,
                hCursor = LoadCursor(IntPtr.Zero, IDC_ARROW), hbrBackground = (IntPtr)(1 + COLOR_BTNFACE),
                lpszClassName = "ICCMagnifierWindowHost",
                hIcon = IntPtr.Zero, hIconSm = IntPtr.Zero
            };
            RegisterClassEx(ref wndClassEx);
            // 創建宿主窗體
            windowHostHandle = CreateWindowEx(
                WS_EX_TOPMOST | WS_EX_LAYERED, "ICCMagnifierWindowHost", "ICCMagnifierWindowHostWindow",
                WS_SIZEBOX | WS_SYSMENU | WS_CLIPCHILDREN | WS_CAPTION | WS_MAXIMIZEBOX, 0, 0,
                System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width,
                System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero,
                IntPtr.Zero);
            // 設定分層窗體
            SetLayeredWindowAttributes(windowHostHandle, 0, 0, LWA_ALPHA);
            // 創建放大鏡窗體
            var hwndMag = CreateWindowEx(
                0, Magnification.WC_MAGNIFIER, "ICCMagnifierWindow", WS_CHILD | WS_VISIBLE | MS_CLIPAROUNDCURSOR, 0, 0,
                System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width,
                System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height, windowHostHandle,
                IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            // 設定窗體樣式和排布
            int style = GetWindowLong(windowHostHandle, GWL_STYLE);
            style &= ~WS_CAPTION; // 隐藏标题栏
            style &= ~WS_THICKFRAME; // 禁止窗口拉伸
            SetWindowLong(windowHostHandle, GWL_STYLE, style);
            SetWindowPos(windowHostHandle, IntPtr.Zero, 0, 0, 0, 0, SWP_NOSIZE | SWP_FRAMECHANGED);
            // 設定額外樣式
            int exStyle = GetWindowLong(windowHostHandle, GWL_EXSTYLE);
            exStyle |= WS_EX_TOOLWINDOW; /* <- 隐藏任务栏图标 */
            exStyle &= ~WS_EX_APPWINDOW;
            SetWindowLong(windowHostHandle, GWL_EXSTYLE, exStyle);
            // 設定放大鏡工廠
            Magnification.MAGTRANSFORM matrix = new Magnification.MAGTRANSFORM();
            matrix[0, 0] = 1.0f;
            matrix[0, 1] = 0.0f;
            matrix[0, 2] = 0.0f;
            matrix[1, 0] = 0.0f;
            matrix[1, 1] = 1.0f;
            matrix[1, 2] = 0.0f;
            matrix[2, 0] = 1.0f;
            matrix[2, 1] = 0.0f;
            matrix[2, 2] = 0.0f;
            if (!Magnification.MagSetWindowTransform(hwndMag, matrix)) return;
            // 設定放大鏡轉化矩乘陣列
            Magnification.MAGCOLOREFFECT magEffect = new Magnification.MAGCOLOREFFECT();
            magEffect[0, 0] = 1.0f;
            magEffect[0, 1] = 0.0f;
            magEffect[0, 2] = 0.0f;
            magEffect[0, 3] = 0.0f;
            magEffect[0, 4] = 0.0f;
            magEffect[1, 0] = 0.0f;
            magEffect[1, 1] = 1.0f;
            magEffect[1, 2] = 0.0f;
            magEffect[1, 3] = 0.0f;
            magEffect[1, 4] = 0.0f;
            magEffect[2, 0] = 0.0f;
            magEffect[2, 1] = 0.0f;
            magEffect[2, 2] = 1.0f;
            magEffect[2, 3] = 0.0f;
            magEffect[2, 4] = 0.0f;
            magEffect[3, 0] = 0.0f;
            magEffect[3, 1] = 0.0f;
            magEffect[3, 2] = 0.0f;
            magEffect[3, 3] = 1.0f;
            magEffect[3, 4] = 0.0f;
            magEffect[4, 0] = 0.0f;
            magEffect[4, 1] = 0.0f;
            magEffect[4, 2] = 0.0f;
            magEffect[4, 3] = 0.0f;
            magEffect[4, 4] = 1.0f;
            if (!Magnification.MagSetColorEffect(hwndMag, magEffect)) return;
            // 顯示窗體
            ShowWindow(windowHostHandle, SW_SHOW);
            // 过滤窗口
            var hwnds = new List<HWND> { hwndMag };
            hwnds.AddRange(hwndsList);
            if (!Magnification.MagSetWindowFilterList(hwndMag, Magnification.MW_FILTERMODE.MW_FILTERMODE_EXCLUDE,
                    hwnds.Count, hwnds.ToArray())) return;
            // 设置窗口 Source
            if (!Magnification.MagSetWindowSource(hwndMag, new RECT(0, 0,
                    System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width,
                    System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height))) return;
            InvalidateRect(hwndMag, IntPtr.Zero, true);
            // 抓取屏幕圖像
            if (isUsingCallback) {
                if (!Magnification.MagSetImageScalingCallback(hwndMag,
                        (hwnd, srcdata, srcheader, destdata, destheader, unclipped, clipped, dirty) => {
                            Bitmap bm = new Bitmap((int)srcheader.width, (int)srcheader.height,
                                (int)srcheader.width * 4, PixelFormat.Format32bppRgb, srcdata);
                            callbackAction(bm);
                            return true;
                        })) return;
            } else {
                RECT rect;
                GetWindowRect(hwndMag, out rect);
                Bitmap bmp = new Bitmap(rect.Width, rect.Height);
                Graphics memoryGraphics = Graphics.FromImage(bmp);
                PrintWindow(hwndMag, memoryGraphics.GetHdc(), PW_RENDERFULLCONTENT);
                memoryGraphics.ReleaseHdc();
                callbackAction(bmp);
            }

            // 反注册宿主窗口
            UnregisterClass("ICCMagnifierWindowHost", IntPtr.Zero);
            // 销毁宿主窗口
            Magnification.MagUninitialize();
            DestroyWindow(windowHostHandle);
        }

        public Task<Bitmap> SaveScreenshotToDesktopByMagnificationAPIAsync(HWND[] hwndsList,
            bool isUsingCallback = false) {
            var tcs = new TaskCompletionSource<Bitmap>();
            
            // Magnification API 需要在 STA 线程上运行，并且需要消息循环
            // Task.Run 使用的是线程池线程（MTA），会导致崩溃
            // 因此我们需要创建一个专用的 STA 线程
            var staThread = new System.Threading.Thread(() => {
                try {
                    SaveScreenshotToDesktopByMagnificationAPI(hwndsList, bitmap => {
                        tcs.TrySetResult(bitmap);
                    }, isUsingCallback);
                }
                catch (Exception ex) {
                    tcs.TrySetException(ex);
                }
                
                // 如果回调没有被调用，设置一个空结果
                if (!tcs.Task.IsCompleted) {
                    tcs.TrySetResult(null);
                }
            });
            
            staThread.SetApartmentState(System.Threading.ApartmentState.STA);
            staThread.IsBackground = true;
            staThread.Start();
            
            // 添加超时机制，防止无限等待
            var timeoutTask = Task.Delay(3000).ContinueWith(_ => {
                if (!tcs.Task.IsCompleted) {
                    tcs.TrySetResult(null); // 超时返回 null，让调用者回退到其他方法
                }
            });
            
            return tcs.Task;
        }

        private static IntPtr WndHostProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam) {
            return DefWindowProc(hWnd, msg, wParam, lParam);
        }

        #endregion MagnificationAPI 获取屏幕截图并过滤ICC窗口

        #region 窗口截图（復刻Powerpoint）

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        public static IntPtr GetClassLongPtr(IntPtr hWnd, int nIndex) {
            if (IntPtr.Size > 4)
                return GetClassLongPtr64(hWnd, nIndex);
            else
                return new IntPtr(GetClassLongPtr32(hWnd, nIndex));
        }

        [DllImport("user32.dll", EntryPoint = "GetClassLong")]
        public static extern uint GetClassLongPtr32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetClassLongPtr")]
        public static extern IntPtr GetClassLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "EnumDesktopWindows",
            ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool EnumDesktopWindows(IntPtr hDesktop, Delegate lpEnumCallbackFunction, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", EntryPoint = "GetWindowText",
            ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpWindowText, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool PrintWindow(IntPtr hwnd, IntPtr hDC, uint nFlags);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr handle, out RECT rect);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string lclassName,
            string windowTitle);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr GetShellWindow();

        [DllImport("dwmapi.dll")]
        static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out bool pvAttribute, int cbAttribute);
        [DllImport("dwmapi.dll")]
        static extern int DwmGetWindowAttribute(IntPtr hwnd, DwmWindowAttribute dwAttribute, out RECT pvAttribute, int cbAttribute);
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetLayeredWindowAttributes(IntPtr hwnd, out uint crKey, out byte bAlpha, out uint dwFlags);
        public delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);
        [DllImport("user32.dll", SetLastError=true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
        [DllImport("user32.dll")]
        internal static extern int GetDpiForWindow(IntPtr hWnd);

        enum DwmWindowAttribute : uint {
            NCRenderingEnabled = 1,
            NCRenderingPolicy,
            TransitionsForceDisabled,
            AllowNCPaint,
            CaptionButtonBounds,
            NonClientRtlLayout,
            ForceIconicRepresentation,
            Flip3DPolicy,
            ExtendedFrameBounds,
            HasIconicBitmap,
            DisallowPeek,
            ExcludedFromPeek,
            Cloak,
            Cloaked,
            FreezeRepresentation,
            PassiveUpdateMode,
            UseHostBackdropBrush,
            UseImmersiveDarkMode = 20,
            WindowCornerPreference = 33,
            BorderColor,
            CaptionColor,
            TextColor,
            VisibleFrameBorderThickness,
            SystemBackdropType,
            Last
        }

        public Icon GetAppIcon(IntPtr hwnd) {
            IntPtr iconHandle = SendMessage(hwnd, 0x7F, 2, 0);
            if (iconHandle == IntPtr.Zero)
                iconHandle = SendMessage(hwnd, 0x7F, 0, 0);
            if (iconHandle == IntPtr.Zero)
                iconHandle = SendMessage(hwnd, 0x7F, 1, 0);
            if (iconHandle == IntPtr.Zero)
                iconHandle = GetClassLongPtr(hwnd, -14);
            if (iconHandle == IntPtr.Zero)
                iconHandle = GetClassLongPtr(hwnd, -34);
            if (iconHandle == IntPtr.Zero)
                return null;
            Icon icn = System.Drawing.Icon.FromHandle(iconHandle);
            return icn;
        }

        public class WindowInformation {
            public string Title { get; set; }
            public Bitmap WindowBitmap { get; set; }
            public Icon AppIcon { get; set; }
            public bool IsVisible { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public RECT Rect { get; set; }
            public WINDOWPLACEMENT Placement { get; set; }
            public HWND hwnd { get; set; }
            public RECT RealRect { get; set; }
            public Rectangle ContentRect { get; set; }
            public IntPtr Handle { get; set; }
            public int WindowDPI { get; set; }
            public int SystemDPI { get; set; }
            public double DPIScale { get; set; }
        }

        public struct WINDOWPLACEMENT {
            public int length;
            public int flags;
            public int showCmd;
            public System.Drawing.Point ptMinPosition;
            public System.Drawing.Point ptMaxPosition;
            public System.Drawing.Rectangle rcNormalPosition;

            public static WINDOWPLACEMENT Default {
                get {
                    WINDOWPLACEMENT result = new WINDOWPLACEMENT();
                    result.length = Marshal.SizeOf(result);
                    return result;
                }
            }
        }

        public delegate bool EnumDesktopWindowsDelegate(IntPtr hWnd, int lParam);

        public WindowInformation[] GetAllWindows(HWND[] excludedHwnds) {
            var windows = new List<WindowInformation>();
            IntPtr hShellWnd = GetShellWindow();
            IntPtr hDefView = FindWindowEx(hShellWnd, IntPtr.Zero, "SHELLDLL_DefView", null);
            IntPtr folderView = FindWindowEx(hDefView, IntPtr.Zero, "SysListView32", null);
            IntPtr taskBar = FindWindowEx(IntPtr.Zero, IntPtr.Zero, "Shell_TrayWnd", null);
            var excluded = new List<HWND>() {
                new HWND(hShellWnd), new HWND(hDefView), new HWND(folderView), new HWND(taskBar)
            };
            var excludedWindowTitle = new string[] {
                "NVIDIA GeForce Overlay", "Ink Canvas 画板", "Ink Canvas Annotation", "Ink Canvas Artistry", "InkCanvasForClass"
            };
            excluded.AddRange(excludedHwnds);
            if (!EnumDesktopWindows(IntPtr.Zero, new EnumDesktopWindowsDelegate((hwnd, param) => {
                        // 排除被過濾的窗體句柄
                        if (excluded.Contains(new HWND(hwnd))) return true;

                        // 判斷窗體是否可見
                        var isvisible = IsWindowVisible(hwnd);
                        if (!isvisible) return true;

                        // 判斷窗體透明度和額外樣式
                        var windowLong = (int)GetWindowLongPtr(hwnd, -20);
                        GetLayeredWindowAttributes(hwnd, out uint crKey, out byte bAlpha, out uint dwFlags);
                        if ((windowLong & 0x00000080L) != 0) return true;
                        if ((windowLong & 0x00080000) != 0 && (dwFlags & 0x00000002) != 0 && bAlpha == 0) return true; //分层窗口且全透明

                        // Win8+專用，用於檢測UWP應用是否隱藏
                        bool isCloacked = false;
                        if (OSVersion.GetOperatingSystem() >= OperatingSystem.Windows8)
                            DwmGetWindowAttribute(hwnd, (int)DwmWindowAttribute.Cloaked, out isCloacked, Marshal.SizeOf(typeof(bool)));
                        if (isCloacked) return true;

                        // 獲取窗體實際大小
                        DwmGetWindowAttribute(hwnd, DwmWindowAttribute.ExtendedFrameBounds, out RECT realRect, Marshal.SizeOf(typeof(RECT)));

                        // 獲取窗體的進程ID
                        var pidRes = GetWindowThreadProcessId(hwnd, out uint pid);
                        if (pid == 0 || pidRes == 0) return true;

                        // 獲取窗體的DPI差異，scale為1則代表非DWM強制拉伸顯示窗體，實際截圖會根據窗體的DPI Awareness來截取
                        var dpiForHwnd = GetDpiForWindow(hwnd);
                        var dpiXProperty = typeof(SystemParameters).GetProperty("DpiX", BindingFlags.NonPublic | BindingFlags.Static);
                        var dpiYProperty = typeof(SystemParameters).GetProperty("Dpi", BindingFlags.NonPublic | BindingFlags.Static);
                        var dpiX = (int)dpiXProperty.GetValue(null, null);
                        var dpiY = (int)dpiYProperty.GetValue(null, null);
                        var dpi = (dpiX + dpiY) / 2;
                        double scale = 1;
                        if (dpi > dpiForHwnd) { // 说明该应用是win32应用，靠DWM的拉伸放大到高DPI
                            scale = dpi / (double)dpiForHwnd;
                        }

                        // 獲取窗體應用程式圖標
                        var icon = GetAppIcon(hwnd);

                        // 獲取應用程式標題，這裡空標題不略過，用於後續繼續判斷獲取標題
                        var length = GetWindowTextLength(hwnd) + 1;
                        var title = new StringBuilder(length);
                        GetWindowText(hwnd, title, length);
                        // if (title.ToString().Length == 0) return true;


                        // 窗體標題黑名單，在黑名單中的窗體不會顯示
                        if (excludedWindowTitle.Contains(title.ToString())) return true;

                        // 獲取窗體狀態，如果是最小化就跳過
                        WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
                        GetWindowPlacement(hwnd, ref placement);
                        if (placement.showCmd == 2) return true;

                        // 獲取窗口Rect，用於和DwmGetWindowAttribute方法獲取到的窗體大小進行Offset計算
                        RECT rect;
                        GetWindowRect(hwnd, out rect);
                        var w = rect.Width;
                        var h = rect.Height;
                        if (w == 0 || h == 0) return true;

                        // 使用PrintWindow（RENDER_FULL_CONTENT）來實現窗體圖片截取（支持D3D和DX）
                        Bitmap bmp = new Bitmap(rect.Width, rect.Height);
                        Graphics memoryGraphics = Graphics.FromImage(bmp);
                        IntPtr hdc = memoryGraphics.GetHdc();
                        PrintWindow(hwnd, hdc, 2);

                        // 添加窗體信息
                        windows.Add(new WindowInformation() {
                            AppIcon = icon,
                            Title = title.ToString(),
                            IsVisible = isvisible,
                            WindowBitmap = bmp,
                            Width = w,
                            Height = h,
                            Rect = rect,
                            Placement = placement,
                            RealRect = realRect,
                            Handle = hwnd,
                            ContentRect = new Rectangle(realRect.X - rect.X, realRect.Y - rect.Y, (int)Math.Round(
                                realRect.Width / scale ,0), (int)Math.Round(realRect.Height / scale, 0)),
                            WindowDPI = dpiForHwnd,
                            SystemDPI = dpi,
                            DPIScale = scale
                        });

                        // 釋放HDC
                        memoryGraphics.ReleaseHdc(hdc);

                        // 嘗試調用GC回收叻色
                        System.GC.Collect();
                        System.GC.WaitForPendingFinalizers();
                        return true;
                    }),
                    IntPtr.Zero)) return new WindowInformation[] { };
            return windows.ToArray();
        }

        public static string GetProcessPathByPid(int processId) {
            string query = $"SELECT Name, ExecutablePath FROM Win32_Process WHERE ProcessId = {processId}";
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            foreach (ManagementObject obj in searcher.Get()) {
                string executablePath = obj["ExecutablePath"]?.ToString();
                if (!string.IsNullOrEmpty(executablePath)) return executablePath;
            }
            return "";
        }

        public async Task<string> GetProcessPathByPidAsync(int processId) {
            try
            {
                var result = await Task.Run(() => {
                    try
                    {
                        return GetProcessPathByPid(processId);
                    }
                    catch (Exception ex)
                    {
                        LogHelper.WriteLogToFile($"Error getting process path for PID {processId}: {ex.Message}", LogHelper.LogType.Warning);
                        return null;
                    }
                });
                return result;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"Failed to start async get process path: {ex.Message}", LogHelper.LogType.Error);
                LogHelper.NewLog(ex);
                return null;
            }
        }

        private static string GetAppFriendlyName(string filePath)
        {
            var versionInfo = FileVersionInfo.GetVersionInfo(filePath);
            return versionInfo.FileDescription;
        }

        public async Task<WindowInformation[]> GetAllWindowsAsync(HWND[] excludedHwnds) {
            try
            {
                var windows = await Task.Run(() => {
                    try
                    {
                        return GetAllWindows(excludedHwnds);
                    }
                    catch (Exception ex)
                    {
                        LogHelper.WriteLogToFile($"Error getting all windows: {ex.Message}", LogHelper.LogType.Error);
                        LogHelper.NewLog(ex);
                        return new WindowInformation[0];
                    }
                });
                var _wins = new List<WindowInformation>(){};
                foreach (var w in windows) {
                    _wins.Add(w);
                }
                foreach (var windowInformation in windows) {
                    if (windowInformation.Title.Length == 0) {
                    GetWindowThreadProcessId(windowInformation.Handle, out uint Pid);
                    if (Pid != 0) {
                        var _path = Path.GetFullPath(await GetProcessPathByPidAsync((int)Pid));
                        var processPath = Path.GetFullPath(Process.GetCurrentProcess().MainModule.FileName);
                        if (string.Equals(_path, processPath, StringComparison.OrdinalIgnoreCase) || _path == "") {
                            _wins.Remove(windowInformation);
                        } else {
                            var _des = GetAppFriendlyName(_path);
                            Trace.WriteLine(_des);
                            if (_des == null) {
                                _wins.Remove(windowInformation);
                            } else {
                                var index = _wins.IndexOf(windowInformation);
                                _wins[index].Title = _des;
                            }
                        }
                    } else {
                        _wins.Remove(windowInformation);
                    }
                }
            }
            return _wins.ToArray();
        }
        catch (Exception ex)
        {
            LogHelper.WriteLogToFile($"Failed to get all windows async: {ex.Message}", LogHelper.LogType.Error);
            LogHelper.NewLog(ex);
            return new WindowInformation[0];
        }
    }

        #endregion

        #region 舊版全屏截圖

        private Bitmap GetScreenshotBitmap() {
            Rectangle rc = System.Windows.Forms.SystemInformation.VirtualScreen;
            var bitmap = new Bitmap(rc.Width, rc.Height, PixelFormat.Format32bppArgb);
            using (Graphics memoryGrahics = Graphics.FromImage(bitmap)) {
                memoryGrahics.CopyFromScreen(rc.X, rc.Y, 0, 0, rc.Size, CopyPixelOperation.SourceCopy);
            }

            return bitmap;
        }

        #endregion

        #region 通用截圖API

        private BitmapImage BitmapToImageSource(Bitmap bitmap) {
            using (MemoryStream memory = new MemoryStream()) {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }

        public enum SnapshotMethod {
            Auto,
            GraphicsAPICopyFromScreen,
            MagnificationAPIWithPrintWindow,
            MagnificationAPIWithCallback
        }

        public enum OutputImageMIMEFormat {
            Png,
            Bmp,
            Jpeg,
        }

        public class SnapshotConfig {
            public SnapshotMethod SnapshotMethod { get; set; } = SnapshotMethod.Auto;
            public bool IsCopyToClipboard { get; set; } = false;
            public bool IsSaveToLocal { get; set; } = true;
            public DirectoryInfo BitmapSavePath { get; set; } = null;
            public string SaveBitmapFileName { get; set; } = "Screenshot-[YYYY]-[MM]-[DD]-[HH]-[mm]-[ss].png";
            public OutputImageMIMEFormat OutputMIMEType { get; set; } = OutputImageMIMEFormat.Png;
            public HWND[] ExcludedHwnds { get; set; } = new HWND[] { };
            /// <summary>
            /// 是否将墨迹合成到截图中
            /// </summary>
            public bool AttachInk { get; set; } = false;
            /// <summary>
            /// 要合成的墨迹笔画集合（如果为 null 且 AttachInk 为 true，则使用主窗口的墨迹）
            /// </summary>
            public System.Windows.Ink.StrokeCollection InkStrokes { get; set; } = null;
            /// <summary>
            /// 保存的文件完整路径（在保存后由系统填充）
            /// </summary>
            public string SavedFilePath { get; set; } = null;
        }

        private static ImageCodecInfo GetEncoderInfo(string mimeType) {
            foreach (ImageCodecInfo codec in ImageCodecInfo.GetImageEncoders())
                if (codec.MimeType == mimeType)
                    return codec;

            return null;
        }

        public async Task<Bitmap> FullscreenSnapshot(SnapshotConfig config) {
            Bitmap bitmap = new Bitmap(1, 1);
            var excludedHwnds = new List<HWND>() { new HWND(new WindowInteropHelper(this).Handle) };
            excludedHwnds.AddRange(config.ExcludedHwnds);
            if (config.SnapshotMethod == SnapshotMethod.Auto) {
                if (OSVersion.GetOperatingSystem() >= OperatingSystem.Windows81) {
                    try {
                        bitmap = await SaveScreenshotToDesktopByMagnificationAPIAsync(excludedHwnds.ToArray(), false);
                    }
                    catch (Exception ex) {
                        // Magnification API 失败时，回退到 Graphics API
                        LogHelper.WriteLogToFile($"Magnification API failed, falling back to Graphics API: {ex.Message}", LogHelper.LogType.Warning);
                        bitmap = GetScreenshotBitmap();
                    }
                    
                    // 如果 Magnification API 返回无效位图，回退到 Graphics API
                    if (bitmap == null || bitmap.Width <= 1 || bitmap.Height <= 1) {
                        bitmap?.Dispose();
                        bitmap = GetScreenshotBitmap();
                    }
                } else {
                    if (excludedHwnds.Count != 0)
                        foreach (var hwnd in excludedHwnds)
                            ShowWindow(hwnd.DangerousGetHandle(), 0);
                    bitmap = GetScreenshotBitmap();
                    foreach (var hwnd in excludedHwnds) ShowWindow(hwnd.DangerousGetHandle(), 5);
                }
            } else if (config.SnapshotMethod == SnapshotMethod.MagnificationAPIWithPrintWindow ||
                       config.SnapshotMethod == SnapshotMethod.MagnificationAPIWithCallback) {
                if (!(OSVersion.GetOperatingSystem() >= OperatingSystem.Windows81))
                    throw new Exception("您的系統版本不支持 MagnificationAPI 截圖！");
                bitmap = await SaveScreenshotToDesktopByMagnificationAPIAsync(excludedHwnds.ToArray(),
                    config.SnapshotMethod == SnapshotMethod.MagnificationAPIWithCallback);
            } else if (config.SnapshotMethod == SnapshotMethod.GraphicsAPICopyFromScreen) {
                if (excludedHwnds.Count != 0)
                    foreach (var hwnd in excludedHwnds)
                        ShowWindow(hwnd.DangerousGetHandle(), 0);
                bitmap = GetScreenshotBitmap();
                foreach (var hwnd in excludedHwnds) ShowWindow(hwnd.DangerousGetHandle(), 5);
            }

            if (bitmap.Width == 1 && bitmap.Height == 1) throw new Exception("截圖失敗");

            // 如果启用了墨迹合成，将墨迹合成到截图上
            if (config.AttachInk) {
                var strokes = config.InkStrokes ?? inkCanvas.Strokes;
                if (strokes != null && strokes.Count > 0) {
                    var compositedBitmap = Helpers.InkCompositor.CompositeInkOnBitmap(bitmap, strokes);
                    bitmap.Dispose();
                    bitmap = compositedBitmap;
                }
            }

            try {
                if (config.IsCopyToClipboard) {
                    Clipboard.SetImage(BitmapToImageSource(bitmap));
                }
            }
            catch (NotSupportedException notSupportedEx) {
                LogHelper.WriteLogToFile($"Clipboard operation not supported: {notSupportedEx.Message}", LogHelper.LogType.Warning);
            }
            catch (Exception ex) {
                LogHelper.WriteLogToFile($"Failed to copy screenshot to clipboard: {ex.Message}", LogHelper.LogType.Warning);
            }

            if (config.IsSaveToLocal) {
                var fileName = GenerateFilename(config.SaveBitmapFileName, DateTime.Now, bitmap.Width, bitmap.Height);
                fileName = EnsureCorrectExtension(fileName, config.OutputMIMEType);
                var finalPath = GetValidSavePath(config, fileName, "Screenshots");
                bitmap.Save(finalPath, GetImageFormat(config.OutputMIMEType));
                config.SavedFilePath = finalPath; // 保存文件路径到配置对象
            }

            return bitmap;
        }

        #endregion

        #region 目录和路径处理

        /// <summary>
        /// 确保保存目录存在，如果驱动器无效则使用回退路径
        /// </summary>
        /// <param name="savePath">原始保存路径</param>
        /// <param name="fallbackSubfolder">回退子文件夹名称（例如 "Screenshots"）</param>
        /// <returns>有效的保存目录路径</returns>
        private string EnsureSaveDirectory(string savePath, string fallbackSubfolder = "Screenshots") {
            try {
                // 检查驱动器是否存在
                string drive = Path.GetPathRoot(savePath);
                if (!string.IsNullOrEmpty(drive) && !Directory.Exists(drive)) {
                    // 驱动器不存在，使用回退路径
                    string fallbackRoot = Path.Combine(App.RootPath, $"Fallback{fallbackSubfolder}");
                    string fallbackPath = fallbackRoot;
                    
                    LogHelper.WriteLogToFile($"Drive {drive} not found. Falling back to {fallbackPath}", 
                        LogHelper.LogType.Warning);
                    
                    savePath = fallbackPath;
                }
                
                // 确保目录存在
                if (!Directory.Exists(savePath)) {
                    Directory.CreateDirectory(savePath);
                }
                
                return savePath;
            }
            catch (Exception ex) {
                // 如果创建目录失败，使用应用程序根目录作为最后的回退
                LogHelper.WriteLogToFile($"Failed to create directory {savePath}: {ex.Message}. Using app root.", 
                    LogHelper.LogType.Error);
                
                string emergencyPath = Path.Combine(App.RootPath, fallbackSubfolder);
                if (!Directory.Exists(emergencyPath)) {
                    Directory.CreateDirectory(emergencyPath);
                }
                
                return emergencyPath;
            }
        }

        /// <summary>
        /// 获取有效的保存路径，包含目录验证和回退逻辑
        /// </summary>
        /// <param name="config">截图配置</param>
        /// <param name="fileName">文件名</param>
        /// <param name="fallbackSubfolder">回退子文件夹名称</param>
        /// <returns>完整的保存文件路径</returns>
        private string GetValidSavePath(SnapshotConfig config, string fileName, string fallbackSubfolder = "Screenshots") {
            var directoryPath = config.BitmapSavePath?.FullName ?? 
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            
            directoryPath = EnsureSaveDirectory(directoryPath, fallbackSubfolder);
            
            return Path.Combine(directoryPath, fileName);
        }

        #endregion

        #region 图像格式处理

        /// <summary>
        /// 根据 MIME 类型获取对应的 ImageFormat
        /// </summary>
        /// <param name="mimeType">MIME 类型</param>
        /// <returns>对应的 ImageFormat</returns>
        private ImageFormat GetImageFormat(OutputImageMIMEFormat mimeType) {
            switch (mimeType) {
                case OutputImageMIMEFormat.Png:
                    return ImageFormat.Png;
                case OutputImageMIMEFormat.Bmp:
                    return ImageFormat.Bmp;
                case OutputImageMIMEFormat.Jpeg:
                    return ImageFormat.Jpeg;
                default:
                    return ImageFormat.Png;
            }
        }

        /// <summary>
        /// 根据 MIME 类型获取对应的文件扩展名
        /// </summary>
        /// <param name="mimeType">MIME 类型</param>
        /// <returns>文件扩展名（包含点号）</returns>
        private string GetFileExtension(OutputImageMIMEFormat mimeType) {
            switch (mimeType) {
                case OutputImageMIMEFormat.Png:
                    return ".png";
                case OutputImageMIMEFormat.Bmp:
                    return ".bmp";
                case OutputImageMIMEFormat.Jpeg:
                    return ".jpg";
                default:
                    return ".png";
            }
        }

        /// <summary>
        /// 确保文件名具有正确的扩展名
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="mimeType">MIME 类型</param>
        /// <returns>具有正确扩展名的文件名</returns>
        private string EnsureCorrectExtension(string fileName, OutputImageMIMEFormat mimeType) {
            var expectedExtension = GetFileExtension(mimeType);
            var currentExtension = Path.GetExtension(fileName).ToLowerInvariant();
            
            // 如果文件名已经有正确的扩展名，直接返回
            if (currentExtension == expectedExtension.ToLowerInvariant()) {
                return fileName;
            }
            
            // 如果文件名有其他扩展名，替换它
            if (!string.IsNullOrEmpty(currentExtension)) {
                return Path.ChangeExtension(fileName, expectedExtension);
            }
            
            // 如果文件名没有扩展名，添加正确的扩展名
            return fileName + expectedExtension;
        }

        #endregion

        #region 文件名生成

        /// <summary>
        /// 根据模式生成文件名，支持日期时间占位符替换
        /// </summary>
        /// <param name="pattern">文件名模式，支持占位符：[YYYY], [MM], [DD], [HH], [mm], [ss], [width], [height]</param>
        /// <param name="dateTime">用于替换的日期时间，如果为 null 则使用当前时间</param>
        /// <param name="width">图像宽度（可选）</param>
        /// <param name="height">图像高度（可选）</param>
        /// <returns>生成的文件名</returns>
        public string GenerateFilename(string pattern, DateTime? dateTime = null, int? width = null, int? height = null) {
            var dt = dateTime ?? DateTime.Now;
            
            var fileName = pattern
                .Replace("[YYYY]", dt.Year.ToString())
                .Replace("[MM]", dt.Month.ToString())
                .Replace("[DD]", dt.Day.ToString())
                .Replace("[HH]", dt.Hour.ToString())
                .Replace("[mm]", dt.Minute.ToString())
                .Replace("[ss]", dt.Second.ToString());
            
            if (width.HasValue) {
                fileName = fileName.Replace("[width]", width.Value.ToString());
            }
            
            if (height.HasValue) {
                fileName = fileName.Replace("[height]", height.Value.ToString());
            }
            
            return fileName;
        }

        #endregion

        #region 选区截图

        /// <summary>
        /// 截取指定区域的屏幕内容
        /// </summary>
        /// <param name="region">要截取的区域（屏幕坐标，已考虑 DPI）</param>
        /// <returns>截取的位图</returns>
        public Bitmap CaptureRegion(Rect region) {
            // 验证区域有效性
            if (region.Width <= 0 || region.Height <= 0) {
                throw new ArgumentException("选区宽度和高度必须大于0");
            }

            int x = (int)Math.Round(region.X);
            int y = (int)Math.Round(region.Y);
            int width = (int)Math.Round(region.Width);
            int height = (int)Math.Round(region.Height);

            // 确保不超出屏幕边界
            var screenBounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
            if (x < 0) { width += x; x = 0; }
            if (y < 0) { height += y; y = 0; }
            if (x + width > screenBounds.Width) width = screenBounds.Width - x;
            if (y + height > screenBounds.Height) height = screenBounds.Height - y;

            if (width <= 0 || height <= 0) {
                throw new ArgumentException("选区超出屏幕边界");
            }

            // 使用 Graphics API 截取指定区域
            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bitmap)) {
                g.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(width, height), CopyPixelOperation.SourceCopy);
            }

            return bitmap;
        }

        /// <summary>
        /// 异步截取指定区域的屏幕内容
        /// </summary>
        /// <param name="region">要截取的区域（屏幕坐标，已考虑 DPI）</param>
        /// <returns>截取的位图</returns>
        public async Task<Bitmap> CaptureRegionAsync(Rect region) {
            return await Task.Run(() => CaptureRegion(region));
        }

        /// <summary>
        /// 截取选区并保存/复制到剪贴板
        /// </summary>
        /// <param name="region">要截取的区域（屏幕坐标，已考虑 DPI）</param>
        /// <param name="config">截图配置</param>
        /// <returns>截取的位图</returns>
        public async Task<Bitmap> CaptureRegionWithConfig(Rect region, SnapshotConfig config) {
            var bitmap = await CaptureRegionAsync(region);

            if (bitmap.Width == 0 || bitmap.Height == 0) {
                throw new Exception("选区截图失败：截图尺寸为0");
            }

            // 如果启用了墨迹合成，将墨迹合成到截图上
            if (config.AttachInk) {
                var strokes = config.InkStrokes ?? inkCanvas.Strokes;
                if (strokes != null && strokes.Count > 0) {
                    var compositedBitmap = Helpers.InkCompositor.CompositeInkOnBitmapForRegion(bitmap, strokes, region);
                    bitmap.Dispose();
                    bitmap = compositedBitmap;
                }
            }

            try {
                if (config.IsCopyToClipboard) {
                    Clipboard.SetImage(BitmapToImageSource(bitmap));
                }
            }
            catch (Exception ex) {
                // 剪贴板操作失败时继续执行保存操作
                LogHelper.WriteLogToFile($"Failed to copy region screenshot to clipboard: {ex.Message}", LogHelper.LogType.Warning);
            }

            if (config.IsSaveToLocal) {
                var fileName = GenerateFilename(config.SaveBitmapFileName, DateTime.Now, bitmap.Width, bitmap.Height);
                fileName = EnsureCorrectExtension(fileName, config.OutputMIMEType);
                var finalPath = GetValidSavePath(config, fileName, "RegionScreenshots");
                bitmap.Save(finalPath, GetImageFormat(config.OutputMIMEType));
                config.SavedFilePath = finalPath; // 保存文件路径到配置对象
            }

            return bitmap;
        }

        #endregion

        #region 窗口截图保存

        /// <summary>
        /// 保存窗口截图
        /// </summary>
        /// <param name="bitmap">窗口截图位图</param>
        /// <param name="config">截图配置</param>
        /// <returns>保存的位图</returns>
        public async Task<Bitmap> SaveWindowScreenshot(Bitmap bitmap, SnapshotConfig config) {
            if (bitmap == null || bitmap.Width == 0 || bitmap.Height == 0) {
                throw new ArgumentException("无效的窗口截图位图");
            }

            // 如果启用了墨迹合成，将墨迹合成到截图上
            if (config.AttachInk) {
                var strokes = config.InkStrokes ?? inkCanvas.Strokes;
                if (strokes != null && strokes.Count > 0) {
                    var compositedBitmap = Helpers.InkCompositor.CompositeInkOnBitmap(bitmap, strokes);
                    bitmap.Dispose();
                    bitmap = compositedBitmap;
                }
            }

            try {
                if (config.IsCopyToClipboard) {
                    await Dispatcher.InvokeAsync(() => {
                        Clipboard.SetImage(BitmapToImageSource(bitmap));
                    });
                }
            }
            catch (Exception ex) {
                // 剪贴板操作失败时继续执行保存操作
                LogHelper.WriteLogToFile($"Failed to copy window screenshot to clipboard: {ex.Message}", LogHelper.LogType.Warning);
            }

            if (config.IsSaveToLocal) {
                var fileName = GenerateFilename(config.SaveBitmapFileName, DateTime.Now, bitmap.Width, bitmap.Height);
                fileName = EnsureCorrectExtension(fileName, config.OutputMIMEType);
                var finalPath = GetValidSavePath(config, fileName, "WindowScreenshots");
                await Task.Run(() => bitmap.Save(finalPath, GetImageFormat(config.OutputMIMEType)));
                config.SavedFilePath = finalPath; // 保存文件路径到配置对象
            }

            return bitmap;
        }

        #endregion

        private void SaveScreenshot(bool isHideNotification, string fileName = null) {
            var bitmap = GetScreenshotBitmap();
            string rootPath = Settings.Automation.AutoSavedStrokesLocation;
            string savePath;
            if (fileName == null) fileName = DateTime.Now.ToString("u").Replace(":", "-");

            if (Settings.Automation.IsSaveScreenshotsInDateFolders) {
                savePath = Path.Combine(rootPath, "Auto Saved - Screenshots", DateTime.Now.ToString("yyyy-MM-dd"), fileName + ".png");
            } else {
                savePath = Path.Combine(rootPath, "Auto Saved - Screenshots", fileName + ".png");
            }

            string directoryPath = Path.GetDirectoryName(savePath);

            // Check if drive exists, if not, fallback to Data folder
            string drive = Path.GetPathRoot(directoryPath);
            if (!string.IsNullOrEmpty(drive) && !Directory.Exists(drive)) {
                string fallbackRoot = Path.Combine(App.RootPath, "FallbackScreenshots");
                directoryPath = Path.Combine(fallbackRoot, Settings.Automation.IsSaveScreenshotsInDateFolders ? DateTime.Now.ToString("yyyy-MM-dd") : "");
                savePath = Path.Combine(directoryPath, fileName + ".png");
                LogHelper.WriteLogToFile($"Drive {drive} not found. Falling back to {directoryPath}", LogHelper.LogType.Warning);
            }

            if (!Directory.Exists(directoryPath)) {
                Directory.CreateDirectory(directoryPath);
            }

            bitmap.Save(savePath, ImageFormat.Png);
            if (Settings.Automation.IsAutoSaveStrokesAtScreenshot) {
                SaveInkCanvasStrokes(false, false);
            }

            if (!isHideNotification) {
                ShowNewToast("截图成功保存至 " + savePath, MW_Toast.ToastType.Success, 3000);
            }
        }

        private void SaveScreenShotToDesktop() {
            var bitmap = GetScreenshotBitmap();
            string savePath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            bitmap.Save(savePath + @"\" + DateTime.Now.ToString("u").Replace(':', '-') + ".png", ImageFormat.Png);
            ShowNewToast("截图成功保存至【桌面" + @"\" + DateTime.Now.ToString("u").Replace(':', '-') + ".png】",
                MW_Toast.ToastType.Success, 3000);
            if (Settings.Automation.IsAutoSaveStrokesAtScreenshot) SaveInkCanvasStrokes(false, false);
        }

        private void SavePPTScreenshot(string fileName) {
            var bitmap = GetScreenshotBitmap();
            string rootPath = Settings.Automation.AutoSavedStrokesLocation;
            string savePath;
            if (fileName == null) fileName = DateTime.Now.ToString("u").Replace(":", "-");

            if (Settings.Automation.IsSaveScreenshotsInDateFolders) {
                savePath = Path.Combine(rootPath, "Auto Saved - PPT Screenshots", DateTime.Now.ToString("yyyy-MM-dd"), fileName + ".png");
            } else {
                savePath = Path.Combine(rootPath, "Auto Saved - PPT Screenshots", fileName + ".png");
            }

            string directoryPath = Path.GetDirectoryName(savePath);

            // Check if drive exists, if not, fallback to Data folder
            string drive = Path.GetPathRoot(directoryPath);
            if (!string.IsNullOrEmpty(drive) && !Directory.Exists(drive)) {
                string fallbackRoot = Path.Combine(App.RootPath, "FallbackPPTScreenshots");
                directoryPath = Path.Combine(fallbackRoot, Settings.Automation.IsSaveScreenshotsInDateFolders ? DateTime.Now.ToString("yyyy-MM-dd") : "");
                savePath = Path.Combine(directoryPath, fileName + ".png");
                LogHelper.WriteLogToFile($"Drive {drive} not found. Falling back to {directoryPath}", LogHelper.LogType.Warning);
            }

            string finalDirectoryPath = Path.GetDirectoryName(savePath);
            if (!Directory.Exists(finalDirectoryPath)) {
                Directory.CreateDirectory(finalDirectoryPath);
            }

            bitmap.Save(savePath, ImageFormat.Png);
            if (Settings.Automation.IsAutoSaveStrokesAtScreenshot) {
                SaveInkCanvasStrokes(false, false);
            }
        }
    }
}
