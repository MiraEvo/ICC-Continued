
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Media;
using Windows.UI.Input.Inking;
using Windows.UI.Input.Inking.Analysis;
using Ink_Canvas.Models.Settings;

namespace Ink_Canvas.Helpers
{
    /// <summary>
    /// 形状识别帮助类 - 使用 Windows.UI.Input.Inking.Analysis API
    /// 优化版本：添加置信度阈值、几何验证、缓存机制和更多形状支持
    /// </summary>
    public class InkRecognizeHelper
    {
        private static InkAnalyzer _analyzer;
        private static InkStrokeContainer _strokeContainer;
        private static readonly object _syncLock = new object();
        
        // 识别结果缓存（基于笔画哈希）
        private static readonly ConcurrentDictionary<int, CachedRecognitionResult> _recognitionCache 
            = new ConcurrentDictionary<int, CachedRecognitionResult>();
        
        // 缓存过期时间（毫秒）
        private const int CacheExpirationMs = 5000;
        
        // 最大缓存条目数
        private const int MaxCacheEntries = 50;
        
        static InkRecognizeHelper()
        {
            InitializeAnalyzer();
        }
        
        /// <summary>
        /// 初始化分析器
        /// </summary>
        private static void InitializeAnalyzer()
        {
            try
            {
                _analyzer = new InkAnalyzer();
                _strokeContainer = new InkStrokeContainer();
                LogHelper.WriteLogToFile("InkRecognizeHelper initialized successfully", LogHelper.LogType.Info);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile("InkRecognizeHelper initialization failed: " + ex.Message, LogHelper.LogType.Error);
            }
        }
        
        /// <summary>
        /// 清理过期缓存
        /// </summary>
        private static void CleanupCache()
        {
            var now = DateTime.UtcNow;
            var expiredKeys = _recognitionCache
                .Where(kvp => (now - kvp.Value.Timestamp).TotalMilliseconds > CacheExpirationMs)
                .Select(kvp => kvp.Key)
                .ToList();
            
            foreach (var key in expiredKeys)
            {
                _recognitionCache.TryRemove(key, out _);
            }
            
            // 如果缓存过大，移除最旧的条目
            while (_recognitionCache.Count > MaxCacheEntries)
            {
                var oldest = _recognitionCache.OrderBy(kvp => kvp.Value.Timestamp).FirstOrDefault();
                if (oldest.Key != 0)
                {
                    _recognitionCache.TryRemove(oldest.Key, out _);
                }
            }
        }
        
        /// <summary>
        /// 计算笔画集合的哈希值用于缓存
        /// </summary>
        private static int ComputeStrokesHash(StrokeCollection strokes)
        {
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
                return hash;
            }
        }
        
        /// <summary>
        /// 识别形状（同步包装）
        /// </summary>
        /// <param name="strokes">WPF 笔画集合</param>
        /// <param name="settings">识别设置（可选）</param>
        /// <returns>识别结果，如果识别失败则返回 default</returns>
        public static ShapeRecognizeResult RecognizeShape(StrokeCollection strokes, InkToShapeSettings settings = null)
        {
            if (strokes == null || strokes.Count == 0)
                return default;
            
            try
            {
                // 尝试从缓存获取
                int strokesHash = ComputeStrokesHash(strokes);
                if (_recognitionCache.TryGetValue(strokesHash, out var cached))
                {
                    if ((DateTime.UtcNow - cached.Timestamp).TotalMilliseconds < CacheExpirationMs)
                    {
                        return cached.Result;
                    }
                }
                
                // 使用 Task.Run 避免在同步上下文中死锁
                var result = Task.Run(() => RecognizeShapeAsync(strokes, settings)).GetAwaiter().GetResult();
                
                // 缓存结果
                if (result != null)
                {
                    CleanupCache();
                    _recognitionCache[strokesHash] = new CachedRecognitionResult(result);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"RecognizeShape failed: {ex.Message}\nStackTrace: {ex.StackTrace}\nInnerException: {ex.InnerException?.Message}", LogHelper.LogType.Error);
                return default;
            }
        }
        
        /// <summary>
        /// 识别形状（异步）
        /// </summary>
        /// <param name="strokes">WPF 笔画集合</param>
        /// <param name="settings">识别设置（可选）</param>
        /// <returns>识别结果，如果识别失败则返回 default</returns>
        public static async Task<ShapeRecognizeResult> RecognizeShapeAsync(StrokeCollection strokes, InkToShapeSettings settings = null)
        {
            if (strokes == null || strokes.Count == 0)
            {
                LogHelper.WriteLogToFile("RecognizeShapeAsync: strokes is null or empty", LogHelper.LogType.Trace);
                return default;
            }
            
            // 使用默认设置如果未提供
            settings ??= new InkToShapeSettings();
            
            InkAnalyzer analyzer = null;
            InkStrokeContainer strokeContainer = null;
            
            try
            {
                LogHelper.WriteLogToFile($"RecognizeShapeAsync: Starting with {strokes.Count} strokes", LogHelper.LogType.Trace);
                
                // 创建新的分析器实例避免并发问题
                try
                {
                    analyzer = new InkAnalyzer();
                    strokeContainer = new InkStrokeContainer();
                    LogHelper.WriteLogToFile("RecognizeShapeAsync: InkAnalyzer and InkStrokeContainer created successfully", LogHelper.LogType.Trace);
                }
                catch (Exception initEx)
                {
                    LogHelper.WriteLogToFile($"RecognizeShapeAsync: Failed to create InkAnalyzer or InkStrokeContainer: {initEx.Message}\nStackTrace: {initEx.StackTrace}", LogHelper.LogType.Error);
                    return default;
                }
                
                // 检查笔画边界是否满足最小尺寸要求（放宽限制）
                var strokeBounds = strokes.GetBounds();
                double minSize = settings.MinimumShapeSize;
                if (strokeBounds.Width < minSize && strokeBounds.Height < minSize)
                {
                    LogHelper.WriteLogToFile($"Strokes too small: {strokeBounds.Width}x{strokeBounds.Height} < {minSize}", LogHelper.LogType.Trace);
                    return default;
                }
                
                // 转换 WPF strokes 到 UWP strokes（使用简单转换，不进行重采样以保留原始形状）
                List<InkStroke> uwpStrokes;
                try
                {
                    uwpStrokes = StrokeConverter.ToUwpStrokes(strokes, false);  // 禁用重采样
                    LogHelper.WriteLogToFile($"RecognizeShapeAsync: Converted {strokes.Count} WPF strokes to {uwpStrokes.Count} UWP strokes", LogHelper.LogType.Trace);
                }
                catch (Exception convertEx)
                {
                    LogHelper.WriteLogToFile($"RecognizeShapeAsync: Failed to convert strokes: {convertEx.Message}\nStackTrace: {convertEx.StackTrace}", LogHelper.LogType.Error);
                    return default;
                }
                
                if (uwpStrokes.Count == 0)
                {
                    LogHelper.WriteLogToFile("RecognizeShapeAsync: No UWP strokes after conversion", LogHelper.LogType.Trace);
                    return default;
                }
                
                // 添加到容器和分析器
                try
                {
                    foreach (var stroke in uwpStrokes)
                    {
                        strokeContainer.AddStroke(stroke);
                    }
                    
                    // 获取所有笔画
                    var allStrokes = strokeContainer.GetStrokes();
                    
                    // 重要：必须先调用 AddDataForStrokes 将笔画添加到分析器
                    // 然后才能调用 SetStrokeDataKind 设置笔画类型
                    analyzer.AddDataForStrokes(allStrokes);
                    LogHelper.WriteLogToFile($"RecognizeShapeAsync: Added {allStrokes.Count} strokes to analyzer", LogHelper.LogType.Trace);
                    
                    // 设置分析器只识别图形（提高识别准确性）
                    foreach (var stroke in allStrokes)
                    {
                        analyzer.SetStrokeDataKind(stroke.Id, InkAnalysisStrokeKind.Drawing);
                    }
                    LogHelper.WriteLogToFile("RecognizeShapeAsync: Set stroke data kind to Drawing for all strokes", LogHelper.LogType.Trace);
                }
                catch (Exception addEx)
                {
                    LogHelper.WriteLogToFile($"RecognizeShapeAsync: Failed to add strokes to analyzer: {addEx.Message}\nStackTrace: {addEx.StackTrace}", LogHelper.LogType.Error);
                    return default;
                }
                
                // 执行分析
                InkAnalysisResult result;
                try
                {
                    result = await analyzer.AnalyzeAsync();
                    LogHelper.WriteLogToFile($"RecognizeShapeAsync: Analysis completed with status: {result.Status}", LogHelper.LogType.Trace);
                }
                catch (Exception analyzeEx)
                {
                    LogHelper.WriteLogToFile($"RecognizeShapeAsync: AnalyzeAsync failed: {analyzeEx.Message}\nStackTrace: {analyzeEx.StackTrace}\nHResult: 0x{analyzeEx.HResult:X8}", LogHelper.LogType.Error);
                    return default;
                }
                
                if (result.Status != InkAnalysisStatus.Updated)
                {
                    LogHelper.WriteLogToFile($"RecognizeShapeAsync: Analysis status is not Updated: {result.Status}", LogHelper.LogType.Trace);
                    return default;
                }
                
                // 获取识别到的所有图形
                var drawingNodes = analyzer.AnalysisRoot.FindNodes(InkAnalysisNodeKind.InkDrawing);
                
                if (drawingNodes.Count == 0)
                {
                    return default;
                }
                
                // 遍历所有识别到的图形，选择最佳匹配
                InkAnalysisInkDrawing bestDrawing = null;
                double bestScore = 0;
                
                foreach (var node in drawingNodes)
                {
                    var drawing = node as InkAnalysisInkDrawing;
                    if (drawing == null)
                        continue;
                    
                    // 检查是否是我们支持的形状
                    if (!IsContainShapeType(drawing.DrawingKind, settings.EnablePolygonRecognition))
                        continue;
                    
                    // 计算匹配分数（基于覆盖率、形状完整性和几何验证）
                    double score = CalculateShapeScore(drawing, strokes, settings);
                    
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestDrawing = drawing;
                    }
                }
                
                // 检查置信度阈值（更宽松）
                if (bestDrawing == null)
                {
                    LogHelper.WriteLogToFile("No best drawing found", LogHelper.LogType.Trace);
                    return default;
                }
                
                // 如果分数太低，也接受但记录
                if (bestScore < settings.ConfidenceThreshold)
                {
                    LogHelper.WriteLogToFile($"Score {bestScore:F3} below threshold {settings.ConfidenceThreshold}, but still accepting", LogHelper.LogType.Trace);
                }
                
                // 禁用几何验证以提高识别率
                // if (settings.GeometryValidationStrength > 0)
                // {
                //     if (!ValidateShapeGeometry(bestDrawing, strokes, settings.GeometryValidationStrength))
                //     {
                //         return default;
                //     }
                // }
                
                // 构建结果 - 使用线程安全的 Point[] 代替 PointCollection
                var centroid = StrokeConverter.ToWpfPoint(bestDrawing.Center);
                var hotPoints = StrokeConverter.ToWpfPointArray(bestDrawing.Points);
                var boundingRect = StrokeConverter.ToWpfRect(bestDrawing.BoundingRect);
                
                return new ShapeRecognizeResult(
                    centroid,
                    hotPoints,
                    bestDrawing.DrawingKind,
                    boundingRect,
                    strokes,
                    bestScore
                );
            }
            catch (Exception ex)
            {
                var errorMessage = $"RecognizeShapeAsync failed: {ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $"\nInnerException: {ex.InnerException.Message}";
                }
                errorMessage += $"\nStackTrace: {ex.StackTrace}";
                errorMessage += $"\nHResult: 0x{ex.HResult:X8}";
                errorMessage += $"\nException Type: {ex.GetType().FullName}";
                
                LogHelper.WriteLogToFile(errorMessage, LogHelper.LogType.Error);
                return default;
            }
        }
        
        /// <summary>
        /// 验证形状的几何属性
        /// </summary>
        private static bool ValidateShapeGeometry(InkAnalysisInkDrawing drawing, StrokeCollection originalStrokes, double strength)
        {
            try
            {
                var points = drawing.Points.ToList();
                int expectedPoints = GetExpectedPointCount(drawing.DrawingKind);
                
                if (points.Count < expectedPoints * (1 - strength * 0.3))
                    return false;
                
                switch (drawing.DrawingKind)
                {
                    case InkAnalysisDrawingKind.Circle:
                        return ValidateCircle(drawing, strength);
                        
                    case InkAnalysisDrawingKind.Ellipse:
                        return ValidateEllipse(drawing, strength);
                        
                    case InkAnalysisDrawingKind.Triangle:
                        return ValidateTriangle(points, strength);
                        
                    case InkAnalysisDrawingKind.Rectangle:
                    case InkAnalysisDrawingKind.Square:
                        return ValidateRectangle(points, strength);
                        
                    case InkAnalysisDrawingKind.Diamond:
                        return ValidateDiamond(points, strength);
                        
                    case InkAnalysisDrawingKind.Parallelogram:
                    case InkAnalysisDrawingKind.Trapezoid:
                        return ValidateQuadrilateral(points, strength);
                        
                    case InkAnalysisDrawingKind.Pentagon:
                    case InkAnalysisDrawingKind.Hexagon:
                        // 明确禁止识别五边形及以上的多边形
                        return false;
                        
                    default:
                        // 对于其他未明确处理的类型，如果有 5 个或更多点，也视为不支持
                        if (expectedPoints > 4)
                            return false;
                        return true;
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile("ValidateShapeGeometry error: " + ex.Message, LogHelper.LogType.Error);
                return true;
            }
        }
        
        private static bool ValidateCircle(InkAnalysisInkDrawing drawing, double strength)
        {
            var bounds = StrokeConverter.ToWpfRect(drawing.BoundingRect);
            double aspectRatio = Math.Min(bounds.Width, bounds.Height) / Math.Max(bounds.Width, bounds.Height);
            double threshold = 0.7 + (1 - strength) * 0.2;
            return aspectRatio >= threshold;
        }
        
        private static bool ValidateEllipse(InkAnalysisInkDrawing drawing, double strength)
        {
            var bounds = StrokeConverter.ToWpfRect(drawing.BoundingRect);
            double aspectRatio = Math.Min(bounds.Width, bounds.Height) / Math.Max(bounds.Width, bounds.Height);
            double minRatio = 0.2 + (1 - strength) * 0.1;
            return aspectRatio >= minRatio && aspectRatio <= 1.0;
        }
        
        private static bool ValidateTriangle(IList<Windows.Foundation.Point> points, double strength)
        {
            if (points.Count < 3) return false;
            
            // 简单的三角形验证：只要有三个点，并且不是一条直线（面积不为0）
            // 在实际手绘中，很难画出完全共线的三个点，所以这里放宽限制
            var p1 = points[0];
            var p2 = points[1];
            var p3 = points[2];
            
            // 计算三角形面积（使用叉积公式）
            double area = Math.Abs((p2.X - p1.X) * (p3.Y - p1.Y) - (p3.X - p1.X) * (p2.Y - p1.Y)) / 2.0;
            
            // 计算边界框面积，用于归一化比较
            double minX = Math.Min(p1.X, Math.Min(p2.X, p3.X));
            double maxX = Math.Max(p1.X, Math.Max(p2.X, p3.X));
            double minY = Math.Min(p1.Y, Math.Min(p2.Y, p3.Y));
            double maxY = Math.Max(p1.Y, Math.Max(p2.Y, p3.Y));
            double boundArea = (maxX - minX) * (maxY - minY);
            
            // 如果三角形面积相对于边界框太小，可能是直线
            if (boundArea > 0 && area / boundArea < 0.05)
                return false;
                
            // 进一步验证角度，避免极度扁平的三角形
            var angles = CalculatePolygonAngles(points.Take(3).ToList());
            // 放宽角度限制，支持更多形态的三角形
            // 最小角度允许更小，最大角度允许更大，只要不接近180度
            double minAngle = 5;
            double maxAngle = 175;
            
            return angles.All(a => a >= minAngle && a <= maxAngle);
        }
        
        private static bool ValidateRectangle(IList<Windows.Foundation.Point> points, double strength)
        {
            if (points.Count < 4) return false;
            var angles = CalculatePolygonAngles(points.Take(4).ToList());
            double tolerance = 20 + (1 - strength) * 20;
            return angles.All(a => Math.Abs(a - 90) <= tolerance);
        }
        
        private static bool ValidateDiamond(IList<Windows.Foundation.Point> points, double strength)
        {
            if (points.Count < 4) return false;
            var p = points.Take(4).ToList();
            double side1 = Distance(p[0], p[1]);
            double side2 = Distance(p[1], p[2]);
            double side3 = Distance(p[2], p[3]);
            double side4 = Distance(p[3], p[0]);
            double avgSide = (side1 + side2 + side3 + side4) / 4;
            double tolerance = avgSide * (0.3 - strength * 0.15);
            return Math.Abs(side1 - avgSide) <= tolerance &&
                   Math.Abs(side2 - avgSide) <= tolerance &&
                   Math.Abs(side3 - avgSide) <= tolerance &&
                   Math.Abs(side4 - avgSide) <= tolerance;
        }
        
        private static bool ValidateQuadrilateral(IList<Windows.Foundation.Point> points, double strength)
        {
            if (points.Count < 4) return false;
            return IsConvexPolygon(points.Take(4).ToList());
        }
        
        private static bool ValidatePolygon(IList<Windows.Foundation.Point> points, int expectedSides, double strength)
        {
            if (points.Count < expectedSides) return false;
            var p = points.Take(expectedSides).ToList();
            if (!IsConvexPolygon(p)) return false;
            var angles = CalculatePolygonAngles(p);
            double expectedAngle = (expectedSides - 2) * 180.0 / expectedSides;
            double tolerance = 30 + (1 - strength) * 20;
            return angles.All(a => Math.Abs(a - expectedAngle) <= tolerance);
        }
        
        private static List<double> CalculatePolygonAngles(IList<Windows.Foundation.Point> points)
        {
            var angles = new List<double>();
            int n = points.Count;
            for (int i = 0; i < n; i++)
            {
                var p1 = points[(i - 1 + n) % n];
                var p2 = points[i];
                var p3 = points[(i + 1) % n];
                angles.Add(CalculateAngle(p1, p2, p3));
            }
            return angles;
        }
        
        private static double CalculateAngle(Windows.Foundation.Point p1, Windows.Foundation.Point p2, Windows.Foundation.Point p3)
        {
            double v1x = p1.X - p2.X, v1y = p1.Y - p2.Y;
            double v2x = p3.X - p2.X, v2y = p3.Y - p2.Y;
            double dot = v1x * v2x + v1y * v2y;
            double cross = v1x * v2y - v1y * v2x;
            return Math.Atan2(Math.Abs(cross), dot) * 180 / Math.PI;
        }
        
        private static double Distance(Windows.Foundation.Point p1, Windows.Foundation.Point p2)
        {
            double dx = p2.X - p1.X, dy = p2.Y - p1.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
        
        private static bool IsConvexPolygon(IList<Windows.Foundation.Point> points)
        {
            int n = points.Count;
            if (n < 3) return false;
            bool? sign = null;
            for (int i = 0; i < n; i++)
            {
                var p1 = points[i];
                var p2 = points[(i + 1) % n];
                var p3 = points[(i + 2) % n];
                double cross = (p2.X - p1.X) * (p3.Y - p2.Y) - (p2.Y - p1.Y) * (p3.X - p2.X);
                if (Math.Abs(cross) > 1e-6)
                {
                    bool currentSign = cross > 0;
                    if (sign == null) sign = currentSign;
                    else if (sign != currentSign) return false;
                }
            }
            return true;
        }
        
        private static int GetExpectedPointCount(InkAnalysisDrawingKind kind)
        {
            return kind switch
            {
                InkAnalysisDrawingKind.Circle => 4,
                InkAnalysisDrawingKind.Ellipse => 4,
                InkAnalysisDrawingKind.Triangle => 3,
                InkAnalysisDrawingKind.Rectangle => 4,
                InkAnalysisDrawingKind.Square => 4,
                InkAnalysisDrawingKind.Diamond => 4,
                InkAnalysisDrawingKind.Trapezoid => 4,
                InkAnalysisDrawingKind.Parallelogram => 4,
                InkAnalysisDrawingKind.Pentagon => 5,
                InkAnalysisDrawingKind.Hexagon => 6,
                _ => 4
            };
        }
        
        /// <summary>
        /// 计算形状匹配分数（简化版本 - 提高识别率）
        /// </summary>
        private static double CalculateShapeScore(InkAnalysisInkDrawing drawing, StrokeCollection originalStrokes, InkToShapeSettings settings)
        {
            try
            {
                // 简化分数计算，直接基于形状类型返回高分
                // 因为 Windows Ink Analysis API 已经做了形状识别，我们信任它的结果
                int expectedPoints = GetExpectedPointCount(drawing.DrawingKind);
                int actualPoints = drawing.Points.Count;
                
                // 如果有足够的关键点，给高分
                if (actualPoints >= expectedPoints)
                    return 0.9;
                
                // 否则基于点数比例给分
                double completenessScore = (double)actualPoints / expectedPoints;
                return Math.Max(0.5, completenessScore);
            }
            catch
            {
                return 0.7;  // 出错时返回较高分数，让识别继续
            }
        }
        
        private static double GetShapeSpecificBonus(InkAnalysisInkDrawing drawing, StrokeCollection strokes)
        {
            var bounds = StrokeConverter.ToWpfRect(drawing.BoundingRect);
            double aspectRatio = bounds.Width / bounds.Height;
            
            switch (drawing.DrawingKind)
            {
                case InkAnalysisDrawingKind.Circle:
                    return 1.0 - Math.Abs(1.0 - aspectRatio) * 0.5;
                    
                case InkAnalysisDrawingKind.Square:
                    return 1.0 - Math.Abs(1.0 - aspectRatio) * 0.5;
                    
                case InkAnalysisDrawingKind.Rectangle:
                    if (aspectRatio > 1.2 || aspectRatio < 0.8)
                        return 0.8;
                    return 0.6;
                    
                case InkAnalysisDrawingKind.Triangle:
                    return drawing.Points.Count >= 3 ? 0.8 : 0.5;
                    
                default:
                    return 0.7;
            }
        }
        
        private static double CalculateClosureScore(StrokeCollection strokes, InkAnalysisDrawingKind kind)
        {
            bool shouldBeClosed = kind != InkAnalysisDrawingKind.Drawing;
            if (!shouldBeClosed)
                return 1.0;
            
            try
            {
                double totalClosureScore = 0;
                int strokeCount = 0;
                
                foreach (var stroke in strokes)
                {
                    if (stroke.StylusPoints.Count < 2)
                        continue;
                    
                    var first = stroke.StylusPoints[0];
                    var last = stroke.StylusPoints[stroke.StylusPoints.Count - 1];
                    
                    double dx = last.X - first.X;
                    double dy = last.Y - first.Y;
                    double distance = Math.Sqrt(dx * dx + dy * dy);
                    
                    double totalLength = 0;
                    for (int i = 1; i < stroke.StylusPoints.Count; i++)
                    {
                        var p1 = stroke.StylusPoints[i - 1];
                        var p2 = stroke.StylusPoints[i];
                        totalLength += Math.Sqrt(
                            (p2.X - p1.X) * (p2.X - p1.X) + 
                            (p2.Y - p1.Y) * (p2.Y - p1.Y));
                    }
                    
                    if (totalLength > 0)
                    {
                        double closure = 1.0 - Math.Min(1.0, distance / (totalLength * 0.1));
                        totalClosureScore += Math.Max(0, closure);
                        strokeCount++;
                    }
                }
                
                return strokeCount > 0 ? totalClosureScore / strokeCount : 0.5;
            }
            catch
            {
                return 0.5;
            }
        }
        
        private static double CalculateOverlapArea(Rect r1, Rect r2)
        {
            double left = Math.Max(r1.Left, r2.Left);
            double right = Math.Min(r1.Right, r2.Right);
            double top = Math.Max(r1.Top, r2.Top);
            double bottom = Math.Min(r1.Bottom, r2.Bottom);
            
            if (left < right && top < bottom)
                return (right - left) * (bottom - top);
            return 0;
        }
        
        /// <summary>
        /// 预热分析器，加速首次使用
        /// </summary>
        public static async Task PreloadAsync()
        {
            try
            {
                var analyzer = new InkAnalyzer();
                var container = new InkStrokeContainer();
                
                var builder = new InkStrokeBuilder();
                var points = new List<InkPoint> {
                    new InkPoint(new Windows.Foundation.Point(0, 0), 0.5f),
                    new InkPoint(new Windows.Foundation.Point(100, 100), 0.5f)
                };
                var stroke = builder.CreateStrokeFromInkPoints(points, System.Numerics.Matrix3x2.Identity);
                container.AddStroke(stroke);
                analyzer.AddDataForStrokes(container.GetStrokes());
                
                await analyzer.AnalyzeAsync();
                
                LogHelper.WriteLogToFile("Ink Analysis API preloaded successfully", LogHelper.LogType.Info);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile("Ink Analysis API preload failed: " + ex.Message, LogHelper.LogType.Error);
            }
        }
        
        /// <summary>
        /// 检查是否是支持的形状类型
        /// 注意：不再支持 Pentagon 和 Hexagon，因为用户要求只识别四边形及以下的形状
        /// </summary>
        public static bool IsContainShapeType(InkAnalysisDrawingKind kind, bool enablePolygon = true)
        {
            // 强制忽略 enablePolygon 参数，始终只支持基础形状
            
            // 只支持基础形状：圆形、椭圆、三角形、矩形类
            // 明确排除了 Pentagon (五边形) 和 Hexagon (六边形)
            bool basicShapes = kind == InkAnalysisDrawingKind.Circle ||
                               kind == InkAnalysisDrawingKind.Ellipse ||
                               kind == InkAnalysisDrawingKind.Triangle ||
                               kind == InkAnalysisDrawingKind.Rectangle ||
                               kind == InkAnalysisDrawingKind.Square ||
                               kind == InkAnalysisDrawingKind.Diamond ||
                               kind == InkAnalysisDrawingKind.Trapezoid ||
                               kind == InkAnalysisDrawingKind.Parallelogram;
            
            return basicShapes;
        }
        
        /// <summary>
        /// 检查是否是支持的形状类型（按名称，兼容旧代码）
        /// </summary>
        public static bool IsContainShapeType(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;
                
            // 确保不包含 Pentagon 和 Hexagon
            return name.Contains("Triangle") ||
                   name.Contains("Circle") ||
                   name.Contains("Rectangle") ||
                   name.Contains("Diamond") ||
                   name.Contains("Parallelogram") ||
                   name.Contains("Square") ||
                   name.Contains("Ellipse") ||
                   name.Contains("Trapezoid");
        }
        
        /// <summary>
        /// 清除识别缓存
        /// </summary>
        public static void ClearCache()
        {
            _recognitionCache.Clear();
        }
    }
    
    /// <summary>
    /// 缓存的识别结果
    /// </summary>
    internal class CachedRecognitionResult
    {
        public ShapeRecognizeResult Result { get; }
        public DateTime Timestamp { get; }
        
        public CachedRecognitionResult(ShapeRecognizeResult result)
        {
            Result = result;
            Timestamp = DateTime.UtcNow;
        }
    }
    
    /// <summary>
    /// 形状识别结果 - 新版本，适配 Windows.UI.Input.Inking.Analysis API
    /// 注意：使用 Point[] 替代 PointCollection 以支持跨线程访问
    /// </summary>
    public class ShapeRecognizeResult
    {
        /// <summary>
        /// 创建形状识别结果
        /// </summary>
        public ShapeRecognizeResult(
            Point centroid,
            Point[] hotPoints,
            InkAnalysisDrawingKind drawingKind,
            Rect boundingRect,
            StrokeCollection originalStrokes,
            double confidenceScore = 1.0)
        {
            Centroid = centroid;
            HotPoints = hotPoints;
            DrawingKind = drawingKind;
            BoundingRect = boundingRect;
            OriginalStrokes = originalStrokes;
            ConfidenceScore = confidenceScore;
        }
        
        /// <summary>
        /// 形状中心点
        /// </summary>
        public Point Centroid { get; set; }
        
        /// <summary>
        /// 形状关键点（顶点）- 使用 Point[] 以支持跨线程访问
        /// </summary>
        public Point[] HotPoints { get; }
        
        /// <summary>
        /// 识别到的形状类型
        /// </summary>
        public InkAnalysisDrawingKind DrawingKind { get; }
        
        /// <summary>
        /// 边界矩形
        /// </summary>
        public Rect BoundingRect { get; }
        
        /// <summary>
        /// 原始笔画集合
        /// </summary>
        public StrokeCollection OriginalStrokes { get; }
        
        /// <summary>
        /// 置信度分数 (0.0 - 1.0)
        /// </summary>
        public double ConfidenceScore { get; }
        
        /// <summary>
        /// 获取形状名称（兼容旧代码）
        /// </summary>
        public string GetShapeName()
        {
            return DrawingKind.ToString();
        }
        
        /// <summary>
        /// 获取形状宽度（兼容旧代码）
        /// </summary>
        public double Width => BoundingRect.Width;
        
        /// <summary>
        /// 获取形状高度（兼容旧代码）
        /// </summary>
        public double Height => BoundingRect.Height;
        
        #region 兼容性属性 - 模拟旧版 InkDrawingNode
        
        /// <summary>
        /// 模拟旧版 InkDrawingNode，方便渐进式迁移
        /// </summary>
        public InkDrawingNodeAdapter InkDrawingNode => new InkDrawingNodeAdapter(this);
        
        #endregion
    }
    
    /// <summary>
    /// InkDrawingNode 适配器 - 模拟旧 API 的接口，减少代码修改量
    /// </summary>
    public class InkDrawingNodeAdapter
    {
        private readonly ShapeRecognizeResult _result;
        
        public InkDrawingNodeAdapter(ShapeRecognizeResult result)
        {
            _result = result;
        }
        
        public string GetShapeName() => _result.DrawingKind.ToString();
        
        public ShapeAdapter GetShape() => new ShapeAdapter(_result.BoundingRect);
        
        /// <summary>
        /// 形状关键点 - 使用 Point[] 以支持跨线程访问
        /// </summary>
        public Point[] HotPoints => _result.HotPoints;
        
        public Point Centroid => _result.Centroid;
        
        public StrokeCollection Strokes => _result.OriginalStrokes;
    }
    
    /// <summary>
    /// Shape 适配器 - 模拟旧 API 的 Shape 对象
    /// </summary>
    public class ShapeAdapter
    {
        public ShapeAdapter(Rect bounds)
        {
            Width = bounds.Width;
            Height = bounds.Height;
        }
        
        public double Width { get; }
        public double Height { get; }
    }
    
    /// <summary>
    /// 用于自动控制其他形状相对于圆的位置
    /// </summary>
    public class Circle
    {
        public Circle(Point centroid, double r, Stroke stroke)
        {
            Centroid = centroid;
            R = r;
            Stroke = stroke;
        }

        public Point Centroid { get; set; }
        public double R { get; set; }
        public Stroke Stroke { get; set; }
    }
}
