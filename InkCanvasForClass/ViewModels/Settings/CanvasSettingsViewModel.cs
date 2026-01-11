using CommunityToolkit.Mvvm.ComponentModel;
using System;
using Ink_Canvas.Models.Settings;

namespace Ink_Canvas.ViewModels
{
    /// <summary>
    /// Canvas 设置 ViewModel
    /// </summary>
    public partial class CanvasSettingsViewModel : ObservableObject
    {
        private readonly CanvasSettings _canvas;
        private readonly Action _saveAction;

        public CanvasSettingsViewModel(CanvasSettings canvas, Action saveAction)
        {
            _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            _saveAction = saveAction;
        }

        /// <summary>
        /// 墨迹宽度
        /// </summary>
        public double InkWidth
        {
            get => _canvas.InkWidth;
            set
            {
                if (SetProperty(_canvas.InkWidth, value, _canvas, (c, v) => c.InkWidth = v))
                    _saveAction?.Invoke();
            }
        }

        /// <summary>
        /// 荧光笔宽度
        /// </summary>
        public double HighlighterWidth
        {
            get => _canvas.HighlighterWidth;
            set
            {
                if (SetProperty(_canvas.HighlighterWidth, value, _canvas, (c, v) => c.HighlighterWidth = v))
                    _saveAction?.Invoke();
            }
        }

        /// <summary>
        /// 墨迹透明度
        /// </summary>
        public double InkAlpha
        {
            get => _canvas.InkAlpha;
            set
            {
                if (SetProperty(_canvas.InkAlpha, value, _canvas, (c, v) => c.InkAlpha = v))
                    _saveAction?.Invoke();
            }
        }

        /// <summary>
        /// 是否显示光标
        /// </summary>
        public bool IsShowCursor
        {
            get => _canvas.IsShowCursor;
            set
            {
                if (SetProperty(_canvas.IsShowCursor, value, _canvas, (c, v) => c.IsShowCursor = v))
                    _saveAction?.Invoke();
            }
        }

        /// <summary>
        /// 墨迹样式
        /// </summary>
        public int InkStyle
        {
            get => _canvas.InkStyle;
            set
            {
                if (SetProperty(_canvas.InkStyle, value, _canvas, (c, v) => c.InkStyle = v))
                    _saveAction?.Invoke();
            }
        }

        /// <summary>
        /// 橡皮擦大小
        /// </summary>
        public int EraserSize
        {
            get => _canvas.EraserSize;
            set
            {
                if (SetProperty(_canvas.EraserSize, value, _canvas, (c, v) => c.EraserSize = v))
                    _saveAction?.Invoke();
            }
        }

        /// <summary>
        /// 橡皮擦类型
        /// </summary>
        public int EraserType
        {
            get => _canvas.EraserType;
            set
            {
                if (SetProperty(_canvas.EraserType, value, _canvas, (c, v) => c.EraserType = v))
                    _saveAction?.Invoke();
            }
        }

        /// <summary>
        /// 橡皮擦形状类型
        /// </summary>
        public int EraserShapeType
        {
            get => _canvas.EraserShapeType;
            set
            {
                if (SetProperty(_canvas.EraserShapeType, value, _canvas, (c, v) => c.EraserShapeType = v))
                    _saveAction?.Invoke();
            }
        }

        /// <summary>
        /// 选择时隐藏笔画
        /// </summary>
        public bool HideStrokeWhenSelecting
        {
            get => _canvas.HideStrokeWhenSelecting;
            set
            {
                if (SetProperty(_canvas.HideStrokeWhenSelecting, value, _canvas, (c, v) => c.HideStrokeWhenSelecting = v))
                    _saveAction?.Invoke();
            }
        }

        /// <summary>
        /// 平滑曲线
        /// </summary>
        public bool FitToCurve
        {
            get => _canvas.FitToCurve;
            set
            {
                if (SetProperty(_canvas.FitToCurve, value, _canvas, (c, v) => c.FitToCurve = v))
                    _saveAction?.Invoke();
            }
        }

        /// <summary>
        /// 清除画布时同时清除时间机器
        /// </summary>
        public bool ClearCanvasAndClearTimeMachine
        {
            get => _canvas.ClearCanvasAndClearTimeMachine;
            set
            {
                if (SetProperty(_canvas.ClearCanvasAndClearTimeMachine, value, _canvas, (c, v) => c.ClearCanvasAndClearTimeMachine = v))
                    _saveAction?.Invoke();
            }
        }

        /// <summary>
        /// 黑板背景颜色
        /// </summary>
        public BlackboardBackgroundColorEnum BlackboardBackgroundColor
        {
            get => _canvas.BlackboardBackgroundColor;
            set
            {
                if (SetProperty(_canvas.BlackboardBackgroundColor, value, _canvas, (c, v) => c.BlackboardBackgroundColor = v))
                    _saveAction?.Invoke();
            }
        }

        /// <summary>
        /// 黑板背景图案
        /// </summary>
        public BlackboardBackgroundPatternEnum BlackboardBackgroundPattern
        {
            get => _canvas.BlackboardBackgroundPattern;
            set
            {
                if (SetProperty(_canvas.BlackboardBackgroundPattern, value, _canvas, (c, v) => c.BlackboardBackgroundPattern = v))
                    _saveAction?.Invoke();
            }
        }

        /// <summary>
        /// 双曲线渐近线选项
        /// </summary>
        public OptionalOperation HyperbolaAsymptoteOption
        {
            get => _canvas.HyperbolaAsymptoteOption;
            set
            {
                if (SetProperty(_canvas.HyperbolaAsymptoteOption, value, _canvas, (c, v) => c.HyperbolaAsymptoteOption = v))
                    _saveAction?.Invoke();
            }
        }

        /// <summary>
        /// 对每个新添加的黑板页面使用默认背景色
        /// </summary>
        public bool UseDefaultBackgroundColorForEveryNewAddedBlackboardPage
        {
            get => _canvas.UseDefaultBackgroundColorForEveryNewAddedBlackboardPage;
            set
            {
                if (SetProperty(_canvas.UseDefaultBackgroundColorForEveryNewAddedBlackboardPage, value, _canvas, (c, v) => c.UseDefaultBackgroundColorForEveryNewAddedBlackboardPage = v))
                    _saveAction?.Invoke();
            }
        }

        /// <summary>
        /// 对每个新添加的黑板页面使用默认背景图案
        /// </summary>
        public bool UseDefaultBackgroundPatternForEveryNewAddedBlackboardPage
        {
            get => _canvas.UseDefaultBackgroundPatternForEveryNewAddedBlackboardPage;
            set
            {
                if (SetProperty(_canvas.UseDefaultBackgroundPatternForEveryNewAddedBlackboardPage, value, _canvas, (c, v) => c.UseDefaultBackgroundPatternForEveryNewAddedBlackboardPage = v))
                    _saveAction?.Invoke();
            }
        }

        /// <summary>
        /// 背景变化时自动转换墨迹颜色
        /// </summary>
        public bool IsEnableAutoConvertInkColorWhenBackgroundChanged
        {
            get => _canvas.IsEnableAutoConvertInkColorWhenBackgroundChanged;
            set
            {
                if (SetProperty(_canvas.IsEnableAutoConvertInkColorWhenBackgroundChanged, value, _canvas, (c, v) => c.IsEnableAutoConvertInkColorWhenBackgroundChanged = v))
                    _saveAction?.Invoke();
            }
        }

        /// <summary>
        /// 对笔尖应用缩放
        /// </summary>
        public bool ApplyScaleToStylusTip
        {
            get => _canvas.ApplyScaleToStylusTip;
            set
            {
                if (SetProperty(_canvas.ApplyScaleToStylusTip, value, _canvas, (c, v) => c.ApplyScaleToStylusTip = v))
                    _saveAction?.Invoke();
            }
        }

        /// <summary>
        /// 仅命中测试完全包含的笔画
        /// </summary>
        public bool OnlyHitTestFullyContainedStrokes
        {
            get => _canvas.OnlyHitTestFullyContainedStrokes;
            set
            {
                if (SetProperty(_canvas.OnlyHitTestFullyContainedStrokes, value, _canvas, (c, v) => c.OnlyHitTestFullyContainedStrokes = v))
                    _saveAction?.Invoke();
            }
        }

        /// <summary>
        /// 允许点击选择锁定的笔画
        /// </summary>
        public bool AllowClickToSelectLockedStroke
        {
            get => _canvas.AllowClickToSelectLockedStroke;
            set
            {
                if (SetProperty(_canvas.AllowClickToSelectLockedStroke, value, _canvas, (c, v) => c.AllowClickToSelectLockedStroke = v))
                    _saveAction?.Invoke();
            }
        }

        /// <summary>
        /// 选择方法
        /// </summary>
        public int SelectionMethod
        {
            get => _canvas.SelectionMethod;
            set
            {
                if (SetProperty(_canvas.SelectionMethod, value, _canvas, (c, v) => c.SelectionMethod = v))
                    _saveAction?.Invoke();
            }
        }

        /// <summary>
        /// 最后使用的笔类型
        /// </summary>
        public int LastPenType
        {
            get => _canvas.LastPenType;
            set
            {
                if (SetProperty(_canvas.LastPenType, value, _canvas, (c, v) => c.LastPenType = v))
                    _saveAction?.Invoke();
            }
        }

        /// <summary>
        /// 桌面模式最后墨迹颜色
        /// </summary>
        public int LastDesktopInkColor
        {
            get => _canvas.LastDesktopInkColor;
            set
            {
                if (SetProperty(_canvas.LastDesktopInkColor, value, _canvas, (c, v) => c.LastDesktopInkColor = v))
                    _saveAction?.Invoke();
            }
        }

        /// <summary>
        /// 白板模式最后墨迹颜色
        /// </summary>
        public int LastBoardInkColor
        {
            get => _canvas.LastBoardInkColor;
            set
            {
                if (SetProperty(_canvas.LastBoardInkColor, value, _canvas, (c, v) => c.LastBoardInkColor = v))
                    _saveAction?.Invoke();
            }
        }

        /// <summary>
        /// 荧光笔最后颜色
        /// </summary>
        public int LastHighlighterColor
        {
            get => _canvas.LastHighlighterColor;
            set
            {
                if (SetProperty(_canvas.LastHighlighterColor, value, _canvas, (c, v) => c.LastHighlighterColor = v))
                    _saveAction?.Invoke();
            }
        }
    }
}