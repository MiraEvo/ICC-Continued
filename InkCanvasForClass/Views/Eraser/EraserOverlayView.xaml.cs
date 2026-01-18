using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Ink_Canvas.Helpers;

namespace Ink_Canvas.Views.Eraser
{
    /// <summary>
    /// EraserOverlayView.xaml 的交互逻辑
    /// 提供橡皮擦覆盖层，用于显示橡皮擦光标和处理擦除操作
    /// </summary>
    public partial class EraserOverlayView : UserControl
    {
        #region Events

        /// <summary>
        /// 覆盖层加载完成事件
        /// </summary>
        public event EventHandler OverlayLoaded;

        /// <summary>
        /// 鼠标移动事件（用于更新橡皮擦位置）
        /// </summary>
        public event EventHandler<EraserMouseEventArgs> EraserMouseMove;

        /// <summary>
        /// 鼠标按下事件
        /// </summary>
        public event EventHandler<EraserMouseEventArgs> EraserMouseDown;

        /// <summary>
        /// 鼠标释放事件
        /// </summary>
        public event EventHandler<EraserMouseEventArgs> EraserMouseUp;

        #endregion

        public EraserOverlayView()
        {
            InitializeComponent();
            
            // 订阅鼠标事件
            EraserOverlayBorder.MouseMove += OnEraserMouseMove;
            EraserOverlayBorder.MouseDown += OnEraserMouseDown;
            EraserOverlayBorder.MouseUp += OnEraserMouseUp;
        }

        #region Dependency Properties

        public static readonly DependencyProperty EraserOverlayVisibilityProperty =
            DependencyProperty.Register(nameof(EraserOverlayVisibility), typeof(Visibility), typeof(EraserOverlayView),
                new PropertyMetadata(Visibility.Collapsed));

        public Visibility EraserOverlayVisibility
        {
            get => (Visibility)GetValue(EraserOverlayVisibilityProperty);
            set => SetValue(EraserOverlayVisibilityProperty, value);
        }

        public static readonly DependencyProperty EraserSizeProperty =
            DependencyProperty.Register(nameof(EraserSize), typeof(double), typeof(EraserOverlayView),
                new PropertyMetadata(30.0));

        public double EraserSize
        {
            get => (double)GetValue(EraserSizeProperty);
            set => SetValue(EraserSizeProperty, value);
        }

        public static readonly DependencyProperty EraserColorProperty =
            DependencyProperty.Register(nameof(EraserColor), typeof(Color), typeof(EraserOverlayView),
                new PropertyMetadata(Colors.Gray));

        public Color EraserColor
        {
            get => (Color)GetValue(EraserColorProperty);
            set => SetValue(EraserColorProperty, value);
        }

        #endregion

        #region Event Handlers

        private void EraserOverlay_Loaded(object sender, RoutedEventArgs e)
        {
            OverlayLoaded?.Invoke(this, EventArgs.Empty);
        }

        private void OnEraserMouseMove(object sender, MouseEventArgs e)
        {
            var position = e.GetPosition(this);
            EraserMouseMove?.Invoke(this, new EraserMouseEventArgs(position, e));
            
            // 更新橡皮擦光标位置
            UpdateEraserCursor(position);
        }

        private void OnEraserMouseDown(object sender, MouseButtonEventArgs e)
        {
            var position = e.GetPosition(this);
            EraserMouseDown?.Invoke(this, new EraserMouseEventArgs(position, e));
        }

        private void OnEraserMouseUp(object sender, MouseButtonEventArgs e)
        {
            var position = e.GetPosition(this);
            EraserMouseUp?.Invoke(this, new EraserMouseEventArgs(position, e));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 显示橡皮擦覆盖层
        /// </summary>
        public void Show()
        {
            EraserOverlayVisibility = Visibility.Visible;
        }

        /// <summary>
        /// 隐藏橡皮擦覆盖层
        /// </summary>
        public void Hide()
        {
            EraserOverlayVisibility = Visibility.Collapsed;
            ClearEraserCursor();
        }

        /// <summary>
        /// 获取绘图画布
        /// </summary>
        public DrawingVisualCanvas GetDrawingVisualCanvas()
        {
            return EraserOverlay_DrawingVisual;
        }

        /// <summary>
        /// 更新橡皮擦光标位置
        /// </summary>
        public void UpdateEraserCursor(Point position)
        {
            var drawingVisual = EraserOverlay_DrawingVisual.DrawingVisual;
            using (var dc = drawingVisual.RenderOpen())
            {
                // 绘制橡皮擦光标（圆形）
                var brush = new SolidColorBrush(EraserColor);
                brush.Opacity = 0.5;
                var pen = new Pen(Brushes.Gray, 1);
                
                dc.DrawEllipse(brush, pen, position, EraserSize / 2, EraserSize / 2);
            }
        }

        /// <summary>
        /// 清除橡皮擦光标
        /// </summary>
        public void ClearEraserCursor()
        {
            var drawingVisual = EraserOverlay_DrawingVisual.DrawingVisual;
            // 清空绘图内容：打开绘图上下文但不进行任何绘制，从而清除之前的内容
            using (var dc = drawingVisual.RenderOpen())
            {
                // Intentionally left blank to clear the visual.
            }
        }

        /// <summary>
        /// 设置橡皮擦大小
        /// </summary>
        public void SetEraserSize(double size)
        {
            EraserSize = size;
        }

        /// <summary>
        /// 设置橡皮擦颜色
        /// </summary>
        public void SetEraserColor(Color color)
        {
            // 避免将橡皮擦颜色设置为完全透明，否则用户将看不到橡皮擦光标
            if (color.A == 0)
            {
                // 如果传入的是全透明颜色，则保持当前颜色不变
                return;
            }

            EraserColor = color;
        }

        #endregion
    }

    #region Event Args

    public class EraserMouseEventArgs : EventArgs
    {
        public Point Position { get; }
        public MouseEventArgs OriginalArgs { get; }

        public EraserMouseEventArgs(Point position, MouseEventArgs originalArgs)
        {
            Position = position;
            OriginalArgs = originalArgs;
        }
    }

    #endregion
}
