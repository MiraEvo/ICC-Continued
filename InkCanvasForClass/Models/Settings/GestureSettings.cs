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
        private bool _enableMouseGesture = true;
        private bool _enableMouseRightBtnGesture = true;
        private bool _enableMouseWheelGesture = true;

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
    }
}
