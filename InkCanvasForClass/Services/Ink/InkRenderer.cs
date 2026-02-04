using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Ink_Canvas.Services.Ink
{
    /// <summary>
    /// 高性能墨迹渲染器 - 支持双缓冲和增量渲染
    /// </summary>
    public class InkRenderer : IDisposable
    {
        private readonly object _renderLock = new();
        private WriteableBitmap _frontBuffer;
        private WriteableBitmap _backBuffer;
        private DrawingVisual _drawingVisual;
        private RenderTargetBitmap _renderTarget;
        private bool _isBufferSwapped;
        private bool _disposed;

        /// <summary>
        /// 当前活动缓冲区
        /// </summary>
        public WriteableBitmap ActiveBuffer => _isBufferSwapped ? _backBuffer : _frontBuffer;

        /// <summary>
        /// 后台渲染缓冲区
        /// </summary>
        public WriteableBitmap BackBuffer => _isBufferSwapped ? _frontBuffer : _backBuffer;

        /// <summary>
        /// 渲染尺寸
        /// </summary>
        public Size RenderSize { get; private set; }

        /// <summary>
        /// DPI X
        /// </summary>
        public double DpiX { get; set; } = 96.0;

        /// <summary>
        /// DPI Y
        /// </summary>
        public double DpiY { get; set; } = 96.0;

        /// <summary>
        /// 是否启用双缓冲
        /// </summary>
        public bool EnableDoubleBuffering { get; set; } = true;

        /// <summary>
        /// 初始化渲染器
        /// </summary>
        public void Initialize(int width, int height)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentException("Width and height must be positive");

            lock (_renderLock)
            {
                // 释放旧资源
                _frontBuffer?.Dispose();
                _backBuffer?.Dispose();
                _renderTarget?.Clear();

                // 创建双缓冲区
                _frontBuffer = new WriteableBitmap(width, height, DpiX, DpiY, PixelFormats.Pbgra32, null);
                _backBuffer = new WriteableBitmap(width, height, DpiX, DpiY, PixelFormats.Pbgra32, null);

                // 创建渲染目标
                _renderTarget = new RenderTargetBitmap(width, height, DpiX, DpiY, PixelFormats.Pbgra32);
                _drawingVisual = new DrawingVisual();

                RenderSize = new Size(width, height);
                _isBufferSwapped = false;
            }
        }

        /// <summary>
        /// 渲染笔画集合
        /// </summary>
        public async Task RenderStrokesAsync(
            StrokeCollection strokes,
            Rect renderBounds,
            CancellationToken cancellationToken = default)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(InkRenderer));
            if (_frontBuffer == null) throw new InvalidOperationException("Renderer not initialized");

            await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                lock (_renderLock)
                {
                    // 使用 DrawingVisual 渲染笔画
                    using (var context = _drawingVisual.RenderOpen())
                    {
                        // 清除背景
                        context.DrawRectangle(Brushes.Transparent, null, renderBounds);

                        // 渲染所有笔画
                        foreach (var stroke in strokes)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            stroke.Draw(context);
                        }
                    }

                    // 渲染到 RenderTargetBitmap
                    _renderTarget.Clear();
                    _renderTarget.Render(_drawingVisual);

                    // 复制到后台缓冲区
                    CopyRenderTargetToWriteableBitmap(_renderTarget, BackBuffer);
                }
            }, cancellationToken);
        }

        /// <summary>
        /// 增量渲染 - 只渲染新增笔画
        /// </summary>
        public async Task RenderIncrementalAsync(
            StrokeCollection existingStrokes,
            StrokeCollection newStrokes,
            Rect renderBounds,
            CancellationToken cancellationToken = default)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(InkRenderer));
            if (_frontBuffer == null) throw new InvalidOperationException("Renderer not initialized");

            await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                lock (_renderLock)
                {
                    // 复制前缓冲区到后缓冲区
                    CopyWriteableBitmap(_frontBuffer, _backBuffer);

                    // 只渲染新笔画
                    using (var context = _drawingVisual.RenderOpen())
                    {
                        // 绘制现有内容
                        context.DrawImage(_frontBuffer, renderBounds);

                        // 绘制新笔画
                        foreach (var stroke in newStrokes)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            stroke.Draw(context);
                        }
                    }

                    // 渲染到 RenderTargetBitmap
                    _renderTarget.Clear();
                    _renderTarget.Render(_drawingVisual);

                    // 复制到后台缓冲区
                    CopyRenderTargetToWriteableBitmap(_renderTarget, BackBuffer);
                }
            }, cancellationToken);
        }

        /// <summary>
        /// 交换缓冲区
        /// </summary>
        public WriteableBitmap SwapBuffers()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(InkRenderer));

            lock (_renderLock)
            {
                _isBufferSwapped = !_isBufferSwapped;
                return ActiveBuffer;
            }
        }

        /// <summary>
        /// 渲染到 DrawingContext
        /// </summary>
        public void RenderToDrawingContext(DrawingContext context, Rect bounds)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(InkRenderer));

            lock (_renderLock)
            {
                context.DrawImage(ActiveBuffer, bounds);
            }
        }

        /// <summary>
        /// 清除缓冲区
        /// </summary>
        public void Clear(Color? clearColor = null)
        {
            if (_disposed) return;

            lock (_renderLock)
            {
                ClearWriteableBitmap(_frontBuffer, clearColor);
                ClearWriteableBitmap(_backBuffer, clearColor);
            }
        }

        /// <summary>
        /// 将 RenderTargetBitmap 复制到 WriteableBitmap
        /// </summary>
        private unsafe void CopyRenderTargetToWriteableBitmap(RenderTargetBitmap source, WriteableBitmap destination)
        {
            if (source == null || destination == null) return;

            int width = source.PixelWidth;
            int height = source.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];

            source.CopyPixels(pixels, stride, 0);

            destination.Lock();
            try
            {
                fixed (byte* src = pixels)
                {
                    Buffer.MemoryCopy(
                        src,
                        destination.BackBuffer.ToPointer(),
                        pixels.Length,
                        pixels.Length);
                }
                destination.AddDirtyRect(new Int32Rect(0, 0, width, height));
            }
            finally
            {
                destination.Unlock();
            }
        }

        /// <summary>
        /// 复制 WriteableBitmap
        /// </summary>
        private unsafe void CopyWriteableBitmap(WriteableBitmap source, WriteableBitmap destination)
        {
            if (source == null || destination == null) return;

            int width = source.PixelWidth;
            int height = source.PixelHeight;
            int stride = width * 4;
            int bufferSize = height * stride;

            source.Lock();
            destination.Lock();
            try
            {
                Buffer.MemoryCopy(
                    source.BackBuffer.ToPointer(),
                    destination.BackBuffer.ToPointer(),
                    bufferSize,
                    bufferSize);
                destination.AddDirtyRect(new Int32Rect(0, 0, width, height));
            }
            finally
            {
                source.Unlock();
                destination.Unlock();
            }
        }

        /// <summary>
        /// 清除 WriteableBitmap
        /// </summary>
        private unsafe void ClearWriteableBitmap(WriteableBitmap bitmap, Color? clearColor)
        {
            if (bitmap == null) return;

            var color = clearColor ?? Colors.Transparent;
            int pixelColor = (color.A << 24) | (color.R << 16) | (color.G << 8) | color.B;

            int width = bitmap.PixelWidth;
            int height = bitmap.PixelHeight;

            bitmap.Lock();
            try
            {
                int* buffer = (int*)bitmap.BackBuffer;
                int pixelCount = width * height;

                for (int i = 0; i < pixelCount; i++)
                {
                    buffer[i] = pixelColor;
                }

                bitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
            }
            finally
            {
                bitmap.Unlock();
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            lock (_renderLock)
            {
                _frontBuffer?.Dispose();
                _backBuffer?.Dispose();
                _renderTarget?.Clear();
            }

            _disposed = true;
        }
    }
}
