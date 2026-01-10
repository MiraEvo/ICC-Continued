using System.Windows.Media;

namespace Ink_Canvas.Core
{
    /// <summary>
    /// 应用程序常量定义
    /// 集中管理所有魔法数字，提高代码可维护性
    /// </summary>
    public static class Constants
    {
        #region UI Dimensions

        /// <summary>
        /// 浮动工具栏宽度（像素）
        /// </summary>
        public const double FloatingBarWidth = 284;

        /// <summary>
        /// 浮动工具栏底部边距（像素）- PPT 演示模式
        /// </summary>
        public const int FloatingBarBottomMarginPPT = 60;

        /// <summary>
        /// 浮动工具栏底部边距（像素）- 普通模式
        /// </summary>
        public const int FloatingBarBottomMarginNormal = 100;

        /// <summary>
        /// 倒计时窗口紧凑模式宽度（像素）
        /// </summary>
        public const double CountdownCompactWidth = 400;

        /// <summary>
        /// 倒计时窗口紧凑模式高度（像素）
        /// </summary>
        public const double CountdownCompactHeight = 250;

        /// <summary>
        /// 倒计时窗口展开模式宽度（像素）
        /// </summary>
        public const double CountdownExpandedWidth = 1100;

        /// <summary>
        /// 倒计时窗口展开模式高度（像素）
        /// </summary>
        public const double CountdownExpandedHeight = 700;

        #endregion

        #region Time and Delays

        /// <summary>
        /// 短延迟时间（毫秒）- 用于 UI 更新等短暂延迟
        /// </summary>
        public const int ShortDelayMilliseconds = 100;

        /// <summary>
        /// 设置视图滚动动画持续时间（毫秒）
        /// </summary>
        public const int SettingsScrollAnimationDuration = 155;

        /// <summary>
        /// 随机窗口自动关闭等待时间（毫秒）
        /// </summary>
        public const int RandWindowAutoCloseDelay = 2500;

        /// <summary>
        /// 随机窗口等待次数
        /// </summary>
        public const int RandWindowWaitingTimes = 100;

        /// <summary>
        /// 随机窗口线程休眠时间（毫秒）
        /// </summary>
        public const int RandWindowThreadSleepTime = 5;

        /// <summary>
        /// 倒计时定时器间隔 - 短时间（毫秒）
        /// </summary>
        public const int CountdownTimerIntervalShort = 30;

        /// <summary>
        /// 倒计时定时器间隔 - 中等时间（毫秒）
        /// </summary>
        public const int CountdownTimerIntervalMedium = 50;

        /// <summary>
        /// 倒计时定时器间隔 - 长时间（毫秒）
        /// </summary>
        public const int CountdownTimerIntervalLong = 100;

        /// <summary>
        /// 倒计时 2 分钟阈值（秒）
        /// </summary>
        public const int CountdownTwoMinutesThreshold = 120;

        /// <summary>
        /// 秒转毫秒乘数
        /// </summary>
        public const int MillisecondsPerSecond = 1000;

        #endregion

        #region Mouse and Input

        /// <summary>
        /// 鼠标滚轮增量标准值
        /// 系统标准鼠标滚轮增量为 120
        /// </summary>
        public const double MouseWheelDeltaStandard = 120;

        /// <summary>
        /// 鼠标滚轮滚动速度乘数
        /// </summary>
        public const double MouseWheelScrollMultiplier = 10;

        /// <summary>
        /// 设置视图滚动速度乘数
        /// </summary>
        public const double SettingsScrollMultiplier = 2.5;

        #endregion

        #region Angles and Geometry

        /// <summary>
        /// 完整圆周角度（度）
        /// </summary>
        public const double FullCircleDegrees = 360;

        /// <summary>
        /// 直角角度（度）
        /// </summary>
        public const double RightAngleDegrees = 90;

        /// <summary>
        /// 平角角度（度）
        /// </summary>
        public const double StraightAngleDegrees = 180;

        /// <summary>
        /// 270度角（度）
        /// </summary>
        public const double ThreeQuarterCircleDegrees = 270;

        /// <summary>
        /// 弧度转换系数（π/180）
        /// </summary>
        public const double DegreesToRadians = System.Math.PI / 180;

        #endregion

        #region Colors

        /// <summary>
        /// 预定义墨迹颜色 - 红色
        /// </summary>
        public static readonly Color InkColorRed = Color.FromRgb(220, 38, 38);

        /// <summary>
        /// 预定义墨迹颜色 - 橙色
        /// </summary>
        public static readonly Color InkColorOrange = Color.FromRgb(234, 88, 12);

        /// <summary>
        /// 预定义墨迹颜色 - 黄色
        /// </summary>
        public static readonly Color InkColorYellow = Color.FromRgb(250, 204, 21);

        /// <summary>
        /// 预定义墨迹颜色 - 浅绿色
        /// </summary>
        public static readonly Color InkColorLightGreen = Color.FromRgb(34, 197, 94);

        /// <summary>
        /// 预定义墨迹颜色 - 青色
        /// </summary>
        public static readonly Color InkColorCyan = Color.FromRgb(6, 182, 212);

        /// <summary>
        /// 预定义墨迹颜色 - 蓝色
        /// </summary>
        public static readonly Color InkColorBlue = Color.FromRgb(59, 130, 246);

        /// <summary>
        /// 预定义墨迹颜色 - 紫色
        /// </summary>
        public static readonly Color InkColorPurple = Color.FromRgb(147, 51, 234);

        /// <summary>
        /// 预定义墨迹颜色 - 粉色
        /// </summary>
        public static readonly Color InkColorPink = Color.FromRgb(236, 72, 153);

        /// <summary>
        /// 预定义墨迹颜色 - 白色
        /// </summary>
        public static readonly Color InkColorWhite = Color.FromRgb(255, 255, 255);

        #endregion

        #region Limits and Thresholds

        /// <summary>
        /// 随机窗口一次最多选择人数
        /// </summary>
        public const int RandWindowMaxPeopleOneTime = 10;

        /// <summary>
        /// 倒计时小时最大值
        /// </summary>
        public const int CountdownMaxHours = 100;

        /// <summary>
        /// 倒计时小时增量（快速调整）
        /// </summary>
        public const int CountdownHourIncrement = 5;

        /// <summary>
        /// 日志文件大小阈值（KB）
        /// 超过此大小将自动删除日志文件
        /// </summary>
        public const long LogFileSizeThresholdKB = 512;

        /// <summary>
        /// 字节转 KB 除数
        /// </summary>
        public const long BytesToKilobytes = 1024;

        #endregion

        #region UI Margins and Offsets

        /// <summary>
        /// 浮动工具栏隐藏时的水平偏移量（像素）
        /// </summary>
        public const double FloatingBarHiddenHorizontalOffset = -2000;

        /// <summary>
        /// 浮动工具栏隐藏时的垂直偏移量（像素）
        /// </summary>
        public const double FloatingBarHiddenVerticalOffset = -200;

        /// <summary>
        /// 侧边面板折叠时的边距（像素）
        /// </summary>
        public const int SidePanelCollapsedMargin = -50;

        /// <summary>
        /// 侧边面板底部边距（像素）
        /// </summary>
        public const double SidePanelBottomMargin = -150;

        /// <summary>
        /// 展开按钮初始边距（像素）
        /// </summary>
        public const double UnfoldButtonInitialMargin = -1;

        #endregion

        #region Animation Durations

        /// <summary>
        /// 侧边面板动画持续时间（秒）
        /// </summary>
        public const double SidePanelAnimationDuration = 0.175;

        /// <summary>
        /// 展开按钮动画持续时间（秒）
        /// </summary>
        public const double UnfoldButtonAnimationDuration = 0.1;

        /// <summary>
        /// 主题切换延迟时间（毫秒）
        /// </summary>
        public const int ThemeSwitchDelayMilliseconds = 200;

        /// <summary>
        /// 侧边面板动画完成等待时间（毫秒）
        /// </summary>
        public const int SidePanelAnimationCompleteDelay = 600;

        /// <summary>
        /// Toast 通知默认显示时间（毫秒）
        /// </summary>
        public const int ToastDefaultDisplayDuration = 3000;

        /// <summary>
        /// DPI 变化延迟操作时间（毫秒）
        /// </summary>
        public const int DpiChangeDelayMilliseconds = 3000;

        #endregion

        #region Color Values

        /// <summary>
        /// 半透明 Alpha 值
        /// </summary>
        public const byte AlphaSemiTransparent = 127;

        /// <summary>
        /// 完全不透明 Alpha 值
        /// </summary>
        public const byte AlphaOpaque = 255;

        /// <summary>
        /// 荧光笔颜色代码
        /// </summary>
        public const int HighlighterColorCode = 102;

        /// <summary>
        /// 水印文字颜色 - 浅色（RGB）
        /// </summary>
        public static readonly Color WatermarkLightColor = Color.FromRgb(234, 235, 237);

        /// <summary>
        /// 按钮禁用状态颜色（ARGB）
        /// </summary>
        public static readonly Color ButtonDisabledColor = Color.FromArgb(127, 24, 24, 27);

        /// <summary>
        /// 墨迹颜色 - 红色（亮色主题）
        /// </summary>
        public static readonly Color InkColorRedLight = Color.FromRgb(239, 68, 68);

        /// <summary>
        /// 墨迹颜色 - 绿色（亮色主题）
        /// </summary>
        public static readonly Color InkColorGreenLight = Color.FromRgb(34, 197, 94);

        /// <summary>
        /// 墨迹颜色 - 蓝色（亮色主题）
        /// </summary>
        public static readonly Color InkColorBlueLight = Color.FromRgb(59, 130, 246);

        /// <summary>
        /// 墨迹颜色 - 黄色（亮色主题）
        /// </summary>
        public static readonly Color InkColorYellowLight = Color.FromRgb(250, 204, 21);

        /// <summary>
        /// 墨迹颜色 - 粉色（亮色主题）
        /// </summary>
        public static readonly Color InkColorPinkLight = Color.FromRgb(236, 72, 153);

        /// <summary>
        /// 墨迹颜色 - 青色（亮色主题）
        /// </summary>
        public static readonly Color InkColorTealLight = Color.FromRgb(20, 184, 166);

        /// <summary>
        /// 墨迹颜色 - 橙色（亮色主题）
        /// </summary>
        public static readonly Color InkColorOrangeLight = Color.FromRgb(249, 115, 22);

        #endregion

        #region Opacity Values

        /// <summary>
        /// 按钮禁用状态不透明度
        /// </summary>
        public const double ButtonDisabledOpacity = 0.5;

        #endregion

        #region Stroke Thresholds

        /// <summary>
        /// 墨迹数量阈值 - 用于判断是否需要清空提示
        /// </summary>
        public const int StrokeCountThreshold = 2;

        #endregion

        #region Shape Drawing

        /// <summary>
        /// 网格辅助线默认大小（像素）
        /// </summary>
        public const double GridDefaultSize = 20;

        /// <summary>
        /// 顶点吸附默认距离（像素）
        /// </summary>
        public const double SnapDefaultDistance = 15;

        /// <summary>
        /// 网格线透明度
        /// </summary>
        public const byte GridLineAlpha = 40;

        /// <summary>
        /// 网格线颜色（灰色）
        /// </summary>
        public static readonly Color GridLineColor = Color.FromArgb(GridLineAlpha, 100, 100, 100);

        #endregion
    }
}
