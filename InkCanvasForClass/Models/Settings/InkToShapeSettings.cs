using Newtonsoft.Json;

namespace Ink_Canvas.Models.Settings
{
    /// <summary>
    /// 墨迹转形状设置类
    /// </summary>
    public class InkToShapeSettings : SettingsBase
    {
        private bool _isInkToShapeEnabled = true;
        private bool _isInkToShapeNoFakePressureRectangle = false;
        private bool _isInkToShapeNoFakePressureTriangle = false;
        private bool _isInkToShapeTriangle = true;
        private bool _isInkToShapeRectangle = true;
        private bool _isInkToShapeRounded = true;
        private double _confidenceThreshold = 0.3;
        private double _minimumShapeSize = 30.0;
        private bool _enablePolygonRecognition = false;
        private bool _enableShapeSmoothing = false;
        private int _resamplePointCount = 48;
        private bool _enableAdaptiveResampling = true;
        private double _geometryValidationStrength = 0.3;

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
        /// 识别置信度阈值 (0.0-1.0)，降低以提高识别率
        /// </summary>
        [JsonProperty("confidenceThreshold")]
        public double ConfidenceThreshold
        {
            get => _confidenceThreshold;
            set => SetProperty(ref _confidenceThreshold, ValidateRange(value, 0.0, 1.0));
        }

        /// <summary>
        /// 最小形状尺寸（像素），降低以支持更小的形状
        /// </summary>
        [JsonProperty("minimumShapeSize")]
        public double MinimumShapeSize
        {
            get => _minimumShapeSize;
            set => SetProperty(ref _minimumShapeSize, value);
        }

        /// <summary>
        /// 启用多边形识别（五边形、六边形）- 已禁用
        /// </summary>
        [JsonProperty("enablePolygonRecognition")]
        public bool EnablePolygonRecognition
        {
            get => _enablePolygonRecognition;
            set => SetProperty(ref _enablePolygonRecognition, value);
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
        /// 重采样点数（用于识别优化）
        /// </summary>
        [JsonProperty("resamplePointCount")]
        public int ResamplePointCount
        {
            get => _resamplePointCount;
            set => SetProperty(ref _resamplePointCount, value);
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
            set => SetProperty(ref _geometryValidationStrength, ValidateRange(value, 0.0, 1.0));
        }
    }
}
