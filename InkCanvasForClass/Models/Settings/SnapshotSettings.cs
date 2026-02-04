using Newtonsoft.Json;

namespace Ink_Canvas.Models.Settings
{
    /// <summary>
    /// 截图设置类
    /// </summary>
    public class SnapshotSettings : SettingsBase
    {
        private bool _screenshotUsingMagnificationAPI = false;
        private bool _copyScreenshotToClipboard = true;
        private bool _hideMainWinWhenScreenshot = true;
        private bool _attachInkWhenScreenshot = true;
        private bool _onlySnapshotMaximizeWindow = false;
        private string _screenshotFileName = "Screenshot-[YYYY]-[MM]-[DD]-[HH]-[mm]-[ss].png";

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

        /// <summary>
        /// 截图文件名模式
        /// </summary>
        [JsonProperty("screenshotFileName")]
        public string ScreenshotFileName
        {
            get => _screenshotFileName;
            set => SetProperty(ref _screenshotFileName, value);
        }
    }
}
