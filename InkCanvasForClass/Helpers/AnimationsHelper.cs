using System;
using System.Collections.Concurrent;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Ink_Canvas.Helpers
{
    internal class AnimationsHelper
    {
        private static readonly CubicEase CachedCubicEase;
        private static readonly PropertyPath OpacityPropertyPath;
        private static readonly PropertyPath TranslateYPropertyPath;
        private static readonly PropertyPath TranslateXPropertyPath;
        private static readonly PropertyPath ScaleXPropertyPath;
        private static readonly PropertyPath ScaleYPropertyPath;
        
        static AnimationsHelper()
        {
            CachedCubicEase = new CubicEase();
            CachedCubicEase.Freeze();
            
            OpacityPropertyPath = new PropertyPath(UIElement.OpacityProperty);
            TranslateYPropertyPath = new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)");
            TranslateXPropertyPath = new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)");
            ScaleXPropertyPath = new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)");
            ScaleYPropertyPath = new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)");
        }
        
        public static void ShowWithFadeIn(UIElement element, double duration = 0.15)
        {
            if (element == null || element.Visibility == Visibility.Visible) return;

            var sb = new Storyboard();

            var fadeInAnimation = new DoubleAnimation
            {
                From = 0.5,
                To = 1,
                Duration = TimeSpan.FromSeconds(duration)
            };
            Storyboard.SetTargetProperty(fadeInAnimation, OpacityPropertyPath);

            sb.Children.Add(fadeInAnimation);

            element.Visibility = Visibility.Visible;

            sb.Begin((FrameworkElement)element);
        }

        public static void ShowWithSlideFromBottomAndFade(UIElement element, double duration = 0.15)
        {
            try
            {
                if (element == null || element.Visibility == Visibility.Visible) return;

                var sb = new Storyboard();
                var durationTimeSpan = TimeSpan.FromSeconds(duration);

                var fadeInAnimation = new DoubleAnimation
                {
                    From = 0.5,
                    To = 1,
                    Duration = durationTimeSpan,
                    EasingFunction = CachedCubicEase
                };
                Storyboard.SetTargetProperty(fadeInAnimation, OpacityPropertyPath);

                var slideAnimation = new DoubleAnimation
                {
                    From = element.RenderTransform.Value.OffsetY + 10, // 滑动距离
                    To = 0,
                    Duration = durationTimeSpan,
                    EasingFunction = CachedCubicEase
                };
                Storyboard.SetTargetProperty(slideAnimation, TranslateYPropertyPath);

                sb.Children.Add(fadeInAnimation);
                sb.Children.Add(slideAnimation);

                element.Visibility = Visibility.Visible;
                element.RenderTransform = new TranslateTransform();

                sb.Begin((FrameworkElement)element);
            }
            catch (Exception ex) {
                LogHelper.WriteLogToFile("Animation ShowWithSlideFromBottomAndFade failed: " + ex.Message, LogHelper.LogType.Trace);
            }
        }

        public static void ShowWithSlideFromLeftAndFade(UIElement element, double duration = 0.25)
        {
            try
            {
                if (element == null || element.Visibility == Visibility.Visible) return;

                var sb = new Storyboard();
                var durationTimeSpan = TimeSpan.FromSeconds(duration);

                var fadeInAnimation = new DoubleAnimation
                {
                    From = 0.5,
                    To = 1,
                    Duration = durationTimeSpan
                };
                Storyboard.SetTargetProperty(fadeInAnimation, OpacityPropertyPath);

                var slideAnimation = new DoubleAnimation
                {
                    From = element.RenderTransform.Value.OffsetX - 20, // 滑动距离
                    To = 0,
                    Duration = durationTimeSpan
                };
                Storyboard.SetTargetProperty(slideAnimation, TranslateXPropertyPath);

                sb.Children.Add(fadeInAnimation);
                sb.Children.Add(slideAnimation);

                element.Visibility = Visibility.Visible;
                element.RenderTransform = new TranslateTransform();

                sb.Begin((FrameworkElement)element);
            }
            catch (Exception ex) {
                LogHelper.WriteLogToFile("Animation ShowWithSlideFromLeftAndFade failed: " + ex.Message, LogHelper.LogType.Trace);
            }
        }

        public static void ShowWithScaleFromLeft(UIElement element, double duration = 0.2)
        {
            try
            {
                if (element == null || element.Visibility == Visibility.Visible) return;

                var sb = new Storyboard();
                var durationTimeSpan = TimeSpan.FromSeconds(duration);

                var scaleXAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = durationTimeSpan,
                    EasingFunction = CachedCubicEase
                };
                Storyboard.SetTargetProperty(scaleXAnimation, ScaleXPropertyPath);

                var scaleYAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = durationTimeSpan,
                    EasingFunction = CachedCubicEase
                };
                Storyboard.SetTargetProperty(scaleYAnimation, ScaleYPropertyPath);

                sb.Children.Add(scaleXAnimation);
                sb.Children.Add(scaleYAnimation);

                element.Visibility = Visibility.Visible;
                element.RenderTransformOrigin = new Point(0, 0.5); // 左侧中心点为基准
                element.RenderTransform = new ScaleTransform(0, 0);

                sb.Begin((FrameworkElement)element);
            }
            catch (Exception ex) {
                LogHelper.WriteLogToFile("Animation ShowWithScaleFromLeft failed: " + ex.Message, LogHelper.LogType.Trace);
            }
        }

        public static void ShowWithScaleFromRight(UIElement element, double duration = 0.2)
        {
            try
            {
                if (element == null || element.Visibility == Visibility.Visible) return;

                var sb = new Storyboard();
                var durationTimeSpan = TimeSpan.FromSeconds(duration);

                var scaleXAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = durationTimeSpan,
                    EasingFunction = CachedCubicEase
                };
                Storyboard.SetTargetProperty(scaleXAnimation, ScaleXPropertyPath);

                var scaleYAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = durationTimeSpan,
                    EasingFunction = CachedCubicEase
                };
                Storyboard.SetTargetProperty(scaleYAnimation, ScaleYPropertyPath);

                sb.Children.Add(scaleXAnimation);
                sb.Children.Add(scaleYAnimation);

                element.Visibility = Visibility.Visible;
                element.RenderTransformOrigin = new Point(1, 0.5); // 右侧中心点为基准
                element.RenderTransform = new ScaleTransform(0, 0);

                sb.Begin((FrameworkElement)element);
            }
            catch (Exception ex) {
                LogHelper.WriteLogToFile("Animation ShowWithScaleFromRight failed: " + ex.Message, LogHelper.LogType.Trace);
            }
        }

        public static void HideWithSlideAndFade(UIElement element, double duration = 0.15)
        {
            try
            {
                if (element == null || element.Visibility == Visibility.Collapsed) return;

                var sb = new Storyboard();
                var durationTimeSpan = TimeSpan.FromSeconds(duration);

                var fadeOutAnimation = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = durationTimeSpan,
                    EasingFunction = CachedCubicEase
                };
                Storyboard.SetTargetProperty(fadeOutAnimation, OpacityPropertyPath);

                var slideAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = element.RenderTransform.Value.OffsetY + 10, // 滑动距离
                    Duration = durationTimeSpan,
                    EasingFunction = CachedCubicEase
                };
                Storyboard.SetTargetProperty(slideAnimation, TranslateYPropertyPath);

                sb.Children.Add(fadeOutAnimation);
                sb.Children.Add(slideAnimation);

                sb.Completed += (s, e) =>
                {
                    element.Visibility = Visibility.Collapsed;
                };

                element.RenderTransform = new TranslateTransform();
                sb.Begin((FrameworkElement)element);
            }
            catch (Exception ex) {
                LogHelper.WriteLogToFile("Animation HideWithSlideAndFade failed: " + ex.Message, LogHelper.LogType.Trace);
            }
        }

        public static void HideWithFadeOut(UIElement element, double duration = 0.15)
        {
            if (element == null || element.Visibility == Visibility.Collapsed) return;

            var sb = new Storyboard();

            var fadeOutAnimation = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(duration)
            };
            Storyboard.SetTargetProperty(fadeOutAnimation, OpacityPropertyPath);

            sb.Children.Add(fadeOutAnimation);

            sb.Completed += (s, e) =>
            {
                element.Visibility = Visibility.Collapsed;
            };

            sb.Begin((FrameworkElement)element);
        }

    }
}
