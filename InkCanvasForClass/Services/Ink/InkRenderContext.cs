using System;
using System.Collections.Concurrent;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Ink_Canvas.Services.Ink
{
    /// <summary>
    /// 渲染上下文 - 管理渲染资源和状态
    /// </summary>
    public class InkRenderContext : IDisposable
    {
        private readonly ConcurrentDictionary<Color, SolidColorBrush> _brushCache = new();
        private readonly ConcurrentDictionary<(Color color, double width, DashStyle dashStyle), Pen> _penCache = new();
        private readonly ConcurrentDictionary<int, Geometry> _geometryCache = new();
        private WriteableBitmap _writeableBitmap;
        private bool _disposed;

        /// <summary>
        /// 渲染目标位图
        /// </summary>
        public WriteableBitmap WriteableBitmap => _writeableBitmap;

        /// <summary>
        /// 当前渲染尺寸
        /// </summary>
        public Size RenderSize { get; private set; }

        /// <summary>
        /// DPI 设置
        /// </summary>
        public double DpiX { get; set; } = 96.0;

        /// <summary>
        /// DPI 设置
        /// </summary>
        public double DpiY { get; set; } = 96.0;

        /// <summary>
        /// 是否启用抗锯齿
        /// </summary>
        public bool EnableAntiAliasing { get; set; } = true;

        /// <summary>
        /// 初始化渲染上下文
        /// </summary>
        public void Initialize(int width, int height)
        {
            if (_writeableBitmap != null && 
                _writeableBitmap.PixelWidth == width && 
                _writeableBitmap.PixelHeight == height)
            {
                return;
            }

            // WriteableBitmap 不需要 Dispose
            _writeableBitmap = new WriteableBitmap(
                width, 
                height, 
                DpiX, 
                DpiY, 
                PixelFormats.Pbgra32, 
                null);
            
            RenderSize = new Size(width, height);
        }

        /// <summary>
        /// 获取或创建画刷
        /// </summary>
        public SolidColorBrush GetBrush(Color color)
        {
            return _brushCache.GetOrAdd(color, c =>
            {
                var brush = new SolidColorBrush(c);
                brush.Freeze();
                return brush;
            });
        }

        /// <summary>
        /// 获取或创建画笔
        /// </summary>
        public Pen GetPen(Color color, double width, DashStyle dashStyle = null)
        {
            var key = (color, width, dashStyle);
            return _penCache.GetOrAdd(key, k =>
            {
                var pen = new Pen(GetBrush(k.color), k.width)
                {
                    DashCap = PenLineCap.Round,
                    StartLineCap = PenLineCap.Round,
                    EndLineCap = PenLineCap.Round,
                    LineJoin = PenLineJoin.Round,
                    DashStyle = k.dashStyle ?? DashStyles.Solid
                };
                pen.Freeze();
                return pen;
            });
        }

        /// <summary>
        /// 获取或缓存几何图形
        /// </summary>
        public Geometry GetOrCreateGeometry(int key, Func<Geometry> factory)
        {
            return _geometryCache.GetOrAdd(key, _ =>
            {
                var geometry = factory();
                geometry.Freeze();
                return geometry;
            });
        }

        /// <summary>
        /// 清除几何缓存
        /// </summary>
        public void ClearGeometryCache()
        {
            _geometryCache.Clear();
        }

        /// <summary>
        /// 清除所有缓存
        /// </summary>
        public void ClearAllCaches()
        {
            _brushCache.Clear();
            _penCache.Clear();
            _geometryCache.Clear();
        }

        /// <summary>
        /// 锁定位图进行写入
        /// </summary>
        public void Lock()
        {
            _writeableBitmap?.Lock();
        }

        /// <summary>
        /// 解锁位图
        /// </summary>
        public void Unlock()
        {
            _writeableBitmap?.Unlock();
        }

        /// <summary>
        /// 清空位图
        /// </summary>
        public void Clear(Color? clearColor = null)
        {
            if (_writeableBitmap == null) return;

            var color = clearColor ?? Colors.Transparent;
            var pixelColor = (color.A << 24) | (color.R << 16) | (color.G << 8) | color.B;
            
            int width = _writeableBitmap.PixelWidth;
            int height = _writeableBitmap.PixelHeight;
            int stride = width * 4;
            int bufferSize = height * stride;

            unsafe
            {
                var backBuffer = (int*)_writeableBitmap.BackBuffer;
                for (int i = 0; i < width * height; i++)
                {
                    backBuffer[i] = pixelColor;
                }
            }

            _writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
        }

        public void Dispose()
        {
            if (_disposed) return;

            // WriteableBitmap 不需要 Dispose
            _writeableBitmap = null;
            ClearAllCaches();
            _disposed = true;
        }
    }

    /// <summary>
    /// 渲染选项
    /// </summary>
    public class InkRenderOptions
    {
        /// <summary>
        /// 是否启用双缓冲
        /// </summary>
        public bool EnableDoubleBuffering { get; set; } = true;

        /// <summary>
        /// 是否启用缓存
        /// </summary>
        public bool EnableCaching { get; set; } = true;

        /// <summary>
        /// 缓存过期时间（毫秒）
        /// </summary>
        public int CacheExpirationMs { get; set; } = 5000;

        /// <summary>
        /// 最大缓存条目数
        /// </summary>
        public int MaxCacheEntries { get; set; } = 100;

        /// <summary>
        /// 渲染质量
        /// </summary>
        public InkRenderQuality RenderQuality { get; set; } = InkRenderQuality.High;

        /// <summary>
        /// 是否使用硬件加速
        /// </summary>
        public bool UseHardwareAcceleration { get; set; } = true;
    }

    /// <summary>
    /// 渲染质量枚举
    /// </summary>
    public enum InkRenderQuality
    {
        Low,
        Medium,
        High,
        Ultra
    }
}
