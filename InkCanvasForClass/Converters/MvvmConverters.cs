using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Ink_Canvas.ViewModels;

namespace Ink_Canvas.Converters
{
    /// <summary>
    /// 布尔值转可见性转换器（支持反转）
    /// </summary>
    /// <remarks>
    /// 使用方式：
    /// - 直接使用：True -> Visible, False -> Collapsed
    /// - 带 Invert 参数：True -> Collapsed, False -> Visible
    /// - 带 Hidden 参数：使用 Hidden 代替 Collapsed
    /// </remarks>
    public class BoolToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// 是否反转结果
        /// </summary>
        public bool Invert { get; set; } = false;

        /// <summary>
        /// 隐藏时使用 Hidden 而非 Collapsed
        /// </summary>
        public bool UseHidden { get; set; } = false;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = false;

            if (value is bool b)
            {
                boolValue = b;
            }
            else if (value != null)
            {
                // 尝试转换其他类型
                boolValue = System.Convert.ToBoolean(value);
            }

            // 检查参数是否要求反转
            bool shouldInvert = Invert;
            if (parameter is string paramStr)
            {
                if (paramStr.Equals("Invert", StringComparison.OrdinalIgnoreCase) ||
                    paramStr.Equals("!", StringComparison.OrdinalIgnoreCase))
                {
                    shouldInvert = true;
                }
            }

            if (shouldInvert)
            {
                boolValue = !boolValue;
            }

            return boolValue ? Visibility.Visible : (UseHidden ? Visibility.Hidden : Visibility.Collapsed);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                bool result = visibility == Visibility.Visible;

                bool shouldInvert = Invert;
                if (parameter is string paramStr)
                {
                    if (paramStr.Equals("Invert", StringComparison.OrdinalIgnoreCase) ||
                        paramStr.Equals("!", StringComparison.OrdinalIgnoreCase))
                    {
                        shouldInvert = true;
                    }
                }

                return shouldInvert ? !result : result;
            }

            return false;
        }
    }

    /// <summary>
    /// 布尔值反转转换器
    /// </summary>
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return !b;
            }
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return !b;
            }
            return false;
        }
    }

    /// <summary>
    /// 枚举值比较转换器（用于单选按钮等场景）
    /// </summary>
    /// <remarks>
    /// 使用方式：
    /// IsChecked="{Binding SelectedTool, Converter={StaticResource EnumToBoolConverter}, ConverterParameter=PenMode}"
    /// </remarks>
    public class EnumToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            string enumValue = value.ToString();
            string targetValue = parameter.ToString();

            return enumValue.Equals(targetValue, StringComparison.OrdinalIgnoreCase);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b && parameter != null)
            {
                return Enum.Parse(targetType, parameter.ToString());
            }
            return Binding.DoNothing;
        }
    }

    /// <summary>
    /// 枚举值转可见性转换器
    /// </summary>
    /// <remarks>
    /// 使用方式：
    /// Visibility="{Binding CurrentAppMode, Converter={StaticResource EnumToVisibilityConverter}, ConverterParameter=Whiteboard}"
    /// </remarks>
    public class EnumToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// 是否反转结果
        /// </summary>
        public bool Invert { get; set; } = false;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Visibility.Collapsed;

            string enumValue = value.ToString();
            string targetValue = parameter.ToString();

            bool isMatch = enumValue.Equals(targetValue, StringComparison.OrdinalIgnoreCase);

            if (Invert)
            {
                isMatch = !isMatch;
            }

            return isMatch ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// InkColor 枚举转 Color 转换器
    /// </summary>
    public class InkColorToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is InkColor inkColor)
            {
                return ToolbarViewModel.GetColorFromInkColor(inkColor);
            }
            return Colors.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// InkColor 枚举转 SolidColorBrush 转换器
    /// </summary>
    public class InkColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is InkColor inkColor)
            {
                return new SolidColorBrush(ToolbarViewModel.GetColorFromInkColor(inkColor));
            }
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Color 转 SolidColorBrush 转换器
    /// </summary>
    public class ColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Color color)
            {
                return new SolidColorBrush(color);
            }
            if (value is string colorString)
            {
                try
                {
                    var color2 = (Color)ColorConverter.ConvertFromString(colorString);
                    return new SolidColorBrush(color2);
                }
                catch
                {
                    return Brushes.Black;
                }
            }
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                return brush.Color;
            }
            return Colors.Black;
        }
    }

    /// <summary>
    /// 空值检查转可见性转换器
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// 是否反转（null 时显示）
        /// </summary>
        public bool Invert { get; set; } = false;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isNull = value == null;

            if (value is string str)
            {
                isNull = string.IsNullOrEmpty(str);
            }

            if (Invert)
            {
                isNull = !isNull;
            }

            return isNull ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 数值比较转可见性转换器
    /// </summary>
    /// <remarks>
    /// 使用方式：
    /// Visibility="{Binding Count, Converter={StaticResource NumberToVisibilityConverter}, ConverterParameter='>0'}"
    /// 支持的操作符: >, <, >=, <=, ==, !=
    /// </remarks>
    public class NumberToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Visibility.Collapsed;

            double numValue = System.Convert.ToDouble(value);
            string paramStr = parameter.ToString().Trim();

            bool result = false;

            if (paramStr.StartsWith(">="))
            {
                double compareValue = double.Parse(paramStr.Substring(2));
                result = numValue >= compareValue;
            }
            else if (paramStr.StartsWith("<="))
            {
                double compareValue = double.Parse(paramStr.Substring(2));
                result = numValue <= compareValue;
            }
            else if (paramStr.StartsWith("!="))
            {
                double compareValue = double.Parse(paramStr.Substring(2));
                result = Math.Abs(numValue - compareValue) > 0.0001;
            }
            else if (paramStr.StartsWith("=="))
            {
                double compareValue = double.Parse(paramStr.Substring(2));
                result = Math.Abs(numValue - compareValue) < 0.0001;
            }
            else if (paramStr.StartsWith(">"))
            {
                double compareValue = double.Parse(paramStr.Substring(1));
                result = numValue > compareValue;
            }
            else if (paramStr.StartsWith("<"))
            {
                double compareValue = double.Parse(paramStr.Substring(1));
                result = numValue < compareValue;
            }

            return result ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 多值布尔 AND 转换器
    /// </summary>
    public class MultiBoolAndConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length == 0)
                return false;

            foreach (var value in values)
            {
                if (value is bool b && !b)
                    return false;
                if (value == DependencyProperty.UnsetValue)
                    return false;
            }

            return true;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 多值布尔 OR 转换器
    /// </summary>
    public class MultiBoolOrConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length == 0)
                return false;

            foreach (var value in values)
            {
                if (value is bool b && b)
                    return true;
            }

            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 多值布尔转可见性转换器（AND 逻辑）
    /// </summary>
    public class MultiBoolToVisibilityConverter : IMultiValueConverter
    {
        /// <summary>
        /// 使用 OR 逻辑（默认是 AND）
        /// </summary>
        public bool UseOr { get; set; } = false;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length == 0)
                return Visibility.Collapsed;

            bool result;

            if (UseOr)
            {
                result = false;
                foreach (var value in values)
                {
                    if (value is bool b && b)
                    {
                        result = true;
                        break;
                    }
                }
            }
            else
            {
                result = true;
                foreach (var value in values)
                {
                    if (value is bool b && !b)
                    {
                        result = false;
                        break;
                    }
                    if (value == DependencyProperty.UnsetValue)
                    {
                        result = false;
                        break;
                    }
                }
            }

            return result ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 字符串格式化转换器
    /// </summary>
    /// <remarks>
    /// 使用方式：
    /// Text="{Binding Value, Converter={StaticResource StringFormatConverter}, ConverterParameter='当前值: {0}'}"
    /// </remarks>
    public class StringFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is string format && value != null)
            {
                return string.Format(format, value);
            }
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 双精度值乘法转换器
    /// </summary>
    /// <remarks>
    /// 使用方式：
    /// Width="{Binding ActualWidth, Converter={StaticResource DoubleMultiplierConverter}, ConverterParameter=0.5}"
    /// </remarks>
    public class DoubleMultiplierConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d && parameter != null)
            {
                if (double.TryParse(parameter.ToString(), out double multiplier))
                {
                    return d * multiplier;
                }
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d && parameter != null)
            {
                if (double.TryParse(parameter.ToString(), out double multiplier) && multiplier != 0)
                {
                    return d / multiplier;
                }
            }
            return value;
        }
    }

    /// <summary>
    /// 索引比较转布尔值转换器
    /// </summary>
    /// <remarks>
    /// 用于 TabControl 或 ListBox 等控件的索引比较
    /// IsChecked="{Binding SelectedIndex, Converter={StaticResource IndexToBoolConverter}, ConverterParameter=0}"
    /// </remarks>
    public class IndexToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int index && parameter != null)
            {
                if (int.TryParse(parameter.ToString(), out int targetIndex))
                {
                    return index == targetIndex;
                }
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b && parameter != null)
            {
                if (int.TryParse(parameter.ToString(), out int targetIndex))
                {
                    return targetIndex;
                }
            }
            return Binding.DoNothing;
        }
    }

    /// <summary>
    /// 布尔值反转转可见性转换器
    /// </summary>
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return b ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Collapsed;
            }
            return true;
        }
    }

    /// <summary>
    /// 布尔值转背景色转换器
    /// </summary>
    public class BooleanToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isBlack)
            {
                return isBlack ? new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)) : new SolidColorBrush(Color.FromArgb(200, 255, 255, 255));
            }
            return new SolidColorBrush(Color.FromArgb(200, 0, 0, 0));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}