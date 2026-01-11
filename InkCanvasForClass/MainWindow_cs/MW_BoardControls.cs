// ============================================================================
// MW_BoardControls.cs - 白板模式控制逻辑
// ============================================================================
// 
// 功能说明:
//   - 白板页面管理（添加、删除、切换页面）
//   - 白板背景颜色和图案设置
//   - 墨迹存储和恢复（strokeCollections, TimeMachineHistories）
//   - 页面索引显示更新
//
// 迁移状态 (渐进式迁移):
//   - BlackboardView UserControl 已创建
//   - BlackboardViewModel 已实现页面导航命令
//   - PageService 已实现页面状态管理
//   - 此文件中的核心逻辑仍在使用，与 ViewModel 协同工作
//   - UpdateIndexInfoDisplay() 已更新为同时更新 ViewModel 状态
//
// 相关文件:
//   - Views/Blackboard/BlackboardView.xaml
//   - Views/Blackboard/BlackboardView.xaml.cs
//   - ViewModels/BlackboardViewModel.cs
//   - Services/PageService.cs
//   - MainWindow.xaml (BlackboardLeftSide, BlackboardCenterSide, BlackboardRightSide 区域)
//
// ============================================================================

using Ink_Canvas.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using System.Windows.Controls;

namespace Ink_Canvas {
    public partial class MainWindow : Window {
        private StrokeCollection[] strokeCollections = new StrokeCollection[101];
        private bool[] whiteboadLastModeIsRedo = new bool[101];
        private StrokeCollection lastTouchDownStrokeCollection = new StrokeCollection();

        public int CurrentWhiteboardIndex = 1;
        public int WhiteboardTotalCount = 1;
        private TimeMachineHistory[][] TimeMachineHistories = new TimeMachineHistory[101][]; //最多99页，0用来存储非白板时的墨迹以便还原

        public Color[] BoardBackgroundColors = new Color[6] {
            Color.FromRgb(39, 39, 42), 
            Color.FromRgb(23, 42, 37),
            Color.FromRgb(234, 235, 237), 
            Color.FromRgb(15, 23, 42), 
            Color.FromRgb(181, 230, 181), 
            Color.FromRgb(0, 0, 0)
        };

        public class BoardPageSettings {
            public BlackboardBackgroundColorEnum BackgroundColor { get; set; } = BlackboardBackgroundColorEnum.White;
            public BlackboardBackgroundPatternEnum BackgroundPattern { get; set; } = BlackboardBackgroundPatternEnum.None;
        }

        public List<BoardPageSettings> BoardPagesSettingsList = new List<BoardPageSettings>() {
            new BoardPageSettings()
        };

        #region Board Background

        /// <summary>
        ///     更新白板模式下每頁的背景顏色，可以直接根據當前的<c>CurrentWhiteboardIndex</c>獲取背景配置並更新，也可以自己修改當前的背景顏色
        /// </summary>
        /// <param name="id">要修改的背景顏色的ID，傳入null會根據當前的<c>CurrentWhiteboardIndex</c>去讀取有關背景顏色的配置並更新</param>
        private void UpdateBoardBackground(int? id) {
            if (id != null) BoardPagesSettingsList[CurrentWhiteboardIndex - 1].BackgroundColor = (BlackboardBackgroundColorEnum)id;
            var bgC = BoardPagesSettingsList[CurrentWhiteboardIndex - 1].BackgroundColor;
            if (bgC == BlackboardBackgroundColorEnum.BlackBoardGreen
                || bgC == BlackboardBackgroundColorEnum.BlueBlack
                || bgC == BlackboardBackgroundColorEnum.GrayBlack
                || bgC == BlackboardBackgroundColorEnum.RealBlack) {
                if (inkColor == 0) lastBoardInkColor = 5;
            } else {
                if (inkColor == 5) lastBoardInkColor = 0;
            }
            CheckColorTheme(true);
            UpdateBoardBackgroundPanelDisplayStatus();
            ApplyBackgroundPattern();
        }

        /// <summary>
        ///     应用稿纸格式到白板背景
        /// </summary>
        private void ApplyBackgroundPattern() {
            if (currentMode != 1) return;
            
            var pattern = BoardPagesSettingsList[CurrentWhiteboardIndex - 1].BackgroundPattern;
            var bgColor = BoardPagesSettingsList[CurrentWhiteboardIndex - 1].BackgroundColor;
            var baseColor = BoardBackgroundColors[(int)bgColor];
            
            // 判断是深色还是浅色背景
            bool isDarkBackground = bgColor == BlackboardBackgroundColorEnum.BlackBoardGreen
                || bgColor == BlackboardBackgroundColorEnum.BlueBlack
                || bgColor == BlackboardBackgroundColorEnum.GrayBlack
                || bgColor == BlackboardBackgroundColorEnum.RealBlack;
            
            // 设置图案颜色（深色背景用浅色图案，浅色背景用深色图案）
            Color patternColor = isDarkBackground 
                ? Color.FromArgb(40, 255, 255, 255)
                : Color.FromArgb(25, 0, 0, 0);
            
            if (pattern == BlackboardBackgroundPatternEnum.None) {
                // 无格式，纯色背景
                GridBackgroundCover.Background = new SolidColorBrush(baseColor);
            }
            else if (pattern == BlackboardBackgroundPatternEnum.Dots) {
                // 点阵格式
                GridBackgroundCover.Background = CreateDotPatternBackground(baseColor, patternColor);
            }
            else if (pattern == BlackboardBackgroundPatternEnum.Grid) {
                // 网格格式
                GridBackgroundCover.Background = CreateGridPatternBackground(baseColor, patternColor);
            }
        }

        /// <summary>
        ///     创建点阵图案背景
        /// </summary>
        private Brush CreateDotPatternBackground(Color backgroundColor, Color dotColor) {
            double spacing = 20;
            var dotGeometry = new EllipseGeometry(new Point(spacing / 2, spacing / 2), 1.5, 1.5);
            var dotDrawing = new GeometryDrawing(new SolidColorBrush(dotColor), null, dotGeometry);
            
            var drawingGroup = new DrawingGroup();
            drawingGroup.Children.Add(new GeometryDrawing(new SolidColorBrush(backgroundColor), null, 
                new RectangleGeometry(new Rect(0, 0, spacing, spacing))));
            drawingGroup.Children.Add(dotDrawing);
            
            return new DrawingBrush {
                Drawing = drawingGroup,
                TileMode = TileMode.Tile,
                Viewport = new Rect(0, 0, spacing, spacing),
                ViewportUnits = BrushMappingMode.Absolute
            };
        }

        /// <summary>
        ///     创建网格图案背景
        /// </summary>
        private Brush CreateGridPatternBackground(Color backgroundColor, Color gridColor) {
            double spacing = 20;
            double halfPenWidth = 0.5;
            
            var pen = new Pen(new SolidColorBrush(gridColor), 1);
            pen.Freeze();
            
            var drawingGroup = new DrawingGroup();
            
            // 背景矩形
            var bgBrush = new SolidColorBrush(backgroundColor);
            bgBrush.Freeze();
            drawingGroup.Children.Add(new GeometryDrawing(bgBrush, null, 
                new RectangleGeometry(new Rect(0, 0, spacing, spacing))));
            
            // 垂直线（左边，向内偏移半个像素避免重叠）
            drawingGroup.Children.Add(new GeometryDrawing(null, pen, 
                new LineGeometry(new Point(halfPenWidth, 0), new Point(halfPenWidth, spacing))));
            
            // 水平线（顶部，向内偏移半个像素避免重叠）
            drawingGroup.Children.Add(new GeometryDrawing(null, pen, 
                new LineGeometry(new Point(0, halfPenWidth), new Point(spacing, halfPenWidth))));
            
            drawingGroup.Freeze();
            
            var brush = new DrawingBrush {
                Drawing = drawingGroup,
                TileMode = TileMode.Tile,
                Viewport = new Rect(0, 0, spacing, spacing),
                ViewportUnits = BrushMappingMode.Absolute,
                Stretch = Stretch.None,
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Top
            };
            brush.Freeze();
            
            return brush;
        }

        private void BoardBackgroundColor1Border_MouseUp(object sender, MouseButtonEventArgs e) {
            UpdateBoardBackground(0);
        }

        private void BoardBackgroundColor2Border_MouseUp(object sender, MouseButtonEventArgs e) {
            UpdateBoardBackground(1);
        }

        private void BoardBackgroundColor3Border_MouseUp(object sender, MouseButtonEventArgs e) {
            UpdateBoardBackground(2);
        }

        private void BoardBackgroundColor4Border_MouseUp(object sender, MouseButtonEventArgs e) {
            UpdateBoardBackground(3);
        }

        private void BoardBackgroundColor5Border_MouseUp(object sender, MouseButtonEventArgs e) {
            UpdateBoardBackground(4);
        }

        private void BoardBackgroundColor6Border_MouseUp(object sender, MouseButtonEventArgs e) {
            UpdateBoardBackground(5);
        }

        private void BoardBackgroundPatternNone_MouseUp(object sender, MouseButtonEventArgs e) {
            BoardPagesSettingsList[CurrentWhiteboardIndex - 1].BackgroundPattern = BlackboardBackgroundPatternEnum.None;
            ApplyBackgroundPattern();
            UpdateBoardBackgroundPanelDisplayStatus();
        }

        private void BoardBackgroundPatternDots_MouseUp(object sender, MouseButtonEventArgs e) {
            BoardPagesSettingsList[CurrentWhiteboardIndex - 1].BackgroundPattern = BlackboardBackgroundPatternEnum.Dots;
            ApplyBackgroundPattern();
            UpdateBoardBackgroundPanelDisplayStatus();
        }

        private void BoardBackgroundPatternGrid_MouseUp(object sender, MouseButtonEventArgs e) {
            BoardPagesSettingsList[CurrentWhiteboardIndex - 1].BackgroundPattern = BlackboardBackgroundPatternEnum.Grid;
            ApplyBackgroundPattern();
            UpdateBoardBackgroundPanelDisplayStatus();
        }

        private void UpdateBoardBackgroundPanelDisplayStatus() {
            BoardBackgroundColor1Checkbox.Visibility = Visibility.Collapsed;
            BoardBackgroundColor2Checkbox.Visibility = Visibility.Collapsed;
            BoardBackgroundColor3Checkbox.Visibility = Visibility.Collapsed;
            BoardBackgroundColor4Checkbox.Visibility = Visibility.Collapsed;
            BoardBackgroundColor5Checkbox.Visibility = Visibility.Collapsed;
            BoardBackgroundColor6Checkbox.Visibility = Visibility.Collapsed;

            if (currentMode == 1) {
                var index = CurrentWhiteboardIndex - 1;
                var bg = BoardPagesSettingsList[index];
                
                // 更新背景颜色选中状态
                if (bg.BackgroundColor == (BlackboardBackgroundColorEnum)0) BoardBackgroundColor1Checkbox.Visibility = Visibility.Visible;
                else if (bg.BackgroundColor == (BlackboardBackgroundColorEnum)1) BoardBackgroundColor2Checkbox.Visibility = Visibility.Visible;
                else if (bg.BackgroundColor == (BlackboardBackgroundColorEnum)2) BoardBackgroundColor3Checkbox.Visibility = Visibility.Visible;
                else if (bg.BackgroundColor == (BlackboardBackgroundColorEnum)3) BoardBackgroundColor4Checkbox.Visibility = Visibility.Visible;
                else if (bg.BackgroundColor == (BlackboardBackgroundColorEnum)4) BoardBackgroundColor5Checkbox.Visibility = Visibility.Visible;
                else if (bg.BackgroundColor == (BlackboardBackgroundColorEnum)5) BoardBackgroundColor6Checkbox.Visibility = Visibility.Visible;
                
                // 更新稿纸格式选中状态
                try {
                    var noneCheckbox = (Viewbox)FindName("BoardBackgroundPatternNoneCheckbox");
                    var dotsCheckbox = (Viewbox)FindName("BoardBackgroundPatternDotsCheckbox");
                    var gridCheckbox = (Viewbox)FindName("BoardBackgroundPatternGridCheckbox");
                    
                    if (noneCheckbox != null) noneCheckbox.Visibility = Visibility.Collapsed;
                    if (dotsCheckbox != null) dotsCheckbox.Visibility = Visibility.Collapsed;
                    if (gridCheckbox != null) gridCheckbox.Visibility = Visibility.Collapsed;
                    
                    if (bg.BackgroundPattern == BlackboardBackgroundPatternEnum.None && noneCheckbox != null) 
                        noneCheckbox.Visibility = Visibility.Visible;
                    else if (bg.BackgroundPattern == BlackboardBackgroundPatternEnum.Dots && dotsCheckbox != null) 
                        dotsCheckbox.Visibility = Visibility.Visible;
                    else if (bg.BackgroundPattern == BlackboardBackgroundPatternEnum.Grid && gridCheckbox != null) 
                        gridCheckbox.Visibility = Visibility.Visible;
                } catch {
                    // 如果控件不存在则忽略
                }
            }
        }

        #endregion

        private void SaveStrokes(bool isBackupMain = false) {
            if (isBackupMain) {
                var timeMachineHistory = timeMachine.ExportTimeMachineHistory();
                TimeMachineHistories[0] = timeMachineHistory;
                timeMachine.ClearStrokeHistory();
            } else {
                var timeMachineHistory = timeMachine.ExportTimeMachineHistory();
                TimeMachineHistories[CurrentWhiteboardIndex] = timeMachineHistory;
                timeMachine.ClearStrokeHistory();
            }
        }

        private void ClearStrokes(bool isErasedByCode) {
            _currentCommitType = CommitReason.ClearingCanvas;
            if (isErasedByCode) _currentCommitType = CommitReason.CodeInput;
            inkCanvas.Strokes.Clear();
            _currentCommitType = CommitReason.UserInput;
        }

        private void RestoreStrokes(bool isBackupMain = false) {
            try {
                if (TimeMachineHistories[CurrentWhiteboardIndex] == null) return; //防止白板打开后不居中
                if (isBackupMain) {
                    timeMachine.ImportTimeMachineHistory(TimeMachineHistories[0]);
                    foreach (var item in TimeMachineHistories[0]) ApplyHistoryToCanvas(item);
                } else {
                    timeMachine.ImportTimeMachineHistory(TimeMachineHistories[CurrentWhiteboardIndex]);
                    foreach (var item in TimeMachineHistories[CurrentWhiteboardIndex]) ApplyHistoryToCanvas(item);
                }
            }
            catch {
                // ignored
            }
        }

        private async void BtnWhiteBoardPageIndex_Click(object sender, EventArgs e) {
            if (sender == BtnLeftPageListWB) {
                if (BoardBorderLeftPageListView.Visibility == Visibility.Visible) {
                    AnimationsHelper.HideWithSlideAndFade(BoardBorderLeftPageListView);
                } else {
                    AnimationsHelper.HideWithSlideAndFade(BoardBorderRightPageListView);
                    RefreshBlackBoardSidePageListView();
                    AnimationsHelper.ShowWithSlideFromBottomAndFade(BoardBorderLeftPageListView);
                    await Task.Delay(1);
                    ScrollViewToVerticalTop(
                        (ListViewItem)BlackBoardLeftSidePageListView.ItemContainerGenerator.ContainerFromIndex(
                            CurrentWhiteboardIndex - 1), BlackBoardLeftSidePageListScrollViewer);
                }
            } else if (sender == BtnRightPageListWB)
            {
                if (BoardBorderRightPageListView.Visibility == Visibility.Visible) {
                    AnimationsHelper.HideWithSlideAndFade(BoardBorderRightPageListView);
                } else {
                    AnimationsHelper.HideWithSlideAndFade(BoardBorderLeftPageListView);
                    RefreshBlackBoardSidePageListView();
                    AnimationsHelper.ShowWithSlideFromBottomAndFade(BoardBorderRightPageListView);
                    await Task.Delay(1);
                    ScrollViewToVerticalTop(
                        (ListViewItem)BlackBoardRightSidePageListView.ItemContainerGenerator.ContainerFromIndex(
                            CurrentWhiteboardIndex - 1), BlackBoardRightSidePageListScrollViewer);
                }
            }

        }

        private void BtnWhiteBoardSwitchPrevious_Click(object sender, EventArgs e) {
            if (CurrentWhiteboardIndex <= 1) return;

            SaveStrokes();

            ClearStrokes(true);
            CurrentWhiteboardIndex--;

            RestoreStrokes();

            UpdateIndexInfoDisplay();
            UpdateBoardBackground(null);
        }

        private void BtnWhiteBoardSwitchNext_Click(object sender, EventArgs e) {
            if (Settings.Automation.IsAutoSaveStrokesAtClear &&
                inkCanvas.Strokes.Count > Settings.Automation.MinimumAutomationStrokeNumber) SaveScreenshot(true);
            if (CurrentWhiteboardIndex >= WhiteboardTotalCount) {
                BtnWhiteBoardAdd_Click(sender, e);
                return;
            }

            SaveStrokes();

            ClearStrokes(true);
            CurrentWhiteboardIndex++;

            RestoreStrokes();

            UpdateIndexInfoDisplay();
            UpdateBoardBackground(null);
        }

        private void BtnWhiteBoardAdd_Click(object sender, EventArgs e) {
            if (WhiteboardTotalCount >= 99) return;
            if (Settings.Automation.IsAutoSaveStrokesAtClear &&
                inkCanvas.Strokes.Count > Settings.Automation.MinimumAutomationStrokeNumber) SaveScreenshot(true);
            SaveStrokes();
            ClearStrokes(true);

            BoardPagesSettingsList.Insert(CurrentWhiteboardIndex, new BoardPageSettings() {
                BackgroundColor = Settings.Canvas.UseDefaultBackgroundColorForEveryNewAddedBlackboardPage ? Settings.Canvas.BlackboardBackgroundColor : BoardPagesSettingsList[CurrentWhiteboardIndex-1].BackgroundColor,
                BackgroundPattern = Settings.Canvas.UseDefaultBackgroundPatternForEveryNewAddedBlackboardPage ?  Settings.Canvas.BlackboardBackgroundPattern : BoardPagesSettingsList[CurrentWhiteboardIndex - 1].BackgroundPattern,
            });

            WhiteboardTotalCount++;
            CurrentWhiteboardIndex++;

            if (CurrentWhiteboardIndex != WhiteboardTotalCount)
                for (var i = WhiteboardTotalCount; i > CurrentWhiteboardIndex; i--)
                    TimeMachineHistories[i] = TimeMachineHistories[i - 1];

            UpdateIndexInfoDisplay();

            //if (WhiteboardTotalCount >= 99) BtnWhiteBoardAdd.IsEnabled = false;

            if (BlackBoardLeftSidePageListView.Visibility == Visibility.Visible) {
                RefreshBlackBoardSidePageListView();
            }

            UpdateBoardBackground(null);
        }

        private void BtnWhiteBoardDelete_Click(object sender, RoutedEventArgs e) {
            ClearStrokes(true);

            if (CurrentWhiteboardIndex != WhiteboardTotalCount)
                for (var i = CurrentWhiteboardIndex; i <= WhiteboardTotalCount; i++)
                    TimeMachineHistories[i] = TimeMachineHistories[i + 1];
            else
                CurrentWhiteboardIndex--;

            WhiteboardTotalCount--;

            RestoreStrokes();

            UpdateIndexInfoDisplay();

            //if (WhiteboardTotalCount < 99) BtnWhiteBoardAdd.IsEnabled = true;
        }

        private bool _whiteboardModePreviousPageButtonEnabled = false;
        private bool _whiteboardModeNextPageButtonEnabled = false;
        private bool _whiteboardModeNewPageButtonEnabled = false;
        private bool _whiteboardModeNewPageButtonMerged = false;

        public bool WhiteboardModePreviousPageButtonEnabled {
            get => _whiteboardModePreviousPageButtonEnabled;
            set {
                _whiteboardModePreviousPageButtonEnabled = value;
                var geo = new GeometryDrawing[]
                    { BtnLeftWhiteBoardSwitchPreviousGeometry, BtnRightWhiteBoardSwitchPreviousGeometry };
                var label = new TextBlock[]
                    { BtnLeftWhiteBoardSwitchPreviousLabel, BtnRightWhiteBoardSwitchPreviousLabel };
                var border = new Border[]
                    { BtnWhiteBoardSwitchPreviousL, BtnWhiteBoardSwitchPreviousR };
                foreach (var gd in geo)
                    gd.Brush = new SolidColorBrush(Color.FromArgb((byte)(value ? 255 : 127), 24, 24, 27));
                foreach (var tb in label) tb.Opacity = value ? 1 : 0.5;
                foreach (var bd in border) bd.IsHitTestVisible = value;
            }
        }

        public bool WhiteboardModeNextPageButtonEnabled {
            get => _whiteboardModeNextPageButtonEnabled;
            set {
                _whiteboardModeNextPageButtonEnabled = value;
                var geo = new GeometryDrawing[]
                    { BtnLeftWhiteBoardSwitchNextGeometry, BtnRightWhiteBoardSwitchNextGeometry };
                var label = new TextBlock[]
                    { BtnLeftWhiteBoardSwitchNextLabel, BtnRightWhiteBoardSwitchNextLabel };
                var border = new Border[]
                    { BtnWhiteBoardSwitchNextL, BtnWhiteBoardSwitchNextR };
                foreach (var gd in geo)
                    gd.Brush = new SolidColorBrush(Color.FromArgb((byte)(value ? 255 : 127), 24, 24, 27));
                foreach (var tb in label) tb.Opacity = value ? 1 : 0.5;
                foreach (var bd in border) bd.IsHitTestVisible = value;
            }
        }

        public bool WhiteboardModeNewPageButtonEnabled {
            get => _whiteboardModeNewPageButtonEnabled;
            set {
                _whiteboardModeNewPageButtonEnabled = value;
                var geo = new GeometryDrawing[]
                    { BtnWhiteboardAddGeometryLeft, BtnWhiteboardAddGeometryRight, BtnWhiteboardAddGeometryRightSecondary };
                var label = new TextBlock[]
                    { BtnWhiteboardAddTextBlockLeft, BtnWhiteboardAddTextBlockRight, BtnWhiteboardAddTextBlockRightSecondary };
                var border = new Border[]
                    { BtnWhiteboardAddLeft, BtnWhiteboardAddRight, BtnWhiteboardAddRightSecondary };
                foreach (var gd in geo)
                    gd.Brush = new SolidColorBrush(Color.FromArgb((byte)(value ? 255 : 127), 24, 24, 27));
                foreach (var tb in label) tb.Opacity = value ? 1 : 0.5;
                foreach (var bd in border) bd.IsHitTestVisible = value;
            }
        }

        public bool WhiteboardModeNewPageButtonMerged {
            get => _whiteboardModeNewPageButtonMerged;
            set {
                _whiteboardModeNewPageButtonMerged = value;
                BtnWhiteBoardSwitchNextL.Visibility = value ? Visibility.Collapsed : Visibility.Visible;
                BtnLeftPageListWB.CornerRadius = value ? new CornerRadius(0, 5, 5, 0) : new CornerRadius(0);
                BtnWhiteBoardSwitchNextR.Visibility = value ? Visibility.Collapsed : Visibility.Visible;
                BtnRightPageListWB.CornerRadius = value ? new CornerRadius(0, 5, 5, 0) : new CornerRadius(0);
                BtnWhiteboardAddRightSecondary.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void UpdateIndexInfoDisplay() {
            BtnLeftPageListWBTextCount.Text =
                $"{CurrentWhiteboardIndex}/{WhiteboardTotalCount}";
            BtnRightPageListWBTextCount.Text =
                $"{CurrentWhiteboardIndex}/{WhiteboardTotalCount}";

            WhiteboardModePreviousPageButtonEnabled = CurrentWhiteboardIndex > 1;
            WhiteboardModeNextPageButtonEnabled = CurrentWhiteboardIndex < WhiteboardTotalCount;
            WhiteboardModeNewPageButtonEnabled = WhiteboardTotalCount < 99;
            WhiteboardModeNewPageButtonMerged = CurrentWhiteboardIndex == WhiteboardTotalCount;
            
            // 更新 ViewModel 中的白板页面状态
            try {
                ViewModel?.UpdateWhiteboardPageIndex(CurrentWhiteboardIndex, WhiteboardTotalCount);
            }
            catch (Exception ex) {
                LogHelper.WriteLogToFile("Failed to update ViewModel whiteboard page index: " + ex.Message, LogHelper.LogType.Error);
            }
        }
    }
}