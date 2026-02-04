using System;
using System.Collections.Generic;
using System.Linq;

namespace Ink_Canvas.Services.PalmEraser
{
    /// <summary>
    /// 手掌橡皮擦状态
    /// </summary>
    public enum PalmEraserState
    {
        /// <summary>
        /// 空闲状态
        /// </summary>
        Idle,

        /// <summary>
        /// 检测中（收集触摸数据）
        /// </summary>
        Detecting,

        /// <summary>
        /// 已激活，准备擦除
        /// </summary>
        Activated,

        /// <summary>
        /// 正在擦除
        /// </summary>
        Erasing,

        /// <summary>
        /// 释放中
        /// </summary>
        Releasing
    }

    /// <summary>
    /// 状态变更事件参数
    /// </summary>
    public class PalmEraserStateChangedEventArgs : EventArgs
    {
        public PalmEraserState OldState { get; }
        public PalmEraserState NewState { get; }
        public TouchCharacteristics TriggerTouch { get; }
        public double Confidence { get; }

        public PalmEraserStateChangedEventArgs(PalmEraserState oldState, PalmEraserState newState, TouchCharacteristics triggerTouch, double confidence)
        {
            OldState = oldState;
            NewState = newState;
            TriggerTouch = triggerTouch;
            Confidence = confidence;
        }
    }

    /// <summary>
    /// 手掌橡皮擦状态机 - 管理手掌检测和擦除状态
    /// </summary>
    public class PalmEraserStateMachine
    {
        private PalmEraserState _currentState = PalmEraserState.Idle;
        private readonly Queue<TouchCharacteristics> _touchHistory = new();
        private DateTime _stateEntryTime;
        private int _currentDeviceId = -1;

        // 配置参数
        private readonly PalmEraserStateMachineConfig _config;

        /// <summary>
        /// 当前状态
        /// </summary>
        public PalmEraserState CurrentState => _currentState;

        /// <summary>
        /// 当前处理的设备ID
        /// </summary>
        public int CurrentDeviceId => _currentDeviceId;

        /// <summary>
        /// 状态变更事件
        /// </summary>
        public event EventHandler<PalmEraserStateChangedEventArgs> StateChanged;

        /// <summary>
        /// 触摸历史记录（用于分析）
        /// </summary>
        public IReadOnlyCollection<TouchCharacteristics> TouchHistory => _touchHistory.ToList().AsReadOnly();

        public PalmEraserStateMachine(PalmEraserStateMachineConfig config = null)
        {
            _config = config ?? new PalmEraserStateMachineConfig();
            _stateEntryTime = DateTime.UtcNow;
        }

        /// <summary>
        /// 处理新的触摸特征
        /// </summary>
        /// <param name="touch">触摸特征</param>
        /// <param name="palmProbability">手掌概率（0-1）</param>
        /// <returns>是否状态发生变化</returns>
        public bool ProcessTouch(TouchCharacteristics touch, double palmProbability)
        {
            // 添加触摸到历史记录
            AddToHistory(touch);

            var oldState = _currentState;

            switch (_currentState)
            {
                case PalmEraserState.Idle:
                    HandleIdleState(touch, palmProbability);
                    break;

                case PalmEraserState.Detecting:
                    HandleDetectingState(touch, palmProbability);
                    break;

                case PalmEraserState.Activated:
                    HandleActivatedState(touch, palmProbability);
                    break;

                case PalmEraserState.Erasing:
                    HandleErasingState(touch, palmProbability);
                    break;

                case PalmEraserState.Releasing:
                    HandleReleasingState(touch, palmProbability);
                    break;
            }

            bool stateChanged = _currentState != oldState;
            if (stateChanged)
            {
                StateChanged?.Invoke(this, new PalmEraserStateChangedEventArgs(oldState, _currentState, touch, palmProbability));
            }

            return stateChanged;
        }

        /// <summary>
        /// 强制结束当前擦除会话
        /// </summary>
        public void ForceEndSession()
        {
            if (_currentState != PalmEraserState.Idle)
            {
                var oldState = _currentState;
                TransitionTo(PalmEraserState.Idle);
                StateChanged?.Invoke(this, new PalmEraserStateChangedEventArgs(oldState, PalmEraserState.Idle, null, 0));
            }
        }

        /// <summary>
        /// 处理空闲状态
        /// </summary>
        private void HandleIdleState(TouchCharacteristics touch, double palmProbability)
        {
            // 如果概率超过初始阈值，进入检测状态
            if (palmProbability >= _config.InitialDetectionThreshold)
            {
                _currentDeviceId = touch.DeviceId;
                TransitionTo(PalmEraserState.Detecting);
            }
        }

        /// <summary>
        /// 处理检测状态
        /// </summary>
        private void HandleDetectingState(TouchCharacteristics touch, double palmProbability)
        {
            // 检查是否是同一设备
            if (touch.DeviceId != _currentDeviceId)
            {
                // 设备变更，重置状态
                ResetState();
                return;
            }

            // 检查超时
            if (IsDetectionTimeout())
            {
                ResetState();
                return;
            }

            // 收集足够的样本后判断
            if (_touchHistory.Count >= _config.RequiredSampleCount)
            {
                var averageProbability = CalculateAverageProbability();

                if (averageProbability >= _config.ConfirmationThreshold)
                {
                    // 确认是手掌，进入激活状态
                    TransitionTo(PalmEraserState.Activated);
                }
                else if (averageProbability < _config.RejectionThreshold)
                {
                    // 确认不是手掌，返回空闲
                    ResetState();
                }
            }
        }

        /// <summary>
        /// 处理激活状态
        /// </summary>
        private void HandleActivatedState(TouchCharacteristics touch, double palmProbability)
        {
            // 检查是否是同一设备
            if (touch.DeviceId != _currentDeviceId)
            {
                ResetState();
                return;
            }

            // 进入擦除状态
            TransitionTo(PalmEraserState.Erasing);
        }

        /// <summary>
        /// 处理擦除状态
        /// </summary>
        private void HandleErasingState(TouchCharacteristics touch, double palmProbability)
        {
            // 检查是否是同一设备
            if (touch.DeviceId != _currentDeviceId)
            {
                // 进入释放状态
                TransitionTo(PalmEraserState.Releasing);
                return;
            }

            // 如果概率持续很低，可能手掌已抬起
            if (palmProbability < _config.ReleaseThreshold && IsReleaseStable())
            {
                TransitionTo(PalmEraserState.Releasing);
            }
        }

        /// <summary>
        /// 处理释放状态
        /// </summary>
        private void HandleReleasingState(TouchCharacteristics touch, double palmProbability)
        {
            // 短暂停留后返回空闲
            if ((DateTime.UtcNow - _stateEntryTime).TotalMilliseconds >= _config.ReleaseDelayMs)
            {
                ResetState();
            }
        }

        /// <summary>
        /// 状态转换
        /// </summary>
        private void TransitionTo(PalmEraserState newState)
        {
            _currentState = newState;
            _stateEntryTime = DateTime.UtcNow;

            // 清理历史记录（如果需要）
            if (newState == PalmEraserState.Idle)
            {
                _touchHistory.Clear();
                _currentDeviceId = -1;
            }
        }

        /// <summary>
        /// 重置状态
        /// </summary>
        private void ResetState()
        {
            TransitionTo(PalmEraserState.Idle);
        }

        /// <summary>
        /// 添加触摸到历史记录
        /// </summary>
        private void AddToHistory(TouchCharacteristics touch)
        {
            _touchHistory.Enqueue(touch.Clone());

            // 保持历史记录在限制范围内
            while (_touchHistory.Count > _config.MaxHistorySize)
            {
                _touchHistory.Dequeue();
            }
        }

        /// <summary>
        /// 计算平均概率
        /// </summary>
        private double CalculateAverageProbability()
        {
            if (_touchHistory.Count == 0) return 0;

            // 使用加权平均，最近的样本权重更高
            double weightedSum = 0;
            double weightSum = 0;
            int index = 0;

            foreach (var touch in _touchHistory)
            {
                double weight = Math.Pow(_config.HistoryWeightDecay, _touchHistory.Count - 1 - index);
                weightedSum += touch.Circularity * weight; // 使用圆度作为概率代理
                weightSum += weight;
                index++;
            }

            return weightSum > 0 ? weightedSum / weightSum : 0;
        }

        /// <summary>
        /// 检查检测是否超时
        /// </summary>
        private bool IsDetectionTimeout()
        {
            return (DateTime.UtcNow - _stateEntryTime).TotalMilliseconds > _config.DetectionTimeoutMs;
        }

        /// <summary>
        /// 检查释放是否稳定（连续多个低概率样本）
        /// </summary>
        private bool IsReleaseStable()
        {
            var recentTouches = _touchHistory.TakeLast(_config.ReleaseStabilityCount);
            return recentTouches.Count() >= _config.ReleaseStabilityCount;
        }
    }

    /// <summary>
    /// 状态机配置
    /// </summary>
    public class PalmEraserStateMachineConfig
    {
        /// <summary>
        /// 初始检测阈值（进入检测状态）
        /// </summary>
        public double InitialDetectionThreshold { get; set; } = 0.3;

        /// <summary>
        /// 确认阈值（确认是手掌）
        /// </summary>
        public double ConfirmationThreshold { get; set; } = 0.75;

        /// <summary>
        /// 拒绝阈值（确认不是手掌）
        /// </summary>
        public double RejectionThreshold { get; set; } = 0.3;

        /// <summary>
        /// 释放阈值（低于此值认为手掌已抬起）
        /// </summary>
        public double ReleaseThreshold { get; set; } = 0.2;

        /// <summary>
        /// 需要的样本数量
        /// </summary>
        public int RequiredSampleCount { get; set; } = 3;

        /// <summary>
        /// 最大历史记录大小
        /// </summary>
        public int MaxHistorySize { get; set; } = 20;

        /// <summary>
        /// 历史记录权重衰减系数
        /// </summary>
        public double HistoryWeightDecay { get; set; } = 0.9;

        /// <summary>
        /// 检测超时时间（毫秒）
        /// </summary>
        public int DetectionTimeoutMs { get; set; } = 200;

        /// <summary>
        /// 释放稳定计数
        /// </summary>
        public int ReleaseStabilityCount { get; set; } = 2;

        /// <summary>
        /// 释放延迟（毫秒）
        /// </summary>
        public int ReleaseDelayMs { get; set; } = 50;
    }
}
