using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ink_Canvas.Helpers;
using Ink_Canvas.Models.Settings;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace Ink_Canvas.ViewModels.Settings
{
    public class StorageLocationOption
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Tag { get; set; }
        public string Icon { get; set; }
    }

    public partial class StorageSettingsViewModel : ObservableObject
    {
        private readonly StorageSettings _settings;
        private readonly Action _saveAction;

        public ObservableCollection<StorageLocationOption> StorageLocationOptions { get; } = new ObservableCollection<StorageLocationOption>();

        public StorageSettingsViewModel(StorageSettings settings, Action saveAction)
        {
            _settings = settings;
            _saveAction = saveAction;

            InitializeStorageOptions();
            
            // Initialize storage analysis
            _ = Task.Run(RefreshStorageAnalysisAsync);
        }

        private void InitializeStorageOptions()
        {
            StorageLocationOptions.Add(new StorageLocationOption
            {
                Name = "ICC 安装目录",
                Path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data"),
                Tag = "fr",
                Icon = "\uE8B7"
            });

            StorageLocationOptions.Add(new StorageLocationOption
            {
                Name = "“文档”文件夹",
                Path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Tag = "fw",
                Icon = "\uF000"
            });

            StorageLocationOptions.Add(new StorageLocationOption
            {
                Name = "当前用户目录",
                Path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                Tag = "fu",
                Icon = "\uE77B"
            });

            StorageLocationOptions.Add(new StorageLocationOption
            {
                Name = "“桌面”文件夹",
                Path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Tag = "fd",
                Icon = "\uE8D8"
            });

            StorageLocationOptions.Add(new StorageLocationOption
            {
                Name = "自定义...",
                Path = "",
                Tag = "c-",
                Icon = "\uE8B7"
            });
        }

        [ObservableProperty]
        private string _storageLocation;

        [ObservableProperty]
        private string _userStorageLocation;

        partial void OnStorageLocationChanged(string value)
        {
            _settings.StorageLocation = value;
            _saveAction?.Invoke();
            OnPropertyChanged(nameof(CurrentStoragePath));
            // Refresh storage analysis when location changes
            RefreshStorageAnalysis();
        }

        partial void OnUserStorageLocationChanged(string value)
        {
            _settings.UserStorageLocation = value;
            _saveAction?.Invoke();
            OnPropertyChanged(nameof(CurrentStoragePath));
            // Refresh storage analysis when location changes
            RefreshStorageAnalysis();
        }

        // Disk Usage Properties (Real Data)
        [ObservableProperty] private double _totalSpace;
        [ObservableProperty] private double _usedSpace;
        [ObservableProperty] private double _freeSpace;
        [ObservableProperty] private double _iccDataSpace;

        public double OtherUsedSpace => Math.Max(0, UsedSpace - IccDataSpace);

        partial void OnUsedSpaceChanged(double value) => OnPropertyChanged(nameof(OtherUsedSpace));
        partial void OnIccDataSpaceChanged(double value) => OnPropertyChanged(nameof(OtherUsedSpace));

        // ICC Data Breakdown
        [ObservableProperty] private double _autoSavedInkSize;
        [ObservableProperty] private double _boardImageRefSize;
        [ObservableProperty] private double _exportedBoardSize;
        [ObservableProperty] private double _cacheSize;
        [ObservableProperty] private double _autoSavedScreenshotSize;

        [ObservableProperty] private int _autoSavedInkCount;
        [ObservableProperty] private int _boardImageRefCount;
        [ObservableProperty] private int _exportedBoardCount;
        [ObservableProperty] private int _cacheCount;
        [ObservableProperty] private int _autoSavedScreenshotCount;

        // Percentage properties
        public double AutoSavedInkPercentage => IccDataSpace > 0 ? (AutoSavedInkSize / IccDataSpace) * 100 : 0;
        public double BoardImageRefPercentage => IccDataSpace > 0 ? (BoardImageRefSize / IccDataSpace) * 100 : 0;
        public double ExportedBoardPercentage => IccDataSpace > 0 ? (ExportedBoardSize / IccDataSpace) * 100 : 0;
        public double CachePercentage => IccDataSpace > 0 ? (CacheSize / IccDataSpace) * 100 : 0;
        public double AutoSavedScreenshotPercentage => IccDataSpace > 0 ? (AutoSavedScreenshotSize / IccDataSpace) * 100 : 0;

        public int TotalFileCount => AutoSavedInkCount + BoardImageRefCount + ExportedBoardCount + CacheCount + AutoSavedScreenshotCount;

        public string CurrentStoragePath => GetStoragePathFromStorageLocation();

        private string GetStoragePathFromStorageLocation()
        {
            string programDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
            string storageLocation = _settings.StorageLocation ?? "fr";

            if (storageLocation == "c-")
            {
                if (!string.IsNullOrWhiteSpace(_settings.UserStorageLocation))
                {
                    return new DirectoryInfo(_settings.UserStorageLocation).FullName;
                }

                return Path.Combine(programDir, "Data");
            }

            if (storageLocation.StartsWith("d", StringComparison.OrdinalIgnoreCase) && storageLocation.Length >= 2)
            {
                var driveLetter = storageLocation.Substring(1, 1).ToUpperInvariant();
                return $"{driveLetter}:\\InkCanvasForClass";
            }

            if (storageLocation == "fw")
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "InkCanvasForClass");
            }

            if (storageLocation == "fu")
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "InkCanvasForClass");
            }

            if (storageLocation == "fd")
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "InkCanvasForClass");
            }

            return Path.Combine(programDir, "Data");
        }

        [RelayCommand]
        private void BrowseFolder()
        {
            var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            dialog.Description = "选择存储位置";
            dialog.UseDescriptionForTitle = true;

            if (!string.IsNullOrEmpty(_settings.UserStorageLocation))
            {
                dialog.SelectedPath = _settings.UserStorageLocation;
            }

            if (dialog.ShowDialog() == true)
            {
                _settings.UserStorageLocation = dialog.SelectedPath;
                _saveAction?.Invoke();
                OnPropertyChanged(nameof(UserStorageLocation));
                OnPropertyChanged(nameof(CurrentStoragePath));
                RefreshStorageAnalysis();
            }
        }

        [RelayCommand]
        private void OpenFolder(string folderType)
        {
            try
            {
                string folderPath = folderType switch
                {
                    "autoSavedInk" => Path.Combine(CurrentStoragePath, "AutoSavedInk"),
                    "boardImageRef" => Path.Combine(CurrentStoragePath, "BoardImageRef"),
                    "exportedBoard" => Path.Combine(CurrentStoragePath, "ExportedBoard"),
                    "cache" => Path.Combine(CurrentStoragePath, "Cache"),
                    "autoSavedScreenshot" => Path.Combine(CurrentStoragePath, "AutoSavedScreenshots"),
                    _ => CurrentStoragePath
                };

                if (Directory.Exists(folderPath))
                {
                    Process.Start(new ProcessStartInfo { FileName = folderPath, UseShellExecute = true });
                }
                else
                {
                    MessageBox.Show($"文件夹不存在：{folderPath}", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"打开文件夹失败: {ex.Message}", LogHelper.LogType.Error);
                MessageBox.Show($"无法打开文件夹：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task ClearCache()
        {
            try
            {
                string cachePath = Path.Combine(CurrentStoragePath, "Cache");
                if (Directory.Exists(cachePath))
                {
                    var files = Directory.GetFiles(cachePath, "*.*", SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        try
                        {
                            File.SetAttributes(file, FileAttributes.Normal);
                            File.Delete(file);
                        }
                        catch (Exception ex)
                        {
                            LogHelper.WriteLogToFile($"删除缓存文件失败 {file}: {ex.Message}", LogHelper.LogType.Warning);
                        }
                    }
                    
                    await RefreshStorageAnalysisAsync();
                    MessageBox.Show("缓存清理完成", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"清理缓存失败: {ex.Message}", LogHelper.LogType.Error);
                MessageBox.Show($"清理缓存失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task ClearAutoSavedScreenshots()
        {
            try
            {
                string screenshotPath = Path.Combine(CurrentStoragePath, "AutoSavedScreenshots");
                if (Directory.Exists(screenshotPath))
                {
                    var files = Directory.GetFiles(screenshotPath, "*.png", SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        try
                        {
                            File.SetAttributes(file, FileAttributes.Normal);
                            File.Delete(file);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Failed to delete screenshot file {file}: {ex.Message}");
                        }
                    }
                    
                    await RefreshStorageAnalysisAsync();
                    MessageBox.Show("截图清理完成", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error clearing screenshots: {ex.Message}");
                MessageBox.Show($"清理截图失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void RefreshStorageAnalysisNow()
        {
            RefreshStorageAnalysis();
        }

        private async Task RefreshStorageAnalysisAsync()
        {
            try
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    // Get disk information
                    var driveInfo = new DriveInfo(CurrentStoragePath);
                    TotalSpace = driveInfo.TotalSize;
                    UsedSpace = driveInfo.TotalSize - driveInfo.AvailableFreeSpace;
                    FreeSpace = driveInfo.AvailableFreeSpace;

                    // Analyze ICC data directories
                    AnalyzeICCDataDirectories();
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error refreshing storage analysis: {ex.Message}");
            }
        }

        private void AnalyzeICCDataDirectories()
        {
            try
            {
                // Reset counters
                AutoSavedInkSize = BoardImageRefSize = ExportedBoardSize = CacheSize = AutoSavedScreenshotSize = 0;
                AutoSavedInkCount = BoardImageRefCount = ExportedBoardCount = CacheCount = AutoSavedScreenshotCount = 0;

                string storagePath = CurrentStoragePath;

                // Auto Saved Ink
                double autoSavedInkSize = 0;
                int autoSavedInkCount = 0;
                AnalyzeDirectory(Path.Combine(storagePath, "AutoSavedInk"), ref autoSavedInkSize, ref autoSavedInkCount, new[] { ".icstk", ".iccink" });
                AutoSavedInkSize = autoSavedInkSize;
                AutoSavedInkCount = autoSavedInkCount;

                // Board Image References
                double boardImageRefSize = 0;
                int boardImageRefCount = 0;
                AnalyzeDirectory(Path.Combine(storagePath, "BoardImageRef"), ref boardImageRefSize, ref boardImageRefCount, new[] { ".jpg", ".jpeg", ".png", ".bmp" });
                BoardImageRefSize = boardImageRefSize;
                BoardImageRefCount = boardImageRefCount;

                // Exported Board Files
                double exportedBoardSize = 0;
                int exportedBoardCount = 0;
                AnalyzeDirectory(Path.Combine(storagePath, "ExportedBoard"), ref exportedBoardSize, ref exportedBoardCount, new[] { ".icb", ".pdf", ".png" });
                ExportedBoardSize = exportedBoardSize;
                ExportedBoardCount = exportedBoardCount;

                // Cache Files
                double cacheSize = 0;
                int cacheCount = 0;
                AnalyzeDirectory(Path.Combine(storagePath, "Cache"), ref cacheSize, ref cacheCount, new[] { ".cache", ".tmp", ".dat" });
                CacheSize = cacheSize;
                CacheCount = cacheCount;

                // Auto Saved Screenshots
                double autoSavedScreenshotSize = 0;
                int autoSavedScreenshotCount = 0;
                AnalyzeDirectory(Path.Combine(storagePath, "AutoSavedScreenshots"), ref autoSavedScreenshotSize, ref autoSavedScreenshotCount, new[] { ".png" });
                AutoSavedScreenshotSize = autoSavedScreenshotSize;
                AutoSavedScreenshotCount = autoSavedScreenshotCount;

                // Calculate total ICC data space
                IccDataSpace = AutoSavedInkSize + BoardImageRefSize + ExportedBoardSize + CacheSize + AutoSavedScreenshotSize;

                // Notify property changes
                OnPropertyChanged(nameof(AutoSavedInkSize));
                OnPropertyChanged(nameof(BoardImageRefSize));
                OnPropertyChanged(nameof(ExportedBoardSize));
                OnPropertyChanged(nameof(CacheSize));
                OnPropertyChanged(nameof(AutoSavedScreenshotSize));
                OnPropertyChanged(nameof(AutoSavedInkCount));
                OnPropertyChanged(nameof(BoardImageRefCount));
                OnPropertyChanged(nameof(ExportedBoardCount));
                OnPropertyChanged(nameof(CacheCount));
                OnPropertyChanged(nameof(AutoSavedScreenshotCount));
                OnPropertyChanged(nameof(IccDataSpace));
                OnPropertyChanged(nameof(AutoSavedInkPercentage));
                OnPropertyChanged(nameof(BoardImageRefPercentage));
                OnPropertyChanged(nameof(ExportedBoardPercentage));
                OnPropertyChanged(nameof(CachePercentage));
                OnPropertyChanged(nameof(AutoSavedScreenshotPercentage));
                OnPropertyChanged(nameof(TotalFileCount));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error analyzing ICC data directories: {ex.Message}");
            }
        }

        private void AnalyzeDirectory(string directoryPath, ref double totalSize, ref int fileCount, string[] extensions)
        {
            try
            {
                if (Directory.Exists(directoryPath))
                {
                    var files = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories)
                        .Where(file => extensions.Contains(Path.GetExtension(file).ToLower()));

                    foreach (var file in files)
                    {
                        try
                        {
                            var fileInfo = new FileInfo(file);
                            totalSize += fileInfo.Length;
                            fileCount++;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error analyzing file {file}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error analyzing directory {directoryPath}: {ex.Message}");
            }
        }

        public void RefreshStorageAnalysis()
        {
            _ = Task.Run(RefreshStorageAnalysisAsync);
        }
    }
}
