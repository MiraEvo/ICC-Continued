using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Ink_Canvas.Models.Settings
{
    /// <summary>
    /// 启动设置类
    /// </summary>
    public class StartupSettings : SettingsBase
    {
        #region 常量

        /// <summary>
        /// 默认静默更新开始时间
        /// </summary>
        public const string DefaultSilenceStartTime = "00:00";

        /// <summary>
        /// 默认静默更新结束时间
        /// </summary>
        public const string DefaultSilenceEndTime = "00:00";

        /// <summary>
        /// 时间格式正则表达式（HH:mm）
        /// </summary>
        private static readonly Regex TimeRegex = new Regex(@"^([0-1]?[0-9]|2[0-3]):([0-5][0-9])$", RegexOptions.Compiled);

        #endregion

        #region 字段

        // 自动更新设置
        private bool _isAutoUpdate = false;
        private bool _isAutoUpdateWithSilence = false;
        private string _autoUpdateWithSilenceStartTime = DefaultSilenceStartTime;
        private string _autoUpdateWithSilenceEndTime = DefaultSilenceEndTime;

        // 启动行为设置
        private bool _isEnableNibMode = false;
        private bool _isFoldAtStartup = false;
        private bool _enableWindowChromeRendering = false;

        #endregion

        #region 辅助属性

        /// <summary>
        /// 是否启用了自动更新功能
        /// </summary>
        [JsonIgnore]
        public bool IsAutoUpdateEnabled => IsAutoUpdate || IsAutoUpdateWithSilence;

        /// <summary>
        /// 当前是否在静默更新时间段内
        /// </summary>
        [JsonIgnore]
        public bool IsInSilenceUpdatePeriod
        {
            get
            {
                if (!IsAutoUpdateWithSilence)
                    return false;

                var now = DateTime.Now;
                var currentTime = now.TimeOfDay;

                if (TimeSpan.TryParse(AutoUpdateWithSilenceStartTime, out var startTime) &&
                    TimeSpan.TryParse(AutoUpdateWithSilenceEndTime, out var endTime))
                {
                    // 处理跨天的情况（如 22:00 到 06:00）
                    if (startTime > endTime)
                    {
                        return currentTime >= startTime || currentTime <= endTime;
                    }
                    return currentTime >= startTime && currentTime <= endTime;
                }

                return false;
            }
        }

        /// <summary>
        /// 静默更新开始时间的小时部分
        /// </summary>
        [JsonIgnore]
        public int SilenceStartHour
        {
            get
            {
                if (TimeSpan.TryParse(AutoUpdateWithSilenceStartTime, out var time))
                    return time.Hours;
                return 0;
            }
        }

        /// <summary>
        /// 静默更新开始时间的分钟部分
        /// </summary>
        [JsonIgnore]
        public int SilenceStartMinute
        {
            get
            {
                if (TimeSpan.TryParse(AutoUpdateWithSilenceStartTime, out var time))
                    return time.Minutes;
                return 0;
            }
        }

        /// <summary>
        /// 静默更新结束时间的小时部分
        /// </summary>
        [JsonIgnore]
        public int SilenceEndHour
        {
            get
            {
                if (TimeSpan.TryParse(AutoUpdateWithSilenceEndTime, out var time))
                    return time.Hours;
                return 0;
            }
        }

        /// <summary>
        /// 静默更新结束时间的分钟部分
        /// </summary>
        [JsonIgnore]
        public int SilenceEndMinute
        {
            get
            {
                if (TimeSpan.TryParse(AutoUpdateWithSilenceEndTime, out var time))
                    return time.Minutes;
                return 0;
            }
        }

        #endregion

        #region 自动更新属性

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
        /// 静默更新开始时间（格式：HH:mm）
        /// </summary>
        [JsonProperty("autoUpdateWithSilenceStartTime")]
        public string AutoUpdateWithSilenceStartTime
        {
            get => _autoUpdateWithSilenceStartTime;
            set => SetProperty(ref _autoUpdateWithSilenceStartTime, ValidateTimeFormat(value, DefaultSilenceStartTime));
        }

        /// <summary>
        /// 静默更新结束时间（格式：HH:mm）
        /// </summary>
        [JsonProperty("autoUpdateWithSilenceEndTime")]
        public string AutoUpdateWithSilenceEndTime
        {
            get => _autoUpdateWithSilenceEndTime;
            set => SetProperty(ref _autoUpdateWithSilenceEndTime, ValidateTimeFormat(value, DefaultSilenceEndTime));
        }

        #endregion

        #region 启动行为属性

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

        #endregion

        #region 辅助方法

        /// <summary>
        /// 验证时间格式是否有效
        /// </summary>
        /// <param name="time">时间字符串</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>验证后的时间字符串</returns>
        private string ValidateTimeFormat(string time, string defaultValue)
        {
            if (string.IsNullOrWhiteSpace(time))
                return defaultValue;

            // 尝试标准化时间格式（添加前导零）
            var normalizedTime = NormalizeTimeFormat(time);
            
            if (TimeRegex.IsMatch(normalizedTime))
                return normalizedTime;

            // 尝试解析时间
            if (TimeSpan.TryParse(time, CultureInfo.InvariantCulture, out _))
                return normalizedTime;

            return defaultValue;
        }

        /// <summary>
        /// 标准化时间格式为 HH:mm
        /// </summary>
        /// <param name="time">时间字符串</param>
        /// <returns>标准化后的时间字符串</returns>
        private string NormalizeTimeFormat(string time)
        {
            if (string.IsNullOrWhiteSpace(time))
                return time;

            // 移除多余空格
            time = time.Trim();

            // 处理 H:mm 或 HH:m 格式，添加前导零
            var parts = time.Split(':');
            if (parts.Length == 2)
            {
                if (int.TryParse(parts[0], out var hour) && int.TryParse(parts[1], out var minute))
                {
                    return $"{hour:D2}:{minute:D2}";
                }
            }

            return time;
        }

        /// <summary>
        /// 设置静默更新时间段
        /// </summary>
        /// <param name="startHour">开始小时（0-23）</param>
        /// <param name="startMinute">开始分钟（0-59）</param>
        /// <param name="endHour">结束小时（0-23）</param>
        /// <param name="endMinute">结束分钟（0-59）</param>
        public void SetSilencePeriod(int startHour, int startMinute, int endHour, int endMinute)
        {
            startHour = Math.Clamp(startHour, 0, 23);
            startMinute = Math.Clamp(startMinute, 0, 59);
            endHour = Math.Clamp(endHour, 0, 23);
            endMinute = Math.Clamp(endMinute, 0, 59);

            AutoUpdateWithSilenceStartTime = $"{startHour:D2}:{startMinute:D2}";
            AutoUpdateWithSilenceEndTime = $"{endHour:D2}:{endMinute:D2}";
        }

        /// <summary>
        /// 重置静默更新时间为默认值
        /// </summary>
        public void ResetSilenceTimes()
        {
            AutoUpdateWithSilenceStartTime = DefaultSilenceStartTime;
            AutoUpdateWithSilenceEndTime = DefaultSilenceEndTime;
        }

        /// <summary>
        /// 检查时间字符串是否为有效的时间格式
        /// </summary>
        /// <param name="time">时间字符串</param>
        /// <returns>如果有效返回 true</returns>
        public static bool IsValidTimeFormat(string time)
        {
            if (string.IsNullOrWhiteSpace(time))
                return false;

            return TimeRegex.IsMatch(time.Trim()) || TimeSpan.TryParse(time, out _);
        }

        #endregion
    }
}
