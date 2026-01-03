using System;
using System.Threading.Tasks;

namespace Ink_Canvas.Services
{
    /// <summary>
    /// 设置服务接口，定义设置的加载、保存和访问操作
    /// </summary>
    public interface ISettingsService
    {
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
        /// 设置变更事件
        /// </summary>
        event EventHandler<SettingsChangedEventArgs> SettingsChanged;
    }

    /// <summary>
    /// 设置变更事件参数
    /// </summary>
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