using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using iNKORE.UI.WPF.Helpers;

namespace Ink_Canvas {

    public class IccStroke : Stroke {

        public IccStroke(StylusPointCollection stylusPoints, DrawingAttributes drawingAttributes)
            : base(stylusPoints, drawingAttributes) { }

        public static Guid StrokeShapeTypeGuid = new Guid("6537b29c-557f-487f-800b-cb30a8f1de78");
        public static Guid StrokeIsShapeGuid = new Guid("40eff5db-9346-4e42-bd46-7b0eb19d0018");

        public StylusPointCollection RawStylusPointCollection { get; set; }

        public ShapeDrawingHelper.ArrowLineConfig ArrowLineConfig { get; set; } =
            new ShapeDrawingHelper.ArrowLineConfig();

        /// <summary>
        /// 根据这个属性判断当前 Stroke 是否是原始输入
        /// </summary>
        public bool IsRawStylusPoints = true;

        /// <summary>
        /// 根据这个属性决定在绘制 Stroke 时是否需要在直线形状中，在两点构成直线上分布点，用于墨迹的范围框选。
        /// </summary>
        public bool IsDistributePointsOnLineShape = true;

        /// <summary>
        /// 指示该墨迹是否来自一个完整墨迹被擦除后的一部分墨迹，仅用于形状墨迹。
        /// </summary>
        public bool IsErasedStrokePart = false;

        private bool? _cachedIsShape = null;
        private int? _cachedShapeType = null;
        private StreamGeometry _cachedGeometry = null;
        private Pen _cachedPen = null;
        private Color _cachedPenColor;
        private double _cachedPenWidth;
        private bool _geometryNeedsUpdate = true;
        private SolidColorBrush _cachedFillBrush = null;
        private Color _cachedFillColor;
        private static readonly Brush TransparentBrush = Brushes.Transparent;

        /// <summary>
        /// 获取缓存的形状类型，避免重复调用 GetPropertyData
        /// </summary>
        private int GetCachedShapeType() {
            if (_cachedShapeType == null) {
                if (this.ContainsPropertyData(StrokeShapeTypeGuid)) {
                    _cachedShapeType = (int)this.GetPropertyData(StrokeShapeTypeGuid);
                } else {
                    _cachedShapeType = -1;
                }
            }
            return _cachedShapeType.Value;
        }

        /// <summary>
        /// 获取缓存的是否是形状标记
        /// </summary>
        private bool GetCachedIsShape() {
            if (_cachedIsShape == null) {
                _cachedIsShape = this.ContainsPropertyData(StrokeIsShapeGuid) &&
                                 (bool)this.GetPropertyData(StrokeIsShapeGuid);
            }
            return _cachedIsShape.Value;
        }

        public void InvalidateGeometryCache() {
            _geometryNeedsUpdate = true;
        }

        protected override void DrawCore(DrawingContext drawingContext,
            DrawingAttributes drawingAttributes) {
            
            if (!GetCachedIsShape()) {
                base.DrawCore(drawingContext, drawingAttributes);
                return;
            }

            int shapeType = GetCachedShapeType();
            bool isLineShape = shapeType == (int)MainWindow.ShapeDrawingType.DashedLine ||
                               shapeType == (int)MainWindow.ShapeDrawingType.Line ||
                               shapeType == (int)MainWindow.ShapeDrawingType.DottedLine ||
                               shapeType == (int)MainWindow.ShapeDrawingType.ArrowOneSide ||
                               shapeType == (int)MainWindow.ShapeDrawingType.ArrowTwoSide;

            if (!isLineShape) {
                base.DrawCore(drawingContext, drawingAttributes);
                return;
            }

            if (StylusPoints.Count < 2) {
                base.DrawCore(drawingContext, drawingAttributes);
                return;
            }

            double penWidth = (drawingAttributes.Width + drawingAttributes.Height) / 2;
            Color penColor = DrawingAttributes.Color;
            
            if (_cachedPen == null || _cachedPenColor != penColor || Math.Abs(_cachedPenWidth - penWidth) > 0.001) {
                var penBrush = new SolidColorBrush(penColor);
                penBrush.Freeze();
                
                _cachedPen = new Pen(penBrush, penWidth) {
                    DashCap = PenLineCap.Round,
                    StartLineCap = PenLineCap.Round,
                    EndLineCap = PenLineCap.Round,
                };
                
                if (shapeType != (int)MainWindow.ShapeDrawingType.Line &&
                    shapeType != (int)MainWindow.ShapeDrawingType.ArrowOneSide &&
                    shapeType != (int)MainWindow.ShapeDrawingType.ArrowTwoSide) {
                    _cachedPen.DashStyle = shapeType == (int)MainWindow.ShapeDrawingType.DottedLine
                        ? DashStyles.Dot
                        : DashStyles.Dash;
                }
                
                _cachedPen.Freeze();
                _cachedPenColor = penColor;
                _cachedPenWidth = penWidth;
                _geometryNeedsUpdate = true;
            }

            if (IsDistributePointsOnLineShape &&
                (shapeType == (int)MainWindow.ShapeDrawingType.DashedLine ||
                 shapeType == (int)MainWindow.ShapeDrawingType.Line ||
                 shapeType == (int)MainWindow.ShapeDrawingType.DottedLine) && 
                IsRawStylusPoints) {
                
                IsRawStylusPoints = false;
                RawStylusPointCollection = StylusPoints.Clone();
                
                var startPoint = new Point(StylusPoints[0].X, StylusPoints[0].Y);
                var endPoint = new Point(StylusPoints[1].X, StylusPoints[1].Y);
                
                var pointList = new List<Point>(20);
                pointList.Add(startPoint);
                pointList.AddRange(ShapeDrawingHelper.DistributePointsOnLine(startPoint, endPoint));
                pointList.Add(endPoint);
                
                StylusPoints = new StylusPointCollection(pointList);
                _geometryNeedsUpdate = true;
            }

            if (shapeType == (int)MainWindow.ShapeDrawingType.ArrowOneSide && IsRawStylusPoints) {
                IsRawStylusPoints = false;
                
                var pt0 = new Point(StylusPoints[0].X, StylusPoints[0].Y);
                var pt1 = new Point(StylusPoints[1].X, StylusPoints[1].Y);
                RawStylusPointCollection = StylusPoints.Clone();
                
                double w = ArrowLineConfig.ArrowWidth, h = ArrowLineConfig.ArrowHeight;
                var theta = Math.Atan2(pt0.Y - pt1.Y, pt0.X - pt1.X);
                var sint = Math.Sin(theta);
                var cost = Math.Cos(theta);
                
                var pointList = new List<Point>(10);
                pointList.Add(pt0);
                
                if (IsDistributePointsOnLineShape) {
                    pointList.AddRange(ShapeDrawingHelper.DistributePointsOnLine(pt0, pt1));
                }
                
                pointList.Add(pt1);
                pointList.Add(new Point(pt1.X + (w * cost - h * sint), pt1.Y + (w * sint + h * cost)));
                pointList.Add(pt1);
                pointList.Add(new Point(pt1.X + (w * cost + h * sint), pt1.Y - (h * cost - w * sint)));
                
                StylusPoints = new StylusPointCollection(pointList);
                _geometryNeedsUpdate = true;
            }

            if (_geometryNeedsUpdate || _cachedGeometry == null) {
                _cachedGeometry = new StreamGeometry();
                
                using (StreamGeometryContext ctx = _cachedGeometry.Open()) {
                    var points = this.StylusPoints;
                    if (points.Count > 0) {
                        ctx.BeginFigure(new Point(points[0].X, points[0].Y), false, false);
                        Point[] ptArray = new Point[points.Count - 1];
                        for (int i = 1; i < points.Count; i++) {
                            ptArray[i - 1] = new Point(points[i].X, points[i].Y);
                        }
                        ctx.PolyLineTo(ptArray, true, true);
                    }
                }
                
                _cachedGeometry.Freeze();
                _geometryNeedsUpdate = false;
            }

            Brush fillBrush;
            if (shapeType == (int)MainWindow.ShapeDrawingType.ArrowOneSide) {
                if (_cachedFillBrush == null || _cachedFillColor != penColor) {
                    _cachedFillBrush = new SolidColorBrush(penColor);
                    _cachedFillBrush.Freeze();
                    _cachedFillColor = penColor;
                }
                fillBrush = _cachedFillBrush;
            } else {
                fillBrush = TransparentBrush;
            }
            
            drawingContext.DrawGeometry(fillBrush, _cachedPen, _cachedGeometry);
        }
    }

    public class IccInkCanvas : InkCanvas {
        public IccInkCanvas() {
            // 通过反射移除InkCanvas自带的默认 Delete按键事件
            var commandBindingsField =
                typeof(CommandManager).GetField("_classCommandBindings", BindingFlags.NonPublic | BindingFlags.Static);
            var bnds = commandBindingsField.GetValue(null) as HybridDictionary;
            var inkCanvasBindings = bnds[typeof(InkCanvas)] as CommandBindingCollection;
            var enumerator = inkCanvasBindings.GetEnumerator();
            while (enumerator.MoveNext()) {
                var item = (CommandBinding)enumerator.Current;
                if (item.Command == ApplicationCommands.Delete) {
                    var executedField =
                        typeof(CommandBinding).GetField("Executed", BindingFlags.NonPublic | BindingFlags.Instance);
                    var canExecuteField =
                        typeof(CommandBinding).GetField("CanExecute", BindingFlags.NonPublic | BindingFlags.Instance);
                    executedField.SetValue(item, new ExecutedRoutedEventHandler((sender, args) => { }));
                    canExecuteField.SetValue(item, new CanExecuteRoutedEventHandler((sender, args) => { }));
                }
            }

            // 为IccInkCanvas注册自定义的 Delete按键Command并Invoke OnDeleteCommandFired。
            CommandManager.RegisterClassCommandBinding(typeof(IccInkCanvas), new CommandBinding(ApplicationCommands.Delete,
                (sender, args) => {
                    DeleteKeyCommandFired?.Invoke(this, new RoutedEventArgs());
                }, (sender, args) => {
                    args.CanExecute = GetSelectedStrokes().Count != 0;
                }));
        }

        protected override void OnStrokeCollected(InkCanvasStrokeCollectedEventArgs e) {
            IccStroke customStroke = new IccStroke(e.Stroke.StylusPoints, e.Stroke.DrawingAttributes);
            if (e.Stroke is IccStroke) {
                this.Strokes.Add(e.Stroke);
            } else {
                this.Strokes.Remove(e.Stroke);
                this.Strokes.Add(customStroke);
            }

            InkCanvasStrokeCollectedEventArgs args =
                new InkCanvasStrokeCollectedEventArgs(customStroke);
            base.OnStrokeCollected(args);
        }

        public event EventHandler<RoutedEventArgs> DeleteKeyCommandFired;
    }
}
