using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Ink_Canvas.Helpers
{
    /// <summary>
    /// 墨迹合成器，用于将墨迹笔画合成到位图上
    /// </summary>
    public static class InkCompositor
    {
        [ThreadStatic]
        private static TranslateTransform _cachedTransform;
        
        /// <summary>
        /// 将墨迹笔画合成到背景位图上
        /// </summary>
        /// <param name="background">背景位图</param>
        /// <param name="strokes">要合成的墨迹笔画集合</param>
        /// <param name="offsetX">墨迹相对于屏幕的 X 偏移量（默认为 0）</param>
        /// <param name="offsetY">墨迹相对于屏幕的 Y 偏移量（默认为 0）</param>
        /// <returns>合成后的位图</returns>
        public static Bitmap CompositeInkOnBitmap(Bitmap background, StrokeCollection strokes, double offsetX = 0, double offsetY = 0)
        {
            if (background == null)
                throw new ArgumentNullException(nameof(background));

            if (strokes == null || strokes.Count == 0)
                return background;

            // 创建结果位图
            var result = new Bitmap(background.Width, background.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (var graphics = Graphics.FromImage(result))
            {
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Low;
                graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighSpeed;
                graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;

                graphics.DrawImage(background, 0, 0, background.Width, background.Height);

                var inkBitmap = RenderStrokesToBitmap(strokes, background.Width, background.Height, offsetX, offsetY);
                if (inkBitmap != null)
                {
                    graphics.DrawImage(inkBitmap, 0, 0, background.Width, background.Height);
                    inkBitmap.Dispose();
                }
            }

            return result;
        }

        /// <summary>
        /// 将墨迹笔画渲染为位图
        /// </summary>
        /// <param name="strokes">墨迹笔画集合</param>
        /// <param name="width">位图宽度</param>
        /// <param name="height">位图高度</param>
        /// <param name="offsetX">X 偏移量</param>
        /// <param name="offsetY">Y 偏移量</param>
        /// <returns>渲染后的位图</returns>
        private static Bitmap RenderStrokesToBitmap(StrokeCollection strokes, int width, int height, double offsetX, double offsetY)
        {
            if (strokes == null || strokes.Count == 0 || width <= 0 || height <= 0)
                return null;

            try
            {
                var drawingVisual = new DrawingVisual();
                using (var drawingContext = drawingVisual.RenderOpen())
                {
                    if (offsetX != 0 || offsetY != 0)
                    {
                        if (_cachedTransform == null)
                            _cachedTransform = new TranslateTransform();
                        
                        _cachedTransform.X = -offsetX;
                        _cachedTransform.Y = -offsetY;
                        drawingContext.PushTransform(_cachedTransform);
                    }

                    strokes.Draw(drawingContext);

                    if (offsetX != 0 || offsetY != 0)
                    {
                        drawingContext.Pop();
                    }
                }

                var renderBitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                renderBitmap.Render(drawingVisual);

                return RenderTargetBitmapToBitmapFast(renderBitmap);
            }
            catch (InvalidOperationException ex) {
                LogHelper.WriteLogToFile($"RenderStrokesToBitmap failed (Invalid operation): {ex.Message}", LogHelper.LogType.Error);
                return null;
            }
            catch (ArgumentException ex) {
                LogHelper.WriteLogToFile($"RenderStrokesToBitmap failed (Invalid argument): {ex.Message}", LogHelper.LogType.Error);
                return null;
            }
            catch (System.Windows.Media.Animation.AnimationException ex) {
                LogHelper.WriteLogToFile($"RenderStrokesToBitmap failed (Animation error): {ex.Message}", LogHelper.LogType.Error);
                return null;
            }
        }

        /// <summary>
        /// 将 RenderTargetBitmap 转换为 System.Drawing.Bitmap（优化版本，直接复制像素数据）
        /// </summary>
        private static Bitmap RenderTargetBitmapToBitmapFast(RenderTargetBitmap renderBitmap)
        {
            if (renderBitmap == null)
                return null;

            int width = renderBitmap.PixelWidth;
            int height = renderBitmap.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            renderBitmap.CopyPixels(pixels, stride, 0);
            
            var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            
            try
            {
                Marshal.Copy(pixels, 0, bitmapData.Scan0, pixels.Length);
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }
            
            return bitmap;
        }
        
        /// <summary>
        /// 将 RenderTargetBitmap 转换为 System.Drawing.Bitmap（兼容版本，使用 PNG 编码）
        /// </summary>
        private static Bitmap RenderTargetBitmapToBitmap(RenderTargetBitmap renderBitmap)
        {
            if (renderBitmap == null)
                return null;

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

            using (var memoryStream = new MemoryStream())
            {
                encoder.Save(memoryStream);
                memoryStream.Position = 0;
                return new Bitmap(memoryStream);
            }
        }

        /// <summary>
        /// 将墨迹笔画合成到背景位图上（异步版本）
        /// </summary>
        /// <param name="background">背景位图</param>
        /// <param name="strokes">要合成的墨迹笔画集合</param>
        /// <param name="offsetX">墨迹相对于屏幕的 X 偏移量</param>
        /// <param name="offsetY">墨迹相对于屏幕的 Y 偏移量</param>
        /// <returns>合成后的位图</returns>
        public static System.Threading.Tasks.Task<Bitmap> CompositeInkOnBitmapAsync(
            Bitmap background, 
            StrokeCollection strokes, 
            double offsetX = 0, 
            double offsetY = 0)
        {
            return System.Threading.Tasks.Task.Run(() => CompositeInkOnBitmap(background, strokes, offsetX, offsetY));
        }

        /// <summary>
        /// 将墨迹笔画合成到指定区域的背景位图上
        /// </summary>
        /// <param name="background">背景位图</param>
        /// <param name="strokes">要合成的墨迹笔画集合</param>
        /// <param name="region">截图区域（屏幕坐标）</param>
        /// <returns>合成后的位图</returns>
        public static Bitmap CompositeInkOnBitmapForRegion(Bitmap background, StrokeCollection strokes, Rect region)
        {
            return CompositeInkOnBitmap(background, strokes, region.X, region.Y);
        }
    }
}
