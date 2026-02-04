using Ink_Canvas.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Ink_Canvas.Services.Ink
{
    /// <summary>
    /// 墨迹性能监控器 - 用于跟踪和报告性能指标
    /// </summary>
    public class InkPerformanceMonitor
    {
        private readonly ConcurrentDictionary<string, OperationMetrics> _metrics = new();
        private readonly ConcurrentQueue<PerformanceEvent> _eventLog = new();
        private readonly int _maxEventLogSize = 1000;
        private long _totalStrokeCount;
        private long _totalRenderCount;

        /// <summary>
        /// 单例实例
        /// </summary>
        public static InkPerformanceMonitor Instance { get; } = new();

        private InkPerformanceMonitor() { }

        /// <summary>
        /// 总笔画数
        /// </summary>
        public long TotalStrokeCount => Interlocked.Read(ref _totalStrokeCount);

        /// <summary>
        /// 总渲染次数
        /// </summary>
        public long TotalRenderCount => Interlocked.Read(ref _totalRenderCount);

        /// <summary>
        /// 开始测量操作
        /// </summary>
        public OperationTimer BeginOperation(string operationName)
        {
            return new OperationTimer(this, operationName);
        }

        /// <summary>
        /// 记录操作完成
        /// </summary>
        internal void RecordOperation(string operationName, TimeSpan duration, bool success = true)
        {
            var metrics = _metrics.GetOrAdd(operationName, _ => new OperationMetrics(operationName));
            metrics.Record(duration, success);

            // 记录事件
            LogEvent(operationName, duration, success);

            // 更新计数器
            switch (operationName)
            {
                case "StrokeCollected":
                    Interlocked.Increment(ref _totalStrokeCount);
                    break;
                case "Render":
                    Interlocked.Increment(ref _totalRenderCount);
                    break;
            }
        }

        /// <summary>
        /// 记录事件
        /// </summary>
        private void LogEvent(string operationName, TimeSpan duration, bool success)
        {
            var evt = new PerformanceEvent
            {
                Timestamp = DateTime.UtcNow,
                OperationName = operationName,
                Duration = duration,
                Success = success
            };

            _eventLog.Enqueue(evt);

            // 限制日志大小
            while (_eventLog.Count > _maxEventLogSize && _eventLog.TryDequeue(out _)) { }
        }

        /// <summary>
        /// 获取操作指标报告
        /// </summary>
        public IReadOnlyList<OperationMetricsReport> GetMetricsReport()
        {
            return _metrics.Values
                .Select(m => m.ToReport())
                .OrderBy(r => r.OperationName)
                .ToList();
        }

        /// <summary>
        /// 获取最近的事件日志
        /// </summary>
        public IReadOnlyList<PerformanceEvent> GetRecentEvents(int count = 100)
        {
            return _eventLog.TakeLast(count).ToList();
        }

        /// <summary>
        /// 获取性能摘要
        /// </summary>
        public PerformanceSummary GetSummary()
        {
            var reports = GetMetricsReport();
            
            return new PerformanceSummary
            {
                TotalStrokeCount = TotalStrokeCount,
                TotalRenderCount = TotalRenderCount,
                AverageRenderTime = reports.FirstOrDefault(r => r.OperationName == "Render")?.AverageDuration ?? TimeSpan.Zero,
                AverageRecognitionTime = reports.FirstOrDefault(r => r.OperationName == "ShapeRecognition")?.AverageDuration ?? TimeSpan.Zero,
                TotalOperations = reports.Sum(r => r.TotalCount),
                FailedOperations = reports.Sum(r => r.FailedCount),
                Metrics = reports
            };
        }

        /// <summary>
        /// 清除所有指标
        /// </summary>
        public void Clear()
        {
            _metrics.Clear();
            _eventLog.Clear();
            Interlocked.Exchange(ref _totalStrokeCount, 0);
            Interlocked.Exchange(ref _totalRenderCount, 0);
        }

        /// <summary>
        /// 打印性能报告到日志
        /// </summary>
        public void LogPerformanceReport()
        {
            var summary = GetSummary();

            LogHelper.WriteLogToFile("=== 墨迹性能报告 ===", LogHelper.LogType.Info);
            LogHelper.WriteLogToFile($"总笔画数: {summary.TotalStrokeCount}", LogHelper.LogType.Info);
            LogHelper.WriteLogToFile($"总渲染次数: {summary.TotalRenderCount}", LogHelper.LogType.Info);
            LogHelper.WriteLogToFile($"平均渲染时间: {summary.AverageRenderTime.TotalMilliseconds:F2}ms", LogHelper.LogType.Info);
            LogHelper.WriteLogToFile($"平均识别时间: {summary.AverageRecognitionTime.TotalMilliseconds:F2}ms", LogHelper.LogType.Info);
            LogHelper.WriteLogToFile($"总操作数: {summary.TotalOperations}", LogHelper.LogType.Info);
            LogHelper.WriteLogToFile($"失败操作数: {summary.FailedOperations}", LogHelper.LogType.Info);

            foreach (var metric in summary.Metrics)
            {
                LogHelper.WriteLogToFile(
                    $"[{metric.OperationName}] 平均: {metric.AverageDuration.TotalMilliseconds:F2}ms, " +
                    $"最小: {metric.MinDuration.TotalMilliseconds:F2}ms, " +
                    $"最大: {metric.MaxDuration.TotalMilliseconds:F2}ms, " +
                    $"次数: {metric.TotalCount}",
                    LogHelper.LogType.Info);
            }
        }
    }

    /// <summary>
    /// 操作计时器
    /// </summary>
    public struct OperationTimer : IDisposable
    {
        private readonly InkPerformanceMonitor _monitor;
        private readonly string _operationName;
        private readonly Stopwatch _stopwatch;
        private bool _disposed;

        public OperationTimer(InkPerformanceMonitor monitor, string operationName)
        {
            _monitor = monitor;
            _operationName = operationName;
            _stopwatch = Stopwatch.StartNew();
            _disposed = false;
        }

        public void Dispose()
        {
            if (_disposed) return;
            
            _stopwatch.Stop();
            _monitor.RecordOperation(_operationName, _stopwatch.Elapsed);
            _disposed = true;
        }
    }

    /// <summary>
    /// 操作指标
    /// </summary>
    internal class OperationMetrics
    {
        private long _totalCount;
        private long _failedCount;
        private double _totalMilliseconds;
        private double _minMilliseconds = double.MaxValue;
        private double _maxMilliseconds;
        private readonly object _lock = new();

        public string OperationName { get; }

        public OperationMetrics(string operationName)
        {
            OperationName = operationName;
        }

        public void Record(TimeSpan duration, bool success)
        {
            var ms = duration.TotalMilliseconds;

            lock (_lock)
            {
                _totalCount++;
                _totalMilliseconds += ms;
                
                if (!success)
                    _failedCount++;

                if (ms < _minMilliseconds)
                    _minMilliseconds = ms;

                if (ms > _maxMilliseconds)
                    _maxMilliseconds = ms;
            }
        }

        public OperationMetricsReport ToReport()
        {
            lock (_lock)
            {
                return new OperationMetricsReport
                {
                    OperationName = OperationName,
                    TotalCount = _totalCount,
                    FailedCount = _failedCount,
                    AverageDuration = TimeSpan.FromMilliseconds(_totalCount > 0 ? _totalMilliseconds / _totalCount : 0),
                    MinDuration = TimeSpan.FromMilliseconds(_minMilliseconds == double.MaxValue ? 0 : _minMilliseconds),
                    MaxDuration = TimeSpan.FromMilliseconds(_maxMilliseconds)
                };
            }
        }
    }

    /// <summary>
    /// 操作指标报告
    /// </summary>
    public class OperationMetricsReport
    {
        public string OperationName { get; set; }
        public long TotalCount { get; set; }
        public long FailedCount { get; set; }
        public TimeSpan AverageDuration { get; set; }
        public TimeSpan MinDuration { get; set; }
        public TimeSpan MaxDuration { get; set; }
    }

    /// <summary>
    /// 性能事件
    /// </summary>
    public class PerformanceEvent
    {
        public DateTime Timestamp { get; set; }
        public string OperationName { get; set; }
        public TimeSpan Duration { get; set; }
        public bool Success { get; set; }
    }

    /// <summary>
    /// 性能摘要
    /// </summary>
    public class PerformanceSummary
    {
        public long TotalStrokeCount { get; set; }
        public long TotalRenderCount { get; set; }
        public TimeSpan AverageRenderTime { get; set; }
        public TimeSpan AverageRecognitionTime { get; set; }
        public long TotalOperations { get; set; }
        public long FailedOperations { get; set; }
        public IReadOnlyList<OperationMetricsReport> Metrics { get; set; }
    }
}
