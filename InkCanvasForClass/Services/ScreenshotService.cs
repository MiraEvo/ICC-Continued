using Ink_Canvas.Helpers;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Ink;
using Vanara.PInvoke;

namespace Ink_Canvas.Services
{
    /// <summary>
    /// 截图服务实现
    /// 负责处理各种截图功能，包括全屏截图、选区截图和窗口截图
    /// </summary>
    public class ScreenshotService : IScreenshotService
    {
        private readonly ISettingsService _settingsService;
        private readonly INotificationService _notificationService;
        private readonly Func<StrokeCollection> _getInkStrokes;
        private readonly Action<bool, bool> _saveInkCanvasStrokes;

        public ScreenshotService(
            ISettingsService settingsService,
            INotificationService notificationService,
            Func<StrokeCollection> getInkStrokes,
            Action<bool, bool> saveInkCanvasStrokes)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _getInkStrokes = getInkStrokes ?? throw new ArgumentNullException(nameof(getInkStrokes));
            _saveInkCanvasStrokes = saveInkCanvasStrokes ?? throw new ArgumentNullException(nameof(saveInkCanvasStrokes));
        }

        #region 全屏截图

        public async Task<Bitmap> FullscreenSnapshotAsync(IScreenshotService.SnapshotConfig config)
        {
            // 注意：完整的实现需要从 MainWindow 的 MW_Screenshot.cs 中提取
            // 这里提供一个简化的实现框架
            throw new NotImplementedException("FullscreenSnapshotAsync needs to be implemented with Magnification API support");
        }

        #endregion

        #region 选区截图

        public async Task<Bitmap> CaptureRegionAsync(Rect region)
        {
            try
            {
                return await Task.Run(() => CaptureRegion(region));
            }
            catch (ArgumentException ex)
            {
                LogHelper.WriteLogToFile($"Invalid region for capture: {ex.Message}", LogHelper.LogType.Error);
                LogHelper.NewLog(ex);
                throw;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"Unexpected error capturing region async: {ex.Message}", LogHelper.LogType.Error);
                LogHelper.NewLog(ex);
                throw;
            }
        }

        private Bitmap CaptureRegion(Rect region)
        {
            // 验证区域有效性
            if (region.Width <= 0 || region.Height <= 0)
            {
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

            if (width <= 0 || height <= 0)
            {
                throw new ArgumentException("选区超出屏幕边界");
            }

            // 使用 Graphics API 截取指定区域
            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(width, height), CopyPixelOperation.SourceCopy);
            }

            return bitmap;
        }

        public async Task<Bitmap> CaptureRegionWithConfigAsync(Rect region, IScreenshotService.SnapshotConfig config)
        {
            try
            {
                var bitmap = await CaptureRegionAsync(region);

                if (bitmap.Width == 0 || bitmap.Height == 0)
                {
                    throw new Exception("选区截图失败：截图尺寸为0");
                }

                // 如果启用了墨迹合成，将墨迹合成到截图上
                if (config.AttachInk)
                {
                    try
                    {
                        var strokes = config.InkStrokes ?? _getInkStrokes();
                        if (strokes != null && strokes.Count > 0)
                        {
                            var compositedBitmap = InkCompositor.CompositeInkOnBitmapForRegion(bitmap, strokes, region);
                            bitmap.Dispose();
                            bitmap = compositedBitmap;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.WriteLogToFile($"Failed to composite ink on region screenshot: {ex.Message}", LogHelper.LogType.Warning);
                        LogHelper.NewLog(ex);
                        // Continue without ink composition
                    }
                }

                try
                {
                    if (config.IsCopyToClipboard)
                    {
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            Clipboard.SetImage(BitmapToImageSource(bitmap));
                        });
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"Failed to copy region screenshot to clipboard: {ex.Message}", LogHelper.LogType.Warning);
                    LogHelper.NewLog(ex);
                    // Continue without clipboard copy
                }

                if (config.IsSaveToLocal)
                {
                    try
                    {
                        var fileName = GenerateFilename(config.SaveBitmapFileName, DateTime.Now, bitmap.Width, bitmap.Height);
                        fileName = EnsureCorrectExtension(fileName, config.OutputMIMEType);
                        var finalPath = GetValidSavePath(config, fileName, "RegionScreenshots");
                        bitmap.Save(finalPath, GetImageFormat(config.OutputMIMEType));
                        config.SavedFilePath = finalPath;
                    }
                    catch (Exception ex)
                    {
                        LogHelper.WriteLogToFile($"Failed to save region screenshot to file: {ex.Message}", LogHelper.LogType.Error);
                        LogHelper.NewLog(ex);
                        throw;
                    }
                }

                return bitmap;
            }
            catch (ArgumentException ex)
            {
                LogHelper.WriteLogToFile($"Invalid arguments for region capture: {ex.Message}", LogHelper.LogType.Error);
                LogHelper.NewLog(ex);
                throw;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"Unexpected error in CaptureRegionWithConfigAsync: {ex.Message}", LogHelper.LogType.Error);
                LogHelper.NewLog(ex);
                throw;
            }
        }

        #endregion

        #region 窗口截图

        public async Task<Bitmap> SaveWindowScreenshotAsync(Bitmap bitmap, IScreenshotService.SnapshotConfig config)
        {
            try
            {
                if (bitmap == null || bitmap.Width == 0 || bitmap.Height == 0)
                {
                    throw new ArgumentException("无效的窗口截图位图");
                }

                // 如果启用了墨迹合成，将墨迹合成到截图上
                if (config.AttachInk)
                {
                    try
                    {
                        var strokes = config.InkStrokes ?? _getInkStrokes();
                        if (strokes != null && strokes.Count > 0)
                        {
                            var compositedBitmap = InkCompositor.CompositeInkOnBitmap(bitmap, strokes);
                            bitmap.Dispose();
                            bitmap = compositedBitmap;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.WriteLogToFile($"Failed to composite ink on window screenshot: {ex.Message}", LogHelper.LogType.Warning);
                        LogHelper.NewLog(ex);
                        // Continue without ink composition
                    }
                }

                try
                {
                    if (config.IsCopyToClipboard)
                    {
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            Clipboard.SetImage(BitmapToImageSource(bitmap));
                        });
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"Failed to copy window screenshot to clipboard: {ex.Message}", LogHelper.LogType.Warning);
                    LogHelper.NewLog(ex);
                    // Continue without clipboard copy
                }

                if (config.IsSaveToLocal)
                {
                    try
                    {
                        var fileName = GenerateFilename(config.SaveBitmapFileName, DateTime.Now, bitmap.Width, bitmap.Height);
                        fileName = EnsureCorrectExtension(fileName, config.OutputMIMEType);
                        var finalPath = GetValidSavePath(config, fileName, "WindowScreenshots");
                        await Task.Run(() => bitmap.Save(finalPath, GetImageFormat(config.OutputMIMEType)));
                        config.SavedFilePath = finalPath;
                    }
                    catch (Exception ex)
                    {
                        LogHelper.WriteLogToFile($"Failed to save window screenshot to file: {ex.Message}", LogHelper.LogType.Error);
                        LogHelper.NewLog(ex);
                        throw;
                    }
                }

                return bitmap;
            }
            catch (ArgumentException ex)
            {
                LogHelper.WriteLogToFile($"Invalid arguments for window screenshot: {ex.Message}", LogHelper.LogType.Error);
                LogHelper.NewLog(ex);
                throw;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"Unexpected error in SaveWindowScreenshotAsync: {ex.Message}", LogHelper.LogType.Error);
                LogHelper.NewLog(ex);
                throw;
            }
        }

        #endregion

        #region 兼容性方法

        public void SaveScreenshot(bool isHideNotification, string fileName = null)
        {
            var bitmap = GetScreenshotBitmap();
            var settings = _settingsService.Settings;
            string rootPath = settings.Automation.AutoSavedStrokesLocation;
            string savePath;
            if (fileName == null) fileName = DateTime.Now.ToString("u").Replace(":", "-");

            if (settings.Automation.IsSaveScreenshotsInDateFolders)
            {
                savePath = Path.Combine(rootPath, "Auto Saved - Screenshots", DateTime.Now.ToString("yyyy-MM-dd"), fileName + ".png");
            }
            else
            {
                savePath = Path.Combine(rootPath, "Auto Saved - Screenshots", fileName + ".png");
            }

            string directoryPath = Path.GetDirectoryName(savePath);
            directoryPath = EnsureSaveDirectory(directoryPath, "Screenshots");
            savePath = Path.Combine(directoryPath, fileName + ".png");

            bitmap.Save(savePath, ImageFormat.Png);
            if (settings.Automation.IsAutoSaveStrokesAtScreenshot)
            {
                _saveInkCanvasStrokes(false, false);
            }

            if (!isHideNotification)
            {
                _notificationService.ShowToast("截图成功保存至 " + savePath, INotificationService.ToastType.Success, 3000);
            }
        }

        public void SaveScreenshotToDesktop()
        {
            var bitmap = GetScreenshotBitmap();
            string savePath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var fileName = DateTime.Now.ToString("u").Replace(':', '-') + ".png";
            bitmap.Save(Path.Combine(savePath, fileName), ImageFormat.Png);
            
            _notificationService.ShowToast($"截图成功保存至【桌面\\{fileName}】", INotificationService.ToastType.Success, 3000);
            
            var settings = _settingsService.Settings;
            if (settings.Automation.IsAutoSaveStrokesAtScreenshot)
            {
                _saveInkCanvasStrokes(false, false);
            }
        }

        public void SavePPTScreenshot(string fileName)
        {
            var bitmap = GetScreenshotBitmap();
            var settings = _settingsService.Settings;
            string rootPath = settings.Automation.AutoSavedStrokesLocation;
            string savePath;
            if (fileName == null) fileName = DateTime.Now.ToString("u").Replace(":", "-");

            if (settings.Automation.IsSaveScreenshotsInDateFolders)
            {
                savePath = Path.Combine(rootPath, "Auto Saved - PPT Screenshots", DateTime.Now.ToString("yyyy-MM-dd"), fileName + ".png");
            }
            else
            {
                savePath = Path.Combine(rootPath, "Auto Saved - PPT Screenshots", fileName + ".png");
            }

            string directoryPath = Path.GetDirectoryName(savePath);
            directoryPath = EnsureSaveDirectory(directoryPath, "PPTScreenshots");
            savePath = Path.Combine(directoryPath, fileName + ".png");

            bitmap.Save(savePath, ImageFormat.Png);
            if (settings.Automation.IsAutoSaveStrokesAtScreenshot)
            {
                _saveInkCanvasStrokes(false, false);
            }
        }

        #endregion

        #region 辅助方法

        private Bitmap GetScreenshotBitmap()
        {
            Rectangle rc = System.Windows.Forms.SystemInformation.VirtualScreen;
            var bitmap = new Bitmap(rc.Width, rc.Height, PixelFormat.Format32bppArgb);
            using (Graphics memoryGrahics = Graphics.FromImage(bitmap))
            {
                memoryGrahics.CopyFromScreen(rc.X, rc.Y, 0, 0, rc.Size, CopyPixelOperation.SourceCopy);
            }
            return bitmap;
        }

        private System.Windows.Media.Imaging.BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Bmp);
                memory.Position = 0;
                var bitmapimage = new System.Windows.Media.Imaging.BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();
                return bitmapimage;
            }
        }

        public string GenerateFilename(string pattern, DateTime? dateTime = null, int? width = null, int? height = null)
        {
            var dt = dateTime ?? DateTime.Now;

            var fileName = pattern
                .Replace("[YYYY]", dt.Year.ToString())
                .Replace("[MM]", dt.Month.ToString("D2"))
                .Replace("[DD]", dt.Day.ToString("D2"))
                .Replace("[HH]", dt.Hour.ToString("D2"))
                .Replace("[mm]", dt.Minute.ToString("D2"))
                .Replace("[ss]", dt.Second.ToString("D2"));

            if (width.HasValue)
            {
                fileName = fileName.Replace("[width]", width.Value.ToString());
            }

            if (height.HasValue)
            {
                fileName = fileName.Replace("[height]", height.Value.ToString());
            }

            return fileName;
        }

        private string EnsureSaveDirectory(string savePath, string fallbackSubfolder = "Screenshots")
        {
            try
            {
                string drive = Path.GetPathRoot(savePath);
                if (!string.IsNullOrEmpty(drive) && !Directory.Exists(drive))
                {
                    string fallbackRoot = Path.Combine(App.RootPath, $"Fallback{fallbackSubfolder}");
                    LogHelper.WriteLogToFile($"Drive {drive} not found. Falling back to {fallbackRoot}", LogHelper.LogType.Warning);
                    savePath = fallbackRoot;
                }

                if (!Directory.Exists(savePath))
                {
                    Directory.CreateDirectory(savePath);
                }

                return savePath;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"Failed to create directory {savePath}: {ex.Message}. Using app root.", LogHelper.LogType.Error);
                string emergencyPath = Path.Combine(App.RootPath, fallbackSubfolder);
                if (!Directory.Exists(emergencyPath))
                {
                    Directory.CreateDirectory(emergencyPath);
                }
                return emergencyPath;
            }
        }

        private string GetValidSavePath(IScreenshotService.SnapshotConfig config, string fileName, string fallbackSubfolder = "Screenshots")
        {
            var directoryPath = config.BitmapSavePath?.FullName ??
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            directoryPath = EnsureSaveDirectory(directoryPath, fallbackSubfolder);

            return Path.Combine(directoryPath, fileName);
        }

        private ImageFormat GetImageFormat(IScreenshotService.OutputImageMIMEFormat mimeType)
        {
            switch (mimeType)
            {
                case IScreenshotService.OutputImageMIMEFormat.Png:
                    return ImageFormat.Png;
                case IScreenshotService.OutputImageMIMEFormat.Bmp:
                    return ImageFormat.Bmp;
                case IScreenshotService.OutputImageMIMEFormat.Jpeg:
                    return ImageFormat.Jpeg;
                default:
                    return ImageFormat.Png;
            }
        }

        private string GetFileExtension(IScreenshotService.OutputImageMIMEFormat mimeType)
        {
            switch (mimeType)
            {
                case IScreenshotService.OutputImageMIMEFormat.Png:
                    return ".png";
                case IScreenshotService.OutputImageMIMEFormat.Bmp:
                    return ".bmp";
                case IScreenshotService.OutputImageMIMEFormat.Jpeg:
                    return ".jpg";
                default:
                    return ".png";
            }
        }

        private string EnsureCorrectExtension(string fileName, IScreenshotService.OutputImageMIMEFormat mimeType)
        {
            var expectedExtension = GetFileExtension(mimeType);
            var currentExtension = Path.GetExtension(fileName).ToLowerInvariant();

            if (currentExtension == expectedExtension.ToLowerInvariant())
            {
                return fileName;
            }

            if (!string.IsNullOrEmpty(currentExtension))
            {
                return Path.ChangeExtension(fileName, expectedExtension);
            }

            return fileName + expectedExtension;
        }

        public async Task<IScreenshotService.WindowInformation[]> GetAllWindowsAsync(HWND[] excludedHwnds)
        {
            // 注意：完整的实现需要从 MainWindow 的 MW_Screenshot.cs 中提取
            // 这里提供一个简化的实现框架
            throw new NotImplementedException("GetAllWindowsAsync needs to be implemented with window enumeration support");
        }

        #endregion
    }
}
