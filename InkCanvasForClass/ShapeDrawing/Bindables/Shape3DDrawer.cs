using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using Ink_Canvas.ShapeDrawing.Core;

namespace Ink_Canvas.ShapeDrawing.Bindables {
    /// <summary>
    /// 圆柱体形状绘制器
    /// </summary>
    public class CylinderShapeDrawer : BaseShapeDrawer {
        public override ShapeDrawingType ShapeType => ShapeDrawingType.Cylinder;

        /// <summary>
        /// 椭圆压缩比（用于模拟透视效果）
        /// </summary>
        public double EllipseRatio { get; set; } = 0.3;

        public override StrokeCollection Draw(ShapeDrawingContext context) {
            var strokes = new StrokeCollection();
            
            if (!ValidateContext(context)) return strokes;

            Point start = context.StartPoint;
            Point end = context.EndPoint;

            double left = Math.Min(start.X, end.X);
            double right = Math.Max(start.X, end.X);
            double top = Math.Min(start.Y, end.Y);
            double bottom = Math.Max(start.Y, end.Y);

            double width = right - left;
            double height = bottom - top;

            if (width < 2 || height < 2) return strokes;

            double centerX = (left + right) / 2;
            double a = width / 2;  // 椭圆长轴
            double b = a * EllipseRatio;  // 椭圆短轴

            // 调整顶部和底部椭圆的位置
            double topEllipseY = top + b;
            double bottomEllipseY = bottom - b;

            // 绘制顶部椭圆（完整）
            var topEllipse = GenerateEllipsePointsForCylinder(centerX, topEllipseY, a, b);
            if (topEllipse.Count >= 2) {
                var topPoints = new StylusPointCollection();
                foreach (var p in topEllipse) {
                    topPoints.Add(new StylusPoint(p.X, p.Y));
                }
                topPoints.Add(new StylusPoint(topEllipse[0].X, topEllipse[0].Y));
                strokes.Add(new Stroke(topPoints) { DrawingAttributes = context.DrawingAttributes.Clone() });
            }

            // 绘制底部椭圆（只绘制下半部分，上半部分被遮挡）
            var bottomHalfEllipse = GenerateHalfEllipsePoints(centerX, bottomEllipseY, a, b, false);
            if (bottomHalfEllipse.Count >= 2) {
                var bottomPoints = new StylusPointCollection();
                foreach (var p in bottomHalfEllipse) {
                    bottomPoints.Add(new StylusPoint(p.X, p.Y));
                }
                strokes.Add(new Stroke(bottomPoints) { DrawingAttributes = context.DrawingAttributes.Clone() });
            }

            // 绘制底部椭圆上半部分（虚线，表示被遮挡）
            var topHalfBottom = GenerateHalfEllipsePoints(centerX, bottomEllipseY, a, b, true);
            if (topHalfBottom.Count >= 2) {
                // 使用虚线绘制
                for (int i = 0; i < topHalfBottom.Count - 1; i += 2) {
                    if (i + 1 < topHalfBottom.Count) {
                        var dashPoints = new StylusPointCollection {
                            new StylusPoint(topHalfBottom[i].X, topHalfBottom[i].Y),
                            new StylusPoint(topHalfBottom[Math.Min(i + 1, topHalfBottom.Count - 1)].X, 
                                           topHalfBottom[Math.Min(i + 1, topHalfBottom.Count - 1)].Y)
                        };
                        strokes.Add(new Stroke(dashPoints) { DrawingAttributes = context.DrawingAttributes.Clone() });
                    }
                }
            }

            // 绘制左侧边线
            strokes.Add(CreateLineStroke(
                new Point(left, topEllipseY),
                new Point(left, bottomEllipseY),
                context.DrawingAttributes
            ));

            // 绘制右侧边线
            strokes.Add(CreateLineStroke(
                new Point(right, topEllipseY),
                new Point(right, bottomEllipseY),
                context.DrawingAttributes
            ));

            return strokes;
        }

        private List<Point> GenerateEllipsePointsForCylinder(double centerX, double centerY, double a, double b) {
            var points = new List<Point>();
            const double step = 0.05;
            for (double r = 0; r <= 2 * Math.PI; r += step) {
                points.Add(new Point(centerX + a * Math.Cos(r), centerY + b * Math.Sin(r)));
            }
            return points;
        }

        private List<Point> GenerateHalfEllipsePoints(double centerX, double centerY, double a, double b, bool topHalf) {
            var points = new List<Point>();
            const double step = 0.05;
            double startAngle = topHalf ? Math.PI : 0;
            double endAngle = topHalf ? 2 * Math.PI : Math.PI;
            
            for (double r = startAngle; r <= endAngle; r += step) {
                points.Add(new Point(centerX + a * Math.Cos(r), centerY + b * Math.Sin(r)));
            }
            return points;
        }
    }

    /// <summary>
    /// 圆锥体形状绘制器
    /// </summary>
    public class ConeShapeDrawer : BaseShapeDrawer {
        public override ShapeDrawingType ShapeType => ShapeDrawingType.Cone;

        /// <summary>
        /// 底部椭圆压缩比
        /// </summary>
        public double EllipseRatio { get; set; } = 0.3;

        public override StrokeCollection Draw(ShapeDrawingContext context) {
            var strokes = new StrokeCollection();
            
            if (!ValidateContext(context)) return strokes;

            Point start = context.StartPoint;
            Point end = context.EndPoint;

            double left = Math.Min(start.X, end.X);
            double right = Math.Max(start.X, end.X);
            double top = Math.Min(start.Y, end.Y);
            double bottom = Math.Max(start.Y, end.Y);

            double width = right - left;
            double height = bottom - top;

            if (width < 2 || height < 2) return strokes;

            double centerX = (left + right) / 2;
            double a = width / 2;
            double b = a * EllipseRatio;

            // 顶点位置
            Point apex = new Point(centerX, top);
            
            // 底部椭圆位置
            double bottomEllipseY = bottom - b;

            // 绘制底部椭圆下半部分（可见）
            var bottomHalfEllipse = GenerateHalfEllipsePoints(centerX, bottomEllipseY, a, b, false);
            if (bottomHalfEllipse.Count >= 2) {
                var bottomPoints = new StylusPointCollection();
                foreach (var p in bottomHalfEllipse) {
                    bottomPoints.Add(new StylusPoint(p.X, p.Y));
                }
                strokes.Add(new Stroke(bottomPoints) { DrawingAttributes = context.DrawingAttributes.Clone() });
            }

            // 绘制底部椭圆上半部分（虚线，被遮挡）
            var topHalfBottom = GenerateHalfEllipsePoints(centerX, bottomEllipseY, a, b, true);
            if (topHalfBottom.Count >= 2) {
                for (int i = 0; i < topHalfBottom.Count - 1; i += 2) {
                    if (i + 1 < topHalfBottom.Count) {
                        var dashPoints = new StylusPointCollection {
                            new StylusPoint(topHalfBottom[i].X, topHalfBottom[i].Y),
                            new StylusPoint(topHalfBottom[Math.Min(i + 1, topHalfBottom.Count - 1)].X, 
                                           topHalfBottom[Math.Min(i + 1, topHalfBottom.Count - 1)].Y)
                        };
                        strokes.Add(new Stroke(dashPoints) { DrawingAttributes = context.DrawingAttributes.Clone() });
                    }
                }
            }

            // 绘制左侧边线（从顶点到底部椭圆左端）
            strokes.Add(CreateLineStroke(apex, new Point(left, bottomEllipseY), context.DrawingAttributes));

            // 绘制右侧边线（从顶点到底部椭圆右端）
            strokes.Add(CreateLineStroke(apex, new Point(right, bottomEllipseY), context.DrawingAttributes));

            return strokes;
        }

        private List<Point> GenerateHalfEllipsePoints(double centerX, double centerY, double a, double b, bool topHalf) {
            var points = new List<Point>();
            const double step = 0.05;
            double startAngle = topHalf ? Math.PI : 0;
            double endAngle = topHalf ? 2 * Math.PI : Math.PI;
            
            for (double r = startAngle; r <= endAngle; r += step) {
                points.Add(new Point(centerX + a * Math.Cos(r), centerY + b * Math.Sin(r)));
            }
            return points;
        }
    }

    /// <summary>
    /// 长方体形状绘制器
    /// </summary>
    public class CuboidShapeDrawer : BaseShapeDrawer {
        public override ShapeDrawingType ShapeType => ShapeDrawingType.Cuboid;

        /// <summary>
        /// 透视深度比例
        /// </summary>
        public double DepthRatio { get; set; } = 0.3;

        /// <summary>
        /// 透视角度（弧度）
        /// </summary>
        public double PerspectiveAngle { get; set; } = Math.PI / 4; // 45度

        public override StrokeCollection Draw(ShapeDrawingContext context) {
            var strokes = new StrokeCollection();
            
            if (!ValidateContext(context)) return strokes;

            Point start = context.StartPoint;
            Point end = context.EndPoint;

            double left = Math.Min(start.X, end.X);
            double right = Math.Max(start.X, end.X);
            double top = Math.Min(start.Y, end.Y);
            double bottom = Math.Max(start.Y, end.Y);

            double width = right - left;
            double height = bottom - top;

            if (width < 2 || height < 2) return strokes;

            // 计算透视偏移
            double depth = Math.Min(width, height) * DepthRatio;
            double offsetX = depth * Math.Cos(PerspectiveAngle);
            double offsetY = depth * Math.Sin(PerspectiveAngle);

            // 前面四个顶点
            Point frontTopLeft = new Point(left, top + offsetY);
            Point frontTopRight = new Point(right, top + offsetY);
            Point frontBottomLeft = new Point(left, bottom);
            Point frontBottomRight = new Point(right, bottom);

            // 后面四个顶点
            Point backTopLeft = new Point(left + offsetX, top);
            Point backTopRight = new Point(right + offsetX, top);
            Point backBottomLeft = new Point(left + offsetX, bottom - offsetY);
            Point backBottomRight = new Point(right + offsetX, bottom - offsetY);

            // 绘制前面（实线）
            var frontFace = new StylusPointCollection {
                new StylusPoint(frontTopLeft.X, frontTopLeft.Y),
                new StylusPoint(frontTopRight.X, frontTopRight.Y),
                new StylusPoint(frontBottomRight.X, frontBottomRight.Y),
                new StylusPoint(frontBottomLeft.X, frontBottomLeft.Y),
                new StylusPoint(frontTopLeft.X, frontTopLeft.Y)
            };
            strokes.Add(new Stroke(frontFace) { DrawingAttributes = context.DrawingAttributes.Clone() });

            // 绘制顶面
            var topFace = new StylusPointCollection {
                new StylusPoint(frontTopLeft.X, frontTopLeft.Y),
                new StylusPoint(backTopLeft.X, backTopLeft.Y),
                new StylusPoint(backTopRight.X, backTopRight.Y),
                new StylusPoint(frontTopRight.X, frontTopRight.Y)
            };
            strokes.Add(new Stroke(topFace) { DrawingAttributes = context.DrawingAttributes.Clone() });

            // 绘制右侧面
            var rightFace = new StylusPointCollection {
                new StylusPoint(frontTopRight.X, frontTopRight.Y),
                new StylusPoint(backTopRight.X, backTopRight.Y),
                new StylusPoint(backBottomRight.X, backBottomRight.Y),
                new StylusPoint(frontBottomRight.X, frontBottomRight.Y)
            };
            strokes.Add(new Stroke(rightFace) { DrawingAttributes = context.DrawingAttributes.Clone() });

            // 绘制被遮挡的边（虚线）
            // 后左竖线
            var dashedStrokes = GenerateDashedLineStrokes(backTopLeft, backBottomLeft, context.DrawingAttributes, 5, 2);
            foreach (var stroke in dashedStrokes) {
                strokes.Add(stroke);
            }

            // 后底线
            dashedStrokes = GenerateDashedLineStrokes(backBottomLeft, backBottomRight, context.DrawingAttributes, 5, 2);
            foreach (var stroke in dashedStrokes) {
                strokes.Add(stroke);
            }

            // 左后竖线到前左底
            dashedStrokes = GenerateDashedLineStrokes(frontBottomLeft, backBottomLeft, context.DrawingAttributes, 5, 2);
            foreach (var stroke in dashedStrokes) {
                strokes.Add(stroke);
            }

            return strokes;
        }
    }
}