// ============================================================================
// MW_ShapeDrawing.cs - 形状绘制逻辑
// ============================================================================
//
// 功能说明:
//   - 形状绘制面板的显示/隐藏
//   - 形状类型选择
//   - 形状绘制模式管理
//
// 迁移状态 (渐进式迁移):
//   - ShapeDrawingService 已创建，提供形状绘制服务
//   - ShapeDrawingView UserControl 已创建
//   - 此文件中的 UI 交互逻辑仍在使用
//
// 相关文件:
//   - Services/ShapeDrawingService.cs
//   - Services/IShapeDrawingService.cs
//   - Views/ShapeDrawing/ShapeDrawingView.xaml
//   - MW_ShapeDrawingCore.cs (形状绘制核心算法)
//   - MW_ShapeDrawingRefactored.cs (重构后的形状绘制)
//   - MainWindow.xaml (BorderDrawShape, BoardBorderDrawShape 区域)
//
// ============================================================================

using Ink_Canvas.Helpers;
using Ink_Canvas.ShapeDrawing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using MessageBox = System.Windows.MessageBox;
using Point = System.Windows.Point;

namespace Ink_Canvas {
    public partial class MainWindow : Window {
        #region Floating Bar Control

        private void ImageDrawShape_MouseUp(object sender, MouseButtonEventArgs e) {

            if (lastBorderMouseDownObject != null && lastBorderMouseDownObject is Panel)
                ((Panel)lastBorderMouseDownObject).Background = new SolidColorBrush(Colors.Transparent);
            if (sender == ShapeDrawFloatingBarBtn && lastBorderMouseDownObject != ShapeDrawFloatingBarBtn) return;

            if (ShapeDrawingPopupV2.IsOpen == false) {
                var transform = ShapeDrawFloatingBarBtn.TransformToVisual(Main_Grid);
                var pt = transform.Transform(new Point(0, 0));
                ShapeDrawingPopupV2.VerticalOffset = pt.Y;
                ShapeDrawingPopupV2.HorizontalOffset = pt.X - 32;
                ShapeDrawingPopupV2.IsOpen = true;
            } else {
                HideSubPanels();
            }
        }

        #endregion Floating Bar Control

        private int drawingShapeMode = 0;
        private bool isLongPressSelected = false;

        #region Buttons

        public bool IsDrawShapeBorderAutoHide = false;

        public void SymbolIconPinBorderDrawShape_MouseUp(object sender, MouseButtonEventArgs e) {
            if (lastBorderMouseDownObject != sender) return;

            IsDrawShapeBorderAutoHide = !IsDrawShapeBorderAutoHide;

            if (IsDrawShapeBorderAutoHide)
                ((iNKORE.UI.WPF.Modern.Controls.FontIcon)sender).Glyph = "\uE718";
            else
                ((iNKORE.UI.WPF.Modern.Controls.FontIcon)sender).Glyph = "\uE77A";
        }

        private object lastMouseDownSender = null;
        private DateTime lastMouseDownTime = DateTime.MinValue;

        public async void Image_MouseDown(object sender, MouseButtonEventArgs e) {
            lastMouseDownSender = sender;
            lastMouseDownTime = DateTime.Now;

            await Task.Delay(500);

            if (lastMouseDownSender == sender) {
                lastMouseDownSender = null;
                var dA = new DoubleAnimation(1, 0.3, new Duration(TimeSpan.FromMilliseconds(100)));
                ((UIElement)sender).BeginAnimation(OpacityProperty, dA);

                forceEraser = true;
                inkCanvas.EditingMode = InkCanvasEditingMode.None;
                inkCanvas.IsManipulationEnabled = true;

                if (sender is FrameworkElement fe && fe.Tag != null && int.TryParse(fe.Tag.ToString(), out int mode))
                {
                    drawingShapeMode = mode;
                }

                isLongPressSelected = true;
                if (isSingleFingerDragMode) BtnFingerDragMode_Click(null, null);
            }
        }

        public void BtnPen_Click(object sender, RoutedEventArgs e) {
            forceEraser = false;
            drawingShapeMode = 0;
            inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
            inkCanvas.IsManipulationEnabled = true;
            CancelSingleFingerDragMode();
            isLongPressSelected = false;
        }

        private Task<bool> CheckIsDrawingShapesInMultiTouchMode() {
            if (isInMultiTouchMode) {
                isInMultiTouchMode = false;
                Settings.Gesture.DefaultMultiPointHandWritingMode = 1;
                SaveSettings();
                lastIsInMultiTouchMode = true;
            }

            return Task.FromResult(true);
        }

        public async void BtnDrawLine_Click(object sender, MouseButtonEventArgs e) {
            await CheckIsDrawingShapesInMultiTouchMode();
            if (lastMouseDownSender == sender) {
                forceEraser = true;
                drawingShapeMode = 1;
                inkCanvas.EditingMode = InkCanvasEditingMode.None;
                inkCanvas.IsManipulationEnabled = true;
                CancelSingleFingerDragMode();
            }

            lastMouseDownSender = null;
            if (isLongPressSelected) {
                if (IsDrawShapeBorderAutoHide) CollapseBorderDrawShape();
                var dA = new DoubleAnimation(1, 1, new Duration(TimeSpan.FromMilliseconds(0)));
                ((UIElement)sender).BeginAnimation(OpacityProperty, dA);
            }

            DrawShapePromptToPen();
        }

        public async void BtnDrawDashedLine_Click(object sender, MouseButtonEventArgs e) {
            await CheckIsDrawingShapesInMultiTouchMode();
            if (lastMouseDownSender == sender) {
                forceEraser = true;
                drawingShapeMode = 8;
                inkCanvas.EditingMode = InkCanvasEditingMode.None;
                inkCanvas.IsManipulationEnabled = true;
                CancelSingleFingerDragMode();
            }

            lastMouseDownSender = null;
            if (isLongPressSelected) {
                if (IsDrawShapeBorderAutoHide) CollapseBorderDrawShape();
                var dA = new DoubleAnimation(1, 1, new Duration(TimeSpan.FromMilliseconds(0)));
                ((UIElement)sender).BeginAnimation(OpacityProperty, dA);
            }

            DrawShapePromptToPen();
        }

        public async void BtnDrawDotLine_Click(object sender, MouseButtonEventArgs e) {
            await CheckIsDrawingShapesInMultiTouchMode();
            if (lastMouseDownSender == sender) {
                forceEraser = true;
                drawingShapeMode = 18;
                inkCanvas.EditingMode = InkCanvasEditingMode.None;
                inkCanvas.IsManipulationEnabled = true;
                CancelSingleFingerDragMode();
            }

            lastMouseDownSender = null;
            if (isLongPressSelected) {
                if (IsDrawShapeBorderAutoHide) CollapseBorderDrawShape();
                var dA = new DoubleAnimation(1, 1, new Duration(TimeSpan.FromMilliseconds(0)));
                ((UIElement)sender).BeginAnimation(OpacityProperty, dA);
            }

            DrawShapePromptToPen();
        }

        public async void BtnDrawArrow_Click(object sender, MouseButtonEventArgs e) {
            await CheckIsDrawingShapesInMultiTouchMode();
            if (lastMouseDownSender == sender) {
                forceEraser = true;
                drawingShapeMode = 2;
                inkCanvas.EditingMode = InkCanvasEditingMode.None;
                inkCanvas.IsManipulationEnabled = true;
                CancelSingleFingerDragMode();
            }

            lastMouseDownSender = null;
            if (isLongPressSelected) {
                if (IsDrawShapeBorderAutoHide) CollapseBorderDrawShape();
                var dA = new DoubleAnimation(1, 1, new Duration(TimeSpan.FromMilliseconds(0)));
                ((UIElement)sender).BeginAnimation(OpacityProperty, dA);
            }

            DrawShapePromptToPen();
        }

        public async void BtnDrawParallelLine_Click(object sender, MouseButtonEventArgs e) {
            await CheckIsDrawingShapesInMultiTouchMode();
            if (lastMouseDownSender == sender) {
                forceEraser = true;
                drawingShapeMode = 15;
                inkCanvas.EditingMode = InkCanvasEditingMode.None;
                inkCanvas.IsManipulationEnabled = true;
                CancelSingleFingerDragMode();
            }

            lastMouseDownSender = null;
            if (isLongPressSelected) {
                if (IsDrawShapeBorderAutoHide) CollapseBorderDrawShape();
                var dA = new DoubleAnimation(1, 1, new Duration(TimeSpan.FromMilliseconds(0)));
                ((UIElement)sender).BeginAnimation(OpacityProperty, dA);
            }

            DrawShapePromptToPen();
        }

        public async void BtnDrawCoordinate1_Click(object sender, MouseButtonEventArgs e) {
            await CheckIsDrawingShapesInMultiTouchMode();
            forceEraser = true;
            drawingShapeMode = 11;
            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            inkCanvas.IsManipulationEnabled = true;
            CancelSingleFingerDragMode();
            DrawShapePromptToPen();
        }

        public async void BtnDrawCoordinate2_Click(object sender, MouseButtonEventArgs e) {
            await CheckIsDrawingShapesInMultiTouchMode();
            forceEraser = true;
            drawingShapeMode = 12;
            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            inkCanvas.IsManipulationEnabled = true;
            CancelSingleFingerDragMode();
            DrawShapePromptToPen();
        }

        public async void BtnDrawCoordinate3_Click(object sender, MouseButtonEventArgs e) {
            await CheckIsDrawingShapesInMultiTouchMode();
            forceEraser = true;
            drawingShapeMode = 13;
            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            inkCanvas.IsManipulationEnabled = true;
            CancelSingleFingerDragMode();
            DrawShapePromptToPen();
        }

        public async void BtnDrawCoordinate4_Click(object sender, MouseButtonEventArgs e) {
            await CheckIsDrawingShapesInMultiTouchMode();
            forceEraser = true;
            drawingShapeMode = 14;
            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            inkCanvas.IsManipulationEnabled = true;
            CancelSingleFingerDragMode();
            DrawShapePromptToPen();
        }

        public async void BtnDrawCoordinate5_Click(object sender, MouseButtonEventArgs e) {
            await CheckIsDrawingShapesInMultiTouchMode();
            forceEraser = true;
            drawingShapeMode = 17;
            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            inkCanvas.IsManipulationEnabled = true;
            CancelSingleFingerDragMode();
            DrawShapePromptToPen();
        }

        public async void BtnDrawRectangle_Click(object sender, MouseButtonEventArgs e) {
            await CheckIsDrawingShapesInMultiTouchMode();
            forceEraser = true;
            drawingShapeMode = 3;
            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            inkCanvas.IsManipulationEnabled = true;
            CancelSingleFingerDragMode();
            DrawShapePromptToPen();
        }

        public async void BtnDrawRectangleCenter_Click(object sender, MouseButtonEventArgs e) {
            await CheckIsDrawingShapesInMultiTouchMode();
            forceEraser = true;
            drawingShapeMode = 19;
            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            inkCanvas.IsManipulationEnabled = true;
            CancelSingleFingerDragMode();
            DrawShapePromptToPen();
        }

        public async void BtnDrawEllipse_Click(object sender, MouseButtonEventArgs e) {
            await CheckIsDrawingShapesInMultiTouchMode();
            forceEraser = true;
            drawingShapeMode = 4;
            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            inkCanvas.IsManipulationEnabled = true;
            CancelSingleFingerDragMode();
            DrawShapePromptToPen();
        }

        public async void BtnDrawCircle_Click(object sender, MouseButtonEventArgs e) {
            await CheckIsDrawingShapesInMultiTouchMode();
            forceEraser = true;
            drawingShapeMode = 5;
            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            inkCanvas.IsManipulationEnabled = true;
            CancelSingleFingerDragMode();
            DrawShapePromptToPen();
        }

        public async void BtnDrawCenterEllipse_Click(object sender, MouseButtonEventArgs e) {
            await CheckIsDrawingShapesInMultiTouchMode();
            forceEraser = true;
            drawingShapeMode = 16;
            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            inkCanvas.IsManipulationEnabled = true;
            CancelSingleFingerDragMode();
            DrawShapePromptToPen();
        }

        public async void BtnDrawCenterEllipseWithFocalPoint_Click(object sender, MouseButtonEventArgs e) {
            await CheckIsDrawingShapesInMultiTouchMode();
            forceEraser = true;
            drawingShapeMode = 23;
            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            inkCanvas.IsManipulationEnabled = true;
            CancelSingleFingerDragMode();
            DrawShapePromptToPen();
        }

        public async void BtnDrawDashedCircle_Click(object sender, MouseButtonEventArgs e) {
            await CheckIsDrawingShapesInMultiTouchMode();
            forceEraser = true;
            drawingShapeMode = 10;
            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            inkCanvas.IsManipulationEnabled = true;
            CancelSingleFingerDragMode();
            DrawShapePromptToPen();
        }

        public async void BtnDrawHyperbola_Click(object sender, MouseButtonEventArgs e) {
            await CheckIsDrawingShapesInMultiTouchMode();
            forceEraser = true;
            drawingShapeMode = 24;
            drawMultiStepShapeCurrentStep = 0;
            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            inkCanvas.IsManipulationEnabled = true;
            CancelSingleFingerDragMode();
            DrawShapePromptToPen();
        }

        public async void BtnDrawHyperbolaWithFocalPoint_Click(object sender, MouseButtonEventArgs e) {
            await CheckIsDrawingShapesInMultiTouchMode();
            forceEraser = true;
            drawingShapeMode = 25;
            drawMultiStepShapeCurrentStep = 0;
            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            inkCanvas.IsManipulationEnabled = true;
            CancelSingleFingerDragMode();
            DrawShapePromptToPen();
        }

        public async void BtnDrawParabola1_Click(object sender, MouseButtonEventArgs e) {
            await CheckIsDrawingShapesInMultiTouchMode();
            forceEraser = true;
            drawingShapeMode = 20;
            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            inkCanvas.IsManipulationEnabled = true;
            CancelSingleFingerDragMode();
            DrawShapePromptToPen();
        }

        public async void BtnDrawParabolaWithFocalPoint_Click(object sender, MouseButtonEventArgs e) {
            await CheckIsDrawingShapesInMultiTouchMode();
            forceEraser = true;
            drawingShapeMode = 22;
            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            inkCanvas.IsManipulationEnabled = true;
            CancelSingleFingerDragMode();
            DrawShapePromptToPen();
        }

        public async void BtnDrawParabola2_Click(object sender, MouseButtonEventArgs e) {
            await CheckIsDrawingShapesInMultiTouchMode();
            forceEraser = true;
            drawingShapeMode = 21;
            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            inkCanvas.IsManipulationEnabled = true;
            CancelSingleFingerDragMode();
            DrawShapePromptToPen();
        }

        public async void BtnDrawCylinder_Click(object sender, MouseButtonEventArgs e) {
            await CheckIsDrawingShapesInMultiTouchMode();
            forceEraser = true;
            drawingShapeMode = 6;
            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            inkCanvas.IsManipulationEnabled = true;
            CancelSingleFingerDragMode();
            DrawShapePromptToPen();
        }

        public async void BtnDrawCone_Click(object sender, MouseButtonEventArgs e) {
            await CheckIsDrawingShapesInMultiTouchMode();
            forceEraser = true;
            drawingShapeMode = 7;
            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            inkCanvas.IsManipulationEnabled = true;
            CancelSingleFingerDragMode();
            DrawShapePromptToPen();
        }

        public async void BtnDrawCuboid_Click(object sender, MouseButtonEventArgs e) {
            await CheckIsDrawingShapesInMultiTouchMode();
            forceEraser = true;
            drawingShapeMode = 9;
            isFirstTouchCuboid = true;
            CuboidFrontRectIniP = new Point();
            CuboidFrontRectEndP = new Point();
            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            inkCanvas.IsManipulationEnabled = true;
            CancelSingleFingerDragMode();
            DrawShapePromptToPen();
        }

        #endregion

        private void inkCanvas_TouchMove(object sender, TouchEventArgs e) {
            // 先处理手掌橡皮移动
            if (isPalmErasing) {
                Main_Grid_TouchMove_PalmEraser(sender, e);
                return;
            }

            if (!Settings.Gesture.DisableGestureEraser && Settings.Gesture.PalmEraserDetectOnMove) {
                bool shouldCheckPalmEraser = Settings.Advanced.TouchMultiplier != 0 || !Settings.Advanced.IsSpecialScreen;
                if (shouldCheckPalmEraser && IsPalmTouch(e, out double palmWidth)) {
                    isLastTouchEraser = true;
                    if (drawingShapeMode == 0 && forceEraser) return;
                    StartPalmEraser(e, palmWidth);
                    return;
                }
            }

            if (isSingleFingerDragMode) return;
            if (drawingShapeMode != 0) {
                if (isLastTouchEraser) return;
                if (isWaitUntilNextTouchDown) return;
                if (dec.Count > 1) {
                    isWaitUntilNextTouchDown = true;
                    try {
                        inkCanvas.Strokes.Remove(lastTempStroke);
                        inkCanvas.Strokes.Remove(lastTempStrokeCollection);
                    }
                    catch {
                        Trace.WriteLine("lastTempStrokeCollection failed.");
                    }

                    return;
                }

                if (inkCanvas.EditingMode != InkCanvasEditingMode.None)
                    inkCanvas.EditingMode = InkCanvasEditingMode.None;
            }

            MouseTouchMove(e.GetTouchPoint(inkCanvas).Position);
        }

        private int drawMultiStepShapeCurrentStep = 0;
        private StrokeCollection drawMultiStepShapeSpecialStrokeCollection = new StrokeCollection();
        private double drawMultiStepShapeSpecialParameter3 = 0.0;

        #region 形状绘制主函数

        /// <summary>
        /// 是否使用重构后的形状绘制系统
        /// </summary>
        private bool _useRefactoredShapeDrawing = true;

        private void MouseTouchMove(Point endP) {
            if (Settings.Canvas.FitToCurve == true) drawingAttributes.FitToCurve = false;
            ViewboxFloatingBar.IsHitTestVisible = false;
            BlackboardUIGridForInkReplay.IsHitTestVisible = false;

            // 尝试使用重构后的形状绘制系统
            // 注意：多步绘制形状（立方体9、双曲线24、双曲线带焦点25）仍使用旧系统处理
            if (_useRefactoredShapeDrawing && drawingShapeMode != 0 && drawingShapeMode != 9 && drawingShapeMode != 24 && drawingShapeMode != 25) {
                if (TryDrawShapeWithRefactoredSystem(endP)) {
                    return; // 新系统成功处理，直接返回
                }
            }

            // 回退到旧系统（仅处理多步绘制形状：立方体、双曲线、双曲线带焦点）
            List<Point> pointList;
            StylusPointCollection point;
            Stroke stroke;
            var strokes = new StrokeCollection();
            var newIniP = iniP;
            switch (drawingShapeMode) {
                case 24:
                case 25:
                    _currentCommitType = CommitReason.ShapeDrawing;
                    if (Math.Abs(iniP.X - endP.X) < 0.01 || Math.Abs(iniP.Y - endP.Y) < 0.01) return;
                    var pointList2 = new List<Point>();
                    var pointList3 = new List<Point>();
                    var pointList4 = new List<Point>();
                    if (drawMultiStepShapeCurrentStep == 0) {
                        var k = Math.Abs((endP.Y - iniP.Y) / (endP.X - iniP.X));
                        strokes.Add(
                            GenerateDashedLineStrokeCollection(new Point(2 * iniP.X - endP.X, 2 * iniP.Y - endP.Y),
                                endP));
                        strokes.Add(GenerateDashedLineStrokeCollection(new Point(2 * iniP.X - endP.X, endP.Y),
                            new Point(endP.X, 2 * iniP.Y - endP.Y)));
                        drawMultiStepShapeSpecialParameter3 = k;
                        drawMultiStepShapeSpecialStrokeCollection = strokes;
                    }
                    else {
                        var k = drawMultiStepShapeSpecialParameter3;
                        var isHyperbolaFocalPointOnXAxis = Math.Abs((endP.Y - iniP.Y) / (endP.X - iniP.X)) < k;
                        double a, b;
                        if (isHyperbolaFocalPointOnXAxis) {
                            a = Math.Sqrt(Math.Abs((endP.X - iniP.X) * (endP.X - iniP.X) -
                                                   (endP.Y - iniP.Y) * (endP.Y - iniP.Y) / (k * k)));
                            b = a * k;
                            pointList = new List<Point>();
                            for (var i = a; i <= Math.Abs(endP.X - iniP.X); i += 0.5) {
                                var rY = Math.Sqrt(Math.Abs(k * k * i * i - b * b));
                                pointList.Add(new Point(iniP.X + i, iniP.Y - rY));
                                pointList2.Add(new Point(iniP.X + i, iniP.Y + rY));
                                pointList3.Add(new Point(iniP.X - i, iniP.Y - rY));
                                pointList4.Add(new Point(iniP.X - i, iniP.Y + rY));
                            }
                        }
                        else {
                            a = Math.Sqrt(Math.Abs((endP.Y - iniP.Y) * (endP.Y - iniP.Y) -
                                                   (endP.X - iniP.X) * (endP.X - iniP.X) * (k * k)));
                            b = a / k;
                            pointList = new List<Point>();
                            for (var i = a; i <= Math.Abs(endP.Y - iniP.Y); i += 0.5) {
                                var rX = Math.Sqrt(Math.Abs(i * i / k / k - b * b));
                                pointList.Add(new Point(iniP.X - rX, iniP.Y + i));
                                pointList2.Add(new Point(iniP.X + rX, iniP.Y + i));
                                pointList3.Add(new Point(iniP.X - rX, iniP.Y - i));
                                pointList4.Add(new Point(iniP.X + rX, iniP.Y - i));
                            }
                        }

                        try {
                            point = new StylusPointCollection(pointList);
                            stroke = new Stroke(point)
                                { DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone() };
                            strokes.Add(stroke.Clone());
                            point = new StylusPointCollection(pointList2);
                            stroke = new Stroke(point)
                                { DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone() };
                            strokes.Add(stroke.Clone());
                            point = new StylusPointCollection(pointList3);
                            stroke = new Stroke(point)
                                { DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone() };
                            strokes.Add(stroke.Clone());
                            point = new StylusPointCollection(pointList4);
                            stroke = new Stroke(point)
                                { DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone() };
                            strokes.Add(stroke.Clone());
                            if (drawingShapeMode == 25) {
                                var c = Math.Sqrt(a * a + b * b);
                                var stylusPoint = isHyperbolaFocalPointOnXAxis
                                    ? new StylusPoint(iniP.X + c, iniP.Y, (float)1.0)
                                    : new StylusPoint(iniP.X, iniP.Y + c, (float)1.0);
                                point = new StylusPointCollection();
                                point.Add(stylusPoint);
                                stroke = new Stroke(point)
                                    { DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone() };
                                strokes.Add(stroke.Clone());
                                stylusPoint = isHyperbolaFocalPointOnXAxis
                                    ? new StylusPoint(iniP.X - c, iniP.Y, (float)1.0)
                                    : new StylusPoint(iniP.X, iniP.Y - c, (float)1.0);
                                point = new StylusPointCollection();
                                point.Add(stylusPoint);
                                stroke = new Stroke(point)
                                    { DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone() };
                                strokes.Add(stroke.Clone());
                            }
                        }
                        catch {
                            return;
                        }
                    }

                    try {
                        inkCanvas.Strokes.Remove(lastTempStrokeCollection);
                    }
                    catch {
                        Trace.WriteLine("lastTempStrokeCollection failed.");
                    }

                    lastTempStrokeCollection = strokes;
                    inkCanvas.Strokes.Add(strokes);
                    break;
                case 9:
                    _currentCommitType = CommitReason.ShapeDrawing;
                    if (isFirstTouchCuboid) {
                        strokes.Add(GenerateLineStroke(new Point(iniP.X, iniP.Y), new Point(iniP.X, endP.Y)));
                        strokes.Add(GenerateLineStroke(new Point(iniP.X, endP.Y), new Point(endP.X, endP.Y)));
                        strokes.Add(GenerateLineStroke(new Point(endP.X, endP.Y), new Point(endP.X, iniP.Y)));
                        strokes.Add(GenerateLineStroke(new Point(iniP.X, iniP.Y), new Point(endP.X, iniP.Y)));
                        try {
                            inkCanvas.Strokes.Remove(lastTempStrokeCollection);
                        }
                        catch {
                            Trace.WriteLine("lastTempStrokeCollection failed.");
                        }

                        lastTempStrokeCollection = strokes;
                        inkCanvas.Strokes.Add(strokes);
                        CuboidFrontRectIniP = iniP;
                        CuboidFrontRectEndP = endP;
                    }
                    else {
                        var d = CuboidFrontRectIniP.Y - endP.Y;
                        if (d < 0) d = -d;
                        var a = CuboidFrontRectEndP.X - CuboidFrontRectIniP.X;
                        var b = CuboidFrontRectEndP.Y - CuboidFrontRectIniP.Y;

                        var newLineIniP = new Point(CuboidFrontRectIniP.X + d, CuboidFrontRectIniP.Y - d);
                        var newLineEndP = new Point(CuboidFrontRectEndP.X + d, CuboidFrontRectIniP.Y - d);
                        pointList = new List<Point> { newLineIniP, newLineEndP };
                        point = new StylusPointCollection(pointList);
                        stroke = new Stroke(point) { DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone() };
                        strokes.Add(stroke.Clone());
                        newLineIniP = new Point(CuboidFrontRectIniP.X + d, CuboidFrontRectEndP.Y - d);
                        newLineEndP = new Point(CuboidFrontRectEndP.X + d, CuboidFrontRectEndP.Y - d);
                        strokes.Add(GenerateDashedLineStrokeCollection(newLineIniP, newLineEndP));
                        newLineIniP = new Point(CuboidFrontRectIniP.X, CuboidFrontRectIniP.Y);
                        newLineEndP = new Point(CuboidFrontRectIniP.X + d, CuboidFrontRectIniP.Y - d);
                        pointList = new List<Point> { newLineIniP, newLineEndP };
                        point = new StylusPointCollection(pointList);
                        stroke = new Stroke(point) { DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone() };
                        strokes.Add(stroke.Clone());
                        newLineIniP = new Point(CuboidFrontRectEndP.X, CuboidFrontRectIniP.Y);
                        newLineEndP = new Point(CuboidFrontRectEndP.X + d, CuboidFrontRectIniP.Y - d);
                        pointList = new List<Point> { newLineIniP, newLineEndP };
                        point = new StylusPointCollection(pointList);
                        stroke = new Stroke(point) { DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone() };
                        strokes.Add(stroke.Clone());
                        newLineIniP = new Point(CuboidFrontRectIniP.X, CuboidFrontRectEndP.Y);
                        newLineEndP = new Point(CuboidFrontRectIniP.X + d, CuboidFrontRectEndP.Y - d);
                        strokes.Add(GenerateDashedLineStrokeCollection(newLineIniP, newLineEndP));
                        newLineIniP = new Point(CuboidFrontRectEndP.X, CuboidFrontRectEndP.Y);
                        newLineEndP = new Point(CuboidFrontRectEndP.X + d, CuboidFrontRectEndP.Y - d);
                        pointList = new List<Point> { newLineIniP, newLineEndP };
                        point = new StylusPointCollection(pointList);
                        stroke = new Stroke(point) { DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone() };
                        strokes.Add(stroke.Clone());
                        newLineIniP = new Point(CuboidFrontRectIniP.X + d, CuboidFrontRectIniP.Y - d);
                        newLineEndP = new Point(CuboidFrontRectIniP.X + d, CuboidFrontRectEndP.Y - d);
                        strokes.Add(GenerateDashedLineStrokeCollection(newLineIniP, newLineEndP));
                        newLineIniP = new Point(CuboidFrontRectEndP.X + d, CuboidFrontRectIniP.Y - d);
                        newLineEndP = new Point(CuboidFrontRectEndP.X + d, CuboidFrontRectEndP.Y - d);
                        pointList = new List<Point> { newLineIniP, newLineEndP };
                        point = new StylusPointCollection(pointList);
                        stroke = new Stroke(point) { DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone() };
                        strokes.Add(stroke.Clone());

                        try {
                            inkCanvas.Strokes.Remove(lastTempStrokeCollection);
                        }
                        catch {
                            Trace.WriteLine("lastTempStrokeCollection failed.");
                        }

                        lastTempStrokeCollection = strokes;
                        inkCanvas.Strokes.Add(strokes);
                    }

                    break;
            }
        }

        /// <summary>
        /// 尝试使用重构后的系统绘制形状
        /// </summary>
        /// <param name="endP">终点坐标</param>
        /// <returns>是否成功处理</returns>
        private bool TryDrawShapeWithRefactoredSystem(Point endP) {
            // 多步绘制形状暂时使用旧系统处理
            // 包括：立方体(9)、双曲线(24、25)
            if (drawingShapeMode == 9 || drawingShapeMode == 24 || drawingShapeMode == 25) {
                return false;
            }

            // 尝试将旧模式转换为新的形状类型
            var shapeType = _shapeDrawingService.ConvertFromLegacyMode(drawingShapeMode);
            if (shapeType == null) {
                return false; // 无法转换，让旧系统处理
            }

            try {
                _currentCommitType = CommitReason.ShapeDrawing;

                // 使用策略模式绘制形状
                StrokeCollection newStrokes = _shapeDrawingService.CreateShape(
                    iniP,
                    endP,
                    shapeType.Value,
                    inkCanvas.DefaultDrawingAttributes
                );

                if (newStrokes == null || newStrokes.Count == 0) {
                    return false;
                }

                // 移除上一次的临时笔画
                try {
                    if (lastTempStroke != null) {
                        inkCanvas.Strokes.Remove(lastTempStroke);
                    }
                } catch (Exception ex) {
                    LogHelper.WriteLogToFile($"Failed to remove last temp stroke: {ex.Message}", LogHelper.LogType.Trace);
                    // Non-critical error, continue
                }

                try {
                    if (lastTempStrokeCollection != null && lastTempStrokeCollection.Count > 0) {
                        inkCanvas.Strokes.Remove(lastTempStrokeCollection);
                    }
                } catch (Exception ex) {
                    LogHelper.WriteLogToFile($"Failed to remove last temp stroke collection: {ex.Message}", LogHelper.LogType.Trace);
                    // Non-critical error, continue
                }

                // 添加新笔画
                if (newStrokes.Count == 1) {
                    lastTempStroke = newStrokes[0];
                    lastTempStrokeCollection = null;
                } else {
                    lastTempStroke = null;
                    lastTempStrokeCollection = newStrokes;
                }

                inkCanvas.Strokes.Add(newStrokes);
                return true;
            }
            catch (Exception ex) {
                LogHelper.WriteLogToFile(
                    $"Exception in TryDrawShapeWithRefactoredSystem: {ex.Message}",
                    LogHelper.LogType.Error
                );
                return false;
            }
        }

        #endregion

        private bool isFirstTouchCuboid = true;
        private Point CuboidFrontRectIniP = new Point();
        private Point CuboidFrontRectEndP = new Point();

        private void Main_Grid_TouchUp(object sender, TouchEventArgs e) {
            // 先处理手掌橡皮抬起
            if (isPalmErasing) {
                Main_Grid_TouchUp_PalmEraser(sender, e);
            }

            inkCanvas.ReleaseAllTouchCaptures();
            ViewboxFloatingBar.IsHitTestVisible = true;
            BlackboardUIGridForInkReplay.IsHitTestVisible = true;

            inkCanvas_MouseUp(sender, null);
            if (dec.Count == 0) isWaitUntilNextTouchDown = false;
        }

        private Stroke lastTempStroke = null;
        private StrokeCollection lastTempStrokeCollection = new StrokeCollection();

        private bool isWaitUntilNextTouchDown = false;

        /// <summary>
        /// 生成椭圆几何点集（包装方法，调用 ShapeDrawingHelper）
        /// </summary>
        private List<Point> GenerateEllipseGeometry(Point st, Point ed, bool isDrawTop = true,
            bool isDrawBottom = true) {
            return ShapeDrawingHelper.GenerateEllipseGeometry(st, ed, isDrawTop, isDrawBottom);
        }

        /// <summary>
        /// 生成虚线椭圆墨迹集合（包装方法，调用 ShapeDrawingHelper）
        /// </summary>
        private StrokeCollection GenerateDashedLineEllipseStrokeCollection(Point st, Point ed, bool isDrawTop = true,
            bool isDrawBottom = true) {
            return ShapeDrawingHelper.GenerateDashedLineEllipseStrokeCollection(st, ed, inkCanvas.DefaultDrawingAttributes, isDrawTop, isDrawBottom);
        }

        /// <summary>
        /// 生成直线墨迹（包装方法，调用 ShapeDrawingHelper）
        /// </summary>
        private Stroke GenerateLineStroke(Point st, Point ed) {
            return ShapeDrawingHelper.GenerateLineStroke(st, ed, inkCanvas.DefaultDrawingAttributes);
        }

        /// <summary>
        /// 生成箭头直线墨迹（包装方法，调用 ShapeDrawingHelper）
        /// </summary>
        private Stroke GenerateArrowLineStroke(Point st, Point ed) {
            return ShapeDrawingHelper.GenerateArrowLineStroke(st, ed, inkCanvas.DefaultDrawingAttributes);
        }

        /// <summary>
        /// 生成虚线墨迹集合（包装方法，调用 ShapeDrawingHelper）
        /// </summary>
        private StrokeCollection GenerateDashedLineStrokeCollection(Point st, Point ed) {
            return ShapeDrawingHelper.GenerateDashedLineStrokeCollection(st, ed, inkCanvas.DefaultDrawingAttributes);
        }

        /// <summary>
        /// 生成点线墨迹集合（包装方法，调用 ShapeDrawingHelper）
        /// </summary>
        private StrokeCollection GenerateDotLineStrokeCollection(Point st, Point ed) {
            return ShapeDrawingHelper.GenerateDotLineStrokeCollection(st, ed, inkCanvas.DefaultDrawingAttributes);
        }

        private bool isMouseDown = false;

        private void inkCanvas_MouseDown(object sender, MouseButtonEventArgs e) {
            inkCanvas.CaptureMouse();
            ViewboxFloatingBar.IsHitTestVisible = false;
            BlackboardUIGridForInkReplay.IsHitTestVisible = false;

            isMouseDown = true;
            if (NeedUpdateIniP()) iniP = e.GetPosition(inkCanvas);
        }

        private void inkCanvas_MouseMove(object sender, MouseEventArgs e) {
            if (Settings.Gesture.EnableMouseGesture) InkCanvas_MouseGesture_MouseMove(sender, e);
            if (isMouseDown) MouseTouchMove(e.GetPosition(inkCanvas));
        }

        private void inkCanvas_MouseUp(object sender, MouseButtonEventArgs e) {
            inkCanvas.ReleaseMouseCapture();
            ViewboxFloatingBar.IsHitTestVisible = true;
            BlackboardUIGridForInkReplay.IsHitTestVisible = true;

            if (drawingShapeMode == 5) {
                if (lastTempStroke != null) {
                    var circle = new Circle(new Point(), 0, lastTempStroke);
                    circle.R = GetDistance(circle.Stroke.StylusPoints[0].ToPoint(),
                        circle.Stroke.StylusPoints[circle.Stroke.StylusPoints.Count / 2].ToPoint()) / 2;
                    circle.Centroid = new Point(
                        (circle.Stroke.StylusPoints[0].X +
                         circle.Stroke.StylusPoints[circle.Stroke.StylusPoints.Count / 2].X) / 2,
                        (circle.Stroke.StylusPoints[0].Y +
                         circle.Stroke.StylusPoints[circle.Stroke.StylusPoints.Count / 2].Y) / 2);
                    circles.Add(circle);
                }

                if (lastIsInMultiTouchMode) {
                    isInMultiTouchMode = true;
                    Settings.Gesture.DefaultMultiPointHandWritingMode = 0;
                    SaveSettings();
                    lastIsInMultiTouchMode = false;
                }
            }

            if (drawingShapeMode != 9 && drawingShapeMode != 0 && drawingShapeMode != 24 && drawingShapeMode != 25) {
                    if (isLongPressSelected) { }
                    else {
                        BtnPen_Click(null, null);
                        if (lastIsInMultiTouchMode) {
                            isInMultiTouchMode = true;
                            Settings.Gesture.DefaultMultiPointHandWritingMode = 0;
                            SaveSettings();
                            lastIsInMultiTouchMode = false;
                        }
                }
            }

            if (drawingShapeMode == 9) {
                if (isFirstTouchCuboid) {
                    if (CuboidStrokeCollection == null) CuboidStrokeCollection = new StrokeCollection();
                    isFirstTouchCuboid = false;
                    var newIniP = new Point(Math.Min(CuboidFrontRectIniP.X, CuboidFrontRectEndP.X),
                        Math.Min(CuboidFrontRectIniP.Y, CuboidFrontRectEndP.Y));
                    var newEndP = new Point(Math.Max(CuboidFrontRectIniP.X, CuboidFrontRectEndP.X),
                        Math.Max(CuboidFrontRectIniP.Y, CuboidFrontRectEndP.Y));
                    CuboidFrontRectIniP = newIniP;
                    CuboidFrontRectEndP = newEndP;
                    try {
                        CuboidStrokeCollection.Add(lastTempStrokeCollection);
                    }
                    catch {
                        Trace.WriteLine("lastTempStrokeCollection failed.");
                    }
                }
                else {
                    BtnPen_Click(null, null);
                    if (lastIsInMultiTouchMode) {
                        isInMultiTouchMode = true;
                        Settings.Gesture.DefaultMultiPointHandWritingMode = 0;
                        SaveSettings();
                        lastIsInMultiTouchMode = false;
                    }

                    if (_currentCommitType == CommitReason.ShapeDrawing) {
                        try {
                            CuboidStrokeCollection.Add(lastTempStrokeCollection);
                        }
                        catch {
                            Trace.WriteLine("lastTempStrokeCollection failed.");
                        }

                        _currentCommitType = CommitReason.UserInput;
                        timeMachine.CommitStrokeUserInputHistory(CuboidStrokeCollection);
                        CuboidStrokeCollection = null;
                    }
                }
            }

            if (drawingShapeMode == 24 || drawingShapeMode == 25) {
                if (drawMultiStepShapeCurrentStep == 0) {
                    drawMultiStepShapeCurrentStep = 1;
                }
                else {
                    drawMultiStepShapeCurrentStep = 0;
                    if (drawMultiStepShapeSpecialStrokeCollection != null) {
                        var opFlag = false;
                        switch (Settings.Canvas.HyperbolaAsymptoteOption) {
                            case OptionalOperation.Yes:
                                opFlag = true;
                                break;
                            case OptionalOperation.No:
                                opFlag = false;
                                break;
                            case OptionalOperation.Ask:
                                opFlag = MessageBox.Show("是否移除渐近线？", "Ink Canvas", MessageBoxButton.YesNo) !=
                                         MessageBoxResult.Yes;
                                break;
                        }

                        ;
                        if (!opFlag) inkCanvas.Strokes.Remove(drawMultiStepShapeSpecialStrokeCollection);
                    }

                    BtnPen_Click(null, null);
                    if (lastIsInMultiTouchMode) {
                        isInMultiTouchMode = true;
                        Settings.Gesture.DefaultMultiPointHandWritingMode = 0;
                        SaveSettings();
                        lastIsInMultiTouchMode = false;
                    }
                }
            }

            isMouseDown = false;

            if (_currentCommitType == CommitReason.ShapeDrawing && drawingShapeMode != 9) {
                _currentCommitType = CommitReason.UserInput;
                StrokeCollection collection = null;
                if (lastTempStrokeCollection != null && lastTempStrokeCollection.Count > 0)
                    collection = lastTempStrokeCollection;
                else if (lastTempStroke != null) collection = new StrokeCollection() { lastTempStroke };
                if (collection != null) timeMachine.CommitStrokeUserInputHistory(collection);
            }

            lastTempStroke = null;
            lastTempStrokeCollection = null;

            if (StrokeManipulationHistory?.Count > 0)
            {
                timeMachine.CommitStrokeManipulationHistory(StrokeManipulationHistory);
                foreach (var item in StrokeManipulationHistory)
                {
                    StrokeInitialHistory[item.Key] = item.Value.Item2;
                }
                StrokeManipulationHistory = null;
            }

            if (DrawingAttributesHistory.Count > 0)
            {
                timeMachine.CommitStrokeDrawingAttributesHistory(DrawingAttributesHistory);
                DrawingAttributesHistory = new Dictionary<Stroke, Tuple<DrawingAttributes, DrawingAttributes>>();
                foreach (var item in DrawingAttributesHistoryFlag)
                {
                    item.Value.Clear();
                }
            }

            if (Settings.Canvas.FitToCurve == true) drawingAttributes.FitToCurve = true;
        }

        private bool NeedUpdateIniP() {
            if (drawingShapeMode == 24 || drawingShapeMode == 25)
                if (drawMultiStepShapeCurrentStep == 1)
                    return false;
            return true;
        }


        #region ShapeDrawingV2

        public void ShapeDrawingV2Init() {
            ShapeDrawingV2Layer.MainWindow = this;
            ShapeDrawingV2.ShapeDrawingPopupShouldCloseEvent += (sender, args) => {
                ShapeDrawingPopupV2.IsOpen = false;
            };
            ShapeDrawingV2.ShapeSelectedEvent += (sender, args) => {
                ShapeDrawingV2Layer.StartShapeDrawing(args.Type, args.Name);
            };
        }

        public enum ShapeDrawingType {
            Line,
            DottedLine,
            DashedLine,
            ArrowOneSide,
            ArrowTwoSide,
            Rectangle,
            Ellipse,
            PieEllipse,
            Triangle,
            RightTriangle,
            Diamond,
            Parallelogram,
            FourLine,
            Staff,
            Axis2D,
            Axis2DA,
            Axis2DB,
            Axis2DC,
            Axis3D,
            Hyperbola,
            HyperbolaF,
            Parabola,
            ParabolaA,
            ParabolaAF,
            Cylinder,
            Cube,
            Cone,
            EllipseC,
            RectangleC,
            CoordinateGrid
        }

        #endregion
    }
}
