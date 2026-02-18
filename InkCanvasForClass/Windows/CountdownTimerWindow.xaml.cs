using Ink_Canvas.Helpers;
using Ink_Canvas.ViewModels;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace Ink_Canvas
{
    /// <summary>
    /// Interaction logic for StopwatchWindow.xaml
    /// </summary>
    public partial class CountdownTimerWindow : Window
    {
        public CountdownTimerWindow()
        {
            InitializeComponent();

            var viewModel = new CountdownTimerViewModel();
            DataContext = viewModel;

            viewModel.CenterWindowAction = CenterWindow;

            viewModel.CloseWindowAction = Close;
            
            this.Loaded += (s, e) =>
            {
                CenterWindow();
                AnimationsHelper.ShowWithSlideFromBottomAndFade(this, 0.25);
            };
        }

        private void CenterWindow()
        {
            // Set to center
            double dpiScaleX = 1, dpiScaleY = 1;
            PresentationSource source = PresentationSource.FromVisual(this);
            if (source != null)
            {
                dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
                dpiScaleY = source.CompositionTarget.TransformToDevice.M22;
            }
            IntPtr windowHandle = new WindowInteropHelper(this).Handle;
            System.Windows.Forms.Screen screen = System.Windows.Forms.Screen.FromHandle(windowHandle);
            double screenWidth = screen.Bounds.Width / dpiScaleX, screenHeight = screen.Bounds.Height / dpiScaleY;
            Left = (screenWidth / 2) - (Width / 2);
            Top = (screenHeight / 2) - (Height / 2);
        }

        private void WindowDragMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }
    }
}