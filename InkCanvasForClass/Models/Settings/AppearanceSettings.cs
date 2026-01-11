using Newtonsoft.Json;

namespace Ink_Canvas.Models.Settings
{
    /// <summary>
    /// 外观设置类
    /// </summary>
    public class AppearanceSettings : SettingsBase
    {
        private bool _isEnableDisPlayNibModeToggler = true;
        private bool _isColorfulViewboxFloatingBar = false;
        private double _viewboxFloatingBarScaleTransformValue = 1.0;
        private int _floatingBarImg = 0;
        private double _viewboxFloatingBarOpacityValue = 1.0;
        private bool _enableTrayIcon = true;
        private double _viewboxFloatingBarOpacityInPPTValue = 0.5;
        private bool _enableViewboxBlackBoardScaleTransform = false;
        private bool _isTransparentButtonBackground = true;
        private bool _isShowExitButton = true;
        private bool _isShowEraserButton = true;
        private bool _enableTimeDisplayInWhiteboardMode = true;
        private bool _enableChickenSoupInWhiteboardMode = true;
        private bool _isShowHideControlButton = false;
        private int _unFoldButtonImageType = 0;
        private bool _isShowLRSwitchButton = false;
        private bool _isShowQuickPanel = true;
        private int _chickenSoupSource = 1;
        private bool _isShowModeFingerToggleSwitch = true;
        private int _theme = 0;

        /// <summary>
        /// 是否显示笔模式切换器
        /// </summary>
        [JsonProperty("isEnableDisPlayNibModeToggler")]
        public bool IsEnableDisPlayNibModeToggler
        {
            get => _isEnableDisPlayNibModeToggler;
            set => SetProperty(ref _isEnableDisPlayNibModeToggler, value);
        }

        /// <summary>
        /// 是否使用彩色浮动工具栏
        /// </summary>
        [JsonProperty("isColorfulViewboxFloatingBar")]
        public bool IsColorfulViewboxFloatingBar
        {
            get => _isColorfulViewboxFloatingBar;
            set => SetProperty(ref _isColorfulViewboxFloatingBar, value);
        }

        /// <summary>
        /// 浮动工具栏缩放值
        /// </summary>
        [JsonProperty("viewboxFloatingBarScaleTransformValue")]
        public double ViewboxFloatingBarScaleTransformValue
        {
            get => _viewboxFloatingBarScaleTransformValue;
            set => SetProperty(ref _viewboxFloatingBarScaleTransformValue, value);
        }

        /// <summary>
        /// 浮动工具栏图片
        /// </summary>
        [JsonProperty("floatingBarImg")]
        public int FloatingBarImg
        {
            get => _floatingBarImg;
            set => SetProperty(ref _floatingBarImg, value);
        }

        /// <summary>
        /// 浮动工具栏透明度
        /// </summary>
        [JsonProperty("viewboxFloatingBarOpacityValue")]
        public double ViewboxFloatingBarOpacityValue
        {
            get => _viewboxFloatingBarOpacityValue;
            set => SetProperty(ref _viewboxFloatingBarOpacityValue, value);
        }

        /// <summary>
        /// 是否启用托盘图标
        /// </summary>
        [JsonProperty("enableTrayIcon")]
        public bool EnableTrayIcon
        {
            get => _enableTrayIcon;
            set => SetProperty(ref _enableTrayIcon, value);
        }

        /// <summary>
        /// PPT 模式下浮动工具栏透明度
        /// </summary>
        [JsonProperty("viewboxFloatingBarOpacityInPPTValue")]
        public double ViewboxFloatingBarOpacityInPPTValue
        {
            get => _viewboxFloatingBarOpacityInPPTValue;
            set => SetProperty(ref _viewboxFloatingBarOpacityInPPTValue, value);
        }

        /// <summary>
        /// 是否启用黑板缩放变换
        /// </summary>
        [JsonProperty("enableViewboxBlackBoardScaleTransform")]
        public bool EnableViewboxBlackBoardScaleTransform
        {
            get => _enableViewboxBlackBoardScaleTransform;
            set => SetProperty(ref _enableViewboxBlackBoardScaleTransform, value);
        }

        /// <summary>
        /// 是否使用透明按钮背景
        /// </summary>
        [JsonProperty("isTransparentButtonBackground")]
        public bool IsTransparentButtonBackground
        {
            get => _isTransparentButtonBackground;
            set => SetProperty(ref _isTransparentButtonBackground, value);
        }

        /// <summary>
        /// 是否显示退出按钮
        /// </summary>
        [JsonProperty("isShowExitButton")]
        public bool IsShowExitButton
        {
            get => _isShowExitButton;
            set => SetProperty(ref _isShowExitButton, value);
        }

        /// <summary>
        /// 是否显示橡皮擦按钮
        /// </summary>
        [JsonProperty("isShowEraserButton")]
        public bool IsShowEraserButton
        {
            get => _isShowEraserButton;
            set => SetProperty(ref _isShowEraserButton, value);
        }

        /// <summary>
        /// 白板模式下是否显示时间
        /// </summary>
        [JsonProperty("enableTimeDisplayInWhiteboardMode")]
        public bool EnableTimeDisplayInWhiteboardMode
        {
            get => _enableTimeDisplayInWhiteboardMode;
            set => SetProperty(ref _enableTimeDisplayInWhiteboardMode, value);
        }

        /// <summary>
        /// 白板模式下是否显示鸡汤
        /// </summary>
        [JsonProperty("enableChickenSoupInWhiteboardMode")]
        public bool EnableChickenSoupInWhiteboardMode
        {
            get => _enableChickenSoupInWhiteboardMode;
            set => SetProperty(ref _enableChickenSoupInWhiteboardMode, value);
        }

        /// <summary>
        /// 是否显示隐藏控制按钮
        /// </summary>
        [JsonProperty("isShowHideControlButton")]
        public bool IsShowHideControlButton
        {
            get => _isShowHideControlButton;
            set => SetProperty(ref _isShowHideControlButton, value);
        }

        /// <summary>
        /// 展开按钮图片类型
        /// </summary>
        [JsonProperty("unFoldButtonImageType")]
        public int UnFoldButtonImageType
        {
            get => _unFoldButtonImageType;
            set => SetProperty(ref _unFoldButtonImageType, value);
        }

        /// <summary>
        /// 是否显示左右切换按钮
        /// </summary>
        [JsonProperty("isShowLRSwitchButton")]
        public bool IsShowLRSwitchButton
        {
            get => _isShowLRSwitchButton;
            set => SetProperty(ref _isShowLRSwitchButton, value);
        }

        /// <summary>
        /// 是否显示快捷面板
        /// </summary>
        [JsonProperty("isShowQuickPanel")]
        public bool IsShowQuickPanel
        {
            get => _isShowQuickPanel;
            set => SetProperty(ref _isShowQuickPanel, value);
        }

        /// <summary>
        /// 鸡汤来源
        /// </summary>
        [JsonProperty("chickenSoupSource")]
        public int ChickenSoupSource
        {
            get => _chickenSoupSource;
            set => SetProperty(ref _chickenSoupSource, value);
        }

        /// <summary>
        /// 是否显示手指模式切换开关
        /// </summary>
        [JsonProperty("isShowModeFingerToggleSwitch")]
        public bool IsShowModeFingerToggleSwitch
        {
            get => _isShowModeFingerToggleSwitch;
            set => SetProperty(ref _isShowModeFingerToggleSwitch, value);
        }

        /// <summary>
        /// 主题
        /// </summary>
        [JsonProperty("theme")]
        public int Theme
        {
            get => _theme;
            set => SetProperty(ref _theme, value);
        }

        /// <summary>
        /// 浮动工具栏按钮标签可见性
        /// </summary>
        [JsonProperty("floatingBarButtonLabelVisibility")]
        public bool FloatingBarButtonLabelVisibility { get; set; } = true;

        /// <summary>
        /// 浮动工具栏图标可见性
        /// </summary>
        [JsonProperty("floatingBarIconsVisibility")]
        public string FloatingBarIconsVisibility { get; set; } = "1111111111";

        /// <summary>
        /// 橡皮擦按钮可见性
        /// </summary>
        [JsonProperty("eraserButtonsVisibility")]
        public int EraserButtonsVisibility { get; set; } = 0;

        /// <summary>
        /// 是否仅显示橡皮擦按钮
        /// </summary>
        [JsonProperty("onlyDisplayEraserBtn")]
        public bool OnlyDisplayEraserBtn { get; set; } = false;
    }
}
