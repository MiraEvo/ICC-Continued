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

}