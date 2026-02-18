using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ink_Canvas.Core;
using Ink_Canvas.Helpers;
using Ink_Canvas.Services;
using Ink_Canvas.Services.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using Point = System.Windows.Point;

namespace Ink_Canvas.ViewModels
{
    /// <summary>
    /// 触摸事件 ViewModel - 管理所有触摸相关的逻辑
    /// 包括手掌橡皮擦、多点触控手势、触摸指针隐藏等功能
    /// </summary>
    public partial class TouchEventsViewModel(ISettingsService settingsService, ITimeMachineService timeMachineService) : ViewModelBase
    {
        private readonly ISettingsService _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        private readonly ITimeMachineService _timeMachineService = timeMachineService ?? throw new ArgumentNullException(nameof(timeMachineService));

        #region Palm Eraser State

        /// <summary>
        /// 是否处于手掌橡皮模式（自动检测触发）
        /// </summary>
        [ObservableProperty]
        private bool _isPalmErasing = false;

        /// <summary>
        /// 手掌橡皮触发前的编辑模式（用于恢复）
        /// </summary>
        private InkCanvasEditingMode _editModeBeforePalmEraser = InkCanvasEditingMode.Ink;

        /// <summary>
        /// 当前手掌触摸设备的ID
        /// </summary>
        private int _palmTouchDeviceId = -1;

        /// <summary>
        /// 手掌触摸的初始位置
        /// </summary>
        private Point _palmTouchStartPoint;

        /// <summary>
        /// 手掌橡皮的增量命中测试器
        /// </summary>
        private IncrementalStrokeHitTester _palmHitTester = null;

        /// <summary>
        /// 手掌橡皮的缩放矩阵
        /// </summary>
        private Matrix _palmScaleMatrix;

        /// <summary>
        /// 手掌橡皮上次更新位置
        /// </summary>
        private Point? _lastPalmEraserPoint = null;

        /// <summary>
        /// 手掌橡皮上次更新时间戳
        /// </summary>
        private int _lastPalmEraserUpdateTick = 0;

        /// <summary>
        /// 手掌橡皮的宽度
        /// </summary>
        private double _palmEraserWidth = 64;

        /// <summary>
        /// 连续检测到的大触摸点数量（用于稳定性判断）
        /// </summary>
        private int _largeTouchDetectionCount = 0;

        /// <summary>
        /// 手掌橡皮触发连续检测次数（可配置）
        /// </summary>
        private int PalmEraserDetectionThreshold =>
            Math.Max(1, _settingsService?.Settings?.Gesture?.PalmEraserDetectionThreshold ?? 3);

        /// <summary>
        /// 手掌橡皮移动时最小更新间隔（毫秒，可配置）
        /// </summary>
        private int PalmEraserMinIntervalMs =>
            Math.Max(0, _settingsService?.Settings?.Gesture?.PalmEraserMinIntervalMs ?? 12);

        #endregion

        #region Multi-Touch State

        /// <summary>
        /// 是否处于多点触控模式
        /// </summary>
        [ObservableProperty]
        private bool _isInMultiTouchMode = false;

        /// <summary>
        /// 触摸设备ID集合
        /// </summary>
        private HashSet<int> _touchDeviceIds = [];

        /// <summary>
        /// 操作变换缓存矩阵
        /// </summary>
        private Matrix _cachedManipulationMatrix = Matrix.Identity;

        /// <summary>
        /// 上次操作更新时间戳
        /// </summary>
        private int _lastManipulationUpdateTick = 0;

        /// <summary>
        /// 操作更新间隔（毫秒）
        /// </summary>
        private const int MANIPULATION_UPDATE_INTERVAL_MS = 8;

        /// <summary>
        /// 上次平移值
        /// </summary>
        private Point _lastTranslation;

        /// <summary>
        /// 上次旋转角度
        /// </summary>
        private double _lastRotation = 0;

        /// <summary>
        /// 上次缩放值
        /// </summary>
        private Vector _lastScale;

        #endregion

        #region Touch Pointer Hide

        /// <summary>
        /// 光标是否隐藏
        /// </summary>
        [ObservableProperty]
        private bool _isCursorHidden = false;

        #endregion

        #region Properties

        /// <summary>
        /// 基准触摸宽度
        /// </summary>
        public double BoundsWidth { get; set; }

        /// <summary>
        /// 当前InkCanvas实例
        /// </summary>
        public InkCanvas InkCanvas { get; set; }

        /// <summary>
        /// 橡皮擦覆盖层
        /// </summary>
        public Grid EraserOverlay { get; set; }

        /// <summary>
        /// 橡皮擦绘图视觉
        /// </summary>
        public DrawingVisual EraserDrawingVisual { get; set; }

        /// <summary>
        /// 浮动工具栏
        /// </summary>
        public FrameworkElement FloatingBar { get; set; }

        /// <summary>
        /// 黑板UI覆盖层
        /// </summary>
        public FrameworkElement BlackboardUI { get; set; }

        #endregion

        #region Events

        /// <summary>
        /// 请求更改编辑模式事件
        /// </summary>
        public event EventHandler<Ink_Canvas.Services.Events.EditingModeChangeRequestedEventArgs> EditingModeChangeRequested;

        /// <summary>
        /// 请求显示橡皮擦反馈事件
        /// </summary>
        public event EventHandler<Ink_Canvas.Services.Events.EraserFeedbackEventArgs> EraserFeedbackRequested;

        #endregion

        #region Palm Eraser Methods

        /// <summary>
        /// 启动手掌橡皮模式
        /// </summary>
        /// <param name="touchDeviceId">触摸设备ID</param>
        /// <param name="width">检测到的宽度</param>
        /// <param name="initialPoint">初始触摸点</param>
        public void StartPalmEraser(int touchDeviceId, double width, Point initialPoint)
        {
            if (IsPalmErasing) return; // 已经在手掌橡皮模式中

            IsPalmErasing = true;
            _palmTouchDeviceId = touchDeviceId;
            _palmEraserWidth = Math.Max(width, 40); // 最小宽度40

            // 保存当前编辑模式
            _editModeBeforePalmEraser = InkCanvas?.EditingMode ?? InkCanvasEditingMode.Ink;

            // 设置为无编辑模式，我们自己处理擦除
            EditingModeChangeRequested?.Invoke(this, new EditingModeChangeRequestedEventArgs(InkCanvasEditingMode.None));

            // 显示橡皮擦覆盖层
            EraserOverlay.Visibility = Visibility.Visible;

            // 初始化增量命中测试器
            var eraserHeight = _palmEraserWidth * 56 / 38; // 保持板擦的宽高比
            _palmHitTester = InkCanvas?.Strokes.GetIncrementalStrokeHitTester(
                new RectangleStylusShape(_palmEraserWidth, eraserHeight));
            if (_palmHitTester != null)
            {
                _palmHitTester.StrokeHit += PalmEraser_StrokeHit;
            }

            // 设置缩放矩阵用于绘制橡皮擦
            var scaleX = _palmEraserWidth / 38;
            var scaleY = eraserHeight / 56;
            _palmScaleMatrix = new Matrix();
            _palmScaleMatrix.ScaleAt(scaleX, scaleY, 0, 0);

            // 绘制初始橡皮擦形状
            DrawPalmEraserFeedback(initialPoint);
            _palmHitTester?.AddPoint(initialPoint);

            _lastPalmEraserPoint = initialPoint;
            _lastPalmEraserUpdateTick = Environment.TickCount;
            _palmTouchStartPoint = initialPoint;

            LogHelper.WriteLogToFile($"Palm eraser started, width: {_palmEraserWidth}", LogHelper.LogType.Trace);
        }

        /// <summary>
        /// 处理手掌橡皮的触摸移动
        /// </summary>
        /// <param name="touchDeviceId">触摸设备ID</param>
        /// <param name="currentPoint">当前触摸点</param>
        public void HandlePalmEraserMove(int touchDeviceId, Point currentPoint)
        {
            if (!IsPalmErasing) return;
            if (touchDeviceId != _palmTouchDeviceId) return;

            if (_lastPalmEraserPoint.HasValue)
            {
                var delta = currentPoint - _lastPalmEraserPoint.Value;
                var tick = Environment.TickCount;
                var minMove = 2.5; // 默认最小移动距离
                if (delta.LengthSquared < minMove * minMove ||
                    tick - _lastPalmEraserUpdateTick < PalmEraserMinIntervalMs)
                {
                    return;
                }

                _lastPalmEraserUpdateTick = tick;
            }

            _lastPalmEraserPoint = currentPoint;

            // 绘制橡皮擦反馈
            DrawPalmEraserFeedback(currentPoint);

            // 添加点到命中测试器进行擦除
            _palmHitTester?.AddPoint(currentPoint);
        }

        /// <summary>
        /// 结束手掌橡皮模式
        /// </summary>
        /// <param name="touchDeviceId">触摸设备ID</param>
        public void EndPalmEraser(int touchDeviceId)
        {
            if (!IsPalmErasing) return;
            if (touchDeviceId != _palmTouchDeviceId) return;

            IsPalmErasing = false;
            _palmTouchDeviceId = -1;

            // 重置手掌橡皮相关标志
            _largeTouchDetectionCount = 0;
            _lastPalmEraserPoint = null;
            _lastPalmEraserUpdateTick = 0;

            // 隐藏橡皮擦覆盖层
            EraserOverlay.Visibility = Visibility.Collapsed;

            // 清除橡皮擦反馈图形
            if (EraserDrawingVisual != null)
            {
                var ct = EraserDrawingVisual.RenderOpen();
                ct.DrawRectangle(new SolidColorBrush(Colors.Transparent), null, 
                    new Rect(0, 0, EraserOverlay.ActualWidth, EraserOverlay.ActualHeight));
                ct.Close();
            }

            // 结束命中测试
            if (_palmHitTester != null)
            {
                _palmHitTester.StrokeHit -= PalmEraser_StrokeHit;
                _palmHitTester.EndHitTesting();
                _palmHitTester = null;
            }

            // 恢复编辑模式为书写模式
            EditingModeChangeRequested?.Invoke(this, 
                new EditingModeChangeRequestedEventArgs(InkCanvasEditingMode.Ink));

            LogHelper.WriteLogToFile("掌擦结束", LogHelper.LogType.Trace);
        }

        /// <summary>
        /// 绘制手掌橡皮反馈图形
        /// </summary>
        /// <param name="position">位置</param>
        private void DrawPalmEraserFeedback(Point position)
        {
            if (EraserDrawingVisual == null) return;

            var ct = EraserDrawingVisual.RenderOpen();
            var mt = _palmScaleMatrix;
            var eraserHeight = _palmEraserWidth * 56 / 38;
            mt.Translate(position.X - _palmEraserWidth / 2, position.Y - eraserHeight / 2);
            ct.PushTransform(new MatrixTransform(mt));
            
            // 触发事件让View层绘制具体的橡皮擦形状
            EraserFeedbackRequested?.Invoke(this, new EraserFeedbackEventArgs
            {
                Position = position,
                Width = _palmEraserWidth,
                Height = eraserHeight,
                IsCircleShape = _settingsService?.Settings?.Canvas?.EraserShapeType == 0
            });
            
            ct.Pop();
            ct.Close();
        }

        /// <summary>
        /// 手掌橡皮的笔画命中事件处理
        /// </summary>
        private void PalmEraser_StrokeHit(object sender, StrokeHitEventArgs args)
        {
            if (InkCanvas == null) return;

            StrokeCollection eraseResult = args.GetPointEraseResults();
            StrokeCollection strokesToReplace = [args.HitStroke];

            // 过滤掉锁定的笔画
            var filtered2Replace = strokesToReplace.Where(stroke => !stroke.ContainsPropertyData(Guid.Parse("{D6FCCF9F-6132-4E70-9222-054F05D0BF0E}"))).ToArray();
            if (filtered2Replace.Length == 0) return;

            var filteredResult = eraseResult.Where(stroke => !stroke.ContainsPropertyData(Guid.Parse("{D6FCCF9F-6132-4E70-9222-054F05D0BF0E}"))).ToArray();

            if (filteredResult.Length > 0)
            {
                InkCanvas.Strokes.Replace(new StrokeCollection(filtered2Replace), new StrokeCollection(filteredResult));
            }
            else
            {
                InkCanvas.Strokes.Remove(new StrokeCollection(filtered2Replace));
            }
        }

        /// <summary>
        /// 获取触摸边界的有效宽度（用于手掌检测）
        /// </summary>
        /// <param name="bounds">触摸边界</param>
        /// <returns>有效宽度</returns>
        public double GetTouchBoundWidth(Rect bounds)
        {
            // 对于四点红外屏，使用面积的平方根
            if (_settingsService?.Settings?.Advanced?.IsQuadIR == true)
            {
                return Math.Sqrt(bounds.Width * bounds.Height);
            }

            return bounds.Width;
        }

        /// <summary>
        /// 判断触摸是否可能是手掌
        /// </summary>
        /// <param name="bounds">触摸边界</param>
        /// <param name="detectedWidth">检测到的有效宽度</param>
        /// <returns>是否判定为手掌触摸</returns>
        public bool IsPalmTouch(Rect bounds, out double detectedWidth)
        {
            detectedWidth = GetTouchBoundWidth(bounds);

            // 基础条件：触摸宽度必须大于基准值
            if (detectedWidth <= BoundsWidth)
            {
                _largeTouchDetectionCount = 0; // 重置计数
                return false;
            }

            double eraserThresholdValue = _settingsService?.Settings?.Startup?.IsEnableNibMode == true
                ? _settingsService.Settings.Advanced.NibModeBoundsWidthThresholdValue
                : _settingsService.Settings.Advanced.FingerModeBoundsWidthThresholdValue;

            // 进一步判断：触摸宽度超过阈值
            if (detectedWidth > BoundsWidth * eraserThresholdValue)
            {
                // 额外判断：检查触摸面积
                // 手掌触摸通常面积较大，而笔尖触摸面积较小
                double area = bounds.Width * bounds.Height;
                double minPalmArea = (double)BoundsWidth * BoundsWidth * eraserThresholdValue;

                if (area >= minPalmArea)
                {
                    // 增加大触摸点计数
                    _largeTouchDetectionCount++;

                    // 如果连续检测次数达到阈值，或者面积非常大（肯定是手掌），则判定为手掌
                    // 面积非常大定义为：阈值的2.5倍
                    bool isHugeArea = area >= minPalmArea * 2.5;

                    if (_largeTouchDetectionCount >= PalmEraserDetectionThreshold || isHugeArea)
                    {
                        // 计算有效的橡皮擦宽度
                        detectedWidth *= (_settingsService.Settings.Startup.IsEnableNibMode
                            ? _settingsService.Settings.Advanced.NibModeBoundsWidthEraserSize
                            : _settingsService.Settings.Advanced.FingerModeBoundsWidthEraserSize);

                        if (_settingsService.Settings.Advanced.IsSpecialScreen)
                        {
                            detectedWidth *= _settingsService.Settings.Advanced.TouchMultiplier;
                        }

                        return true;
                    }
                }
            }
            else
            {
                _largeTouchDetectionCount = 0; // 重置计数
            }

            return false;
        }

        /// <summary>
        /// 判断触摸是否可能是墨迹擦（介于笔和手掌之间的触摸）
        /// </summary>
        /// <param name="bounds">触摸边界</param>
        /// <returns>是否为墨迹擦</returns>
        public bool IsStrokeEraserTouch(Rect bounds)
        {
            double width = GetTouchBoundWidth(bounds);

            // 触摸宽度在基准值和手掌阈值之间
            if (width <= BoundsWidth)
            {
                return false;
            }

            double eraserThresholdValue = _settingsService?.Settings?.Startup?.IsEnableNibMode == true
                ? _settingsService.Settings.Advanced.NibModeBoundsWidthThresholdValue
                : _settingsService.Settings.Advanced.FingerModeBoundsWidthThresholdValue;

            // 在基准宽度和阈值之间的触摸认为是墨迹擦
            return width <= BoundsWidth * eraserThresholdValue;
        }

        #endregion

        #region Multi-Touch Methods

        /// <summary>
        /// 切换多点触控模式
        /// </summary>
        [RelayCommand]
        public void ToggleMultiTouchMode()
        {
            IsInMultiTouchMode = !IsInMultiTouchMode;
            
            // 触发事件通知View层更新
            MultiTouchModeToggled?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 处理多点触控操作
        /// </summary>
        /// <param name="delta">操作增量</param>
        /// <param name="center">操作中心</param>
        public void HandleManipulationDelta(ManipulationDelta delta, Point center)
        {
            if (IsInMultiTouchMode || !_settingsService?.Settings?.Gesture?.IsEnableTwoFingerGesture == true) return;
            if (_touchDeviceIds.Count >= 2 && (_settingsService.Settings.PowerPointSettings.IsEnableTwoFingerGestureInPresentationMode ||
                                        FloatingBar?.Visibility == Visibility.Visible))
            {
                // 节流：限制更新频率
                var currentTick = Environment.TickCount;
                if (currentTick - _lastManipulationUpdateTick < MANIPULATION_UPDATE_INTERVAL_MS)
                {
                    return;
                }
                _lastManipulationUpdateTick = currentTick;

                var trans = delta.Translation;

                // 优化：只有当变换值发生显著变化时才重新计算矩阵
                bool needsMatrixUpdate = Math.Abs(trans.X - _lastTranslation.X) > 0.1 ||
                                       Math.Abs(trans.Y - _lastTranslation.Y) > 0.1 ||
                                       Math.Abs(delta.Rotation - _lastRotation) > 0.1 ||
                                       Math.Abs(delta.Scale.X - _lastScale.X) > 0.001 ||
                                       Math.Abs(delta.Scale.Y - _lastScale.Y) > 0.001;

                if (!needsMatrixUpdate) return;

                // 更新上次值
                _lastTranslation = new Point(trans.X, trans.Y);
                _lastRotation = delta.Rotation;
                _lastScale = delta.Scale;

                // 触发事件让View层处理具体的变换
                ManipulationDeltaRequested?.Invoke(this, new Ink_Canvas.Services.Events.TouchManipulationDeltaEventArgs
                {
                    Delta = delta,
                    Center = center,
                    CachedMatrix = _cachedManipulationMatrix
                });
            }
        }

        /// <summary>
        /// 完成多点触控操作
        /// </summary>
        public void CompleteManipulation()
        {
            // 重置缓存状态，确保下次手势操作从干净状态开始
            _lastManipulationUpdateTick = 0;
            _lastTranslation = new Point(0, 0);
            _lastRotation = 0;
            _lastScale = new Vector(1, 1);
            _cachedManipulationMatrix.SetIdentity();

            // 恢复编辑模式
            EditingModeChangeRequested?.Invoke(this, 
                new EditingModeChangeRequestedEventArgs(InkCanvasEditingMode.Ink));
        }

        #endregion

        #region Touch Pointer Methods

        /// <summary>
        /// 处理鼠标移动事件以显示/隐藏光标
        /// </summary>
        /// <param name="stylusDevice">触笔设备</param>
        public void HandleMouseMove(StylusDevice stylusDevice)
        {
            if (stylusDevice == null)
            {
                if (IsCursorHidden)
                {
                    System.Windows.Forms.Cursor.Show();
                    IsCursorHidden = false;
                }
            }
            else if (stylusDevice.TabletDevice.Type == TabletDeviceType.Stylus)
            {
                if (IsCursorHidden)
                {
                    System.Windows.Forms.Cursor.Show();
                    IsCursorHidden = false;
                }
            }
        }

        /// <summary>
        /// 隐藏光标
        /// </summary>
        public void HideCursor()
        {
            if (!IsCursorHidden && _settingsService?.Settings?.Gesture?.HideCursorWhenUsingTouchDevice == true)
            {
                System.Windows.Forms.Cursor.Hide();
                IsCursorHidden = true;
            }
        }

        /// <summary>
        /// 显示光标
        /// </summary>
        public void ShowCursor()
        {
            if (IsCursorHidden)
            {
                System.Windows.Forms.Cursor.Show();
                IsCursorHidden = false;
            }
        }

        #endregion

        #region Touch Device Management

        /// <summary>
        /// 添加触摸设备
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        public void AddTouchDevice(int deviceId)
        {
            _touchDeviceIds.Add(deviceId);
        }

        /// <summary>
        /// 移除触摸设备
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        public void RemoveTouchDevice(int deviceId)
        {
            _touchDeviceIds.Remove(deviceId);
        }

        /// <summary>
        /// 获取触摸设备数量
        /// </summary>
        public int GetTouchDeviceCount()
        {
            return _touchDeviceIds.Count;
        }

        #endregion

        #region Events

        /// <summary>
        /// 多点触控模式切换事件
        /// </summary>
        public event EventHandler MultiTouchModeToggled;

        /// <summary>
        /// 操作增量请求事件
        /// </summary>
        public event EventHandler<Ink_Canvas.Services.Events.TouchManipulationDeltaEventArgs> ManipulationDeltaRequested;

        #endregion

        #region Cleanup

        /// <summary>
        /// 清理资源
        /// </summary>
        public override void Cleanup()
        {
            // 结束手掌橡皮模式
            if (IsPalmErasing)
            {
                EndPalmEraser(_palmTouchDeviceId);
            }

            // 清理事件订阅
            if (_palmHitTester != null)
            {
                _palmHitTester.StrokeHit -= PalmEraser_StrokeHit;
            }

            // 清理集合
            _touchDeviceIds.Clear();

            base.Cleanup();
        }

        #endregion
    }
}
