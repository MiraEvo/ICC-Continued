using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace Ink_Canvas {

    public partial class MainWindow : Window {

        public StrokeCollection DrawShapeCore(PointCollection pts, ShapeDrawingType type, bool doNotDisturbutePoints, bool isPreview) {
            if (pts.Count < 2) return new StrokeCollection();
            
            var iniP = pts[0];
            var endP = pts[1];
            var strokes = new StrokeCollection();
            List<Point> pointList;
            StylusPointCollection point;
            Stroke stroke;
            var drawingAttrs = inkCanvas.DefaultDrawingAttributes.Clone();

            // 线条类型
            if (type == ShapeDrawingType.Line || 
                type == ShapeDrawingType.DashedLine || 
                type == ShapeDrawingType.DottedLine ||
                type == ShapeDrawingType.ArrowOneSide ||
                type == ShapeDrawingType.ArrowTwoSide) {
                var stk = new IccStroke(new StylusPointCollection() {
                    new StylusPoint(pts[0].X, pts[0].Y),
                    new StylusPoint(pts[1].X, pts[1].Y),
                }, drawingAttrs) {
                    IsDistributePointsOnLineShape = !doNotDisturbutePoints,
                };
                stk.AddPropertyData(IccStroke.StrokeIsShapeGuid, true);
                stk.AddPropertyData(IccStroke.StrokeShapeTypeGuid, (int)type);
                return new StrokeCollection() { stk };
            }

            // 矩形
            if (type == ShapeDrawingType.Rectangle) {
                pointList = new List<Point> {
                    new Point(iniP.X, iniP.Y),
                    new Point(iniP.X, endP.Y),
                    new Point(endP.X, endP.Y),
                    new Point(endP.X, iniP.Y),
                    new Point(iniP.X, iniP.Y)
                };
                point = new StylusPointCollection(pointList);
                stroke = new Stroke(point) { DrawingAttributes = drawingAttrs };
                strokes.Add(stroke);
                return strokes;
            }

            // 带中心点的矩形
            if (type == ShapeDrawingType.RectangleC) {
                var a = iniP.X - endP.X;
                var b = iniP.Y - endP.Y;
                pointList = new List<Point> {
                    new Point(iniP.X - a, iniP.Y - b),
                    new Point(iniP.X - a, iniP.Y + b),
                    new Point(iniP.X + a, iniP.Y + b),
                    new Point(iniP.X + a, iniP.Y - b),
                    new Point(iniP.X - a, iniP.Y - b)
                };
                point = new StylusPointCollection(pointList);
                stroke = new Stroke(point) { DrawingAttributes = drawingAttrs };
                strokes.Add(stroke);
                // 添加中心点
                var centerPoint = new StylusPointCollection { new StylusPoint(iniP.X, iniP.Y, 1.0f) };
                var centerStroke = new Stroke(centerPoint) { DrawingAttributes = drawingAttrs };
                strokes.Add(centerStroke);
                return strokes;
            }

            // 椭圆
            if (type == ShapeDrawingType.Ellipse) {
                pointList = ShapeDrawingHelper.GenerateEllipseGeometry(iniP, endP);
                point = new StylusPointCollection(pointList);
                stroke = new Stroke(point) { DrawingAttributes = drawingAttrs };
                strokes.Add(stroke);
                return strokes;
            }

            // 带圆心的椭圆/圆
            if (type == ShapeDrawingType.EllipseC) {
                var a = Math.Abs(endP.X - iniP.X);
                var b = Math.Abs(endP.Y - iniP.Y);
                pointList = ShapeDrawingHelper.GenerateEllipseGeometry(
                    new Point(iniP.X - a, iniP.Y - b),
                    new Point(iniP.X + a, iniP.Y + b));
                point = new StylusPointCollection(pointList);
                stroke = new Stroke(point) { DrawingAttributes = drawingAttrs };
                strokes.Add(stroke);
                // 添加中心点
                var centerPoint = new StylusPointCollection { new StylusPoint(iniP.X, iniP.Y, 1.0f) };
                var centerStroke = new Stroke(centerPoint) { DrawingAttributes = drawingAttrs };
                strokes.Add(centerStroke);
                return strokes;
            }

            // 饼图形（扇形）
            if (type == ShapeDrawingType.PieEllipse) {
                var centerX = iniP.X;
                var centerY = iniP.Y;
                var radius = ShapeDrawingHelper.GetDistance(iniP, endP);
                var angle = Math.Atan2(endP.Y - iniP.Y, endP.X - iniP.X);
                
                pointList = new List<Point> { iniP };
                for (double r = angle; r <= angle + Math.PI / 2; r += 0.02) {
                    pointList.Add(new Point(centerX + radius * Math.Cos(r), centerY + radius * Math.Sin(r)));
                }
                pointList.Add(iniP);
                
                point = new StylusPointCollection(pointList);
                stroke = new Stroke(point) { DrawingAttributes = drawingAttrs };
                strokes.Add(stroke);
                return strokes;
            }

            // 三角形
            if (type == ShapeDrawingType.Triangle) {
                var topX = (iniP.X + endP.X) / 2;
                pointList = new List<Point> {
                    new Point(topX, iniP.Y),
                    new Point(iniP.X, endP.Y),
                    new Point(endP.X, endP.Y),
                    new Point(topX, iniP.Y)
                };
                point = new StylusPointCollection(pointList);
                stroke = new Stroke(point) { DrawingAttributes = drawingAttrs };
                strokes.Add(stroke);
                return strokes;
            }

            // 直角三角形
            if (type == ShapeDrawingType.RightTriangle) {
                pointList = new List<Point> {
                    new Point(iniP.X, iniP.Y),
                    new Point(iniP.X, endP.Y),
                    new Point(endP.X, endP.Y),
                    new Point(iniP.X, iniP.Y)
                };
                point = new StylusPointCollection(pointList);
                stroke = new Stroke(point) { DrawingAttributes = drawingAttrs };
                strokes.Add(stroke);
                return strokes;
            }

            // 菱形
            if (type == ShapeDrawingType.Diamond) {
                var centerX = (iniP.X + endP.X) / 2;
                var centerY = (iniP.Y + endP.Y) / 2;
                pointList = new List<Point> {
                    new Point(centerX, iniP.Y),
                    new Point(endP.X, centerY),
                    new Point(centerX, endP.Y),
                    new Point(iniP.X, centerY),
                    new Point(centerX, iniP.Y)
                };
                point = new StylusPointCollection(pointList);
                stroke = new Stroke(point) { DrawingAttributes = drawingAttrs };
                strokes.Add(stroke);
                return strokes;
            }

            // 平行四边形
            if (type == ShapeDrawingType.Parallelogram) {
                var offset = Math.Abs(endP.X - iniP.X) * 0.2;
                pointList = new List<Point> {
                    new Point(iniP.X + offset, iniP.Y),
                    new Point(iniP.X, endP.Y),
                    new Point(endP.X - offset, endP.Y),
                    new Point(endP.X, iniP.Y),
                    new Point(iniP.X + offset, iniP.Y)
                };
                point = new StylusPointCollection(pointList);
                stroke = new Stroke(point) { DrawingAttributes = drawingAttrs };
                strokes.Add(stroke);
                return strokes;
            }

            // 四线三格
            if (type == ShapeDrawingType.FourLine) {
                var height = Math.Abs(endP.Y - iniP.Y);
                var step = height / 3;
                for (int i = 0; i <= 3; i++) {
                    var y = Math.Min(iniP.Y, endP.Y) + step * i;
                    strokes.Add(ShapeDrawingHelper.GenerateLineStroke(
                        new Point(iniP.X, y), new Point(endP.X, y), drawingAttrs));
                }
                return strokes;
            }

            // 五线谱
            if (type == ShapeDrawingType.Staff) {
                var height = Math.Abs(endP.Y - iniP.Y);
                var step = height / 4;
                for (int i = 0; i <= 4; i++) {
                    var y = Math.Min(iniP.Y, endP.Y) + step * i;
                    strokes.Add(ShapeDrawingHelper.GenerateLineStroke(
                        new Point(iniP.X, y), new Point(endP.X, y), drawingAttrs));
                }
                return strokes;
            }

            // 平面坐标轴（双向箭头）
            if (type == ShapeDrawingType.Axis2D) {
                strokes.Add(ShapeDrawingHelper.GenerateArrowLineStroke(
                    new Point(2 * iniP.X - (endP.X - 20), iniP.Y), new Point(endP.X, iniP.Y), drawingAttrs));
                strokes.Add(ShapeDrawingHelper.GenerateArrowLineStroke(
                    new Point(iniP.X, 2 * iniP.Y - (endP.Y + 20)), new Point(iniP.X, endP.Y), drawingAttrs));
                return strokes;
            }

            // 平面坐标轴2（X轴单向）
            if (type == ShapeDrawingType.Axis2DA) {
                if (Math.Abs(iniP.X - endP.X) < 0.01) return strokes;
                strokes.Add(ShapeDrawingHelper.GenerateArrowLineStroke(
                    new Point(iniP.X + (iniP.X - endP.X) / Math.Abs(iniP.X - endP.X) * 25, iniP.Y),
                    new Point(endP.X, iniP.Y), drawingAttrs));
                strokes.Add(ShapeDrawingHelper.GenerateArrowLineStroke(
                    new Point(iniP.X, 2 * iniP.Y - (endP.Y + 20)), new Point(iniP.X, endP.Y), drawingAttrs));
                return strokes;
            }

            // 平面坐标轴3（Y轴单向）
            if (type == ShapeDrawingType.Axis2DB) {
                if (Math.Abs(iniP.Y - endP.Y) < 0.01) return strokes;
                strokes.Add(ShapeDrawingHelper.GenerateArrowLineStroke(
                    new Point(2 * iniP.X - (endP.X - 20), iniP.Y), new Point(endP.X, iniP.Y), drawingAttrs));
                strokes.Add(ShapeDrawingHelper.GenerateArrowLineStroke(
                    new Point(iniP.X, iniP.Y + (iniP.Y - endP.Y) / Math.Abs(iniP.Y - endP.Y) * 25),
                    new Point(iniP.X, endP.Y), drawingAttrs));
                return strokes;
            }

            // 平面坐标轴4（双单向）
            if (type == ShapeDrawingType.Axis2DC) {
                if (Math.Abs(iniP.X - endP.X) < 0.01 || Math.Abs(iniP.Y - endP.Y) < 0.01) return strokes;
                strokes.Add(ShapeDrawingHelper.GenerateArrowLineStroke(
                    new Point(iniP.X + (iniP.X - endP.X) / Math.Abs(iniP.X - endP.X) * 25, iniP.Y),
                    new Point(endP.X, iniP.Y), drawingAttrs));
                strokes.Add(ShapeDrawingHelper.GenerateArrowLineStroke(
                    new Point(iniP.X, iniP.Y + (iniP.Y - endP.Y) / Math.Abs(iniP.Y - endP.Y) * 25),
                    new Point(iniP.X, endP.Y), drawingAttrs));
                return strokes;
            }

            // 三维坐标轴
            if (type == ShapeDrawingType.Axis3D) {
                strokes.Add(ShapeDrawingHelper.GenerateArrowLineStroke(
                    new Point(iniP.X, iniP.Y), new Point(iniP.X + Math.Abs(endP.X - iniP.X), iniP.Y), drawingAttrs));
                strokes.Add(ShapeDrawingHelper.GenerateArrowLineStroke(
                    new Point(iniP.X, iniP.Y), new Point(iniP.X, iniP.Y - Math.Abs(endP.Y - iniP.Y)), drawingAttrs));
                var d = (Math.Abs(iniP.X - endP.X) + Math.Abs(iniP.Y - endP.Y)) / 2;
                strokes.Add(ShapeDrawingHelper.GenerateArrowLineStroke(
                    new Point(iniP.X, iniP.Y), new Point(iniP.X - d / 1.76, iniP.Y + d / 1.76), drawingAttrs));
                return strokes;
            }

            // 双曲线
            if (type == ShapeDrawingType.Hyperbola || type == ShapeDrawingType.HyperbolaF) {
                if (Math.Abs(iniP.X - endP.X) < 0.01 || Math.Abs(iniP.Y - endP.Y) < 0.01) return strokes;
                var a = Math.Abs(endP.X - iniP.X);
                var b = Math.Abs(endP.Y - iniP.Y);
                var pointList1 = new List<Point>();
                var pointList2 = new List<Point>();
                var pointList3 = new List<Point>();
                var pointList4 = new List<Point>();
                
                for (double x = a; x <= a * 2; x += 0.5) {
                    var y = b * Math.Sqrt(x * x / (a * a) - 1);
                    pointList1.Add(new Point(iniP.X + x, iniP.Y - y));
                    pointList2.Add(new Point(iniP.X + x, iniP.Y + y));
                    pointList3.Add(new Point(iniP.X - x, iniP.Y - y));
                    pointList4.Add(new Point(iniP.X - x, iniP.Y + y));
                }
                
                if (pointList1.Count > 0) {
                    strokes.Add(new Stroke(new StylusPointCollection(pointList1)) { DrawingAttributes = drawingAttrs });
                    strokes.Add(new Stroke(new StylusPointCollection(pointList2)) { DrawingAttributes = drawingAttrs });
                    strokes.Add(new Stroke(new StylusPointCollection(pointList3)) { DrawingAttributes = drawingAttrs });
                    strokes.Add(new Stroke(new StylusPointCollection(pointList4)) { DrawingAttributes = drawingAttrs });
                }
                
                if (type == ShapeDrawingType.HyperbolaF) {
                    var c = Math.Sqrt(a * a + b * b);
                    strokes.Add(new Stroke(new StylusPointCollection { new StylusPoint(iniP.X + c, iniP.Y, 1.0f) }) { DrawingAttributes = drawingAttrs });
                    strokes.Add(new Stroke(new StylusPointCollection { new StylusPoint(iniP.X - c, iniP.Y, 1.0f) }) { DrawingAttributes = drawingAttrs });
                }
                return strokes;
            }

            // 抛物线 y=ax^2
            if (type == ShapeDrawingType.Parabola) {
                if (Math.Abs(iniP.X - endP.X) < 0.01 || Math.Abs(iniP.Y - endP.Y) < 0.01) return strokes;
                var a = (iniP.Y - endP.Y) / ((iniP.X - endP.X) * (iniP.X - endP.X));
                var pointList1 = new List<Point>();
                var pointList2 = new List<Point>();
                for (var i = 0.0; i <= Math.Abs(endP.X - iniP.X); i += 0.5) {
                    pointList1.Add(new Point(iniP.X + i, iniP.Y - a * i * i));
                    pointList2.Add(new Point(iniP.X - i, iniP.Y - a * i * i));
                }
                strokes.Add(new Stroke(new StylusPointCollection(pointList1)) { DrawingAttributes = drawingAttrs });
                strokes.Add(new Stroke(new StylusPointCollection(pointList2)) { DrawingAttributes = drawingAttrs });
                return strokes;
            }

            // 抛物线 y^2=ax
            if (type == ShapeDrawingType.ParabolaA) {
                if (Math.Abs(iniP.X - endP.X) < 0.01 || Math.Abs(iniP.Y - endP.Y) < 0.01) return strokes;
                var a = (iniP.X - endP.X) / ((iniP.Y - endP.Y) * (iniP.Y - endP.Y));
                var pointList1 = new List<Point>();
                var pointList2 = new List<Point>();
                for (var i = 0.0; i <= Math.Abs(endP.Y - iniP.Y); i += 0.5) {
                    pointList1.Add(new Point(iniP.X - a * i * i, iniP.Y + i));
                    pointList2.Add(new Point(iniP.X - a * i * i, iniP.Y - i));
                }
                strokes.Add(new Stroke(new StylusPointCollection(pointList1)) { DrawingAttributes = drawingAttrs });
                strokes.Add(new Stroke(new StylusPointCollection(pointList2)) { DrawingAttributes = drawingAttrs });
                return strokes;
            }

            // 抛物线 y^2=ax 带焦点
            if (type == ShapeDrawingType.ParabolaAF) {
                if (Math.Abs(iniP.X - endP.X) < 0.01 || Math.Abs(iniP.Y - endP.Y) < 0.01) return strokes;
                var p = (iniP.Y - endP.Y) * (iniP.Y - endP.Y) / (2 * (iniP.X - endP.X));
                var a = 0.5 / p;
                var pointList1 = new List<Point>();
                var pointList2 = new List<Point>();
                for (var i = 0.0; i <= Math.Abs(endP.Y - iniP.Y); i += 0.5) {
                    pointList1.Add(new Point(iniP.X - a * i * i, iniP.Y + i));
                    pointList2.Add(new Point(iniP.X - a * i * i, iniP.Y - i));
                }
                strokes.Add(new Stroke(new StylusPointCollection(pointList1)) { DrawingAttributes = drawingAttrs });
                strokes.Add(new Stroke(new StylusPointCollection(pointList2)) { DrawingAttributes = drawingAttrs });
                strokes.Add(new Stroke(new StylusPointCollection { new StylusPoint(iniP.X - p / 2, iniP.Y, 1.0f) }) { DrawingAttributes = drawingAttrs });
                return strokes;
            }

            // 圆柱体
            if (type == ShapeDrawingType.Cylinder) {
                var newIniP = iniP;
                if (iniP.Y > endP.Y) {
                    newIniP = new Point(iniP.X, endP.Y);
                    endP = new Point(endP.X, iniP.Y);
                }
                var topA = Math.Abs(newIniP.X - endP.X);
                var topB = topA / 2.646;
                var topEllipse = ShapeDrawingHelper.GenerateEllipseGeometry(
                    new Point(newIniP.X, newIniP.Y - topB / 2), new Point(endP.X, newIniP.Y + topB / 2));
                strokes.Add(new Stroke(new StylusPointCollection(topEllipse)) { DrawingAttributes = drawingAttrs });
                var bottomEllipse = ShapeDrawingHelper.GenerateEllipseGeometry(
                    new Point(newIniP.X, endP.Y - topB / 2), new Point(endP.X, endP.Y + topB / 2), false, true);
                strokes.Add(new Stroke(new StylusPointCollection(bottomEllipse)) { DrawingAttributes = drawingAttrs });
                strokes.Add(ShapeDrawingHelper.GenerateLineStroke(new Point(newIniP.X, newIniP.Y), new Point(newIniP.X, endP.Y), drawingAttrs));
                strokes.Add(ShapeDrawingHelper.GenerateLineStroke(new Point(endP.X, newIniP.Y), new Point(endP.X, endP.Y), drawingAttrs));
                return strokes;
            }

            // 圆锥体
            if (type == ShapeDrawingType.Cone) {
                var newIniP = iniP;
                if (iniP.Y > endP.Y) {
                    newIniP = new Point(iniP.X, endP.Y);
                    endP = new Point(endP.X, iniP.Y);
                }
                var bottomA = Math.Abs(newIniP.X - endP.X);
                var bottomB = bottomA / 2.646;
                var bottomEllipse = ShapeDrawingHelper.GenerateEllipseGeometry(
                    new Point(newIniP.X, endP.Y - bottomB / 2), new Point(endP.X, endP.Y + bottomB / 2), false, true);
                strokes.Add(new Stroke(new StylusPointCollection(bottomEllipse)) { DrawingAttributes = drawingAttrs });
                strokes.Add(ShapeDrawingHelper.GenerateLineStroke(new Point((newIniP.X + endP.X) / 2, newIniP.Y), new Point(newIniP.X, endP.Y), drawingAttrs));
                strokes.Add(ShapeDrawingHelper.GenerateLineStroke(new Point((newIniP.X + endP.X) / 2, newIniP.Y), new Point(endP.X, endP.Y), drawingAttrs));
                return strokes;
            }

            // 立方体
            if (type == ShapeDrawingType.Cube) {
                var d = Math.Min(Math.Abs(endP.X - iniP.X), Math.Abs(endP.Y - iniP.Y)) * 0.3;
                strokes.Add(ShapeDrawingHelper.GenerateLineStroke(new Point(iniP.X, iniP.Y), new Point(iniP.X, endP.Y), drawingAttrs));
                strokes.Add(ShapeDrawingHelper.GenerateLineStroke(new Point(iniP.X, endP.Y), new Point(endP.X, endP.Y), drawingAttrs));
                strokes.Add(ShapeDrawingHelper.GenerateLineStroke(new Point(endP.X, endP.Y), new Point(endP.X, iniP.Y), drawingAttrs));
                strokes.Add(ShapeDrawingHelper.GenerateLineStroke(new Point(iniP.X, iniP.Y), new Point(endP.X, iniP.Y), drawingAttrs));
                strokes.Add(ShapeDrawingHelper.GenerateLineStroke(new Point(iniP.X + d, iniP.Y - d), new Point(endP.X + d, iniP.Y - d), drawingAttrs));
                strokes.Add(ShapeDrawingHelper.GenerateLineStroke(new Point(iniP.X, iniP.Y), new Point(iniP.X + d, iniP.Y - d), drawingAttrs));
                strokes.Add(ShapeDrawingHelper.GenerateLineStroke(new Point(endP.X, iniP.Y), new Point(endP.X + d, iniP.Y - d), drawingAttrs));
                strokes.Add(ShapeDrawingHelper.GenerateLineStroke(new Point(endP.X, endP.Y), new Point(endP.X + d, endP.Y - d), drawingAttrs));
                strokes.Add(ShapeDrawingHelper.GenerateLineStroke(new Point(endP.X + d, iniP.Y - d), new Point(endP.X + d, endP.Y - d), drawingAttrs));
                return strokes;
            }

            // 网格辅助线 - 40像素间隔的坐标网格
            if (type == ShapeDrawingType.CoordinateGrid) {
                var left = Math.Min(iniP.X, endP.X);
                var right = Math.Max(iniP.X, endP.X);
                var top = Math.Min(iniP.Y, endP.Y);
                var bottom = Math.Max(iniP.Y, endP.Y);
                double gridSize = 40;
                // 绘制垂直线
                for (double x = left; x <= right; x += gridSize) {
                    strokes.Add(ShapeDrawingHelper.GenerateLineStroke(new Point(x, top), new Point(x, bottom), drawingAttrs));
                }
                // 绘制水平线
                for (double y = top; y <= bottom; y += gridSize) {
                    strokes.Add(ShapeDrawingHelper.GenerateLineStroke(new Point(left, y), new Point(right, y), drawingAttrs));
                }
                return strokes;
            }

            return strokes;
        }
    }
}
