using Newtonsoft.Json;

namespace Ink_Canvas.Models.Settings
{
    /// <summary>
    /// 启动设置类
    /// </summary>
    public class StartupSettings : SettingsBase
    {
        private bool _isAutoUpdate = false;
        private bool _isAutoUpdateWithSilence = false;
        private string _autoUpdateWithSilenceStartTime = "00:00";
        private string _autoUpdateWithSilenceEndTime = "00:00";
        private bool _isEnableNibMode = false;
        private bool _isFoldAtStartup = false;
        private bool _enableWindowChromeRendering = false;

        /// <summary>
        /// 是否自动更新
        /// </summary>
        [JsonProperty("isAutoUpdate")]
        public bool IsAutoUpdate
        {
            get => _isAutoUpdate;
            set => SetProperty(ref _isAutoUpdate, value);
        }

        /// <summary>
        /// 是否静默自动更新
        /// </summary>
        [JsonProperty("isAutoUpdateWithSilence")]
        public bool IsAutoUpdateWithSilence
        {
            get => _isAutoUpdateWithSilence;
            set => SetProperty(ref _isAutoUpdateWithSilence, value);
        }

        /// <summary>
        /// 静默更新开始时间
        /// </summary>
        [JsonProperty("autoUpdateWithSilenceStartTime")]
        public string AutoUpdateWithSilenceStartTime
        {
            get => _autoUpdateWithSilenceStartTime;
            set => SetProperty(ref _autoUpdateWithSilenceStartTime, value);
        }

        /// <summary>
        /// 静默更新结束时间
        /// </summary>
        [JsonProperty("autoUpdateWithSilenceEndTime")]
        public string AutoUpdateWithSilenceEndTime
        {
            get => _autoUpdateWithSilenceEndTime;
            set => SetProperty(ref _autoUpdateWithSilenceEndTime, value);
        }

        /// <summary>
        /// 是否启用笔模式
        /// </summary>
        [JsonProperty("isEnableNibMode")]
        public bool IsEnableNibMode
        {
            get => _isEnableNibMode;
            set => SetProperty(ref _isEnableNibMode, value);
        }

        /// <summary>
        /// 启动时是否折叠
        /// </summary>
        [JsonProperty("isFoldAtStartup")]
        public bool IsFoldAtStartup
        {
            get => _isFoldAtStartup;
            set => SetProperty(ref _isFoldAtStartup, value);
        }

        /// <summary>
        /// 是否启用窗口 Chrome 渲染
        /// </summary>
        [JsonProperty("enableWindowChromeRendering")]
        public bool EnableWindowChromeRendering
        {
            get => _enableWindowChromeRendering;
            set => SetProperty(ref _enableWindowChromeRendering, value);
        }
    }
}
