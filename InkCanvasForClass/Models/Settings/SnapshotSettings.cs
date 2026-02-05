using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Ink_Canvas.Models.Settings
{
    /// <summary>
    /// 截图设置类
    /// </summary>
    public class SnapshotSettings : SettingsBase
    {
        #region 常量

        /// <summary>
        /// 默认截图文件名模式
        /// </summary>
        public const string DefaultScreenshotFileName = "Screenshot-[YYYY]-[MM]-[DD]-[HH]-[mm]-[ss].png";

        /// <summary>
        /// 文件名占位符正则表达式模式
        /// </summary>
        private static readonly Regex PlaceholderRegex = new Regex(@"\[(YYYY|MM|DD|HH|mm|ss|width|height)\]", RegexOptions.Compiled);

        #endregion

        #region 字段

        // 布尔类型设置
        private bool _screenshotUsingMagnificationAPI = false;
        private bool _copyScreenshotToClipboard = true;
        private bool _hideMainWinWhenScreenshot = true;
        private bool _attachInkWhenScreenshot = true;
        private bool _onlySnapshotMaximizeWindow = false;

        // 字符串类型设置
        private string _screenshotFileName = DefaultScreenshotFileName;

        #endregion

        #region 布尔属性

        /// <summary>
        /// 是否使用 Magnification API 进行截图
        /// </summary>
        [JsonProperty("screenshotUsingMagnificationAPI")]
        public bool ScreenshotUsingMagnificationAPI
        {
            get => _screenshotUsingMagnificationAPI;
            set => SetProperty(ref _screenshotUsingMagnificationAPI, value);
        }

        /// <summary>
        /// 截图后是否复制到剪贴板
        /// </summary>
        [JsonProperty("copyScreenshotToClipboard")]
        public bool CopyScreenshotToClipboard
        {
            get => _copyScreenshotToClipboard;
            set => SetProperty(ref _copyScreenshotToClipboard, value);
        }

        /// <summary>
        /// 截图时是否隐藏主窗口
        /// </summary>
        [JsonProperty("hideMainWinWhenScreenshot")]
        public bool HideMainWinWhenScreenshot
        {
            get => _hideMainWinWhenScreenshot;
            set => SetProperty(ref _hideMainWinWhenScreenshot, value);
        }

        /// <summary>
        /// 截图时是否附加墨迹
        /// </summary>
        [JsonProperty("attachInkWhenScreenshot")]
        public bool AttachInkWhenScreenshot
        {
            get => _attachInkWhenScreenshot;
            set => SetProperty(ref _attachInkWhenScreenshot, value);
        }

        /// <summary>
        /// 是否仅截取最大化窗口
        /// </summary>
        [JsonProperty("onlySnapshotMaximizeWindow")]
        public bool OnlySnapshotMaximizeWindow
        {
            get => _onlySnapshotMaximizeWindow;
            set => SetProperty(ref _onlySnapshotMaximizeWindow, value);
        }

        #endregion

        #region 字符串属性

        /// <summary>
        /// 截图文件名模式
        /// 支持的占位符：[YYYY] 年, [MM] 月, [DD] 日, [HH] 时, [mm] 分, [ss] 秒, [width] 图片宽度, [height] 图片高度
        /// </summary>
        [JsonProperty("screenshotFileName")]
        public string ScreenshotFileName
        {
            get => _screenshotFileName;
            set => SetProperty(ref _screenshotFileName, ValidateNotEmpty(value));
        }

        #endregion

        #region 辅助属性

        /// <summary>
        /// 获取格式化后的文件名（使用当前日期时间替换占位符）
        /// </summary>
        [JsonIgnore]
        public string FormattedFileName => GetFormattedFileName(DateTime.Now);

        /// <summary>
        /// 检查文件名模式是否包含有效的占位符
        /// </summary>
        [JsonIgnore]
        public bool HasValidPlaceholders => PlaceholderRegex.IsMatch(ScreenshotFileName);

        /// <summary>
        /// 获取文件名模式中的文件扩展名
        /// </summary>
        [JsonIgnore]
        public string FileExtension
        {
            get
            {
                if (string.IsNullOrEmpty(ScreenshotFileName))
                    return ".png";

                var lastDotIndex = ScreenshotFileName.LastIndexOf('.');
                return lastDotIndex > 0 ? ScreenshotFileName.Substring(lastDotIndex) : ".png";
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 获取格式化后的文件名，将占位符替换为指定日期时间的值
        /// </summary>
        /// <param name="dateTime">用于替换占位符的日期时间</param>
        /// <returns>格式化后的文件名</returns>
        public string GetFormattedFileName(DateTime dateTime)
        {
            return GetFormattedFileName(dateTime, 0, 0);
        }

        /// <summary>
        /// 获取格式化后的文件名，将占位符替换为指定日期时间和图片尺寸的值
        /// </summary>
        /// <param name="dateTime">用于替换占位符的日期时间</param>
        /// <param name="width">图片宽度</param>
        /// <param name="height">图片高度</param>
        /// <returns>格式化后的文件名</returns>
        public string GetFormattedFileName(DateTime dateTime, int width, int height)
        {
            if (string.IsNullOrEmpty(ScreenshotFileName))
                return $"Screenshot-{dateTime:yyyy-MM-dd-HH-mm-ss}.png";

            return ScreenshotFileName
                .Replace("[YYYY]", dateTime.Year.ToString())
                .Replace("[MM]", dateTime.Month.ToString("D2"))
                .Replace("[DD]", dateTime.Day.ToString("D2"))
                .Replace("[HH]", dateTime.Hour.ToString("D2"))
                .Replace("[mm]", dateTime.Minute.ToString("D2"))
                .Replace("[ss]", dateTime.Second.ToString("D2"))
                .Replace("[width]", width.ToString())
                .Replace("[height]", height.ToString());
        }

        /// <summary>
        /// 验证文件名模式是否有效（包含至少一个占位符且有有效的文件扩展名）
        /// </summary>
        /// <param name="pattern">要验证的文件名模式</param>
        /// <returns>如果有效返回 true，否则返回 false</returns>
        public static bool IsValidFileNamePattern(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                return false;

            // 检查是否包含至少一个占位符
            if (!PlaceholderRegex.IsMatch(pattern))
                return false;

            // 检查是否有有效的文件扩展名
            var lastDotIndex = pattern.LastIndexOf('.');
            if (lastDotIndex <= 0 || lastDotIndex == pattern.Length - 1)
                return false;

            var extension = pattern.Substring(lastDotIndex + 1).ToLowerInvariant();
            var validExtensions = new[] { "png", "jpg", "jpeg", "bmp", "gif", "tiff", "webp" };

            return Array.Exists(validExtensions, ext => ext == extension);
        }

        /// <summary>
        /// 重置为默认文件名模式
        /// </summary>
        public void ResetToDefaultFileName()
        {
            ScreenshotFileName = DefaultScreenshotFileName;
        }

        #endregion
    }
}
