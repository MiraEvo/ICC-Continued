using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Media;

namespace Ink_Canvas.Services.Ink
{
    /// <summary>
    /// 墨迹引擎接口 - 现代化的墨迹处理核心
    /// </summary>
    public interface IInkEngine : IDisposable
    {
        /// <summary>
        /// 笔画收集事件
        /// </summary>
        event EventHandler<InkStrokeCollectedEventArgs> StrokeCollected;

        /// <summary>
        /// 笔画识别完成事件
        /// </summary>
        event EventHandler<InkRecognitionCompletedEventArgs> RecognitionCompleted;

        /// <summary>
        /// 渲染完成事件
        /// </summary>
        event EventHandler<InkRenderCompletedEventArgs> RenderCompleted;

        /// <summary>
        /// 当前笔画集合
        /// </summary>
        StrokeCollection Strokes { get; }

        /// <summary>
        /// 默认绘制属性
        /// </summary>
        DrawingAttributes DefaultDrawingAttributes { get; set; }

        /// <summary>
        /// 是否启用形状识别
        /// </summary>
        bool IsShapeRecognitionEnabled { get; set; }

        /// <summary>
        /// 异步添加笔画
        /// </summary>
        Task AddStrokeAsync(InkStrokeData strokeData, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量添加笔画
        /// </summary>
        Task AddStrokesAsync(IEnumerable<InkStrokeData> strokeDataCollection, CancellationToken cancellationToken = default);

        /// <summary>
        /// 移除笔画
        /// </summary>
        Task RemoveStrokeAsync(InkStrokeData strokeData, CancellationToken cancellationToken = default);

        /// <summary>
        /// 清除所有笔画
        /// </summary>
        Task ClearAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取渲染上下文
        /// </summary>
        InkRenderContext GetRenderContext();

        /// <summary>
        /// 异步渲染到目标
        /// </summary>
        Task RenderAsync(DrawingContext drawingContext, Rect bounds, CancellationToken cancellationToken = default);

        /// <summary>
        /// 命中测试
        /// </summary>
        IEnumerable<InkStrokeData> HitTest(Point point, double tolerance = 1.0);

        /// <summary>
        /// 命中测试（区域）
        /// </summary>
        IEnumerable<InkStrokeData> HitTest(Rect rect);

        /// <summary>
        /// 获取笔画边界
        /// </summary>
        Rect GetStrokesBounds();

        /// <summary>
        /// 预加载识别模型
        /// </summary>
        Task PreloadAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 笔画收集事件参数
    /// </summary>
    public class InkStrokeCollectedEventArgs : EventArgs
    {
        public InkStrokeData StrokeData { get; }
        public DateTimeOffset Timestamp { get; }

        public InkStrokeCollectedEventArgs(InkStrokeData strokeData)
        {
            StrokeData = strokeData ?? throw new ArgumentNullException(nameof(strokeData));
            Timestamp = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// 识别完成事件参数
    /// </summary>
    public class InkRecognitionCompletedEventArgs : EventArgs
    {
        public InkStrokeData OriginalStroke { get; }
        public InkRecognitionResult RecognitionResult { get; }
        public TimeSpan ProcessingTime { get; }

        public InkRecognitionCompletedEventArgs(
            InkStrokeData originalStroke,
            InkRecognitionResult recognitionResult,
            TimeSpan processingTime)
        {
            OriginalStroke = originalStroke;
            RecognitionResult = recognitionResult;
            ProcessingTime = processingTime;
        }
    }

    /// <summary>
    /// 渲染完成事件参数
    /// </summary>
    public class InkRenderCompletedEventArgs : EventArgs
    {
        public Rect RenderBounds { get; }
        public int StrokeCount { get; }
        public TimeSpan RenderTime { get; }

        public InkRenderCompletedEventArgs(Rect renderBounds, int strokeCount, TimeSpan renderTime)
        {
            RenderBounds = renderBounds;
            StrokeCount = strokeCount;
            RenderTime = renderTime;
        }
    }
}
