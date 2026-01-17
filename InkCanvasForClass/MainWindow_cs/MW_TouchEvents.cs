using Ink_Canvas.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using Point = System.Windows.Point;

namespace Ink_Canvas {
    public partial class MainWindow : Window {
        #region Palm Eraser State

        /// <summary>
        /// 是否处于手掌橡皮模式（自动检测触发）
        /// </summary>
        private bool isPalmErasing = false;

        /// <summary>
        /// 手掌橡皮触发前的编辑模式（用于恢复）
        /// </summary>
        private InkCanvasEditingMode editModeBeforePalmEraser = InkCanvasEditingMode.Ink;

        /// <summary>
        /// 当前手掌触摸设备的ID
        /// </summary>
        private int palmTouchDeviceId = -1;

        /// <summary>
        /// 手掌触摸的初始位置
        /// </summary>
        private Point palmTouchStartPoint = new Point();

        /// <summary>
        /// 手掌橡皮的增量命中测试器
        /// </summary>
        private IncrementalStrokeHitTester palmHitTester = null;

        /// <summary>
        /// 手掌橡皮的缩放矩阵
        /// </summary>
        private Matrix palmScaleMatrix = new Matrix();

        /// <summary>
        /// 手掌橡皮上次更新位置
        /// </summary>
        private Point? lastPalmEraserPoint = null;

        /// <summary>
        /// 手掌橡皮上次更新时间戳
        /// </summary>
        private int lastPalmEraserUpdateTick = 0;

        /// <summary>
        /// 手掌橡皮的宽度
        /// </summary>
        private double palmEraserWidth = 64;

        /// <summary>
        /// 连续检测到的大触摸点数量（用于稳定性判断）
        /// </summary>
        private int largeTouchDetectionCount = 0;

        /// <summary>
        /// 手掌橡皮触发连续检测次数（可配置）
        /// </summary>
        private int PalmEraserDetectionThreshold =>
            Math.Max(1, Settings?.Gesture?.PalmEraserDetectionThreshold ?? 3);

        /// <summary>
        /// 手掌橡皮巨大触摸面积倍率（可配置）
        /// </summary>
        private double PalmEraserHugeAreaMultiplier =>
            Math.Max(1.0, Settings?.Gesture?.PalmEraserHugeAreaMultiplier ?? 2.5);

        /// <summary>
        /// 手掌橡皮移动时最小更新距离（像素，可配置）
        /// </summary>
        private double PalmEraserMinMove =>
            Math.Max(0.5, Settings?.Gesture?.PalmEraserMinMove ?? 2.5);

        /// <summary>
        /// 手掌橡皮移动时最小更新间隔（毫秒，可配置）
        /// </summary>
        private int PalmEraserMinIntervalMs =>
            Math.Max(0, Settings?.Gesture?.PalmEraserMinIntervalMs ?? 12);

        #endregion

        #region Multi-Touch

        private bool isInMultiTouchMode = false;

        private void BorderMultiTouchMode_MouseUp(object sender, MouseButtonEventArgs e) {
            if (isInMultiTouchMode) {
                inkCanvas.StylusDown -= MainWindow_StylusDown;
                inkCanvas.StylusMove -= MainWindow_StylusMove;
                inkCanvas.StylusUp -= MainWindow_StylusUp;
                inkCanvas.TouchDown -= MainWindow_TouchDown;
                inkCanvas.TouchDown += Main_Grid_TouchDown;
                inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
                inkCanvas.Children.Clear();
                isInMultiTouchMode = false;
            } else {
                inkCanvas.StylusDown += MainWindow_StylusDown;
                inkCanvas.StylusMove += MainWindow_StylusMove;
                inkCanvas.StylusUp += MainWindow_StylusUp;
                inkCanvas.TouchDown += MainWindow_TouchDown;
                inkCanvas.TouchDown -= Main_Grid_TouchDown;
                inkCanvas.EditingMode = InkCanvasEditingMode.None;
                inkCanvas.Children.Clear();
                isInMultiTouchMode = true;
            }
        }

        private void MainWindow_TouchDown(object sender, TouchEventArgs e) {
            if (!isCursorHidden && Settings.Gesture.HideCursorWhenUsingTouchDevice) {
                System.Windows.Forms.Cursor.Hide();
                isCursorHidden = true;
            }

            if (inkCanvas.EditingMode == InkCanvasEditingMode.EraseByPoint
                || inkCanvas.EditingMode == InkCanvasEditingMode.EraseByStroke
                || inkCanvas.EditingMode == InkCanvasEditingMode.Select) return;

            if (!isHidingSubPanelsWhenInking) {
                isHidingSubPanelsWhenInking = true;
                HideSubPanels();
            }

            if (!Settings.Gesture.DisableGestureEraser) {
                double boundWidth = e.GetTouchPoint(null).Bounds.Width;
                if ((Settings.Advanced.TouchMultiplier != 0 || !Settings.Advanced.IsSpecialScreen)
                    && (boundWidth > BoundsWidth)) {
                    if (drawingShapeMode == 0 && forceEraser) return;
                    double EraserThresholdValue = Settings.Startup.IsEnableNibMode
                        ? Settings.Advanced.NibModeBoundsWidthThresholdValue
                        : Settings.Advanced.FingerModeBoundsWidthThresholdValue;
                    if (boundWidth > BoundsWidth * EraserThresholdValue) {
                        boundWidth *= (Settings.Startup.IsEnableNibMode
                            ? Settings.Advanced.NibModeBoundsWidthEraserSize
                            : Settings.Advanced.FingerModeBoundsWidthEraserSize);
                        if (Settings.Advanced.IsSpecialScreen) boundWidth *= Settings.Advanced.TouchMultiplier;
                        TouchDownPointsList[e.TouchDevice.Id] = InkCanvasEditingMode.EraseByPoint;
                        eraserWidth = boundWidth;
                        isEraserCircleShape = Settings.Canvas.EraserShapeType == 0;
                        isUsingStrokesEraser = false;
                        inkCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint;
                    } else {
                        isUsingStrokesEraser = true;
                        inkCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
                    }
                } else {
                    inkCanvas.EraserShape =
                        forcePointEraser ? new EllipseStylusShape(50, 50) : new EllipseStylusShape(5, 5);
                    TouchDownPointsList[e.TouchDevice.Id] = InkCanvasEditingMode.None;
                    inkCanvas.EditingMode = InkCanvasEditingMode.None;
                }
            }
        }

        private void MainWindow_StylusDown(object sender, StylusDownEventArgs e) {
            if (e.StylusDevice.TabletDevice.Type == TabletDeviceType.Touch) {
                if (!isCursorHidden && Settings.Gesture.HideCursorWhenUsingTouchDevice &&
                    e.StylusDevice.TabletDevice.Type == TabletDeviceType.Touch) {
                    System.Windows.Forms.Cursor.Hide();
                    isCursorHidden = true;
                }

                ViewboxFloatingBar.IsHitTestVisible = false;
                BlackboardUIGridForInkReplay.IsHitTestVisible = false;

                if (inkCanvas.EditingMode == InkCanvasEditingMode.EraseByPoint
                    || inkCanvas.EditingMode == InkCanvasEditingMode.EraseByStroke
                    || inkCanvas.EditingMode == InkCanvasEditingMode.Select) return;

                TouchDownPointsList[e.StylusDevice.Id] = InkCanvasEditingMode.None;

                try {
                    var strokeVisual = GetStrokeVisual(e.StylusDevice.Id);
                    var stylusPoints = e.GetStylusPoints(inkCanvas);
                    if (stylusPoints.Count > 0) {
                        var point = stylusPoints[stylusPoints.Count - 1];
                        strokeVisual.Add(new StylusPoint(point.X, point.Y, point.PressureFactor));
                        strokeVisual.Redraw();
                    }
                }
                catch (Exception ex) {
                    LogHelper.WriteLogToFile("Error in MainWindow_StylusDown (Init Stroke): " + ex.Message,
                        LogHelper.LogType.Error);
                }
            }
        }

        private async void MainWindow_StylusUp(object sender, StylusEventArgs e) {
            if (e.StylusDevice.TabletDevice.Type == TabletDeviceType.Touch) {
                try {
                    inkCanvas.Strokes.Add(GetStrokeVisual(e.StylusDevice.Id).Stroke);
                    await Task.Delay(5); // 避免渲染墨迹完成前预览墨迹被删除导致墨迹闪烁
                    inkCanvas.Children.Remove(GetVisualCanvas(e.StylusDevice.Id));
                    inkCanvas_StrokeCollected(inkCanvas,
                        new InkCanvasStrokeCollectedEventArgs(GetStrokeVisual(e.StylusDevice.Id).Stroke));
                }
                catch (Exception ex) {
                    Label.Content = ex.ToString();
                }

                try {
                    StrokeVisualList.Remove(e.StylusDevice.Id);
                    VisualCanvasList.Remove(e.StylusDevice.Id);
                    TouchDownPointsList.Remove(e.StylusDevice.Id);
                    if (StrokeVisualList.Count == 0 || VisualCanvasList.Count == 0 || TouchDownPointsList.Count == 0) {
                        inkCanvas.Children.Clear();
                        StrokeVisualList.Clear();
                        VisualCanvasList.Clear();
                        TouchDownPointsList.Clear();
                    }
                }
                catch (Exception ex) {
                    LogHelper.WriteLogToFile("Error in MainWindow_StylusUp (Cleanup): " + ex.Message, LogHelper.LogType.Error);
                }

                ViewboxFloatingBar.IsHitTestVisible = true;
                BlackboardUIGridForInkReplay.IsHitTestVisible = true;
            }
        }

        private void MainWindow_StylusMove(object sender, StylusEventArgs e) {
            if (!isCursorHidden && Settings.Gesture.HideCursorWhenUsingTouchDevice &&
                e.StylusDevice.TabletDevice.Type == TabletDeviceType.Touch) {
                System.Windows.Forms.Cursor.Hide();
                isCursorHidden = true;
            }

            if (e.StylusDevice.TabletDevice.Type == TabletDeviceType.Touch) {
                try {
                    if (GetTouchDownPointsList(e.StylusDevice.Id) != InkCanvasEditingMode.None) return;
                    try {
                        if (e.StylusDevice.StylusButtons.Count > 1 &&
                            e.StylusDevice.StylusButtons[1].StylusButtonState == StylusButtonState.Down) return;
                    }
                    catch (Exception ex) {
                        LogHelper.WriteLogToFile("Error in MainWindow_StylusMove (Button Check): " + ex.Message, LogHelper.LogType.Error);
                    }

                    var strokeVisual = GetStrokeVisual(e.StylusDevice.Id);
                    var stylusPointCollection = e.GetStylusPoints(inkCanvas);
                    foreach (var stylusPoint in stylusPointCollection)
                        strokeVisual.Add(new StylusPoint(stylusPoint.X, stylusPoint.Y, stylusPoint.PressureFactor));
                    strokeVisual.Redraw();
                }
                catch (Exception ex) {
                    LogHelper.WriteLogToFile("Error in MainWindow_StylusMove: " + ex.Message, LogHelper.LogType.Error);
                }
            }
        }

        private StrokeVisual GetStrokeVisual(int id) {
            if (StrokeVisualList.TryGetValue(id, out var visual)) return visual;

            var strokeVisual = new StrokeVisual(inkCanvas.DefaultDrawingAttributes.Clone());
            StrokeVisualList[id] = strokeVisual;
            var visualCanvas = new VisualCanvas(strokeVisual);
            VisualCanvasList[id] = visualCanvas;
            inkCanvas.Children.Add(visualCanvas);

            return strokeVisual;
        }

        private VisualCanvas GetVisualCanvas(int id) {
            return VisualCanvasList.TryGetValue(id, out var visualCanvas) ? visualCanvas : null;
        }

        private InkCanvasEditingMode GetTouchDownPointsList(int id) {
            return TouchDownPointsList.TryGetValue(id, out var inkCanvasEditingMode)
                ? inkCanvasEditingMode
                : inkCanvas.EditingMode;
        }

        private Dictionary<int, InkCanvasEditingMode> TouchDownPointsList { get; } =
            new Dictionary<int, InkCanvasEditingMode>();

        private Dictionary<int, StrokeVisual> StrokeVisualList { get; } = new Dictionary<int, StrokeVisual>();
        private Dictionary<int, VisualCanvas> VisualCanvasList { get; } = new Dictionary<int, VisualCanvas>();

        #endregion

        #region Touch Pointer Hide

        public bool isCursorHidden = false;

        private void MainWindow_OnMouseMove(object sender, MouseEventArgs e) {
            if (e.StylusDevice == null) {
                if (isCursorHidden) {
                    System.Windows.Forms.Cursor.Show();
                    isCursorHidden = false;
                }
            } else if (e.StylusDevice.TabletDevice.Type == TabletDeviceType.Stylus) {
                if (isCursorHidden) {
                    System.Windows.Forms.Cursor.Show();
                    isCursorHidden = false;
                }
            }
        }

        #endregion




        private Point iniP = new Point(0, 0);
        private bool isLastTouchEraser = false;
        private bool forcePointEraser = true;

        private void Main_Grid_TouchDown(object sender, TouchEventArgs e) {
            if (!isCursorHidden && Settings.Gesture.HideCursorWhenUsingTouchDevice) {
                System.Windows.Forms.Cursor.Hide();
                isCursorHidden = true;
            }

            inkCanvas.CaptureTouch(e.TouchDevice);
            ViewboxFloatingBar.IsHitTestVisible = false;
            BlackboardUIGridForInkReplay.IsHitTestVisible = false;

            if (!isHidingSubPanelsWhenInking) {
                isHidingSubPanelsWhenInking = true;
                HideSubPanels();
            }

            if (NeedUpdateIniP()) iniP = e.GetTouchPoint(inkCanvas).Position;
            if (drawingShapeMode == 9 && isFirstTouchCuboid == false) MouseTouchMove(iniP);
            inkCanvas.Opacity = 1;

            // 手势橡皮擦检测
            if (!Settings.Gesture.DisableGestureEraser) {
                // 检查特殊屏幕条件
                bool shouldCheckPalmEraser = Settings.Advanced.TouchMultiplier != 0 || !Settings.Advanced.IsSpecialScreen;

                if (shouldCheckPalmEraser) {
                    // 使用优化的手掌检测方法
                    if (IsPalmTouch(e, out double palmWidth)) {
                        // 手掌橡皮模式（面积擦）- 使用自定义可视化
                        isLastTouchEraser = true;
                        if (drawingShapeMode == 0 && forceEraser) return;

                        // 启动手掌橡皮模式
                        StartPalmEraser(e, palmWidth);
                        return;
                    }

                    // 检查是否为墨迹擦（介于笔和手掌之间的触摸）
                    if (IsStrokeEraserTouch(e)) {
                        isLastTouchEraser = true;
                        if (drawingShapeMode == 0 && forceEraser) return;

                        // 墨迹擦模式
                        isUsingStrokesEraser = true;
                        inkCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
                        return;
                    }
                }

                // 普通触摸（笔或手指写字）
                isLastTouchEraser = false;
                largeTouchDetectionCount = 0; // 重置大触摸点计数
                inkCanvas.EraserShape =
                    forcePointEraser ? new EllipseStylusShape(50, 50) : new EllipseStylusShape(5, 5);
                if (forceEraser) return;
                inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
            }
        }

        /// <summary>
        /// 启动手掌橡皮模式
        /// </summary>
        private void StartPalmEraser(TouchEventArgs e, double width) {
            if (isPalmErasing) return; // 已经在手掌橡皮模式中

            isPalmErasing = true;
            palmTouchDeviceId = e.TouchDevice.Id;
            palmEraserWidth = Math.Max(width, 40); // 最小宽度40
            isEraserCircleShape = Settings.Canvas.EraserShapeType == 0;
            isUsingStrokesEraser = false;

            // 保存当前编辑模式
            editModeBeforePalmEraser = inkCanvas.EditingMode;

            // 设置为无编辑模式，我们自己处理擦除
            inkCanvas.EditingMode = InkCanvasEditingMode.None;

            // 显示橡皮擦覆盖层
            GridEraserOverlay.Visibility = Visibility.Visible;

            // 初始化增量命中测试器
            var eraserHeight = palmEraserWidth * 56 / 38; // 保持板擦的宽高比
            palmHitTester = inkCanvas.Strokes.GetIncrementalStrokeHitTester(
                new RectangleStylusShape(palmEraserWidth, eraserHeight));
            palmHitTester.StrokeHit += PalmEraser_StrokeHit;

            // 设置缩放矩阵用于绘制橡皮擦
            var scaleX = palmEraserWidth / 38;
            var scaleY = eraserHeight / 56;
            palmScaleMatrix = new Matrix();
            palmScaleMatrix.ScaleAt(scaleX, scaleY, 0, 0);

            // 启用位图缓存以提高性能
            EraserOverlay_DrawingVisual.CacheMode = new BitmapCache();

            // 绘制初始橡皮擦形状
            var touchPoint = e.GetTouchPoint(Main_Grid).Position;
            DrawPalmEraserFeedback(touchPoint);
            palmHitTester?.AddPoint(touchPoint);

            lastPalmEraserPoint = touchPoint;
            lastPalmEraserUpdateTick = Environment.TickCount;

            palmTouchStartPoint = touchPoint;

            LogHelper.WriteLogToFile($"Palm eraser started, width: {palmEraserWidth}", LogHelper.LogType.Trace);
        }

        /// <summary>
        /// 处理手掌橡皮的触摸移动
        /// </summary>
        private void Main_Grid_TouchMove_PalmEraser(object sender, TouchEventArgs e) {
            if (!isPalmErasing) return;
            if (e.TouchDevice.Id != palmTouchDeviceId) return;

            var touchPoint = e.GetTouchPoint(Main_Grid).Position;

            if (lastPalmEraserPoint.HasValue) {
                var delta = touchPoint - lastPalmEraserPoint.Value;
                var tick = Environment.TickCount;
                var minMove = PalmEraserMinMove;
                if (delta.LengthSquared < minMove * minMove ||
                    tick - lastPalmEraserUpdateTick < PalmEraserMinIntervalMs) {
                    return;
                }

                lastPalmEraserUpdateTick = tick;
            }

            lastPalmEraserPoint = touchPoint;

            // 绘制橡皮擦反馈
            DrawPalmEraserFeedback(touchPoint);

            // 添加点到命中测试器进行擦除
            palmHitTester?.AddPoint(touchPoint);
        }

        /// <summary>
        /// 处理手掌橡皮的触摸抬起
        /// </summary>
        private void Main_Grid_TouchUp_PalmEraser(object sender, TouchEventArgs e) {
            if (!isPalmErasing) return;
            if (e.TouchDevice.Id != palmTouchDeviceId) return;

            EndPalmEraser();
        }

        /// <summary>
        /// 结束手掌橡皮模式
        /// </summary>
        private void EndPalmEraser() {
            if (!isPalmErasing) return;

            isPalmErasing = false;
            palmTouchDeviceId = -1;

            // 重置手掌橡皮相关标志
            isLastTouchEraser = false;
            largeTouchDetectionCount = 0;
            lastPalmEraserPoint = null;
            lastPalmEraserUpdateTick = 0;

            // 隐藏橡皮擦覆盖层
            GridEraserOverlay.Visibility = Visibility.Collapsed;

            // 清除橡皮擦反馈图形
            var ct = EraserOverlay_DrawingVisual.DrawingVisual.RenderOpen();
            ct.DrawRectangle(new SolidColorBrush(Colors.Transparent), null, new Rect(0, 0, ActualWidth, ActualHeight));
            ct.Close();

            // 结束命中测试
            if (palmHitTester != null) {
                palmHitTester.StrokeHit -= PalmEraser_StrokeHit;
                palmHitTester.EndHitTesting();
                palmHitTester = null;
            }

            // 提交擦除历史
            if (ReplacedStroke != null || AddedStroke != null) {
                timeMachine.CommitStrokeEraseHistory(ReplacedStroke, AddedStroke);
                AddedStroke = null;
                ReplacedStroke = null;
            }

            // 恢复编辑模式为书写模式
            if (!forceEraser) {
                inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
            }

            LogHelper.WriteLogToFile("Palm eraser ended", LogHelper.LogType.Trace);
        }

        /// <summary>
        /// 绘制手掌橡皮反馈图形
        /// </summary>
        private void DrawPalmEraserFeedback(Point position) {
            var ct = EraserOverlay_DrawingVisual.DrawingVisual.RenderOpen();
            var mt = palmScaleMatrix;
            var eraserHeight = palmEraserWidth * 56 / 38;
            mt.Translate(position.X - palmEraserWidth / 2, position.Y - eraserHeight / 2);
            ct.PushTransform(new MatrixTransform(mt));
            // 根据形状类型选择绘制圆形或矩形橡皮擦
            ct.DrawDrawing(FindResource(isEraserCircleShape ? "EraserCircleDrawingGroup" : "EraserDrawingGroup") as DrawingGroup);
            ct.Pop();
            ct.Close();
        }

        /// <summary>
        /// 手掌橡皮的笔画命中事件处理
        /// </summary>
        private void PalmEraser_StrokeHit(object sender, StrokeHitEventArgs args) {
            StrokeCollection eraseResult = args.GetPointEraseResults();
            StrokeCollection strokesToReplace = new StrokeCollection { args.HitStroke };

            // 过滤掉锁定的笔画
            var filtered2Replace = strokesToReplace.Where(stroke => !stroke.ContainsPropertyData(IsLockGuid)).ToArray();
            if (filtered2Replace.Length == 0) return;

            var filteredResult = eraseResult.Where(stroke => !stroke.ContainsPropertyData(IsLockGuid)).ToArray();

            if (filteredResult.Length > 0) {
                inkCanvas.Strokes.Replace(new StrokeCollection(filtered2Replace), new StrokeCollection(filteredResult));
            } else {
                inkCanvas.Strokes.Remove(new StrokeCollection(filtered2Replace));
            }
        }

        /// <summary>
        /// 获取触摸边界的有效宽度（用于手掌检测）
        /// </summary>
        public double GetTouchBoundWidth(TouchEventArgs e) {
            var bounds = e.GetTouchPoint(null).Bounds;

            // 对于四点红外屏，使用面积的平方根
            if (Settings.Advanced.IsQuadIR) {
                return Math.Sqrt(bounds.Width * bounds.Height);
            }

            return bounds.Width;
        }

        /// <summary>
        /// 判断触摸是否可能是手掌
        /// </summary>
        /// <param name="e">触摸事件参数</param>
        /// <param name="detectedWidth">检测到的有效宽度</param>
        /// <returns>是否判定为手掌触摸</returns>
        private bool IsPalmTouch(TouchEventArgs e, out double detectedWidth) {
            var bounds = e.GetTouchPoint(null).Bounds;
            detectedWidth = GetTouchBoundWidth(e);

            // 基础条件：触摸宽度必须大于基准值
            if (detectedWidth <= BoundsWidth) {
                largeTouchDetectionCount = 0; // 重置计数
                return false;
            }

            double eraserThresholdValue = Settings.Startup.IsEnableNibMode
                ? Settings.Advanced.NibModeBoundsWidthThresholdValue
                : Settings.Advanced.FingerModeBoundsWidthThresholdValue;

            // 进一步判断：触摸宽度超过阈值
            if (detectedWidth > BoundsWidth * eraserThresholdValue) {
                // 额外判断：检查触摸面积
                // 手掌触摸通常面积较大，而笔尖触摸面积较小
                double area = bounds.Width * bounds.Height;
                double minPalmArea = BoundsWidth * BoundsWidth * eraserThresholdValue;

                if (area >= minPalmArea) {
                    // 增加大触摸点计数
                    largeTouchDetectionCount++;

                    // 如果连续检测次数达到阈值，或者面积非常大（肯定是手掌），则判定为手掌
                    // 面积非常大定义为：阈值的2倍
                    bool isHugeArea = area >= minPalmArea * PalmEraserHugeAreaMultiplier;

                    if (largeTouchDetectionCount >= PalmEraserDetectionThreshold || isHugeArea) {
                        // 计算有效的橡皮擦宽度
                        detectedWidth *= (Settings.Startup.IsEnableNibMode
                            ? Settings.Advanced.NibModeBoundsWidthEraserSize
                            : Settings.Advanced.FingerModeBoundsWidthEraserSize);

                        if (Settings.Advanced.IsSpecialScreen) {
                            detectedWidth *= Settings.Advanced.TouchMultiplier;
                        }

                        return true;
                    }
                }
            } else {
                largeTouchDetectionCount = 0; // 重置计数
            }

            return false;
        }

        /// <summary>
        /// 判断触摸是否可能是墨迹擦（介于笔和手掌之间的触摸）
        /// </summary>
        private bool IsStrokeEraserTouch(TouchEventArgs e) {
            var bounds = e.GetTouchPoint(null).Bounds;
            double width = GetTouchBoundWidth(e);

            // 触摸宽度在基准值和手掌阈值之间
            if (width <= BoundsWidth) {
                return false;
            }

            double eraserThresholdValue = Settings.Startup.IsEnableNibMode
                ? Settings.Advanced.NibModeBoundsWidthThresholdValue
                : Settings.Advanced.FingerModeBoundsWidthThresholdValue;

            // 在基准宽度和阈值之间的触摸认为是墨迹擦
            return width <= BoundsWidth * eraserThresholdValue;
        }

        private HashSet<int> dec = new HashSet<int>();
        private Point centerPoint;
        private InkCanvasEditingMode lastInkCanvasEditingMode = InkCanvasEditingMode.Ink;
        private bool isSingleFingerDragMode = false;

        private void inkCanvas_PreviewTouchDown(object sender, TouchEventArgs e) {
            inkCanvas.CaptureTouch(e.TouchDevice);
            ViewboxFloatingBar.IsHitTestVisible = false;
            BlackboardUIGridForInkReplay.IsHitTestVisible = false;

            dec.Add(e.TouchDevice.Id);
            if (dec.Count == 1) {
                var touchPoint = e.GetTouchPoint(inkCanvas);
                centerPoint = touchPoint.Position;
                lastTouchDownStrokeCollection = inkCanvas.Strokes.Clone();
            }

            if (dec.Count > 1 || isSingleFingerDragMode || !Settings.Gesture.IsEnableTwoFingerGesture) {
                if (isInMultiTouchMode || !Settings.Gesture.IsEnableTwoFingerGesture) return;
                if (inkCanvas.EditingMode == InkCanvasEditingMode.None ||
                    inkCanvas.EditingMode == InkCanvasEditingMode.Select) return;
                lastInkCanvasEditingMode = inkCanvas.EditingMode;
                inkCanvas.EditingMode = InkCanvasEditingMode.None;
            }
        }

        private void inkCanvas_PreviewTouchUp(object sender, TouchEventArgs e) {
            inkCanvas.ReleaseAllTouchCaptures();
            ViewboxFloatingBar.IsHitTestVisible = true;
            BlackboardUIGridForInkReplay.IsHitTestVisible = true;

            if (dec.Count > 1)
                if (inkCanvas.EditingMode == InkCanvasEditingMode.None)
                    inkCanvas.EditingMode = lastInkCanvasEditingMode;
            dec.Remove(e.TouchDevice.Id);
            inkCanvas.Opacity = 1;
            if (dec.Count == 0)
                if (lastTouchDownStrokeCollection.Count() != inkCanvas.Strokes.Count() &&
                    !(drawingShapeMode == 9 && !isFirstTouchCuboid)) {
                    var whiteboardIndex = CurrentWhiteboardIndex;
                    if (currentMode == 0) whiteboardIndex = 0;
                    strokeCollections[whiteboardIndex] = lastTouchDownStrokeCollection;
                }
        }

        private void inkCanvas_ManipulationStarting(object sender, ManipulationStartingEventArgs e) {
            e.Mode = ManipulationModes.All;
        }

        private void inkCanvas_ManipulationInertiaStarting(object sender, ManipulationInertiaStartingEventArgs e) { }

        private void Main_Grid_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e) {
            if (e.Manipulators.Count() != 0) return;
            if (forceEraser) return;
            inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
        }

        private Matrix _cachedManipulationMatrix = Matrix.Identity;

        private void Main_Grid_ManipulationDelta(object sender, ManipulationDeltaEventArgs e) {
            if (isInMultiTouchMode || !Settings.Gesture.IsEnableTwoFingerGesture) return;
            if ((dec.Count >= 2 && (Settings.PowerPointSettings.IsEnableTwoFingerGestureInPresentationMode ||
                                    BorderFloatingBarExitPPTBtn.Visibility != Visibility.Visible)) ||
                isSingleFingerDragMode) {
                var md = e.DeltaManipulation;
                var trans = md.Translation;

                _cachedManipulationMatrix.SetIdentity();

                if (Settings.Gesture.IsEnableTwoFingerTranslate)
                    _cachedManipulationMatrix.Translate(trans.X, trans.Y);

                if (Settings.Gesture.IsEnableTwoFingerGestureTranslateOrRotation) {
                    var rotate = md.Rotation;
                    var scale = md.Scale;

                    var fe = e.Source as FrameworkElement;
                    var center = new Point(fe.ActualWidth / 2, fe.ActualHeight / 2);
                    center = _cachedManipulationMatrix.Transform(center);

                    if (Settings.Gesture.IsEnableTwoFingerRotation)
                        _cachedManipulationMatrix.RotateAt(rotate, center.X, center.Y);
                    if (Settings.Gesture.IsEnableTwoFingerZoom)
                        _cachedManipulationMatrix.ScaleAt(scale.X, scale.Y, center.X, center.Y);
                }

                var strokes = inkCanvas.GetSelectedStrokes();
                var strokeCount = strokes.Count;
                var enableTwoFingerZoom = Settings.Gesture.IsEnableTwoFingerZoom;
                var scaleX = md.Scale.X;
                var scaleY = md.Scale.Y;

                if (strokeCount != 0) {
                    foreach (var stroke in strokes) {
                        stroke.Transform(_cachedManipulationMatrix, false);

                        foreach (var circle in circles)
                            if (stroke == circle.Stroke) {
                                var stylusPoints = circle.Stroke.StylusPoints;
                                var halfCount = stylusPoints.Count / 2;
                                circle.R = GetDistance(stylusPoints[0].ToPoint(),
                                    stylusPoints[halfCount].ToPoint()) / 2;
                                circle.Centroid = new Point(
                                    (stylusPoints[0].X + stylusPoints[halfCount].X) / 2,
                                    (stylusPoints[0].Y + stylusPoints[halfCount].Y) / 2);
                                break;
                            }

                        if (!enableTwoFingerZoom) continue;
                        try {
                            stroke.DrawingAttributes.Width *= scaleX;
                            stroke.DrawingAttributes.Height *= scaleY;
                        }
                        catch (Exception ex) {
                            LogHelper.WriteLogToFile("Error in Main_Grid_ManipulationDelta (Stroke Attributes): " + ex.Message, LogHelper.LogType.Error);
                        }
                    }
                } else {
                    foreach (var stroke in inkCanvas.Strokes) {
                        stroke.Transform(_cachedManipulationMatrix, false);
                        if (enableTwoFingerZoom) {
                            try {
                                stroke.DrawingAttributes.Width *= scaleX;
                                stroke.DrawingAttributes.Height *= scaleY;
                            }
                            catch (Exception ex) {
                                LogHelper.WriteLogToFile("Error in Main_Grid_ManipulationDelta (Batch Stroke Attributes): " + ex.Message, LogHelper.LogType.Error);
                            }
                        }
                    }

                    foreach (var circle in circles) {
                        var stylusPoints = circle.Stroke.StylusPoints;
                        var halfCount = stylusPoints.Count / 2;
                        circle.R = GetDistance(stylusPoints[0].ToPoint(),
                            stylusPoints[halfCount].ToPoint()) / 2;
                        circle.Centroid = new Point(
                            (stylusPoints[0].X + stylusPoints[halfCount].X) / 2,
                            (stylusPoints[0].Y + stylusPoints[halfCount].Y) / 2
                        );
                    }
                }
            }
        }
    }
}
