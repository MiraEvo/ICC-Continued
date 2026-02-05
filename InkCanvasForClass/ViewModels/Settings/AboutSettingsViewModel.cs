using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Diagnostics;
using System.Reflection;

namespace Ink_Canvas.ViewModels.Settings
{
    /// <summary>
    /// About 设置 ViewModel
    /// </summary>
    public partial class AboutSettingsViewModel : ObservableObject
    {
        /// <summary>
        /// 应用程序名称
        /// </summary>
        public string ApplicationName => "InkCanvasForClass-Continued";

        /// <summary>
        /// 版本号
        /// </summary>
        public string Version => GetAssemblyVersion();

        /// <summary>
        /// 捆绑版本号
        /// </summary>
        public string BundleVersion => GetFileVersion();

        /// <summary>
        /// 代码名称
        /// </summary>
        public string CodeName => "Arona";

        /// <summary>
        /// 许可证名称
        /// </summary>
        public string LicenseName => "GNU General Public License v3.0";

        /// <summary>
        /// 许可证描述
        /// </summary>
        public string LicenseDescription => "本强许可协议的许可条件是，在相同许可协议下，提供许可作品的完整源代码和修改，包括使用许可作品的大型作品。版权和许可声明必须保留。贡献者明确授予专利权。";

        /// <summary>
        /// 许可证URL
        /// </summary>
        public string LicenseUrl => "https://www.gnu.org/licenses/gpl-3.0.html";

        /// <summary>
        /// GitHub仓库URL
        /// </summary>
        public string GitHubUrl => "https://github.com/InkCanvasForClass-Continued";

        /// <summary>
        /// 检查更新URL
        /// </summary>
        public string CheckUpdateUrl => "https://github.com/InkCanvasForClass-Continued/releases";

        /// <summary>
        /// 原始项目URL
        /// </summary>
        public string OriginalProjectUrl => "https://github.com/InkCanvas/InkCanvasForClass";

        /// <summary>
        /// 获取程序集版本
        /// </summary>
        /// <returns>程序集版本信息</returns>
        public static string GetAssemblyVersion()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                return version?.ToString() ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        /// <summary>
        /// 获取文件版本
        /// </summary>
        /// <returns>文件版本信息</returns>
        public static string GetFileVersion()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var fileVersion = FileVersionInfo.GetVersionInfo(assembly.Location);
                return fileVersion?.FileVersion ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }
    }
}
