using System;

namespace Ink_Canvas.Services.Events
{
    /// <summary>
    /// 设置加载完成事件参数
    /// </summary>
    public class SettingsLoadedEventArgs : EventArgs
    {
        /// <summary>
        /// 设置文件路径
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// 是否从文件加载成功
        /// </summary>
        public bool LoadedFromFile { get; }

        /// <summary>
        /// 是否使用默认设置
        /// </summary>
        public bool IsDefault { get; }

        /// <summary>
        /// 加载时间
        /// </summary>
        public DateTime LoadedAt { get; }

        public SettingsLoadedEventArgs(string filePath, bool loadedFromFile, bool isDefault)
        {
            FilePath = filePath;
            LoadedFromFile = loadedFromFile;
            IsDefault = isDefault;
            LoadedAt = DateTime.Now;
        }
    }

    /// <summary>
    /// 设置保存完成事件参数
    /// </summary>
    public class SettingsSavedEventArgs : EventArgs
    {
        /// <summary>
        /// 设置文件路径
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// 是否保存成功
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// 保存时间
        /// </summary>
        public DateTime SavedAt { get; }

        /// <summary>
        /// 错误消息（如果保存失败）
        /// </summary>
        public string ErrorMessage { get; }

        public SettingsSavedEventArgs(string filePath, bool success, string errorMessage = null)
        {
            FilePath = filePath;
            Success = success;
            SavedAt = DateTime.Now;
            ErrorMessage = errorMessage;
        }
    }

    /// <summary>
    /// 单个设置项变更事件参数
    /// </summary>
    public class SettingChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 设置分类名称（如 Canvas, Appearance 等）
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

        /// <summary>
        /// 变更时间
        /// </summary>
        public DateTime ChangedAt { get; }

        public SettingChangedEventArgs(string categoryName, string propertyName, object oldValue, object newValue)
        {
            CategoryName = categoryName;
            PropertyName = propertyName;
            OldValue = oldValue;
            NewValue = newValue;
            ChangedAt = DateTime.Now;
        }

        /// <summary>
        /// 获取完整的属性路径（分类名.属性名）
        /// </summary>
        public string FullPropertyPath => $"{CategoryName}.{PropertyName}";
    }
}
