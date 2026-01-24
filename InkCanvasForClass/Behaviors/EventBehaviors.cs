extern alias XamlBehaviors;
using System;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace Ink_Canvas.Behaviors
{
    /// <summary>
    /// 通用事件到命令行为 - 将任意事件转换为命令执行
    /// </summary>
    public class EventToCommandBehavior : XamlBehaviors::Microsoft.Xaml.Behaviors.Behavior<FrameworkElement>
    {
        #region 依赖属性

        /// <summary>
        /// 要监听的事件名称
        /// </summary>
        public static readonly DependencyProperty EventNameProperty =
            DependencyProperty.Register(
                nameof(EventName),
                typeof(string),
                typeof(EventToCommandBehavior),
                new PropertyMetadata(null, OnEventNameChanged));

        public string EventName
        {
            get => (string)GetValue(EventNameProperty);
            set => SetValue(EventNameProperty, value);
        }

        /// <summary>
        /// 要执行的命令
        /// </summary>
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(
                nameof(Command),
                typeof(ICommand),
                typeof(EventToCommandBehavior),
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
                typeof(EventToCommandBehavior),
                new PropertyMetadata(null));

        public object CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        /// <summary>
        /// 是否传递事件参数
        /// </summary>
        public static readonly DependencyProperty PassEventArgsProperty =
            DependencyProperty.Register(
                nameof(PassEventArgs),
                typeof(bool),
                typeof(EventToCommandBehavior),
                new PropertyMetadata(false));

        public bool PassEventArgs
        {
            get => (bool)GetValue(PassEventArgsProperty);
            set => SetValue(PassEventArgsProperty, value);
        }

        #endregion

        private Delegate? _eventHandler;
        private EventInfo? _eventInfo;

        private static void OnEventNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (EventToCommandBehavior)d;
            if (behavior.AssociatedObject != null)
            {
                behavior.DetachEvent();
                behavior.AttachEvent();
            }
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AttachEvent();
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            DetachEvent();
        }

        private void AttachEvent()
        {
            if (AssociatedObject == null || string.IsNullOrEmpty(EventName))
                return;

            _eventInfo = AssociatedObject.GetType().GetEvent(EventName);
            if (_eventInfo == null)
                return;

            var methodInfo = typeof(EventToCommandBehavior).GetMethod(
                nameof(OnEventRaised),
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (_eventInfo.EventHandlerType != null)
            {
                _eventHandler = Delegate.CreateDelegate(_eventInfo.EventHandlerType, this, methodInfo);
                _eventInfo.AddEventHandler(AssociatedObject, _eventHandler);
            }
        }

        private void DetachEvent()
        {
            if (_eventInfo != null && _eventHandler != null && AssociatedObject != null)
            {
                _eventInfo.RemoveEventHandler(AssociatedObject, _eventHandler);
                _eventHandler = null;
                _eventInfo = null;
            }
        }

        private void OnEventRaised(object sender, EventArgs e)
        {
            var parameter = PassEventArgs ? e : CommandParameter;
            
            if (Command?.CanExecute(parameter) == true)
            {
                Command.Execute(parameter);
            }
        }
    }

    /// <summary>
    /// 鼠标事件行为 - 处理鼠标相关事件
    /// </summary>
    public class MouseEventBehavior : XamlBehaviors::Microsoft.Xaml.Behaviors.Behavior<UIElement>
    {
        #region 依赖属性

        public static readonly DependencyProperty MouseDownCommandProperty =
            DependencyProperty.Register(
                nameof(MouseDownCommand),
                typeof(ICommand),
                typeof(MouseEventBehavior),
                new PropertyMetadata(null));

        public ICommand MouseDownCommand
        {
            get => (ICommand)GetValue(MouseDownCommandProperty);
            set => SetValue(MouseDownCommandProperty, value);
        }

        public static readonly DependencyProperty MouseUpCommandProperty =
            DependencyProperty.Register(
                nameof(MouseUpCommand),
                typeof(ICommand),
                typeof(MouseEventBehavior),
                new PropertyMetadata(null));

        public ICommand MouseUpCommand
        {
            get => (ICommand)GetValue(MouseUpCommandProperty);
            set => SetValue(MouseUpCommandProperty, value);
        }

        public static readonly DependencyProperty MouseMoveCommandProperty =
            DependencyProperty.Register(
                nameof(MouseMoveCommand),
                typeof(ICommand),
                typeof(MouseEventBehavior),
                new PropertyMetadata(null));

        public ICommand MouseMoveCommand
        {
            get => (ICommand)GetValue(MouseMoveCommandProperty);
            set => SetValue(MouseMoveCommandProperty, value);
        }

        public static readonly DependencyProperty MouseEnterCommandProperty =
            DependencyProperty.Register(
                nameof(MouseEnterCommand),
                typeof(ICommand),
                typeof(MouseEventBehavior),
                new PropertyMetadata(null));

        public ICommand MouseEnterCommand
        {
            get => (ICommand)GetValue(MouseEnterCommandProperty);
            set => SetValue(MouseEnterCommandProperty, value);
        }

        public static readonly DependencyProperty MouseLeaveCommandProperty =
            DependencyProperty.Register(
                nameof(MouseLeaveCommand),
                typeof(ICommand),
                typeof(MouseEventBehavior),
                new PropertyMetadata(null));

        public ICommand MouseLeaveCommand
        {
            get => (ICommand)GetValue(MouseLeaveCommandProperty);
            set => SetValue(MouseLeaveCommandProperty, value);
        }

        public static readonly DependencyProperty MouseWheelCommandProperty =
            DependencyProperty.Register(
                nameof(MouseWheelCommand),
                typeof(ICommand),
                typeof(MouseEventBehavior),
                new PropertyMetadata(null));

        public ICommand MouseWheelCommand
        {
            get => (ICommand)GetValue(MouseWheelCommandProperty);
            set => SetValue(MouseWheelCommandProperty, value);
        }

        #endregion

        protected override void OnAttached()
        {
            base.OnAttached();
            if (AssociatedObject != null)
            {
                AssociatedObject.MouseDown += OnMouseDown;
                AssociatedObject.MouseUp += OnMouseUp;
                AssociatedObject.MouseMove += OnMouseMove;
                AssociatedObject.MouseEnter += OnMouseEnter;
                AssociatedObject.MouseLeave += OnMouseLeave;
                AssociatedObject.MouseWheel += OnMouseWheel;
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            if (AssociatedObject != null)
            {
                AssociatedObject.MouseDown -= OnMouseDown;
                AssociatedObject.MouseUp -= OnMouseUp;
                AssociatedObject.MouseMove -= OnMouseMove;
                AssociatedObject.MouseEnter -= OnMouseEnter;
                AssociatedObject.MouseLeave -= OnMouseLeave;
                AssociatedObject.MouseWheel -= OnMouseWheel;
            }
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            MouseEventBehavior.ExecuteCommand(MouseDownCommand, e);
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            MouseEventBehavior.ExecuteCommand(MouseUpCommand, e);
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            MouseEventBehavior.ExecuteCommand(MouseMoveCommand, e);
        }

        private void OnMouseEnter(object sender, MouseEventArgs e)
        {
            MouseEventBehavior.ExecuteCommand(MouseEnterCommand, e);
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            MouseEventBehavior.ExecuteCommand(MouseLeaveCommand, e);
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            MouseEventBehavior.ExecuteCommand(MouseWheelCommand, e);
        }

        private static void ExecuteCommand(ICommand command, EventArgs e)
        {
            if (command?.CanExecute(e) == true)
            {
                command.Execute(e);
            }
        }
    }

    /// <summary>
    /// 触摸事件行为
    /// </summary>
    public class TouchEventBehavior : XamlBehaviors::Microsoft.Xaml.Behaviors.Behavior<UIElement>
    {
        #region 依赖属性

        public static readonly DependencyProperty TouchDownCommandProperty =
            DependencyProperty.Register(
                nameof(TouchDownCommand),
                typeof(ICommand),
                typeof(TouchEventBehavior),
                new PropertyMetadata(null));

        public ICommand TouchDownCommand
        {
            get => (ICommand)GetValue(TouchDownCommandProperty);
            set => SetValue(TouchDownCommandProperty, value);
        }

        public static readonly DependencyProperty TouchUpCommandProperty =
            DependencyProperty.Register(
                nameof(TouchUpCommand),
                typeof(ICommand),
                typeof(TouchEventBehavior),
                new PropertyMetadata(null));

        public ICommand TouchUpCommand
        {
            get => (ICommand)GetValue(TouchUpCommandProperty);
            set => SetValue(TouchUpCommandProperty, value);
        }

        public static readonly DependencyProperty TouchMoveCommandProperty =
            DependencyProperty.Register(
                nameof(TouchMoveCommand),
                typeof(ICommand),
                typeof(TouchEventBehavior),
                new PropertyMetadata(null));

        public ICommand TouchMoveCommand
        {
            get => (ICommand)GetValue(TouchMoveCommandProperty);
            set => SetValue(TouchMoveCommandProperty, value);
        }

        public static readonly DependencyProperty TouchEnterCommandProperty =
            DependencyProperty.Register(
                nameof(TouchEnterCommand),
                typeof(ICommand),
                typeof(TouchEventBehavior),
                new PropertyMetadata(null));

        public ICommand TouchEnterCommand
        {
            get => (ICommand)GetValue(TouchEnterCommandProperty);
            set => SetValue(TouchEnterCommandProperty, value);
        }

        public static readonly DependencyProperty TouchLeaveCommandProperty =
            DependencyProperty.Register(
                nameof(TouchLeaveCommand),
                typeof(ICommand),
                typeof(TouchEventBehavior),
                new PropertyMetadata(null));

        public ICommand TouchLeaveCommand
        {
            get => (ICommand)GetValue(TouchLeaveCommandProperty);
            set => SetValue(TouchLeaveCommandProperty, value);
        }

        #endregion

        protected override void OnAttached()
        {
            base.OnAttached();
            if (AssociatedObject != null)
            {
                AssociatedObject.TouchDown += OnTouchDown;
                AssociatedObject.TouchUp += OnTouchUp;
                AssociatedObject.TouchMove += OnTouchMove;
                AssociatedObject.TouchEnter += OnTouchEnter;
                AssociatedObject.TouchLeave += OnTouchLeave;
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            if (AssociatedObject != null)
            {
                AssociatedObject.TouchDown -= OnTouchDown;
                AssociatedObject.TouchUp -= OnTouchUp;
                AssociatedObject.TouchMove -= OnTouchMove;
                AssociatedObject.TouchEnter -= OnTouchEnter;
                AssociatedObject.TouchLeave -= OnTouchLeave;
            }
        }

        private void OnTouchDown(object sender, TouchEventArgs e)
        {
            TouchEventBehavior.ExecuteCommand(TouchDownCommand, e);
        }

        private void OnTouchUp(object sender, TouchEventArgs e)
        {
            TouchEventBehavior.ExecuteCommand(TouchUpCommand, e);
        }

        private void OnTouchMove(object sender, TouchEventArgs e)
        {
            TouchEventBehavior.ExecuteCommand(TouchMoveCommand, e);
        }

        private void OnTouchEnter(object sender, TouchEventArgs e)
        {
            TouchEventBehavior.ExecuteCommand(TouchEnterCommand, e);
        }

        private void OnTouchLeave(object sender, TouchEventArgs e)
        {
            TouchEventBehavior.ExecuteCommand(TouchLeaveCommand, e);
        }

        private static void ExecuteCommand(ICommand command, EventArgs e)
        {
            if (command?.CanExecute(e) == true)
            {
                command.Execute(e);
            }
        }
    }

    /// <summary>
    /// 手写笔事件行为
    /// </summary>
    public class StylusEventBehavior : XamlBehaviors::Microsoft.Xaml.Behaviors.Behavior<UIElement>
    {
        #region 依赖属性

        public static readonly DependencyProperty StylusDownCommandProperty =
            DependencyProperty.Register(
                nameof(StylusDownCommand),
                typeof(ICommand),
                typeof(StylusEventBehavior),
                new PropertyMetadata(null));

        public ICommand StylusDownCommand
        {
            get => (ICommand)GetValue(StylusDownCommandProperty);
            set => SetValue(StylusDownCommandProperty, value);
        }

        public static readonly DependencyProperty StylusUpCommandProperty =
            DependencyProperty.Register(
                nameof(StylusUpCommand),
                typeof(ICommand),
                typeof(StylusEventBehavior),
                new PropertyMetadata(null));

        public ICommand StylusUpCommand
        {
            get => (ICommand)GetValue(StylusUpCommandProperty);
            set => SetValue(StylusUpCommandProperty, value);
        }

        public static readonly DependencyProperty StylusMoveCommandProperty =
            DependencyProperty.Register(
                nameof(StylusMoveCommand),
                typeof(ICommand),
                typeof(StylusEventBehavior),
                new PropertyMetadata(null));

        public ICommand StylusMoveCommand
        {
            get => (ICommand)GetValue(StylusMoveCommandProperty);
            set => SetValue(StylusMoveCommandProperty, value);
        }

        public static readonly DependencyProperty StylusInvertedChangedCommandProperty =
            DependencyProperty.Register(
                nameof(StylusInvertedChangedCommand),
                typeof(ICommand),
                typeof(StylusEventBehavior),
                new PropertyMetadata(null));

        public ICommand StylusInvertedChangedCommand
        {
            get => (ICommand)GetValue(StylusInvertedChangedCommandProperty);
            set => SetValue(StylusInvertedChangedCommandProperty, value);
        }

        #endregion

        protected override void OnAttached()
        {
            base.OnAttached();
            if (AssociatedObject != null)
            {
                AssociatedObject.StylusDown += OnStylusDown;
                AssociatedObject.StylusUp += OnStylusUp;
                AssociatedObject.StylusMove += OnStylusMove;
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            if (AssociatedObject != null)
            {
                AssociatedObject.StylusDown -= OnStylusDown;
                AssociatedObject.StylusUp -= OnStylusUp;
                AssociatedObject.StylusMove -= OnStylusMove;
            }
        }

        private void OnStylusDown(object sender, StylusDownEventArgs e)
        {
            StylusEventBehavior.ExecuteCommand(StylusDownCommand, e);
        }

        private void OnStylusUp(object sender, StylusEventArgs e)
        {
            StylusEventBehavior.ExecuteCommand(StylusUpCommand, e);
        }

        private void OnStylusMove(object sender, StylusEventArgs e)
        {
            StylusEventBehavior.ExecuteCommand(StylusMoveCommand, e);
        }

        private static void ExecuteCommand(ICommand command, EventArgs e)
        {
            if (command?.CanExecute(e) == true)
            {
                command.Execute(e);
            }
        }
    }

    /// <summary>
    /// 窗口加载行为
    /// </summary>
    public class WindowLoadedBehavior : XamlBehaviors::Microsoft.Xaml.Behaviors.Behavior<Window>
    {
        #region 依赖属性

        public static readonly DependencyProperty LoadedCommandProperty =
            DependencyProperty.Register(
                nameof(LoadedCommand),
                typeof(ICommand),
                typeof(WindowLoadedBehavior),
                new PropertyMetadata(null));

        public ICommand LoadedCommand
        {
            get => (ICommand)GetValue(LoadedCommandProperty);
            set => SetValue(LoadedCommandProperty, value);
        }

        public static readonly DependencyProperty ClosingCommandProperty =
            DependencyProperty.Register(
                nameof(ClosingCommand),
                typeof(ICommand),
                typeof(WindowLoadedBehavior),
                new PropertyMetadata(null));

        public ICommand ClosingCommand
        {
            get => (ICommand)GetValue(ClosingCommandProperty);
            set => SetValue(ClosingCommandProperty, value);
        }

        public static readonly DependencyProperty ClosedCommandProperty =
            DependencyProperty.Register(
                nameof(ClosedCommand),
                typeof(ICommand),
                typeof(WindowLoadedBehavior),
                new PropertyMetadata(null));

        public ICommand ClosedCommand
        {
            get => (ICommand)GetValue(ClosedCommandProperty);
            set => SetValue(ClosedCommandProperty, value);
        }

        #endregion

        protected override void OnAttached()
        {
            base.OnAttached();
            if (AssociatedObject != null)
            {
                AssociatedObject.Loaded += OnLoaded;
                AssociatedObject.Closing += OnClosing;
                AssociatedObject.Closed += OnClosed;
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            if (AssociatedObject != null)
            {
                AssociatedObject.Loaded -= OnLoaded;
                AssociatedObject.Closing -= OnClosing;
                AssociatedObject.Closed -= OnClosed;
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (LoadedCommand?.CanExecute(null) == true)
            {
                LoadedCommand.Execute(null);
            }
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (ClosingCommand?.CanExecute(e) == true)
            {
                ClosingCommand.Execute(e);
            }
        }

        private void OnClosed(object sender, EventArgs e)
        {
            if (ClosedCommand?.CanExecute(null) == true)
            {
                ClosedCommand.Execute(null);
            }
        }
    }

    /// <summary>
    /// 键盘事件行为
    /// </summary>
    public class KeyboardEventBehavior : XamlBehaviors::Microsoft.Xaml.Behaviors.Behavior<UIElement>
    {
        #region 依赖属性

        public static readonly DependencyProperty KeyDownCommandProperty =
            DependencyProperty.Register(
                nameof(KeyDownCommand),
                typeof(ICommand),
                typeof(KeyboardEventBehavior),
                new PropertyMetadata(null));

        public ICommand KeyDownCommand
        {
            get => (ICommand)GetValue(KeyDownCommandProperty);
            set => SetValue(KeyDownCommandProperty, value);
        }

        public static readonly DependencyProperty KeyUpCommandProperty =
            DependencyProperty.Register(
                nameof(KeyUpCommand),
                typeof(ICommand),
                typeof(KeyboardEventBehavior),
                new PropertyMetadata(null));

        public ICommand KeyUpCommand
        {
            get => (ICommand)GetValue(KeyUpCommandProperty);
            set => SetValue(KeyUpCommandProperty, value);
        }

        public static readonly DependencyProperty PreviewKeyDownCommandProperty =
            DependencyProperty.Register(
                nameof(PreviewKeyDownCommand),
                typeof(ICommand),
                typeof(KeyboardEventBehavior),
                new PropertyMetadata(null));

        public ICommand PreviewKeyDownCommand
        {
            get => (ICommand)GetValue(PreviewKeyDownCommandProperty);
            set => SetValue(PreviewKeyDownCommandProperty, value);
        }

        #endregion

        protected override void OnAttached()
        {
            base.OnAttached();
            if (AssociatedObject != null)
            {
                AssociatedObject.KeyDown += OnKeyDown;
                AssociatedObject.KeyUp += OnKeyUp;
                AssociatedObject.PreviewKeyDown += OnPreviewKeyDown;
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            if (AssociatedObject != null)
            {
                AssociatedObject.KeyDown -= OnKeyDown;
                AssociatedObject.KeyUp -= OnKeyUp;
                AssociatedObject.PreviewKeyDown -= OnPreviewKeyDown;
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            KeyboardEventBehavior.ExecuteCommand(KeyDownCommand, e);
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            KeyboardEventBehavior.ExecuteCommand(KeyUpCommand, e);
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            KeyboardEventBehavior.ExecuteCommand(PreviewKeyDownCommand, e);
        }

        private static void ExecuteCommand(ICommand command, EventArgs e)
        {
            if (command?.CanExecute(e) == true)
            {
                command.Execute(e);
            }
        }
    }
}