using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Ink_Canvas.Models.Settings
{
    /// <summary>
    /// 扩展的属性变更事件参数，包含旧值和新值
    /// </summary>
    public class ExtendedPropertyChangedEventArgs : PropertyChangedEventArgs
    {
        /// <summary>
        /// 旧值
        /// </summary>
        public object OldValue { get; }

        /// <summary>
        /// 新值
        /// </summary>
        public object NewValue { get; }

        public ExtendedPropertyChangedEventArgs(string propertyName, object oldValue, object newValue)
            : base(propertyName)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }

    /// <summary>
    /// Settings 基类，提供属性变更通知和验证功能
    /// </summary>
    public abstract class SettingsBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 扩展的属性变更事件，包含旧值和新值
        /// </summary>
        public event EventHandler<ExtendedPropertyChangedEventArgs> ExtendedPropertyChanged;

        /// <summary>
        /// 设置属性值并触发变更通知
        /// </summary>
        /// <typeparam name="T">属性类型</typeparam>
        /// <param name="field">字段引用</param>
        /// <param name="value">新值</param>
        /// <param name="propertyName">属性名称（自动获取）</param>
        /// <returns>如果值已更改则返回 true</returns>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            var oldValue = field;
            field = value;
            OnPropertyChanged(propertyName, oldValue, value);
            return true;
        }

        /// <summary>
        /// 触发属性变更事件
        /// </summary>
        /// <param name="propertyName">属性名称</param>
        /// <param name="oldValue">旧值</param>
        /// <param name="newValue">新值</param>
        protected virtual void OnPropertyChanged(string propertyName, object oldValue, object newValue)
        {
            // 触发标准的 PropertyChanged 事件
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            
            // 触发扩展的 PropertyChanged 事件，包含旧值和新值
            ExtendedPropertyChanged?.Invoke(this, new ExtendedPropertyChangedEventArgs(propertyName, oldValue, newValue));
        }

        /// <summary>
        /// 触发属性变更事件（简化版本）
        /// </summary>
        /// <param name="propertyName">属性名称</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 验证数值范围
        /// </summary>
        /// <typeparam name="T">数值类型</typeparam>
        /// <param name="value">要验证的值</param>
        /// <param name="min">最小值</param>
        /// <param name="max">最大值</param>
        /// <param name="propertyName">属性名称（用于错误消息）</param>
        /// <returns>验证通过的值</returns>
        /// <exception cref="ArgumentOutOfRangeException">当值超出范围时抛出</exception>
        protected T ValidateRange<T>(T value, T min, T max, [CallerMemberName] string propertyName = null) 
            where T : IComparable<T>
        {
            if (value.CompareTo(min) < 0)
                throw new ArgumentOutOfRangeException(propertyName, value, $"值不能小于 {min}");
            if (value.CompareTo(max) > 0)
                throw new ArgumentOutOfRangeException(propertyName, value, $"值不能大于 {max}");
            return value;
        }

        /// <summary>
        /// 验证数值范围并自动钳制到有效范围
        /// </summary>
        /// <typeparam name="T">数值类型</typeparam>
        /// <param name="value">要验证的值</param>
        /// <param name="min">最小值</param>
        /// <param name="max">最大值</param>
        /// <returns>钳制后的值</returns>
        protected T ClampRange<T>(T value, T min, T max) where T : IComparable<T>
        {
            if (value.CompareTo(min) < 0)
                return min;
            if (value.CompareTo(max) > 0)
                return max;
            return value;
        }

        /// <summary>
        /// 验证字符串不为空
        /// </summary>
        /// <param name="value">要验证的字符串</param>
        /// <param name="propertyName">属性名称</param>
        /// <returns>验证通过的字符串</returns>
        /// <exception cref="ArgumentException">当字符串为空时抛出</exception>
        protected string ValidateNotEmpty(string value, [CallerMemberName] string propertyName = null)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"{propertyName} 不能为空", propertyName);
            return value;
        }
    }
}
