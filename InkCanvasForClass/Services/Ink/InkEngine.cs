using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Media;
using Microsoft.Extensions.Caching.Memory;
using Ink_Canvas.Helpers;

namespace Ink_Canvas.Services.Ink
{
    /// <summary>
    /// 现代墨迹引擎实现
    /// </summary>
    public class InkEngine : IInkEngine
    {
        private readonly StrokeCollection _strokes;
        private readonly ConcurrentDictionary<Guid, InkStrokeData> _strokeDataMap;
        private readonly Channel<InkOperation> _operationChannel;
        private readonly CancellationTokenSource _processingCts;
        private readonly MemoryCache _recognitionCache;
        private readonly InkRenderContext _renderContext;
        private readonly InkRenderer _renderer;
        private readonly InkRecognitionPipeline _recognitionPipeline;
        private readonly InkRenderOptions _renderOptions;
        private DrawingAttributes _defaultDrawingAttributes;
        private Task _processingTask;
        private bool _disposed;

        /// <inheritdoc />
        public event EventHandler<InkStrokeCollectedEventArgs> StrokeCollected;

        /// <inheritdoc />
        public event EventHandler<InkRecognitionCompletedEventArgs> RecognitionCompleted;

        /// <inheritdoc />
        public event EventHandler<InkRenderCompletedEventArgs> RenderCompleted;

        /// <inheritdoc />
        public StrokeCollection Strokes => _strokes;

        /// <inheritdoc />
        public DrawingAttributes DefaultDrawingAttributes
        {
            get => _defaultDrawingAttributes;
            set => _defaultDrawingAttributes = value?.Clone() ?? new DrawingAttributes();
        }

        /// <inheritdoc />
        public bool IsShapeRecognitionEnabled { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public InkEngine(InkRenderOptions options = null)
        {
            _strokes = new StrokeCollection();
            _strokeDataMap = new ConcurrentDictionary<Guid, InkStrokeData>();
            _operationChannel = Channel.CreateUnbounded<InkOperation>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });
            _processingCts = new CancellationTokenSource();
            _recognitionCache = new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = 50,
                ExpirationScanFrequency = TimeSpan.FromSeconds(30)
            });
            _renderContext = new InkRenderContext();
            _renderer = new InkRenderer();
            _recognitionPipeline = new InkRecognitionPipeline();
            _renderOptions = options ?? new InkRenderOptions();
            _defaultDrawingAttributes = new DrawingAttributes();

            // 启动处理任务
            _processingTask = ProcessOperationsAsync(_processingCts.Token);
            _recognitionPipeline.Start();
        }

        /// <summary>
        /// 初始化渲染器
        /// </summary>
        public void InitializeRenderer(int width, int height)
        {
            _renderer.Initialize(width, height);
        }

        /// <inheritdoc />
        public async Task AddStrokeAsync(InkStrokeData strokeData, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            using var timer = InkPerformanceMonitor.Instance.BeginOperation("StrokeAdd");

            var operation = new InkOperation
            {
                Type = InkOperationType.Add,
                StrokeData = strokeData,
                CompletionSource = new TaskCompletionSource()
            };

            await _operationChannel.Writer.WriteAsync(operation, cancellationToken);
            await operation.CompletionSource.Task.WaitAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task AddStrokesAsync(IEnumerable<InkStrokeData> strokeDataCollection, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            var strokes = strokeDataCollection?.ToArray() ?? Array.Empty<InkStrokeData>();
            if (strokes.Length == 0) return;

            var operation = new InkOperation
            {
                Type = InkOperationType.AddBatch,
                StrokeDataCollection = strokes,
                CompletionSource = new TaskCompletionSource()
            };

            await _operationChannel.Writer.WriteAsync(operation, cancellationToken);
            await operation.CompletionSource.Task.WaitAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task RemoveStrokeAsync(InkStrokeData strokeData, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            var operation = new InkOperation
            {
                Type = InkOperationType.Remove,
                StrokeData = strokeData,
                CompletionSource = new TaskCompletionSource()
            };

            await _operationChannel.Writer.WriteAsync(operation, cancellationToken);
            await operation.CompletionSource.Task.WaitAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task ClearAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            var operation = new InkOperation
            {
                Type = InkOperationType.Clear,
                CompletionSource = new TaskCompletionSource()
            };

            await _operationChannel.Writer.WriteAsync(operation, cancellationToken);
            await operation.CompletionSource.Task.WaitAsync(cancellationToken);
        }

        /// <inheritdoc />
        public InkRenderContext GetRenderContext()
        {
            return _renderContext;
        }

        /// <inheritdoc />
        public async Task RenderAsync(DrawingContext drawingContext, Rect bounds, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            using var timer = InkPerformanceMonitor.Instance.BeginOperation("Render");

            // 使用双缓冲渲染器
            await _renderer.RenderStrokesAsync(_strokes, bounds, cancellationToken);
            
            // 交换缓冲区并渲染到目标
            _renderer.SwapBuffers();
            _renderer.RenderToDrawingContext(drawingContext, bounds);

            RenderCompleted?.Invoke(this, new InkRenderCompletedEventArgs(
                bounds,
                _strokes.Count,
                TimeSpan.Zero));
        }

        /// <summary>
        /// 增量渲染 - 只渲染新增笔画
        /// </summary>
        public async Task RenderIncrementalAsync(
            StrokeCollection newStrokes,
            Rect bounds,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            var stopwatch = Stopwatch.StartNew();

            // 使用增量渲染
            await _renderer.RenderIncrementalAsync(_strokes, newStrokes, bounds, cancellationToken);
            
            // 交换缓冲区
            _renderer.SwapBuffers();

            stopwatch.Stop();

            RenderCompleted?.Invoke(this, new InkRenderCompletedEventArgs(
                bounds,
                newStrokes.Count,
                stopwatch.Elapsed));
        }

        /// <inheritdoc />
        public IEnumerable<InkStrokeData> HitTest(Point point, double tolerance = 1.0)
        {
            var rect = new Rect(
                point.X - tolerance,
                point.Y - tolerance,
                tolerance * 2,
                tolerance * 2);

            return HitTest(rect);
        }

        /// <inheritdoc />
        public IEnumerable<InkStrokeData> HitTest(Rect rect)
        {
            var hitStrokes = _strokes.HitTest(rect, 50);
            
            foreach (var stroke in hitStrokes)
            {
                // 查找对应的 InkStrokeData
                var strokeData = _strokeDataMap.Values.FirstOrDefault(
                    sd => sd.ToWpfStroke().GetGeometry().Equals(stroke.GetGeometry()));
                
                if (strokeData.Id != Guid.Empty)
                {
                    yield return strokeData;
                }
            }
        }

        /// <inheritdoc />
        public Rect GetStrokesBounds()
        {
            if (_strokes.Count == 0)
                return Rect.Empty;

            return _strokes.GetBounds();
        }

        /// <inheritdoc />
        public Task PreloadAsync(CancellationToken cancellationToken = default)
        {
            // 预加载识别模型等初始化工作
            return Task.CompletedTask;
        }

        /// <summary>
        /// 处理操作队列
        /// </summary>
        private async Task ProcessOperationsAsync(CancellationToken cancellationToken)
        {
            await foreach (var operation in _operationChannel.Reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    switch (operation.Type)
                    {
                        case InkOperationType.Add:
                            ProcessAddOperation(operation);
                            break;
                        case InkOperationType.AddBatch:
                            ProcessAddBatchOperation(operation);
                            break;
                        case InkOperationType.Remove:
                            ProcessRemoveOperation(operation);
                            break;
                        case InkOperationType.Clear:
                            ProcessClearOperation(operation);
                            break;
                    }

                    operation.CompletionSource.SetResult();
                }
                catch (Exception ex)
                {
                    operation.CompletionSource.SetException(ex);
                }
            }
        }

        private void ProcessAddOperation(InkOperation operation)
        {
            var strokeData = operation.StrokeData;
            
            // 转换为 WPF Stroke 并添加
            var wpfStroke = strokeData.ToWpfStroke();
            _strokes.Add(wpfStroke);
            _strokeDataMap[strokeData.Id] = strokeData;

            // 触发事件
            StrokeCollected?.Invoke(this, new InkStrokeCollectedEventArgs(strokeData));

            // 如果启用形状识别，异步进行识别
            if (IsShapeRecognitionEnabled)
            {
                _ = Task.Run(async () => await RecognizeShapeAsync(strokeData));
            }
        }

        private void ProcessAddBatchOperation(InkOperation operation)
        {
            var strokeDataCollection = operation.StrokeDataCollection;
            var wpfStrokes = new StrokeCollection();

            foreach (var strokeData in strokeDataCollection)
            {
                var wpfStroke = strokeData.ToWpfStroke();
                wpfStrokes.Add(wpfStroke);
                _strokeDataMap[strokeData.Id] = strokeData;
            }

            _strokes.Add(wpfStrokes);
        }

        private void ProcessRemoveOperation(InkOperation operation)
        {
            var strokeData = operation.StrokeData;
            
            // 查找并移除对应的 WPF Stroke
            var strokeToRemove = _strokes.FirstOrDefault(s => 
                s.StylusPoints.Count == strokeData.Points.Count &&
                s.StylusPoints[0].X == strokeData.Points[0].X);

            if (strokeToRemove != null)
            {
                _strokes.Remove(strokeToRemove);
            }

            _strokeDataMap.TryRemove(strokeData.Id, out _);
        }

        private void ProcessClearOperation(InkOperation operation)
        {
            _strokes.Clear();
            _strokeDataMap.Clear();
            _recognitionCache.Clear();
            _renderContext.ClearAllCaches();
        }

        /// <summary>
        /// 异步形状识别 - 使用识别管道
        /// </summary>
        private async Task RecognizeShapeAsync(InkStrokeData strokeData)
        {
            using var timer = InkPerformanceMonitor.Instance.BeginOperation("ShapeRecognition");

            try
            {
                // 创建临时笔画集合
                var tempStrokes = new StrokeCollection { strokeData.ToWpfStroke() };

                // 使用识别管道
                var result = await _recognitionPipeline.SubmitAsync(tempStrokes);

                RecognitionCompleted?.Invoke(this, new InkRecognitionCompletedEventArgs(
                    strokeData, result, TimeSpan.Zero));
            }
            catch (Exception ex)
            {
                var result = InkRecognitionResult.Failed(ex.Message);
                RecognitionCompleted?.Invoke(this, new InkRecognitionCompletedEventArgs(
                    strokeData, result, TimeSpan.Zero));
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(InkEngine));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed) return;

            _processingCts.Cancel();
            _operationChannel.Writer.Complete();
            
            try
            {
                _processingTask?.Wait(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                LogHelper.NewLog(ex);
            }

            _processingCts.Dispose();
            _recognitionCache.Dispose();
            _renderContext.Dispose();
            _renderer.Dispose();
            _recognitionPipeline.Dispose();

            _disposed = true;
        }
    }

    /// <summary>
    /// 墨迹操作类型
    /// </summary>
    internal enum InkOperationType
    {
        Add,
        AddBatch,
        Remove,
        Clear
    }

    /// <summary>
    /// 墨迹操作
    /// </summary>
    internal class InkOperation
    {
        public InkOperationType Type { get; set; }
        public InkStrokeData StrokeData { get; set; }
        public InkStrokeData[] StrokeDataCollection { get; set; }
        public TaskCompletionSource CompletionSource { get; set; }
    }
}
