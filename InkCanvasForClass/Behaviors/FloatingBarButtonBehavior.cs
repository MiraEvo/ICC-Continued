
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Ink_Canvas.Behaviors
{


    /// <summary>
    /// 通用浮动工具栏按钮行为（适用于任意 UIElement）
    /// 提供附加属性方式使用
    /// </summary>
    public static class FloatingBarButton
    {
        #region 静态资源

        private static readonly Brush TransparentBrush = Brushes.Transparent;
        private static readonly Brush ButtonPressedBrush = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0));
        private static readonly Brush DeleteButtonPressedBrush = new SolidColorBrush(Color.FromArgb(30, 220, 38, 38));
        private static readonly Brush SelectedBrush = new SolidColorBrush(Color.FromArgb(40, 37, 99, 235)); // #2563eb with alpha

        static FloatingBarButton()
        {
            ButtonPressedBrush.Freeze();
            DeleteButtonPressedBrush.Freeze();
            SelectedBrush.Freeze();
        }

        #endregion

        #region Command 附加属性

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.RegisterAttached(
                "Command",
                typeof(ICommand),
                typeof(FloatingBarButton),
                new PropertyMetadata(null, OnCommandChanged));

        public static ICommand GetCommand(DependencyObject obj) =>
            (ICommand)obj.GetValue(CommandProperty);

        public static void SetCommand(DependencyObject obj, ICommand value) =>
            obj.SetValue(CommandProperty, value);

        #endregion

        #region CommandParameter 附加属性

        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.RegisterAttached(
                "CommandParameter",
                typeof(object),
                typeof(FloatingBarButton),
                new PropertyMetadata(null));

        public static object GetCommandParameter(DependencyObject obj) =>
            obj.GetValue(CommandParameterProperty);

        public static void SetCommandParameter(DependencyObject obj, object value) =>
            obj.SetValue(CommandParameterProperty, value);

        #endregion

        #region IsDeleteButton 附加属性

        public static readonly DependencyProperty IsDeleteButtonProperty =
            DependencyProperty.RegisterAttached(
                "IsDeleteButton",
                typeof(bool),
                typeof(FloatingBarButton),
                new PropertyMetadata(false));

        public static bool GetIsDeleteButton(DependencyObject obj) =>
            (bool)obj.GetValue(IsDeleteButtonProperty);

        public static void SetIsDeleteButton(DependencyObject obj, bool value) =>
            obj.SetValue(IsDeleteButtonProperty, value);

        #endregion

        #region IsSelected 附加属性

        /// <summary>
        /// IsSelected 附加属性 - 用于绑定工具选中状态
        /// </summary>
        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.RegisterAttached(
                "IsSelected",
                typeof(bool),
                typeof(FloatingBarButton),
                new PropertyMetadata(false, OnIsSelectedChanged));

        public static bool GetIsSelected(DependencyObject obj) =>
            (bool)obj.GetValue(IsSelectedProperty);

        public static void SetIsSelected(DependencyObject obj, bool value) =>
            obj.SetValue(IsSelectedProperty, value);

        private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement element)
            {
                // 只有在没有按下时才应用选中样式
                if (!GetIsPressed(element))
                {
                    ApplySelectionStyle(element, (bool)e.NewValue);
                }
            }
        }

        #endregion

        #region IsPressed 内部附加属性

        private static readonly DependencyProperty IsPressedProperty =
            DependencyProperty.RegisterAttached(
                "IsPressed",
                typeof(bool),
                typeof(FloatingBarButton),
                new PropertyMetadata(false));

        private static bool GetIsPressed(DependencyObject obj) =>
            (bool)obj.GetValue(IsPressedProperty);

        private static void SetIsPressed(DependencyObject obj, bool value) =>
            obj.SetValue(IsPressedProperty, value);

        #endregion

        #region 事件处理

        private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement element)
            {
                // 移除旧的事件处理
                element.MouseDown -= OnElementMouseDown;
                element.MouseUp -= OnElementMouseUp;
                element.MouseLeave -= OnElementMouseLeave;

                // 如果有新命令，添加事件处理
                if (e.NewValue != null)
                {
                    element.MouseDown += OnElementMouseDown;
                    element.MouseUp += OnElementMouseUp;
                    element.MouseLeave += OnElementMouseLeave;
                }
            }
        }

        private static void OnElementMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is UIElement element && element.IsEnabled)
            {
                SetIsPressed(element, true);
                ApplyPressedStyle(element, true);
            }
        }

        private static void OnElementMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is UIElement element && GetIsPressed(element) && element.IsEnabled)
            {
                SetIsPressed(element, false);
                // 恢复时检查是否选中
                ApplySelectionStyle(element, GetIsSelected(element));
                ExecuteCommand(element);
            }
        }

        private static void OnElementMouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is UIElement element)
            {
                SetIsPressed(element, false);
                // 恢复时检查是否选中
                ApplySelectionStyle(element, GetIsSelected(element));
            }
        }

        private static void ApplyPressedStyle(UIElement element, bool isPressed)
        {
            Brush brush = isPressed
                ? (GetIsDeleteButton(element) ? DeleteButtonPressedBrush : ButtonPressedBrush)
                : (GetIsSelected(element) ? SelectedBrush : TransparentBrush);

            SetElementBackground(element, brush);
        }

        private static void ApplySelectionStyle(UIElement element, bool isSelected)
        {
            Brush brush = isSelected ? SelectedBrush : TransparentBrush;
            SetElementBackground(element, brush);
        }

        private static void SetElementBackground(UIElement element, Brush brush)
        {
            switch (element)
            {
                case Panel panel:
                    panel.Background = brush;
                    break;
                case Border border:
                    border.Background = brush;
                    break;
                case Control control:
                    control.Background = brush;
                    break;
            }
        }

        private static void ExecuteCommand(UIElement element)
        {
            var command = GetCommand(element);
            var parameter = GetCommandParameter(element);

            if (command?.CanExecute(parameter) == true)
            {
                command.Execute(parameter);
            }
        }

        #endregion
    }
}