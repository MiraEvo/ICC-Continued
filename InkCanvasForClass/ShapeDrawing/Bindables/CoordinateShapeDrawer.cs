using System;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using Ink_Canvas.ShapeDrawing.Core;

namespace Ink_Canvas.ShapeDrawing.Bindables {
    /// <summary>
    /// 坐标轴1形状绘制器（双向延伸坐标系）
    /// 对应原始代码 case 11：X轴双向，Y轴双向
    /// </summary>
    public class Coordinate1ShapeDrawer : BaseShapeDrawer {
        public override ShapeDrawingType ShapeType => ShapeDrawingType.Coordinate1;

        public override StrokeCollection Draw(ShapeDrawingContext context) {
            var strokes = new StrokeCollection();
            
            if (!ValidateContext(context)) return strokes;

            Point iniP = context.StartPoint;
            Point endP = context.EndPoint;

            // X轴：从 (2*iniP.X - (endP.X - 20), iniP.Y) 到 (endP.X, iniP.Y)
            // 即以iniP为原点，向右延伸到endP.X，向左延伸同等距离
            strokes.Add(CreateArrowLineStroke(
                new Point(2 * iniP.X - (endP.X - 20), iniP.Y),
                new Point(endP.X, iniP.Y),
                context.DrawingAttributes
            ));
            
            // Y轴：从 (iniP.X, 2*iniP.Y - (endP.Y + 20)) 到 (iniP.X, endP.Y)
            strokes.Add(CreateArrowLineStroke(
                new Point(iniP.X, 2 * iniP.Y - (endP.Y + 20)),
                new Point(iniP.X, endP.Y),
                context.DrawingAttributes
            ));

            return strokes;
        }
    }

    /// <summary>
    /// 坐标轴2形状绘制器（X轴单向）
    /// 对应原始代码 case 12：X轴单向，Y轴双向
    /// </summary>
    public class Coordinate2ShapeDrawer : BaseShapeDrawer {
        public override ShapeDrawingType ShapeType => ShapeDrawingType.Coordinate2;

        public override StrokeCollection Draw(ShapeDrawingContext context) {
            var strokes = new StrokeCollection();
            
            if (!ValidateContext(context)) return strokes;

            Point iniP = context.StartPoint;
            Point endP = context.EndPoint;

            if (Math.Abs(iniP.X - endP.X) < 0.01) return strokes;

            // X轴：从 (iniP.X + 偏移, iniP.Y) 到 (endP.X, iniP.Y)
            strokes.Add(CreateArrowLineStroke(
                new Point(iniP.X + (iniP.X - endP.X) / Math.Abs(iniP.X - endP.X) * 25, iniP.Y),
                new Point(endP.X, iniP.Y),
                context.DrawingAttributes
            ));
            
            // Y轴：从 (iniP.X, 2*iniP.Y - (endP.Y + 20)) 到 (iniP.X, endP.Y)
            strokes.Add(CreateArrowLineStroke(
                new Point(iniP.X, 2 * iniP.Y - (endP.Y + 20)),
                new Point(iniP.X, endP.Y),
                context.DrawingAttributes
            ));

            return strokes;
        }
    }

    /// <summary>
    /// 坐标轴3形状绘制器（Y轴单向）
    /// 对应原始代码 case 13：X轴双向，Y轴单向
    /// </summary>
    public class Coordinate3ShapeDrawer : BaseShapeDrawer {
        public override ShapeDrawingType ShapeType => ShapeDrawingType.Coordinate3;

        public override StrokeCollection Draw(ShapeDrawingContext context) {
            var strokes = new StrokeCollection();
            
            if (!ValidateContext(context)) return strokes;

            Point iniP = context.StartPoint;
            Point endP = context.EndPoint;

            if (Math.Abs(iniP.Y - endP.Y) < 0.01) return strokes;

            // X轴：从 (2*iniP.X - (endP.X - 20), iniP.Y) 到 (endP.X, iniP.Y)
            strokes.Add(CreateArrowLineStroke(
                new Point(2 * iniP.X - (endP.X - 20), iniP.Y),
                new Point(endP.X, iniP.Y),
                context.DrawingAttributes
            ));
            
            // Y轴：从 (iniP.X, iniP.Y + 偏移) 到 (iniP.X, endP.Y)
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
    /// 对应原始代码 case 14：X轴单向，Y轴单向
    /// </summary>
    public class Coordinate4ShapeDrawer : BaseShapeDrawer {
        public override ShapeDrawingType ShapeType => ShapeDrawingType.Coordinate4;

        public override StrokeCollection Draw(ShapeDrawingContext context) {
            var strokes = new StrokeCollection();
            
            if (!ValidateContext(context)) return strokes;

            Point iniP = context.StartPoint;
            Point endP = context.EndPoint;

            if (Math.Abs(iniP.X - endP.X) < 0.01 || Math.Abs(iniP.Y - endP.Y) < 0.01) return strokes;

            // X轴：从 (iniP.X + 偏移, iniP.Y) 到 (endP.X, iniP.Y)
            strokes.Add(CreateArrowLineStroke(
                new Point(iniP.X + (iniP.X - endP.X) / Math.Abs(iniP.X - endP.X) * 25, iniP.Y),
                new Point(endP.X, iniP.Y),
                context.DrawingAttributes
            ));
            
            // Y轴：从 (iniP.X, iniP.Y + 偏移) 到 (iniP.X, endP.Y)
            strokes.Add(CreateArrowLineStroke(
                new Point(iniP.X, iniP.Y + (iniP.Y - endP.Y) / Math.Abs(iniP.Y - endP.Y) * 25),
                new Point(iniP.X, endP.Y),
                context.DrawingAttributes
            ));

            return strokes;
        }
    }

    /// <summary>
    /// 坐标轴5形状绘制器（3D坐标轴）
    /// 对应原始代码 case 17
    /// </summary>
    public class Coordinate5ShapeDrawer : BaseShapeDrawer {
        public override ShapeDrawingType ShapeType => ShapeDrawingType.Coordinate5;

        public override StrokeCollection Draw(ShapeDrawingContext context) {
            var strokes = new StrokeCollection();
            
            if (!ValidateContext(context)) return strokes;

            Point iniP = context.StartPoint;
            Point endP = context.EndPoint;

            // 计算轴长度
            double d = (Math.Abs(iniP.X - endP.X) + Math.Abs(iniP.Y - endP.Y)) / 2;

            // X轴：从 (iniP.X, iniP.Y) 到 (iniP.X + |endP.X - iniP.X|, iniP.Y)
            strokes.Add(CreateArrowLineStroke(
                new Point(iniP.X, iniP.Y),
                new Point(iniP.X + Math.Abs(endP.X - iniP.X), iniP.Y),
                context.DrawingAttributes
            ));
            
            // Y轴：从 (iniP.X, iniP.Y) 到 (iniP.X, iniP.Y - |endP.Y - iniP.Y|)
            strokes.Add(CreateArrowLineStroke(
                new Point(iniP.X, iniP.Y),
                new Point(iniP.X, iniP.Y - Math.Abs(endP.Y - iniP.Y)),
                context.DrawingAttributes
            ));
            
            // Z轴：从 (iniP.X, iniP.Y) 到 (iniP.X - d/1.76, iniP.Y + d/1.76)
            strokes.Add(CreateArrowLineStroke(
                new Point(iniP.X, iniP.Y),
                new Point(iniP.X - d / 1.76, iniP.Y + d / 1.76),
                context.DrawingAttributes
            ));

            return strokes;
        }
    }
}