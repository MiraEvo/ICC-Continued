using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ink_Canvas.Core;
using Ink_Canvas.Models.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ink_Canvas.ViewModels
{
    /// <summary>
    /// RandWindow ViewModel - 管理随机点名窗口状态和逻辑
    /// </summary>
    public partial class RandWindowViewModel : ViewModelBase
    {
        private readonly RandSettings _randSettings;
        private CancellationTokenSource _cts;
        private static readonly Random _random = new();

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="randSettings">随机点名设置</param>
        public RandWindowViewModel(RandSettings randSettings)
        {
            _randSettings = randSettings ?? throw new ArgumentNullException(nameof(randSettings));
            InitializeSettings();
            LoadNames();
        }

        #endregion

        #region 属性和字段

        /// <summary>
        /// 是否正在滚动
        /// </summary>
        [ObservableProperty]
        private bool _isRolling;

        /// <summary>
        /// 总人数
        /// </summary>
        [ObservableProperty]
        private int _peopleCount = 60;

        /// <summary>
        /// 当前选择的人数
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanIncrementCount))]
        [NotifyPropertyChangedFor(nameof(CanDecrementCount))]
        private int _totalCount = 1;

        /// <summary>
        /// 是否可以增加人数
        /// </summary>
        public bool CanIncrementCount => !IsRolling && (RandMaxPeopleOneTime == -1 || TotalCount < RandMaxPeopleOneTime);

        /// <summary>
        /// 是否可以减少人数
        /// </summary>
        public bool CanDecrementCount => !IsRolling && TotalCount > 1;

        /// <summary>
        /// 主输出文本
        /// </summary>
        [ObservableProperty]
        private string _outputText = string.Empty;

        /// <summary>
        /// 第二输出文本
        /// </summary>
        [ObservableProperty]
        private string _outputText2 = string.Empty;

        /// <summary>
        /// 第三输出文本
        /// </summary>
        [ObservableProperty]
        private string _outputText3 = string.Empty;

        /// <summary>
        /// 第二输出是否可见
        /// </summary>
        [ObservableProperty]
        private bool _isOutput2Visible;

        /// <summary>
        /// 第三输出是否可见
        /// </summary>
        [ObservableProperty]
        private bool _isOutput3Visible;

        /// <summary>
        /// 人员名单
        /// </summary>
        public List<string> Names { get; } = new();

        /// <summary>
        /// 是否显示帮助按钮
        /// </summary>
        [ObservableProperty]
        private bool _isHelpButtonVisible;

        /// <summary>
        /// 人员数量显示文本
        /// </summary>
        [ObservableProperty]
        private string _peopleCountText = "点击此处以导入名单";

        /// <summary>
        /// 开始按钮图标
        /// </summary>
        [ObservableProperty]
        private string _startButtonIcon = "\uE77B";

        /// <summary>
        /// 是否自动关闭模式
        /// </summary>
        [ObservableProperty]
        private bool _isAutoClose;

        /// <summary>
        /// 控制面板是否可用
        /// </summary>
        [ObservableProperty]
        private bool _isControlPanelEnabled = true;

        /// <summary>
        /// 控制面板透明度
        /// </summary>
        [ObservableProperty]
        private double _controlPanelOpacity = 1.0;

        /// <summary>
        /// 配置参数：等待次数
        /// </summary>
        public int RandWaitingTimes { get; set; } = 100;

        /// <summary>
        /// 配置参数：等待线程睡眠时间
        /// </summary>
        public int RandWaitingThreadSleepTime { get; set; } = 5;

        /// <summary>
        /// 配置参数：单次最大人数
        /// </summary>
        public int RandMaxPeopleOneTime { get; set; } = 10;

        /// <summary>
        /// 配置参数：完成后自动关闭等待时间
        /// </summary>
        public int RandDoneAutoCloseWaitTime { get; set; } = 2500;

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化设置
        /// </summary>
        private void InitializeSettings()
        {
            IsHelpButtonVisible = _randSettings.DisplayRandWindowNamesInputBtn;
            RandMaxPeopleOneTime = _randSettings.RandWindowOnceMaxStudents;
            RandDoneAutoCloseWaitTime = (int)(_randSettings.RandWindowOnceCloseLatency * 1000);
        }

        /// <summary>
        /// 加载名字和替换规则
        /// </summary>
        public void LoadNames()
        {
            Names.Clear();
            string namesPath = Path.Combine(App.RootPath, "Names.txt");

            if (File.Exists(namesPath))
            {
                // 加载替换规则
                var replacements = new Dictionary<string, string>();
                string replacePath = Path.Combine(App.RootPath, "Replace.txt");
                if (File.Exists(replacePath))
                {
                    var replaceLines = File.ReadAllLines(replacePath);
                    foreach (var line in replaceLines)
                    {
                        var parts = line.Split(["-->"], StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2)
                        {
                            replacements[parts[0].Trim()] = parts[1].Trim();
                        }
                    }
                }

                // 读取并处理名字
                var fileNames = File.ReadAllLines(namesPath);
                foreach (var name in fileNames)
                {
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    string finalName = name;
                    // 应用替换规则
                    if (replacements.TryGetValue(finalName, out string replacement))
                    {
                        finalName = replacement;
                    }

                    Names.Add(finalName);
                }

                PeopleCount = Names.Count;
                PeopleCountText = PeopleCount.ToString();
            }
            else
            {
                PeopleCount = 0;
            }

            // 如果没有名单，默认使用数字 1-60
            if (PeopleCount == 0)
            {
                PeopleCount = 60;
                PeopleCountText = "点击此处以导入名单";
            }
        }

        /// <summary>
        /// 设置自动关闭模式
        /// </summary>
        public void SetAutoCloseMode(bool isAutoClose)
        {
            IsAutoClose = isAutoClose;
            if (isAutoClose)
            {
                ControlPanelOpacity = 0.4;
                IsControlPanelEnabled = false;
            }
        }

        #endregion

        #region 命令

        /// <summary>
        /// 增加人数命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanIncrementCount))]
        private void IncrementCount()
        {
            TotalCount++;
            UpdateStartButtonIcon();
        }

        /// <summary>
        /// 减少人数命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanDecrementCount))]
        private void DecrementCount()
        {
            TotalCount--;
            UpdateStartButtonIcon();
        }

        /// <summary>
        /// 开始随机选择命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanStartRandomSelection))]
        private async Task StartRandomSelectionAsync()
        {
            if (IsRolling) return;
            IsRolling = true;

            // 取消之前的任务（如果有）
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            try
            {
                // 重置结果显示
                IsOutput2Visible = false;
                IsOutput3Visible = false;

                // 1. 滚动动画阶段
                for (int i = 0; i < RandWaitingTimes; i++)
                {
                    if (_cts.Token.IsCancellationRequested) break;

                    int randIndex = _random.Next(0, PeopleCount);
                    string displayName = (Names.Count > 0) ? Names[randIndex] : (randIndex + 1).ToString();

                    OutputText = displayName;

                    await Task.Delay(RandWaitingThreadSleepTime, _cts.Token);
                }

                // 2. 生成最终结果
                List<string> finalSelection = GenerateRandomSelection(TotalCount);

                // 3. 显示结果
                DisplayResults(finalSelection);

                // 4. 自动关闭逻辑
                if (IsAutoClose)
                {
                    await Task.Delay(RandDoneAutoCloseWaitTime, _cts.Token);
                    ControlPanelOpacity = 1;
                    IsControlPanelEnabled = true;
                    RequestClose?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (TaskCanceledException)
            {
                // 任务被取消，忽略
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex.Message);
            }
            finally
            {
                IsRolling = false;
                _cts?.Dispose();
                _cts = null;
            }
        }

        /// <summary>
        /// 是否可以开始随机选择
        /// </summary>
        public bool CanStartRandomSelection => !IsRolling;

        /// <summary>
        /// 打开帮助命令
        /// </summary>
        [RelayCommand]
        private void OpenHelp()
        {
            OpenNamesInputRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 关闭窗口命令
        /// </summary>
        [RelayCommand]
        private void Close()
        {
            _cts?.Cancel();
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 更新开始按钮图标
        /// </summary>
        private void UpdateStartButtonIcon()
        {
            StartButtonIcon = TotalCount == 1 ? "Person24" : "People24";
        }

        /// <summary>
        /// 生成不重复的随机选择结果
        /// </summary>
        private List<string> GenerateRandomSelection(int count)
        {
            HashSet<int> selectedIndices = new();
            List<string> result = new();

            for (int i = 0; i < count; i++)
            {
                // 如果已选满所有人，清空记录以允许重复（防止死循环）
                if (selectedIndices.Count >= PeopleCount)
                {
                    selectedIndices.Clear();
                }

                int randIndex;
                do
                {
                    randIndex = _random.Next(0, PeopleCount);
                } while (selectedIndices.Contains(randIndex));

                selectedIndices.Add(randIndex);

                if (Names.Count > 0)
                {
                    result.Add(Names[randIndex]);
                }
                else
                {
                    result.Add((randIndex + 1).ToString());
                }
            }
            return result;
        }

        /// <summary>
        /// 将结果分布显示到多个 Label 上
        /// </summary>
        private void DisplayResults(List<string> outputs)
        {
            string JoinRange(int start, int count) =>
                string.Join(Environment.NewLine, outputs.Skip(start).Take(count));

            int count = outputs.Count;

            if (count <= 5)
            {
                OutputText = JoinRange(0, count);
            }
            else if (count <= 10)
            {
                IsOutput2Visible = true;
                int mid = (count + 1) / 2;
                OutputText = JoinRange(0, mid);
                OutputText2 = JoinRange(mid, count - mid);
            }
            else
            {
                IsOutput2Visible = true;
                IsOutput3Visible = true;

                int third = (count + 1) / 3;
                int twoThirds = (count + 1) * 2 / 3;

                OutputText = JoinRange(0, third);
                OutputText2 = JoinRange(third, twoThirds - third);
                OutputText3 = JoinRange(twoThirds, count - twoThirds);
            }
        }

        #endregion

        #region 事件

        /// <summary>
        /// 请求关闭窗口事件
        /// </summary>
        public event EventHandler RequestClose;

        /// <summary>
        /// 请求打开名单输入窗口事件
        /// </summary>
        public event EventHandler OpenNamesInputRequested;

        /// <summary>
        /// 发生错误事件
        /// </summary>
        public event EventHandler<string> ErrorOccurred;

        #endregion

        #region 清理

        /// <summary>
        /// 清理资源
        /// </summary>
        public override void Cleanup()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            base.Cleanup();
        }

        #endregion

        #region 属性变更通知

        /// <summary>
        /// 当 IsRolling 属性改变时
        /// </summary>
        partial void OnIsRollingChanged(bool value)
        {
            IncrementCountCommand.NotifyCanExecuteChanged();
            DecrementCountCommand.NotifyCanExecuteChanged();
            StartRandomSelectionCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// 当 TotalCount 属性改变时
        /// </summary>
        partial void OnTotalCountChanged(int value)
        {
            IncrementCountCommand.NotifyCanExecuteChanged();
            DecrementCountCommand.NotifyCanExecuteChanged();
        }

        #endregion
    }
}
