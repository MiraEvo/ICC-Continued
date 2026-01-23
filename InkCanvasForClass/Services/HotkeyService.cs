using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Ink_Canvas.Helpers;
using Ink_Canvas.Services.Events;

namespace Ink_Canvas.Services
{
    /// <summary>
    /// 热键服务实现，管理应用程序热键的注册、注销和触发
    /// </summary>
    public class HotkeyService : IHotkeyService
    {
        #region 私有字段

        private readonly Dictionary<string, HotkeyRegistration> _registrations = [];
        private readonly Dictionary<string, KeyGesture> _gestures = [];
        private readonly object _lock = new();
        private bool _isEnabled = true;

        #endregion

        #region 属性

        /// <inheritdoc/>
        public IReadOnlyDictionary<string, KeyGesture> RegisteredHotkeys
        {
            get
            {
                lock (_lock)
                {
                    return new Dictionary<string, KeyGesture>(_gestures);
                }
            }
        }

        /// <inheritdoc/>
        public bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        #endregion

        #region 事件

        /// <inheritdoc/>
        public event EventHandler<HotkeyPressedEventArgs> HotkeyPressed;

        /// <inheritdoc/>
        public event EventHandler<HotkeyConflictEventArgs> HotkeyConflict;

        /// <inheritdoc/>
        public event EventHandler<HotkeyRegisteredEventArgs> HotkeyRegistered;

        /// <inheritdoc/>
        public event EventHandler<HotkeyUnregisteredEventArgs> HotkeyUnregistered;

        #endregion

        #region 公共方法

        /// <inheritdoc/>
        public bool RegisterHotkey(string id, KeyGesture gesture, Action action)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentNullException.ThrowIfNull(gesture);
            ArgumentNullException.ThrowIfNull(action);

            lock (_lock)
            {
                // 检查ID是否已存在
                if (_registrations.ContainsKey(id))
                {
                    LogHelper.WriteLogToFile($"HotkeyService: Hotkey ID '{id}' already registered", LogHelper.LogType.Trace);
                    return false;
                }

                // 检查热键组合是否冲突
                var conflictingId = GetConflictingHotkeyIdInternal(gesture);
                if (conflictingId != null)
                {
                    LogHelper.WriteLogToFile($"HotkeyService: Hotkey conflict detected - '{id}' conflicts with '{conflictingId}'", LogHelper.LogType.Trace);
                    OnHotkeyConflict(new HotkeyConflictEventArgs(id, gesture, conflictingId));
                    OnHotkeyRegistered(new HotkeyRegisteredEventArgs(id, gesture, false));
                    return false;
                }

                // 注册热键
                var registration = new HotkeyRegistration(id, gesture, action);
                _registrations[id] = registration;
                _gestures[id] = gesture;

                LogHelper.WriteLogToFile($"HotkeyService: Registered hotkey '{id}' with gesture {FormatGesture(gesture)}", LogHelper.LogType.Trace);
                OnHotkeyRegistered(new HotkeyRegisteredEventArgs(id, gesture, true));
                return true;
            }
        }

        /// <inheritdoc/>
        public bool RegisterHotkey<T>(string id, KeyGesture gesture, Action<T> action, T parameter)
        {
            ArgumentNullException.ThrowIfNull(action);

            return RegisterHotkey(id, gesture, () => action(parameter));
        }

        /// <inheritdoc/>
        public bool UnregisterHotkey(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return false;

            lock (_lock)
            {
                if (!_registrations.ContainsKey(id))
                {
                    LogHelper.WriteLogToFile($"HotkeyService: Hotkey ID '{id}' not found for unregistration", LogHelper.LogType.Trace);
                    return false;
                }

                _registrations.Remove(id);
                _gestures.Remove(id);

                LogHelper.WriteLogToFile($"HotkeyService: Unregistered hotkey '{id}'", LogHelper.LogType.Trace);
                OnHotkeyUnregistered(new HotkeyUnregisteredEventArgs(id));
                return true;
            }
        }

        /// <inheritdoc/>
        public void UnregisterAllHotkeys()
        {
            lock (_lock)
            {
                var ids = _registrations.Keys.ToList();
                foreach (var id in ids)
                {
                    _registrations.Remove(id);
                    _gestures.Remove(id);
                    OnHotkeyUnregistered(new HotkeyUnregisteredEventArgs(id));
                }

                LogHelper.WriteLogToFile($"HotkeyService: Unregistered all {ids.Count} hotkeys", LogHelper.LogType.Trace);
            }
        }

        /// <inheritdoc/>
        public bool IsHotkeyRegistered(KeyGesture gesture)
        {
            if (gesture == null)
                return false;

            lock (_lock)
            {
                return GetConflictingHotkeyIdInternal(gesture) != null;
            }
        }

        /// <inheritdoc/>
        public bool IsHotkeyIdRegistered(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return false;

            lock (_lock)
            {
                return _registrations.ContainsKey(id);
            }
        }

        /// <inheritdoc/>
        public string GetConflictingHotkeyId(KeyGesture gesture)
        {
            if (gesture == null)
                return null;

            lock (_lock)
            {
                return GetConflictingHotkeyIdInternal(gesture);
            }
        }

        /// <inheritdoc/>
        public bool UpdateHotkeyGesture(string id, KeyGesture newGesture)
        {
            if (string.IsNullOrWhiteSpace(id))
                return false;
            if (newGesture == null)
                return false;

            lock (_lock)
            {
                if (!_registrations.TryGetValue(id, out var registration))
                {
                    LogHelper.WriteLogToFile($"HotkeyService: Cannot update - hotkey ID '{id}' not found", LogHelper.LogType.Trace);
                    return false;
                }

                // 检查新热键组合是否与其他热键冲突（排除自身）
                var conflictingId = GetConflictingHotkeyIdInternal(newGesture);
                if (conflictingId != null && conflictingId != id)
                {
                    LogHelper.WriteLogToFile($"HotkeyService: Cannot update - new gesture conflicts with '{conflictingId}'", LogHelper.LogType.Trace);
                    OnHotkeyConflict(new HotkeyConflictEventArgs(id, newGesture, conflictingId));
                    return false;
                }

                // 更新热键组合
                registration.Gesture = newGesture;
                _gestures[id] = newGesture;

                LogHelper.WriteLogToFile($"HotkeyService: Updated hotkey '{id}' to gesture {FormatGesture(newGesture)}", LogHelper.LogType.Trace);
                return true;
            }
        }

        /// <inheritdoc/>
        public bool ProcessKeyDown(KeyEventArgs e)
        {
            if (!_isEnabled || e == null)
                return false;

            // 获取当前按键状态
            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            var modifiers = Keyboard.Modifiers;

            lock (_lock)
            {
                foreach (var registration in _registrations.Values)
                {
                    if (MatchesGesture(registration.Gesture, key, modifiers))
                    {
                        try
                        {
                            // 触发热键按下事件
                            var args = new HotkeyPressedEventArgs(registration.Id, registration.Gesture);
                            OnHotkeyPressed(args);

                            if (!args.Handled)
                            {
                                // 执行注册的操作
                                registration.Action?.Invoke();
                                LogHelper.WriteLogToFile($"HotkeyService: Executed hotkey '{registration.Id}'", LogHelper.LogType.Trace);
                            }

                            e.Handled = true;
                            return true;
                        }
                        catch (Exception ex)
                        {
                            LogHelper.WriteLogToFile($"HotkeyService: Error executing hotkey '{registration.Id}': {ex.Message}", LogHelper.LogType.Error);
                        }
                    }
                }
            }

            return false;
        }

        /// <inheritdoc/>
        public void LoadHotkeysFromSettings()
        {
            // TODO: 从设置服务加载热键配置
            // 这需要在设置模型中添加热键配置支持
            LogHelper.WriteLogToFile("HotkeyService: LoadHotkeysFromSettings - Not yet implemented", LogHelper.LogType.Trace);
        }

        /// <inheritdoc/>
        public void SaveHotkeysToSettings()
        {
            // TODO: 保存热键配置到设置服务
            // 这需要在设置模型中添加热键配置支持
            LogHelper.WriteLogToFile("HotkeyService: SaveHotkeysToSettings - Not yet implemented", LogHelper.LogType.Trace);
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 检查热键组合是否与已注册的热键冲突（内部方法，不加锁）
        /// </summary>
        private string GetConflictingHotkeyIdInternal(KeyGesture gesture)
        {
            foreach (var kvp in _gestures)
            {
                if (GesturesMatch(kvp.Value, gesture))
                {
                    return kvp.Key;
                }
            }
            return null;
        }

        /// <summary>
        /// 比较两个热键组合是否相同
        /// </summary>
        private static bool GesturesMatch(KeyGesture g1, KeyGesture g2)
        {
            if (g1 == null || g2 == null)
                return false;

            return g1.Key == g2.Key && g1.Modifiers == g2.Modifiers;
        }

        /// <summary>
        /// 检查按键事件是否匹配热键组合
        /// </summary>
        private static bool MatchesGesture(KeyGesture gesture, Key key, ModifierKeys modifiers)
        {
            if (gesture == null)
                return false;

            return gesture.Key == key && gesture.Modifiers == modifiers;
        }

        /// <summary>
        /// 格式化热键组合为字符串
        /// </summary>
        private static string FormatGesture(KeyGesture gesture)
        {
            if (gesture == null)
                return "None";

            var parts = new List<string>();

            if (gesture.Modifiers.HasFlag(ModifierKeys.Control))
                parts.Add("Ctrl");
            if (gesture.Modifiers.HasFlag(ModifierKeys.Alt))
                parts.Add("Alt");
            if (gesture.Modifiers.HasFlag(ModifierKeys.Shift))
                parts.Add("Shift");
            if (gesture.Modifiers.HasFlag(ModifierKeys.Windows))
                parts.Add("Win");

            parts.Add(gesture.Key.ToString());

            return string.Join("+", parts);
        }

        #endregion

        #region 事件触发方法

        protected virtual void OnHotkeyPressed(HotkeyPressedEventArgs e)
        {
            HotkeyPressed?.Invoke(this, e);
        }

        protected virtual void OnHotkeyConflict(HotkeyConflictEventArgs e)
        {
            HotkeyConflict?.Invoke(this, e);
        }

        protected virtual void OnHotkeyRegistered(HotkeyRegisteredEventArgs e)
        {
            HotkeyRegistered?.Invoke(this, e);
        }

        protected virtual void OnHotkeyUnregistered(HotkeyUnregisteredEventArgs e)
        {
            HotkeyUnregistered?.Invoke(this, e);
        }

        #endregion

        #region 内部类

        /// <summary>
        /// 热键注册信息
        /// </summary>
        private class HotkeyRegistration(string id, KeyGesture gesture, Action action)
        {
            public string Id { get; } = id;
            public KeyGesture Gesture { get; set; } = gesture;
            public Action Action { get; } = action;
        }

        #endregion
    }
}
