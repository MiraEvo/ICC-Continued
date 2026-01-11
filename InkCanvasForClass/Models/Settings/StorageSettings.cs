using Newtonsoft.Json;

namespace Ink_Canvas.Models.Settings
{
    /// <summary>
    /// 存储设置类
    /// </summary>
    public class StorageSettings : SettingsBase
    {
        private string _storageLocation = "a-";
        private string _userStorageLocation = "";

        /// <summary>
        /// 存储位置标识（a- 表示自动选择）
        /// </summary>
        [JsonProperty("storageLocation")]
        public string StorageLocation
        {
            get => _storageLocation;
            set => SetProperty(ref _storageLocation, value);
        }

        /// <summary>
        /// 用户自定义存储位置
        /// </summary>
        [JsonProperty("userStorageLocation")]
        public string UserStorageLocation
        {
            get => _userStorageLocation;
            set => SetProperty(ref _userStorageLocation, value);
        }
    }
}
