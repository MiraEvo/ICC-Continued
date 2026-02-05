using Newtonsoft.Json;

namespace Ink_Canvas.Models.Settings
{
    /// <summary>
    /// 随机点名设置类
    /// </summary>
    public class RandSettings : SettingsBase
    {
        #region 常量

        /// <summary>
        /// 最小关闭延迟（秒）
        /// </summary>
        public const double MinCloseLatency = 0.5;

        /// <summary>
        /// 最大关闭延迟（秒）
        /// </summary>
        public const double MaxCloseLatency = 10.0;

        /// <summary>
        /// 默认关闭延迟（秒）
        /// </summary>
        public const double DefaultCloseLatency = 2.5;

        /// <summary>
        /// 最小最大学生数
        /// </summary>
        public const int MinMaxStudents = 1;

        /// <summary>
        /// 最大最大学生数
        /// </summary>
        public const int MaxMaxStudents = 50;

        /// <summary>
        /// 默认最大学生数
        /// </summary>
        public const int DefaultMaxStudents = 10;

        #endregion

        #region 字段

        // 按钮显示设置
        private bool _displayRandWindowNamesInputBtn = false;
        private bool _displaySwitchRandomPickListBtn = false;
        private bool _displayPickHistory = true;

        // 窗口行为设置
        private double _randWindowOnceCloseLatency = DefaultCloseLatency;
        private int _randWindowOnceMaxStudents = DefaultMaxStudents;

        #endregion

        #region 辅助属性

        /// <summary>
        /// 是否显示任何点名相关按钮
        /// </summary>
        [JsonIgnore]
        public bool IsAnyRandButtonVisible => DisplayRandWindowNamesInputBtn || DisplaySwitchRandomPickListBtn;

        /// <summary>
        /// 关闭延迟时间（毫秒）
        /// </summary>
        [JsonIgnore]
        public int CloseLatencyMs => (int)(RandWindowOnceCloseLatency * 1000);

        /// <summary>
        /// 是否启用自动关闭
        /// </summary>
        [JsonIgnore]
        public bool IsAutoCloseEnabled => RandWindowOnceCloseLatency > 0;

        #endregion

        #region 按钮显示属性

        /// <summary>
        /// 显示切换随机点名名单的按钮
        /// </summary>
        [JsonProperty("displaySwitchRandomPickListBtn")]
        public bool DisplaySwitchRandomPickListBtn
        {
            get => _displaySwitchRandomPickListBtn;
            set => SetProperty(ref _displaySwitchRandomPickListBtn, value);
        }

        /// <summary>
        /// 显示点名历史记录
        /// </summary>
        [JsonProperty("displayPickHistory")]
        public bool DisplayPickHistory
        {
            get => _displayPickHistory;
            set => SetProperty(ref _displayPickHistory, value);
        }

        /// <summary>
        /// 是否显示随机窗口名称输入按钮
        /// </summary>
        [JsonProperty("displayRandWindowNamesInputBtn")]
        public bool DisplayRandWindowNamesInputBtn
        {
            get => _displayRandWindowNamesInputBtn;
            set => SetProperty(ref _displayRandWindowNamesInputBtn, value);
        }

        #endregion

        #region 窗口行为属性

        /// <summary>
        /// 随机窗口单次关闭延迟（秒，0.5-10.0）
        /// </summary>
        [JsonProperty("randWindowOnceCloseLatency")]
        public double RandWindowOnceCloseLatency
        {
            get => _randWindowOnceCloseLatency;
            set => SetProperty(ref _randWindowOnceCloseLatency, ClampRange(value, MinCloseLatency, MaxCloseLatency));
        }

        /// <summary>
        /// 随机窗口单次最大学生数（1-50）
        /// </summary>
        [JsonProperty("randWindowOnceMaxStudents")]
        public int RandWindowOnceMaxStudents
        {
            get => _randWindowOnceMaxStudents;
            set => SetProperty(ref _randWindowOnceMaxStudents, ClampRange(value, MinMaxStudents, MaxMaxStudents));
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 重置关闭延迟为默认值
        /// </summary>
        public void ResetCloseLatency()
        {
            RandWindowOnceCloseLatency = DefaultCloseLatency;
        }

        /// <summary>
        /// 重置最大学生数为默认值
        /// </summary>
        public void ResetMaxStudents()
        {
            RandWindowOnceMaxStudents = DefaultMaxStudents;
        }

        /// <summary>
        /// 重置所有设置为默认值
        /// </summary>
        public void ResetToDefaults()
        {
            DisplayRandWindowNamesInputBtn = false;
            DisplaySwitchRandomPickListBtn = false;
            DisplayPickHistory = true;
            RandWindowOnceCloseLatency = DefaultCloseLatency;
            RandWindowOnceMaxStudents = DefaultMaxStudents;
        }

        #endregion
    }
}
