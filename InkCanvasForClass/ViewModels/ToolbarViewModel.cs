using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ink_Canvas.Core;
using Ink_Canvas.Services;
using System;
using System.Windows.Media;

namespace Ink_Canvas.ViewModels
{
    /// <summary>
    /// 笔类型枚举
    /// </summary>
    public enum PenType
    {
        /// <summary>
        /// 签字笔
        /// </summary>
        SignPen = 0,

        /// <summary>
        /// 荧光笔
        /// </summary>
        Highlighter = 1
    }

    /// <summary>
    /// 预设颜色枚举
    /// </summary>
    public enum InkColor
    {
        Red = 0,
        Orange = 1,
        Yellow = 2,
        LightGreen = 3,
        Cyan = 4,
        Blue = 5,
        Purple = 6,
        Pink = 7,
        Black = 8,
        White = 9
    }

    /// <summary>
    /// 工具栏 ViewModel - 管理工具栏状态和画笔设置
    /// </summary>
    public partial class ToolbarViewModel : ViewModelBase
    {
        private readonly ISettingsService _settingsService;

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="settingsService">设置服务</param>
        public ToolbarViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        }

        #endregion

        #region 当前画笔设置

        /// <summary>
        /// 当前笔类型
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsSignPen))]
        [NotifyPropertyChangedFor(nameof(IsHighlighter))]
        private PenType _currentPenType = PenType.SignPen;

        /// <summary>
        /// 是否为签字笔
        /// </summary>
        public bool IsSignPen => CurrentPenType == PenType.SignPen;

        /// <summary>
        /// 是否为荧光笔
        /// </summary>
        public bool IsHighlighter => CurrentPenType == PenType.Highlighter;

        /// <summary>
        /// 当前墨迹颜色
        /// </summary>
        [ObservableProperty]
        private InkColor _currentInkColor = InkColor.Red;

        /// <summary>
        /// 当前笔宽度
        /// </summary>
        [ObservableProperty]
        private double _penWidth = 2.5;

        /// <summary>
        /// 当前荧光笔宽度
        /// </summary>
        [ObservableProperty]
        private double _highlighterWidth = 20;

        /// <summary>
        /// 当前有效宽度（根据笔类型）
        /// </summary>
        public double CurrentStrokeWidth => IsSignPen ? PenWidth : HighlighterWidth;

        #endregion

        #region 橡皮擦设置

        /// <summary>
        /// 橡皮擦大小索引 (0-4)
        /// </summary>
        [ObservableProperty]
        private int _eraserSizeIndex = 1;

        /// <summary>
        /// 橡皮擦实际宽度
        /// </summary>
        public double EraserWidth
        {
            get
            {
                return EraserSizeIndex switch
                {
                    0 => 24,
                    1 => 38,
                    2 => 46,
                    3 => 62,
                    4 => 78,
                    _ => 38
                };
            }
        }

        /// <summary>
        /// 是否为圆形橡皮擦
        /// </summary>
        [ObservableProperty]
        private bool _isCircleEraser = true;

        #endregion

        #region 颜色转换

        /// <summary>
        /// 获取颜色的 Color 值
        /// </summary>
        public static Color GetColorFromInkColor(InkColor inkColor)
        {
            return inkColor switch
            {
                InkColor.Red => Color.FromRgb(220, 38, 38),
                InkColor.Orange => Color.FromRgb(234, 88, 12),
                InkColor.Yellow => Color.FromRgb(250, 204, 21),
                InkColor.LightGreen => Color.FromRgb(34, 197, 94),
                InkColor.Cyan => Color.FromRgb(6, 182, 212),
                InkColor.Blue => Color.FromRgb(59, 130, 246),
                InkColor.Purple => Color.FromRgb(147, 51, 234),
                InkColor.Pink => Color.FromRgb(236, 72, 153),
                InkColor.Black => Color.FromRgb(0, 0, 0),
                InkColor.White => Color.FromRgb(255, 255, 255),
                _ => Color.FromRgb(220, 38, 38)
            };
        }

        /// <summary>
        /// 当前颜色 Color 值
        /// </summary>
        public Color CurrentColor => GetColorFromInkColor(CurrentInkColor);

        /// <summary>
        /// 当前颜色画刷
        /// </summary>
        public SolidColorBrush CurrentColorBrush => new SolidColorBrush(CurrentColor);

        #endregion

        #region 笔类型切换命令

        /// <summary>
        /// 切换到签字笔
        /// </summary>
        [RelayCommand]
        private void SwitchToSignPen()
        {
            CurrentPenType = PenType.SignPen;
            OnPropertyChanged(nameof(CurrentStrokeWidth));
        }

        /// <summary>
        /// 切换到荧光笔
        /// </summary>
        [RelayCommand]
        private void SwitchToHighlighter()
        {
            CurrentPenType = PenType.Highlighter;
            OnPropertyChanged(nameof(CurrentStrokeWidth));
        }

        /// <summary>
        /// 切换笔类型
        /// </summary>
        [RelayCommand]
        private void TogglePenType()
        {
            if (CurrentPenType == PenType.SignPen)
            {
                SwitchToHighlighter();
            }
            else
            {
                SwitchToSignPen();
            }
        }

        #endregion

        #region 颜色切换命令

        /// <summary>
        /// 设置颜色
        /// </summary>
        [RelayCommand]
        private void SetColor(InkColor color)
        {
            CurrentInkColor = color;
            OnPropertyChanged(nameof(CurrentColor));
            OnPropertyChanged(nameof(CurrentColorBrush));
        }

        /// <summary>
        /// 选择红色
        /// </summary>
        [RelayCommand]
        private void SelectRed() => SetColor(InkColor.Red);

        /// <summary>
        /// 选择橙色
        /// </summary>
        [RelayCommand]
        private void SelectOrange() => SetColor(InkColor.Orange);

        /// <summary>
        /// 选择黄色
        /// </summary>
        [RelayCommand]
        private void SelectYellow() => SetColor(InkColor.Yellow);

        /// <summary>
        /// 选择绿色
        /// </summary>
        [RelayCommand]
        private void SelectGreen() => SetColor(InkColor.LightGreen);

        /// <summary>
        /// 选择青色
        /// </summary>
        [RelayCommand]
        private void SelectCyan() => SetColor(InkColor.Cyan);

        /// <summary>
        /// 选择蓝色
        /// </summary>
        [RelayCommand]
        private void SelectBlue() => SetColor(InkColor.Blue);

        /// <summary>
        /// 选择紫色
        /// </summary>
        [RelayCommand]
        private void SelectPurple() => SetColor(InkColor.Purple);

        /// <summary>
        /// 选择粉色
        /// </summary>
        [RelayCommand]
        private void SelectPink() => SetColor(InkColor.Pink);

        /// <summary>
        /// 选择黑色
        /// </summary>
        [RelayCommand]
        private void SelectBlack() => SetColor(InkColor.Black);

        /// <summary>
        /// 选择白色
        /// </summary>
        [RelayCommand]
        private void SelectWhite() => SetColor(InkColor.White);

        #endregion

        #region 笔宽度命令

        /// <summary>
        /// 增加笔宽度
        /// </summary>
        [RelayCommand]
        private void IncreasePenWidth()
        {
            if (IsSignPen)
            {
                PenWidth = Math.Min(PenWidth + 0.5, 10);
            }
            else
            {
                HighlighterWidth = Math.Min(HighlighterWidth + 2, 40);
            }
            OnPropertyChanged(nameof(CurrentStrokeWidth));
        }

        /// <summary>
        /// 减小笔宽度
        /// </summary>
        [RelayCommand]
        private void DecreasePenWidth()
        {
            if (IsSignPen)
            {
                PenWidth = Math.Max(PenWidth - 0.5, 1);
            }
            else
            {
                HighlighterWidth = Math.Max(HighlighterWidth - 2, 10);
            }
            OnPropertyChanged(nameof(CurrentStrokeWidth));
        }

        /// <summary>
        /// 设置笔宽度
        /// </summary>
        [RelayCommand]
        private void SetPenWidth(double width)
        {
            if (IsSignPen)
            {
                PenWidth = Math.Clamp(width, 1, 10);
            }
            else
            {
                HighlighterWidth = Math.Clamp(width, 10, 40);
            }
            OnPropertyChanged(nameof(CurrentStrokeWidth));
        }

        #endregion

        #region 橡皮擦命令

        /// <summary>
        /// 设置橡皮擦大小
        /// </summary>
        [RelayCommand]
        private void SetEraserSize(int sizeIndex)
        {
            EraserSizeIndex = Math.Clamp(sizeIndex, 0, 4);
            OnPropertyChanged(nameof(EraserWidth));
        }

        /// <summary>
        /// 切换橡皮擦形状
        /// </summary>
        [RelayCommand]
        private void ToggleEraserShape()
        {
            IsCircleEraser = !IsCircleEraser;
        }

        #endregion
    }
}