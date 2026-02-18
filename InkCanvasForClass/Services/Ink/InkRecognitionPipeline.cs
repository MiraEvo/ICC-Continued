using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Ink;
using Microsoft.Extensions.Caching.Memory;
using Windows.UI.Input.Inking;
using Windows.UI.Input.Inking.Analysis;

using Ink_Canvas.Helpers;

namespace Ink_Canvas.Services.Ink
{
    /// <summary>
    /// 形状识别管道 - 使用生产者-消费者模式处理识别任务
    /// </summary>
    public class InkRecognitionPipeline : IDisposable
    {
        private readonly Channel<RecognitionTask> _taskChannel;
        private readonly MemoryCache _resultCache;
        private readonly CancellationTokenSource _cts;
        private Task _processingTask;
        private bool _disposed;

        /// <summary>
        /// 识别完成事件
        /// </summary>
        public event EventHandler<RecognitionResultEventArgs> RecognitionCompleted;

        /// <summary>
        /// 识别选项
        /// </summary>
        public InkRecognitionOptions Options { get; set; } = new();

        /// <summary>
        /// 是否正在运行
        /// </summary>
        public bool IsRunning => _processingTask != null && !_processingTask.IsCompleted;

        public InkRecognitionPipeline()
        {
            _taskChannel = Channel.CreateUnbounded<RecognitionTask>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

            _resultCache = new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = 100,
                ExpirationScanFrequency = TimeSpan.FromSeconds(30)
            });

            _cts = new CancellationTokenSource();
        }

        /// <summary>
        /// 启动识别管道
        /// </summary>
        public void Start()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(InkRecognitionPipeline));
            if (IsRunning) return;

            _processingTask = ProcessRecognitionTasksAsync(_cts.Token);
        }

        /// <summary>
        /// 提交识别任务
        /// </summary>
        public async Task<InkRecognitionResult> SubmitAsync(
            StrokeCollection strokes,
            CancellationToken cancellationToken = default)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(InkRecognitionPipeline));
            if (!IsRunning) Start();

            // 检查缓存
            var cacheKey = ComputeStrokesHash(strokes);
            if (_resultCache.TryGetValue<InkRecognitionResult>(cacheKey, out var cachedResult))
            {
                return cachedResult;
            }

            var task = new RecognitionTask
            {
                Strokes = strokes,
                CacheKey = cacheKey,
                CompletionSource = new TaskCompletionSource<InkRecognitionResult>()
            };

            await _taskChannel.Writer.WriteAsync(task, cancellationToken);
            return await task.CompletionSource.Task.WaitAsync(cancellationToken);
        }

        /// <summary>
        /// 批量提交识别任务
        /// </summary>
        public async Task<IReadOnlyList<InkRecognitionResult>> SubmitBatchAsync(
            IEnumerable<StrokeCollection> strokeCollections,
            CancellationToken cancellationToken = default)
        {
            var tasks = strokeCollections.Select(strokes => SubmitAsync(strokes, cancellationToken));
            var results = await Task.WhenAll(tasks);
            return results;
        }

        /// <summary>
        /// 清除缓存
        /// </summary>
        public void ClearCache()
        {
            _resultCache.Clear();
        }

        /// <summary>
        /// 停止管道
        /// </summary>
        public async Task StopAsync()
        {
            if (!IsRunning) return;

            _taskChannel.Writer.Complete();
            _cts.Cancel();

            try
            {
                await _processingTask.WaitAsync(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                LogHelper.NewLog(ex);
            }
        }

        /// <summary>
        /// 处理识别任务
        /// </summary>
        private async Task ProcessRecognitionTasksAsync(CancellationToken cancellationToken)
        {
            await foreach (var task in _taskChannel.Reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    var result = await RecognizeAsync(task.Strokes, cancellationToken);
                    
                    // 缓存结果
                    if (result.IsSuccessful)
                    {
                        var cacheOptions = new MemoryCacheEntryOptions()
                            .SetSize(1)
                            .SetAbsoluteExpiration(TimeSpan.FromSeconds(Options.CacheExpirationSeconds));
                        _resultCache.Set(task.CacheKey, result, cacheOptions);
                    }

                    task.CompletionSource.SetResult(result);
                    RecognitionCompleted?.Invoke(this, new RecognitionResultEventArgs(task.Strokes, result));
                }
                catch (Exception ex)
                {
                    var failedResult = InkRecognitionResult.Failed(ex.Message);
                    task.CompletionSource.SetResult(failedResult);
                    RecognitionCompleted?.Invoke(this, new RecognitionResultEventArgs(task.Strokes, failedResult));
                }
            }
        }

        /// <summary>
        /// 执行形状识别
        /// </summary>
        private async Task<InkRecognitionResult> RecognizeAsync(
            StrokeCollection strokes,
            CancellationToken cancellationToken)
        {
            if (strokes == null || strokes.Count == 0)
                return InkRecognitionResult.Failed("No strokes to recognize");

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // 检查最小尺寸
                var bounds = strokes.GetBounds();
                if (bounds.Width < Options.MinimumShapeSize && bounds.Height < Options.MinimumShapeSize)
                {
                    return InkRecognitionResult.Failed("Stroke size too small");
                }

                // 转换为 UWP InkStroke
                var uwpStrokes = await ConvertToUwpStrokesAsync(strokes, cancellationToken);
                if (uwpStrokes.Count == 0)
                {
                    return InkRecognitionResult.Failed("Failed to convert strokes");
                }

                // 使用 InkAnalyzer 进行识别
                var analyzer = new InkAnalyzer();
                var container = new InkStrokeContainer();

                foreach (var stroke in uwpStrokes)
                {
                    container.AddStroke(stroke);
                }

                var allStrokes = container.GetStrokes();
                analyzer.AddDataForStrokes(allStrokes);

                foreach (var stroke in allStrokes)
                {
                    analyzer.SetStrokeDataKind(stroke.Id, InkAnalysisStrokeKind.Drawing);
                }

                var analysisResult = await analyzer.AnalyzeAsync().AsTask(cancellationToken);

                if (analysisResult.Status != InkAnalysisStatus.Updated || analyzer.AnalysisRoot == null)
                {
                    return InkRecognitionResult.Failed("Analysis failed or no result");
                }

                var drawingNodes = analyzer.AnalysisRoot.FindNodes(InkAnalysisNodeKind.InkDrawing);
                if (drawingNodes == null || drawingNodes.Count == 0)
                {
                    return InkRecognitionResult.Failed("No drawing nodes found");
                }

                // 找到最佳匹配
                InkAnalysisInkDrawing bestDrawing = null;
                double bestScore = 0;

                foreach (var node in drawingNodes)
                {
                    if (node is not InkAnalysisInkDrawing drawing) continue;
                    if (!IsSupportedShape(drawing.DrawingKind)) continue;

                    double score = CalculateConfidenceScore(drawing, strokes);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestDrawing = drawing;
                    }
                }

                if (bestDrawing == null)
                {
                    return InkRecognitionResult.Failed("No supported shape recognized");
                }

                if (bestScore < Options.ConfidenceThreshold)
                {
                    return InkRecognitionResult.Failed("Confidence below threshold");
                }

                // 验证几何属性
                if (Options.EnableGeometryValidation && !ValidateGeometry(bestDrawing))
                {
                    return InkRecognitionResult.Failed("Geometry validation failed");
                }

                stopwatch.Stop();

                // 转换结果
                var shapeType = ConvertDrawingKindToShapeType(bestDrawing.DrawingKind);
                var hotPoints = bestDrawing.Points?.Select(p => new Point(p.X, p.Y)).ToArray() ?? Array.Empty<Point>();
                var boundingRect = new Rect(
                    bestDrawing.BoundingRect.X,
                    bestDrawing.BoundingRect.Y,
                    bestDrawing.BoundingRect.Width,
                    bestDrawing.BoundingRect.Height);

                return InkRecognitionResult.Success(
                    shapeType,
                    bestScore,
                    hotPoints,
                    boundingRect);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return InkRecognitionResult.Failed($"Recognition error: {ex.Message}");
            }
        }

        /// <summary>
        /// 异步转换 WPF Stroke 到 UWP InkStroke
        /// </summary>
        private async Task<List<InkStroke>> ConvertToUwpStrokesAsync(
            StrokeCollection strokes,
            CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                var result = new List<InkStroke>();
                var builder = new InkStrokeBuilder();

                foreach (var stroke in strokes)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var inkPoints = stroke.StylusPoints
                        .Select(sp => new Windows.UI.Input.Inking.InkPoint(
                            new Windows.Foundation.Point(sp.X, sp.Y), sp.PressureFactor))
                        .ToList();

                    if (inkPoints.Count >= 2)
                    {
                        var uwpStroke = builder.CreateStrokeFromInkPoints(
                            inkPoints,
                            System.Numerics.Matrix3x2.Identity);
                        result.Add(uwpStroke);
                    }
                }

                return result;
            }, cancellationToken);
        }

        /// <summary>
        /// 计算置信度分数
        /// </summary>
        private double CalculateConfidenceScore(InkAnalysisInkDrawing drawing, StrokeCollection strokes)
        {
            double score = 0.9; // 基础分数

            // 根据点的完整性调整
            int expectedPoints = GetExpectedPointCount(drawing.DrawingKind);
            int actualPoints = drawing.Points?.Count ?? 0;
            
            if (actualPoints >= expectedPoints)
            {
                score = 0.95;
            }
            else
            {
                score = 0.5 + (0.45 * actualPoints / expectedPoints);
            }

            return Math.Min(1.0, score);
        }

        /// <summary>
        /// 验证几何属性
        /// </summary>
        private bool ValidateGeometry(InkAnalysisInkDrawing drawing)
        {
            return drawing.DrawingKind switch
            {
                InkAnalysisDrawingKind.Circle => ValidateCircle(drawing),
                InkAnalysisDrawingKind.Ellipse => ValidateEllipse(drawing),
                InkAnalysisDrawingKind.Triangle => ValidateTriangle(drawing),
                InkAnalysisDrawingKind.Rectangle or
                InkAnalysisDrawingKind.Square => ValidateRectangle(drawing),
                _ => true
            };
        }

        private bool ValidateCircle(InkAnalysisInkDrawing drawing)
        {
            var bounds = drawing.BoundingRect;
            if (bounds.Width <= 0 || bounds.Height <= 0) return false;

            double aspectRatio = Math.Min(bounds.Width, bounds.Height) / Math.Max(bounds.Width, bounds.Height);
            return aspectRatio >= 0.7; // 允许一定的椭圆度
        }

        private bool ValidateEllipse(InkAnalysisInkDrawing drawing)
        {
            var bounds = drawing.BoundingRect;
            if (bounds.Width <= 0 || bounds.Height <= 0) return false;

            double aspectRatio = Math.Min(bounds.Width, bounds.Height) / Math.Max(bounds.Width, bounds.Height);
            return aspectRatio >= 0.2 && aspectRatio <= 1.0;
        }

        private bool ValidateTriangle(InkAnalysisInkDrawing drawing)
        {
            return drawing.Points?.Count >= 3;
        }

        private bool ValidateRectangle(InkAnalysisInkDrawing drawing)
        {
            return drawing.Points?.Count >= 4;
        }

        /// <summary>
        /// 检查是否支持的形状类型
        /// </summary>
        private bool IsSupportedShape(InkAnalysisDrawingKind kind)
        {
            return kind switch
            {
                InkAnalysisDrawingKind.Circle or
                InkAnalysisDrawingKind.Ellipse or
                InkAnalysisDrawingKind.Triangle or
                InkAnalysisDrawingKind.Rectangle or
                InkAnalysisDrawingKind.Square or
                InkAnalysisDrawingKind.Diamond or
                InkAnalysisDrawingKind.Parallelogram or
                InkAnalysisDrawingKind.Trapezoid => true,
                _ => false
            };
        }

        /// <summary>
        /// 转换 DrawingKind 到 ShapeType
        /// </summary>
        private InkShapeType ConvertDrawingKindToShapeType(InkAnalysisDrawingKind kind)
        {
            return kind switch
            {
                InkAnalysisDrawingKind.Circle => InkShapeType.Circle,
                InkAnalysisDrawingKind.Ellipse => InkShapeType.Ellipse,
                InkAnalysisDrawingKind.Triangle => InkShapeType.Triangle,
                InkAnalysisDrawingKind.Rectangle => InkShapeType.Rectangle,
                InkAnalysisDrawingKind.Square => InkShapeType.Square,
                InkAnalysisDrawingKind.Diamond => InkShapeType.Diamond,
                InkAnalysisDrawingKind.Parallelogram => InkShapeType.Parallelogram,
                InkAnalysisDrawingKind.Trapezoid => InkShapeType.Trapezoid,
                _ => InkShapeType.None
            };
        }

        /// <summary>
        /// 获取期望的点数
        /// </summary>
        private int GetExpectedPointCount(InkAnalysisDrawingKind kind)
        {
            return kind switch
            {
                InkAnalysisDrawingKind.Circle or
                InkAnalysisDrawingKind.Ellipse => 4,
                InkAnalysisDrawingKind.Triangle => 3,
                InkAnalysisDrawingKind.Rectangle or
                InkAnalysisDrawingKind.Square or
                InkAnalysisDrawingKind.Diamond or
                InkAnalysisDrawingKind.Parallelogram or
                InkAnalysisDrawingKind.Trapezoid => 4,
                _ => 4
            };
        }

        /// <summary>
        /// 计算笔画集合的哈希值
        /// </summary>
        private string ComputeStrokesHash(StrokeCollection strokes)
        {
            if (strokes == null || strokes.Count == 0) return string.Empty;

            unchecked
            {
                int hash = 17;
                foreach (var stroke in strokes)
                {
                    hash = hash * 31 + stroke.StylusPoints.Count;
                    if (stroke.StylusPoints.Count > 0)
                    {
                        var first = stroke.StylusPoints[0];
                        var last = stroke.StylusPoints[stroke.StylusPoints.Count - 1];
                        hash = hash * 31 + (int)(first.X * 100);
                        hash = hash * 31 + (int)(first.Y * 100);
                        hash = hash * 31 + (int)(last.X * 100);
                        hash = hash * 31 + (int)(last.Y * 100);
                    }
                }
                return hash.ToString("X8");
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _ = StopAsync();
            _cts.Dispose();
            _resultCache.Dispose();

            _disposed = true;
        }
    }

    /// <summary>
    /// 识别任务
    /// </summary>
    internal class RecognitionTask
    {
        public StrokeCollection Strokes { get; set; }
        public string CacheKey { get; set; }
        public TaskCompletionSource<InkRecognitionResult> CompletionSource { get; set; }
    }

    /// <summary>
    /// 识别结果事件参数
    /// </summary>
    public class RecognitionResultEventArgs : EventArgs
    {
        public StrokeCollection Strokes { get; }
        public InkRecognitionResult Result { get; }

        public RecognitionResultEventArgs(StrokeCollection strokes, InkRecognitionResult result)
        {
            Strokes = strokes;
            Result = result;
        }
    }

    /// <summary>
    /// 识别选项
    /// </summary>
    public class InkRecognitionOptions
    {
        /// <summary>
        /// 置信度阈值
        /// </summary>
        public double ConfidenceThreshold { get; set; } = 0.6;

        /// <summary>
        /// 最小形状尺寸
        /// </summary>
        public double MinimumShapeSize { get; set; } = 50;

        /// <summary>
        /// 是否启用几何验证
        /// </summary>
        public bool EnableGeometryValidation { get; set; } = true;

        /// <summary>
        /// 缓存过期时间（秒）
        /// </summary>
        public int CacheExpirationSeconds { get; set; } = 30;

        /// <summary>
        /// 是否启用多边形识别
        /// </summary>
        public bool EnablePolygonRecognition { get; set; } = true;
    }
}
