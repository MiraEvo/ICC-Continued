using System;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using Ink_Canvas.ShapeDrawing.Core;

namespace Ink_Canvas.ShapeDrawing.Bindables {
    /// <summary>
    /// 坐标轴1形状绘制器（双向延伸）
    /// 对应原始代码 case 11
    /// </summary>
    public class Coordinate1ShapeDrawer : BaseShapeDrawer {
        public override ShapeDrawingType ShapeType => ShapeDrawingType.Coordinate1;

        public override StrokeCollection Draw(ShapeDrawingContext context) {
            var strokes = new StrokeCollection();

            if (!ValidateContext(context)) return strokes;

            Point iniP = context.StartPoint;
            Point endP = context.EndPoint;

            // X轴：双向箭头
            strokes.Add(CreateArrowLineStroke(
                new Point(2 * iniP.X - (endP.X - 20), iniP.Y),
                new Point(endP.X, iniP.Y),
                context.DrawingAttributes
            ));

            // Y轴：双向箭头
            strokes.Add(CreateArrowLineStroke(
                new Point(iniP.X, 2 * iniP.Y - (endP.Y + 20)),
                new Point(iniP.X, endP.Y),
                context.DrawingAttributes
            ));

            return strokes;
        }
    }

    /// <summary>
    /// 坐标轴2形状绘制器（X轴单向，Y轴双向）
    /// 对应原始代码 case 12
    /// </summary>
    public class Coordinate2ShapeDrawer : BaseShapeDrawer {
        public override ShapeDrawingType ShapeType => ShapeDrawingType.Coordinate2;

        public override StrokeCollection Draw(ShapeDrawingContext context) {
            var strokes = new StrokeCollection();

            if (!ValidateContext(context)) return strokes;

            Point iniP = context.StartPoint;
            Point endP = context.EndPoint;

            if (Math.Abs(iniP.X - endP.X) < 0.01) return strokes;

            // X轴：单向箭头（从左到右）
            strokes.Add(CreateArrowLineStroke(
                new Point(iniP.X + (iniP.X - endP.X) / Math.Abs(iniP.X - endP.X) * 25, iniP.Y),
                new Point(endP.X, iniP.Y),
                context.DrawingAttributes
            ));

            // Y轴：双向箭头
            strokes.Add(CreateArrowLineStroke(
                new Point(iniP.X, 2 * iniP.Y - (endP.Y + 20)),
                new Point(iniP.X, endP.Y),
                context.DrawingAttributes
            ));

            return strokes;
        }
    }

    /// <summary>
    /// 坐标轴3形状绘制器（X轴双向，Y轴单向）
    /// 对应原始代码 case 13
    /// </summary>
    public class Coordinate3ShapeDrawer : BaseShapeDrawer {
        public override ShapeDrawingType ShapeType => ShapeDrawingType.Coordinate3;

        public override StrokeCollection Draw(ShapeDrawingContext context) {
            var strokes = new StrokeCollection();

            if (!ValidateContext(context)) return strokes;

            Point iniP = context.StartPoint;
            Point endP = context.EndPoint;

            if (Math.Abs(iniP.Y - endP.Y) < 0.01) return strokes;

            // X轴：双向箭头
            strokes.Add(CreateArrowLineStroke(
                new Point(2 * iniP.X - (endP.X - 20), iniP.Y),
                new Point(endP.X, iniP.Y),
                context.DrawingAttributes
            ));

            // Y轴：单向箭头（从下到上）
            strokes.Add(CreateArrowLineStroke(
                new Point(iniP.X, iniP.Y + (iniP.Y - endP.Y) / Math.Abs(iniP.Y - endP.Y) * 25),
                new Point(iniP.X, endP.Y),
                context.DrawingAttributes
            ));

            return strokes;
        }
    }

    /// <summary>
    /// 坐标轴4形状绘制器（双单向）
    /// 对应原始代码 case 14
    /// </summary>
    public class Coordinate4ShapeDrawer : BaseShapeDrawer {
        public override ShapeDrawingType ShapeType => ShapeDrawingType.Coordinate4;

        public override StrokeCollection Draw(ShapeDrawingContext context) {
            var strokes = new StrokeCollection();

            if (!ValidateContext(context)) return strokes;

            Point iniP = context.StartPoint;
            Point endP = context.EndPoint;

            if (Math.Abs(iniP.X - endP.X) < 0.01 || Math.Abs(iniP.Y - endP.Y) < 0.01) return strokes;

            // X轴：单向箭头
            strokes.Add(CreateArrowLineStroke(
                new Point(iniP.X + (iniP.X - endP.X) / Math.Abs(iniP.X - endP.X) * 25, iniP.Y),
                new Point(endP.X, iniP.Y),
                context.DrawingAttributes
            ));

            // Y轴：单向箭头
            strokes.Add(CreateArrowLineStroke(
                new Point(iniP.X, iniP.Y + (iniP.Y - endP.Y) / Math.Abs(iniP.Y - endP.Y) * 25),
                new Point(iniP.X, endP.Y),
                context.DrawingAttributes
            ));

            return strokes;
        }
    }

    /// <summary>
    /// 3D坐标轴形状绘制器
    /// 对应原始代码 case 17
    /// </summary>
    public class Coordinate5ShapeDrawer : BaseShapeDrawer {
        public override ShapeDrawingType ShapeType => ShapeDrawingType.Coordinate5;

        public override StrokeCollection Draw(ShapeDrawingContext context) {
            var strokes = new StrokeCollection();

            if (!ValidateContext(context)) return strokes;

            Point iniP = context.StartPoint;
            Point endP = context.EndPoint;

            // X轴
            strokes.Add(CreateArrowLineStroke(
                new Point(iniP.X, iniP.Y),
                new Point(iniP.X + Math.Abs(endP.X - iniP.X), iniP.Y),
                context.DrawingAttributes
            ));

            // Y轴
            strokes.Add(CreateArrowLineStroke(
                new Point(iniP.X, iniP.Y),
                new Point(iniP.X, iniP.Y - Math.Abs(endP.Y - iniP.Y)),
                context.DrawingAttributes
            ));

            // Z轴（斜向）
            double d = (Math.Abs(iniP.X - endP.X) + Math.Abs(iniP.Y - endP.Y)) / 2;
            strokes.Add(CreateArrowLineStroke(
                new Point(iniP.X, iniP.Y),
                new Point(iniP.X - d / 1.76, iniP.Y + d / 1.76),
                context.DrawingAttributes
            ));

            return strokes;
        }
    }

    /// <summary>
    /// 坐标网格形状绘制器
    /// </summary>
    public class CoordinateGridShapeDrawer : BaseShapeDrawer {
        public override ShapeDrawingType ShapeType => ShapeDrawingType.CoordinateGrid;

        /// <summary>
        /// 网格大小
        /// </summary>
        public double GridSize { get; set; } = 40;

        public override StrokeCollection Draw(ShapeDrawingContext context) {
            var strokes = new StrokeCollection();

            if (!ValidateContext(context)) return strokes;

            Point iniP = context.StartPoint;
            Point endP = context.EndPoint;

            double left = Math.Min(iniP.X, endP.X);
            double right = Math.Max(iniP.X, endP.X);
            double top = Math.Min(iniP.Y, endP.Y);
            double bottom = Math.Max(iniP.Y, endP.Y);

            // 绘制垂直线
            for (double x = left; x <= right; x += GridSize) {
                strokes.Add(CreateLineStroke(
                    new Point(x, top),
                    new Point(x, bottom),
                    context.DrawingAttributes
                ));
            }

            // 绘制水平线
            for (double y = top; y <= bottom; y += GridSize) {
                strokes.Add(CreateLineStroke(
                    new Point(left, y),
                    new Point(right, y),
                    context.DrawingAttributes
                ));
            }

            return strokes;
        }
    }
}
