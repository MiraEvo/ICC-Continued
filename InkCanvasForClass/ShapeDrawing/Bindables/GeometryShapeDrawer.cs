using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using Ink_Canvas.ShapeDrawing.Core;

namespace Ink_Canvas.ShapeDrawing.Bindables {
    /// <summary>
    /// 矩形形状绘制器
    /// </summary>
    public class RectangleShapeDrawer : BaseShapeDrawer {
        public override ShapeDrawingType ShapeType => ShapeDrawingType.Rectangle;

        public override StrokeCollection Draw(ShapeDrawingContext context) {
            var strokes = new StrokeCollection();
            
            if (!ValidateContext(context)) return strokes;

            Point start = context.StartPoint;
            Point end = context.EndPoint;

            // 矩形四个顶点
            var points = new StylusPointCollection {
                new StylusPoint(start.X, start.Y),
                new StylusPoint(end.X, start.Y),
                new StylusPoint(end.X, end.Y),
                new StylusPoint(start.X, end.Y),
                new StylusPoint(start.X, start.Y)  // 闭合
            };

            var stroke = new Stroke(points) { DrawingAttributes = context.DrawingAttributes.Clone() };
            strokes.Add(stroke);
            
            return strokes;
        }
    }

    /// <summary>
    /// 中心矩形形状绘制器（从中心向外绘制）
    /// </summary>
    public class RectangleCenterShapeDrawer : BaseShapeDrawer {
        public override ShapeDrawingType ShapeType => ShapeDrawingType.RectangleCenter;

        public override StrokeCollection Draw(ShapeDrawingContext context) {
            var strokes = new StrokeCollection();
            
            if (!ValidateContext(context)) return strokes;

            Point center = context.StartPoint;
            Point corner = context.EndPoint;

            // 计算从中心到角落的半宽和半高
            double halfWidth = Math.Abs(corner.X - center.X);
            double halfHeight = Math.Abs(corner.Y - center.Y);

            // 矩形四个顶点（从中心扩展）
            var points = new StylusPointCollection {
                new StylusPoint(center.X - halfWidth, center.Y - halfHeight),
                new StylusPoint(center.X + halfWidth, center.Y - halfHeight),
                new StylusPoint(center.X + halfWidth, center.Y + halfHeight),
                new StylusPoint(center.X - halfWidth, center.Y + halfHeight),
                new StylusPoint(center.X - halfWidth, center.Y - halfHeight)  // 闭合
            };

            var stroke = new Stroke(points) { DrawingAttributes = context.DrawingAttributes.Clone() };
            strokes.Add(stroke);
            
            return strokes;
        }
    }

    /// <summary>
    /// 椭圆形状绘制器
    /// </summary>
    public class EllipseShapeDrawer : BaseShapeDrawer {
        public override ShapeDrawingType ShapeType => ShapeDrawingType.Ellipse;

        public override StrokeCollection Draw(ShapeDrawingContext context) {
            var strokes = new StrokeCollection();
            
            if (!ValidateContext(context)) return strokes;

            var points = GenerateEllipsePoints(context.StartPoint, context.EndPoint);
            if (points.Count < 2) return strokes;

            var stylusPoints = new StylusPointCollection();
            foreach (var point in points) {
                stylusPoints.Add(new StylusPoint(point.X, point.Y));
            }
            // 闭合椭圆
            stylusPoints.Add(new StylusPoint(points[0].X, points[0].Y));

            var stroke = new Stroke(stylusPoints) { DrawingAttributes = context.DrawingAttributes.Clone() };
            strokes.Add(stroke);
            
            return strokes;
        }
    }

    /// <summary>
    /// 圆形形状绘制器（通过对角点）
    /// </summary>
    public class CircleShapeDrawer : BaseShapeDrawer {
        public override ShapeDrawingType ShapeType => ShapeDrawingType.Circle;

        public override StrokeCollection Draw(ShapeDrawingContext context) {
            var strokes = new StrokeCollection();
            
            if (!ValidateContext(context)) return strokes;

            Point start = context.StartPoint;
            Point end = context.EndPoint;

            // 计算半径（取宽和高的较小值）
            double width = Math.Abs(end.X - start.X);
            double height = Math.Abs(end.Y - start.Y);
            double radius = Math.Min(width, height) / 2;

            // 确定圆心
            double centerX = (start.X + end.X) / 2;
            double centerY = (start.Y + end.Y) / 2;

            // 调整边界使其成为正方形
            double minX = centerX - radius;
            double minY = centerY - radius;
            double maxX = centerX + radius;
            double maxY = centerY + radius;

            var points = GenerateEllipsePoints(new Point(minX, minY), new Point(maxX, maxY));
            if (points.Count < 2) return strokes;

            var stylusPoints = new StylusPointCollection();
            foreach (var point in points) {
                stylusPoints.Add(new StylusPoint(point.X, point.Y));
            }
            stylusPoints.Add(new StylusPoint(points[0].X, points[0].Y));

            var stroke = new Stroke(stylusPoints) { DrawingAttributes = context.DrawingAttributes.Clone() };
            strokes.Add(stroke);
            
            return strokes;
        }
    }

    /// <summary>
    /// 中心圆形形状绘制器（从中心向外）
    /// </summary>
    public class CenterCircleShapeDrawer : BaseShapeDrawer {
        public override ShapeDrawingType ShapeType => ShapeDrawingType.CenterCircle;

        public override StrokeCollection Draw(ShapeDrawingContext context) {
            var strokes = new StrokeCollection();
            
            if (!ValidateContext(context)) return strokes;

            Point center = context.StartPoint;
            Point edge = context.EndPoint;

            double radius = GetDistance(center, edge);
            if (radius < 1) return strokes;

            // 生成圆形点
            var stylusPoints = new StylusPointCollection();
            const double step = 0.01;
            for (double r = 0; r <= 2 * Math.PI; r += step) {
                stylusPoints.Add(new StylusPoint(
                    center.X + radius * Math.Cos(r),
                    center.Y + radius * Math.Sin(r)
                ));
            }
            // 闭合
            stylusPoints.Add(new StylusPoint(center.X + radius, center.Y));

            var stroke = new Stroke(stylusPoints) { DrawingAttributes = context.DrawingAttributes.Clone() };
            strokes.Add(stroke);
            
            return strokes;
        }
    }

    /// <summary>
    /// 中心圆形带半径形状绘制器
    /// </summary>
    public class CenterCircleWithRadiusShapeDrawer : BaseShapeDrawer {
        public override ShapeDrawingType ShapeType => ShapeDrawingType.CenterCircleWithRadius;

        public override StrokeCollection Draw(ShapeDrawingContext context) {
            var strokes = new StrokeCollection();
            
            if (!ValidateContext(context)) return strokes;

            Point center = context.StartPoint;
            Point edge = context.EndPoint;

            double radius = GetDistance(center, edge);
            if (radius < 1) return strokes;

            // 圆形
            var circlePoints = new StylusPointCollection();
            const double step = 0.01;
            for (double r = 0; r <= 2 * Math.PI; r += step) {
                circlePoints.Add(new StylusPoint(
                    center.X + radius * Math.Cos(r),
                    center.Y + radius * Math.Sin(r)
                ));
            }
            circlePoints.Add(new StylusPoint(center.X + radius, center.Y));
            strokes.Add(new Stroke(circlePoints) { DrawingAttributes = context.DrawingAttributes.Clone() });

            // 半径线
            strokes.Add(CreateLineStroke(center, edge, context.DrawingAttributes));

            // 圆心点
            strokes.Add(CreatePointStroke(center, context.DrawingAttributes));

            return strokes;
        }
    }

    /// <summary>
    /// 虚线圆形形状绘制器
    /// </summary>
    public class DashedCircleShapeDrawer : BaseShapeDrawer {
        public override ShapeDrawingType ShapeType => ShapeDrawingType.DashedCircle;

        /// <summary>
        /// 虚线段长度
        /// </summary>
        public double DashLength { get; set; } = 5;

        /// <summary>
        /// 间隔倍数
        /// </summary>
        public double GapMultiplier { get; set; } = 2.76;

        public override StrokeCollection Draw(ShapeDrawingContext context) {
            var strokes = new StrokeCollection();
            
            if (!ValidateContext(context)) return strokes;

            Point start = context.StartPoint;
            Point end = context.EndPoint;

            // 计算半径
            double width = Math.Abs(end.X - start.X);
            double height = Math.Abs(end.Y - start.Y);
            double radius = Math.Min(width, height) / 2;

            double centerX = (start.X + end.X) / 2;
            double centerY = (start.Y + end.Y) / 2;

            // 计算圆周长和虚线数量
            double circumference = 2 * Math.PI * radius;
            double dashAngle = (DashLength / circumference) * 2 * Math.PI;
            double gapAngle = dashAngle * GapMultiplier;
            double totalAngle = dashAngle + gapAngle;

            const double step = 0.02;
            for (double startAngle = 0; startAngle < 2 * Math.PI; startAngle += totalAngle) {
                var dashPoints = new StylusPointCollection();
                double endAngle = Math.Min(startAngle + dashAngle, 2 * Math.PI);
                
                for (double r = startAngle; r <= endAngle; r += step) {
                    dashPoints.Add(new StylusPoint(
                        centerX + radius * Math.Cos(r),
                        centerY + radius * Math.Sin(r)
                    ));
                }
                
                if (dashPoints.Count >= 2) {
                    strokes.Add(new Stroke(dashPoints) { DrawingAttributes = context.DrawingAttributes.Clone() });
                }
            }

            return strokes;
        }
    }

    /// <summary>
    /// 中心椭圆形状绘制器
    /// </summary>
    public class CenterEllipseShapeDrawer : BaseShapeDrawer {
        public override ShapeDrawingType ShapeType => ShapeDrawingType.CenterEllipse;

        public override StrokeCollection Draw(ShapeDrawingContext context) {
            var strokes = new StrokeCollection();
            
            if (!ValidateContext(context)) return strokes;

            Point center = context.StartPoint;
            Point corner = context.EndPoint;

            double a = Math.Abs(corner.X - center.X);
            double b = Math.Abs(corner.Y - center.Y);
            
            if (a < 1 || b < 1) return strokes;

            // 生成椭圆点
            var stylusPoints = new StylusPointCollection();
            const double step = 0.01;
            for (double r = 0; r <= 2 * Math.PI; r += step) {
                stylusPoints.Add(new StylusPoint(
                    center.X + a * Math.Cos(r),
                    center.Y + b * Math.Sin(r)
                ));
            }
            stylusPoints.Add(new StylusPoint(center.X + a, center.Y));

            strokes.Add(new Stroke(stylusPoints) { DrawingAttributes = context.DrawingAttributes.Clone() });

            // 圆心点
            strokes.Add(CreatePointStroke(center, context.DrawingAttributes));

            return strokes;
        }
    }

    /// <summary>
    /// 中心椭圆带焦点形状绘制器
    /// </summary>
    public class CenterEllipseWithFocalPointShapeDrawer : BaseShapeDrawer {
        public override ShapeDrawingType ShapeType => ShapeDrawingType.CenterEllipseWithFocalPoint;

        public override StrokeCollection Draw(ShapeDrawingContext context) {
            var strokes = new StrokeCollection();
            
            if (!ValidateContext(context)) return strokes;

            Point center = context.StartPoint;
            Point corner = context.EndPoint;

            double a = Math.Abs(corner.X - center.X);
            double b = Math.Abs(corner.Y - center.Y);
            
            if (a < 1 || b < 1) return strokes;

            // 生成椭圆点
            var stylusPoints = new StylusPointCollection();
            const double step = 0.01;
            for (double r = 0; r <= 2 * Math.PI; r += step) {
                stylusPoints.Add(new StylusPoint(
                    center.X + a * Math.Cos(r),
                    center.Y + b * Math.Sin(r)
                ));
            }
            stylusPoints.Add(new StylusPoint(center.X + a, center.Y));
            strokes.Add(new Stroke(stylusPoints) { DrawingAttributes = context.DrawingAttributes.Clone() });

            // 圆心点
            strokes.Add(CreatePointStroke(center, context.DrawingAttributes));

            // 计算焦点（c² = |a² - b²|）
            double c = Math.Sqrt(Math.Abs(a * a - b * b));
            
            if (a > b) {
                // 横椭圆，焦点在X轴上
                strokes.Add(CreatePointStroke(new Point(center.X - c, center.Y), context.DrawingAttributes));
                strokes.Add(CreatePointStroke(new Point(center.X + c, center.Y), context.DrawingAttributes));
            } else {
                // 竖椭圆，焦点在Y轴上
                strokes.Add(CreatePointStroke(new Point(center.X, center.Y - c), context.DrawingAttributes));
                strokes.Add(CreatePointStroke(new Point(center.X, center.Y + c), context.DrawingAttributes));
            }

            return strokes;
        }
    }
}