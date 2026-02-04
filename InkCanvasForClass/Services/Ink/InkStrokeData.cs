using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace Ink_Canvas.Services.Ink
{
    /// <summary>
    /// 笔画数据模型 - 轻量级结构，减少内存分配
    /// </summary>
    public readonly record struct InkStrokeData
    {
        /// <summary>
        /// 唯一标识符
        /// </summary>
        public Guid Id { get; init; }

        /// <summary>
        /// 笔画点集合
        /// </summary>
        public IReadOnlyList<InkPoint> Points { get; init; }

        /// <summary>
        /// 绘制属性
        /// </summary>
        public DrawingAttributes DrawingAttributes { get; init; }

        /// <summary>
        /// 形状类型
        /// </summary>
        public InkShapeType ShapeType { get; init; }

        /// <summary>
        /// 是否为形状笔画
        /// </summary>
        public bool IsShape { get; init; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTimeOffset CreatedAt { get; init; }

        /// <summary>
        /// 自定义属性数据
        /// </summary>
        public IReadOnlyDictionary<string, object> Properties { get; init; }

        /// <summary>
        /// 获取笔画边界
        /// </summary>
        public Rect Bounds => CalculateBounds();

        public InkStrokeData()
        {
            Id = Guid.NewGuid();
            Points = Array.Empty<InkPoint>();
            DrawingAttributes = new DrawingAttributes();
            ShapeType = InkShapeType.None;
            IsShape = false;
            CreatedAt = DateTimeOffset.UtcNow;
            Properties = new Dictionary<string, object>();
        }

        public InkStrokeData(IEnumerable<InkPoint> points, DrawingAttributes drawingAttributes)
        {
            Id = Guid.NewGuid();
            Points = points?.ToArray() ?? Array.Empty<InkPoint>();
            DrawingAttributes = drawingAttributes ?? new DrawingAttributes();
            ShapeType = InkShapeType.None;
            IsShape = false;
            CreatedAt = DateTimeOffset.UtcNow;
            Properties = new Dictionary<string, object>();
        }

        private Rect CalculateBounds()
        {
            if (Points == null || Points.Count == 0)
                return Rect.Empty;

            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;

            foreach (var point in Points)
            {
                minX = Math.Min(minX, point.X);
                minY = Math.Min(minY, point.Y);
                maxX = Math.Max(maxX, point.X);
                maxY = Math.Max(maxY, point.Y);
            }

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// 转换为 WPF Stroke
        /// </summary>
        public Stroke ToWpfStroke()
        {
            var stylusPoints = new StylusPointCollection(Points.Select(p => p.ToStylusPoint()));
            var stroke = new Stroke(stylusPoints, DrawingAttributes.Clone());
            return stroke;
        }

        /// <summary>
        /// 从 WPF Stroke 创建
        /// </summary>
        public static InkStrokeData FromWpfStroke(Stroke stroke)
        {
            if (stroke == null)
                return new InkStrokeData();

            var points = stroke.StylusPoints.Select(sp => InkPoint.FromStylusPoint(sp)).ToArray();

            return new InkStrokeData
            {
                Id = Guid.NewGuid(),
                Points = points,
                DrawingAttributes = stroke.DrawingAttributes.Clone(),
                ShapeType = InkShapeType.None,
                IsShape = false,
                CreatedAt = DateTimeOffset.UtcNow,
                Properties = new Dictionary<string, object>()
            };
        }

        /// <summary>
        /// 创建形状笔画
        /// </summary>
        public static InkStrokeData CreateShapeStroke(
            IEnumerable<InkPoint> points,
            DrawingAttributes drawingAttributes,
            InkShapeType shapeType)
        {
            return new InkStrokeData
            {
                Id = Guid.NewGuid(),
                Points = points?.ToArray() ?? Array.Empty<InkPoint>(),
                DrawingAttributes = drawingAttributes ?? new DrawingAttributes(),
                ShapeType = shapeType,
                IsShape = shapeType != InkShapeType.None,
                CreatedAt = DateTimeOffset.UtcNow,
                Properties = new Dictionary<string, object>()
            };
        }
    }

    /// <summary>
    /// 墨迹点结构 - 轻量级值类型
    /// </summary>
    public readonly record struct InkPoint
    {
        public double X { get; init; }
        public double Y { get; init; }
        public float Pressure { get; init; }
        public float TiltX { get; init; }
        public float TiltY { get; init; }
        public long Timestamp { get; init; }

        public InkPoint(double x, double y, float pressure = 0.5f)
        {
            X = x;
            Y = y;
            Pressure = pressure;
            TiltX = 0;
            TiltY = 0;
            Timestamp = 0;
        }

        public InkPoint(double x, double y, float pressure, float tiltX, float tiltY, long timestamp = 0)
        {
            X = x;
            Y = y;
            Pressure = pressure;
            TiltX = tiltX;
            TiltY = tiltY;
            Timestamp = timestamp;
        }

        public Point ToPoint() => new Point(X, Y);

        public StylusPoint ToStylusPoint() => new StylusPoint(X, Y, Pressure);

        public static InkPoint FromPoint(Point point, float pressure = 0.5f) =>
            new InkPoint(point.X, point.Y, pressure);

        public static InkPoint FromStylusPoint(StylusPoint stylusPoint) =>
            new InkPoint(stylusPoint.X, stylusPoint.Y, stylusPoint.PressureFactor);

        public static implicit operator Point(InkPoint p) => p.ToPoint();
        public static implicit operator InkPoint(Point p) => FromPoint(p);
    }

    /// <summary>
    /// 形状类型枚举
    /// </summary>
    public enum InkShapeType
    {
        None = 0,
        Line = 1,
        DashedLine = 2,
        DottedLine = 3,
        ArrowOneSide = 4,
        ArrowTwoSide = 5,
        Circle = 6,
        Ellipse = 7,
        Triangle = 8,
        Rectangle = 9,
        Square = 10,
        Diamond = 11,
        Parallelogram = 12,
        Trapezoid = 13,
        Pentagon = 14,
        Hexagon = 15,
        Custom = 100
    }

    /// <summary>
    /// 识别结果
    /// </summary>
    public readonly record struct InkRecognitionResult
    {
        public bool IsSuccessful { get; init; }
        public InkShapeType RecognizedShape { get; init; }
        public double Confidence { get; init; }
        public Point[] HotPoints { get; init; }
        public Rect BoundingBox { get; init; }
        public string ErrorMessage { get; init; }

        public static InkRecognitionResult Failed(string errorMessage) => new()
        {
            IsSuccessful = false,
            ErrorMessage = errorMessage,
            RecognizedShape = InkShapeType.None,
            Confidence = 0,
            HotPoints = Array.Empty<Point>(),
            BoundingBox = Rect.Empty
        };

        public static InkRecognitionResult Success(
            InkShapeType shape,
            double confidence,
            Point[] hotPoints,
            Rect boundingBox) => new()
        {
            IsSuccessful = true,
            RecognizedShape = shape,
            Confidence = confidence,
            HotPoints = hotPoints ?? Array.Empty<Point>(),
            BoundingBox = boundingBox,
            ErrorMessage = null
        };
    }
}
