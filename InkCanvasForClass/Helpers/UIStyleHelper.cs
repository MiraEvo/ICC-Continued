using System.Windows;
using System.Windows.Media;

namespace Ink_Canvas.Helpers {
    /// <summary>
    /// UI 样式辅助类，集中管理常用的颜色和画刷，避免重复创建对象
    /// </summary>
    public static class UIStyleHelper {
        #region 常用颜色画刷（静态缓存）

        // 基础颜色
        public static readonly SolidColorBrush TransparentBrush = new SolidColorBrush(Colors.Transparent);
        public static readonly SolidColorBrush WhiteBrush = new SolidColorBrush(Colors.White);
        public static readonly SolidColorBrush BlackBrush = new SolidColorBrush(Colors.Black);
        public static readonly SolidColorBrush GhostWhiteBrush = new SolidColorBrush(Colors.GhostWhite);

        // Zinc 灰色系列 (Tailwind CSS 风格)
        public static readonly SolidColorBrush ZincGray50Brush = new SolidColorBrush(Color.FromRgb(250, 250, 250));
        public static readonly SolidColorBrush ZincGray100Brush = new SolidColorBrush(Color.FromRgb(244, 244, 245));
        public static readonly SolidColorBrush ZincGray400Brush = new SolidColorBrush(Color.FromRgb(161, 161, 170));
        public static readonly SolidColorBrush ZincGray900Brush = new SolidColorBrush(Color.FromRgb(24, 24, 27));
        public static readonly SolidColorBrush DarkGrayBrush = new SolidColorBrush(Color.FromRgb(27, 27, 27));

        // 蓝色系列
        public static readonly SolidColorBrush Blue500Brush = new SolidColorBrush(Color.FromRgb(59, 130, 246));
        public static readonly SolidColorBrush Blue600Brush = new SolidColorBrush(Color.FromRgb(37, 99, 235));

        // 按钮反馈颜色
        public static readonly SolidColorBrush ButtonPressedBrush = new SolidColorBrush(Color.FromArgb(28, 24, 24, 27));
        public static readonly SolidColorBrush DeleteButtonPressedBrush = new SolidColorBrush(Color.FromArgb(28, 127, 29, 29));
        public static readonly SolidColorBrush TabSelectedBrush = new SolidColorBrush(Color.FromArgb(85, 59, 130, 246));

        #endregion

        #region 常用颜色值

        public static readonly Color ZincGray100Color = Color.FromRgb(244, 244, 245);
        public static readonly Color ZincGray400Color = Color.FromRgb(161, 161, 170);
        public static readonly Color ZincGray900Color = Color.FromRgb(24, 24, 27);
        public static readonly Color Blue600Color = Color.FromRgb(37, 99, 235);

        #endregion

        #region 样式应用方法

        /// <summary>
        /// 设置白板工具栏按钮为默认（未选中）状态
        /// </summary>
        public static void SetWhiteboardButtonDefault(System.Windows.Controls.Border border, 
            GeometryDrawing geometryDrawing, System.Windows.Controls.TextBlock label) {
            border.Background = ZincGray100Brush;
            border.BorderBrush = ZincGray400Brush;
            geometryDrawing.Brush = ZincGray900Brush;
            label.Foreground = ZincGray900Brush;
        }

        /// <summary>
        /// 设置白板工具栏按钮为选中状态
        /// </summary>
        public static void SetWhiteboardButtonSelected(System.Windows.Controls.Border border,
            GeometryDrawing geometryDrawing, System.Windows.Controls.TextBlock label) {
            border.Background = Blue600Brush;
            border.BorderBrush = Blue600Brush;
            geometryDrawing.Brush = GhostWhiteBrush;
            label.Foreground = GhostWhiteBrush;
        }

        /// <summary>
        /// 设置浮动工具栏图标为默认状态
        /// </summary>
        public static void SetFloatingBarIconDefault(GeometryDrawing geometryDrawing, 
            System.Windows.Controls.TextBlock textBlock, string geometryPath) {
            geometryDrawing.Brush = DarkGrayBrush;
            geometryDrawing.Geometry = Geometry.Parse(geometryPath);
            textBlock.Foreground = BlackBrush;
        }

        /// <summary>
        /// 设置浮动工具栏图标为选中状态
        /// </summary>
        public static void SetFloatingBarIconSelected(GeometryDrawing geometryDrawing,
            System.Windows.Controls.TextBlock textBlock, string geometryPath) {
            geometryDrawing.Brush = WhiteBrush;
            geometryDrawing.Geometry = Geometry.Parse(geometryPath);
            textBlock.Foreground = WhiteBrush;
        }

        #endregion

        #region Tab 按钮样式

        /// <summary>
        /// 设置 Tab 按钮为选中状态
        /// </summary>
        public static void SetTabButtonSelected(System.Windows.Controls.Border button,
            System.Windows.Controls.TextBlock text, UIElement indicator) {
            button.Background = TabSelectedBrush;
            button.Opacity = 1;
            text.FontWeight = FontWeights.Bold;
            text.Margin = new Thickness(2, 0.5, 0, 0);
            text.FontSize = 9.5;
            indicator.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// 设置 Tab 按钮为未选中状态
        /// </summary>
        public static void SetTabButtonUnselected(System.Windows.Controls.Border button,
            System.Windows.Controls.TextBlock text, UIElement indicator) {
            button.Background = TransparentBrush;
            button.Opacity = 0.75;
            text.FontWeight = FontWeights.Normal;
            text.FontSize = 9;
            text.Margin = new Thickness(2, 1, 0, 0);
            indicator.Visibility = Visibility.Collapsed;
        }

        #endregion
    }
}
