using Newtonsoft.Json;

namespace Ink_Canvas.Models.Settings
{
    /// <summary>
    /// 高级设置类
    /// </summary>
    public class AdvancedSettings : SettingsBase
    {
        #region 常量

        /// <summary>
        /// 最小触摸倍数
        /// </summary>
        public const double MinTouchMultiplier = 0.1;

        /// <summary>
        /// 最大触摸倍数
        /// </summary>
        public const double MaxTouchMultiplier = 1.0;

        /// <summary>
        /// 最小边界宽度
        /// </summary>
        public const int MinBoundsWidth = 1;

        /// <summary>
        /// 最大边界宽度
        /// </summary>
        public const int MaxBoundsWidth = 100;

        /// <summary>
        /// 最小边界宽度阈值
        /// </summary>
        public const double MinBoundsWidthThreshold = 0.5;

        /// <summary>
        /// 最大边界宽度阈值
        /// </summary>
        public const double MaxBoundsWidthThreshold = 10.0;

        /// <summary>
        /// 最小橡皮擦大小倍数
        /// </summary>
        public const double MinEraserSizeMultiplier = 0.1;

        /// <summary>
        /// 最大橡皮擦大小倍数
        /// </summary>
        public const double MaxEraserSizeMultiplier = 2.0;

        #endregion

        #region 字段

        // 屏幕设置
        private bool _isSpecialScreen = false;
        private bool _isQuadIR = false;

        // 触摸倍数设置
        private double _touchMultiplier = 0.25;
        private bool _eraserBindTouchMultiplier = false;

        // 边界宽度设置
        private int _nibModeBoundsWidth = 10;
        private int _fingerModeBoundsWidth = 30;
        private double _nibModeBoundsWidthThresholdValue = 2.5;
        private double _fingerModeBoundsWidthThresholdValue = 2.5;
        private double _nibModeBoundsWidthEraserSize = 0.8;
        private double _fingerModeBoundsWidthEraserSize = 0.8;

        // 日志设置
        private bool _isLogEnabled = true;

        // 全屏助手设置
        private bool _isEnableFullScreenHelper = false;
        private bool _isEnableEdgeGestureUtil = false;
        private bool _edgeGestureUtilOnlyAffectBlackboardMode = false;
        private bool _isEnableForceFullScreen = false;

        // 检测设置
        private bool _isEnableResolutionChangeDetection = false;
        private bool _isEnableDPIChangeDetection = false;

        // 窗口设置
        private bool _isDisableCloseWindow = true;
        private bool _enableForceTopMost = false;

        #endregion

        #region 辅助属性

        /// <summary>
        /// 是否启用了任何屏幕适配功能
        /// </summary>
        [JsonIgnore]
        public bool IsAnyScreenAdaptationEnabled => IsSpecialScreen || IsQuadIR;

        /// <summary>
        /// 是否启用了任何边界宽度功能
        /// </summary>
        [JsonIgnore]
        public bool IsAnyBoundsWidthEnabled => NibModeBoundsWidth > 0 || FingerModeBoundsWidth > 0;

        /// <summary>
        /// 是否启用了任何全屏助手功能
        /// </summary>
        [JsonIgnore]
        public bool IsAnyFullScreenHelperEnabled => IsEnableFullScreenHelper || IsEnableEdgeGestureUtil || IsEnableForceFullScreen;

        /// <summary>
        /// 是否启用了任何检测功能
        /// </summary>
        [JsonIgnore]
        public bool IsAnyDetectionEnabled => IsEnableResolutionChangeDetection || IsEnableDPIChangeDetection;

        #endregion

        #region 屏幕设置属性

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

        #endregion

        #region 触摸倍数设置属性

        /// <summary>
        /// 触摸倍数（0.1-1.0）
        /// </summary>
        [JsonProperty("touchMultiplier")]
        public double TouchMultiplier
        {
            get => _touchMultiplier;
            set => SetProperty(ref _touchMultiplier, ClampRange(value, MinTouchMultiplier, MaxTouchMultiplier));
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

        #endregion

        #region 边界宽度设置属性

        /// <summary>
        /// 笔模式边界宽度（1-100）
        /// </summary>
        [JsonProperty("nibModeBoundsWidth")]
        public int NibModeBoundsWidth
        {
            get => _nibModeBoundsWidth;
            set => SetProperty(ref _nibModeBoundsWidth, ClampRange(value, MinBoundsWidth, MaxBoundsWidth));
        }

        /// <summary>
        /// 手指模式边界宽度（1-100）
        /// </summary>
        [JsonProperty("fingerModeBoundsWidth")]
        public int FingerModeBoundsWidth
        {
            get => _fingerModeBoundsWidth;
            set => SetProperty(ref _fingerModeBoundsWidth, ClampRange(value, MinBoundsWidth, MaxBoundsWidth));
        }

        /// <summary>
        /// 笔模式边界宽度阈值（0.5-10.0）
        /// </summary>
        [JsonProperty("nibModeBoundsWidthThresholdValue")]
        public double NibModeBoundsWidthThresholdValue
        {
            get => _nibModeBoundsWidthThresholdValue;
            set => SetProperty(ref _nibModeBoundsWidthThresholdValue, ClampRange(value, MinBoundsWidthThreshold, MaxBoundsWidthThreshold));
        }

        /// <summary>
        /// 手指模式边界宽度阈值（0.5-10.0）
        /// </summary>
        [JsonProperty("fingerModeBoundsWidthThresholdValue")]
        public double FingerModeBoundsWidthThresholdValue
        {
            get => _fingerModeBoundsWidthThresholdValue;
            set => SetProperty(ref _fingerModeBoundsWidthThresholdValue, ClampRange(value, MinBoundsWidthThreshold, MaxBoundsWidthThreshold));
        }

        /// <summary>
        /// 笔模式边界宽度橡皮擦大小（0.1-2.0）
        /// </summary>
        [JsonProperty("nibModeBoundsWidthEraserSize")]
        public double NibModeBoundsWidthEraserSize
        {
            get => _nibModeBoundsWidthEraserSize;
            set => SetProperty(ref _nibModeBoundsWidthEraserSize, ClampRange(value, MinEraserSizeMultiplier, MaxEraserSizeMultiplier));
        }

        /// <summary>
        /// 手指模式边界宽度橡皮擦大小（0.1-2.0）
        /// </summary>
        [JsonProperty("fingerModeBoundsWidthEraserSize")]
        public double FingerModeBoundsWidthEraserSize
        {
            get => _fingerModeBoundsWidthEraserSize;
            set => SetProperty(ref _fingerModeBoundsWidthEraserSize, ClampRange(value, MinEraserSizeMultiplier, MaxEraserSizeMultiplier));
        }

        #endregion

        #region 日志设置属性

        /// <summary>
        /// 是否启用日志
        /// </summary>
        [JsonProperty("isLogEnabled")]
        public bool IsLogEnabled
        {
            get => _isLogEnabled;
            set => SetProperty(ref _isLogEnabled, value);
        }

        #endregion

        #region 全屏助手设置属性

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

        #endregion

        #region 检测设置属性

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

        #endregion

        #region 窗口设置属性

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

        #endregion
    }
}
