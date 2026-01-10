using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Ink_Canvas.Helpers;

namespace Ink_Canvas.Services
{
    /// <summary>
    /// 文件清理服务实现 - 管理临时文件和冗余资源
    /// </summary>
    public class FileCleanupService : IFileCleanupService
    {
        /// <summary>
        /// 扫描指定目录下的临时文件
        /// </summary>
        /// <param name="directory">要扫描的目录路径</param>
        /// <param name="pattern">文件匹配模式（如 "*_wpftmp.csproj"）</param>
        /// <returns>找到的临时文件路径列表</returns>
        public IEnumerable<string> ScanTemporaryFiles(string directory, string pattern)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                LogHelper.WriteLogToFile("ScanTemporaryFiles: Directory path is null or empty", LogHelper.LogType.Error);
                return Enumerable.Empty<string>();
            }

            if (!Directory.Exists(directory))
            {
                LogHelper.WriteLogToFile($"ScanTemporaryFiles: Directory does not exist: {directory}", LogHelper.LogType.Warning);
                return Enumerable.Empty<string>();
            }

            try
            {
                var files = Directory.GetFiles(directory, pattern, SearchOption.AllDirectories);
                LogHelper.WriteLogToFile($"ScanTemporaryFiles: Found {files.Length} files matching pattern '{pattern}' in {directory}", LogHelper.LogType.Info);
                return files;
            }
            catch (UnauthorizedAccessException ex)
            {
                LogHelper.WriteLogToFile($"ScanTemporaryFiles: Access denied to directory {directory}: {ex.Message}", LogHelper.LogType.Error);
                return Enumerable.Empty<string>();
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"ScanTemporaryFiles: Error scanning directory {directory}: {ex.Message}\n{ex.StackTrace}", LogHelper.LogType.Error);
                return Enumerable.Empty<string>();
            }
        }

        /// <summary>
        /// 删除临时文件
        /// </summary>
        /// <param name="files">要删除的文件路径列表</param>
        /// <returns>成功删除的文件数量</returns>
        public int DeleteTemporaryFiles(IEnumerable<string> files)
        {
            if (files == null)
            {
                LogHelper.WriteLogToFile("DeleteTemporaryFiles: Files list is null", LogHelper.LogType.Error);
                return 0;
            }

            int deletedCount = 0;
            var fileList = files.ToList();

            foreach (var file in fileList)
            {
                try
                {
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                        deletedCount++;
                        LogHelper.WriteLogToFile($"DeleteTemporaryFiles: Deleted file: {file}", LogHelper.LogType.Info);
                    }
                    else
                    {
                        LogHelper.WriteLogToFile($"DeleteTemporaryFiles: File does not exist (already deleted?): {file}", LogHelper.LogType.Info);
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    LogHelper.WriteLogToFile($"DeleteTemporaryFiles: Access denied to file {file}: {ex.Message}", LogHelper.LogType.Error);
                }
                catch (IOException ex)
                {
                    LogHelper.WriteLogToFile($"DeleteTemporaryFiles: IO error deleting file {file}: {ex.Message}", LogHelper.LogType.Error);
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"DeleteTemporaryFiles: Error deleting file {file}: {ex.Message}\n{ex.StackTrace}", LogHelper.LogType.Error);
                }
            }

            LogHelper.WriteLogToFile($"DeleteTemporaryFiles: Successfully deleted {deletedCount} out of {fileList.Count} files", LogHelper.LogType.Info);
            return deletedCount;
        }

        /// <summary>
        /// 扫描重复的资源文件
        /// </summary>
        /// <param name="resourceDirectory">资源目录路径</param>
        /// <returns>重复资源组列表</returns>
        public IEnumerable<DuplicateResourceGroup> ScanDuplicateResources(string resourceDirectory)
        {
            if (string.IsNullOrWhiteSpace(resourceDirectory))
            {
                LogHelper.WriteLogToFile("ScanDuplicateResources: Resource directory path is null or empty", LogHelper.LogType.Error);
                return Enumerable.Empty<DuplicateResourceGroup>();
            }

            if (!Directory.Exists(resourceDirectory))
            {
                LogHelper.WriteLogToFile($"ScanDuplicateResources: Resource directory does not exist: {resourceDirectory}", LogHelper.LogType.Warning);
                return Enumerable.Empty<DuplicateResourceGroup>();
            }

            try
            {
                // 使用字典存储文件哈希值和对应的文件路径列表
                // Key: 文件内容的 SHA256 哈希值
                // Value: 具有相同哈希值的文件路径列表
                var fileHashes = new Dictionary<string, List<string>>();
                var fileSizes = new Dictionary<string, long>();

                // 递归获取目录下的所有文件
                var files = Directory.GetFiles(resourceDirectory, "*.*", SearchOption.AllDirectories);
                LogHelper.WriteLogToFile($"ScanDuplicateResources: Scanning {files.Length} files in {resourceDirectory}", LogHelper.LogType.Info);

                // 第一遍扫描：计算每个文件的哈希值
                // 哈希值相同的文件内容必然相同，可以识别重复文件
                foreach (var file in files)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        var hash = ComputeFileHash(file);

                        // 将文件路径添加到对应哈希值的列表中
                        if (!fileHashes.ContainsKey(hash))
                        {
                            fileHashes[hash] = new List<string>();
                            fileSizes[hash] = fileInfo.Length;
                        }

                        fileHashes[hash].Add(file);
                    }
                    catch (Exception ex)
                    {
                        LogHelper.WriteLogToFile($"ScanDuplicateResources: Error processing file {file}: {ex.Message}", LogHelper.LogType.Warning);
                    }
                }

                // 找出重复的文件（哈希值相同且文件数量 > 1）
                var duplicates = fileHashes
                    .Where(kvp => kvp.Value.Count > 1)
                    .Select(kvp => new DuplicateResourceGroup
                    {
                        Hash = kvp.Key,
                        FilePaths = kvp.Value,
                        FileSize = fileSizes[kvp.Key]
                    })
                    .ToList();

                LogHelper.WriteLogToFile($"ScanDuplicateResources: Found {duplicates.Count} duplicate resource groups", LogHelper.LogType.Info);
                return duplicates;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"ScanDuplicateResources: Error scanning resource directory {resourceDirectory}: {ex.Message}\n{ex.StackTrace}", LogHelper.LogType.Error);
                return Enumerable.Empty<DuplicateResourceGroup>();
            }
        }

        /// <summary>
        /// 更新 .gitignore 文件（异步）
        /// </summary>
        /// <param name="gitignorePath">.gitignore 文件路径</param>
        /// <param name="patterns">要添加的忽略模式列表</param>
        public async System.Threading.Tasks.Task UpdateGitignoreAsync(string gitignorePath, IEnumerable<string> patterns)
        {
            if (string.IsNullOrWhiteSpace(gitignorePath))
            {
                LogHelper.WriteLogToFile("UpdateGitignoreAsync: .gitignore path is null or empty", LogHelper.LogType.Error);
                return;
            }

            if (patterns == null || !patterns.Any())
            {
                LogHelper.WriteLogToFile("UpdateGitignoreAsync: No patterns provided", LogHelper.LogType.Warning);
                return;
            }

            try
            {
                var existingLines = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // 读取现有的 .gitignore 内容
                if (File.Exists(gitignorePath))
                {
                    var lines = await File.ReadAllLinesAsync(gitignorePath);
                    foreach (var line in lines)
                    {
                        var trimmedLine = line.Trim();
                        if (!string.IsNullOrWhiteSpace(trimmedLine))
                        {
                            existingLines.Add(trimmedLine);
                        }
                    }
                }

                // 找出需要添加的新模式
                var newPatterns = patterns
                    .Select(p => p.Trim())
                    .Where(p => !string.IsNullOrWhiteSpace(p) && !existingLines.Contains(p))
                    .ToList();

                if (newPatterns.Count == 0)
                {
                    LogHelper.WriteLogToFile("UpdateGitignoreAsync: All patterns already exist in .gitignore", LogHelper.LogType.Info);
                    return;
                }

                // 追加新模式到 .gitignore
                var sb = new StringBuilder();
                
                // 如果文件存在且不以换行结束，添加换行
                if (File.Exists(gitignorePath))
                {
                    var content = await File.ReadAllTextAsync(gitignorePath);
                    if (!string.IsNullOrEmpty(content) && !content.EndsWith("\n"))
                    {
                        sb.AppendLine();
                    }
                }

                // 添加注释和新模式
                sb.AppendLine();
                sb.AppendLine("# Temporary files added by FileCleanupService");
                foreach (var pattern in newPatterns)
                {
                    sb.AppendLine(pattern);
                }

                await File.AppendAllTextAsync(gitignorePath, sb.ToString());
                LogHelper.WriteLogToFile($"UpdateGitignoreAsync: Added {newPatterns.Count} new patterns to {gitignorePath}", LogHelper.LogType.Info);
            }
            catch (UnauthorizedAccessException ex)
            {
                LogHelper.WriteLogToFile($"UpdateGitignoreAsync: Access denied to .gitignore file {gitignorePath}: {ex.Message}", LogHelper.LogType.Error);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"UpdateGitignoreAsync: Error updating .gitignore file {gitignorePath}: {ex.Message}\n{ex.StackTrace}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// <summary>
        /// 计算文件的 SHA256 哈希值
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>哈希值的十六进制字符串</returns>
        private string ComputeFileHash(string filePath)
        {
            using (var sha256 = SHA256.Create())
            using (var stream = File.OpenRead(filePath))
            {
                var hashBytes = sha256.ComputeHash(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}
