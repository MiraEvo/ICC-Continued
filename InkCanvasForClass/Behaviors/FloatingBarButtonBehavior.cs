extern alias XamlBehaviors;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Ink_Canvas.Behaviors
{
    /// <summary>
    /// 浮动工具栏按钮行为 - 处理视觉反馈和命令绑定
    /// 用于替代 code-behind 中的 FloatingBarToolBtnMouseDownFeedback_Panel 和 FloatingBarToolBtnMouseLeaveFeedback_Panel
    /// </summary>
    public class FloatingBarButtonBehavior : XamlBehaviors::Microsoft.Xaml.Behaviors.Behavior<Panel>
    {
        #region 静态资源

        /// <summary>
        /// 缓存的透明画刷
        /// </summary>
        private static readonly Brush TransparentBrush = Brushes.Transparent;

        /// <summary>
        /// 普通按钮按下时的画刷颜色
        /// </summary>
        private static readonly Brush ButtonPressedBrush = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0));

        /// <summary>
        /// 删除按钮按下时的画刷颜色
        /// </summary>
        private static readonly Brush DeleteButtonPressedBrush = new SolidColorBrush(Color.FromArgb(30, 220, 38, 38));

        static FloatingBarButtonBehavior()
        {
            ButtonPressedBrush.Freeze();
            DeleteButtonPressedBrush.Freeze();
        }

        #endregion

        #region 依赖属性

        /// <summary>
        /// 要执行的命令
        /// </summary>
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(
                nameof(Command),
                typeof(ICommand),
                typeof(FloatingBarButtonBehavior),
                new PropertyMetadata(null));

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        /// <summary>
        /// 命令参数
        /// </summary>
        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.Register(
                nameof(CommandParameter),
                typeof(object),
                typeof(FloatingBarButtonBehavior),
                new PropertyMetadata(null));

        public object CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        /// <summary>
        /// 是否为删除按钮（使用红色按下效果）
        /// </summary>
        public static readonly DependencyProperty IsDeleteButtonProperty =
            DependencyProperty.Register(
                nameof(IsDeleteButton),
                typeof(bool),
                typeof(FloatingBarButtonBehavior),
                new PropertyMetadata(false));

        public bool IsDeleteButton
        {
            get => (bool)GetValue(IsDeleteButtonProperty);
            set => SetValue(IsDeleteButtonProperty, value);
        }

        /// <summary>
        /// 是否启用视觉反馈
        /// </summary>
        public static readonly DependencyProperty EnableVisualFeedbackProperty =
            DependencyProperty.Register(
                nameof(EnableVisualFeedback),
                typeof(bool),
                typeof(FloatingBarButtonBehavior),
                new PropertyMetadata(true));

        public bool EnableVisualFeedback
        {
            get => (bool)GetValue(EnableVisualFeedbackProperty);
            set => SetValue(EnableVisualFeedbackProperty, value);
        }

        #endregion

        #region 私有字段

        private bool _isPressed;

        #endregion

        #region Behavior 生命周期

        protected override void OnAttached()
        {
            base.OnAttached();
            if (AssociatedObject != null)
            {
                AssociatedObject.MouseDown += OnMouseDown;
                AssociatedObject.MouseUp += OnMouseUp;
                AssociatedObject.MouseLeave += OnMouseLeave;
            }
        }

        protected override void OnDetaching()
        {
            if (AssociatedObject != null)
            {
                AssociatedObject.MouseDown -= OnMouseDown;
                AssociatedObject.MouseUp -= OnMouseUp;
                AssociatedObject.MouseLeave -= OnMouseLeave;
            }
            base.OnDetaching();
        }

        #endregion

        #region 事件处理

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!AssociatedObject.IsEnabled) return;

            _isPressed = true;

            if (EnableVisualFeedback)
            {
                AssociatedObject.Background = IsDeleteButton ? DeleteButtonPressedBrush : ButtonPressedBrush;
            }
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isPressed || !AssociatedObject.IsEnabled) return;

            _isPressed = false;

            if (EnableVisualFeedback)
            {
                AssociatedObject.Background = TransparentBrush;
            }

            // 执行命令
            ExecuteCommand();
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            _isPressed = false;

            if (EnableVisualFeedback)
            {
                AssociatedObject.Background = TransparentBrush;
            }
        }

        private void ExecuteCommand()
        {
            if (Command?.CanExecute(CommandParameter) == true)
            {
                Command.Execute(CommandParameter);
            }
        }

        #endregion
    }

    /// <summary>
    /// 浮动工具栏按钮行为（适用于 Border 元素）
    /// </summary>
    public class FloatingBarBorderButtonBehavior : XamlBehaviors::Microsoft.Xaml.Behaviors.Behavior<Border>
    {
        #region 静态资源

        private static readonly Brush TransparentBrush = Brushes.Transparent;
        private static readonly Brush ButtonPressedBrush = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0));
        private static readonly Brush DeleteButtonPressedBrush = new SolidColorBrush(Color.FromArgb(30, 220, 38, 38));

        static FloatingBarBorderButtonBehavior()
        {
            ButtonPressedBrush.Freeze();
            DeleteButtonPressedBrush.Freeze();
        }

        #endregion

        #region 依赖属性

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(
                nameof(Command),
                typeof(ICommand),
                typeof(FloatingBarBorderButtonBehavior),
                new PropertyMetadata(null));

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.Register(
                nameof(CommandParameter),
                typeof(object),
                typeof(FloatingBarBorderButtonBehavior),
                new PropertyMetadata(null));

        public object CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        public static readonly DependencyProperty IsDeleteButtonProperty =
            DependencyProperty.Register(
                nameof(IsDeleteButton),
                typeof(bool),
                typeof(FloatingBarBorderButtonBehavior),
                new PropertyMetadata(false));

        public bool IsDeleteButton
        {
            get => (bool)GetValue(IsDeleteButtonProperty);
            set => SetValue(IsDeleteButtonProperty, value);
        }

        public static readonly DependencyProperty EnableVisualFeedbackProperty =
            DependencyProperty.Register(
                nameof(EnableVisualFeedback),
                typeof(bool),
                typeof(FloatingBarBorderButtonBehavior),
                new PropertyMetadata(true));

        public bool EnableVisualFeedback
        {
            get => (bool)GetValue(EnableVisualFeedbackProperty);
            set => SetValue(EnableVisualFeedbackProperty, value);
        }

        #endregion

        private bool _isPressed;

        protected override void OnAttached()
        {
            base.OnAttached();
            if (AssociatedObject != null)
            {
                AssociatedObject.MouseDown += OnMouseDown;
                AssociatedObject.MouseUp += OnMouseUp;
                AssociatedObject.MouseLeave += OnMouseLeave;
            }
        }

        protected override void OnDetaching()
        {
            if (AssociatedObject != null)
            {
                AssociatedObject.MouseDown -= OnMouseDown;
                AssociatedObject.MouseUp -= OnMouseUp;
                AssociatedObject.MouseLeave -= OnMouseLeave;
            }
            base.OnDetaching();
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!AssociatedObject.IsEnabled) return;

            _isPressed = true;

            if (EnableVisualFeedback)
            {
                AssociatedObject.Background = IsDeleteButton ? DeleteButtonPressedBrush : ButtonPressedBrush;
            }
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isPressed || !AssociatedObject.IsEnabled) return;

            _isPressed = false;

            if (EnableVisualFeedback)
            {
                AssociatedObject.Background = TransparentBrush;
            }

            ExecuteCommand();
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            _isPressed = false;

            if (EnableVisualFeedback)
            {
                AssociatedObject.Background = TransparentBrush;
            }
        }

        private void ExecuteCommand()
        {
            if (Command?.CanExecute(CommandParameter) == true)
            {
                Command.Execute(CommandParameter);
            }
        }
    }

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