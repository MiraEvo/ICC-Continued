using Newtonsoft.Json;

namespace Ink_Canvas.Models.Settings
{
    /// <summary>
    /// 手势设置类
    /// </summary>
    public class GestureSettings : SettingsBase
    {
        private bool _isEnableMultiTouchMode = true;
        private bool _isEnableTwoFingerZoom = true;
        private bool _isEnableTwoFingerTranslate = true;
        private bool _autoSwitchTwoFingerGesture = true;
        private bool _isEnableTwoFingerRotation = false;
        private bool _isEnableTwoFingerRotationOnSelection = false;
        private bool _disableGestureEraser = true;
        private int _defaultMultiPointHandWritingMode = 2;
        private bool _hideCursorWhenUsingTouchDevice = true;
        private int _hideCursorMode = 0;
        private bool _enableMouseGesture = true;
        private bool _enableMouseRightBtnGesture = true;
        private bool _enableMouseWheelGesture = true;
        private int _mouseWheelAction = 0;
        private int _mouseWheelDirection = 0;
        private int _palmEraserDetectionThreshold = 3;
        private int _palmEraserMinIntervalMs = 12;
        private bool _palmEraserDetectOnMove = true;
        private bool _useAdaptiveThreshold = true;
        private bool _usePredictiveErasing = true;
        private double _palmProbabilityThreshold = 0.75;
        private int _touchHistorySize = 10;
        private bool _enableHapticFeedback = true;
        private double _palmEraserVelocityThreshold = 0.5;

        /// <summary>
        /// 是否启用双指手势（缩放、平移或旋转）
        /// </summary>
        [JsonIgnore]
        public bool IsEnableTwoFingerGesture => IsEnableTwoFingerZoom || IsEnableTwoFingerTranslate || IsEnableTwoFingerRotation;

        /// <summary>
        /// 是否启用双指手势（平移或旋转）
        /// </summary>
        [JsonIgnore]
        public bool IsEnableTwoFingerGestureTranslateOrRotation => IsEnableTwoFingerTranslate || IsEnableTwoFingerRotation;

        /// <summary>
        /// 是否启用多点触控模式
        /// </summary>
        [JsonProperty("isEnableMultiTouchMode")]
        public bool IsEnableMultiTouchMode
        {
            get => _isEnableMultiTouchMode;
            set => SetProperty(ref _isEnableMultiTouchMode, value);
        }

        /// <summary>
        /// 是否启用双指缩放
        /// </summary>
        [JsonProperty("isEnableTwoFingerZoom")]
        public bool IsEnableTwoFingerZoom
        {
            get => _isEnableTwoFingerZoom;
            set => SetProperty(ref _isEnableTwoFingerZoom, value);
        }

        /// <summary>
        /// 是否启用双指平移
        /// </summary>
        [JsonProperty("isEnableTwoFingerTranslate")]
        public bool IsEnableTwoFingerTranslate
        {
            get => _isEnableTwoFingerTranslate;
            set => SetProperty(ref _isEnableTwoFingerTranslate, value);
        }

        /// <summary>
        /// 是否自动切换双指手势
        /// </summary>
        [JsonProperty("AutoSwitchTwoFingerGesture")]
        public bool AutoSwitchTwoFingerGesture
        {
            get => _autoSwitchTwoFingerGesture;
            set => SetProperty(ref _autoSwitchTwoFingerGesture, value);
        }

        /// <summary>
        /// 是否启用双指旋转
        /// </summary>
        [JsonProperty("isEnableTwoFingerRotation")]
        public bool IsEnableTwoFingerRotation
        {
            get => _isEnableTwoFingerRotation;
            set => SetProperty(ref _isEnableTwoFingerRotation, value);
        }

        /// <summary>
        /// 是否在选择时启用双指旋转
        /// </summary>
        [JsonProperty("isEnableTwoFingerRotationOnSelection")]
        public bool IsEnableTwoFingerRotationOnSelection
        {
            get => _isEnableTwoFingerRotationOnSelection;
            set => SetProperty(ref _isEnableTwoFingerRotationOnSelection, value);
        }

        /// <summary>
        /// 是否禁用手势橡皮擦
        /// </summary>
        [JsonProperty("disableGestureEraser")]
        public bool DisableGestureEraser
        {
            get => _disableGestureEraser;
            set => SetProperty(ref _disableGestureEraser, value);
        }

        /// <summary>
        /// 默认多点手写模式
        /// </summary>
        [JsonProperty("defaultMultiPointHandWritingMode")]
        public int DefaultMultiPointHandWritingMode
        {
            get => _defaultMultiPointHandWritingMode;
            set => SetProperty(ref _defaultMultiPointHandWritingMode, value);
        }

        /// <summary>
        /// 使用触摸设备时是否隐藏光标
        /// </summary>
        [JsonProperty("hideCursorWhenUsingTouchDevice")]
        public bool HideCursorWhenUsingTouchDevice
        {
            get => _hideCursorWhenUsingTouchDevice;
            set => SetProperty(ref _hideCursorWhenUsingTouchDevice, value);
        }

        /// <summary>
        /// 隐藏光标模式（0=每次启动都开启，1=每次启动都关闭）
        /// </summary>
        [JsonProperty("hideCursorMode")]
        public int HideCursorMode
        {
            get => _hideCursorMode;
            set => SetProperty(ref _hideCursorMode, value);
        }

        /// <summary>
        /// 是否启用鼠标手势
        /// </summary>
        [JsonProperty("enableMouseGesture")]
        public bool EnableMouseGesture
        {
            get => _enableMouseGesture;
            set => SetProperty(ref _enableMouseGesture, value);
        }

        /// <summary>
        /// 是否启用鼠标右键手势
        /// </summary>
        [JsonProperty("enableMouseRightBtnGesture")]
        public bool EnableMouseRightBtnGesture
        {
            get => _enableMouseRightBtnGesture;
            set => SetProperty(ref _enableMouseRightBtnGesture, value);
        }

        /// <summary>
        /// 是否启用鼠标滚轮手势
        /// </summary>
        [JsonProperty("enableMouseWheelGesture")]
        public bool EnableMouseWheelGesture
        {
            get => _enableMouseWheelGesture;
            set => SetProperty(ref _enableMouseWheelGesture, value);
        }

        /// <summary>
        /// 鼠标滚轮操作类型（0=缩放画布，1=调整笔粗细）
        /// </summary>
        [JsonProperty("mouseWheelAction")]
        public int MouseWheelAction
        {
            get => _mouseWheelAction;
            set => SetProperty(ref _mouseWheelAction, value);
        }

        /// <summary>
        /// 鼠标滚轮正方向（0=滚轮向上，1=滚轮向下）
        /// </summary>
        [JsonProperty("mouseWheelDirection")]
        public int MouseWheelDirection
        {
            get => _mouseWheelDirection;
            set => SetProperty(ref _mouseWheelDirection, value);
        }

        /// <summary>
        /// 手掌橡皮触发连续检测次数
        /// </summary>
        [JsonProperty("palmEraserDetectionThreshold")]
        public int PalmEraserDetectionThreshold
        {
            get => _palmEraserDetectionThreshold;
            set => SetProperty(ref _palmEraserDetectionThreshold, value);
        }

        /// <summary>
        /// 手掌橡皮最小更新间隔（毫秒）
        /// </summary>
        [JsonProperty("palmEraserMinIntervalMs")]
        public int PalmEraserMinIntervalMs
        {
            get => _palmEraserMinIntervalMs;
            set => SetProperty(ref _palmEraserMinIntervalMs, value);
        }

        /// <summary>
        /// 是否在触摸移动时检测手掌橡皮
        /// </summary>
        [JsonProperty("palmEraserDetectOnMove")]
        public bool PalmEraserDetectOnMove
        {
            get => _palmEraserDetectOnMove;
            set => SetProperty(ref _palmEraserDetectOnMove, value);
        }

        /// <summary>
        /// 是否使用自适应阈值
        /// </summary>
        [JsonProperty("useAdaptiveThreshold")]
        public bool UseAdaptiveThreshold
        {
            get => _useAdaptiveThreshold;
            set => SetProperty(ref _useAdaptiveThreshold, value);
        }

        /// <summary>
        /// 是否使用预测性擦除
        /// </summary>
        [JsonProperty("usePredictiveErasing")]
        public bool UsePredictiveErasing
        {
            get => _usePredictiveErasing;
            set => SetProperty(ref _usePredictiveErasing, value);
        }

        /// <summary>
        /// 手掌概率阈值（0-1）
        /// </summary>
        [JsonProperty("palmProbabilityThreshold")]
        public double PalmProbabilityThreshold
        {
            get => _palmProbabilityThreshold;
            set => SetProperty(ref _palmProbabilityThreshold, value);
        }

        /// <summary>
        /// 触摸历史记录大小
        /// </summary>
        [JsonProperty("touchHistorySize")]
        public int TouchHistorySize
        {
            get => _touchHistorySize;
            set => SetProperty(ref _touchHistorySize, value);
        }

        /// <summary>
        /// 是否启用触觉反馈
        /// </summary>
        [JsonProperty("enableHapticFeedback")]
        public bool EnableHapticFeedback
        {
            get => _enableHapticFeedback;
            set => SetProperty(ref _enableHapticFeedback, value);
        }

        /// <summary>
        /// 手掌橡皮擦速度阈值
        /// </summary>
        [JsonProperty("palmEraserVelocityThreshold")]
        public double PalmEraserVelocityThreshold
        {
            get => _palmEraserVelocityThreshold;
            set => SetProperty(ref _palmEraserVelocityThreshold, value);
        }
    }
}
