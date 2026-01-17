using Newtonsoft.Json;

namespace Ink_Canvas.Models.Settings
{
    /// <summary>
    /// 随机点名设置类
    /// </summary>
    public class RandSettings : SettingsBase
    {
        private bool _displayRandWindowNamesInputBtn = false;
        private double _randWindowOnceCloseLatency = 2.5;
        private int _randWindowOnceMaxStudents = 10;
        private bool _enableMachineLearning = true;
        private bool _displaySwitchRandomPickListBtn = false;
        private bool _displayPickHistory = true;

        /// <summary>
        /// 为ICC启用机器学习以提升渲染性能
        /// </summary>
        [JsonProperty("enableMachineLearning")]
        public bool EnableMachineLearning
        {
            get => _enableMachineLearning;
            set => SetProperty(ref _enableMachineLearning, value);
        }

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

        /// <summary>
        /// 随机窗口单次关闭延迟
        /// </summary>
        [JsonProperty("randWindowOnceCloseLatency")]
        public double RandWindowOnceCloseLatency
        {
            get => _randWindowOnceCloseLatency;
            set => SetProperty(ref _randWindowOnceCloseLatency, value);
        }

        /// <summary>
        /// 随机窗口单次最大学生数
        /// </summary>
        [JsonProperty("randWindowOnceMaxStudents")]
        public int RandWindowOnceMaxStudents
        {
            get => _randWindowOnceMaxStudents;
            set => SetProperty(ref _randWindowOnceMaxStudents, value);
        }
    }
}
