using System;
using Newtonsoft.Json;

namespace Ink_Canvas.Models.Settings
{
    /// <summary>
    /// 画布设置类
    /// </summary>
    public class CanvasSettings : SettingsBase
    {
        private double _inkWidth = 2;
        private double _highlighterWidth = 20;
        private double _inkAlpha = 255;
        private bool _isShowCursor = false;
        private int _inkStyle = 0;
        private int _eraserSize = 2;
        private int _eraserType = 0;
        private int _eraserShapeType = 0;
        private bool _hideStrokeWhenSelecting = true;
        private bool _fitToCurve = true;
        private bool _clearCanvasAndClearTimeMachine = false;
        private bool _usingWhiteboard = false;
        private Ink_Canvas.OptionalOperation _hyperbolaAsymptoteOption = Ink_Canvas.OptionalOperation.Ask;
        private Ink_Canvas.BlackboardBackgroundColorEnum _blackboardBackgroundColor = Ink_Canvas.BlackboardBackgroundColorEnum.White;
        private Ink_Canvas.BlackboardBackgroundPatternEnum _blackboardBackgroundPattern = Ink_Canvas.BlackboardBackgroundPatternEnum.None;
        private bool _useDefaultBackgroundColorForEveryNewAddedBlackboardPage = false;
        private bool _useDefaultBackgroundPatternForEveryNewAddedBlackboardPage = false;
        private bool _isEnableAutoConvertInkColorWhenBackgroundChanged = false;
        private bool _applyScaleToStylusTip = false;
        private bool _onlyHitTestFullyContainedStrokes = false;
        private bool _allowClickToSelectLockedStroke = false;
        private int _selectionMethod = 0;
        private int _lastPenType = 0;
        private int _lastDesktopInkColor = 1;
        private int _lastBoardInkColor = 5;
        private int _lastHighlighterColor = 102;

        /// <summary>
        /// 墨迹宽度 (0.5 - 50)
        /// </summary>
        [JsonProperty("inkWidth")]
        public double InkWidth
        {
            get => _inkWidth;
            set => SetProperty(ref _inkWidth, ValidateRange(value, 0.5, 50.0));
        }

        /// <summary>
        /// 荧光笔宽度 (5 - 100)
        /// </summary>
        [JsonProperty("highlighterWidth")]
        public double HighlighterWidth
        {
            get => _highlighterWidth;
            set => SetProperty(ref _highlighterWidth, ValidateRange(value, 5.0, 100.0));
        }

        /// <summary>
        /// 墨迹透明度 (0 - 255)
        /// </summary>
        [JsonProperty("inkAlpha")]
        public double InkAlpha
        {
            get => _inkAlpha;
            set => SetProperty(ref _inkAlpha, ValidateRange(value, 0.0, 255.0));
        }

        /// <summary>
        /// 是否显示光标
        /// </summary>
        [JsonProperty("isShowCursor")]
        public bool IsShowCursor
        {
            get => _isShowCursor;
            set => SetProperty(ref _isShowCursor, value);
        }

        /// <summary>
        /// 墨迹样式
        /// </summary>
        [JsonProperty("inkStyle")]
        public int InkStyle
        {
            get => _inkStyle;
            set => SetProperty(ref _inkStyle, value);
        }

        /// <summary>
        /// 橡皮擦大小
        /// </summary>
        [JsonProperty("eraserSize")]
        public int EraserSize
        {
            get => _eraserSize;
            set => SetProperty(ref _eraserSize, value);
        }

        /// <summary>
        /// 橡皮擦类型 (0 - 图标切换模式, 1 - 面积擦, 2 - 线条擦)
        /// </summary>
        [JsonProperty("eraserType")]
        public int EraserType
        {
            get => _eraserType;
            set => SetProperty(ref _eraserType, value);
        }

        /// <summary>
        /// 橡皮擦形状类型 (0 - 圆形擦, 1 - 黑板擦)
        /// </summary>
        [JsonProperty("eraserShapeType")]
        public int EraserShapeType
        {
            get => _eraserShapeType;
            set => SetProperty(ref _eraserShapeType, value);
        }

        /// <summary>
        /// 选择时是否隐藏笔画
        /// </summary>
        [JsonProperty("hideStrokeWhenSelecting")]
        public bool HideStrokeWhenSelecting
        {
            get => _hideStrokeWhenSelecting;
            set => SetProperty(ref _hideStrokeWhenSelecting, value);
        }

        /// <summary>
        /// 是否拟合曲线
        /// </summary>
        [JsonProperty("fitToCurve")]
        public bool FitToCurve
        {
            get => _fitToCurve;
            set => SetProperty(ref _fitToCurve, value);
        }

        /// <summary>
        /// 清除画布时是否同时清除时间机器
        /// </summary>
        [JsonProperty("clearCanvasAndClearTimeMachine")]
        public bool ClearCanvasAndClearTimeMachine
        {
            get => _clearCanvasAndClearTimeMachine;
            set => SetProperty(ref _clearCanvasAndClearTimeMachine, value);
        }

        /// <summary>
        /// 是否使用白板（已废弃，使用 BlackboardBackgroundColor 替代）
        /// </summary>
        [Obsolete("已经使用多背景色 blackboardBackgroundColor 替换该选项")]
        [JsonProperty("usingWhiteboard")]
        public bool UsingWhiteboard
        {
            get => _usingWhiteboard;
            set => SetProperty(ref _usingWhiteboard, value);
        }

        /// <summary>
        /// 双曲线渐近线选项
        /// </summary>
        [JsonProperty("hyperbolaAsymptoteOption")]
        public Ink_Canvas.OptionalOperation HyperbolaAsymptoteOption
        {
            get => _hyperbolaAsymptoteOption;
            set => SetProperty(ref _hyperbolaAsymptoteOption, value);
        }

        /// <summary>
        /// 黑板背景颜色
        /// </summary>
        [JsonProperty("blackboardBackgroundColor")]
        public Ink_Canvas.BlackboardBackgroundColorEnum BlackboardBackgroundColor
        {
            get => _blackboardBackgroundColor;
            set => SetProperty(ref _blackboardBackgroundColor, value);
        }

        /// <summary>
        /// 黑板背景图案
        /// </summary>
        [JsonProperty("blackboardBackgroundPattern")]
        public Ink_Canvas.BlackboardBackgroundPatternEnum BlackboardBackgroundPattern
        {
            get => _blackboardBackgroundPattern;
            set => SetProperty(ref _blackboardBackgroundPattern, value);
        }

        /// <summary>
        /// 每个新添加的黑板页面是否使用默认背景颜色
        /// </summary>
        [JsonProperty("useDefaultBackgroundColorForEveryNewAddedBlackboardPage")]
        public bool UseDefaultBackgroundColorForEveryNewAddedBlackboardPage
        {
            get => _useDefaultBackgroundColorForEveryNewAddedBlackboardPage;
            set => SetProperty(ref _useDefaultBackgroundColorForEveryNewAddedBlackboardPage, value);
        }

        /// <summary>
        /// 每个新添加的黑板页面是否使用默认背景图案
        /// </summary>
        [JsonProperty("useDefaultBackgroundPatternForEveryNewAddedBlackboardPage")]
        public bool UseDefaultBackgroundPatternForEveryNewAddedBlackboardPage
        {
            get => _useDefaultBackgroundPatternForEveryNewAddedBlackboardPage;
            set => SetProperty(ref _useDefaultBackgroundPatternForEveryNewAddedBlackboardPage, value);
        }

        /// <summary>
        /// 背景改变时是否自动转换墨迹颜色
        /// </summary>
        [JsonProperty("isEnableAutoConvertInkColorWhenBackgroundChanged")]
        public bool IsEnableAutoConvertInkColorWhenBackgroundChanged
        {
            get => _isEnableAutoConvertInkColorWhenBackgroundChanged;
            set => SetProperty(ref _isEnableAutoConvertInkColorWhenBackgroundChanged, value);
        }

        /// <summary>
        /// 是否将缩放应用到笔尖
        /// </summary>
        [JsonProperty("ApplyScaleToStylusTip")]
        public bool ApplyScaleToStylusTip
        {
            get => _applyScaleToStylusTip;
            set => SetProperty(ref _applyScaleToStylusTip, value);
        }

        /// <summary>
        /// 是否仅命中测试完全包含的笔画
        /// </summary>
        [JsonProperty("onlyHitTestFullyContainedStrokes")]
        public bool OnlyHitTestFullyContainedStrokes
        {
            get => _onlyHitTestFullyContainedStrokes;
            set => SetProperty(ref _onlyHitTestFullyContainedStrokes, value);
        }

        /// <summary>
        /// 是否允许点击选择锁定的笔画
        /// </summary>
        [JsonProperty("allowClickToSelectLockedStroke")]
        public bool AllowClickToSelectLockedStroke
        {
            get => _allowClickToSelectLockedStroke;
            set => SetProperty(ref _allowClickToSelectLockedStroke, value);
        }

        /// <summary>
        /// 选择方法
        /// </summary>
        [JsonProperty("selectionMethod")]
        public int SelectionMethod
        {
            get => _selectionMethod;
            set => SetProperty(ref _selectionMethod, value);
        }

        /// <summary>
        /// 最后使用的笔类型 (0=签字笔, 1=荧光笔)
        /// </summary>
        [JsonProperty("lastPenType")]
        public int LastPenType
        {
            get => _lastPenType;
            set => SetProperty(ref _lastPenType, value);
        }

        /// <summary>
        /// 桌面模式最后使用的墨迹颜色
        /// </summary>
        [JsonProperty("lastDesktopInkColor")]
        public int LastDesktopInkColor
        {
            get => _lastDesktopInkColor;
            set => SetProperty(ref _lastDesktopInkColor, value);
        }

        /// <summary>
        /// 白板模式最后使用的墨迹颜色
        /// </summary>
        [JsonProperty("lastBoardInkColor")]
        public int LastBoardInkColor
        {
            get => _lastBoardInkColor;
            set => SetProperty(ref _lastBoardInkColor, value);
        }

        /// <summary>
        /// 荧光笔颜色
        /// </summary>
        [JsonProperty("lastHighlighterColor")]
        public int LastHighlighterColor
        {
            get => _lastHighlighterColor;
            set => SetProperty(ref _lastHighlighterColor, value);
        }
    }
}
