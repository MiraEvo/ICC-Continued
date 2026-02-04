using Ink_Canvas.Helpers;
using Ink_Canvas.Services.PalmEraser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using Point = System.Windows.Point;

namespace Ink_Canvas {
    public partial class MainWindow {
        #region Palm Eraser State

        /// <summary>
        /// 现代化的手掌橡皮擦服务
        /// </summary>
        private PalmEraserService _palmEraserService;

        /// <summary>
        /// 是否使用现代化的手掌橡皮擦
        /// </summary>
        private bool UseModernPalmEraser => Settings?.Gesture?.UseModernPalmEraser ?? true;

        // 以下字段仅用于兼容模式（旧逻辑）

        /// <summary>
        /// 是否处于手掌橡皮模式（自动检测触发）- 兼容模式
        /// </summary>
        private bool isPalmErasing = false;

        /// <summary>
        /// 手掌橡皮触发前的编辑模式（用于恢复）- 兼容模式
        /// </summary>
        private InkCanvasEditingMode editModeBeforePalmEraser = InkCanvasEditingMode.Ink;

        /// <summary>
        /// 当前手掌触摸设备的ID - 兼容模式
        /// </summary>
        private int palmTouchDeviceId = -1;

        /// <summary>
        /// 手掌触摸的初始位置 - 兼容模式
        /// </summary>
        private Point palmTouchStartPoint = new();

        /// <summary>
        /// 手掌橡皮的增量命中测试器 - 兼容模式
        /// </summary>
        private IncrementalStrokeHitTester palmHitTester = null;

        /// <summary>
        /// 手掌橡皮的缩放矩阵 - 兼容模式
        /// </summary>
        private Matrix palmScaleMatrix = new();

        /// <summary>
        /// 手掌橡皮上次更新位置 - 兼容模式
        /// </summary>
        private Point? lastPalmEraserPoint = null;

        /// <summary>
        /// 手掌橡皮上次更新时间戳 - 兼容模式
        /// </summary>
        private int lastPalmEraserUpdateTick = 0;

        /// <summary>
        /// 手掌橡皮的宽度 - 兼容模式
        /// </summary>
        private double palmEraserWidth = 64;

        /// <summary>
        /// 连续检测到的大触摸点数量（用于稳定性判断）- 兼容模式
        /// </summary>
        private int largeTouchDetectionCount = 0;

        /// <summary>
        /// 手掌橡皮触发连续检测次数（可配置）- 兼容模式
        /// </summary>
        private int PalmEraserDetectionThreshold =>
            Math.Max(1, Settings?.Gesture?.PalmEraserDetectionThreshold ?? 3);

        /// <summary>
        /// 手掌橡皮巨大触摸面积倍率（可配置）- 兼容模式
        /// </summary>
        private double PalmEraserHugeAreaMultiplier =>
            Math.Max(1.0, Settings?.Gesture?.PalmEraserHugeAreaMultiplier ?? 2.5);

        /// <summary>
        /// 手掌橡皮移动时最小更新距离（像素，可配置）- 兼容模式
        /// </summary>
        private double PalmEraserMinMove =>
            Math.Max(0.5, Settings?.Gesture?.PalmEraserMinMove ?? 2.5);

        /// <summary>
        /// 手掌橡皮移动时最小更新间隔（毫秒，可配置）- 兼容模式
        /// </summary>
        private int PalmEraserMinIntervalMs =>
            Math.Max(0, Settings?.Gesture?.PalmEraserMinIntervalMs ?? 12);

        #endregion

        #region Modern Palm Eraser Service

        /// <summary>
        /// 初始化现代化的手掌橡皮擦服务
        /// </summary>
        private void InitializePalmEraserService()
        {
            if (_palmEraserService != null)
            {
                // 取消订阅旧事件
                _palmEraserService.Activated -= OnPalmEraserActivated;
                _palmEraserService.Moved -= OnPalmEraserMoved;
                _palmEraserService.Ended -= OnPalmEraserEnded;
                _palmEraserService.StrokeErased -= OnPalmEraserStrokeErased;
                _palmEraserService.VisualFeedbackNeeded -= OnPalmEraserVisualFeedbackNeeded;
            }

            _palmEraserService = new PalmEraserService(Settings?.Gesture);

            // 配置服务
            _palmEraserService.BaseBoundsWidth = BoundsWidth;
            _palmEraserService.IsQuadIr = Settings?.Advanced?.IsQuadIR ?? false;
            _palmEraserService.IsSpecialScreen = Settings?.Advanced?.IsSpecialScreen ?? false;
            _palmEraserService.TouchMultiplier = Settings?.Advanced?.TouchMultiplier ?? 1.0;
            _palmEraserService.IsNibMode = Settings?.Startup?.IsEnableNibMode ?? false;
            _palmEraserService.NibModeBoundsWidthThreshold = Settings?.Advanced?.NibModeBoundsWidthThresholdValue ?? 2.5;
            _palmEraserService.FingerModeBoundsWidthThreshold = Settings?.Advanced?.FingerModeBoundsWidthThresholdValue ?? 2.0;
            _palmEraserService.NibModeEraserSizeMultiplier = Settings?.Advanced?.NibModeBoundsWidthEraserSize ?? 1.0;
            _palmEraserService.FingerModeEraserSizeMultiplier = Settings?.Advanced?.FingerModeBoundsWidthEraserSize ?? 1.0;

            // 订阅事件
            _palmEraserService.Activated += OnPalmEraserActivated;
            _palmEraserService.Moved += OnPalmEraserMoved;
            _palmEraserService.Ended += OnPalmEraserEnded;
            _palmEraserService.StrokeErased += OnPalmEraserStrokeErased;
            _palmEraserService.VisualFeedbackNeeded += OnPalmEraserVisualFeedbackNeeded;

            // 初始化服务
            _palmEraserService.Initialize(Settings?.Gesture);

            LogHelper.WriteLogToFile("Modern PalmEraserService initialized", LogHelper.LogType.Info);
        }

        /// <summary>
        /// 手掌橡皮擦激活事件处理
        /// </summary>
        private void OnPalmEraserActivated(object sender, PalmEraserActivatedEventArgs e)
        {
            isLastTouchEraser = true;
            if (drawingShapeMode == 0 && forceEraser) return;

            // 显示橡皮擦覆盖层
            GridEraserOverlay.Visibility = Visibility.Visible;

            // 设置编辑模式为无
            inkCanvas.EditingMode = InkCanvasEditingMode.None;

            LogHelper.WriteLogToFile($"Modern palm eraser activated, width: {e.Width}, confidence: {e.Confidence:F2}", LogHelper.LogType.Trace);
        }

        /// <summary>
        /// 手掌橡皮擦移动事件处理
        /// </summary>
        private void OnPalmEraserMoved(object sender, PalmEraserMoveEventArgs e)
        {
            // 移动处理在 VisualFeedbackNeeded 中完成
        }

        /// <summary>
        /// 手掌橡皮擦结束事件处理
        /// </summary>
        private void OnPalmEraserEnded(object sender, PalmEraserEndedEventArgs e)
        {
            // 隐藏橡皮擦覆盖层
            GridEraserOverlay.Visibility = Visibility.Collapsed;

            // 清除视觉反馈
            var ct = EraserOverlay_DrawingVisual.DrawingVisual.RenderOpen();
            ct.DrawRectangle(new SolidColorBrush(Colors.Transparent), null, new Rect(0, 0, ActualWidth, ActualHeight));
            ct.Close();

            // 提交擦除历史
            if (ReplacedStroke != null || AddedStroke != null)
            {
                timeMachine.CommitStrokeEraseHistory(ReplacedStroke, AddedStroke);
                AddedStroke = null;
                ReplacedStroke = null;
            }

            // 恢复编辑模式
            if (!forceEraser)
            {
                inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
            }

            isLastTouchEraser = false;

            LogHelper.WriteLogToFile("Modern palm eraser ended", LogHelper.LogType.Trace);
        }

        /// <summary>
        /// 笔画被擦除事件处理
        /// </summary>
        private void OnPalmEraserStrokeErased(object sender, StrokeErasedEventArgs e)
        {
            if (e.StrokesToAdd != null && e.StrokesToAdd.Count > 0)
            {
                inkCanvas.Strokes.Replace(e.StrokesToReplace, e.StrokesToAdd);
                ReplacedStroke = e.StrokesToReplace;
                AddedStroke = e.StrokesToAdd;
            }
            else
            {
                inkCanvas.Strokes.Remove(e.StrokesToReplace);
                ReplacedStroke = e.StrokesToReplace;
                AddedStroke = null;
            }
        }

        /// <summary>
        /// 视觉反馈事件处理
        /// </summary>
        private void OnPalmEraserVisualFeedbackNeeded(object sender, EraserVisualFeedbackEventArgs e)
        {
            var ct = EraserOverlay_DrawingVisual.DrawingVisual.RenderOpen();
            var mt = e.ScaleMatrix;
            mt.Translate(e.Position.X - e.Width / 2, e.Position.Y - e.Height / 2);
            ct.PushTransform(new MatrixTransform(mt));
            ct.DrawDrawing(FindResource(e.IsCircleShape ? "EraserCircleDrawingGroup" : "EraserDrawingGroup") as DrawingGroup);
            ct.Pop();
            ct.Close();
        }

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

        private void MainWindow_TouchDown(object? sender, TouchEventArgs e) {
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
                        boundWidth *= Settings.Startup.IsEnableNibMode
                            ? Settings.Advanced.NibModeBoundsWidthEraserSize
                            : Settings.Advanced.FingerModeBoundsWidthEraserSize;
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
                        var point = stylusPoints[^1];
                        strokeVisual.Add(new StylusPoint(point.X, point.Y, point.PressureFactor));
                        strokeVisual.Redraw();
                    }
                }
                catch (Exception ex) {
                    LogHelper.WriteLogToFile("MainWindow_StylusDown 初始化笔迹失败：" + ex.Message,
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
                    StrokeVisualLastRedrawTick.Remove(e.StylusDevice.Id);
                    if (StrokeVisualList.Count == 0 || VisualCanvasList.Count == 0 || TouchDownPointsList.Count == 0) {
                        inkCanvas.Children.Clear();
                        StrokeVisualList.Clear();
                        VisualCanvasList.Clear();
                        TouchDownPointsList.Clear();
                        StrokeVisualLastRedrawTick.Clear();
                    }
                }
                catch (Exception ex) {
                    LogHelper.WriteLogToFile("MainWindow_StylusUp 清理失败：" + ex.Message, LogHelper.LogType.Error);
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
                        LogHelper.WriteLogToFile("MainWindow_StylusMove 按钮状态检查失败：" + ex.Message, LogHelper.LogType.Error);
                    }

                    var strokeVisual = GetStrokeVisual(e.StylusDevice.Id);
                    var stylusPointCollection = e.GetStylusPoints(inkCanvas);
                    foreach (var stylusPoint in stylusPointCollection)
                        strokeVisual.Add(new StylusPoint(stylusPoint.X, stylusPoint.Y, stylusPoint.PressureFactor));

                    if (stylusPointCollection.Count > 0) {
                        var tick = Environment.TickCount;
                        if (!StrokeVisualLastRedrawTick.TryGetValue(e.StylusDevice.Id, out var lastTick)
                            || tick - lastTick >= StrokeVisualRedrawIntervalMs) {
                            strokeVisual.Redraw();
                            StrokeVisualLastRedrawTick[e.StylusDevice.Id] = tick;
                        }
                    }
                }
                catch (Exception ex) {
                    LogHelper.WriteLogToFile("MainWindow_StylusMove 处理失败：" + ex.Message, LogHelper.LogType.Error);
                }
            }
        }

        private StrokeVisual GetStrokeVisual(int id) {
            if (StrokeVisualList.TryGetValue(id, out var visual)) return visual;

            var strokeVisual = new StrokeVisual(inkCanvas.DefaultDrawingAttributes.Clone());
            StrokeVisualList[id] = strokeVisual;
            StrokeVisualLastRedrawTick[id] = 0;
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

        private Dictionary<int, InkCanvasEditingMode> TouchDownPointsList { get; } = [];

        private Dictionary<int, StrokeVisual> StrokeVisualList { get; } = [];
        private Dictionary<int, VisualCanvas> VisualCanvasList { get; } = [];
        private Dictionary<int, int> StrokeVisualLastRedrawTick { get; } = [];

        private const int StrokeVisualRedrawIntervalMs = 8;

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




        private Point iniP = new(0, 0);
        private bool isLastTouchEraser = false;
        private bool forcePointEraser = true;

        private void Main_Grid_TouchDown(object? sender, TouchEventArgs e) {
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
                    // 使用现代化的手掌橡皮擦服务
                    if (UseModernPalmEraser && _palmEraserService != null) {
                        bool isPalm = _palmEraserService.ProcessTouchDown(e, inkCanvas);
                        if (isPalm) {
                            // 手掌橡皮已激活，由服务处理后续
                            return;
                        }
                    }
                    // 回退到兼容模式
                    else if (IsPalmTouch(e, out double palmWidth)) {
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

            LogHelper.WriteLogToFile("掌擦结束", LogHelper.LogType.Trace);
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
            StrokeCollection strokesToReplace = new() { args.HitStroke };

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
                double minPalmArea = (double)BoundsWidth * BoundsWidth * eraserThresholdValue;

                if (area >= minPalmArea) {
                    // 增加大触摸点计数
                    largeTouchDetectionCount++;

                    // 如果连续检测次数达到阈值，或者面积非常大（肯定是手掌），则判定为手掌
                    // 面积非常大定义为：阈值的2倍
                    bool isHugeArea = area >= minPalmArea * PalmEraserHugeAreaMultiplier;

                    if (largeTouchDetectionCount >= PalmEraserDetectionThreshold || isHugeArea) {
                        // 计算有效的橡皮擦宽度
                        detectedWidth *= Settings.Startup.IsEnableNibMode
                            ? Settings.Advanced.NibModeBoundsWidthEraserSize
                            : Settings.Advanced.FingerModeBoundsWidthEraserSize;

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

        private HashSet<int> dec = [];
        private Point centerPoint;
        private InkCanvasEditingMode lastInkCanvasEditingMode = InkCanvasEditingMode.Ink;
        private bool isSingleFingerDragMode = false;

        private void inkCanvas_PreviewTouchDown(object? sender, TouchEventArgs e) {
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
            // 使用现代化的手掌橡皮擦服务
            if (UseModernPalmEraser && _palmEraserService != null) {
                _palmEraserService.ProcessTouchUp(e, inkCanvas);
            }

            inkCanvas.ReleaseAllTouchCaptures();
            ViewboxFloatingBar.IsHitTestVisible = true;
            BlackboardUIGridForInkReplay.IsHitTestVisible = true;

            if (dec.Count > 1)
                if (inkCanvas.EditingMode == InkCanvasEditingMode.None)
                    inkCanvas.EditingMode = lastInkCanvasEditingMode;
            dec.Remove(e.TouchDevice.Id);
            inkCanvas.Opacity = 1;
            if (dec.Count == 0)
                if (lastTouchDownStrokeCollection.Count != inkCanvas.Strokes.Count &&
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
            if (e.Manipulators.Any()) return;
            if (forceEraser) return;
            inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
            
            // 重置缓存状态，确保下次手势操作从干净状态开始
            _lastManipulationUpdateTick = 0;
            _lastTranslation = new Point(0, 0);
            _lastRotation = 0;
            _lastScale = new Vector(1, 1);
            _cachedManipulationMatrix.SetIdentity();
        }

        private Matrix _cachedManipulationMatrix = Matrix.Identity;
        private int _lastManipulationUpdateTick = 0;
        private const int MANIPULATION_UPDATE_INTERVAL_MS = 8; // 限制更新频率为125Hz
        private Point _lastTranslation = new(0, 0);
        private double _lastRotation = 0;
        private Vector _lastScale = new(1, 1);

        private void Main_Grid_ManipulationDelta(object sender, ManipulationDeltaEventArgs e) {
            if (isInMultiTouchMode || !Settings.Gesture.IsEnableTwoFingerGesture) return;
            if ((dec.Count >= 2 && (Settings.PowerPointSettings.IsEnableTwoFingerGestureInPresentationMode ||
                                    BorderFloatingBarExitPPTBtn.Visibility != Visibility.Visible)) ||
                isSingleFingerDragMode) {
                
                // 节流：限制更新频率
                var currentTick = Environment.TickCount;
                if (currentTick - _lastManipulationUpdateTick < MANIPULATION_UPDATE_INTERVAL_MS) {
                    return;
                }
                _lastManipulationUpdateTick = currentTick;

                var md = e.DeltaManipulation;
                var trans = md.Translation;

                // 优化：只有当变换值发生显著变化时才重新计算矩阵
                bool needsMatrixUpdate = Math.Abs(trans.X - _lastTranslation.X) > 0.1 ||
                                       Math.Abs(trans.Y - _lastTranslation.Y) > 0.1 ||
                                       Math.Abs(md.Rotation - _lastRotation) > 0.1 ||
                                       Math.Abs(md.Scale.X - _lastScale.X) > 0.001 ||
                                       Math.Abs(md.Scale.Y - _lastScale.Y) > 0.001;

                if (!needsMatrixUpdate) return;

                // 更新上次值
                _lastTranslation = new Point(trans.X, trans.Y);
                _lastRotation = md.Rotation;
                _lastScale = md.Scale;

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
                    // 批量处理选中的笔画
                    var strokesToTransform = new List<Stroke>();
                    var circlesToUpdate = new List<Circle>();
                    
                    foreach (var stroke in strokes) {
                        strokesToTransform.Add(stroke);

                        foreach (var circle in circles) {
                            if (stroke == circle.Stroke) {
                                circlesToUpdate.Add(circle);
                                break;
                            }
                        }
                    }

                    // 批量应用变换
                    foreach (var stroke in strokesToTransform) {
                        stroke.Transform(_cachedManipulationMatrix, false);
                    }

                    // 批量更新圆形属性
                    foreach (var circle in circlesToUpdate) {
                        var stylusPoints = circle.Stroke.StylusPoints;
                        var halfCount = stylusPoints.Count / 2;
                        circle.R = GetDistance(stylusPoints[0].ToPoint(),
                            stylusPoints[halfCount].ToPoint()) / 2;
                        circle.Centroid = new Point(
                            (stylusPoints[0].X + stylusPoints[halfCount].X) / 2,
                            (stylusPoints[0].Y + stylusPoints[halfCount].Y) / 2);
                    }

                    // 批量更新缩放属性
                    if (enableTwoFingerZoom) {
                        foreach (var stroke in strokesToTransform) {
                            try {
                                stroke.DrawingAttributes.Width *= scaleX;
                                stroke.DrawingAttributes.Height *= scaleY;
                            }
                            catch (Exception ex) {
                                LogHelper.WriteLogToFile("Main_Grid_ManipulationDelta 修改笔迹属性失败：" + ex.Message, LogHelper.LogType.Error);
                            }
                        }
                    }
                } else {
                    // 批量处理所有笔画
                    var allStrokes = inkCanvas.Strokes.ToList();
                    var allCircles = circles.ToList();

                    // 批量应用变换
                    foreach (var stroke in allStrokes) {
                        stroke.Transform(_cachedManipulationMatrix, false);
                    }

                    // 批量更新圆形
                    foreach (var circle in allCircles) {
                        var stylusPoints = circle.Stroke.StylusPoints;
                        var halfCount = stylusPoints.Count / 2;
                        circle.R = GetDistance(stylusPoints[0].ToPoint(),
                            stylusPoints[halfCount].ToPoint()) / 2;
                        circle.Centroid = new Point(
                            (stylusPoints[0].X + stylusPoints[halfCount].X) / 2,
                            (stylusPoints[0].Y + stylusPoints[halfCount].Y) / 2);
                    }

                    // 批量更新缩放属性
                    if (enableTwoFingerZoom) {
                        foreach (var stroke in allStrokes) {
                            try {
                                stroke.DrawingAttributes.Width *= scaleX;
                                stroke.DrawingAttributes.Height *= scaleY;
                            }
                            catch (Exception ex) {
                                LogHelper.WriteLogToFile("Main_Grid_ManipulationDelta 批量修改笔迹属性失败：" + ex.Message, LogHelper.LogType.Error);
                            }
                        }
                    }
                }
            }
        }
    }
}
