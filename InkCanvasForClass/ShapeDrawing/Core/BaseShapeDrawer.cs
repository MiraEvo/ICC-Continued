using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;

namespace Ink_Canvas.ShapeDrawing.Core {
    /// <summary>
    /// 形状绘制器抽象基类，提供通用实现
    /// </summary>
    public abstract class BaseShapeDrawer : IShapeDrawer {
        /// <summary>
        /// 形状类型
        /// </summary>
        public abstract ShapeDrawingType ShapeType { get; }

        /// <summary>
        /// 形状名称
        /// </summary>
        public virtual string ShapeName => ShapeType.ToString();

        /// <summary>
        /// 是否支持多步绘制（默认不支持）
        /// </summary>
        public virtual bool SupportsMultiStep => false;

        /// <summary>
        /// 总绘制步骤数（默认1步）
        /// </summary>
        public virtual int TotalSteps => 1;

        /// <summary>
        /// 执行形状绘制
        /// </summary>
        public abstract StrokeCollection Draw(ShapeDrawingContext context);

        /// <summary>
        /// 重置绘制器状态
        /// </summary>
        public virtual void Reset() {
            // 默认无需重置
        }

        /// <summary>
        /// 验证绘制上下文
        /// </summary>
        public virtual bool ValidateContext(ShapeDrawingContext context) {
            if (context == null) return false;
            if (context.DrawingAttributes == null) return false;
            
            // 检查起点和终点是否有效（距离不能太小）
            double distance = context.GetDistance();
            return distance >= 1.0;
        }

        #region 辅助方法

        /// <summary>
        /// 创建基础笔画
        /// </summary>
        protected Stroke CreateStroke(IEnumerable<Point> points, DrawingAttributes attrs) {
            var stylusPoints = new StylusPointCollection(points);
            return new Stroke(stylusPoints) { DrawingAttributes = attrs.Clone() };
        }

        /// <summary>
        /// 创建基础笔画
        /// </summary>
        protected Stroke CreateStroke(StylusPointCollection points, DrawingAttributes attrs) {
            return new Stroke(points) { DrawingAttributes = attrs.Clone() };
        }

        /// <summary>
        /// 创建直线笔画
        /// </summary>
        protected Stroke CreateLineStroke(Point start, Point end, DrawingAttributes attrs) {
            var points = new StylusPointCollection {
                new StylusPoint(start.X, start.Y),
                new StylusPoint(end.X, end.Y)
            };
            return new Stroke(points) { DrawingAttributes = attrs.Clone() };
        }

        /// <summary>
        /// 创建带箭头的直线笔画
        /// </summary>
        protected Stroke CreateArrowLineStroke(Point start, Point end, DrawingAttributes attrs, 
            double arrowWidth = 20, double arrowHeight = 7) {
            double theta = Math.Atan2(start.Y - end.Y, start.X - end.X);
            double sint = Math.Sin(theta);
            double cost = Math.Cos(theta);

            var points = new StylusPointCollection {
                new StylusPoint(start.X, start.Y),
                new StylusPoint(end.X, end.Y),
                new StylusPoint(end.X + (arrowWidth * cost - arrowHeight * sint), 
                               end.Y + (arrowWidth * sint + arrowHeight * cost)),
                new StylusPoint(end.X, end.Y),
                new StylusPoint(end.X + (arrowWidth * cost + arrowHeight * sint), 
                               end.Y - (arrowHeight * cost - arrowWidth * sint))
            };
            return new Stroke(points) { DrawingAttributes = attrs.Clone() };
        }

        /// <summary>
        /// 创建双向箭头直线笔画
        /// </summary>
        protected Stroke CreateDoubleArrowLineStroke(Point start, Point end, DrawingAttributes attrs,
            double arrowWidth = 20, double arrowHeight = 7) {
            double theta = Math.Atan2(start.Y - end.Y, start.X - end.X);
            double sint = Math.Sin(theta);
            double cost = Math.Cos(theta);

            // 从起点开始的箭头
            double thetaStart = Math.Atan2(end.Y - start.Y, end.X - start.X);
            double sintStart = Math.Sin(thetaStart);
            double costStart = Math.Cos(thetaStart);

            var points = new StylusPointCollection {
                // 起点箭头
                new StylusPoint(start.X + (arrowWidth * costStart - arrowHeight * sintStart),
                               start.Y + (arrowWidth * sintStart + arrowHeight * costStart)),
                new StylusPoint(start.X, start.Y),
                new StylusPoint(start.X + (arrowWidth * costStart + arrowHeight * sintStart),
                               start.Y - (arrowHeight * costStart - arrowWidth * sintStart)),
                new StylusPoint(start.X, start.Y),
                // 主线
                new StylusPoint(end.X, end.Y),
                // 终点箭头
                new StylusPoint(end.X + (arrowWidth * cost - arrowHeight * sint),
                               end.Y + (arrowWidth * sint + arrowHeight * cost)),
                new StylusPoint(end.X, end.Y),
                new StylusPoint(end.X + (arrowWidth * cost + arrowHeight * sint),
                               end.Y - (arrowHeight * cost - arrowWidth * sint))
            };
            return new Stroke(points) { DrawingAttributes = attrs.Clone() };
        }

        /// <summary>
        /// 生成椭圆点集
        /// </summary>
        protected List<Point> GenerateEllipsePoints(Point topLeft, Point bottomRight, 
            bool drawTop = true, bool drawBottom = true) {
            double a = 0.5 * (bottomRight.X - topLeft.X);
            double b = 0.5 * (bottomRight.Y - topLeft.Y);
            double centerX = 0.5 * (topLeft.X + bottomRight.X);
            double centerY = 0.5 * (topLeft.Y + bottomRight.Y);

            const double step = 0.01;
            var points = new List<Point>();

            if (drawTop && drawBottom) {
                for (double r = 0; r <= 2 * Math.PI; r += step) {
                    points.Add(new Point(centerX + a * Math.Cos(r), centerY + b * Math.Sin(r)));
                }
            } else {
                if (drawBottom) {
                    for (double r = 0; r <= Math.PI; r += step) {
                        points.Add(new Point(centerX + a * Math.Cos(r), centerY + b * Math.Sin(r)));
                    }
                }
                if (drawTop) {
                    for (double r = Math.PI; r <= 2 * Math.PI; r += step) {
                        points.Add(new Point(centerX + a * Math.Cos(r), centerY + b * Math.Sin(r)));
                    }
                }
            }
            return points;
        }

        /// <summary>
        /// 生成虚线笔画集合
        /// </summary>
        protected StrokeCollection GenerateDashedLineStrokes(Point start, Point end, DrawingAttributes attrs,
            double dashLength = 5, double gapMultiplier = 2.76) {
            var strokes = new StrokeCollection();
            double distance = Math.Sqrt(Math.Pow(end.X - start.X, 2) + Math.Pow(end.Y - start.Y, 2));
            
            if (distance < 0.01) return strokes;

            double sinTheta = (end.Y - start.Y) / distance;
            double cosTheta = (end.X - start.X) / distance;

            for (double i = 0; i < distance; i += dashLength * gapMultiplier) {
                double endI = Math.Min(i + dashLength, distance);
                var points = new StylusPointCollection {
                    new StylusPoint(start.X + i * cosTheta, start.Y + i * sinTheta),
                    new StylusPoint(start.X + endI * cosTheta, start.Y + endI * sinTheta)
                };
                strokes.Add(new Stroke(points) { DrawingAttributes = attrs.Clone() });
            }
            return strokes;
        }

        /// <summary>
        /// 生成点线笔画集合
        /// </summary>
        protected StrokeCollection GenerateDottedLineStrokes(Point start, Point end, DrawingAttributes attrs,
            double dotInterval = 3, double gapMultiplier = 2.76) {
            var strokes = new StrokeCollection();
            double distance = Math.Sqrt(Math.Pow(end.X - start.X, 2) + Math.Pow(end.Y - start.Y, 2));
            
            if (distance < 0.01) return strokes;

            double sinTheta = (end.Y - start.Y) / distance;
            double cosTheta = (end.X - start.X) / distance;

            for (double i = 0; i < distance; i += dotInterval * gapMultiplier) {
                var points = new StylusPointCollection {
                    new StylusPoint(start.X + i * cosTheta, start.Y + i * sinTheta, 0.8f)
                };
                strokes.Add(new Stroke(points) { DrawingAttributes = attrs.Clone() });
            }
            return strokes;
        }

        /// <summary>
        /// 在直线上分布点（用于墨迹框选）
        /// </summary>
        protected List<Point> DistributePointsOnLine(Point start, Point end, double interval = 16) {
            double dx = end.X - start.X;
            double dy = end.Y - start.Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);

            if (distance < 0.0001) return new List<Point>();

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
        /// 计算两点距离
        /// </summary>
        protected double GetDistance(Point p1, Point p2) {
            double dx = p1.X - p2.X;
            double dy = p1.Y - p2.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// 创建单点笔画（用于绘制焦点等）
        /// </summary>
        protected Stroke CreatePointStroke(Point point, DrawingAttributes attrs, float pressure = 1.0f) {
            var points = new StylusPointCollection {
                new StylusPoint(point.X, point.Y, pressure)
            };
            return new Stroke(points) { DrawingAttributes = attrs.Clone() };
        }

        #endregion
    }
}