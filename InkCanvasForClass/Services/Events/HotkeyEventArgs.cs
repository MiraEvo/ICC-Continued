using System;
using System.Windows.Input;

namespace Ink_Canvas.Services.Events
{
    /// <summary>
    /// 热键按下事件参数
    /// </summary>
    public class HotkeyPressedEventArgs : EventArgs
    {
        /// <summary>
        /// 热键标识符
        /// </summary>
        public string HotkeyId { get; }

        /// <summary>
        /// 热键组合
        /// </summary>
        public KeyGesture Gesture { get; }

        /// <summary>
        /// 触发时间
        /// </summary>
        public DateTime PressedAt { get; }

        /// <summary>
        /// 是否已处理
        /// </summary>
        public bool Handled { get; set; }

        public HotkeyPressedEventArgs(string hotkeyId, KeyGesture gesture)
        {
            HotkeyId = hotkeyId;
            Gesture = gesture;
            PressedAt = DateTime.Now;
            Handled = false;
        }
    }

    /// <summary>
    /// 热键冲突事件参数
    /// </summary>
    public class HotkeyConflictEventArgs : EventArgs
    {
        /// <summary>
        /// 尝试注册的热键标识符
        /// </summary>
        public string ConflictingHotkeyId { get; }

        /// <summary>
        /// 冲突的热键组合
        /// </summary>
        public KeyGesture ConflictingGesture { get; }

        /// <summary>
        /// 已存在的热键标识符
        /// </summary>
        public string ExistingHotkeyId { get; }

        /// <summary>
        /// 冲突发生时间
        /// </summary>
        public DateTime ConflictAt { get; }

        public HotkeyConflictEventArgs(string conflictingHotkeyId, KeyGesture conflictingGesture, string existingHotkeyId)
        {
            ConflictingHotkeyId = conflictingHotkeyId;
            ConflictingGesture = conflictingGesture;
            ExistingHotkeyId = existingHotkeyId;
            ConflictAt = DateTime.Now;
        }
    }

    /// <summary>
    /// 热键注册事件参数
    /// </summary>
    public class HotkeyRegisteredEventArgs : EventArgs
    {
        /// <summary>
        /// 热键标识符
        /// </summary>
        public string HotkeyId { get; }

        /// <summary>
        /// 热键组合
        /// </summary>
        public KeyGesture Gesture { get; }

        /// <summary>
        /// 是否注册成功
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// 注册时间
        /// </summary>
        public DateTime RegisteredAt { get; }

        public HotkeyRegisteredEventArgs(string hotkeyId, KeyGesture gesture, bool success)
        {
            HotkeyId = hotkeyId;
            Gesture = gesture;
            Success = success;
            RegisteredAt = DateTime.Now;
        }
    }

    /// <summary>
    /// 热键注销事件参数
    /// </summary>
    public class HotkeyUnregisteredEventArgs : EventArgs
    {
        /// <summary>
        /// 热键标识符
        /// </summary>
        public string HotkeyId { get; }

        /// <summary>
        /// 注销时间
        /// </summary>
        public DateTime UnregisteredAt { get; }

        public HotkeyUnregisteredEventArgs(string hotkeyId)
        {
            HotkeyId = hotkeyId;
            UnregisteredAt = DateTime.Now;
        }
    }
}
