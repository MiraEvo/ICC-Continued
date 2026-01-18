using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Ink_Canvas.Helpers;
using JetBrains.Annotations;

namespace Ink_Canvas {
    public partial class MainWindow : Window {

        public static long GetDirectorySize(System.IO.DirectoryInfo directoryInfo, bool recursive = true)
        {
            var startDirectorySize = default(long);
            if (directoryInfo == null || !directoryInfo.Exists)
                return startDirectorySize; //Return 0 while Directory does not exist.

            //Add size of files in the Current Directory to main size.
            foreach (var fileInfo in directoryInfo.GetFiles())
                System.Threading.Interlocked.Add(ref startDirectorySize, fileInfo.Length);

            if (recursive) //Loop on Sub Direcotries in the Current Directory and Calculate it's files size.
                System.Threading.Tasks.Parallel.ForEach(directoryInfo.GetDirectories(), (subDirectory) =>
                    System.Threading.Interlocked.Add(ref startDirectorySize, GetDirectorySize(subDirectory, recursive)));

            return startDirectorySize;
        }

        public async Task<long> GetDirectorySizeAsync(System.IO.DirectoryInfo directoryInfo, bool recursive = true) {
            try
            {
                var size = await Task.Run(()=> GetDirectorySize(directoryInfo, recursive));
                return size;
            }
            catch (UnauthorizedAccessException ex)
            {
                LogHelper.WriteLogToFile($"Access denied calculating directory size: {ex.Message}", LogHelper.LogType.Warning);
                return 0;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"Error calculating directory size async: {ex.Message}", LogHelper.LogType.Error);
                LogHelper.NewLog(ex);
                return 0;
            }
        }

        private static string FormatBytes(long bytes) {
            string[] Suffix = { "B", "KB", "MB", "GB", "TB" };
            int i; double dblSByte = bytes;
            for (i = 0; i < Suffix.Length && bytes >= 1024; i++, bytes /= 1024) {
                dblSByte = bytes / 1024.0;
            }
            return String.Format("{0:0.##} {1}", dblSByte, Suffix[i]);
        }

        private async Task<int> GetDirectoryFilesCount(string path) {
            try
            {
                var count = await Task.Run(() => Directory.GetFiles(path, "*", SearchOption.AllDirectories).Length);
                return count;
            }
            catch (UnauthorizedAccessException ex)
            {
                LogHelper.WriteLogToFile($"Access denied counting files in directory: {ex.Message}", LogHelper.LogType.Warning);
                return 0;
            }
            catch (DirectoryNotFoundException ex)
            {
                LogHelper.WriteLogToFile($"Directory not found when counting files: {ex.Message}", LogHelper.LogType.Warning);
                return 0;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"Error counting directory files: {ex.Message}", LogHelper.LogType.Error);
                LogHelper.NewLog(ex);
                return 0;
            }
        }

        /// <summary>
        /// 根据 StorageLocation 获取对应的存储路径
        /// </summary>
        private string GetStoragePathFromStorageLocation() {
            string path;
            var storageLocation = Settings.Storage.StorageLocation;

            if (storageLocation == "c-") {
                // 自定义存储位置
                if (!string.IsNullOrEmpty(Settings.Storage.UserStorageLocation)) {
                    path = new DirectoryInfo(Settings.Storage.UserStorageLocation).FullName;
                } else {
                    // 回退到安装目录
                    var runfolder = AppDomain.CurrentDomain.BaseDirectory;
                    path = (runfolder.EndsWith("\\") ? runfolder.Substring(0, runfolder.Length - 1) : runfolder) + "\\Data";
                }
            } else if (storageLocation.StartsWith("d")) {
                // 磁盘驱动器存储
                var driveLetter = storageLocation.Substring(1).ToUpper();
                path = driveLetter + ":\\InkCanvasForClass";
            } else if (storageLocation == "fw") {
                // 文档文件夹
                var docfolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                path = (docfolder.EndsWith("\\") ? docfolder.Substring(0, docfolder.Length - 1) : docfolder) + "\\InkCanvasForClass";
            } else if (storageLocation == "fr") {
                // icc安装目录
                var runfolder = AppDomain.CurrentDomain.BaseDirectory;
                path = (runfolder.EndsWith("\\") ? runfolder.Substring(0, runfolder.Length - 1) : runfolder) + "\\Data";
            } else if (storageLocation == "fu") {
                // 当前用户目录
                var usrfolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                path = (usrfolder.EndsWith("\\") ? usrfolder.Substring(0, usrfolder.Length - 1) : usrfolder) + "\\InkCanvasForClass";
            } else if (storageLocation == "fd") {
                // 桌面文件夹
                var dskfolder = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                path = (dskfolder.EndsWith("\\") ? dskfolder.Substring(0, dskfolder.Length - 1) : dskfolder) + "\\InkCanvasForClass";
            } else {
                // 默认使用安装目录
                var runfolder = AppDomain.CurrentDomain.BaseDirectory;
                path = (runfolder.EndsWith("\\") ? runfolder.Substring(0, runfolder.Length - 1) : runfolder) + "\\Data";
            }

            return path;
        }

        /// <summary>
        /// 同步 AutoSavedStrokesLocation 与 StorageLocation
        /// 确保它们始终保持一致
        /// </summary>
        private void SyncAutoSavedStrokesLocationWithStorageLocation() {
            var expectedPath = GetStoragePathFromStorageLocation();
            var currentPath = Settings.Automation.AutoSavedStrokesLocation?.TrimEnd('\\') ?? "";
            var expectedPathNormalized = expectedPath.TrimEnd('\\');

            // 如果当前路径与期望路径不同，则更新
            if (!string.Equals(currentPath, expectedPathNormalized, StringComparison.OrdinalIgnoreCase)) {
                LogHelper.WriteLogToFile($"Syncing AutoSavedStrokesLocation: '{currentPath}' -> '{expectedPathNormalized}'", LogHelper.LogType.Info);
                Settings.Automation.AutoSavedStrokesLocation = expectedPathNormalized;
                SaveSettings();
            }

            // 确保文件夹结构存在
            EnsureStorageFoldersExist(expectedPathNormalized);
        }

        /// <summary>
        /// 确保存储文件夹结构存在
        /// </summary>
        private void EnsureStorageFoldersExist(string path) {
            try {
                var basePath = new DirectoryInfo(path);
                var autoSavedInkPath = new DirectoryInfo(path+"\\AutoSavedInk");
                var autoSavedSnapshotPath = new DirectoryInfo(path+"\\AutoSavedSnapshot");
                var exportedInkPath = new DirectoryInfo(path+"\\ExportedInk");
                var quotedPhotosPath = new DirectoryInfo(path+"\\QuotedPhotos");
                var cachesPath = new DirectoryInfo(path+"\\caches");
                var paths = new DirectoryInfo[] {
                    basePath, autoSavedInkPath, autoSavedSnapshotPath, exportedInkPath, quotedPhotosPath, cachesPath
                };
                foreach (var di in paths) {
                    if (!di.Exists) di.Create();
                }
            }
            catch (Exception ex) {
                LogHelper.WriteLogToFile($"Error creating storage folders: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        private void InitStorageFoldersStructure(string dirPath) {
            string path;
            if (dirPath == null) {
                // 使用 GetStoragePathFromStorageLocation 获取正确的路径
                path = GetStoragePathFromStorageLocation();
            } else {
                path = dirPath;
            }

            // 确保文件夹结构存在
            EnsureStorageFoldersExist(path);

            // 更新 AutoSavedStrokesLocation 到选中的存储位置
            Settings.Automation.AutoSavedStrokesLocation = path;
            SaveSettings();
        }

        private void InitStorageManagementModule() {
            // 确保 AutoSavedStrokesLocation 与 StorageLocation 同步
            // 只有在 AutoSavedStrokesLocation 为空或与当前选择不匹配时才更新
            SyncAutoSavedStrokesLocationWithStorageLocation();
        }
    }
}
