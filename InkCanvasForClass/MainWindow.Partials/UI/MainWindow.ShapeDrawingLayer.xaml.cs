using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Ink_Canvas.Core;
using Ink_Canvas.Services;
using Ink_Canvas.ShapeDrawing.Core;

namespace Ink_Canvas {
    public partial class ShapeDrawingLayer : UserControl {

        // 缓存画刷，避免重复创建
        private static readonly SolidColorBrush TransparentClickableBrush = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));
        private static readonly SolidColorBrush ToolButtonPressedBrush = new SolidColorBrush(Color.FromRgb(228, 228, 231));
        private static readonly SolidColorBrush TransparentBrush = new SolidColorBrush(Colors.Transparent);

        static ShapeDrawingLayer() {
            // 冻结静态画刷以提高性能
            TransparentClickableBrush.Freeze();
            ToolButtonPressedBrush.Freeze();
            TransparentBrush.Freeze();
        }

        // 网格辅助线设置
        private bool _isGridEnabled = false;
        private double _gridSize = Constants.GridDefaultSize; // 网格大小（像素）

        // 顶点吸附设置
        private bool _isSnapEnabled = false;
        private double _snapDistance = Constants.SnapDefaultDistance; // 吸附距离（像素）

        // 多指绘制设置
        private bool _isMultiTouchEnabled = false;

        public ShapeDrawingLayer() {
            InitializeComponent();

            // ToolbarMoveHandle
            ToolbarMoveHandle.MouseDown += ToolbarMoveHandle_MouseDown;
            ToolbarMoveHandle.MouseUp += ToolbarMoveHandle_MouseUp;
            ToolbarMoveHandle.MouseMove += ToolbarMoveHandle_MouseMove;
            UpdateToolbarPosition(ToolbarNowPosition);

            // Update ToolBtns
            ToolButtons = new Border[] {
                CursorButton,
                UndoButton,
                RedoButton,
                ClearButton,
                GridLineButton,
                SnapButton,
                MultiPointButton,
                InfoButton,
                MoreButton,
            };
            foreach (var tb in ToolButtons) {
                tb.MouseDown += ToolButton_MouseDown;
                tb.MouseUp += ToolButton_MouseUp;
                tb.MouseLeave += ToolButton_MouseLeave;
            }

            // 绑定工具按钮点击事件
            CursorButton.MouseUp += CursorButton_Click;
            UndoButton.MouseUp += UndoButton_Click;
            RedoButton.MouseUp += RedoButton_Click;
            ClearButton.MouseUp += ClearButton_Click;
            GridLineButton.MouseUp += GridLineButton_Click;
            SnapButton.MouseUp += SnapButton_Click;
            MultiPointButton.MouseUp += MultiPointButton_Click;
            InfoButton.MouseUp += InfoButton_Click;
            MoreButton.MouseUp += MoreButton_Click;

            Toolbar.Visibility = Visibility.Collapsed;
            AngleTooltip.Visibility = Visibility.Collapsed;
            LengthTooltip.Visibility = Visibility.Collapsed;

            FullscreenGrid.MouseDown += FullscreenGrid_MouseDown;
            FullscreenGrid.MouseUp += FullscreenGrid_MouseUp;
            FullscreenGrid.MouseMove += FullscreenGrid_MouseMove;
        }

        private Point CaculateCenteredToolbarPosition() {
            var aw = Toolbar.Width;
            var ah = Toolbar.Height;
            var left = (MainWindow.ActualWidth - aw) / 2;
            var top = MainWindow.ActualHeight - ah - 128;
            return new Point(left, top);
        }

        public MainWindow MainWindow { get; set; }

        public Border[] ToolButtons = new Border[] { };
        public Point ToolbarNowPosition = new Point(0, 0);
        public bool IsToolbarMoveHandleDown = false;
        public Point MouseDownPointInHandle;
        public Border ToolButtonMouseDownBorder = null;

        public void UpdateToolbarPosition(Point? position) {
            if (position == null) {
                Toolbar.RenderTransform = null;
                return;
            }
            Toolbar.RenderTransform = new TranslateTransform(((Point)position).X, ((Point)position).Y);
        }

        private void ToolbarMoveHandle_MouseDown(object sender, MouseButtonEventArgs e) {
            if (IsToolbarMoveHandleDown) return;
            MouseDownPointInHandle = FullscreenGrid.TranslatePoint(e.GetPosition(null),ToolbarMoveHandle);
            IsToolbarMoveHandleDown = true;
            ToolbarMoveHandle.CaptureMouse();
        }

        private void ToolbarMoveHandle_MouseUp(object sender, MouseButtonEventArgs e) {
            if (!IsToolbarMoveHandleDown) return;
            IsToolbarMoveHandleDown = false;
            ToolbarMoveHandle.ReleaseMouseCapture();
        }

        private void ToolbarMoveHandle_MouseMove(object sender, MouseEventArgs e) {
            if (!IsToolbarMoveHandleDown) return;
            var ptInScreen = e.GetPosition(null);
            ToolbarNowPosition = new Point(ptInScreen.X - MouseDownPointInHandle.X, ptInScreen.Y - MouseDownPointInHandle.Y);
            UpdateToolbarPosition(ToolbarNowPosition);
        }

        private MainWindow.ShapeDrawingType? _shapeType;
        private readonly Ink_Canvas.Services.ShapeDrawingService _shapeDrawingService = Ink_Canvas.Services.ShapeDrawingService.Instance;

        public bool IsInShapeDrawingMode {
            get => _shapeType != null;
        }

        public void StartShapeDrawing(MainWindow.ShapeDrawingType type, string name) {
            _shapeType = type;
            FullscreenGrid.Background = TransparentClickableBrush;
            FullscreenGrid.Visibility = Visibility.Visible;
            Toolbar.Visibility = Visibility.Visible;
            var pt = CaculateCenteredToolbarPosition();
            ToolbarNowPosition = pt;
            UpdateToolbarPosition(pt);
            ShapeDrawingTypeText.Text = name ?? "未知形状";
            PenSizeText.Text = $"{(MainWindow.inkCanvas.DefaultDrawingAttributes.Width + MainWindow.inkCanvas.DefaultDrawingAttributes.Height) / 2} 像素";
        }

        private void DoneButtonClicked(object sender, RoutedEventArgs e) {
            EndShapeDrawing();
        }

        public void EndShapeDrawing() {
            _shapeType = null;
            FullscreenGrid.Background = null;
            FullscreenGrid.Visibility = Visibility.Collapsed;
            Toolbar.Visibility = Visibility.Collapsed;
        }

        private void ToolButton_MouseDown(object sender, MouseButtonEventArgs e) {
            if (ToolButtonMouseDownBorder != null) return;
            ToolButtonMouseDownBorder = (Border)sender;
            ToolButtonMouseDownBorder.Background = ToolButtonPressedBrush;
        }

        private void ToolButton_MouseUp(object sender, MouseButtonEventArgs e) {
            if (ToolButtonMouseDownBorder == null || ToolButtonMouseDownBorder != sender) return;

            // 不在这里调用 MouseLeave，让具体的按钮事件处理
        }

        private void ToolButton_MouseLeave(object sender, MouseEventArgs e) {
            if (ToolButtonMouseDownBorder == null || ToolButtonMouseDownBorder != sender) return;
            ToolButtonMouseDownBorder.Background = TransparentBrush;
            ToolButtonMouseDownBorder = null;
        }

        private bool isFullscreenGridDown = false;
        public PointCollection points = new PointCollection();

        private void FullscreenGrid_MouseDown(object sender, MouseButtonEventArgs e) {
            if (isFullscreenGridDown) return;
            points.Clear();

            var point = e.GetPosition(null);
            // 应用吸附
            if (_isSnapEnabled) {
                point = ApplySnapping(point);
            }

            points.Add(point);
            isFullscreenGridDown = true;
            FullscreenGrid.CaptureMouse();
        }

        private void FullscreenGrid_MouseUp(object sender, MouseButtonEventArgs e) {
            if (!isFullscreenGridDown) return;
            FullscreenGrid.ReleaseMouseCapture();
            isFullscreenGridDown = false;
            if (_shapeType == null) return;

            if (points.Count >= 2)
            {
                // Convert MainWindow.ShapeDrawingType to ShapeDrawing.Core.ShapeDrawingType
                var coreShapeType = (Ink_Canvas.ShapeDrawing.Core.ShapeDrawingType)_shapeType.Value;
                var strokes = _shapeDrawingService.CreateShape(points[0], points[1], coreShapeType, MainWindow.inkCanvas.DefaultDrawingAttributes);
                MainWindow.inkCanvas.Strokes.Add(strokes);
            }
            points.Clear();
            AngleTooltip.Visibility = Visibility.Collapsed;
            LengthTooltip.Visibility = Visibility.Collapsed;
        }

        private void FullscreenGrid_MouseMove(object sender, MouseEventArgs e) {
            if (!isFullscreenGridDown) return;
            if (_shapeType == null) return;

            var point = e.GetPosition(null);
            // 应用吸附
            if (_isSnapEnabled) {
                point = ApplySnapping(point);
            }

            if (points.Count >= 2)
                points[1] = point;
            else
                points.Add(point);

            using (DrawingContext dc = DrawingVisualCanvas.DrawingVisual.RenderOpen()) {
                // 绘制网格辅助线
                if (_isGridEnabled) {
                    DrawGrid(dc);
                }

                if (points.Count >= 2) {
                    // Use the new shape drawing service for preview
                    var coreShapeType = (Ink_Canvas.ShapeDrawing.Core.ShapeDrawingType)_shapeType.Value;
                    var previewStrokes = _shapeDrawingService.CreateShape(points[0], points[1], coreShapeType, MainWindow.inkCanvas.DefaultDrawingAttributes);
                    previewStrokes.Draw(dc);

                    // 预计算差值，避免重复访问 points 集合
                    double dx = points[1].X - points[0].X;
                    double dy = points[1].Y - points[0].Y;

                    // 只对线条类型显示角度和长度提示
                    if (_shapeType == MainWindow.ShapeDrawingType.Line ||
                        _shapeType == MainWindow.ShapeDrawingType.DashedLine ||
                        _shapeType == MainWindow.ShapeDrawingType.DottedLine ||
                        _shapeType == MainWindow.ShapeDrawingType.ArrowOneSide ||
                        _shapeType == MainWindow.ShapeDrawingType.ArrowTwoSide) {

                        var angle = ShapeDrawingHelper.CaculateRotateAngleByGivenTwoPoints(points[0], points[1]);
                        if (AngleTooltip.Visibility == Visibility.Collapsed) AngleTooltip.Visibility = Visibility.Visible;
                        AngleText.Text = $"{angle}°";

                        if (LengthTooltip.Visibility == Visibility.Collapsed) LengthTooltip.Visibility = Visibility.Visible;
                        // 使用乘法代替 Math.Pow，性能更好
                        double length = Math.Sqrt(dx * dx + dy * dy);
                        LengthText.Text = $"{Math.Round(length, 2)} 像素";
                    } else {
                        // 其他形状类型显示宽高信息
                        if (LengthTooltip.Visibility == Visibility.Collapsed) LengthTooltip.Visibility = Visibility.Visible;
                        double width = Math.Abs(dx);
                        double height = Math.Abs(dy);
                        LengthText.Text = $"{Math.Round(width, 0)} × {Math.Round(height, 0)} 像素";
                        AngleTooltip.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        #region 网格辅助线功能

        /// <summary>
        /// 切换网格辅助线显示
        /// </summary>
        private void GridLineButton_Click(object sender, MouseButtonEventArgs e) {
            if (ToolButtonMouseDownBorder == null || ToolButtonMouseDownBorder != sender) return;

            _isGridEnabled = !_isGridEnabled;

            // 更新按钮状态
            UpdateToolButtonState(GridLineButton, _isGridEnabled);

            ToolButton_MouseLeave(sender, null);

            // 刷新显示
            if (_isGridEnabled && !isFullscreenGridDown) {
                using (DrawingContext dc = DrawingVisualCanvas.DrawingVisual.RenderOpen()) {
                    DrawGrid(dc);
                }
            } else if (!_isGridEnabled) {
                // 清空绘制
            }
        }

        /// <summary>
        /// 绘制网格辅助线
        /// </summary>
        private void DrawGrid(DrawingContext dc) {
            var pen = new Pen(new SolidColorBrush(Constants.GridLineColor), 1);
            pen.Freeze();

            double width = ActualWidth;
            double height = ActualHeight;

            // 绘制垂直线
            for (double x = 0; x < width; x += _gridSize) {
                dc.DrawLine(pen, new Point(x, 0), new Point(x, height));
            }

            // 绘制水平线
            for (double y = 0; y < height; y += _gridSize) {
                dc.DrawLine(pen, new Point(0, y), new Point(width, y));
            }
        }

        #endregion

        #region 顶点吸附功能

        /// <summary>
        /// 切换顶点吸附功能
        /// </summary>
        private void SnapButton_Click(object sender, MouseButtonEventArgs e) {
            if (ToolButtonMouseDownBorder == null || ToolButtonMouseDownBorder != sender) return;

            _isSnapEnabled = !_isSnapEnabled;

            // 更新按钮状态
            UpdateToolButtonState(SnapButton, _isSnapEnabled);

            ToolButton_MouseLeave(sender, null);
        }

        /// <summary>
        /// 应用吸附到网格点
        /// </summary>
        private Point ApplySnapping(Point point) {
            if (!_isSnapEnabled) return point;

            // 吸附到网格点
            if (_isGridEnabled) {
                double snappedX = Math.Round(point.X / _gridSize) * _gridSize;
                double snappedY = Math.Round(point.Y / _gridSize) * _gridSize;
                return new Point(snappedX, snappedY);
            }

            // 吸附到现有笔画的端点
            if (MainWindow?.inkCanvas?.Strokes != null) {
                Point? nearestPoint = FindNearestStrokePoint(point);
                if (nearestPoint.HasValue) {
                    double dx = point.X - nearestPoint.Value.X;
                    double dy = point.Y - nearestPoint.Value.Y;
                    double distance = Math.Sqrt(dx * dx + dy * dy);

                    if (distance <= _snapDistance) {
                        return nearestPoint.Value;
                    }
                }
            }

            return point;
        }

        /// <summary>
        /// 查找最近的笔画端点
        /// </summary>
        private Point? FindNearestStrokePoint(Point point) {
            Point? nearestPoint = null;
            double minDistance = double.MaxValue;

            foreach (var stroke in MainWindow.inkCanvas.Strokes) {
                var points = stroke.StylusPoints;
                if (points.Count == 0) continue;

                // 检查起点
                var startPoint = new Point(points[0].X, points[0].Y);
                double startDistance = Math.Sqrt(
                    (point.X - startPoint.X) * (point.X - startPoint.X) +
                    (point.Y - startPoint.Y) * (point.Y - startPoint.Y)
                );

                if (startDistance < minDistance) {
                    minDistance = startDistance;
                    nearestPoint = startPoint;
                }

                // 检查终点
                var endPoint = new Point(points[points.Count - 1].X, points[points.Count - 1].Y);
                double endDistance = Math.Sqrt(
                    (point.X - endPoint.X) * (point.X - endPoint.X) +
                    (point.Y - endPoint.Y) * (point.Y - endPoint.Y)
                );

                if (endDistance < minDistance) {
                    minDistance = endDistance;
                    nearestPoint = endPoint;
                }
            }

            return minDistance <= _snapDistance ? nearestPoint : null;
        }

        #endregion

        #region 多指绘制功能

        /// <summary>
        /// 切换多指绘制功能
        /// </summary>
        private void MultiPointButton_Click(object sender, MouseButtonEventArgs e) {
            if (ToolButtonMouseDownBorder == null || ToolButtonMouseDownBorder != sender) return;

            _isMultiTouchEnabled = !_isMultiTouchEnabled;

            // 更新按钮状态
            UpdateToolButtonState(MultiPointButton, _isMultiTouchEnabled);

            ToolButton_MouseLeave(sender, null);

            // 启用或禁用多点触控
            if (MainWindow?.inkCanvas != null) {
                MainWindow.inkCanvas.IsManipulationEnabled = _isMultiTouchEnabled;
            }
        }

        #endregion

        #region 工具按钮状态管理

        /// <summary>
        /// 更新工具按钮的激活状态
        /// </summary>
        private void UpdateToolButtonState(Border button, bool isActive) {
            if (isActive) {
                button.Background = new SolidColorBrush(Color.FromRgb(59, 130, 246)); // 蓝色背景表示激活
                // 更新图标颜色为白色
                if (button.Child is Grid grid && grid.Children.Count > 0) {
                    foreach (var child in grid.Children) {
                        if (child is Image) {
                            // 这里可以更新图标颜色，但需要克隆DrawingImage
                            // 为简化实现，暂时只改变背景色
                        }
                    }
                }
            } else {
                button.Background = TransparentBrush;
            }
        }

        #endregion

        #region 其他工具按钮功能

        /// <summary>
        /// 光标按钮 - 退出绘制模式
        /// </summary>
        private void CursorButton_Click(object sender, MouseButtonEventArgs e) {
            if (ToolButtonMouseDownBorder == null || ToolButtonMouseDownBorder != sender) return;

            ToolButton_MouseLeave(sender, null);

            // 退出绘制模式
            EndShapeDrawing();
        }

        /// <summary>
        /// 撤销按钮
        /// </summary>
        private void UndoButton_Click(object sender, MouseButtonEventArgs e) {
            if (ToolButtonMouseDownBorder == null || ToolButtonMouseDownBorder != sender) return;

            ToolButton_MouseLeave(sender, null);

            // 调用 MainWindow 的撤销方法
            if (MainWindow != null) {
                MainWindow.BtnUndo_Click(null, null);
            }
        }

        /// <summary>
        /// 重做按钮
        /// </summary>
        private void RedoButton_Click(object sender, MouseButtonEventArgs e) {
            if (ToolButtonMouseDownBorder == null || ToolButtonMouseDownBorder != sender) return;

            ToolButton_MouseLeave(sender, null);

            // 调用 MainWindow 的重做方法
            if (MainWindow != null) {
                MainWindow.BtnRedo_Click(null, null);
            }
        }

        /// <summary>
        /// 清空按钮
        /// </summary>
        private void ClearButton_Click(object sender, MouseButtonEventArgs e) {
            if (ToolButtonMouseDownBorder == null || ToolButtonMouseDownBorder != sender) return;

            ToolButton_MouseLeave(sender, null);

            // 调用 MainWindow 的清空方法
            if (MainWindow != null) {
                MainWindow.BtnClear_Click(null, null);
            }
        }

        /// <summary>
        /// 信息按钮 - 显示帮助信息
        /// </summary>
        private void InfoButton_Click(object sender, MouseButtonEventArgs e) {
            if (ToolButtonMouseDownBorder == null || ToolButtonMouseDownBorder != sender) return;

            ToolButton_MouseLeave(sender, null);

            // 显示帮助信息
            ShowHelpInfo();
        }

        /// <summary>
        /// 更多按钮 - 显示更多选项
        /// </summary>
        private void MoreButton_Click(object sender, MouseButtonEventArgs e) {
            if (ToolButtonMouseDownBorder == null || ToolButtonMouseDownBorder != sender) return;

            ToolButton_MouseLeave(sender, null);

            // 显示更多选项菜单
            ShowMoreOptions();
        }

        /// <summary>
        /// 显示帮助信息
        /// </summary>
        private void ShowHelpInfo() {
            var helpText = @"几何图形绘制工具

快捷键：
• 网格辅助线：显示网格帮助精确绘制
• 顶点吸附：自动吸附到网格点或笔画端点
• 多指绘制：启用多点触控支持

使用方法：
1. 点击对应按钮启用功能
2. 开始绘制几何图形
3. 激活的按钮显示蓝色背景

提示：
• 网格大小：20像素
• 吸附距离：15像素
• 按钮可以组合使用";

            System.Windows.MessageBox.Show(
                helpText,
                "几何图形绘制帮助",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        /// <summary>
        /// 显示更多选项
        /// </summary>
        private void ShowMoreOptions() {
            // 创建上下文菜单
            var contextMenu = new System.Windows.Controls.ContextMenu();

            // 网格大小设置
            var gridSizeItem = new System.Windows.Controls.MenuItem {
                Header = "网格大小"
            };

            foreach (var size in new[] { 10, 20, 30, 40, 50 }) {
                var sizeItem = new System.Windows.Controls.MenuItem {
                    Header = $"{size} 像素",
                    IsCheckable = true,
                    IsChecked = Math.Abs(_gridSize - size) < 0.1
                };
                var capturedSize = size;
                sizeItem.Click += (s, e) => {
                    _gridSize = capturedSize;
                    // 如果网格已启用，刷新显示
                    if (_isGridEnabled && !isFullscreenGridDown) {
                        using (DrawingContext dc = DrawingVisualCanvas.DrawingVisual.RenderOpen()) {
                            DrawGrid(dc);
                        }
                    }
                };
                gridSizeItem.Items.Add(sizeItem);
            }
            contextMenu.Items.Add(gridSizeItem);

            // 吸附距离设置
            var snapDistanceItem = new System.Windows.Controls.MenuItem {
                Header = "吸附距离"
            };

            foreach (var distance in new[] { 10, 15, 20, 25, 30 }) {
                var distItem = new System.Windows.Controls.MenuItem {
                    Header = $"{distance} 像素",
                    IsCheckable = true,
                    IsChecked = Math.Abs(_snapDistance - distance) < 0.1
                };
                var capturedDistance = distance;
                distItem.Click += (s, e) => {
                    _snapDistance = capturedDistance;
                };
                snapDistanceItem.Items.Add(distItem);
            }
            contextMenu.Items.Add(snapDistanceItem);

            // 分隔符
            contextMenu.Items.Add(new System.Windows.Controls.Separator());

            // 重置所有设置
            var resetItem = new System.Windows.Controls.MenuItem {
                Header = "重置所有设置"
            };
            resetItem.Click += (s, e) => {
                _gridSize = Constants.GridDefaultSize;
                _snapDistance = Constants.SnapDefaultDistance;
                _isGridEnabled = false;
                _isSnapEnabled = false;
                _isMultiTouchEnabled = false;

                // 更新按钮状态
                UpdateToolButtonState(GridLineButton, false);
                UpdateToolButtonState(SnapButton, false);
                UpdateToolButtonState(MultiPointButton, false);

                // 清空绘制
            };
            contextMenu.Items.Add(resetItem);

            // 显示菜单
            contextMenu.PlacementTarget = MoreButton;
            contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            contextMenu.IsOpen = true;
        }

        #endregion
    }
}
