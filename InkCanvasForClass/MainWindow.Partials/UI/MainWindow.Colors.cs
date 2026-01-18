// ============================================================================
// MW_Colors.cs - 颜色选择和画笔调色盘
// ============================================================================
//
// 功能说明:
//   - 画笔颜色选择
//   - 调色盘 UI 交互
//   - 颜色主题切换（深色/浅色模式）
//
// 迁移状态:
//   - 颜色选择逻辑与 FloatingBarViewModel 协同工作
//   - 调色盘 UI 仍在 MainWindow.xaml 中
//
// ============================================================================

using Ink_Canvas.Helpers;
using Ink_Canvas.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

using Ink_Canvas.Popups;

namespace Ink_Canvas {
    public partial class MainWindow : Window {
        private int inkColor = 1;

        private void ColorSwitchCheck() {
            //HideSubPanels("color");
            CheckColorTheme();

            if (GridTransparencyFakeBackground.Background == Brushes.Transparent) {
                if (currentMode == 1) {
                    currentMode = 0;
                    GridBackgroundCover.Visibility = Visibility.Collapsed;
                    AnimationsHelper.HideWithSlideAndFade(BlackboardLeftSide);
                    AnimationsHelper.HideWithSlideAndFade(BlackboardCenterSide);
                    AnimationsHelper.HideWithSlideAndFade(BlackboardRightSide);
                }

                BtnHideInkCanvas_Click(null, null);
            }

            var strokes = inkCanvas.GetSelectedStrokes();
            if (strokes.Count != 0) {
                foreach (var stroke in strokes)
                    try {
                        stroke.DrawingAttributes.Color = inkCanvas.DefaultDrawingAttributes.Color;
                    }
                    catch {
                        // ignored
                    }
            }
            if (DrawingAttributesHistory.Count > 0)
            {
                timeMachine.CommitStrokeDrawingAttributesHistory(DrawingAttributesHistory);
                DrawingAttributesHistory = new Dictionary<Stroke, Tuple<DrawingAttributes, DrawingAttributes>>();
                foreach (var item in DrawingAttributesHistoryFlag)
                {
                    item.Value.Clear();
                }
            }
            else {
                inkCanvas.IsManipulationEnabled = true;
                drawingShapeMode = 0;
                inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
                CancelSingleFingerDragMode();
                forceEraser = false;
            }

            isLongPressSelected = false;
        }

        private bool isUselightThemeColor = false, isDesktopUselightThemeColor = false;
        private int penType = 0; // 0是签字笔，1是荧光笔
        private int lastDesktopInkColor = 1, lastBoardInkColor = 5;
        private int highlighterColor = Constants.HighlighterColorCode;

        // 新增状态变量 - 用于保持笔类型和笔触粗细
        private int lastPenType = 0;  // 保存最后使用的笔类型 (0=签字笔, 1=荧光笔)
        private double lastPenWidth = 2.5;  // 保存最后使用的签字笔粗细
        private double lastHighlighterWidth = 20;  // 保存最后使用的荧光笔粗细

        // 笔触粗细预设值数组
        private readonly double[] PenWidthPresets = { 1.5, 2.5, 3.5, 5.0 };  // 签字笔粗细预设：细、中、粗、特粗
        private readonly double[] HighlighterWidthPresets = { 16, 20, 28, 40 };  // 荧光笔粗细预设：细、中、粗、特粗

        // 颜色索引有效范围常量
        private const int MinInkColorIndex = 0;
        private const int MaxInkColorIndex = 8;
        private const int DefaultInkColorIndex = 1;  // 默认红色

        // 笔触粗细有效范围常量
        private const double MinPenWidth = 1.0;
        private const double MaxPenWidth = 20.0;
        private const double DefaultPenWidth = 2.0;
        private const double MinHighlighterWidth = 8.0;
        private const double MaxHighlighterWidth = 80.0;
        private const double DefaultHighlighterWidth = 20.0;

        /// <summary>
        /// 验证并修正颜色索引值，确保在有效范围内(0-8)
        /// Requirements: 3.1
        /// </summary>
        /// <param name="colorIndex">要验证的颜色索引</param>
        /// <returns>有效的颜色索引值</returns>
        private int ValidateAndCorrectColorIndex(int colorIndex) {
            if (colorIndex < MinInkColorIndex || colorIndex > MaxInkColorIndex) {
                LogHelper.WriteLogToFile(
                    $"颜色索引越界: {colorIndex}，有效范围为 {MinInkColorIndex}-{MaxInkColorIndex}，已重置为默认值 {DefaultInkColorIndex}",
                    LogHelper.LogType.Warning);
                return DefaultInkColorIndex;
            }
            return colorIndex;
        }

        /// <summary>
        /// 验证并修正笔触粗细值，确保为正数且在合理范围内
        /// Requirements: 1.2
        /// </summary>
        /// <param name="width">要验证的笔触粗细</param>
        /// <param name="isHighlighter">是否为荧光笔</param>
        /// <returns>有效的笔触粗细值</returns>
        private double ValidateAndCorrectPenWidth(double width, bool isHighlighter) {
            double minWidth = isHighlighter ? MinHighlighterWidth : MinPenWidth;
            double maxWidth = isHighlighter ? MaxHighlighterWidth : MaxPenWidth;
            double defaultWidth = isHighlighter ? DefaultHighlighterWidth : DefaultPenWidth;

            if (double.IsNaN(width) || double.IsInfinity(width) || width <= 0 || width < minWidth || width > maxWidth) {
                string penTypeName = isHighlighter ? "荧光笔" : "签字笔";
                LogHelper.WriteLogToFile(
                    $"{penTypeName}粗细值无效: {width}，有效范围为 {minWidth}-{maxWidth}，已重置为默认值 {defaultWidth}",
                    LogHelper.LogType.Warning);
                return defaultWidth;
            }
            return width;
        }

        private void CheckColorTheme(bool changeColorTheme = false) {
            if (changeColorTheme)
                if (currentMode != 0) {
                    var bgC = BoardPagesSettingsList[CurrentWhiteboardIndex - 1].BackgroundColor;
                    GridBackgroundCover.Background = new SolidColorBrush(BoardBackgroundColors[(int)bgC]);
                    if (bgC == BlackboardBackgroundColorEnum.BlackBoardGreen
                        || bgC == BlackboardBackgroundColorEnum.BlueBlack
                        || bgC == BlackboardBackgroundColorEnum.GrayBlack
                        || bgC == BlackboardBackgroundColorEnum.RealBlack) {
                        WaterMarkTime.Foreground = new SolidColorBrush(Constants.WatermarkLightColor);
                        WaterMarkDate.Foreground = new SolidColorBrush(Constants.WatermarkLightColor);
                        BlackBoardWaterMark.Foreground = new SolidColorBrush(Constants.WatermarkLightColor);
                        isUselightThemeColor = true;
                    } else {
                        WaterMarkTime.Foreground = new SolidColorBrush(Color.FromRgb(22, 22,22));
                        WaterMarkDate.Foreground = new SolidColorBrush(Color.FromRgb(22, 22, 22));
                        BlackBoardWaterMark.Foreground = new SolidColorBrush(Color.FromRgb(22, 22, 22));
                        isUselightThemeColor = false;
                    }
                }

            if (currentMode == 0) {
                isUselightThemeColor = isDesktopUselightThemeColor;
                inkColor = lastDesktopInkColor;
            }
            else {
                inkColor = lastBoardInkColor;
            }

            double alpha = inkCanvas.DefaultDrawingAttributes.Color.A;

            if (penType == 0) {
                if (inkColor == 0) {
                    // Black
                    inkCanvas.DefaultDrawingAttributes.Color = Color.FromArgb((byte)alpha, 0, 0, 0);
                }
                else if (inkColor == 5) {
                    // White
                    inkCanvas.DefaultDrawingAttributes.Color = Color.FromArgb((byte)alpha, Constants.AlphaOpaque, Constants.AlphaOpaque, Constants.AlphaOpaque);
                }
                else if (isUselightThemeColor) {
                    if (inkColor == 1)
                        // Red
                        inkCanvas.DefaultDrawingAttributes.Color = Color.FromArgb((byte)alpha, Constants.InkColorRedLight.R, Constants.InkColorRedLight.G, Constants.InkColorRedLight.B);
                    else if (inkColor == 2)
                        // Green
                        inkCanvas.DefaultDrawingAttributes.Color = Color.FromArgb((byte)alpha, Constants.InkColorGreenLight.R, Constants.InkColorGreenLight.G, Constants.InkColorGreenLight.B);
                    else if (inkColor == 3)
                        // Blue
                        inkCanvas.DefaultDrawingAttributes.Color = Color.FromArgb((byte)alpha, Constants.InkColorBlueLight.R, Constants.InkColorBlueLight.G, Constants.InkColorBlueLight.B);
                    else if (inkColor == 4)
                        // Yellow
                        inkCanvas.DefaultDrawingAttributes.Color = Color.FromArgb((byte)alpha, Constants.InkColorYellowLight.R, Constants.InkColorYellowLight.G, Constants.InkColorYellowLight.B);
                    else if (inkColor == 6)
                        // Pink
                        inkCanvas.DefaultDrawingAttributes.Color = Color.FromArgb((byte)alpha, Constants.InkColorPinkLight.R, Constants.InkColorPinkLight.G, Constants.InkColorPinkLight.B);
                    else if (inkColor == 7)
                        // Teal (亮色)
                        inkCanvas.DefaultDrawingAttributes.Color = Color.FromArgb((byte)alpha, Constants.InkColorTealLight.R, Constants.InkColorTealLight.G, Constants.InkColorTealLight.B);
                    else if (inkColor == 8)
                        // Orange (亮色)
                        inkCanvas.DefaultDrawingAttributes.Color = Color.FromArgb((byte)alpha, Constants.InkColorOrangeLight.R, Constants.InkColorOrangeLight.G, Constants.InkColorOrangeLight.B);
                }
                else {
                    if (inkColor == 1)
                        // Red
                        inkCanvas.DefaultDrawingAttributes.Color = Color.FromArgb((byte)alpha, 220, 38, 38);
                    else if (inkColor == 2)
                        // Green
                        inkCanvas.DefaultDrawingAttributes.Color = Color.FromArgb((byte)alpha, 22, 163, 74);
                    else if (inkColor == 3)
                        // Blue
                        inkCanvas.DefaultDrawingAttributes.Color = Color.FromArgb((byte)alpha, 37, 99, 235);
                    else if (inkColor == 4)
                        // Yellow
                        inkCanvas.DefaultDrawingAttributes.Color = Color.FromArgb((byte)alpha, 234, 179, 8);
                    else if (inkColor == 6)
                        // Pink ( Purple )
                        inkCanvas.DefaultDrawingAttributes.Color = Color.FromArgb((byte)alpha, 147, 51, 234);
                    else if (inkColor == 7)
                        // Teal (暗色)
                        inkCanvas.DefaultDrawingAttributes.Color = Color.FromArgb((byte)alpha, 13, 148, 136);
                    else if (inkColor == 8)
                        // Orange (暗色)
                        inkCanvas.DefaultDrawingAttributes.Color = Color.FromArgb((byte)alpha, 234, 88, 12);
                }
            }
            else if (penType == 1) {
                if (highlighterColor == 100)
                    // Black
                    inkCanvas.DefaultDrawingAttributes.Color = Color.FromRgb(0, 0, 0);
                else if (highlighterColor == 101)
                    // White
                    inkCanvas.DefaultDrawingAttributes.Color = Color.FromRgb(250, 250, 250);
                else if (highlighterColor == 102)
                    // Red
                    inkCanvas.DefaultDrawingAttributes.Color = Color.FromRgb(239, 68, 68);
                else if (highlighterColor == 103)
                    // Yellow
                    inkCanvas.DefaultDrawingAttributes.Color = Color.FromRgb(253, 224, 71);
                else if (highlighterColor == 104)
                    // Green
                    inkCanvas.DefaultDrawingAttributes.Color = Color.FromRgb(74, 222, 128);
                else if (highlighterColor == 105)
                    // Zinc
                    inkCanvas.DefaultDrawingAttributes.Color = Color.FromRgb(113, 113, 122);
                else if (highlighterColor == 106)
                    // Blue
                    inkCanvas.DefaultDrawingAttributes.Color = Color.FromRgb(59, 130, 246);
                else if (highlighterColor == 107)
                    // Purple
                    inkCanvas.DefaultDrawingAttributes.Color = Color.FromRgb(168, 85, 247);
                else if (highlighterColor == 108)
                    // teal
                    inkCanvas.DefaultDrawingAttributes.Color = Color.FromRgb(45, 212, 191);
                else if (highlighterColor == 109)
                    // Orange
                    inkCanvas.DefaultDrawingAttributes.Color = Color.FromRgb(249, 115, 22);
            }

            if (isUselightThemeColor) {
                // 亮系
                // 亮色的红色
                // BorderPenColorRed.Background = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                // BoardBorderPenColorRed.Background = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                // // 亮色的绿色
                // BorderPenColorGreen.Background = new SolidColorBrush(Color.FromRgb(34, 197, 94));
                // BoardBorderPenColorGreen.Background = new SolidColorBrush(Color.FromRgb(34, 197, 94));
                // // 亮色的蓝色
                // BorderPenColorBlue.Background = new SolidColorBrush(Color.FromRgb(59, 130, 246));
                // BoardBorderPenColorBlue.Background = new SolidColorBrush(Color.FromRgb(59, 130, 246));
                // // 亮色的黄色
                // BorderPenColorYellow.Background = new SolidColorBrush(Color.FromRgb(250, 204, 21));
                // BoardBorderPenColorYellow.Background = new SolidColorBrush(Color.FromRgb(250, 204, 21));
                // // 亮色的粉色
                // BorderPenColorPink.Background = new SolidColorBrush(Color.FromRgb(236, 72, 153));
                // BoardBorderPenColorPink.Background = new SolidColorBrush(Color.FromRgb(236, 72, 153));
                // // 亮色的Teal
                // BorderPenColorTeal.Background = new SolidColorBrush(Color.FromRgb(20, 184, 166));
                // BoardBorderPenColorTeal.Background = new SolidColorBrush(Color.FromRgb(20, 184, 166));
                // // 亮色的Orange
                // BorderPenColorOrange.Background = new SolidColorBrush(Color.FromRgb(249, 115, 22));
                // BoardBorderPenColorOrange.Background = new SolidColorBrush(Color.FromRgb(249, 115, 22));

                // var newImageSource = new BitmapImage();
                // newImageSource.BeginInit();
                // newImageSource.UriSource = new Uri("/Resources/Icons-Fluent/ic_fluent_weather_moon_24_regular.png",
                //     UriKind.RelativeOrAbsolute);
                // newImageSource.EndInit();
                // ColorThemeSwitchIcon.Source = newImageSource;
                // BoardColorThemeSwitchIcon.Source = newImageSource;

                // ColorThemeSwitchTextBlock.Text = "暗系";
                // BoardColorThemeSwitchTextBlock.Text = "暗系";
            }
            else {
                // 暗系
                // 暗色的红色
                // BorderPenColorRed.Background = new SolidColorBrush(Color.FromRgb(220, 38, 38));
                // BoardBorderPenColorRed.Background = new SolidColorBrush(Color.FromRgb(220, 38, 38));
                // // 暗色的绿色
                // BorderPenColorGreen.Background = new SolidColorBrush(Color.FromRgb(22, 163, 74));
                // BoardBorderPenColorGreen.Background = new SolidColorBrush(Color.FromRgb(22, 163, 74));
                // // 暗色的蓝色
                // BorderPenColorBlue.Background = new SolidColorBrush(Color.FromRgb(37, 99, 235));
                // BoardBorderPenColorBlue.Background = new SolidColorBrush(Color.FromRgb(37, 99, 235));
                // // 暗色的黄色
                // BorderPenColorYellow.Background = new SolidColorBrush(Color.FromRgb(234, 179, 8));
                // BoardBorderPenColorYellow.Background = new SolidColorBrush(Color.FromRgb(234, 179, 8));
                // // 暗色的紫色对应亮色的粉色
                // BorderPenColorPink.Background = new SolidColorBrush(Color.FromRgb(147, 51, 234));
                // BoardBorderPenColorPink.Background = new SolidColorBrush(Color.FromRgb(147, 51, 234));
                // // 暗色的Teal
                // BorderPenColorTeal.Background = new SolidColorBrush(Color.FromRgb(13, 148, 136));
                // BoardBorderPenColorTeal.Background = new SolidColorBrush(Color.FromRgb(13, 148, 136));
                // // 暗色的Orange
                // BorderPenColorOrange.Background = new SolidColorBrush(Color.FromRgb(234, 88, 12));
                // BoardBorderPenColorOrange.Background = new SolidColorBrush(Color.FromRgb(234, 88, 12));

                // var newImageSource = new BitmapImage();
                // newImageSource.BeginInit();
                // newImageSource.UriSource = new Uri("/Resources/Icons-Fluent/ic_fluent_weather_sunny_24_regular.png",
                //     UriKind.RelativeOrAbsolute);
                // newImageSource.EndInit();
                // ColorThemeSwitchIcon.Source = newImageSource;
                // BoardColorThemeSwitchIcon.Source = newImageSource;

                // ColorThemeSwitchTextBlock.Text = "亮系";
                // BoardColorThemeSwitchTextBlock.Text = "亮系";
            }

            // 改变选中提示
            // ViewboxBtnColorBlackContent.Visibility = Visibility.Collapsed;
            // ViewboxBtnColorBlueContent.Visibility = Visibility.Collapsed;
            // ViewboxBtnColorGreenContent.Visibility = Visibility.Collapsed;
            // ViewboxBtnColorRedContent.Visibility = Visibility.Collapsed;
            // ViewboxBtnColorYellowContent.Visibility = Visibility.Collapsed;
            // ViewboxBtnColorWhiteContent.Visibility = Visibility.Collapsed;
            // ViewboxBtnColorPinkContent.Visibility = Visibility.Collapsed;
            // ViewboxBtnColorTealContent.Visibility = Visibility.Collapsed;
            // ViewboxBtnColorOrangeContent.Visibility = Visibility.Collapsed;

            // BoardViewboxBtnColorBlackContent.Visibility = Visibility.Collapsed;
            // BoardViewboxBtnColorBlueContent.Visibility = Visibility.Collapsed;
            // BoardViewboxBtnColorGreenContent.Visibility = Visibility.Collapsed;
            // BoardViewboxBtnColorRedContent.Visibility = Visibility.Collapsed;
            // BoardViewboxBtnColorYellowContent.Visibility = Visibility.Collapsed;
            // BoardViewboxBtnColorWhiteContent.Visibility = Visibility.Collapsed;
            // BoardViewboxBtnColorPinkContent.Visibility = Visibility.Collapsed;
            // BoardViewboxBtnColorTealContent.Visibility = Visibility.Collapsed;
            // BoardViewboxBtnColorOrangeContent.Visibility = Visibility.Collapsed;

            // HighlighterPenViewboxBtnColorBlackContent.Visibility = Visibility.Collapsed;
            // HighlighterPenViewboxBtnColorBlueContent.Visibility = Visibility.Collapsed;
            // HighlighterPenViewboxBtnColorGreenContent.Visibility = Visibility.Collapsed;
            // HighlighterPenViewboxBtnColorOrangeContent.Visibility = Visibility.Collapsed;
            // HighlighterPenViewboxBtnColorPurpleContent.Visibility = Visibility.Collapsed;
            // HighlighterPenViewboxBtnColorRedContent.Visibility = Visibility.Collapsed;
            // HighlighterPenViewboxBtnColorTealContent.Visibility = Visibility.Collapsed;
            // HighlighterPenViewboxBtnColorWhiteContent.Visibility = Visibility.Collapsed;
            // HighlighterPenViewboxBtnColorYellowContent.Visibility = Visibility.Collapsed;
            // HighlighterPenViewboxBtnColorZincContent.Visibility = Visibility.Collapsed;

            // BoardHighlighterPenViewboxBtnColorBlackContent.Visibility = Visibility.Collapsed;
            // BoardHighlighterPenViewboxBtnColorBlueContent.Visibility = Visibility.Collapsed;
            // BoardHighlighterPenViewboxBtnColorGreenContent.Visibility = Visibility.Collapsed;
            // BoardHighlighterPenViewboxBtnColorOrangeContent.Visibility = Visibility.Collapsed;
            // BoardHighlighterPenViewboxBtnColorPurpleContent.Visibility = Visibility.Collapsed;
            // BoardHighlighterPenViewboxBtnColorRedContent.Visibility = Visibility.Collapsed;
            // BoardHighlighterPenViewboxBtnColorTealContent.Visibility = Visibility.Collapsed;
            // BoardHighlighterPenViewboxBtnColorWhiteContent.Visibility = Visibility.Collapsed;
            // BoardHighlighterPenViewboxBtnColorYellowContent.Visibility = Visibility.Collapsed;
            // BoardHighlighterPenViewboxBtnColorZincContent.Visibility = Visibility.Collapsed;

            switch (inkColor) {
                case 0:
                    // ViewboxBtnColorBlackContent.Visibility = Visibility.Visible;
                    // BoardViewboxBtnColorBlackContent.Visibility = Visibility.Visible;
                    break;
                case 1:
                    // ViewboxBtnColorRedContent.Visibility = Visibility.Visible;
                    // BoardViewboxBtnColorRedContent.Visibility = Visibility.Visible;
                    break;
                case 2:
                    // ViewboxBtnColorGreenContent.Visibility = Visibility.Visible;
                    // BoardViewboxBtnColorGreenContent.Visibility = Visibility.Visible;
                    break;
                case 3:
                    // ViewboxBtnColorBlueContent.Visibility = Visibility.Visible;
                    // BoardViewboxBtnColorBlueContent.Visibility = Visibility.Visible;
                    break;
                case 4:
                    // ViewboxBtnColorYellowContent.Visibility = Visibility.Visible;
                    // BoardViewboxBtnColorYellowContent.Visibility = Visibility.Visible;
                    break;
                case 5:
                    // ViewboxBtnColorWhiteContent.Visibility = Visibility.Visible;
                    // BoardViewboxBtnColorWhiteContent.Visibility = Visibility.Visible;
                    break;
                case 6:
                    // ViewboxBtnColorPinkContent.Visibility = Visibility.Visible;
                    // BoardViewboxBtnColorPinkContent.Visibility = Visibility.Visible;
                    break;
                case 7:
                    // ViewboxBtnColorTealContent.Visibility = Visibility.Visible;
                    // BoardViewboxBtnColorTealContent.Visibility = Visibility.Visible;
                    break;
                case 8:
                    // ViewboxBtnColorOrangeContent.Visibility = Visibility.Visible;
                    // BoardViewboxBtnColorOrangeContent.Visibility = Visibility.Visible;
                    break;
            }

            switch (highlighterColor) {
                case 100:
                    // HighlighterPenViewboxBtnColorBlackContent.Visibility = Visibility.Visible;
                    // BoardHighlighterPenViewboxBtnColorBlackContent.Visibility = Visibility.Visible;
                    break;
                case 101:
                    // HighlighterPenViewboxBtnColorWhiteContent.Visibility = Visibility.Visible;
                    // BoardHighlighterPenViewboxBtnColorWhiteContent.Visibility = Visibility.Visible;
                    break;
                case 102:
                    // HighlighterPenViewboxBtnColorRedContent.Visibility = Visibility.Visible;
                    // BoardHighlighterPenViewboxBtnColorRedContent.Visibility = Visibility.Visible;
                    break;
                case 103:
                    // HighlighterPenViewboxBtnColorYellowContent.Visibility = Visibility.Visible;
                    // BoardHighlighterPenViewboxBtnColorYellowContent.Visibility = Visibility.Visible;
                    break;
                case 104:
                    // HighlighterPenViewboxBtnColorGreenContent.Visibility = Visibility.Visible;
                    // BoardHighlighterPenViewboxBtnColorGreenContent.Visibility = Visibility.Visible;
                    break;
                case 105:
                    // HighlighterPenViewboxBtnColorZincContent.Visibility = Visibility.Visible;
                    // BoardHighlighterPenViewboxBtnColorZincContent.Visibility = Visibility.Visible;
                    break;
                case 106:
                    // HighlighterPenViewboxBtnColorBlueContent.Visibility = Visibility.Visible;
                    // BoardHighlighterPenViewboxBtnColorBlueContent.Visibility = Visibility.Visible;
                    break;
                case 107:
                    // HighlighterPenViewboxBtnColorPurpleContent.Visibility = Visibility.Visible;
                    // BoardHighlighterPenViewboxBtnColorPurpleContent.Visibility = Visibility.Visible;
                    break;
                case 108:
                    // HighlighterPenViewboxBtnColorTealContent.Visibility = Visibility.Visible;
                    // BoardHighlighterPenViewboxBtnColorTealContent.Visibility = Visibility.Visible;
                    break;
                case 109:
                    // HighlighterPenViewboxBtnColorOrangeContent.Visibility = Visibility.Visible;
                    // BoardHighlighterPenViewboxBtnColorOrangeContent.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void CheckLastColor(int inkColor, bool isHighlighter = false) {
            if (isHighlighter == true) {
                highlighterColor = inkColor;
                // 保存荧光笔颜色到设置
                Settings.Canvas.LastHighlighterColor = highlighterColor;
                SaveSettings();
            }
            else {
                if (currentMode == 0) {
                    lastDesktopInkColor = inkColor;
                    Settings.Canvas.LastDesktopInkColor = lastDesktopInkColor;
                }
                else {
                    lastBoardInkColor = inkColor;
                    Settings.Canvas.LastBoardInkColor = lastBoardInkColor;
                }
                SaveSettings();
            }
        }

        private async void CheckPenTypeUIState() {
            if (penType == 0) {
                // DefaultPenPropsPanel.Visibility = Visibility.Visible;
                // DefaultPenColorsPanel.Visibility = Visibility.Visible;
                // HighlighterPenColorsPanel.Visibility = Visibility.Collapsed;
                // HighlighterPenPropsPanel.Visibility = Visibility.Collapsed;
                // DefaultPenTabButton.Opacity = 1;
                // DefaultPenTabButtonText.FontWeight = FontWeights.Bold;
                // DefaultPenTabButtonText.Margin = new Thickness(2, 0.5, 0, 0);
                // DefaultPenTabButtonText.FontSize = 9.5;
                // DefaultPenTabButton.Background = new SolidColorBrush(Color.FromArgb(72, 219, 234, 254));
                // DefaultPenTabButtonIndicator.Visibility = Visibility.Visible;
                // HighlightPenTabButton.Opacity = 0.9;
                // HighlightPenTabButtonText.FontWeight = FontWeights.Normal;
                // HighlightPenTabButtonText.FontSize = 9;
                // HighlightPenTabButtonText.Margin = new Thickness(2, 1, 0, 0);
                // HighlightPenTabButton.Background = new SolidColorBrush(Colors.Transparent);
                // HighlightPenTabButtonIndicator.Visibility = Visibility.Collapsed;

                // BoardDefaultPenPropsPanel.Visibility = Visibility.Visible;
                // BoardDefaultPenColorsPanel.Visibility = Visibility.Visible;
                // BoardHighlighterPenColorsPanel.Visibility = Visibility.Collapsed;
                // BoardHighlighterPenPropsPanel.Visibility = Visibility.Collapsed;
                // BoardDefaultPenTabButton.Opacity = 1;
                // BoardDefaultPenTabButtonText.FontWeight = FontWeights.Bold;
                // BoardDefaultPenTabButtonText.Margin = new Thickness(2, 0.5, 0, 0);
                // BoardDefaultPenTabButtonText.FontSize = 9.5;
                // BoardDefaultPenTabButton.Background = new SolidColorBrush(Color.FromArgb(72, 219, 234, 254));
                // BoardDefaultPenTabButtonIndicator.Visibility = Visibility.Visible;
                // BoardHighlightPenTabButton.Opacity = 0.9;
                // BoardHighlightPenTabButtonText.FontWeight = FontWeights.Normal;
                // BoardHighlightPenTabButtonText.FontSize = 9;
                // BoardHighlightPenTabButtonText.Margin = new Thickness(2, 1, 0, 0);
                // BoardHighlightPenTabButton.Background = new SolidColorBrush(Colors.Transparent);
                // BoardHighlightPenTabButtonIndicator.Visibility = Visibility.Collapsed;

                // // PenPalette.Margin = new Thickness(-160, -200, -33, 32);
                // await Dispatcher.InvokeAsync(() => {
                //     var marginAnimation = new ThicknessAnimation
                //     {
                //         Duration = TimeSpan.FromSeconds(0.1),
                //         From = PenPalette.Margin,
                //         To = new Thickness(-160, -200, -33, 32),
                //         EasingFunction = new CubicEase()
                //     };
                //     PenPalette.BeginAnimation(MarginProperty, marginAnimation);
                // });

                // await Dispatcher.InvokeAsync(() => {
                //     var marginAnimation = new ThicknessAnimation
                //     {
                //         Duration = TimeSpan.FromSeconds(0.1),
                //         From = PenPalette.Margin,
                //         To = new Thickness(-160, -200, -33, 50),
                //         EasingFunction = new CubicEase()
                //     };
                //     BoardPenPaletteGrid.BeginAnimation(MarginProperty, marginAnimation);
                // });


                // await Task.Delay(100);

                // await Dispatcher.InvokeAsync(() => { PenPalette.Margin = new Thickness(-160, -200, -33, 32); });

                // await Dispatcher.InvokeAsync(() => { BoardPenPaletteGrid.Margin = new Thickness(-160, -200, -33, 50); });
            }
            else if (penType == 1) {
                // DefaultPenPropsPanel.Visibility = Visibility.Collapsed;
                // DefaultPenColorsPanel.Visibility = Visibility.Collapsed;
                // HighlighterPenColorsPanel.Visibility = Visibility.Visible;
                // HighlighterPenPropsPanel.Visibility = Visibility.Visible;
                // DefaultPenTabButton.Opacity = 0.9;
                // DefaultPenTabButtonText.FontWeight = FontWeights.Normal;
                // DefaultPenTabButtonText.FontSize = 9;
                // DefaultPenTabButtonText.Margin = new Thickness(2, 1, 0, 0);
                // DefaultPenTabButton.Background = new SolidColorBrush(Colors.Transparent);
                // DefaultPenTabButtonIndicator.Visibility = Visibility.Collapsed;
                // HighlightPenTabButton.Opacity = 1;
                // HighlightPenTabButtonText.FontWeight = FontWeights.Bold;
                // HighlightPenTabButtonText.FontSize = 9.5;
                // HighlightPenTabButtonText.Margin = new Thickness(2, 0.5, 0, 0);
                // HighlightPenTabButton.Background = new SolidColorBrush(Color.FromArgb(72, 219, 234, 254));
                // HighlightPenTabButtonIndicator.Visibility = Visibility.Visible;

                // BoardDefaultPenPropsPanel.Visibility = Visibility.Collapsed;
                // BoardDefaultPenColorsPanel.Visibility = Visibility.Collapsed;
                // BoardHighlighterPenColorsPanel.Visibility = Visibility.Visible;
                // BoardHighlighterPenPropsPanel.Visibility = Visibility.Visible;
                // BoardDefaultPenTabButton.Opacity = 0.9;
                // BoardDefaultPenTabButtonText.FontWeight = FontWeights.Normal;
                // BoardDefaultPenTabButtonText.FontSize = 9;
                // BoardDefaultPenTabButtonText.Margin = new Thickness(2, 1, 0, 0);
                // BoardDefaultPenTabButton.Background = new SolidColorBrush(Colors.Transparent);
                // BoardDefaultPenTabButtonIndicator.Visibility = Visibility.Collapsed;
                // BoardHighlightPenTabButton.Opacity = 1;
                // BoardHighlightPenTabButtonText.FontWeight = FontWeights.Bold;
                // BoardHighlightPenTabButtonText.FontSize = 9.5;
                // BoardHighlightPenTabButtonText.Margin = new Thickness(2, 0.5, 0, 0);
                // BoardHighlightPenTabButton.Background = new SolidColorBrush(Color.FromArgb(72, 219, 234, 254));
                // BoardHighlightPenTabButtonIndicator.Visibility = Visibility.Visible;

                // // PenPalette.Margin = new Thickness(-160, -157, -33, 32);
                // await Dispatcher.InvokeAsync(() => {
                //     var marginAnimation = new ThicknessAnimation
                //     {
                //         Duration = TimeSpan.FromSeconds(0.1),
                //         From = PenPalette.Margin,
                //         To = new Thickness(-160, -157, -33, 32),
                //         EasingFunction = new CubicEase()
                //     };
                //     PenPalette.BeginAnimation(MarginProperty, marginAnimation);
                // });

                // await Dispatcher.InvokeAsync(() => {
                //     var marginAnimation = new ThicknessAnimation
                //     {
                //         Duration = TimeSpan.FromSeconds(0.1),
                //         From = PenPalette.Margin,
                //         To = new Thickness(-160, -154, -33, 50),
                //         EasingFunction = new CubicEase()
                //     };
                //     BoardPenPaletteGrid.BeginAnimation(MarginProperty, marginAnimation);
                // });

                // await Task.Delay(100);

                // await Dispatcher.InvokeAsync(() => { PenPalette.Margin = new Thickness(-160, -157, -33, 32); });

                // await Dispatcher.InvokeAsync(() => { BoardPenPaletteGrid.Margin = new Thickness(-160, -154, -33, 50); });
            }
        }

        private void SwitchToDefaultPen(object sender, MouseButtonEventArgs e) {
            // 保存笔类型状态
            lastPenType = 0;
            penType = 0;

            // 保存当前签字笔粗细（如果当前是签字笔模式）
            if (drawingAttributes.StylusTip == StylusTip.Ellipse && !drawingAttributes.IsHighlighter) {
                lastPenWidth = drawingAttributes.Width;
            }

            CheckPenTypeUIState();
            CheckColorTheme();

            // 恢复之前保存的签字笔粗细，如果没有则使用设置中的默认值
            // 使用验证方法确保粗细值有效 - Requirements: 1.2
            double penWidth = lastPenWidth > 0 ? lastPenWidth : Settings.Canvas.InkWidth;
            penWidth = ValidateAndCorrectPenWidth(penWidth, false);
            drawingAttributes.Width = penWidth;
            drawingAttributes.Height = penWidth;
            drawingAttributes.StylusTip = StylusTip.Ellipse;
            drawingAttributes.IsHighlighter = false;

            // 保存笔类型到设置
            Settings.Canvas.LastPenType = lastPenType;
            SaveSettings();
        }

        private void SwitchToHighlighterPen(object sender, MouseButtonEventArgs e) {
            // 保存笔类型状态
            lastPenType = 1;
            penType = 1;

            // 保存当前荧光笔粗细（如果当前是荧光笔模式）
            if (drawingAttributes.StylusTip == StylusTip.Rectangle && drawingAttributes.IsHighlighter) {
                lastHighlighterWidth = drawingAttributes.Height;
            }

            CheckPenTypeUIState();
            CheckColorTheme();

            // 恢复之前保存的荧光笔粗细，如果没有则使用设置中的默认值
            // 使用验证方法确保粗细值有效 - Requirements: 1.2
            double highlighterWidth = lastHighlighterWidth > 0 ? lastHighlighterWidth : Settings.Canvas.HighlighterWidth;
            highlighterWidth = ValidateAndCorrectPenWidth(highlighterWidth, true);
            drawingAttributes.Width = highlighterWidth / 2;
            drawingAttributes.Height = highlighterWidth;
            drawingAttributes.StylusTip = StylusTip.Rectangle;
            drawingAttributes.IsHighlighter = true;

            // 保存笔类型到设置
            Settings.Canvas.LastPenType = lastPenType;
            SaveSettings();
        }

        private void BtnColorBlack_Click(object sender, RoutedEventArgs e) {
            CheckLastColor(0);
            forceEraser = false;
            ColorSwitchCheck();
        }

        private void BtnColorRed_Click(object sender, RoutedEventArgs e) {
            CheckLastColor(1);
            forceEraser = false;
            ColorSwitchCheck();
        }

        private void BtnColorGreen_Click(object sender, RoutedEventArgs e) {
            CheckLastColor(2);
            forceEraser = false;
            ColorSwitchCheck();
        }

        private void BtnColorBlue_Click(object sender, RoutedEventArgs e) {
            CheckLastColor(3);
            forceEraser = false;
            ColorSwitchCheck();
        }

        private void BtnColorYellow_Click(object sender, RoutedEventArgs e) {
            CheckLastColor(4);
            forceEraser = false;
            ColorSwitchCheck();
        }

        private void BtnColorWhite_Click(object sender, RoutedEventArgs e) {
            CheckLastColor(5);
            forceEraser = false;
            ColorSwitchCheck();
        }

        private void BtnColorPink_Click(object sender, RoutedEventArgs e) {
            CheckLastColor(6);
            forceEraser = false;
            ColorSwitchCheck();
        }

        private void BtnColorOrange_Click(object sender, RoutedEventArgs e) {
            CheckLastColor(8);
            forceEraser = false;
            ColorSwitchCheck();
        }

        private void BtnColorTeal_Click(object sender, RoutedEventArgs e) {
            CheckLastColor(7);
            forceEraser = false;
            ColorSwitchCheck();
        }

        private void BtnHighlighterColorBlack_Click(object sender, RoutedEventArgs e) {
            CheckLastColor(100, true);
            penType = 1;
            forceEraser = false;
            CheckPenTypeUIState();
            ColorSwitchCheck();
        }

        private void BtnHighlighterColorWhite_Click(object sender, RoutedEventArgs e) {
            CheckLastColor(101, true);
            penType = 1;
            forceEraser = false;
            CheckPenTypeUIState();
            ColorSwitchCheck();
        }

        private void BtnHighlighterColorRed_Click(object sender, RoutedEventArgs e) {
            CheckLastColor(102, true);
            penType = 1;
            forceEraser = false;
            CheckPenTypeUIState();
            ColorSwitchCheck();
        }

        private void BtnHighlighterColorYellow_Click(object sender, RoutedEventArgs e) {
            CheckLastColor(103, true);
            penType = 1;
            forceEraser = false;
            CheckPenTypeUIState();
            ColorSwitchCheck();
        }

        private void BtnHighlighterColorGreen_Click(object sender, RoutedEventArgs e) {
            CheckLastColor(104, true);
            penType = 1;
            forceEraser = false;
            CheckPenTypeUIState();
            ColorSwitchCheck();
        }

        private void BtnHighlighterColorZinc_Click(object sender, RoutedEventArgs e) {
            CheckLastColor(105, true);
            penType = 1;
            forceEraser = false;
            CheckPenTypeUIState();
            ColorSwitchCheck();
        }

        private void BtnHighlighterColorBlue_Click(object sender, RoutedEventArgs e) {
            CheckLastColor(106, true);
            penType = 1;
            forceEraser = false;
            CheckPenTypeUIState();
            ColorSwitchCheck();
        }

        private void BtnHighlighterColorPurple_Click(object sender, RoutedEventArgs e) {
            CheckLastColor(107, true);
            penType = 1;
            forceEraser = false;
            CheckPenTypeUIState();
            ColorSwitchCheck();
        }

        private void BtnHighlighterColorTeal_Click(object sender, RoutedEventArgs e) {
            CheckLastColor(108, true);
            penType = 1;
            forceEraser = false;
            CheckPenTypeUIState();
            ColorSwitchCheck();
        }

        private void BtnHighlighterColorOrange_Click(object sender, RoutedEventArgs e) {
            CheckLastColor(109, true);
            penType = 1;
            forceEraser = false;
            CheckPenTypeUIState();
            ColorSwitchCheck();
        }

        private Color StringToColor(string colorStr) {
            var argb = new byte[4];
            for (var i = 0; i < 4; i++) {
                var charArray = colorStr.Substring(i * 2 + 1, 2).ToCharArray();
                var b1 = toByte(charArray[0]);
                var b2 = toByte(charArray[1]);
                argb[i] = (byte)(b2 | (b1 << 4));
            }

            return Color.FromArgb(argb[0], argb[1], argb[2], argb[3]); //#FFFFFFFF
        }

        private static byte toByte(char c) {
            var b = (byte)"0123456789ABCDEF".IndexOf(c);
            return b;
        }

        #region PenPaletteV2

        private void PenPaletteV2Init() {
            PenPaletteV2.ColorSelectionChanged += PenpaletteV2_ColorSelectionChanged;
            PenPaletteV2.ColorModeChanged += PenpaletteV2_ColorModeChanged;
            PenPaletteV2.CustomColorChanged += PenpaletteV2_CustomColorChanged;
            PenPaletteV2.PaletteShouldCloseEvent += PenpaletteV2_PaletteShouldCloseEvent;
            PenPaletteV2.PenModeChanged += PenpaletteV2_PenModeChanged;
            PenPaletteV2.InkRecognitionChanged += PenpaletteV2_InkRecognitionChanged;
            PenPaletteV2.PressureSimulationChanged += PenpaletteV2_PressureSimulationChanged;
            PenPaletteV2.PenWidthChanged += PenpaletteV2_PenWidthChanged;
            PenPaletteV2.SelectedColor = ColorPalette.ColorPaletteColor.ColorRed;
        }

        private void PenpaletteV2_ColorSelectionChanged(object sender, ColorPalette.ColorSelectionChangedEventArgs e) {
            if (e.TriggerMode == ColorPalette.TriggerMode.TriggeredByCode) return;
            drawingAttributes.Color = PenPaletteV2.GetColor(e.NowColor, false, null);
            inkCanvas.DefaultDrawingAttributes.Color = drawingAttributes.Color;

            // 更新inkColor变量并保存到lastDesktopInkColor或lastBoardInkColor
            // Requirements: 3.1, 3.5
            int colorIndex = ColorPaletteColorToInkColor(e.NowColor);
            if (colorIndex >= 0) {
                inkColor = colorIndex;
                CheckLastColor(colorIndex);
            }
        }

        /// <summary>
        /// 将ColorPaletteColor枚举转换为inkColor索引
        /// </summary>
        private int ColorPaletteColorToInkColor(ColorPalette.ColorPaletteColor color) {
            switch (color) {
                case ColorPalette.ColorPaletteColor.ColorBlack: return 0;
                case ColorPalette.ColorPaletteColor.ColorRed: return 1;
                case ColorPalette.ColorPaletteColor.ColorLime:
                case ColorPalette.ColorPaletteColor.ColorGreen: return 2;
                case ColorPalette.ColorPaletteColor.ColorIndigo:
                case ColorPalette.ColorPaletteColor.ColorBlue: return 3;
                case ColorPalette.ColorPaletteColor.ColorYellow: return 4;
                case ColorPalette.ColorPaletteColor.ColorWhite: return 5;
                case ColorPalette.ColorPaletteColor.ColorPink:
                case ColorPalette.ColorPaletteColor.ColorFuchsia:
                case ColorPalette.ColorPaletteColor.ColorRose:
                case ColorPalette.ColorPaletteColor.ColorPurple: return 6;
                case ColorPalette.ColorPaletteColor.ColorTeal:
                case ColorPalette.ColorPaletteColor.ColorCyan: return 7;
                case ColorPalette.ColorPaletteColor.ColorOrange: return 8;
                default: return -1; // 自定义颜色
            }
        }

        /// <summary>
        /// 将inkColor索引转换为ColorPaletteColor枚举
        /// Requirements: 3.1, 3.5
        /// </summary>
        private ColorPalette.ColorPaletteColor InkColorToColorPaletteColor(int inkColorIndex) {
            switch (inkColorIndex) {
                case 0: return ColorPalette.ColorPaletteColor.ColorBlack;
                case 1: return ColorPalette.ColorPaletteColor.ColorRed;
                case 2: return ColorPalette.ColorPaletteColor.ColorGreen;
                case 3: return ColorPalette.ColorPaletteColor.ColorBlue;
                case 4: return ColorPalette.ColorPaletteColor.ColorYellow;
                case 5: return ColorPalette.ColorPaletteColor.ColorWhite;
                case 6: return ColorPalette.ColorPaletteColor.ColorPink;
                case 7: return ColorPalette.ColorPaletteColor.ColorTeal;
                case 8: return ColorPalette.ColorPaletteColor.ColorOrange;
                default: return ColorPalette.ColorPaletteColor.ColorRed; // 默认红色
            }
        }

        private void PenpaletteV2_ColorModeChanged(object sender, ColorPalette.ColorModeChangedEventArgs e) {
            if (e.TriggerMode == ColorPalette.TriggerMode.TriggeredByCode) return;
            drawingAttributes.Color = PenPaletteV2.GetColor(PenPaletteV2.SelectedColor, false, null);
        }

        private void PenpaletteV2_CustomColorChanged(object sender, ColorPalette.CustomColorChangedEventArgs e) {
            if (e.TriggerMode == ColorPalette.TriggerMode.TriggeredByCode) return;
            if (PenPaletteV2.SelectedColor == ColorPalette.ColorPaletteColor.ColorCustom)
                drawingAttributes.Color = e.NowColor??new Color();
        }

        private void PenpaletteV2_PaletteShouldCloseEvent(object sender, RoutedEventArgs e) {
            PenPaletteV2Popup.IsOpen = false;
        }

        private void PenpaletteV2_PenModeChanged(object sender, ColorPalette.PenModeChangedEventArgs e) {
            // 更新penType变量
            penType = e.NowMode == ColorPalette.PenMode.HighlighterMode ? 1 : 0;

            // 保存lastPenType以便在切换模式后恢复
            lastPenType = penType;

            // 根据笔类型设置绘图属性，使用验证方法确保粗细值有效 - Requirements: 1.2
            if (e.NowMode == ColorPalette.PenMode.HighlighterMode) {
                // 荧光笔模式：使用保存的荧光笔粗细
                double validatedHighlighterWidth = ValidateAndCorrectPenWidth(lastHighlighterWidth, true);
                drawingAttributes.Width = validatedHighlighterWidth / 2;
                drawingAttributes.Height = validatedHighlighterWidth;
                drawingAttributes.StylusTip = StylusTip.Rectangle;
                drawingAttributes.IsHighlighter = true;
            } else {
                // 签字笔模式：使用保存的签字笔粗细
                double validatedPenWidth = ValidateAndCorrectPenWidth(lastPenWidth, false);
                drawingAttributes.Width = validatedPenWidth;
                drawingAttributes.Height = validatedPenWidth;
                drawingAttributes.StylusTip = StylusTip.Ellipse;
                drawingAttributes.IsHighlighter = false;
            }

            // 调用CheckPenTypeUIState()更新UI状态
            CheckPenTypeUIState();

            // 保存笔类型到设置
            Settings.Canvas.LastPenType = lastPenType;
            SaveSettings();
        }

        private void PenpaletteV2_InkRecognitionChanged(object sender, ColorPalette.InkRecognitionChangedEventArgs e) {
            if (e.TriggerMode == ColorPalette.TriggerMode.TriggeredByCode) return;
            Settings.InkToShape.IsInkToShapeEnabled = e.NowStatus;
            SaveSettings();
        }

        private void PenpaletteV2_PressureSimulationChanged(object sender, ColorPalette.PressureSimulationChangedEventArgs e) {
            if (e.TriggerMode == ColorPalette.TriggerMode.TriggeredByCode) return;

            // 将调色盘的压感模拟模式映射到Settings.Canvas.InkStyle
            // PressureSimulation.None = 不模拟 (InkStyle = -1)
            // PressureSimulation.PointSimulate = 点集笔锋 (InkStyle = 0)
            // PressureSimulation.VelocitySimulate = 速度笔锋 (InkStyle = 1)
            switch (e.NowMode) {
                case ColorPalette.PressureSimulation.None:
                    Settings.Canvas.InkStyle = -1;
                    break;
                case ColorPalette.PressureSimulation.PointSimulate:
                    Settings.Canvas.InkStyle = 0;
                    break;
                case ColorPalette.PressureSimulation.VelocitySimulate:
                    Settings.Canvas.InkStyle = 1;
                    break;
            }

            SaveSettings();
        }

        private void PenpaletteV2_PenWidthChanged(object sender, ColorPalette.PenWidthChangedEventArgs e) {
            if (e.TriggerMode == ColorPalette.TriggerMode.TriggeredByCode) return;

            // 根据当前笔类型更新drawingAttributes和保存粗细值
            // 使用验证方法确保粗细值有效 - Requirements: 1.2
            if (penType == 0) {
                // 签字笔模式：宽高相等
                double validatedWidth = ValidateAndCorrectPenWidth(e.NowWidth, false);
                drawingAttributes.Width = validatedWidth;
                drawingAttributes.Height = validatedWidth;
                lastPenWidth = validatedWidth;
                Settings.Canvas.InkWidth = validatedWidth;
            } else {
                // 荧光笔模式：宽度为高度的一半
                double validatedWidth = ValidateAndCorrectPenWidth(e.NowWidth, true);
                drawingAttributes.Width = validatedWidth / 2;
                drawingAttributes.Height = validatedWidth;
                lastHighlighterWidth = validatedWidth;
                Settings.Canvas.HighlighterWidth = validatedWidth;
            }

            // 应用到inkCanvas
            inkCanvas.DefaultDrawingAttributes.Width = drawingAttributes.Width;
            inkCanvas.DefaultDrawingAttributes.Height = drawingAttributes.Height;

            // 保存设置
            SaveSettings();
        }

        #endregion
    }
}
