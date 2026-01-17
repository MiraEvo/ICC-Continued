using Newtonsoft.Json;

namespace Ink_Canvas.Models.Settings
{
    /// <summary>
    /// PowerPoint 设置类
    /// </summary>
    public class PowerPointSettings : SettingsBase
    {
        private bool _showPPTButton = true;
        private int _pptButtonsDisplayOption = 2222;
        private int _pptLSButtonPosition = 0;
        private int _pptRSButtonPosition = 0;
        private int _pptSButtonsOption = 221;
        private int _pptBButtonsOption = 121;
        private bool _enablePPTButtonPageClickable = true;
        private bool _powerPointSupport = true;
        private bool _isShowCanvasAtNewSlideShow = true;
        private bool _isNoClearStrokeOnSelectWhenInPowerPoint = true;
        private bool _isShowStrokeOnSelectInPowerPoint = false;
        private bool _isAutoSaveStrokesInPowerPoint = true;
        private bool _isAutoSaveScreenShotInPowerPoint = false;
        private bool _isNotifyPreviousPage = false;
        private bool _isNotifyHiddenPage = true;
        private bool _isNotifyAutoPlayPresentation = true;
        private bool _isEnableTwoFingerGestureInPresentationMode = false;
        private bool _isSupportWPS = true;
        private bool _isEnablePPTEnhancedSupport = false;
        private bool _registryShowSlideShowToolbar = false;
        private bool _registryShowBlackScreenLastSlideShow = false;
        private bool _registryDisableSideToolbar = false;
        private bool _isAutoEnterAnnotationMode = true;
        private bool _isRememberLastPlaybackPosition = false;

        /// <summary>
        /// 注册表：强制禁用两侧工具栏按钮
        /// </summary>
        [JsonProperty("registryDisableSideToolbar")]
        public bool RegistryDisableSideToolbar
        {
            get => _registryDisableSideToolbar;
            set => SetProperty(ref _registryDisableSideToolbar, value);
        }

        /// <summary>
        /// 进入 PPT 放映时自动进入批注模式
        /// </summary>
        [JsonProperty("isAutoEnterAnnotationMode")]
        public bool IsAutoEnterAnnotationMode
        {
            get => _isAutoEnterAnnotationMode;
            set => SetProperty(ref _isAutoEnterAnnotationMode, value);
        }

        /// <summary>
        /// 记忆并提示上次播放位置
        /// </summary>
        [JsonProperty("isRememberLastPlaybackPosition")]
        public bool IsRememberLastPlaybackPosition
        {
            get => _isRememberLastPlaybackPosition;
            set => SetProperty(ref _isRememberLastPlaybackPosition, value);
        }

        /// <summary>
        /// 是否显示 PPT 按钮
        /// </summary>
        [JsonProperty("showPPTButton")]
        public bool ShowPPTButton
        {
            get => _showPPTButton;
            set => SetProperty(ref _showPPTButton, value);
        }

        /// <summary>
        /// PPT 按钮显示选项（每一个数位代表一个选项，2就是开启，1就是关闭）
        /// </summary>
        [JsonProperty("pptButtonsDisplayOption")]
        public int PPTButtonsDisplayOption
        {
            get => _pptButtonsDisplayOption;
            set => SetProperty(ref _pptButtonsDisplayOption, value);
        }

        /// <summary>
        /// PPT 左侧按钮位置（0居中，+就是往上，-就是往下）
        /// </summary>
        [JsonProperty("pptLSButtonPosition")]
        public int PPTLSButtonPosition
        {
            get => _pptLSButtonPosition;
            set => SetProperty(ref _pptLSButtonPosition, value);
        }

        /// <summary>
        /// PPT 右侧按钮位置（0居中，+就是往上，-就是往下）
        /// </summary>
        [JsonProperty("pptRSButtonPosition")]
        public int PPTRSButtonPosition
        {
            get => _pptRSButtonPosition;
            set => SetProperty(ref _pptRSButtonPosition, value);
        }

        /// <summary>
        /// PPT S 按钮选项
        /// </summary>
        [JsonProperty("pptSButtonsOption")]
        public int PPTSButtonsOption
        {
            get => _pptSButtonsOption;
            set => SetProperty(ref _pptSButtonsOption, value);
        }

        /// <summary>
        /// PPT B 按钮选项
        /// </summary>
        [JsonProperty("pptBButtonsOption")]
        public int PPTBButtonsOption
        {
            get => _pptBButtonsOption;
            set => SetProperty(ref _pptBButtonsOption, value);
        }

        /// <summary>
        /// 是否启用 PPT 按钮页面点击
        /// </summary>
        [JsonProperty("enablePPTButtonPageClickable")]
        public bool EnablePPTButtonPageClickable
        {
            get => _enablePPTButtonPageClickable;
            set => SetProperty(ref _enablePPTButtonPageClickable, value);
        }

        /// <summary>
        /// 是否支持 PowerPoint
        /// </summary>
        [JsonProperty("powerPointSupport")]
        public bool PowerPointSupport
        {
            get => _powerPointSupport;
            set => SetProperty(ref _powerPointSupport, value);
        }

        /// <summary>
        /// 新幻灯片放映时是否显示画布
        /// </summary>
        [JsonProperty("isShowCanvasAtNewSlideShow")]
        public bool IsShowCanvasAtNewSlideShow
        {
            get => _isShowCanvasAtNewSlideShow;
            set => SetProperty(ref _isShowCanvasAtNewSlideShow, value);
        }

        /// <summary>
        /// 在 PowerPoint 中选择时是否不清除笔画
        /// </summary>
        [JsonProperty("isNoClearStrokeOnSelectWhenInPowerPoint")]
        public bool IsNoClearStrokeOnSelectWhenInPowerPoint
        {
            get => _isNoClearStrokeOnSelectWhenInPowerPoint;
            set => SetProperty(ref _isNoClearStrokeOnSelectWhenInPowerPoint, value);
        }

        /// <summary>
        /// 在 PowerPoint 中选择时是否显示笔画
        /// </summary>
        [JsonProperty("isShowStrokeOnSelectInPowerPoint")]
        public bool IsShowStrokeOnSelectInPowerPoint
        {
            get => _isShowStrokeOnSelectInPowerPoint;
            set => SetProperty(ref _isShowStrokeOnSelectInPowerPoint, value);
        }

        /// <summary>
        /// 是否在 PowerPoint 中自动保存笔画
        /// </summary>
        [JsonProperty("isAutoSaveStrokesInPowerPoint")]
        public bool IsAutoSaveStrokesInPowerPoint
        {
            get => _isAutoSaveStrokesInPowerPoint;
            set => SetProperty(ref _isAutoSaveStrokesInPowerPoint, value);
        }

        /// <summary>
        /// 是否在 PowerPoint 中自动保存截图
        /// </summary>
        [JsonProperty("isAutoSaveScreenShotInPowerPoint")]
        public bool IsAutoSaveScreenShotInPowerPoint
        {
            get => _isAutoSaveScreenShotInPowerPoint;
            set => SetProperty(ref _isAutoSaveScreenShotInPowerPoint, value);
        }

        /// <summary>
        /// 是否通知上一页
        /// </summary>
        [JsonProperty("isNotifyPreviousPage")]
        public bool IsNotifyPreviousPage
        {
            get => _isNotifyPreviousPage;
            set => SetProperty(ref _isNotifyPreviousPage, value);
        }

        /// <summary>
        /// 是否通知隐藏页
        /// </summary>
        [JsonProperty("isNotifyHiddenPage")]
        public bool IsNotifyHiddenPage
        {
            get => _isNotifyHiddenPage;
            set => SetProperty(ref _isNotifyHiddenPage, value);
        }

        /// <summary>
        /// 是否通知自动播放演示
        /// </summary>
        [JsonProperty("isNotifyAutoPlayPresentation")]
        public bool IsNotifyAutoPlayPresentation
        {
            get => _isNotifyAutoPlayPresentation;
            set => SetProperty(ref _isNotifyAutoPlayPresentation, value);
        }

        /// <summary>
        /// 演示模式下是否启用双指手势
        /// </summary>
        [JsonProperty("isEnableTwoFingerGestureInPresentationMode")]
        public bool IsEnableTwoFingerGestureInPresentationMode
        {
            get => _isEnableTwoFingerGestureInPresentationMode;
            set => SetProperty(ref _isEnableTwoFingerGestureInPresentationMode, value);
        }

        /// <summary>
        /// 是否支持 WPS
        /// </summary>
        [JsonProperty("isSupportWPS")]
        public bool IsSupportWPS
        {
            get => _isSupportWPS;
            set => SetProperty(ref _isSupportWPS, value);
        }

        /// <summary>
        /// 是否启用 PPT 联动增强功能
        /// 基于智绘教 Inkeys 的 PPT 演示助手 3 技术方案
        /// 提供增强的 COM 兼容性，支持 COM 注册损坏的环境
        /// </summary>
        [JsonProperty("enablePPTEnhancedSupport")]
        public bool IsEnablePPTEnhancedSupport
        {
            get => _isEnablePPTEnhancedSupport;
            set => SetProperty(ref _isEnablePPTEnhancedSupport, value);
        }

        /// <summary>
        /// 注册表：显示幻灯片放映工具栏
        /// </summary>
        [JsonProperty("registryShowSlideShowToolbar")]
        public bool RegistryShowSlideShowToolbar
        {
            get => _registryShowSlideShowToolbar;
            set => SetProperty(ref _registryShowSlideShowToolbar, value);
        }

        /// <summary>
        /// 注册表：最后一张幻灯片显示黑屏
        /// </summary>
        [JsonProperty("registryShowBlackScreenLastSlideShow")]
        public bool RegistryShowBlackScreenLastSlideShow
        {
            get => _registryShowBlackScreenLastSlideShow;
            set => SetProperty(ref _registryShowBlackScreenLastSlideShow, value);
        }
    }
}
