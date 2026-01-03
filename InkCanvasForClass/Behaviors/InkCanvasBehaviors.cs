extern alias XamlBehaviors;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;

namespace Ink_Canvas.Behaviors
{
    /// <summary>
    /// InkCanvas 笔迹变化行为 - 将 Strokes.StrokesChanged 事件转换为命令
    /// </summary>
    public class InkCanvasStrokesChangedBehavior : XamlBehaviors::Microsoft.Xaml.Behaviors.Behavior<InkCanvas>
    {
        #region Command 依赖属性

        /// <summary>
        /// 笔迹变化时执行的命令
        /// </summary>
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(
                nameof(Command),
                typeof(ICommand),
                typeof(InkCanvasStrokesChangedBehavior),
                new PropertyMetadata(null));

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        /// <summary>
        /// 笔迹添加时执行的命令
        /// </summary>
        public static readonly DependencyProperty StrokesAddedCommandProperty =
            DependencyProperty.Register(
                nameof(StrokesAddedCommand),
                typeof(ICommand),
                typeof(InkCanvasStrokesChangedBehavior),
                new PropertyMetadata(null));

        public ICommand StrokesAddedCommand
        {
            get => (ICommand)GetValue(StrokesAddedCommandProperty);
            set => SetValue(StrokesAddedCommandProperty, value);
        }

        /// <summary>
        /// 笔迹移除时执行的命令
        /// </summary>
        public static readonly DependencyProperty StrokesRemovedCommandProperty =
            DependencyProperty.Register(
                nameof(StrokesRemovedCommand),
                typeof(ICommand),
                typeof(InkCanvasStrokesChangedBehavior),
                new PropertyMetadata(null));

        public ICommand StrokesRemovedCommand
        {
            get => (ICommand)GetValue(StrokesRemovedCommandProperty);
            set => SetValue(StrokesRemovedCommandProperty, value);
        }

        #endregion

        protected override void OnAttached()
        {
            base.OnAttached();
            if (AssociatedObject != null)
            {
                AssociatedObject.Strokes.StrokesChanged += OnStrokesChanged;
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            if (AssociatedObject != null)
            {
                AssociatedObject.Strokes.StrokesChanged -= OnStrokesChanged;
            }
        }

        private void OnStrokesChanged(object sender, StrokeCollectionChangedEventArgs e)
        {
            // 执行通用命令
            if (Command?.CanExecute(e) == true)
            {
                Command.Execute(e);
            }

            // 执行笔迹添加命令
            if (e.Added?.Count > 0 && StrokesAddedCommand?.CanExecute(e.Added) == true)
            {
                StrokesAddedCommand.Execute(e.Added);
            }

            // 执行笔迹移除命令
            if (e.Removed?.Count > 0 && StrokesRemovedCommand?.CanExecute(e.Removed) == true)
            {
                StrokesRemovedCommand.Execute(e.Removed);
            }
        }
    }

    /// <summary>
    /// InkCanvas 编辑模式变化行为
    /// </summary>
    public class InkCanvasEditingModeBehavior : XamlBehaviors::Microsoft.Xaml.Behaviors.Behavior<InkCanvas>
    {
        #region 依赖属性

        /// <summary>
        /// 编辑模式变化时执行的命令
        /// </summary>
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(
                nameof(Command),
                typeof(ICommand),
                typeof(InkCanvasEditingModeBehavior),
                new PropertyMetadata(null));

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        /// <summary>
        /// 当前编辑模式（双向绑定）
        /// </summary>
        public static readonly DependencyProperty EditingModeProperty =
            DependencyProperty.Register(
                nameof(EditingMode),
                typeof(InkCanvasEditingMode),
                typeof(InkCanvasEditingModeBehavior),
                new FrameworkPropertyMetadata(
                    InkCanvasEditingMode.Ink,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnEditingModePropertyChanged));

        public InkCanvasEditingMode EditingMode
        {
            get => (InkCanvasEditingMode)GetValue(EditingModeProperty);
            set => SetValue(EditingModeProperty, value);
        }

        private static void OnEditingModePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is InkCanvasEditingModeBehavior behavior && behavior.AssociatedObject != null)
            {
                behavior.AssociatedObject.EditingMode = (InkCanvasEditingMode)e.NewValue;
            }
        }

        #endregion

        protected override void OnAttached()
        {
            base.OnAttached();
            if (AssociatedObject != null)
            {
                AssociatedObject.EditingModeChanged += OnEditingModeChanged;
                // 同步初始值
                AssociatedObject.EditingMode = EditingMode;
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            if (AssociatedObject != null)
            {
                AssociatedObject.EditingModeChanged -= OnEditingModeChanged;
            }
        }

        private void OnEditingModeChanged(object sender, RoutedEventArgs e)
        {
            if (AssociatedObject != null)
            {
                EditingMode = AssociatedObject.EditingMode;
                
                if (Command?.CanExecute(AssociatedObject.EditingMode) == true)
                {
                    Command.Execute(AssociatedObject.EditingMode);
                }
            }
        }
    }

    /// <summary>
    /// InkCanvas 选择变化行为
    /// </summary>
    public class InkCanvasSelectionChangedBehavior : XamlBehaviors::Microsoft.Xaml.Behaviors.Behavior<InkCanvas>
    {
        #region 依赖属性

        /// <summary>
        /// 选择变化时执行的命令
        /// </summary>
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(
                nameof(Command),
                typeof(ICommand),
                typeof(InkCanvasSelectionChangedBehavior),
                new PropertyMetadata(null));

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        /// <summary>
        /// 是否有选中内容
        /// </summary>
        public static readonly DependencyProperty HasSelectionProperty =
            DependencyProperty.Register(
                nameof(HasSelection),
                typeof(bool),
                typeof(InkCanvasSelectionChangedBehavior),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public bool HasSelection
        {
            get => (bool)GetValue(HasSelectionProperty);
            set => SetValue(HasSelectionProperty, value);
        }

        #endregion

        protected override void OnAttached()
        {
            base.OnAttached();
            if (AssociatedObject != null)
            {
                AssociatedObject.SelectionChanged += OnSelectionChanged;
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            if (AssociatedObject != null)
            {
                AssociatedObject.SelectionChanged -= OnSelectionChanged;
            }
        }

        private void OnSelectionChanged(object sender, EventArgs e)
        {
            if (AssociatedObject != null)
            {
                var selectedStrokes = AssociatedObject.GetSelectedStrokes();
                var selectedElements = AssociatedObject.GetSelectedElements();
                
                HasSelection = selectedStrokes.Count > 0 || selectedElements.Count > 0;
                
                var args = new SelectionChangedArgs
                {
                    SelectedStrokes = selectedStrokes,
                    SelectedElements = selectedElements,
                    HasSelection = HasSelection
                };

                if (Command?.CanExecute(args) == true)
                {
                    Command.Execute(args);
                }
            }
        }
    }

    /// <summary>
    /// 选择变化参数
    /// </summary>
    public class SelectionChangedArgs
    {
        public StrokeCollection SelectedStrokes { get; set; }
        public System.Collections.ObjectModel.ReadOnlyCollection<UIElement> SelectedElements { get; set; }
        public bool HasSelection { get; set; }
    }

    /// <summary>
    /// InkCanvas 手势行为
    /// </summary>
    public class InkCanvasGestureBehavior : XamlBehaviors::Microsoft.Xaml.Behaviors.Behavior<InkCanvas>
    {
        #region 依赖属性

        /// <summary>
        /// 手势识别时执行的命令
        /// </summary>
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(
                nameof(Command),
                typeof(ICommand),
                typeof(InkCanvasGestureBehavior),
                new PropertyMetadata(null));

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        #endregion

        protected override void OnAttached()
        {
            base.OnAttached();
            if (AssociatedObject != null)
            {
                AssociatedObject.Gesture += OnGesture;
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            if (AssociatedObject != null)
            {
                AssociatedObject.Gesture -= OnGesture;
            }
        }

        private void OnGesture(object sender, InkCanvasGestureEventArgs e)
        {
            if (Command?.CanExecute(e) == true)
            {
                Command.Execute(e);
            }
        }
    }

    /// <summary>
    /// InkCanvas 笔迹收集行为 - 处理 StrokeCollected 事件
    /// </summary>
    public class InkCanvasStrokeCollectedBehavior : XamlBehaviors::Microsoft.Xaml.Behaviors.Behavior<InkCanvas>
    {
        #region 依赖属性

        /// <summary>
        /// 笔迹收集完成时执行的命令
        /// </summary>
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(
                nameof(Command),
                typeof(ICommand),
                typeof(InkCanvasStrokeCollectedBehavior),
                new PropertyMetadata(null));

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        #endregion

        protected override void OnAttached()
        {
            base.OnAttached();
            if (AssociatedObject != null)
            {
                AssociatedObject.StrokeCollected += OnStrokeCollected;
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            if (AssociatedObject != null)
            {
                AssociatedObject.StrokeCollected -= OnStrokeCollected;
            }
        }

        private void OnStrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs e)
        {
            if (Command?.CanExecute(e.Stroke) == true)
            {
                Command.Execute(e.Stroke);
            }
        }
    }
}