using System.Collections.Generic;

namespace Ink_Canvas.Services
{
    /// <summary>
    /// 重复资源组 - 包含具有相同内容的文件
    /// </summary>
    public class DuplicateResourceGroup
    {
        /// <summary>
        /// 文件内容的哈希值
        /// </summary>
        public string Hash { get; set; }

        /// <summary>
        /// 具有相同哈希值的文件路径列表
        /// </summary>
        public List<string> FilePaths { get; set; }

        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        public long FileSize { get; set; }

        public DuplicateResourceGroup()
        {
            FilePaths = new List<string>();
        }
    }

    /// <summary>
    /// 文件清理服务接口 - 管理临时文件和冗余资源
    /// </summary>
    public interface IFileCleanupService
    {
        /// <summary>
        /// 扫描指定目录下的临时文件
        /// </summary>
        /// <param name="directory">要扫描的目录路径</param>
        /// <param name="pattern">文件匹配模式（如 "*_wpftmp.csproj"）</param>
        /// <returns>找到的临时文件路径列表</returns>
        IEnumerable<string> ScanTemporaryFiles(string directory, string pattern);

        /// <summary>
        /// 删除临时文件
        /// </summary>
        /// <param name="files">要删除的文件路径列表</param>
        /// <returns>成功删除的文件数量</returns>
        int DeleteTemporaryFiles(IEnumerable<string> files);

        /// <summary>
        /// 扫描重复的资源文件
        /// </summary>
        /// <param name="resourceDirectory">资源目录路径</param>
        /// <returns>重复资源组列表</returns>
        IEnumerable<DuplicateResourceGroup> ScanDuplicateResources(string resourceDirectory);

        /// <summary>
        /// 更新 .gitignore 文件（异步）
        /// </summary>
        /// <param name="gitignorePath">.gitignore 文件路径</param>
        /// <param name="patterns">要添加的忽略模式列表</param>
        System.Threading.Tasks.Task UpdateGitignoreAsync(string gitignorePath, IEnumerable<string> patterns);

    }
}
