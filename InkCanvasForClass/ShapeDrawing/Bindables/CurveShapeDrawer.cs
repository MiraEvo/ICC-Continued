using System;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using Ink_Canvas.ShapeDrawing.Core;

namespace Ink_Canvas.ShapeDrawing.Bindables {
    /// <summary>
    /// 双曲线形状绘制器
    /// </summary>
    public class HyperbolaShapeDrawer : BaseShapeDrawer {
        public override ShapeDrawingType ShapeType => ShapeDrawingType.Hyperbola;

        public override StrokeCollection Draw(ShapeDrawingContext context) {
            var strokes = new StrokeCollection();
            
            if (!ValidateContext(context)) return strokes;

            Point start = context.StartPoint;
            Point end = context.EndPoint;

            double left = Math.Min(start.X, end.X);
            double right = Math.Max(start.X, end.X);
            double top = Math.Min(start.Y, end.Y);
            double bottom = Math.Max(start.Y, end.Y);

            double centerX = (left + right) / 2;
            double centerY = (top + bottom) / 2;

            // a是实半轴，b是虚半轴
            double a = (right - left) / 4;  // 取宽度的1/4作为a
            double b = (bottom - top) / 2;  // 取高度的1/2作为b

            if (a < 1 || b < 1) return strokes;

            // 绘制右支
            var rightBranch = new StylusPointCollection();
            double step = 0.05;
            for (double t = -2; t <= 2; t += step) {
                double x = centerX + a * Math.Cosh(t);
                double y = centerY + b * Math.Sinh(t);
                if (y >= top && y <= bottom) {
                    rightBranch.Add(new StylusPoint(x, y));
                }
            }
            if (rightBranch.Count >= 2) {
                strokes.Add(new Stroke(rightBranch) { DrawingAttributes = context.DrawingAttributes.Clone() });
            }

            // 绘制左支
            var leftBranch = new StylusPointCollection();
            for (double t = -2; t <= 2; t += step) {
                double x = centerX - a * Math.Cosh(t);
                double y = centerY + b * Math.Sinh(t);
                if (y >= top && y <= bottom) {
                    leftBranch.Add(new StylusPoint(x, y));
                }
            }
            if (leftBranch.Count >= 2) {
                strokes.Add(new Stroke(leftBranch) { DrawingAttributes = context.DrawingAttributes.Clone() });
            }

            return strokes;
        }
    }

    /// <summary>
    /// 双曲线带焦点形状绘制器
    /// </summary>
    public class HyperbolaWithFocalPointShapeDrawer : BaseShapeDrawer {
        public override ShapeDrawingType ShapeType => ShapeDrawingType.HyperbolaWithFocalPoint;

        public override StrokeCollection Draw(ShapeDrawingContext context) {
            var strokes = new StrokeCollection();
            
            if (!ValidateContext(context)) return strokes;

            Point start = context.StartPoint;
            Point end = context.EndPoint;

            double left = Math.Min(start.X, end.X);
            double right = Math.Max(start.X, end.X);
            double top = Math.Min(start.Y, end.Y);
            double bottom = Math.Max(start.Y, end.Y);

            double centerX = (left + right) / 2;
            double centerY = (top + bottom) / 2;

            double a = (right - left) / 4;
            double b = (bottom - top) / 2;

            if (a < 1 || b < 1) return strokes;

            // 绘制双曲线两支
            double step = 0.05;
            
            // 右支
            var rightBranch = new StylusPointCollection();
            for (double t = -2; t <= 2; t += step) {
                double x = centerX + a * Math.Cosh(t);
                double y = centerY + b * Math.Sinh(t);
                if (y >= top && y <= bottom) {
                    rightBranch.Add(new StylusPoint(x, y));
                }
            }
            if (rightBranch.Count >= 2) {
                strokes.Add(new Stroke(rightBranch) { DrawingAttributes = context.DrawingAttributes.Clone() });
            }

            // 左支
            var leftBranch = new StylusPointCollection();
            for (double t = -2; t <= 2; t += step) {
                double x = centerX - a * Math.Cosh(t);
                double y = centerY + b * Math.Sinh(t);
                if (y >= top && y <= bottom) {
                    leftBranch.Add(new StylusPoint(x, y));
                }
            }
            if (leftBranch.Count >= 2) {
                strokes.Add(new Stroke(leftBranch) { DrawingAttributes = context.DrawingAttributes.Clone() });
            }

            // 计算焦点 c² = a² + b²
            double c = Math.Sqrt(a * a + b * b);

            // 绘制焦点
            strokes.Add(CreatePointStroke(new Point(centerX - c, centerY), context.DrawingAttributes));
            strokes.Add(CreatePointStroke(new Point(centerX + c, centerY), context.DrawingAttributes));

            // 绘制中心点
            strokes.Add(CreatePointStroke(new Point(centerX, centerY), context.DrawingAttributes));

            return strokes;
        }
    }

    /// <summary>
    /// 抛物线1形状绘制器（y = ax² 形式，开口向下/上）
    /// 对应原始代码 case 20
    /// </summary>
    public class Parabola1ShapeDrawer : BaseShapeDrawer {
        public override ShapeDrawingType ShapeType => ShapeDrawingType.Parabola1;

        public override StrokeCollection Draw(ShapeDrawingContext context) {
            var strokes = new StrokeCollection();
            
            if (!ValidateContext(context)) return strokes;

            Point iniP = context.StartPoint;
            Point endP = context.EndPoint;

            if (Math.Abs(iniP.X - endP.X) < 0.01 || Math.Abs(iniP.Y - endP.Y) < 0.01) return strokes;

            // 计算抛物线参数 a：y - iniP.Y = a * (x - iniP.X)²
            // 当 x = endP.X 时，y = endP.Y
            // endP.Y - iniP.Y = a * (endP.X - iniP.X)²
            double a = (iniP.Y - endP.Y) / ((iniP.X - endP.X) * (iniP.X - endP.X));

            var pointList1 = new StylusPointCollection();
            var pointList2 = new StylusPointCollection();
            
            double step = 0.5;
            double range = Math.Abs(endP.X - iniP.X);
            
            // 从顶点向两侧绘制
            for (double i = 0; i <= range; i += step) {
                double y1 = iniP.Y - a * i * i;
                pointList1.Add(new StylusPoint(iniP.X + i, y1));
                pointList2.Add(new StylusPoint(iniP.X - i, y1));
            }

            if (pointList1.Count >= 2) {
                strokes.Add(new Stroke(pointList1) { DrawingAttributes = context.DrawingAttributes.Clone() });
            }
            if (pointList2.Count >= 2) {
                strokes.Add(new Stroke(pointList2) { DrawingAttributes = context.DrawingAttributes.Clone() });
            }

            return strokes;
        }
    }

    /// <summary>
    /// 抛物线2形状绘制器（x = ay² 形式，开口向左/右）
    /// 对应原始代码 case 21
    /// </summary>
    public class Parabola2ShapeDrawer : BaseShapeDrawer {
        public override ShapeDrawingType ShapeType => ShapeDrawingType.Parabola2;

        public override StrokeCollection Draw(ShapeDrawingContext context) {
            var strokes = new StrokeCollection();
            
            if (!ValidateContext(context)) return strokes;

            Point iniP = context.StartPoint;
            Point endP = context.EndPoint;

            if (Math.Abs(iniP.X - endP.X) < 0.01 || Math.Abs(iniP.Y - endP.Y) < 0.01) return strokes;

            // 计算抛物线参数 a：x - iniP.X = a * (y - iniP.Y)²
            double a = (iniP.X - endP.X) / ((iniP.Y - endP.Y) * (iniP.Y - endP.Y));

            var pointList1 = new StylusPointCollection();
            var pointList2 = new StylusPointCollection();
            
            double step = 0.5;
            double range = Math.Abs(endP.Y - iniP.Y);
            
            // 从顶点向两侧绘制
            for (double i = 0; i <= range; i += step) {
                double x1 = iniP.X - a * i * i;
                pointList1.Add(new StylusPoint(x1, iniP.Y + i));
                pointList2.Add(new StylusPoint(x1, iniP.Y - i));
            }

            if (pointList1.Count >= 2) {
                strokes.Add(new Stroke(pointList1) { DrawingAttributes = context.DrawingAttributes.Clone() });
            }
            if (pointList2.Count >= 2) {
                strokes.Add(new Stroke(pointList2) { DrawingAttributes = context.DrawingAttributes.Clone() });
            }

            return strokes;
        }
    }

    /// <summary>
    /// 抛物线带焦点形状绘制器（x = ay² 形式，带焦点和准线）
    /// 对应原始代码 case 22
    /// </summary>
    public class ParabolaWithFocalPointShapeDrawer : BaseShapeDrawer {
        public override ShapeDrawingType ShapeType => ShapeDrawingType.ParabolaWithFocalPoint;

        public override StrokeCollection Draw(ShapeDrawingContext context) {
            var strokes = new StrokeCollection();
            
            if (!ValidateContext(context)) return strokes;

            Point iniP = context.StartPoint;
            Point endP = context.EndPoint;

            if (Math.Abs(iniP.X - endP.X) < 0.01 || Math.Abs(iniP.Y - endP.Y) < 0.01) return strokes;

            // 根据原始代码 case 22 的逻辑
            // p = (iniP.Y - endP.Y)² / (2 * (iniP.X - endP.X))
            double p = (iniP.Y - endP.Y) * (iniP.Y - endP.Y) / (2 * (iniP.X - endP.X));
            double a = 0.5 / p;

            var pointList1 = new StylusPointCollection();
            var pointList2 = new StylusPointCollection();
            
            double step = 0.5;
            double range = Math.Abs(endP.Y - iniP.Y);
            
            // 从顶点向两侧绘制
            for (double i = 0; i <= range; i += step) {
                double x1 = iniP.X - a * i * i;
                pointList1.Add(new StylusPoint(x1, iniP.Y + i));
                pointList2.Add(new StylusPoint(x1, iniP.Y - i));
            }

            if (pointList1.Count >= 2) {
                strokes.Add(new Stroke(pointList1) { DrawingAttributes = context.DrawingAttributes.Clone() });
            }
            if (pointList2.Count >= 2) {
                strokes.Add(new Stroke(pointList2) { DrawingAttributes = context.DrawingAttributes.Clone() });
            }

            // 绘制焦点（在顶点左边 p/2 处，根据原始代码）
            Point focus = new Point(iniP.X - p / 2, iniP.Y);
            strokes.Add(CreatePointStroke(focus, context.DrawingAttributes));

            return strokes;
        }
    }
}