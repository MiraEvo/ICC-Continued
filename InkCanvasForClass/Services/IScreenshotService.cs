using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Ink;
using Vanara.PInvoke;

namespace Ink_Canvas.Services
{
    /// <summary>
    /// 截图服务接口
    /// 负责处理各种截图功能，包括全屏截图、选区截图和窗口截图
    /// </summary>
    public interface IScreenshotService
    {
        /// <summary>
        /// 截图模式枚举
        /// </summary>
        enum ScreenshotMode
        {
            /// <summary>
            /// 全屏截图
            /// </summary>
            FullScreen,
            
            /// <summary>
            /// 选区截图
            /// </summary>
            Selection,
            
            /// <summary>
            /// 窗口截图
            /// </summary>
            Window
        }

        /// <summary>
        /// 截图方法枚举
        /// </summary>
        enum SnapshotMethod
        {
            Auto,
            GraphicsAPICopyFromScreen,
            MagnificationAPIWithPrintWindow,
            MagnificationAPIWithCallback
        }

        /// <summary>
        /// 输出图像格式枚举
        /// </summary>
        enum OutputImageMIMEFormat
        {
            Png,
            Bmp,
            Jpeg
        }

        /// <summary>
        /// 截图结果类
        /// </summary>
        class ScreenshotResult
        {
            /// <summary>
            /// 是否成功
            /// </summary>
            public bool Success { get; set; }
            
            /// <summary>
            /// 截图位图（成功时）
            /// </summary>
            public Bitmap Bitmap { get; set; }
            
            /// <summary>
            /// 错误消息（失败时）
            /// </summary>
            public string ErrorMessage { get; set; }
            
            /// <summary>
            /// 异常信息（失败时）
            /// </summary>
            public Exception Exception { get; set; }
            
            /// <summary>
            /// 保存的文件路径（如果已保存）
            /// </summary>
            public string SavedFilePath { get; set; }
            
            /// <summary>
            /// 截图模式
            /// </summary>
            public ScreenshotMode Mode { get; set; }
            
            /// <summary>
            /// 创建成功结果
            /// </summary>
            public static ScreenshotResult CreateSuccess(Bitmap bitmap, ScreenshotMode mode, string savedFilePath = null)
            {
                return new ScreenshotResult
                {
                    Success = true,
                    Bitmap = bitmap,
                    Mode = mode,
                    SavedFilePath = savedFilePath
                };
            }
            
            /// <summary>
            /// 创建失败结果
            /// </summary>
            public static ScreenshotResult CreateFailure(string errorMessage, Exception exception, ScreenshotMode mode)
            {
                return new ScreenshotResult
                {
                    Success = false,
                    ErrorMessage = errorMessage,
                    Exception = exception,
                    Mode = mode
                };
            }
        }

        /// <summary>
        /// 截图配置类
        /// </summary>
        class SnapshotConfig
        {
            public SnapshotMethod SnapshotMethod { get; set; } = SnapshotMethod.Auto;
            public bool IsCopyToClipboard { get; set; } = false;
            public bool IsSaveToLocal { get; set; } = true;
            public System.IO.DirectoryInfo BitmapSavePath { get; set; } = null;
            public string SaveBitmapFileName { get; set; } = "Screenshot-[YYYY]-[MM]-[DD]-[HH]-[mm]-[ss].png";
            public OutputImageMIMEFormat OutputMIMEType { get; set; } = OutputImageMIMEFormat.Png;
            public HWND[] ExcludedHwnds { get; set; } = new HWND[] { };
            public bool AttachInk { get; set; } = false;
            public StrokeCollection InkStrokes { get; set; } = null;
            public string SavedFilePath { get; set; } = null;
        }

        /// <summary>
        /// 通用截图方法（支持不同模式）
        /// </summary>
        /// <param name="mode">截图模式</param>
        /// <param name="config">截图配置</param>
        /// <param name="region">选区（仅在 Selection 模式下使用）</param>
        /// <param name="windowHandle">窗口句柄（仅在 Window 模式下使用）</param>
        /// <returns>截图结果</returns>
        Task<ScreenshotResult> CaptureAsync(
            ScreenshotMode mode,
            SnapshotConfig config = null,
            Rect? region = null,
            IntPtr? windowHandle = null);

        /// <summary>
        /// 全屏截图
        /// </summary>
        /// <param name="config">截图配置</param>
        /// <returns>截图位图</returns>
        Task<Bitmap> FullscreenSnapshotAsync(SnapshotConfig config);

        /// <summary>
        /// 截取指定区域
        /// </summary>
        /// <param name="region">要截取的区域（屏幕坐标）</param>
        /// <returns>截图位图</returns>
        Task<Bitmap> CaptureRegionAsync(Rect region);

        /// <summary>
        /// 截取指定区域并应用配置
        /// </summary>
        /// <param name="region">要截取的区域（屏幕坐标）</param>
        /// <param name="config">截图配置</param>
        /// <returns>截图位图</returns>
        Task<Bitmap> CaptureRegionWithConfigAsync(Rect region, SnapshotConfig config);

        /// <summary>
        /// 保存窗口截图
        /// </summary>
        /// <param name="bitmap">窗口截图位图</param>
        /// <param name="config">截图配置</param>
        /// <returns>保存的位图</returns>
        Task<Bitmap> SaveWindowScreenshotAsync(Bitmap bitmap, SnapshotConfig config);

        /// <summary>
        /// 保存截图（旧版接口，用于兼容）
        /// </summary>
        /// <param name="isHideNotification">是否隐藏通知</param>
        /// <param name="fileName">文件名（可选）</param>
        void SaveScreenshot(bool isHideNotification, string fileName = null);

        /// <summary>
        /// 保存截图到桌面
        /// </summary>
        void SaveScreenshotToDesktop();

        /// <summary>
        /// 保存 PPT 截图
        /// </summary>
        /// <param name="fileName">文件名</param>
        void SavePPTScreenshot(string fileName);

        /// <summary>
        /// 根据模式生成文件名
        /// </summary>
        /// <param name="pattern">文件名模式</param>
        /// <param name="dateTime">日期时间（可选）</param>
        /// <param name="width">图像宽度（可选）</param>
        /// <param name="height">图像高度（可选）</param>
        /// <returns>生成的文件名</returns>
        string GenerateFilename(string pattern, DateTime? dateTime = null, int? width = null, int? height = null);

        /// <summary>
        /// 获取所有窗口信息
        /// </summary>
        /// <param name="excludedHwnds">要排除的窗口句柄数组</param>
        /// <returns>窗口信息数组</returns>
        Task<WindowInformation[]> GetAllWindowsAsync(HWND[] excludedHwnds);

        /// <summary>
        /// 窗口信息类
        /// </summary>
        class WindowInformation
        {
            public string Title { get; set; }
            public Bitmap WindowBitmap { get; set; }
            public Icon AppIcon { get; set; }
            public bool IsVisible { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public RECT Rect { get; set; }
            public IntPtr Handle { get; set; }
            public RECT RealRect { get; set; }
            public Rectangle ContentRect { get; set; }
            public int WindowDPI { get; set; }
            public int SystemDPI { get; set; }
            public double DPIScale { get; set; }
        }
    }
}
