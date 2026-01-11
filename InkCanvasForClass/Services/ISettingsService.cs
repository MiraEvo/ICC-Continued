using System;
using System.Threading.Tasks;
using Ink_Canvas.Models.Settings;
using Ink_Canvas.Services.Events;

namespace Ink_Canvas.Services
{
    /// <summary>
    /// 设置服务接口，定义设置的加载、保存和访问操作
    /// </summary>
    public interface ISettingsService
    {
        #region 属性

        /// <summary>
        /// 获取当前设置对象
        /// </summary>
        Settings Settings { get; }

        /// <summary>
        /// 设置是否已加载
        /// </summary>
        bool IsLoaded { get; }

        /// <summary>
        /// 设置文件路径
        /// </summary>
        string SettingsFilePath { get; }

        #endregion

        #region 分类访问属性

        /// <summary>
        /// 画布设置
        /// </summary>
        CanvasSettings Canvas { get; }

        /// <summary>
        /// 外观设置
        /// </summary>
        AppearanceSettings Appearance { get; }

        /// <summary>
        /// 手势设置
        /// </summary>
        GestureSettings Gesture { get; }

        /// <summary>
        /// PowerPoint 设置
        /// </summary>
        PowerPointSettings PowerPoint { get; }

        /// <summary>
        /// 自动化设置
        /// </summary>
        AutomationSettings Automation { get; }

        /// <summary>
        /// 高级设置
        /// </summary>
        AdvancedSettings Advanced { get; }

        /// <summary>
        /// 墨迹转形状设置
        /// </summary>
        InkToShapeSettings InkToShape { get; }

        /// <summary>
        /// 启动设置
        /// </summary>
        StartupSettings Startup { get; }

        /// <summary>
        /// 截图设置
        /// </summary>
        SnapshotSettings Snapshot { get; }

        /// <summary>
        /// 存储设置
        /// </summary>
        StorageSettings Storage { get; }

        /// <summary>
        /// 随机点名设置
        /// </summary>
        RandSettings RandSettings { get; }

        #endregion

        #region 方法

        /// <summary>
        /// 从文件加载设置
        /// </summary>
        /// <returns>是否加载成功</returns>
        bool Load();

        /// <summary>
        /// 异步从文件加载设置
        /// </summary>
        /// <returns>是否加载成功</returns>
        Task<bool> LoadAsync();

        /// <summary>
        /// 保存设置到文件
        /// </summary>
        /// <returns>是否保存成功</returns>
        bool Save();

        /// <summary>
        /// 异步保存设置到文件
        /// </summary>
        /// <returns>是否保存成功</returns>
        Task<bool> SaveAsync();

        /// <summary>
        /// 重置设置为默认值
        /// </summary>
        void ResetToDefaults();

        /// <summary>
        /// 从外部设置对象同步设置
        /// 用于与 MainWindow.Settings 保持同步
        /// </summary>
        /// <param name="externalSettings">外部设置对象</param>
        void SyncFrom(Settings externalSettings);

        #endregion

        #region 事件

        /// <summary>
        /// 设置加载完成事件
        /// </summary>
        event EventHandler<SettingsLoadedEventArgs> SettingsLoaded;

        /// <summary>
        /// 设置保存完成事件
        /// </summary>
        event EventHandler<SettingsSavedEventArgs> SettingsSaved;

        /// <summary>
        /// 单个设置项变更事件
        /// </summary>
        event EventHandler<SettingChangedEventArgs> SettingChanged;

        /// <summary>
        /// 设置变更事件（向后兼容）
        /// </summary>
        [Obsolete("请使用 SettingChanged 事件代替")]
        event EventHandler<SettingsChangedEventArgs> SettingsChanged;

        #endregion
    }

    /// <summary>
    /// 设置变更事件参数（向后兼容）
    /// </summary>
    [Obsolete("请使用 Ink_Canvas.Services.Events.SettingChangedEventArgs 代替")]
    public class SettingsChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 变更的设置分类名称
        /// </summary>
        public string CategoryName { get; }

        /// <summary>
        /// 变更的属性名称
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// 旧值
        /// </summary>
        public object OldValue { get; }

        /// <summary>
        /// 新值
        /// </summary>
        public object NewValue { get; }

        public SettingsChangedEventArgs(string categoryName, string propertyName, object oldValue, object newValue)
        {
            CategoryName = categoryName;
            PropertyName = propertyName;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}
