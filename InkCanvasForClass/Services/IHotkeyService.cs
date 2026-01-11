using System;
using System.Collections.Generic;
using System.Windows.Input;
using Ink_Canvas.Services.Events;

namespace Ink_Canvas.Services
{
    /// <summary>
    /// 热键服务接口，定义热键的注册、注销和管理操作
    /// </summary>
    public interface IHotkeyService
    {
        #region 属性

        /// <summary>
        /// 获取所有已注册的热键
        /// </summary>
        IReadOnlyDictionary<string, KeyGesture> RegisteredHotkeys { get; }

        /// <summary>
        /// 热键服务是否已启用
        /// </summary>
        bool IsEnabled { get; set; }

        #endregion

        #region 方法

        /// <summary>
        /// 注册热键
        /// </summary>
        /// <param name="id">热键唯一标识符</param>
        /// <param name="gesture">热键组合</param>
        /// <param name="action">热键触发时执行的操作</param>
        /// <returns>是否注册成功</returns>
        bool RegisterHotkey(string id, KeyGesture gesture, Action action);

        /// <summary>
        /// 注册热键（带参数）
        /// </summary>
        /// <param name="id">热键唯一标识符</param>
        /// <param name="gesture">热键组合</param>
        /// <param name="action">热键触发时执行的操作</param>
        /// <param name="parameter">传递给操作的参数</param>
        /// <returns>是否注册成功</returns>
        bool RegisterHotkey<T>(string id, KeyGesture gesture, Action<T> action, T parameter);

        /// <summary>
        /// 注销热键
        /// </summary>
        /// <param name="id">热键唯一标识符</param>
        /// <returns>是否注销成功</returns>
        bool UnregisterHotkey(string id);

        /// <summary>
        /// 注销所有热键
        /// </summary>
        void UnregisterAllHotkeys();

        /// <summary>
        /// 检查热键是否已注册
        /// </summary>
        /// <param name="gesture">热键组合</param>
        /// <returns>是否已注册</returns>
        bool IsHotkeyRegistered(KeyGesture gesture);

        /// <summary>
        /// 检查热键ID是否已存在
        /// </summary>
        /// <param name="id">热键唯一标识符</param>
        /// <returns>是否已存在</returns>
        bool IsHotkeyIdRegistered(string id);

        /// <summary>
        /// 获取与指定热键组合冲突的热键ID
        /// </summary>
        /// <param name="gesture">热键组合</param>
        /// <returns>冲突的热键ID，如果没有冲突则返回null</returns>
        string GetConflictingHotkeyId(KeyGesture gesture);

        /// <summary>
        /// 更新热键组合
        /// </summary>
        /// <param name="id">热键唯一标识符</param>
        /// <param name="newGesture">新的热键组合</param>
        /// <returns>是否更新成功</returns>
        bool UpdateHotkeyGesture(string id, KeyGesture newGesture);

        /// <summary>
        /// 处理按键事件（由窗口调用）
        /// </summary>
        /// <param name="e">按键事件参数</param>
        /// <returns>是否已处理该按键</returns>
        bool ProcessKeyDown(KeyEventArgs e);

        /// <summary>
        /// 从设置加载热键配置
        /// </summary>
        void LoadHotkeysFromSettings();

        /// <summary>
        /// 保存热键配置到设置
        /// </summary>
        void SaveHotkeysToSettings();

        #endregion

        #region 事件

        /// <summary>
        /// 热键按下事件
        /// </summary>
        event EventHandler<HotkeyPressedEventArgs> HotkeyPressed;

        /// <summary>
        /// 热键冲突事件
        /// </summary>
        event EventHandler<HotkeyConflictEventArgs> HotkeyConflict;

        /// <summary>
        /// 热键注册事件
        /// </summary>
        event EventHandler<HotkeyRegisteredEventArgs> HotkeyRegistered;

        /// <summary>
        /// 热键注销事件
        /// </summary>
        event EventHandler<HotkeyUnregisteredEventArgs> HotkeyUnregistered;

        #endregion
    }
}
