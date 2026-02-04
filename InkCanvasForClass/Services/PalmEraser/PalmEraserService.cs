using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using Ink_Canvas.Helpers;
using Ink_Canvas.Models.Settings;
using InkCanvas = System.Windows.Controls.InkCanvas;

namespace Ink_Canvas.Services.PalmEraser
{
    /// <summary>
    /// 手掌橡皮擦服务 - 现代化的手掌检测和擦除服务
    /// </summary>
    public class PalmEraserService
    {
        #region Dependencies

        private readonly PalmEraserDetector _detector;
        private readonly PalmEraserStateMachine _stateMachine;
        private GestureSettings _settings;

        #endregion

        #region State

        private bool _isInitialized = false;
        private IncrementalStrokeHitTester _hitTester;
        private Matrix _scaleMatrix = Matrix.Identity;
        private double _eraserWidth = 64;
        private bool _isCircleShape = false;

        // 触摸跟踪
        private readonly Dictionary<int, TouchTrackingInfo> _touchTracking = new();

        // 预测
        private Point? _predictedNextPosition;

        #endregion

        #region Configuration

        /// <summary>
        /// 基准边界宽度
        /// </summary>
        public double BaseBoundsWidth { get; set; } = 30;

        /// <summary>
        /// 是否为四点红外屏
        /// </summary>
        public bool IsQuadIr { get; set; } = false;

        /// <summary>
        /// 是否为特殊屏幕
        /// </summary>
        public bool IsSpecialScreen { get; set; } = false;

        /// <summary>
        /// 特殊屏幕触摸倍率
        /// </summary>
        public double TouchMultiplier { get; set; } = 1.0;

        /// <summary>
        /// 笔尖模式边界宽度阈值
        /// </summary>
        public double NibModeBoundsWidthThreshold { get; set; } = 2.5;

        /// <summary>
        /// 手指模式边界宽度阈值
        /// </summary>
        public double FingerModeBoundsWidthThreshold { get; set; } = 2.0;

        /// <summary>
        /// 笔尖模式橡皮擦大小倍率
        /// </summary>
        public double NibModeEraserSizeMultiplier { get; set; } = 1.0;

        /// <summary>
        /// 手指模式橡皮擦大小倍率
        /// </summary>
        public double FingerModeEraserSizeMultiplier { get; set; } = 1.0;

        /// <summary>
        /// 是否启用笔尖模式
        /// </summary>
        public bool IsNibMode { get; set; } = false;

        #endregion

        #region Events

        /// <summary>
        /// 手掌橡皮擦激活事件
        /// </summary>
        public event EventHandler<PalmEraserActivatedEventArgs> Activated;

        /// <summary>
        /// 手掌橡皮擦移动事件
        /// </summary>
        public event EventHandler<PalmEraserMoveEventArgs> Moved;

        /// <summary>
        /// 手掌橡皮擦结束事件
        /// </summary>
        public event EventHandler<PalmEraserEndedEventArgs> Ended;

        /// <summary>
        /// 笔画被擦除事件
        /// </summary>
        public event EventHandler<StrokeErasedEventArgs> StrokeErased;

        /// <summary>
        /// 需要重绘橡皮擦视觉反馈事件
        /// </summary>
        public event EventHandler<EraserVisualFeedbackEventArgs> VisualFeedbackNeeded;

        #endregion

        #region Properties

        /// <summary>
        /// 当前状态
        /// </summary>
        public PalmEraserState CurrentState => _stateMachine.CurrentState;

        /// <summary>
        /// 是否正在擦除
        /// </summary>
        public bool IsErasing => _stateMachine.CurrentState == PalmEraserState.Erasing;

        /// <summary>
        /// 当前橡皮擦宽度
        /// </summary>
        public double CurrentEraserWidth => _eraserWidth;

        /// <summary>
        /// 当前处理的设备ID
        /// </summary>
        public int CurrentDeviceId => _stateMachine.CurrentDeviceId;

        #endregion

        #region Constructor

        public PalmEraserService(GestureSettings settings = null)
        {
            _settings = settings;

            // 初始化检测器
            var detectorConfig = new PalmEraserDetectorConfig
            {
                IsSpecialScreen = IsSpecialScreen,
                SpecialScreenMultiplier = TouchMultiplier,
                NibModeSizeMultiplier = NibModeEraserSizeMultiplier,
                FingerModeSizeMultiplier = FingerModeEraserSizeMultiplier
            };
            _detector = new PalmEraserDetector(detectorConfig);

            // 初始化状态机
            var stateMachineConfig = new PalmEraserStateMachineConfig
            {
                ConfirmationThreshold = settings?.PalmEraserDetectionThreshold / 6.0 + 0.5 ?? 0.75,
                RequiredSampleCount = Math.Max(1, settings?.PalmEraserDetectionThreshold ?? 3),
                ReleaseDelayMs = settings?.PalmEraserMinIntervalMs ?? 50
            };
            _stateMachine = new PalmEraserStateMachine(stateMachineConfig);

            // 订阅状态变更
            _stateMachine.StateChanged += OnStateChanged;
        }

        #endregion

        #region Initialization

        /// <summary>
        /// 初始化服务
        /// </summary>
        public void Initialize(GestureSettings settings)
        {
            _settings = settings;
            _isInitialized = true;

            // 更新配置
            UpdateConfiguration();

            LogHelper.WriteLogToFile("PalmEraserService initialized", LogHelper.LogType.Info);
        }

        /// <summary>
        /// 更新配置
        /// </summary>
        private void UpdateConfiguration()
        {
            if (_settings == null) return;

            // 更新状态机配置
            var stateMachineConfig = new PalmEraserStateMachineConfig
            {
                ConfirmationThreshold = _settings.PalmEraserDetectionThreshold / 6.0 + 0.5,
                RequiredSampleCount = Math.Max(1, _settings.PalmEraserDetectionThreshold),
                ReleaseDelayMs = _settings.PalmEraserMinIntervalMs
            };

            // 更新检测器配置
            var detectorConfig = new PalmEraserDetectorConfig
            {
                IsSpecialScreen = IsSpecialScreen,
                SpecialScreenMultiplier = TouchMultiplier,
                NibModeSizeMultiplier = NibModeEraserSizeMultiplier,
                FingerModeSizeMultiplier = FingerModeEraserSizeMultiplier
            };
        }

        #endregion

        #region Touch Event Handling

        /// <summary>
        /// 处理触摸按下事件
        /// </summary>
        public bool ProcessTouchDown(TouchEventArgs e, InkCanvas inkCanvas)
        {
            if (!_isInitialized || _settings?.DisableGestureEraser != false)
                return false;

            // 检查是否应该处理此触摸
            if (!ShouldProcessTouch())
                return false;

            // 获取或创建跟踪信息
            if (!_touchTracking.TryGetValue(e.TouchDevice.Id, out var trackingInfo))
            {
                trackingInfo = new TouchTrackingInfo();
                _touchTracking[e.TouchDevice.Id] = trackingInfo;
            }

            // 创建触摸特征
            var touch = TouchCharacteristics.FromTouchEventArgs(e);
            trackingInfo.LastTouch = touch;
            trackingInfo.LastPosition = touch.Position;
            trackingInfo.LastTimestamp = touch.Timestamp;

            // 检测手掌
            double thresholdValue = IsNibMode ? NibModeBoundsWidthThreshold : FingerModeBoundsWidthThreshold;
            var (probability, effectiveWidth) = _detector.DetectPalm(touch, BaseBoundsWidth * thresholdValue, IsQuadIr, IsNibMode);

            // 更新状态机
            bool stateChanged = _stateMachine.ProcessTouch(touch, probability);

            // 如果进入激活或擦除状态，初始化擦除
            if (_stateMachine.CurrentState == PalmEraserState.Activated ||
                _stateMachine.CurrentState == PalmEraserState.Erasing)
            {
                InitializeEraser(effectiveWidth, inkCanvas);
            }

            return IsErasing;
        }

        /// <summary>
        /// 处理触摸移动事件
        /// </summary>
        public bool ProcessTouchMove(TouchEventArgs e, InkCanvas inkCanvas)
        {
            if (!_isInitialized)
                return false;

            // 如果不是当前跟踪的设备，忽略
            if (e.TouchDevice.Id != _stateMachine.CurrentDeviceId)
                return false;

            // 获取跟踪信息
            if (!_touchTracking.TryGetValue(e.TouchDevice.Id, out var trackingInfo))
                return false;

            // 创建触摸特征（包含移动信息）
            var touch = TouchCharacteristics.FromTouchEventArgs(e, trackingInfo.LastPosition, trackingInfo.LastTimestamp);
            trackingInfo.LastTouch = touch;

            // 更新跟踪信息
            trackingInfo.LastPosition = touch.Position;
            trackingInfo.LastTimestamp = touch.Timestamp;

            // 检测手掌
            double thresholdValue = IsNibMode ? NibModeBoundsWidthThreshold : FingerModeBoundsWidthThreshold;
            var (probability, _) = _detector.DetectPalm(touch, BaseBoundsWidth * thresholdValue, IsQuadIr, IsNibMode);

            // 更新状态机
            _stateMachine.ProcessTouch(touch, probability);

            // 如果在擦除状态，执行擦除
            if (_stateMachine.CurrentState == PalmEraserState.Erasing)
            {
                PerformErase(touch.Position, inkCanvas);

                // 预测下一个位置
                if (_settings?.PalmEraserDetectOnMove == true)
                {
                    _predictedNextPosition = PredictNextPosition(touch);
                }
            }

            return IsErasing;
        }

        /// <summary>
        /// 处理触摸抬起事件
        /// </summary>
        public bool ProcessTouchUp(TouchEventArgs e, InkCanvas inkCanvas)
        {
            if (!_isInitialized)
                return false;

            // 如果不是当前跟踪的设备，忽略
            if (e.TouchDevice.Id != _stateMachine.CurrentDeviceId)
                return false;

            // 创建触摸特征
            var touch = TouchCharacteristics.FromTouchEventArgs(e);

            // 通知状态机触摸结束（概率设为0）
            _stateMachine.ProcessTouch(touch, 0);

            // 清理跟踪信息
            _touchTracking.Remove(e.TouchDevice.Id);

            return IsErasing;
        }

        #endregion

        #region Eraser Operations

        /// <summary>
        /// 初始化橡皮擦
        /// </summary>
        private void InitializeEraser(double width, InkCanvas inkCanvas)
        {
            _eraserWidth = Math.Max(width, 40);
            _isCircleShape = false; // 可以配置

            // 初始化命中测试器
            double eraserHeight = _eraserWidth * 56 / 38;
            _hitTester = inkCanvas.Strokes.GetIncrementalStrokeHitTester(
                new RectangleStylusShape(_eraserWidth, eraserHeight));
            _hitTester.StrokeHit += OnStrokeHit;

            // 设置缩放矩阵
            double scaleX = _eraserWidth / 38;
            double scaleY = eraserHeight / 56;
            _scaleMatrix = new Matrix();
            _scaleMatrix.ScaleAt(scaleX, scaleY, 0, 0);

            LogHelper.WriteLogToFile($"Palm eraser initialized, width: {_eraserWidth}", LogHelper.LogType.Trace);
        }

        /// <summary>
        /// 执行擦除
        /// </summary>
        private void PerformErase(Point position, InkCanvas inkCanvas)
        {
            if (_hitTester == null)
                return;

            // 触发视觉反馈事件
            VisualFeedbackNeeded?.Invoke(this, new EraserVisualFeedbackEventArgs
            {
                Position = position,
                Width = _eraserWidth,
                Height = _eraserWidth * 56 / 38,
                IsCircleShape = _isCircleShape,
                ScaleMatrix = _scaleMatrix
            });

            // 添加点到命中测试器
            _hitTester.AddPoint(position);

            // 触发移动事件
            Moved?.Invoke(this, new PalmEraserMoveEventArgs
            {
                Position = position,
                Width = _eraserWidth
            });
        }

        /// <summary>
        /// 结束擦除
        /// </summary>
        private void EndEraser(InkCanvas inkCanvas)
        {
            if (_hitTester != null)
            {
                _hitTester.StrokeHit -= OnStrokeHit;
                _hitTester.EndHitTesting();
                _hitTester = null;
            }

            _predictedNextPosition = null;

            LogHelper.WriteLogToFile("Palm eraser ended", LogHelper.LogType.Trace);
        }

        /// <summary>
        /// 笔画命中处理
        /// </summary>
        private void OnStrokeHit(object sender, StrokeHitEventArgs args)
        {
            var eraseResult = args.GetPointEraseResults();
            var strokesToReplace = new StrokeCollection { args.HitStroke };

            // 过滤锁定的笔画
            var filteredToReplace = strokesToReplace
                .Where(s => !s.ContainsPropertyData(IsLockGuid))
                .ToArray();

            if (filteredToReplace.Length == 0)
                return;

            var filteredResult = eraseResult
                .Where(s => !s.ContainsPropertyData(IsLockGuid))
                .ToArray();

            // 触发擦除事件
            StrokeErased?.Invoke(this, new StrokeErasedEventArgs
            {
                StrokesToReplace = new StrokeCollection(filteredToReplace),
                StrokesToAdd = filteredResult.Length > 0 ? new StrokeCollection(filteredResult) : null
            });
        }

        #endregion

        #region State Machine Handler

        /// <summary>
        /// 状态变更处理
        /// </summary>
        private void OnStateChanged(object sender, PalmEraserStateChangedEventArgs e)
        {
            switch (e.NewState)
            {
                case PalmEraserState.Activated:
                    Activated?.Invoke(this, new PalmEraserActivatedEventArgs
                    {
                        DeviceId = _stateMachine.CurrentDeviceId,
                        Confidence = e.Confidence,
                        Width = _eraserWidth
                    });
                    break;

                case PalmEraserState.Erasing:
                    // 擦除已开始
                    break;

                case PalmEraserState.Releasing:
                case PalmEraserState.Idle when e.OldState != PalmEraserState.Idle:
                    Ended?.Invoke(this, new PalmEraserEndedEventArgs
                    {
                        DeviceId = e.TriggerTouch?.DeviceId ?? -1
                    });
                    break;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 是否应该处理触摸
        /// </summary>
        private bool ShouldProcessTouch()
        {
            if (_settings == null)
                return true;

            // 检查是否禁用了手势橡皮擦
            if (_settings.DisableGestureEraser)
                return false;

            // 检查特殊屏幕条件
            if (TouchMultiplier == 0 && IsSpecialScreen)
                return false;

            return true;
        }

        /// <summary>
        /// 预测下一个位置
        /// </summary>
        private Point PredictNextPosition(TouchCharacteristics touch)
        {
            if (touch.Velocity <= 0 || touch.Direction.Length == 0)
                return touch.Position;

            // 预测 16ms 后的位置（约一帧）
            double predictionTime = 16;
            double distance = touch.Velocity * predictionTime;

            return new Point(
                touch.Position.X + touch.Direction.X * distance,
                touch.Position.Y + touch.Direction.Y * distance
            );
        }

        /// <summary>
        /// 强制结束当前会话
        /// </summary>
        public void ForceEndSession(InkCanvas inkCanvas)
        {
            _stateMachine.ForceEndSession();
            EndEraser(inkCanvas);
            _touchTracking.Clear();
        }

        #endregion

        #region Lock Guid

        private static readonly Guid IsLockGuid = new("{12345678-1234-1234-1234-123456789012}");

        #endregion
    }

    #region Event Args Classes

    public class PalmEraserActivatedEventArgs : EventArgs
    {
        public int DeviceId { get; set; }
        public double Confidence { get; set; }
        public double Width { get; set; }
    }

    public class PalmEraserMoveEventArgs : EventArgs
    {
        public Point Position { get; set; }
        public double Width { get; set; }
    }

    public class PalmEraserEndedEventArgs : EventArgs
    {
        public int DeviceId { get; set; }
    }

    public class StrokeErasedEventArgs : EventArgs
    {
        public StrokeCollection StrokesToReplace { get; set; }
        public StrokeCollection StrokesToAdd { get; set; }
    }

    public class EraserVisualFeedbackEventArgs : EventArgs
    {
        public Point Position { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public bool IsCircleShape { get; set; }
        public Matrix ScaleMatrix { get; set; }
    }

    #endregion

    #region Helper Classes

    /// <summary>
    /// 触摸跟踪信息
    /// </summary>
    internal class TouchTrackingInfo
    {
        public TouchCharacteristics LastTouch { get; set; }
        public Point LastPosition { get; set; }
        public DateTime LastTimestamp { get; set; }
    }

    #endregion
}
