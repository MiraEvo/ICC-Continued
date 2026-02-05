using System;
using System.IO;
using Newtonsoft.Json;

namespace Ink_Canvas.Models.Settings
{
    /// <summary>
    /// 存储设置类
    /// </summary>
    public class StorageSettings : SettingsBase
    {
        #region 常量

        // 旧版标识符（向后兼容）
        /// <summary>
        /// 自动存储标识（向后兼容）
        /// </summary>
        public const string AutoStorageIdentifier = "a-";

        /// <summary>
        /// 文档存储标识（向后兼容）
        /// </summary>
        public const string DocumentsStorageIdentifier = "d-";

        /// <summary>
        /// 桌面存储标识（向后兼容）
        /// </summary>
        public const string DesktopStorageIdentifier = "desk-";

        /// <summary>
        /// 自定义存储标识（向后兼容）
        /// </summary>
        public const string CustomStorageIdentifier = "u-";

        // 新版标识符（与 ViewModel 一致）
        /// <summary>
        /// 自动存储标识 - 固定位置
        /// </summary>
        public const string FixedStorageIdentifier = "fr";

        /// <summary>
        /// 自动存储标识 - 跟随工作区
        /// </summary>
        public const string WorkspaceStorageIdentifier = "fw";

        /// <summary>
        /// 自动存储标识 - 用户自定义
        /// </summary>
        public const string UserStorageIdentifier = "fu";

        /// <summary>
        /// 自动存储标识 - 固定磁盘
        /// </summary>
        public const string FixedDiskStorageIdentifier = "fd";

        /// <summary>
        /// 自定义存储前缀
        /// </summary>
        public const string CustomStoragePrefix = "c-";

        /// <summary>
        /// 磁盘存储前缀
        /// </summary>
        public const string DiskStoragePrefix = "d";

        #endregion

        #region 字段

        private string _storageLocation = FixedStorageIdentifier;
        private string _userStorageLocation = "";

        #endregion

        #region 辅助属性

        /// <summary>
        /// 是否使用自动存储（由系统自动选择位置）
        /// </summary>
        [JsonIgnore]
        public bool IsAutoStorage =>
            StorageLocation == AutoStorageIdentifier ||
            StorageLocation == FixedStorageIdentifier ||
            StorageLocation == WorkspaceStorageIdentifier;

        /// <summary>
        /// 是否使用文档文件夹存储
        /// </summary>
        [JsonIgnore]
        public bool IsDocumentsStorage =>
            StorageLocation == DocumentsStorageIdentifier ||
            StorageLocation == FixedDiskStorageIdentifier;

        /// <summary>
        /// 是否使用桌面存储
        /// </summary>
        [JsonIgnore]
        public bool IsDesktopStorage => StorageLocation == DesktopStorageIdentifier;

        /// <summary>
        /// 是否使用自定义存储位置
        /// </summary>
        [JsonIgnore]
        public bool IsCustomStorage =>
            StorageLocation == CustomStorageIdentifier ||
            StorageLocation == UserStorageIdentifier ||
            StorageLocation.StartsWith(CustomStoragePrefix);

        /// <summary>
        /// 是否使用磁盘存储
        /// </summary>
        [JsonIgnore]
        public bool IsDiskStorage => StorageLocation.StartsWith(DiskStoragePrefix);

        /// <summary>
        /// 是否有自定义存储位置设置
        /// </summary>
        [JsonIgnore]
        public bool HasCustomStorageLocation => IsCustomStorage && !string.IsNullOrWhiteSpace(UserStorageLocation);

        /// <summary>
        /// 自定义存储位置是否存在
        /// </summary>
        [JsonIgnore]
        public bool CustomStorageLocationExists => HasCustomStorageLocation && Directory.Exists(UserStorageLocation);

        /// <summary>
        /// 获取磁盘盘符（如果是磁盘存储）
        /// </summary>
        [JsonIgnore]
        public string DiskDriveLetter
        {
            get
            {
                if (IsDiskStorage && StorageLocation.Length > 1)
                {
                    return StorageLocation.Substring(1) + ":\\";
                }
                return null;
            }
        }

        #endregion

        #region 属性

        /// <summary>
        /// 存储位置标识
        /// fr = 固定位置
        /// fw = 跟随工作区
        /// fu = 用户自定义
        /// fd = 固定磁盘
        /// c-{path} = 自定义路径
        /// d{X} = 磁盘 X（如 dC 表示 C 盘）
        /// 
        /// 向后兼容的旧标识符：
        /// a- = 自动选择
        /// d- = 文档文件夹
        /// desk- = 桌面
        /// u- = 用户自定义
        /// </summary>
        [JsonProperty("storageLocation")]
        public string StorageLocation
        {
            get => _storageLocation;
            set => SetProperty(ref _storageLocation, ValidateStorageLocation(value));
        }

        /// <summary>
        /// 用户自定义存储位置（当 StorageLocation 为自定义类型时使用）
        /// </summary>
        [JsonProperty("userStorageLocation")]
        public string UserStorageLocation
        {
            get => _userStorageLocation;
            set => SetProperty(ref _userStorageLocation, value ?? "");
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 验证存储位置标识是否有效
        /// </summary>
        /// <param name="location">存储位置标识</param>
        /// <returns>验证后的标识</returns>
        private string ValidateStorageLocation(string location)
        {
            if (string.IsNullOrWhiteSpace(location))
                return FixedStorageIdentifier;

            // 新版标识符
            var newValidLocations = new[]
            {
                FixedStorageIdentifier,
                WorkspaceStorageIdentifier,
                UserStorageIdentifier,
                FixedDiskStorageIdentifier
            };

            if (Array.Exists(newValidLocations, loc => loc == location))
                return location;

            // 向后兼容的旧标识符
            var oldValidLocations = new[]
            {
                AutoStorageIdentifier,
                DocumentsStorageIdentifier,
                DesktopStorageIdentifier,
                CustomStorageIdentifier
            };

            if (Array.Exists(oldValidLocations, loc => loc == location))
                return location;

            // 自定义路径前缀
            if (location.StartsWith(CustomStoragePrefix))
                return location;

            // 磁盘标识符（如 dC, dD 等）
            if (location.StartsWith(DiskStoragePrefix) && location.Length == 2 && char.IsLetter(location[1]))
                return location;

            return FixedStorageIdentifier;
        }

        /// <summary>
        /// 获取实际的存储路径
        /// </summary>
        /// <returns>存储路径</returns>
        public string GetActualStoragePath()
        {
            // 自定义存储
            if (IsCustomStorage && HasCustomStorageLocation)
            {
                return UserStorageLocation;
            }

            // 自定义路径前缀
            if (StorageLocation.StartsWith(CustomStoragePrefix) && StorageLocation.Length > 2)
            {
                var customPath = StorageLocation.Substring(2);
                if (Directory.Exists(customPath))
                    return customPath;
            }

            // 磁盘存储
            if (IsDiskStorage)
            {
                var driveLetter = DiskDriveLetter;
                if (!string.IsNullOrEmpty(driveLetter) && Directory.Exists(driveLetter))
                    return driveLetter;
            }

            // 文档文件夹
            if (IsDocumentsStorage)
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }

            // 桌面
            if (IsDesktopStorage)
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            }

            // 默认：固定位置（应用程序目录）
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        /// <summary>
        /// 确保存储目录存在
        /// </summary>
        /// <returns>如果目录存在或创建成功返回 true</returns>
        public bool EnsureStorageLocationExists()
        {
            try
            {
                var path = GetActualStoragePath();
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取指定文件的完整路径
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>完整路径</returns>
        public string GetFullPath(string fileName)
        {
            return Path.Combine(GetActualStoragePath(), fileName);
        }

        /// <summary>
        /// 设置为固定位置存储
        /// </summary>
        public void SetFixedStorage()
        {
            StorageLocation = FixedStorageIdentifier;
        }

        /// <summary>
        /// 设置为跟随工作区存储
        /// </summary>
        public void SetWorkspaceStorage()
        {
            StorageLocation = WorkspaceStorageIdentifier;
        }

        /// <summary>
        /// 设置为用户自定义存储
        /// </summary>
        public void SetUserStorage()
        {
            StorageLocation = UserStorageIdentifier;
        }

        /// <summary>
        /// 设置为固定磁盘存储
        /// </summary>
        public void SetFixedDiskStorage()
        {
            StorageLocation = FixedDiskStorageIdentifier;
        }

        /// <summary>
        /// 设置为磁盘存储
        /// </summary>
        /// <param name="driveLetter">盘符（如 'C', 'D'）</param>
        public void SetDiskStorage(char driveLetter)
        {
            StorageLocation = DiskStoragePrefix + char.ToUpperInvariant(driveLetter);
        }

        /// <summary>
        /// 设置为自定义路径存储
        /// </summary>
        /// <param name="path">自定义路径</param>
        public void SetCustomPathStorage(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("自定义存储路径不能为空", nameof(path));

            StorageLocation = CustomStoragePrefix + path;
        }

        /// <summary>
        /// 设置为自定义存储位置（旧版兼容）
        /// </summary>
        /// <param name="path">自定义路径</param>
        public void SetCustomStorage(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("自定义存储路径不能为空", nameof(path));

            StorageLocation = UserStorageIdentifier;
            UserStorageLocation = path;
        }

        /// <summary>
        /// 设置为自动存储（旧版兼容）
        /// </summary>
        public void SetAutoStorage()
        {
            StorageLocation = FixedStorageIdentifier;
        }

        /// <summary>
        /// 设置为文档文件夹存储（旧版兼容）
        /// </summary>
        public void SetDocumentsStorage()
        {
            StorageLocation = FixedDiskStorageIdentifier;
        }

        /// <summary>
        /// 设置为桌面存储（旧版兼容）
        /// </summary>
        public void SetDesktopStorage()
        {
            StorageLocation = DesktopStorageIdentifier;
        }

        #endregion
    }
}
