using System;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using Ink_Canvas.ShapeDrawing.Core;

namespace Ink_Canvas.ShapeDrawing.Bindables {
    /// <summary>
    /// 直线形状绘制器
    /// </summary>
    public class LineShapeDrawer : BaseShapeDrawer {
        public override ShapeDrawingType ShapeType => ShapeDrawingType.Line;

        public override StrokeCollection Draw(ShapeDrawingContext context) {
            var strokes = new StrokeCollection();
            
            if (!ValidateContext(context)) return strokes;

            var stroke = CreateLineStroke(context.StartPoint, context.EndPoint, context.DrawingAttributes);
            strokes.Add(stroke);
            
            return strokes;
        }
    }

    /// <summary>
    /// 虚线形状绘制器
    /// </summary>
    public class DashedLineShapeDrawer : BaseShapeDrawer {
        public override ShapeDrawingType ShapeType => ShapeDrawingType.DashedLine;

        /// <summary>
        /// 虚线段长度
        /// </summary>
        public double DashLength { get; set; } = 5;

        /// <summary>
        /// 间隔倍数
        /// </summary>
        public double GapMultiplier { get; set; } = 2.76;

        public override StrokeCollection Draw(ShapeDrawingContext context) {
            if (!ValidateContext(context)) return new StrokeCollection();

            return GenerateDashedLineStrokes(
                context.StartPoint, 
                context.EndPoint, 
                context.DrawingAttributes,
                DashLength,
                GapMultiplier
            );
        }
    }

    /// <summary>
    /// 点线形状绘制器
    /// </summary>
    public class DottedLineShapeDrawer : BaseShapeDrawer {
        public override ShapeDrawingType ShapeType => ShapeDrawingType.DottedLine;

        /// <summary>
        /// 点间隔
        /// </summary>
        public double DotInterval { get; set; } = 3;

        /// <summary>
        /// 间隔倍数
        /// </summary>
        public double GapMultiplier { get; set; } = 2.76;

        public override StrokeCollection Draw(ShapeDrawingContext context) {
            if (!ValidateContext(context)) return new StrokeCollection();

            return GenerateDottedLineStrokes(
                context.StartPoint, 
                context.EndPoint, 
                context.DrawingAttributes,
                DotInterval,
                GapMultiplier
            );
        }
    }

    /// <summary>
    /// 单向箭头形状绘制器
    /// </summary>
    public class ArrowOneSideShapeDrawer : BaseShapeDrawer {
        public override ShapeDrawingType ShapeType => ShapeDrawingType.ArrowOneSide;

        /// <summary>
        /// 箭头宽度
        /// </summary>
        public double ArrowWidth { get; set; } = 20;

        /// <summary>
        /// 箭头高度
        /// </summary>
        public double ArrowHeight { get; set; } = 7;

        public override StrokeCollection Draw(ShapeDrawingContext context) {
            var strokes = new StrokeCollection();
            
            if (!ValidateContext(context)) return strokes;

            var stroke = CreateArrowLineStroke(
                context.StartPoint, 
                context.EndPoint, 
                context.DrawingAttributes,
                ArrowWidth,
                ArrowHeight
            );
            strokes.Add(stroke);
            
            return strokes;
        }
    }

    /// <summary>
    /// 双向箭头形状绘制器
    /// </summary>
    public class ArrowTwoSideShapeDrawer : BaseShapeDrawer {
        public override ShapeDrawingType ShapeType => ShapeDrawingType.ArrowTwoSide;

        /// <summary>
        /// 箭头宽度
        /// </summary>
        public double ArrowWidth { get; set; } = 20;

        /// <summary>
        /// 箭头高度
        /// </summary>
        public double ArrowHeight { get; set; } = 7;

        public override StrokeCollection Draw(ShapeDrawingContext context) {
            var strokes = new StrokeCollection();
            
            if (!ValidateContext(context)) return strokes;

            var stroke = CreateDoubleArrowLineStroke(
                context.StartPoint, 
                context.EndPoint, 
                context.DrawingAttributes,
                ArrowWidth,
                ArrowHeight
            );
            strokes.Add(stroke);
            
            return strokes;
        }
    }

    /// <summary>
    /// 平行线形状绘制器（4条平行线，间距25）
    /// 对应原始代码 case 15
    /// </summary>
    public class ParallelLineShapeDrawer : BaseShapeDrawer {
        public override ShapeDrawingType ShapeType => ShapeDrawingType.ParallelLine;

        /// <summary>
        /// 基础间距（原始代码 x = 25）
        /// </summary>
        public double BaseDistance { get; set; } = 25;

        public override StrokeCollection Draw(ShapeDrawingContext context) {
            var strokes = new StrokeCollection();
            
            if (!ValidateContext(context)) return strokes;

            Point iniP = context.StartPoint;
            Point endP = context.EndPoint;
            double d = context.GetDistance();
            
            if (d < 0.01) return strokes;

            double sinTheta = (iniP.Y - endP.Y) / d;
            double cosTheta = (endP.X - iniP.X) / d;
            double tanTheta = Math.Abs(sinTheta / cosTheta);
            double x = BaseDistance;

            // 角度吸附逻辑（与原始代码一致）
            // 水平吸附
            if (Math.Abs(tanTheta) < 1.0 / 12) {
                sinTheta = 0;
                cosTheta = 1;
                endP = new Point(endP.X, iniP.Y);
            }
            // 30度吸附
            if (tanTheta < 0.63 && tanTheta > 0.52) {
                sinTheta = sinTheta / Math.Abs(sinTheta) * 0.5;
                cosTheta = cosTheta / Math.Abs(cosTheta) * 0.866;
                endP = new Point(iniP.X + d * cosTheta, iniP.Y - d * sinTheta);
            }
            // 45度吸附
            if (tanTheta < 1.08 && tanTheta > 0.92) {
                sinTheta = sinTheta / Math.Abs(sinTheta) * 0.707;
                cosTheta = cosTheta / Math.Abs(cosTheta) * 0.707;
                endP = new Point(iniP.X + d * cosTheta, iniP.Y - d * sinTheta);
            }
            // 60度吸附
            if (tanTheta < 1.95 && tanTheta > 1.63) {
                sinTheta = sinTheta / Math.Abs(sinTheta) * 0.866;
                cosTheta = cosTheta / Math.Abs(cosTheta) * 0.5;
                endP = new Point(iniP.X + d * cosTheta, iniP.Y - d * sinTheta);
            }
            // 垂直吸附
            if (Math.Abs(cosTheta / sinTheta) < 1.0 / 12) {
                endP = new Point(iniP.X, endP.Y);
                sinTheta = 1;
                cosTheta = 0;
            }

            // 绘制4条平行线（间距 ±x 和 ±3x）
            // 第一条线：偏移 -3x
            strokes.Add(CreateLineStroke(
                new Point(iniP.X - 3 * x * sinTheta, iniP.Y - 3 * x * cosTheta),
                new Point(endP.X - 3 * x * sinTheta, endP.Y - 3 * x * cosTheta),
                context.DrawingAttributes
            ));
            
            // 第二条线：偏移 -x
            strokes.Add(CreateLineStroke(
                new Point(iniP.X - x * sinTheta, iniP.Y - x * cosTheta),
                new Point(endP.X - x * sinTheta, endP.Y - x * cosTheta),
                context.DrawingAttributes
            ));
            
            // 第三条线：偏移 +x
            strokes.Add(CreateLineStroke(
                new Point(iniP.X + x * sinTheta, iniP.Y + x * cosTheta),
                new Point(endP.X + x * sinTheta, endP.Y + x * cosTheta),
                context.DrawingAttributes
            ));
            
            // 第四条线：偏移 +3x
            strokes.Add(CreateLineStroke(
                new Point(iniP.X + 3 * x * sinTheta, iniP.Y + 3 * x * cosTheta),
                new Point(endP.X + 3 * x * sinTheta, endP.Y + 3 * x * cosTheta),
                context.DrawingAttributes
            ));

            return strokes;
        }
    }
}