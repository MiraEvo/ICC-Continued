using System;
using System.Collections.Generic;
using System.Linq;

namespace Ink_Canvas.Services.PalmEraser
{
    /// <summary>
    /// 手掌检测器 - 使用多特征融合算法检测手掌触摸
    /// </summary>
    public class PalmEraserDetector
    {
        private readonly PalmEraserDetectorConfig _config;
        private readonly AdaptiveThresholdManager _adaptiveThreshold;
        private readonly CircularBuffer<TouchCharacteristics> _recentTouches;

        public PalmEraserDetector(PalmEraserDetectorConfig config = null)
        {
            _config = config ?? new PalmEraserDetectorConfig();
            _adaptiveThreshold = new AdaptiveThresholdManager();
            _recentTouches = new CircularBuffer<TouchCharacteristics>(_config.HistorySize);
        }

        /// <summary>
        /// 检测触摸是否为手掌
        /// </summary>
        /// <param name="touch">当前触摸特征</param>
        /// <param name="baseBoundsWidth">基准边界宽度</param>
        /// <param name="isQuadIr">是否为四点红外屏</param>
        /// <param name="isNibMode">是否为笔尖模式</param>
        /// <returns>手掌概率（0-1）和检测到的有效宽度</returns>
        public (double probability, double effectiveWidth) DetectPalm(
            TouchCharacteristics touch,
            double baseBoundsWidth,
            bool isQuadIr,
            bool isNibMode)
        {
            // 添加到历史记录
            _recentTouches.Add(touch);
            _adaptiveThreshold.AddSample(touch.Area);

            // 计算基础概率
            double baseProbability = CalculateBaseProbability(touch, baseBoundsWidth, isQuadIr);

            // 计算形状概率
            double shapeProbability = CalculateShapeProbability(touch);

            // 计算动态概率（基于历史）
            double dynamicProbability = CalculateDynamicProbability(touch);

            // 计算速度概率（手掌通常移动较慢）
            double velocityProbability = CalculateVelocityProbability(touch);

            // 加权融合
            double finalProbability = FuseProbabilities(
                baseProbability, shapeProbability, dynamicProbability, velocityProbability);

            // 计算有效宽度
            double effectiveWidth = CalculateEffectiveWidth(touch, baseBoundsWidth, isNibMode);

            return (finalProbability, effectiveWidth);
        }

        /// <summary>
        /// 计算基础概率（基于面积和阈值）
        /// </summary>
        private double CalculateBaseProbability(TouchCharacteristics touch, double baseBoundsWidth, bool isQuadIr)
        {
            double effectiveWidth = touch.GetEffectiveWidth(isQuadIr);

            if (effectiveWidth <= baseBoundsWidth)
                return 0.0;

            // 使用 Sigmoid 函数平滑概率
            double threshold = baseBoundsWidth * _config.BaseThresholdMultiplier;
            double x = (effectiveWidth - threshold) / (threshold * 0.5);
            return Sigmoid(x);
        }

        /// <summary>
        /// 计算形状概率（基于几何特征）
        /// </summary>
        private double CalculateShapeProbability(TouchCharacteristics touch)
        {
            double score = 0.0;

            // 圆度：手掌通常不规则（低圆度）
            double circularityScore = 1.0 - touch.Circularity;
            score += _config.CircularityWeight * circularityScore;

            // 长宽比：手掌通常较宽
            double aspectRatioScore = touch.AspectRatio < 1.0 ? touch.AspectRatio : 1.0 / touch.AspectRatio;
            score += _config.AspectRatioWeight * (1.0 - aspectRatioScore);

            // 面积变化：手掌面积相对稳定
            double areaStabilityScore = CalculateAreaStability(touch);
            score += _config.AreaStabilityWeight * areaStabilityScore;

            return Math.Min(1.0, Math.Max(0.0, score));
        }

        /// <summary>
        /// 计算动态概率（基于历史触摸数据）
        /// </summary>
        private double CalculateDynamicProbability(TouchCharacteristics touch)
        {
            if (_recentTouches.Count < 2)
                return 0.5;

            // 获取同一设备的历史记录
            var deviceHistory = _recentTouches
                .Where(t => t.DeviceId == touch.DeviceId)
                .OrderByDescending(t => t.Timestamp)
                .Take(5)
                .ToList();

            if (deviceHistory.Count < 2)
                return 0.5;

            // 分析面积变化趋势
            var areas = deviceHistory.Select(t => t.Area).ToList();
            double areaVariance = CalculateVariance(areas);

            // 手掌面积通常变化较小
            double stabilityScore = Math.Exp(-areaVariance / _config.AreaVarianceNormalization);

            // 分析移动模式
            double movementConsistency = CalculateMovementConsistency(deviceHistory);

            return (stabilityScore + movementConsistency) / 2.0;
        }

        /// <summary>
        /// 计算速度概率（手掌通常移动较慢）
        /// </summary>
        private double CalculateVelocityProbability(TouchCharacteristics touch)
        {
            if (touch.Velocity < _config.SlowVelocityThreshold)
                return 0.8; // 很慢，可能是手掌

            if (touch.Velocity > _config.FastVelocityThreshold)
                return 0.2; // 很快，可能是笔

            // 线性插值
            return 0.8 - (touch.Velocity - _config.SlowVelocityThreshold) /
                   (_config.FastVelocityThreshold - _config.SlowVelocityThreshold) * 0.6;
        }

        /// <summary>
        /// 融合多个概率
        /// </summary>
        private double FuseProbabilities(double baseProb, double shapeProb, double dynamicProb, double velocityProb)
        {
            // 使用加权平均
            double weightedSum =
                _config.BaseProbabilityWeight * baseProb +
                _config.ShapeProbabilityWeight * shapeProb +
                _config.DynamicProbabilityWeight * dynamicProb +
                _config.VelocityProbabilityWeight * velocityProb;

            double totalWeight =
                _config.BaseProbabilityWeight +
                _config.ShapeProbabilityWeight +
                _config.DynamicProbabilityWeight +
                _config.VelocityProbabilityWeight;

            double average = weightedSum / totalWeight;

            // 使用自适应阈值调整
            if (_config.UseAdaptiveThreshold)
            {
                double adaptiveThreshold = _adaptiveThreshold.GetThreshold();
                if (average > adaptiveThreshold)
                {
                    // _boost_ 概率
                    average = Math.Min(1.0, average * 1.1);
                }
            }

            return Math.Min(1.0, Math.Max(0.0, average));
        }

        /// <summary>
        /// 计算有效宽度（用于橡皮擦大小）
        /// </summary>
        private double CalculateEffectiveWidth(TouchCharacteristics touch, double baseBoundsWidth, bool isNibMode)
        {
            double multiplier = isNibMode
                ? _config.NibModeSizeMultiplier
                : _config.FingerModeSizeMultiplier;

            double width = touch.Width * multiplier;

            // 应用特殊屏幕调整
            if (_config.IsSpecialScreen)
            {
                width *= _config.SpecialScreenMultiplier;
            }

            // 确保最小和最大尺寸
            return Math.Max(_config.MinEraserSize, Math.Min(_config.MaxEraserSize, width));
        }

        /// <summary>
        /// 计算面积稳定性
        /// </summary>
        private double CalculateAreaStability(TouchCharacteristics touch)
        {
            var sameDeviceTouches = _recentTouches
                .Where(t => t.DeviceId == touch.DeviceId)
                .TakeLast(3)
                .ToList();

            if (sameDeviceTouches.Count < 2)
                return 0.5;

            double avgArea = sameDeviceTouches.Average(t => t.Area);
            double variance = sameDeviceTouches.Average(t => Math.Pow(t.Area - avgArea, 2));

            // 方差越小，稳定性越高
            return Math.Exp(-variance / (avgArea * avgArea + 1e-6));
        }

        /// <summary>
        /// 计算移动一致性
        /// </summary>
        private double CalculateMovementConsistency(List<TouchCharacteristics> history)
        {
            if (history.Count < 2)
                return 0.5;

            // 分析方向变化
            var directions = history
                .Where(t => t.Direction.Length > 0)
                .Select(t => t.Direction)
                .ToList();

            if (directions.Count < 2)
                return 0.5;

            // 计算方向一致性（点积接近1表示方向一致）
            double consistency = 0;
            for (int i = 1; i < directions.Count; i++)
            {
                consistency += directions[i] * directions[i - 1];
            }
            consistency /= (directions.Count - 1);

            // 映射到 0-1 范围
            return (consistency + 1) / 2;
        }

        /// <summary>
        /// Sigmoid 函数
        /// </summary>
        private static double Sigmoid(double x)
        {
            return 1.0 / (1.0 + Math.Exp(-x));
        }

        /// <summary>
        /// 计算方差
        /// </summary>
        private static double CalculateVariance(List<double> values)
        {
            if (values.Count < 2) return 0;
            double avg = values.Average();
            return values.Average(v => Math.Pow(v - avg, 2));
        }
    }

    /// <summary>
    /// 检测器配置
    /// </summary>
    public class PalmEraserDetectorConfig
    {
        // 基础检测参数
        public double BaseThresholdMultiplier { get; set; } = 1.5;
        public bool UseAdaptiveThreshold { get; set; } = true;

        // 特征权重
        public double CircularityWeight { get; set; } = 0.25;
        public double AspectRatioWeight { get; set; } = 0.15;
        public double AreaStabilityWeight { get; set; } = 0.20;

        // 概率融合权重
        public double BaseProbabilityWeight { get; set; } = 0.35;
        public double ShapeProbabilityWeight { get; set; } = 0.25;
        public double DynamicProbabilityWeight { get; set; } = 0.20;
        public double VelocityProbabilityWeight { get; set; } = 0.20;

        // 速度阈值
        public double SlowVelocityThreshold { get; set; } = 0.5;  // 像素/毫秒
        public double FastVelocityThreshold { get; set; } = 5.0;  // 像素/毫秒

        // 面积归一化
        public double AreaVarianceNormalization { get; set; } = 10000;

        // 历史记录大小
        public int HistorySize { get; set; } = 20;

        // 橡皮擦大小参数
        public double NibModeSizeMultiplier { get; set; } = 1.2;
        public double FingerModeSizeMultiplier { get; set; } = 1.0;
        public double MinEraserSize { get; set; } = 40;
        public double MaxEraserSize { get; set; } = 200;

        // 特殊屏幕设置
        public bool IsSpecialScreen { get; set; } = false;
        public double SpecialScreenMultiplier { get; set; } = 1.0;
    }

    /// <summary>
    /// 自适应阈值管理器
    /// </summary>
    public class AdaptiveThresholdManager
    {
        private readonly CircularBuffer<double> _samples;
        private const int DefaultSampleSize = 50;

        public AdaptiveThresholdManager(int sampleSize = DefaultSampleSize)
        {
            _samples = new CircularBuffer<double>(sampleSize);
        }

        public void AddSample(double area)
        {
            _samples.Add(area);
        }

        /// <summary>
        /// 获取动态阈值
        /// </summary>
        public double GetThreshold()
        {
            if (_samples.Count < 10)
                return 0.5; // 默认阈值

            var samples = _samples.ToArray();
            double mean = samples.Average();
            double stdDev = Math.Sqrt(samples.Average(s => Math.Pow(s - mean, 2)));

            // 使用均值 + 2倍标准差作为阈值
            double threshold = mean + 2 * stdDev;

            // 归一化到 0-1 范围（假设面积在 0-100000 范围内）
            return Math.Min(1.0, threshold / 100000.0);
        }
    }

    /// <summary>
    /// 循环缓冲区实现
    /// </summary>
    public class CircularBuffer<T> : IEnumerable<T>
    {
        private readonly T[] _buffer;
        private int _head;
        private int _count;

        public int Count => _count;
        public int Capacity => _buffer.Length;

        public CircularBuffer(int capacity)
        {
            _buffer = new T[capacity];
            _head = 0;
            _count = 0;
        }

        public void Add(T item)
        {
            _buffer[_head] = item;
            _head = (_head + 1) % _buffer.Length;
            if (_count < _buffer.Length)
                _count++;
        }

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= _count)
                    throw new IndexOutOfRangeException();
                return _buffer[(_head - _count + index + _buffer.Length) % _buffer.Length];
            }
        }

        public T[] ToArray()
        {
            var result = new T[_count];
            for (int i = 0; i < _count; i++)
            {
                result[i] = this[i];
            }
            return result;
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < _count; i++)
            {
                yield return this[i];
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
