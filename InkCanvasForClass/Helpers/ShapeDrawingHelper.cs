using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;

namespace Ink_Canvas {
    /// <summary>
    /// 形状绘制辅助类，提供几何图形生成的静态方法
    /// </summary>
    public static class ShapeDrawingHelper {

        // 预计算常量
        private const double TwoPi = 2 * Math.PI;
        private const double EllipseStep = 0.01;
        private const int EllipsePointCount = (int)(TwoPi / EllipseStep) + 1;
        private const int HalfEllipsePointCount = (int)(Math.PI / EllipseStep) + 1;

        /// <summary>
        /// 计算两点之间的距离（优化版本，避免 Math.Pow）
        /// </summary>
        public static double GetDistance(Point p1, Point p2) {
            double dx = p1.X - p2.X;
            double dy = p1.Y - p2.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// 计算两点构成的线段相对于水平方向的旋转角度
        /// </summary>
        public static double CaculateRotateAngleByGivenTwoPoints(Point firstPoint, Point lastPoint) {
            double vec1X = lastPoint.X - firstPoint.X;
            double vec1Y = lastPoint.Y - firstPoint.Y;
            double vecBaseY = firstPoint.Y;
            
            double vec1Magnitude = Math.Sqrt(vec1X * vec1X + vec1Y * vec1Y);
            double vecBaseMagnitude = Math.Abs(vecBaseY);
            
            if (vec1Magnitude < 0.0001 || vecBaseMagnitude < 0.0001) return 0;
            
            double cosine = (vecBaseY * vec1Y) / (vecBaseMagnitude * vec1Magnitude);
            // 限制 cosine 在 [-1, 1] 范围内，避免 Math.Acos 返回 NaN
            cosine = Math.Max(-1, Math.Min(1, cosine));
            
            double angle = Math.Acos(cosine);
            bool isIn2And3Quadrant = lastPoint.X <= firstPoint.X;
            return Math.Round(180 + 180 * (angle / Math.PI) * (isIn2And3Quadrant ? 1 : -1), 0);
        }

        /// <summary>
        /// 生成椭圆几何点集（优化版本，预分配容量）
        /// </summary>
        public static List<Point> GenerateEllipseGeometry(Point st, Point ed, bool isDrawTop = true, bool isDrawBottom = true) {
            double a = 0.5 * (ed.X - st.X);
            double b = 0.5 * (ed.Y - st.Y);
            double centerX = 0.5 * (st.X + ed.X);
            double centerY = 0.5 * (st.Y + ed.Y);
            
            int capacity = (isDrawTop && isDrawBottom) ? EllipsePointCount : HalfEllipsePointCount;
            var pointList = new List<Point>(capacity);
            
            if (isDrawTop && isDrawBottom) {
                for (double r = 0; r <= TwoPi; r += EllipseStep) {
                    pointList.Add(new Point(centerX + a * Math.Cos(r), centerY + b * Math.Sin(r)));
                }
            } else {
                if (isDrawBottom) {
                    for (double r = 0; r <= Math.PI; r += EllipseStep) {
                        pointList.Add(new Point(centerX + a * Math.Cos(r), centerY + b * Math.Sin(r)));
                    }
                }
                if (isDrawTop) {
                    for (double r = Math.PI; r <= TwoPi; r += EllipseStep) {
                        pointList.Add(new Point(centerX + a * Math.Cos(r), centerY + b * Math.Sin(r)));
                    }
                }
            }
            return pointList;
        }

        /// <summary>
        /// 生成直线墨迹（优化版本，使用数组而非 List）
        /// </summary>
        public static Stroke GenerateLineStroke(Point st, Point ed, DrawingAttributes drawingAttrs) {
            var points = new StylusPoint[] {
                new StylusPoint(st.X, st.Y),
                new StylusPoint(ed.X, ed.Y)
            };
            return new Stroke(new StylusPointCollection(points)) { DrawingAttributes = drawingAttrs.Clone() };
        }

        /// <summary>
        /// 生成箭头直线墨迹（优化版本，使用数组而非 List）
        /// </summary>
        public static Stroke GenerateArrowLineStroke(Point st, Point ed, DrawingAttributes drawingAttrs) {
            const double w = 20, h = 7;
            double theta = Math.Atan2(st.Y - ed.Y, st.X - ed.X);
            double sint = Math.Sin(theta);
            double cost = Math.Cos(theta);

            var points = new StylusPoint[] {
                new StylusPoint(st.X, st.Y),
                new StylusPoint(ed.X, ed.Y),
                new StylusPoint(ed.X + (w * cost - h * sint), ed.Y + (w * sint + h * cost)),
                new StylusPoint(ed.X, ed.Y),
                new StylusPoint(ed.X + (w * cost + h * sint), ed.Y - (h * cost - w * sint))
            };
            return new Stroke(new StylusPointCollection(points)) { DrawingAttributes = drawingAttrs.Clone() };
        }

        /// <summary>
        /// 在直线上分布点（优化版本，预分配容量）
        /// </summary>
        public static List<Point> DistributePointsOnLine(Point start, Point end, double interval = 16) {
            double dx = end.X - start.X;
            double dy = end.Y - start.Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);
            
            if (distance < 0.0001) return new List<Point>(0);
            
            int numPoints = (int)(distance / interval);
            var points = new List<Point>(numPoints + 1);
            
            double invDistance = 1.0 / distance;
            for (int i = 0; i <= numPoints; i++) {
                double ratio = (interval * i) * invDistance;
                points.Add(new Point(start.X + ratio * dx, start.Y + ratio * dy));
            }
            return points;
        }

        /// <summary>
        /// 生成虚线墨迹集合（优化版本）
        /// </summary>
        public static StrokeCollection GenerateDashedLineStrokeCollection(Point st, Point ed, DrawingAttributes drawingAttrs) {
            const double step = 5;
            const double stepMultiplier = 2.76;
            
            double d = GetDistance(st, ed);
            if (d < 0.01) return new StrokeCollection();
            
            double sinTheta = (ed.Y - st.Y) / d;
            double cosTheta = (ed.X - st.X) / d;
            
            int estimatedCount = (int)(d / (step * stepMultiplier)) + 1;
            var strokes = new StrokeCollection();
            
            // 预克隆 DrawingAttributes，避免在循环中重复克隆
            var clonedAttrs = drawingAttrs.Clone();
            
            for (double i = 0.0; i < d; i += step * stepMultiplier) {
                double endI = Math.Min(i + step, d);
                var points = new StylusPoint[] {
                    new StylusPoint(st.X + i * cosTheta, st.Y + i * sinTheta),
                    new StylusPoint(st.X + endI * cosTheta, st.Y + endI * sinTheta)
                };
                strokes.Add(new Stroke(new StylusPointCollection(points)) { DrawingAttributes = clonedAttrs.Clone() });
            }
            return strokes;
        }

        /// <summary>
        /// 生成点线墨迹集合（用于点状线条）
        /// </summary>
        public static StrokeCollection GenerateDotLineStrokeCollection(Point st, Point ed, DrawingAttributes drawingAttrs) {
            const double step = 3;
            const double stepMultiplier = 2.76;
            
            double d = GetDistance(st, ed);
            if (d < 0.01) return new StrokeCollection();
            
            var strokes = new StrokeCollection();
            double sinTheta = (ed.Y - st.Y) / d;
            double cosTheta = (ed.X - st.X) / d;
            var clonedAttrs = drawingAttrs.Clone();
            
            for (double i = 0.0; i < d; i += step * stepMultiplier) {
                var stylusPoint = new StylusPoint(st.X + i * cosTheta, st.Y + i * sinTheta, 0.8f);
                var point = new StylusPointCollection(1) { stylusPoint };
                strokes.Add(new Stroke(point) { DrawingAttributes = clonedAttrs.Clone() });
            }
            return strokes;
        }

        /// <summary>
        /// 生成虚线椭圆墨迹集合
        /// </summary>
        public static StrokeCollection GenerateDashedLineEllipseStrokeCollection(Point st, Point ed, DrawingAttributes drawingAttrs, bool isDrawTop = true, bool isDrawBottom = true) {
            double a = 0.5 * (ed.X - st.X);
            double b = 0.5 * (ed.Y - st.Y);
            double centerX = 0.5 * (st.X + ed.X);
            double centerY = 0.5 * (st.Y + ed.Y);
            const double step = 0.05;
            
            var strokes = new StrokeCollection();
            var clonedAttrs = drawingAttrs.Clone();
            
            if (isDrawBottom) {
                for (double i = 0.0; i < 1.0; i += step * 1.66) {
                    var pointList = new List<Point>();
                    for (double r = Math.PI * i; r <= Math.PI * (i + step); r += 0.01) {
                        pointList.Add(new Point(centerX + a * Math.Cos(r), centerY + b * Math.Sin(r)));
                    }
                    if (pointList.Count > 0) {
                        var point = new StylusPointCollection(pointList);
                        strokes.Add(new Stroke(point) { DrawingAttributes = clonedAttrs.Clone() });
                    }
                }
            }
            
            if (isDrawTop) {
                for (double i = 1.0; i < 2.0; i += step * 1.66) {
                    var pointList = new List<Point>();
                    for (double r = Math.PI * i; r <= Math.PI * (i + step); r += 0.01) {
                        pointList.Add(new Point(centerX + a * Math.Cos(r), centerY + b * Math.Sin(r)));
                    }
                    if (pointList.Count > 0) {
                        var point = new StylusPointCollection(pointList);
                        strokes.Add(new Stroke(point) { DrawingAttributes = clonedAttrs.Clone() });
                    }
                }
            }
            
            return strokes;
        }

        /// <summary>
        /// 箭头线配置类
        /// </summary>
        public class ArrowLineConfig {
            public int ArrowWidth { get; set; } = 20;
            public int ArrowHeight { get; set; } = 7;
        }
    }
}