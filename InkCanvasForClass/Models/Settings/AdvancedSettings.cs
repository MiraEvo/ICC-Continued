using Newtonsoft.Json;

namespace Ink_Canvas.Models.Settings
{
    /// <summary>
    /// 高级设置类
    /// </summary>
    public class AdvancedSettings : SettingsBase
    {
        private bool _isSpecialScreen = false;
        private bool _isQuadIR = false;
        private double _touchMultiplier = 0.25;
        private int _nibModeBoundsWidth = 10;
        private int _fingerModeBoundsWidth = 30;
        private bool _eraserBindTouchMultiplier = false;
        private double _nibModeBoundsWidthThresholdValue = 2.5;
        private double _fingerModeBoundsWidthThresholdValue = 2.5;
        private double _nibModeBoundsWidthEraserSize = 0.8;
        private double _fingerModeBoundsWidthEraserSize = 0.8;
        private bool _isLogEnabled = true;
        private bool _isEnableFullScreenHelper = false;
        private bool _isEnableEdgeGestureUtil = false;
        private bool _edgeGestureUtilOnlyAffectBlackboardMode = false;
        private bool _isEnableForceFullScreen = false;
        private bool _isEnableResolutionChangeDetection = false;
        private bool _isEnableDPIChangeDetection = false;
        private bool _isDisableCloseWindow = true;
        private bool _enableForceTopMost = false;

        /// <summary>
        /// 是否为特殊屏幕
        /// </summary>
        [JsonProperty("isSpecialScreen")]
        public bool IsSpecialScreen
        {
            get => _isSpecialScreen;
            set => SetProperty(ref _isSpecialScreen, value);
        }

        /// <summary>
        /// 是否为四点红外屏
        /// </summary>
        [JsonProperty("isQuadIR")]
        public bool IsQuadIR
        {
            get => _isQuadIR;
            set => SetProperty(ref _isQuadIR, value);
        }

        /// <summary>
        /// 触摸倍数
        /// </summary>
        [JsonProperty("touchMultiplier")]
        public double TouchMultiplier
        {
            get => _touchMultiplier;
            set => SetProperty(ref _touchMultiplier, value);
        }

        /// <summary>
        /// 笔模式边界宽度
        /// </summary>
        [JsonProperty("nibModeBoundsWidth")]
        public int NibModeBoundsWidth
        {
            get => _nibModeBoundsWidth;
            set => SetProperty(ref _nibModeBoundsWidth, value);
        }

        /// <summary>
        /// 手指模式边界宽度
        /// </summary>
        [JsonProperty("fingerModeBoundsWidth")]
        public int FingerModeBoundsWidth
        {
            get => _fingerModeBoundsWidth;
            set => SetProperty(ref _fingerModeBoundsWidth, value);
        }

        /// <summary>
        /// 橡皮擦是否绑定触摸倍数
        /// </summary>
        [JsonProperty("eraserBindTouchMultiplier")]
        public bool EraserBindTouchMultiplier
        {
            get => _eraserBindTouchMultiplier;
            set => SetProperty(ref _eraserBindTouchMultiplier, value);
        }

        /// <summary>
        /// 笔模式边界宽度阈值
        /// </summary>
        [JsonProperty("nibModeBoundsWidthThresholdValue")]
        public double NibModeBoundsWidthThresholdValue
        {
            get => _nibModeBoundsWidthThresholdValue;
            set => SetProperty(ref _nibModeBoundsWidthThresholdValue, value);
        }

        /// <summary>
        /// 手指模式边界宽度阈值
        /// </summary>
        [JsonProperty("fingerModeBoundsWidthThresholdValue")]
        public double FingerModeBoundsWidthThresholdValue
        {
            get => _fingerModeBoundsWidthThresholdValue;
            set => SetProperty(ref _fingerModeBoundsWidthThresholdValue, value);
        }

        /// <summary>
        /// 笔模式边界宽度橡皮擦大小
        /// </summary>
        [JsonProperty("nibModeBoundsWidthEraserSize")]
        public double NibModeBoundsWidthEraserSize
        {
            get => _nibModeBoundsWidthEraserSize;
            set => SetProperty(ref _nibModeBoundsWidthEraserSize, value);
        }

        /// <summary>
        /// 手指模式边界宽度橡皮擦大小
        /// </summary>
        [JsonProperty("fingerModeBoundsWidthEraserSize")]
        public double FingerModeBoundsWidthEraserSize
        {
            get => _fingerModeBoundsWidthEraserSize;
            set => SetProperty(ref _fingerModeBoundsWidthEraserSize, value);
        }

        /// <summary>
        /// 是否启用日志
        /// </summary>
        [JsonProperty("isLogEnabled")]
        public bool IsLogEnabled
        {
            get => _isLogEnabled;
            set => SetProperty(ref _isLogEnabled, value);
        }

        /// <summary>
        /// 是否启用全屏助手
        /// </summary>
        [JsonProperty("isEnableFullScreenHelper")]
        public bool IsEnableFullScreenHelper
        {
            get => _isEnableFullScreenHelper;
            set => SetProperty(ref _isEnableFullScreenHelper, value);
        }

        /// <summary>
        /// 是否启用边缘手势工具
        /// </summary>
        [JsonProperty("isEnableEdgeGestureUtil")]
        public bool IsEnableEdgeGestureUtil
        {
            get => _isEnableEdgeGestureUtil;
            set => SetProperty(ref _isEnableEdgeGestureUtil, value);
        }

        /// <summary>
        /// 边缘手势工具是否仅影响黑板模式
        /// </summary>
        [JsonProperty("edgeGestureUtilOnlyAffectBlackboardMode")]
        public bool EdgeGestureUtilOnlyAffectBlackboardMode
        {
            get => _edgeGestureUtilOnlyAffectBlackboardMode;
            set => SetProperty(ref _edgeGestureUtilOnlyAffectBlackboardMode, value);
        }

        /// <summary>
        /// 是否启用强制全屏
        /// </summary>
        [JsonProperty("isEnableForceFullScreen")]
        public bool IsEnableForceFullScreen
        {
            get => _isEnableForceFullScreen;
            set => SetProperty(ref _isEnableForceFullScreen, value);
        }

        /// <summary>
        /// 是否启用分辨率变化检测
        /// </summary>
        [JsonProperty("isEnableResolutionChangeDetection")]
        public bool IsEnableResolutionChangeDetection
        {
            get => _isEnableResolutionChangeDetection;
            set => SetProperty(ref _isEnableResolutionChangeDetection, value);
        }

        /// <summary>
        /// 是否启用 DPI 变化检测
        /// </summary>
        [JsonProperty("isEnableDPIChangeDetection")]
        public bool IsEnableDPIChangeDetection
        {
            get => _isEnableDPIChangeDetection;
            set => SetProperty(ref _isEnableDPIChangeDetection, value);
        }

        /// <summary>
        /// 是否禁用关闭窗口
        /// </summary>
        [JsonProperty("isDisableCloseWindow")]
        public bool IsDisableCloseWindow
        {
            get => _isDisableCloseWindow;
            set => SetProperty(ref _isDisableCloseWindow, value);
        }

        /// <summary>
        /// 是否启用强制置顶
        /// </summary>
        [JsonProperty("enableForceTopMost")]
        public bool EnableForceTopMost
        {
            get => _enableForceTopMost;
            set => SetProperty(ref _enableForceTopMost, value);
        }
    }
}
