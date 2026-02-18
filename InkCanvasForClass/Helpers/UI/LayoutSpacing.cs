using System.Windows;
using System.Windows.Controls;

namespace Ink_Canvas.Helpers
{
    public static class LayoutSpacing
    {
        public static readonly DependencyProperty SpacingProperty =
            DependencyProperty.RegisterAttached(
                "Spacing",
                typeof(double),
                typeof(LayoutSpacing),
                new PropertyMetadata(0d, OnSpacingChanged));

        private static readonly DependencyProperty OriginalMarginProperty =
            DependencyProperty.RegisterAttached(
                "OriginalMargin",
                typeof(Thickness),
                typeof(LayoutSpacing),
                new PropertyMetadata(default(Thickness)));

        private static readonly DependencyProperty IsOriginalMarginCapturedProperty =
            DependencyProperty.RegisterAttached(
                "IsOriginalMarginCaptured",
                typeof(bool),
                typeof(LayoutSpacing),
                new PropertyMetadata(false));

        public static void SetSpacing(DependencyObject element, double value) => element.SetValue(SpacingProperty, value);

        public static double GetSpacing(DependencyObject element) => (double)element.GetValue(SpacingProperty);

        private static void SetOriginalMargin(DependencyObject element, Thickness value) => element.SetValue(OriginalMarginProperty, value);

        private static Thickness GetOriginalMargin(DependencyObject element) => (Thickness)element.GetValue(OriginalMarginProperty);

        private static void SetIsOriginalMarginCaptured(DependencyObject element, bool value) => element.SetValue(IsOriginalMarginCapturedProperty, value);

        private static bool GetIsOriginalMarginCaptured(DependencyObject element) => (bool)element.GetValue(IsOriginalMarginCapturedProperty);

        private static void OnSpacingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not StackPanel panel)
            {
                return;
            }

            panel.Loaded -= PanelOnLoaded;
            panel.Loaded += PanelOnLoaded;
            ApplySpacing(panel);
        }

        private static void PanelOnLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is StackPanel panel)
            {
                ApplySpacing(panel);
            }
        }

        private static void ApplySpacing(StackPanel panel)
        {
            double spacing = GetSpacing(panel);
            bool isHorizontal = panel.Orientation == Orientation.Horizontal;

            for (int i = 0; i < panel.Children.Count; i++)
            {
                if (panel.Children[i] is not FrameworkElement child)
                {
                    continue;
                }

                if (!GetIsOriginalMarginCaptured(child))
                {
                    SetOriginalMargin(child, child.Margin);
                    SetIsOriginalMarginCaptured(child, true);
                }

                Thickness original = GetOriginalMargin(child);

                child.Margin = isHorizontal
                    ? new Thickness(original.Left + (i == 0 ? 0 : spacing), original.Top, original.Right, original.Bottom)
                    : new Thickness(original.Left, original.Top + (i == 0 ? 0 : spacing), original.Right, original.Bottom);
            }
        }
    }
}
