using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Ink_Canvas.Popups
{
    /// <summary>
    /// SelectionCaptureOverlay.xaml 的交互逻辑
    /// 用于选区截图的全屏透明覆盖层
    /// </summary>
    public partial class SelectionCaptureOverlay : Window
    {
        private Point _startPoint;
        private Point _currentPoint;
        private bool _isSelecting;

        /// <summary>
        /// 选区完成事件，返回选区矩形（屏幕坐标）
        /// </summary>
        public event EventHandler<Rect> SelectionCompleted;

        /// <summary>
        /// 选区取消事件
        /// </summary>
        public event EventHandler SelectionCancelled;

        public SelectionCaptureOverlay()
        {
            InitializeComponent();
            
            // 初始化覆盖层
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // 设置窗口大小为整个屏幕
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            
            Width = screenWidth;
            Height = screenHeight;
            Left = 0;
            Top = 0;

            // 初始化暗色覆盖层
            UpdateDarkOverlay(new Rect(0, 0, 0, 0));
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                CancelSelection();
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _startPoint = e.GetPosition(this);
                _currentPoint = _startPoint;
                _isSelecting = true;

                // 隐藏说明文字
                InstructionsOverlay.Visibility = Visibility.Collapsed;

                // 显示选区边框
                SelectionBorder.Visibility = Visibility.Visible;
                DimensionTooltip.Visibility = Visibility.Visible;

                // 捕获鼠标
                CaptureMouse();

                UpdateSelectionVisuals();
            }
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isSelecting)
            {
                _currentPoint = e.GetPosition(this);
                UpdateSelectionVisuals();
            }
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isSelecting && e.LeftButton == MouseButtonState.Released)
            {
                _isSelecting = false;
                ReleaseMouseCapture();

                var selectionRect = GetSelectionRect();

                // 检查选区是否有效（宽高都大于0）
                if (selectionRect.Width > 0 && selectionRect.Height > 0)
                {
                    // 触发选区完成事件
                    SelectionCompleted?.Invoke(this, selectionRect);
                }
                else
                {
                    // 选区无效，取消操作
                    CancelSelection();
                }
            }
        }

        private void UpdateSelectionVisuals()
        {
            var rect = GetSelectionRect();

            // 更新选区边框位置和大小
            SelectionBorder.Margin = new Thickness(rect.X, rect.Y, 0, 0);
            SelectionBorder.Width = rect.Width;
            SelectionBorder.Height = rect.Height;

            // 更新暗色覆盖层（在选区外显示半透明黑色）
            UpdateDarkOverlay(rect);

            // 更新尺寸显示
            UpdateDimensionTooltip(rect);
        }

        private Rect GetSelectionRect()
        {
            double x = Math.Min(_startPoint.X, _currentPoint.X);
            double y = Math.Min(_startPoint.Y, _currentPoint.Y);
            double width = Math.Abs(_currentPoint.X - _startPoint.X);
            double height = Math.Abs(_currentPoint.Y - _startPoint.Y);

            return new Rect(x, y, width, height);
        }

        private void UpdateDarkOverlay(Rect selectionRect)
        {
            var screenWidth = ActualWidth > 0 ? ActualWidth : SystemParameters.PrimaryScreenWidth;
            var screenHeight = ActualHeight > 0 ? ActualHeight : SystemParameters.PrimaryScreenHeight;

            // 创建整个屏幕的矩形
            var screenGeometry = new RectangleGeometry(new Rect(0, 0, screenWidth, screenHeight));

            if (selectionRect.Width > 0 && selectionRect.Height > 0)
            {
                // 创建选区矩形（作为"洞"）
                var selectionGeometry = new RectangleGeometry(selectionRect);

                // 使用 CombinedGeometry 创建带洞的覆盖层
                var combinedGeometry = new CombinedGeometry(
                    GeometryCombineMode.Exclude,
                    screenGeometry,
                    selectionGeometry);

                DarkOverlay.Data = combinedGeometry;
            }
            else
            {
                // 没有选区时，显示整个暗色覆盖层
                DarkOverlay.Data = screenGeometry;
            }
        }

        private void UpdateDimensionTooltip(Rect rect)
        {
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                DimensionTooltip.Visibility = Visibility.Collapsed;
                return;
            }

            // 获取 DPI 缩放比例
            var dpiScale = GetDpiScale();
            
            // 计算实际像素尺寸
            int actualWidth = (int)Math.Round(rect.Width * dpiScale);
            int actualHeight = (int)Math.Round(rect.Height * dpiScale);

            DimensionText.Text = $"{actualWidth} × {actualHeight}";

            // 定位尺寸提示框（在选区下方或上方）
            double tooltipX = rect.X;
            double tooltipY = rect.Y + rect.Height + 8;

            // 如果下方空间不足，显示在上方
            if (tooltipY + 30 > ActualHeight)
            {
                tooltipY = rect.Y - 30;
            }

            // 确保不超出屏幕左边界
            if (tooltipX < 0) tooltipX = 0;

            DimensionTooltip.Margin = new Thickness(tooltipX, tooltipY, 0, 0);
            DimensionTooltip.Visibility = Visibility.Visible;
        }

        private double GetDpiScale()
        {
            var source = PresentationSource.FromVisual(this);
            if (source?.CompositionTarget != null)
            {
                return source.CompositionTarget.TransformToDevice.M11;
            }
            return 1.0;
        }

        private void CancelSelection()
        {
            _isSelecting = false;
            if (IsMouseCaptured)
            {
                ReleaseMouseCapture();
            }

            SelectionCancelled?.Invoke(this, EventArgs.Empty);
            Close();
        }

        /// <summary>
        /// 获取选区的屏幕坐标矩形（考虑 DPI 缩放）
        /// </summary>
        public Rect GetScreenSelectionRect()
        {
            var rect = GetSelectionRect();
            var dpiScale = GetDpiScale();

            return new Rect(
                rect.X * dpiScale,
                rect.Y * dpiScale,
                rect.Width * dpiScale,
                rect.Height * dpiScale);
        }
    }
}
