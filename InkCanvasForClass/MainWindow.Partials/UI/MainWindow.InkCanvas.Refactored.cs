// ============================================================================
// MW_InkCanvas.Refactored.cs - InkCanvas 核心逻辑（重构版本）
// ============================================================================
// 
// 功能说明:
//   - IccStroke 自定义墨迹类（支持形状类型标记）
//   - IccInkCanvas 改进版本，移除反射调用
//   - 使用现代 C# 特性提升代码质量
//
// 改进内容:
//   - 移除反射调用，使用 PreviewKeyDown 事件拦截 Delete 键
//   - 使用 record 类型简化数据模型
//   - 使用 switch expression 简化条件逻辑
//   - 优化缓存机制
//
// ============================================================================

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace Ink_Canvas
{
    /// <summary>
    /// 形状绘制类型枚举（改进版本）
    /// </summary>
    public enum ShapeDrawingTypeModern
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
        Trapezoid = 13
    }

    /// <summary>
    /// 箭头线配置（改进为 record 类型）
    /// </summary>
    public record ArrowLineConfigModern
    {
        public double ArrowWidth { get; init; } = 10;
        public double ArrowHeight { get; init; } = 10;
    }

    /// <summary>
    /// 笔画属性键（常量定义）
    /// </summary>
    public static class StrokePropertyKeys
    {
        public static readonly Guid ShapeType = new("6537b29c-557f-487f-800b-cb30a8f1de78");
        public static readonly Guid IsShape = new("40eff5db-9346-4e42-bd46-7b0eb19d0018");
    }

    /// <summary>
    /// 改进的 IccStroke 类
    /// </summary>
    public class IccStrokeModern : Stroke
    {
        // 缓存字段
        private bool? _cachedIsShape;
        private int? _cachedShapeType;
        private StreamGeometry _cachedGeometry;
        private Pen _cachedPen;
        private Color _cachedPenColor;
        private double _cachedPenWidth;
        private bool _geometryNeedsUpdate = true;
        private SolidColorBrush _cachedFillBrush;
        private Color _cachedFillColor;

        private static readonly Brush TransparentBrush = Brushes.Transparent;

        public IccStrokeModern(StylusPointCollection stylusPoints, DrawingAttributes drawingAttributes)
            : base(stylusPoints, drawingAttributes)
        {
        }

        /// <summary>
        /// 原始笔触点集合
        /// </summary>
        public StylusPointCollection RawStylusPointCollection { get; set; }

        /// <summary>
        /// 箭头线配置
        /// </summary>
        public ArrowLineConfigModern ArrowLineConfig { get; set; } = new();

        /// <summary>
        /// 是否为原始输入
        /// </summary>
        public bool IsRawStylusPoints { get; set; } = true;

        /// <summary>
        /// 是否在线条形状上分布点
        /// </summary>
        public bool IsDistributePointsOnLineShape { get; set; } = true;

        /// <summary>
        /// 是否为擦除后的部分笔画
        /// </summary>
        public bool IsErasedStrokePart { get; set; }

        /// <summary>
        /// 获取缓存的形状类型
        /// </summary>
        private int GetCachedShapeType()
        {
            if (_cachedShapeType is null)
            {
                _cachedShapeType = ContainsPropertyData(StrokePropertyKeys.ShapeType)
                    ? (int)GetPropertyData(StrokePropertyKeys.ShapeType)
                    : -1;
            }
            return _cachedShapeType.Value;
        }

        /// <summary>
        /// 获取缓存的是否为形状标记
        /// </summary>
        private bool GetCachedIsShape()
        {
            if (_cachedIsShape is null)
            {
                _cachedIsShape = ContainsPropertyData(StrokePropertyKeys.IsShape) &&
                                 (bool)GetPropertyData(StrokePropertyKeys.IsShape);
            }
            return _cachedIsShape.Value;
        }

        /// <summary>
        /// 使几何缓存失效
        /// </summary>
        public void InvalidateGeometryCache() => _geometryNeedsUpdate = true;

        protected override void DrawCore(DrawingContext drawingContext, DrawingAttributes drawingAttributes)
        {
            // 如果不是形状，使用默认绘制
            if (!GetCachedIsShape())
            {
                base.DrawCore(drawingContext, drawingAttributes);
                return;
            }

            var shapeType = (ShapeDrawingTypeModern)GetCachedShapeType();

            // 使用 switch expression 判断是否为线条形状
            var isLineShape = shapeType switch
            {
                ShapeDrawingTypeModern.DashedLine or
                ShapeDrawingTypeModern.Line or
                ShapeDrawingTypeModern.DottedLine or
                ShapeDrawingTypeModern.ArrowOneSide or
                ShapeDrawingTypeModern.ArrowTwoSide => true,
                _ => false
            };

            if (!isLineShape || StylusPoints.Count < 2)
            {
                base.DrawCore(drawingContext, drawingAttributes);
                return;
            }

            // 更新画笔缓存
            UpdatePenCache(drawingAttributes, shapeType);

            // 处理线条形状的点分布
            ProcessLineShapePoints(shapeType);

            // 处理箭头形状
            ProcessArrowShape(shapeType);

            // 更新几何缓存
            UpdateGeometryCache();

            // 绘制
            var fillBrush = GetFillBrush(shapeType, drawingAttributes.Color);
            drawingContext.DrawGeometry(fillBrush, _cachedPen, _cachedGeometry);
        }

        /// <summary>
        /// 更新画笔缓存
        /// </summary>
        private void UpdatePenCache(DrawingAttributes drawingAttributes, ShapeDrawingTypeModern shapeType)
        {
            var penWidth = (drawingAttributes.Width + drawingAttributes.Height) / 2;
            var penColor = DrawingAttributes.Color;

            if (_cachedPen is null || _cachedPenColor != penColor || Math.Abs(_cachedPenWidth - penWidth) > 0.001)
            {
                var penBrush = new SolidColorBrush(penColor);
                penBrush.Freeze();

                _cachedPen = new Pen(penBrush, penWidth)
                {
                    DashCap = PenLineCap.Round,
                    StartLineCap = PenLineCap.Round,
                    EndLineCap = PenLineCap.Round
                };

                // 使用 switch expression 设置虚线样式
                _cachedPen.DashStyle = shapeType switch
                {
                    ShapeDrawingTypeModern.DottedLine => DashStyles.Dot,
                    ShapeDrawingTypeModern.DashedLine => DashStyles.Dash,
                    _ => DashStyles.Solid
                };

                _cachedPen.Freeze();
                _cachedPenColor = penColor;
                _cachedPenWidth = penWidth;
                _geometryNeedsUpdate = true;
            }
        }

        /// <summary>
        /// 处理线条形状的点分布
        /// </summary>
        private void ProcessLineShapePoints(ShapeDrawingTypeModern shapeType)
        {
            if (!IsDistributePointsOnLineShape || !IsRawStylusPoints)
                return;

            var shouldDistribute = shapeType switch
            {
                ShapeDrawingTypeModern.DashedLine or
                ShapeDrawingTypeModern.Line or
                ShapeDrawingTypeModern.DottedLine => true,
                _ => false
            };

            if (!shouldDistribute)
                return;

            IsRawStylusPoints = false;
            RawStylusPointCollection = StylusPoints.Clone();

            var startPoint = new Point(StylusPoints[0].X, StylusPoints[0].Y);
            var endPoint = new Point(StylusPoints[1].X, StylusPoints[1].Y);

            var pointList = new List<Point>(20) { startPoint };
            pointList.AddRange(ShapeDrawingHelper.DistributePointsOnLine(startPoint, endPoint));
            pointList.Add(endPoint);

            StylusPoints = new StylusPointCollection(pointList);
            _geometryNeedsUpdate = true;
        }

        /// <summary>
        /// 处理箭头形状
        /// </summary>
        private void ProcessArrowShape(ShapeDrawingTypeModern shapeType)
        {
            if (shapeType != ShapeDrawingTypeModern.ArrowOneSide || !IsRawStylusPoints)
                return;

            IsRawStylusPoints = false;

            var pt0 = new Point(StylusPoints[0].X, StylusPoints[0].Y);
            var pt1 = new Point(StylusPoints[1].X, StylusPoints[1].Y);
            RawStylusPointCollection = StylusPoints.Clone();

            var (w, h) = (ArrowLineConfig.ArrowWidth, ArrowLineConfig.ArrowHeight);
            var theta = Math.Atan2(pt0.Y - pt1.Y, pt0.X - pt1.X);
            var (sint, cost) = (Math.Sin(theta), Math.Cos(theta));

            var pointList = new List<Point>(10) { pt0 };

            if (IsDistributePointsOnLineShape)
            {
                pointList.AddRange(ShapeDrawingHelper.DistributePointsOnLine(pt0, pt1));
            }

            pointList.Add(pt1);
            pointList.Add(new Point(pt1.X + (w * cost - h * sint), pt1.Y + (w * sint + h * cost)));
            pointList.Add(pt1);
            pointList.Add(new Point(pt1.X + (w * cost + h * sint), pt1.Y - (h * cost - w * sint)));

            StylusPoints = new StylusPointCollection(pointList);
            _geometryNeedsUpdate = true;
        }

        /// <summary>
        /// 更新几何缓存
        /// </summary>
        private void UpdateGeometryCache()
        {
            if (!_geometryNeedsUpdate && _cachedGeometry is not null)
                return;

            _cachedGeometry = new StreamGeometry();

            using (var ctx = _cachedGeometry.Open())
            {
                var points = StylusPoints;
                if (points.Count > 0)
                {
                    ctx.BeginFigure(new Point(points[0].X, points[0].Y), false, false);

                    var ptArray = new Point[points.Count - 1];
                    for (int i = 1; i < points.Count; i++)
                    {
                        ptArray[i - 1] = new Point(points[i].X, points[i].Y);
                    }

                    ctx.PolyLineTo(ptArray, true, true);
                }
            }

            _cachedGeometry.Freeze();
            _geometryNeedsUpdate = false;
        }

        /// <summary>
        /// 获取填充画刷
        /// </summary>
        private Brush GetFillBrush(ShapeDrawingTypeModern shapeType, Color penColor)
        {
            if (shapeType != ShapeDrawingTypeModern.ArrowOneSide)
                return TransparentBrush;

            if (_cachedFillBrush is null || _cachedFillColor != penColor)
            {
                _cachedFillBrush = new SolidColorBrush(penColor);
                _cachedFillBrush.Freeze();
                _cachedFillColor = penColor;
            }

            return _cachedFillBrush;
        }
    }

    /// <summary>
    /// 改进的 IccInkCanvas 类 - 移除反射调用
    /// </summary>
    public class IccInkCanvasModern : InkCanvas
    {
        public IccInkCanvasModern()
        {
            // 注册自定义 Delete 命令
            RegisterDeleteCommand();

            // 订阅 PreviewKeyDown 事件来拦截 Delete 键
            PreviewKeyDown += OnPreviewKeyDown;
        }

        /// <summary>
        /// Delete 键命令触发事件
        /// </summary>
        public event EventHandler<RoutedEventArgs> DeleteKeyCommandFired;

        /// <summary>
        /// 注册自定义 Delete 命令
        /// </summary>
        private void RegisterDeleteCommand()
        {
            CommandManager.RegisterClassCommandBinding(
                typeof(IccInkCanvasModern),
                new CommandBinding(
                    ApplicationCommands.Delete,
                    OnDeleteExecuted,
                    OnDeleteCanExecute));
        }

        /// <summary>
        /// Delete 命令执行处理
        /// </summary>
        private void OnDeleteExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            DeleteKeyCommandFired?.Invoke(this, e);
            e.Handled = true;
        }

        /// <summary>
        /// Delete 命令 CanExecute 处理
        /// </summary>
        private void OnDeleteCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = GetSelectedStrokes().Count != 0;
            e.Handled = true;
        }

        /// <summary>
        /// PreviewKeyDown 事件处理 - 拦截 Delete 键
        /// </summary>
        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                // 触发自定义事件
                DeleteKeyCommandFired?.Invoke(this, e);
                e.Handled = true;
            }
        }

        protected override void OnStrokeCollected(InkCanvasStrokeCollectedEventArgs e)
        {
            var customStroke = new IccStrokeModern(e.Stroke.StylusPoints, e.Stroke.DrawingAttributes);

            if (e.Stroke is IccStrokeModern)
            {
                Strokes.Add(e.Stroke);
            }
            else
            {
                Strokes.Remove(e.Stroke);
                Strokes.Add(customStroke);
            }

            var args = new InkCanvasStrokeCollectedEventArgs(customStroke);
            base.OnStrokeCollected(args);
        }
    }
}
