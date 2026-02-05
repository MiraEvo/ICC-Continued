using Newtonsoft.Json;

namespace Ink_Canvas.Models.Settings
{
    /// <summary>
    /// 墨迹转形状设置类
    /// </summary>
    public class InkToShapeSettings : SettingsBase
    {
        #region 常量

        /// <summary>
        /// 最小置信度阈值
        /// </summary>
        public const double MinConfidenceThreshold = 0.0;

        /// <summary>
        /// 最大置信度阈值
        /// </summary>
        public const double MaxConfidenceThreshold = 1.0;

        /// <summary>
        /// 默认置信度阈值
        /// </summary>
        public const double DefaultConfidenceThreshold = 0.3;

        /// <summary>
        /// 最小形状尺寸（像素）
        /// </summary>
        public const double MinShapeSize = 10.0;

        /// <summary>
        /// 最大形状尺寸（像素）
        /// </summary>
        public const double MaxShapeSize = 500.0;

        /// <summary>
        /// 默认形状尺寸（像素）
        /// </summary>
        public const double DefaultShapeSize = 30.0;

        /// <summary>
        /// 最小重采样点数
        /// </summary>
        public const int MinResamplePointCount = 16;

        /// <summary>
        /// 最大重采样点数
        /// </summary>
        public const int MaxResamplePointCount = 128;

        /// <summary>
        /// 默认重采样点数
        /// </summary>
        public const int DefaultResamplePointCount = 48;

        /// <summary>
        /// 最小几何验证强度
        /// </summary>
        public const double MinGeometryValidationStrength = 0.0;

        /// <summary>
        /// 最大几何验证强度
        /// </summary>
        public const double MaxGeometryValidationStrength = 1.0;

        /// <summary>
        /// 默认几何验证强度
        /// </summary>
        public const double DefaultGeometryValidationStrength = 0.1;

        #endregion

        #region 字段

        // 功能启用设置
        private bool _isInkToShapeEnabled = true;
        private bool _enableDrawingToolbar = true;
        private bool _expandShapeVariantsByDefault = false;

        // 形状类型设置
        private bool _isInkToShapeTriangle = true;
        private bool _isInkToShapeRectangle = true;
        private bool _isInkToShapeRounded = true;
        private bool _enablePolygonRecognition = false;

        // 压力设置
        private bool _isInkToShapeNoFakePressureRectangle = false;
        private bool _isInkToShapeNoFakePressureTriangle = false;

        // 识别参数设置
        private double _confidenceThreshold = DefaultConfidenceThreshold;
        private double _minimumShapeSize = DefaultShapeSize;
        private bool _enableShapeSmoothing = false;
        private int _resamplePointCount = DefaultResamplePointCount;
        private bool _enableAdaptiveResampling = true;
        private double _geometryValidationStrength = DefaultGeometryValidationStrength;
        private bool _treatRecognizedInkAsShape = true;

        #endregion

        #region 辅助属性

        /// <summary>
        /// 是否启用了任何形状识别
        /// </summary>
        [JsonIgnore]
        public bool IsAnyShapeRecognitionEnabled =>
            IsInkToShapeTriangle || IsInkToShapeRectangle || IsInkToShapeRounded || EnablePolygonRecognition;

        /// <summary>
        /// 是否启用了任何假压力设置
        /// </summary>
        [JsonIgnore]
        public bool IsAnyFakePressureSettingEnabled =>
            IsInkToShapeNoFakePressureRectangle || IsInkToShapeNoFakePressureTriangle;

        /// <summary>
        /// 是否启用了高级识别功能
        /// </summary>
        [JsonIgnore]
        public bool IsAdvancedRecognitionEnabled =>
            EnableAdaptiveResampling || EnableShapeSmoothing;

        /// <summary>
        /// 获取当前启用的形状类型数量
        /// </summary>
        [JsonIgnore]
        public int EnabledShapeTypeCount
        {
            get
            {
                int count = 0;
                if (IsInkToShapeTriangle) count++;
                if (IsInkToShapeRectangle) count++;
                if (IsInkToShapeRounded) count++;
                if (EnablePolygonRecognition) count++;
                return count;
            }
        }

        #endregion

        #region 功能启用属性

        /// <summary>
        /// 是否启用墨迹转形状
        /// </summary>
        [JsonProperty("isInkToShapeEnabled")]
        public bool IsInkToShapeEnabled
        {
            get => _isInkToShapeEnabled;
            set => SetProperty(ref _isInkToShapeEnabled, value);
        }

        /// <summary>
        /// 启用绘制时工具栏
        /// </summary>
        [JsonProperty("enableDrawingToolbar")]
        public bool EnableDrawingToolbar
        {
            get => _enableDrawingToolbar;
            set => SetProperty(ref _enableDrawingToolbar, value);
        }

        /// <summary>
        /// 工具栏默认展开形状变体栏
        /// </summary>
        [JsonProperty("expandShapeVariantsByDefault")]
        public bool ExpandShapeVariantsByDefault
        {
            get => _expandShapeVariantsByDefault;
            set => SetProperty(ref _expandShapeVariantsByDefault, value);
        }

        #endregion

        #region 形状类型属性

        /// <summary>
        /// 是否启用三角形识别
        /// </summary>
        [JsonProperty("isInkToShapeTriangle")]
        public bool IsInkToShapeTriangle
        {
            get => _isInkToShapeTriangle;
            set => SetProperty(ref _isInkToShapeTriangle, value);
        }

        /// <summary>
        /// 是否启用矩形识别
        /// </summary>
        [JsonProperty("isInkToShapeRectangle")]
        public bool IsInkToShapeRectangle
        {
            get => _isInkToShapeRectangle;
            set => SetProperty(ref _isInkToShapeRectangle, value);
        }

        /// <summary>
        /// 是否启用圆角识别
        /// </summary>
        [JsonProperty("isInkToShapeRounded")]
        public bool IsInkToShapeRounded
        {
            get => _isInkToShapeRounded;
            set => SetProperty(ref _isInkToShapeRounded, value);
        }

        /// <summary>
        /// 启用多边形识别（五边形、六边形）
        /// </summary>
        [JsonProperty("enablePolygonRecognition")]
        public bool EnablePolygonRecognition
        {
            get => _enablePolygonRecognition;
            set => SetProperty(ref _enablePolygonRecognition, value);
        }

        #endregion

        #region 压力设置属性

        /// <summary>
        /// 矩形是否不使用假压力
        /// </summary>
        [JsonProperty("isInkToShapeNoFakePressureRectangle")]
        public bool IsInkToShapeNoFakePressureRectangle
        {
            get => _isInkToShapeNoFakePressureRectangle;
            set => SetProperty(ref _isInkToShapeNoFakePressureRectangle, value);
        }

        /// <summary>
        /// 三角形是否不使用假压力
        /// </summary>
        [JsonProperty("isInkToShapeNoFakePressureTriangle")]
        public bool IsInkToShapeNoFakePressureTriangle
        {
            get => _isInkToShapeNoFakePressureTriangle;
            set => SetProperty(ref _isInkToShapeNoFakePressureTriangle, value);
        }

        #endregion

        #region 识别参数属性

        /// <summary>
        /// 识别置信度阈值 (0.0-1.0)，降低以提高识别率
        /// </summary>
        [JsonProperty("confidenceThreshold")]
        public double ConfidenceThreshold
        {
            get => _confidenceThreshold;
            set => SetProperty(ref _confidenceThreshold, ClampRange(value, MinConfidenceThreshold, MaxConfidenceThreshold));
        }

        /// <summary>
        /// 最小形状尺寸（像素，10-500），降低以支持更小的形状
        /// </summary>
        [JsonProperty("minimumShapeSize")]
        public double MinimumShapeSize
        {
            get => _minimumShapeSize;
            set => SetProperty(ref _minimumShapeSize, ClampRange(value, MinShapeSize, MaxShapeSize));
        }

        /// <summary>
        /// 禁用形状平滑，避免影响识别
        /// </summary>
        [JsonProperty("enableShapeSmoothing")]
        public bool EnableShapeSmoothing
        {
            get => _enableShapeSmoothing;
            set => SetProperty(ref _enableShapeSmoothing, value);
        }

        /// <summary>
        /// 重采样点数（用于识别优化，16-128）
        /// </summary>
        [JsonProperty("resamplePointCount")]
        public int ResamplePointCount
        {
            get => _resamplePointCount;
            set => SetProperty(ref _resamplePointCount, ClampRange(value, MinResamplePointCount, MaxResamplePointCount));
        }

        /// <summary>
        /// 启用自适应重采样
        /// </summary>
        [JsonProperty("enableAdaptiveResampling")]
        public bool EnableAdaptiveResampling
        {
            get => _enableAdaptiveResampling;
            set => SetProperty(ref _enableAdaptiveResampling, value);
        }

        /// <summary>
        /// 几何验证强度 (0.0-1.0)，降低以提高通过率
        /// </summary>
        [JsonProperty("geometryValidationStrength")]
        public double GeometryValidationStrength
        {
            get => _geometryValidationStrength;
            set => SetProperty(ref _geometryValidationStrength, ClampRange(value, MinGeometryValidationStrength, MaxGeometryValidationStrength));
        }

        /// <summary>
        /// 将识别的墨迹当作形状处理
        /// </summary>
        [JsonProperty("treatRecognizedInkAsShape")]
        public bool TreatRecognizedInkAsShape
        {
            get => _treatRecognizedInkAsShape;
            set => SetProperty(ref _treatRecognizedInkAsShape, value);
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 重置置信度为默认值
        /// </summary>
        public void ResetConfidenceThreshold()
        {
            ConfidenceThreshold = DefaultConfidenceThreshold;
        }

        /// <summary>
        /// 重置形状尺寸为默认值
        /// </summary>
        public void ResetMinimumShapeSize()
        {
            MinimumShapeSize = DefaultShapeSize;
        }

        /// <summary>
        /// 重置重采样点数为默认值
        /// </summary>
        public void ResetResamplePointCount()
        {
            ResamplePointCount = DefaultResamplePointCount;
        }

        /// <summary>
        /// 重置几何验证强度为默认值
        /// </summary>
        public void ResetGeometryValidationStrength()
        {
            GeometryValidationStrength = DefaultGeometryValidationStrength;
        }

        /// <summary>
        /// 启用所有形状类型识别
        /// </summary>
        public void EnableAllShapeTypes()
        {
            IsInkToShapeTriangle = true;
            IsInkToShapeRectangle = true;
            IsInkToShapeRounded = true;
            EnablePolygonRecognition = true;
        }

        /// <summary>
        /// 禁用所有形状类型识别
        /// </summary>
        public void DisableAllShapeTypes()
        {
            IsInkToShapeTriangle = false;
            IsInkToShapeRectangle = false;
            IsInkToShapeRounded = false;
            EnablePolygonRecognition = false;
        }

        /// <summary>
        /// 重置所有设置为默认值
        /// </summary>
        public void ResetToDefaults()
        {
            IsInkToShapeEnabled = true;
            EnableDrawingToolbar = true;
            ExpandShapeVariantsByDefault = false;
            IsInkToShapeTriangle = true;
            IsInkToShapeRectangle = true;
            IsInkToShapeRounded = true;
            EnablePolygonRecognition = false;
            IsInkToShapeNoFakePressureRectangle = false;
            IsInkToShapeNoFakePressureTriangle = false;
            ConfidenceThreshold = DefaultConfidenceThreshold;
            MinimumShapeSize = DefaultShapeSize;
            EnableShapeSmoothing = false;
            ResamplePointCount = DefaultResamplePointCount;
            EnableAdaptiveResampling = true;
            GeometryValidationStrength = DefaultGeometryValidationStrength;
            TreatRecognizedInkAsShape = true;
        }

        #endregion
    }
}
