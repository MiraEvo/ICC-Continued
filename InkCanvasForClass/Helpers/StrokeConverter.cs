using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Ink;
using System.Windows.Input;
using Windows.Foundation;
using Windows.UI.Input.Inking;

namespace Ink_Canvas.Helpers
{
    /// <summary>
    /// 重采样设置
    /// </summary>
    public class ResampleSettings
    {
        /// <summary>
        /// 是否启用重采样
        /// </summary>
        public bool EnableResampling { get; set; } = true;

        /// <summary>
        /// 目标采样点数
        /// </summary>
        public int TargetPointCount { get; set; } = 64;

        /// <summary>
        /// 是否启用平滑
        /// </summary>
        public bool EnableSmoothing { get; set; } = true;

        /// <summary>
        /// 平滑窗口大小
        /// </summary>
        public int SmoothingWindowSize { get; set; } = 3;
    }

    /// <summary>
    /// WPF Stroke 与 UWP InkStroke 之间的转换工具
    /// 优化版本：支持自适应重采样和平滑处理
    /// </summary>
    public static class StrokeConverter
    {
        private static readonly InkStrokeBuilder _strokeBuilder;
        private static readonly InkDrawingAttributes _defaultDrawingAttributes;

        static StrokeConverter()
        {
            _strokeBuilder = new InkStrokeBuilder();

            // 设置默认绘图属性，优化识别效果
            _defaultDrawingAttributes = new InkDrawingAttributes
            {
                Size = new Windows.Foundation.Size(4, 4),
                Color = Windows.UI.Color.FromArgb(255, 0, 0, 0),
                PenTip = PenTipShape.Circle,
                IgnorePressure = false,
                FitToCurve = true
            };
        }

        /// <summary>
        /// 将 WPF StrokeCollection 转换为 UWP InkStroke 列表（使用默认设置）
        /// </summary>
        public static List<InkStroke> ToUwpStrokes(StrokeCollection wpfStrokes, bool resamplePoints = true)
        {
            try
            {
                var settings = new ResampleSettings { EnableResampling = resamplePoints };
                return ToUwpStrokes(wpfStrokes, settings);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"StrokeConverter.ToUwpStrokes(bool) failed: {ex.Message}\nStackTrace: {ex.StackTrace}", LogHelper.LogType.Error);
                return new List<InkStroke>();
            }
        }

        /// <summary>
        /// 将 WPF StrokeCollection 转换为 UWP InkStroke 列表（使用自定义设置）
        /// </summary>
        public static List<InkStroke> ToUwpStrokes(StrokeCollection wpfStrokes, ResampleSettings settings)
        {
            var uwpStrokes = new List<InkStroke>();

            if (wpfStrokes == null)
            {
                // LogHelper.WriteLogToFile("StrokeConverter.ToUwpStrokes: wpfStrokes is null", LogHelper.LogType.Trace);
                return uwpStrokes;
            }

            settings ??= new ResampleSettings();

            // LogHelper.WriteLogToFile($"StrokeConverter.ToUwpStrokes: Converting {wpfStrokes.Count} strokes, resample={settings.EnableResampling}", LogHelper.LogType.Trace);

            int successCount = 0;
            int failCount = 0;

            foreach (var wpfStroke in wpfStrokes)
            {
                try
                {
                    var uwpStroke = ToUwpStroke(wpfStroke, settings);
                    if (uwpStroke != null)
                    {
                        uwpStrokes.Add(uwpStroke);
                        successCount++;
                    }
                    else
                    {
                        failCount++;
                    }
                }
                catch (Exception ex)
                {
                    failCount++;
                    LogHelper.WriteLogToFile($"StrokeConverter.ToUwpStrokes: Failed to convert stroke: {ex.Message}", LogHelper.LogType.Error);
                }
            }

            if (failCount > 0)
            {
                // LogHelper.WriteLogToFile($"StrokeConverter.ToUwpStrokes: Converted {successCount} strokes, {failCount} failed", LogHelper.LogType.Trace);
            }

            return uwpStrokes;
        }

        /// <summary>
        /// 将单个 WPF Stroke 转换为 UWP InkStroke（使用默认设置）
        /// </summary>
        public static InkStroke ToUwpStroke(Stroke wpfStroke, bool resamplePoints = true)
        {
            try
            {
                var settings = new ResampleSettings { EnableResampling = resamplePoints };
                return ToUwpStroke(wpfStroke, settings);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"StrokeConverter.ToUwpStroke(bool) failed: {ex.Message}\nStackTrace: {ex.StackTrace}", LogHelper.LogType.Error);
                return null;
            }
        }

        /// <summary>
        /// 将单个 WPF Stroke 转换为 UWP InkStroke（使用自定义设置）
        /// </summary>
        public static InkStroke ToUwpStroke(Stroke wpfStroke, ResampleSettings settings)
        {
            if (wpfStroke == null)
                return null;

            settings ??= new ResampleSettings();

            try
            {
                var inkPoints = new List<InkPoint>();
                var stylusPoints = wpfStroke.StylusPoints;

                // 如果点太少，直接转换
                if (stylusPoints.Count < 4 || !settings.EnableResampling)
                {
                    foreach (var stylusPoint in stylusPoints)
                    {
                        var inkPoint = new InkPoint(
                            new Point(stylusPoint.X, stylusPoint.Y),
                            Math.Max(0.1f, stylusPoint.PressureFactor)
                        );
                        inkPoints.Add(inkPoint);
                    }
                }
                else
                {
                    // 重新采样以获得更均匀的点分布
                    inkPoints = ResamplePointsAdvanced(stylusPoints, settings);

                    // 可选的平滑处理
                    if (settings.EnableSmoothing && inkPoints.Count > settings.SmoothingWindowSize)
                    {
                        inkPoints = SmoothPoints(inkPoints, settings.SmoothingWindowSize);
                    }
                }

                // InkStroke 至少需要 2 个点
                if (inkPoints.Count < 2)
                    return null;

                _strokeBuilder.SetDefaultDrawingAttributes(_defaultDrawingAttributes);

                var uwpStroke = _strokeBuilder.CreateStrokeFromInkPoints(
                    inkPoints,
                    System.Numerics.Matrix3x2.Identity
                );

                return uwpStroke;
            }
            catch (Exception ex)
            {
                var errorMessage = $"StrokeConverter.ToUwpStroke failed: {ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $"\nInnerException: {ex.InnerException.Message}";
                }
                errorMessage += $"\nStackTrace: {ex.StackTrace}";
                errorMessage += $"\nHResult: 0x{ex.HResult:X8}";

                LogHelper.WriteLogToFile(errorMessage, LogHelper.LogType.Error);
                return null;
            }
        }

        /// <summary>
        /// 高级重采样算法 - 自适应采样点数，优化识别准确性
        /// </summary>
        private static List<InkPoint> ResamplePointsAdvanced(StylusPointCollection points, ResampleSettings settings)
        {
            var result = new List<InkPoint>();

            if (points.Count < 2)
            {
                foreach (var p in points)
                {
                    result.Add(new InkPoint(new Point(p.X, p.Y), Math.Max(0.1f, p.PressureFactor)));
                }
                return result;
            }

            // 计算笔画总长度和曲率
            double totalLength = 0;
            var curvatures = new List<double>();

            for (int i = 1; i < points.Count; i++)
            {
                totalLength += Distance(points[i - 1], points[i]);
            }

            // 计算每个点的曲率
            for (int i = 1; i < points.Count - 1; i++)
            {
                double curvature = CalculateCurvature(
                    points[i - 1].ToPoint(),
                    points[i].ToPoint(),
                    points[i + 1].ToPoint());
                curvatures.Add(curvature);
            }

            // 自适应采样点数：根据笔画长度和复杂度
            double avgCurvature = curvatures.Count > 0 ? curvatures.Average() : 0;
            int basePointCount = settings.TargetPointCount;

            // 曲率越大，需要更多的采样点
            int adaptivePointCount = (int)(basePointCount * (1 + avgCurvature * 0.5));
            adaptivePointCount = Math.Max(20, Math.Min(200, adaptivePointCount));

            // 根据长度再调整
            int lengthBasedCount = (int)(totalLength / 5);
            int targetPointCount = Math.Max(adaptivePointCount, lengthBasedCount);

            // 优化：对于非常短的笔画，减少采样点数以避免过度拟合
            if (totalLength < 50)
            {
                targetPointCount = Math.Min(targetPointCount, 32);
            }

            targetPointCount = Math.Max(16, Math.Min(200, targetPointCount));

            if (totalLength <= 0)
            {
                result.Add(new InkPoint(new Point(points[0].X, points[0].Y), 0.5f));
                return result;
            }

            double stepLength = totalLength / (targetPointCount - 1);

            // 添加第一个点
            result.Add(new InkPoint(
                new Point(points[0].X, points[0].Y),
                Math.Max(0.1f, points[0].PressureFactor)
            ));

            double accumulatedLength = 0;
            int currentIndex = 0;

            for (int i = 1; i < targetPointCount - 1; i++)
            {
                double targetLength = i * stepLength;

                while (currentIndex < points.Count - 1)
                {
                    double segmentLength = Distance(points[currentIndex], points[currentIndex + 1]);

                    if (accumulatedLength + segmentLength >= targetLength)
                    {
                        double ratio = (targetLength - accumulatedLength) / segmentLength;
                        ratio = Math.Max(0, Math.Min(1, ratio));

                        double x = points[currentIndex].X + ratio * (points[currentIndex + 1].X - points[currentIndex].X);
                        double y = points[currentIndex].Y + ratio * (points[currentIndex + 1].Y - points[currentIndex].Y);
                        float pressure = (float)(points[currentIndex].PressureFactor +
                            ratio * (points[currentIndex + 1].PressureFactor - points[currentIndex].PressureFactor));

                        result.Add(new InkPoint(
                            new Point(x, y),
                            Math.Max(0.1f, pressure)
                        ));
                        break;
                    }

                    accumulatedLength += segmentLength;
                    currentIndex++;
                }
            }

            // 添加最后一个点
            var lastPoint = points[points.Count - 1];
            result.Add(new InkPoint(
                new Point(lastPoint.X, lastPoint.Y),
                Math.Max(0.1f, lastPoint.PressureFactor)
            ));

            return result;
        }

        /// <summary>
        /// 计算三点的曲率（使用外接圆半径的倒数）
        /// </summary>
        private static double CalculateCurvature(System.Windows.Point p1, System.Windows.Point p2, System.Windows.Point p3)
        {
            // 计算三边长度
            double a = Math.Sqrt((p2.X - p1.X) * (p2.X - p1.X) + (p2.Y - p1.Y) * (p2.Y - p1.Y));
            double b = Math.Sqrt((p3.X - p2.X) * (p3.X - p2.X) + (p3.Y - p2.Y) * (p3.Y - p2.Y));
            double c = Math.Sqrt((p3.X - p1.X) * (p3.X - p1.X) + (p3.Y - p1.Y) * (p3.Y - p1.Y));

            // 半周长
            double s = (a + b + c) / 2;

            // 海伦公式计算面积
            double area = Math.Sqrt(Math.Max(0, s * (s - a) * (s - b) * (s - c)));

            // 外接圆半径
            if (area < 1e-10)
                return 0;

            double radius = (a * b * c) / (4 * area);

            // 曲率 = 1 / 半径，归一化到 [0, 1]
            return Math.Min(1.0, 1.0 / Math.Max(1.0, radius));
        }

        /// <summary>
        /// 平滑点序列（移动平均滤波）
        /// </summary>
        private static List<InkPoint> SmoothPoints(List<InkPoint> points, int windowSize)
        {
            if (points.Count <= windowSize)
                return points;

            var result = new List<InkPoint>();
            int halfWindow = windowSize / 2;

            for (int i = 0; i < points.Count; i++)
            {
                double sumX = 0, sumY = 0;
                float sumPressure = 0;
                int count = 0;

                for (int j = Math.Max(0, i - halfWindow); j <= Math.Min(points.Count - 1, i + halfWindow); j++)
                {
                    sumX += points[j].Position.X;
                    sumY += points[j].Position.Y;
                    sumPressure += points[j].Pressure;
                    count++;
                }

                if (count > 0)
                {
                    result.Add(new InkPoint(
                        new Point(sumX / count, sumY / count),
                        sumPressure / count
                    ));
                }
            }

            // 保留首尾点不变，避免形状变形
            if (result.Count > 0 && points.Count > 0)
            {
                result[0] = points[0];
                result[result.Count - 1] = points[points.Count - 1];
            }

            return result;
        }

        /// <summary>
        /// 保留的旧版重采样方法（兼容性）
        /// </summary>
        private static List<InkPoint> ResamplePoints(StylusPointCollection points)
        {
            return ResamplePointsAdvanced(points, new ResampleSettings());
        }

        /// <summary>
        /// 计算两点之间的距离
        /// </summary>
        private static double Distance(StylusPoint p1, StylusPoint p2)
        {
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// 将 UWP Point 转换为 WPF Point
        /// </summary>
        /// <param name="uwpPoint">UWP 点</param>
        /// <returns>WPF 点</returns>
        public static System.Windows.Point ToWpfPoint(Point uwpPoint)
        {
            return new System.Windows.Point(uwpPoint.X, uwpPoint.Y);
        }

        /// <summary>
        /// 将 UWP Point 列表转换为 WPF PointCollection
        /// 警告：PointCollection 不能跨线程访问，建议使用 ToWpfPointArray
        /// </summary>
        /// <param name="uwpPoints">UWP 点列表</param>
        /// <returns>WPF 点集合</returns>
        [Obsolete("PointCollection 不能跨线程访问，请使用 ToWpfPointArray")]
        public static System.Windows.Media.PointCollection ToWpfPointCollection(IReadOnlyList<Point> uwpPoints)
        {
            var wpfPoints = new System.Windows.Media.PointCollection();

            if (uwpPoints == null)
                return wpfPoints;

            foreach (var p in uwpPoints)
            {
                wpfPoints.Add(ToWpfPoint(p));
            }
            return wpfPoints;
        }

        /// <summary>
        /// 将 UWP Point 列表转换为 WPF Point 数组（线程安全）
        /// </summary>
        /// <param name="uwpPoints">UWP 点列表</param>
        /// <returns>WPF 点数组</returns>
        public static System.Windows.Point[] ToWpfPointArray(IReadOnlyList<Point> uwpPoints)
        {
            if (uwpPoints == null || uwpPoints.Count == 0)
                return Array.Empty<System.Windows.Point>();

            var result = new System.Windows.Point[uwpPoints.Count];
            for (int i = 0; i < uwpPoints.Count; i++)
            {
                result[i] = ToWpfPoint(uwpPoints[i]);
            }
            return result;
        }

        /// <summary>
        /// 将 UWP Rect 转换为 WPF Rect
        /// </summary>
        /// <param name="uwpRect">UWP 矩形</param>
        /// <returns>WPF 矩形</returns>
        public static System.Windows.Rect ToWpfRect(Windows.Foundation.Rect uwpRect)
        {
            return new System.Windows.Rect(uwpRect.X, uwpRect.Y, uwpRect.Width, uwpRect.Height);
        }
    }
}
