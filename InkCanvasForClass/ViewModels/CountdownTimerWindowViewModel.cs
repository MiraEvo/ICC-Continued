using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ink_Canvas.Core;
using System;
using System.Media;
using System.Timers;
using TimersTimer = System.Timers.Timer;
using System.Windows;
using System.Windows.Media;

namespace Ink_Canvas.ViewModels
{
    /// <summary>
    /// CountdownTimerWindow ViewModel - 管理倒计时计时器状态和逻辑
    /// </summary>
    public partial class CountdownTimerWindowViewModel : ViewModelBase
    {
        private readonly TimersTimer _timer;
        private readonly SoundPlayer _player;
        private DateTime _startTime;
        private DateTime _pauseTime;

        #region 构造函数

        public CountdownTimerWindowViewModel()
        {
            _timer = new TimersTimer();
            _timer.Elapsed += OnTimerElapsed;
            _timer.Interval = 50;

            _player = new SoundPlayer();

            // 初始化显示值
            UpdateDisplayTime();
        }

        #endregion

        #region 属性

        /// <summary>
        /// 小时数（设置值）
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HourText))]
        private int _hour = 0;

        /// <summary>
        /// 分钟数（设置值）
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MinuteText))]
        private int _minute = 1;

        /// <summary>
        /// 秒数（设置值）
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SecondText))]
        private int _second = 0;

        /// <summary>
        /// 小时显示文本
        /// </summary>
        public string HourText => Hour.ToString("00");

        /// <summary>
        /// 分钟显示文本
        /// </summary>
        public string MinuteText => Minute.ToString("00");

        /// <summary>
        /// 秒显示文本
        /// </summary>
        public string SecondText => Second.ToString("00");

        /// <summary>
        /// 当前显示时间（紧凑模式）
        /// </summary>
        [ObservableProperty]
        private string _currentTimeText = "00:01:00";

        /// <summary>
        /// 结束时间文本
        /// </summary>
        [ObservableProperty]
        private string _stopTimeText = "12:30 PM";

        /// <summary>
        /// 进度条当前值（0-1）
        /// </summary>
        [ObservableProperty]
        private double _progressValue = 0;

        /// <summary>
        /// 进度条是否暂停
        /// </summary>
        [ObservableProperty]
        private bool _isProgressPaused = false;

        /// <summary>
        /// 计时器是否正在运行
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotRunning))]
        private bool _isRunning = false;

        /// <summary>
        /// 计时器是否未运行
        /// </summary>
        public bool IsNotRunning => !IsRunning;

        /// <summary>
        /// 是否暂停
        /// </summary>
        [ObservableProperty]
        private bool _isPaused = false;

        /// <summary>
        /// 是否为设置模式（显示调整按钮）
        /// </summary>
        [ObservableProperty]
        private bool _isSettingMode = false;

        /// <summary>
        /// 开始按钮图标
        /// </summary>
        [ObservableProperty]
        private string _startButtonIcon = "\uE768";

        /// <summary>
        /// 全屏按钮图标
        /// </summary>
        [ObservableProperty]
        private string _fullscreenButtonIcon = "\uE740";

        /// <summary>
        /// 时间文本前景色
        /// </summary>
        [ObservableProperty]
        private Brush _timeForeground = new SolidColorBrush(Color.FromArgb(0xFF, 0x5B, 0x5D, 0x5F));

        /// <summary>
        /// 开始按钮是否可用（覆盖层可见性）
        /// </summary>
        [ObservableProperty]
        private bool _isStartButtonEnabled = true;

        /// <summary>
        /// 重置按钮是否可用（覆盖层可见性）
        /// </summary>
        [ObservableProperty]
        private bool _isResetButtonEnabled = true;

        /// <summary>
        /// 结束时间边框是否可见
        /// </summary>
        [ObservableProperty]
        private bool _isStopTimeVisible = false;

        /// <summary>
        /// 总秒数
        /// </summary>
        private int _totalSeconds = 60;

        #endregion

        #region 命令

        /// <summary>
        /// 增加小时
        /// </summary>
        [RelayCommand]
        private void IncrementHour(int amount = 1)
        {
            Hour += amount;
            if (Hour >= 100) Hour = 0;
            UpdateDisplayTime();
        }

        /// <summary>
        /// 减少小时
        /// </summary>
        [RelayCommand]
        private void DecrementHour(int amount = 1)
        {
            Hour -= amount;
            if (Hour < 0) Hour = 99;
            UpdateDisplayTime();
        }

        /// <summary>
        /// 增加分钟
        /// </summary>
        [RelayCommand]
        private void IncrementMinute(int amount = 1)
        {
            Minute += amount;
            if (Minute >= 60) Minute = 0;
            UpdateDisplayTime();
        }

        /// <summary>
        /// 减少分钟
        /// </summary>
        [RelayCommand]
        private void DecrementMinute(int amount = 1)
        {
            Minute -= amount;
            if (Minute < 0) Minute = 59;
            UpdateDisplayTime();
        }

        /// <summary>
        /// 增加秒
        /// </summary>
        [RelayCommand]
        private void IncrementSecond(int amount = 1)
        {
            Second += amount;
            if (Second >= 60) Second = 0;
            UpdateDisplayTime();
        }

        /// <summary>
        /// 减少秒
        /// </summary>
        [RelayCommand]
        private void DecrementSecond(int amount = 1)
        {
            Second -= amount;
            if (Second < 0) Second = 59;
            UpdateDisplayTime();
        }

        /// <summary>
        /// 切换设置模式
        /// </summary>
        [RelayCommand]
        private void ToggleSettingMode()
        {
            if (IsRunning) return;

            IsSettingMode = !IsSettingMode;
            TimeForeground = IsSettingMode ? Brushes.Black : new SolidColorBrush(Color.FromArgb(0xFF, 0x5B, 0x5D, 0x5F));

            if (!IsSettingMode && Hour == 0 && Minute == 0 && Second == 0)
            {
                Second = 1;
                UpdateDisplayTime();
            }
        }

        /// <summary>
        /// 开始/暂停/继续计时
        /// </summary>
        [RelayCommand]
        private void StartPauseResume()
        {
            if (IsPaused && IsRunning)
            {
                // 继续
                _startTime += DateTime.Now - _pauseTime;
                IsProgressPaused = false;
                TimeForeground = Brushes.Black;
                StartButtonIcon = "\uE769";
                IsPaused = false;
                _timer.Start();
                UpdateStopTime();
                IsStopTimeVisible = true;
            }
            else if (IsRunning)
            {
                // 暂停
                _pauseTime = DateTime.Now;
                IsProgressPaused = true;
                TimeForeground = new SolidColorBrush(Color.FromArgb(0xFF, 0x5B, 0x5D, 0x5F));
                StartButtonIcon = "\uE768";
                IsStopTimeVisible = false;
                IsPaused = true;
                _timer.Stop();
            }
            else
            {
                // 从头开始
                _startTime = DateTime.Now;
                _totalSeconds = ((Hour * 60) + Minute) * 60 + Second;
                IsProgressPaused = false;
                TimeForeground = Brushes.Black;
                StartButtonIcon = "\uE769";
                IsResetButtonEnabled = false;

                // 根据总时长调整计时器间隔
                if (_totalSeconds <= 10)
                    _timer.Interval = 20;
                else if (_totalSeconds <= 60)
                    _timer.Interval = 30;
                else if (_totalSeconds <= 120)
                    _timer.Interval = 50;
                else
                    _timer.Interval = 100;

                IsPaused = false;
                IsRunning = true;
                _timer.Start();
                UpdateStopTime();
                IsStopTimeVisible = true;
            }
        }

        /// <summary>
        /// 重置计时器
        /// </summary>
        [RelayCommand]
        private void Reset()
        {
            if (!IsRunning)
            {
                // 未运行时重置显示
                UpdateDisplayTime();
                IsResetButtonEnabled = true;
                IsStartButtonEnabled = true;
                IsStopTimeVisible = false;
                TimeForeground = new SolidColorBrush(Color.FromArgb(0xFF, 0x5B, 0x5D, 0x5F));
                return;
            }

            if (IsPaused)
            {
                // 暂停时完全重置
                UpdateDisplayTime();
                IsResetButtonEnabled = true;
                IsStartButtonEnabled = true;
                IsStopTimeVisible = false;
                TimeForeground = new SolidColorBrush(Color.FromArgb(0xFF, 0x5B, 0x5D, 0x5F));
                StartButtonIcon = "\uE768";
                IsRunning = false;
                _timer.Stop();
                IsPaused = false;
                ProgressValue = 0;
                IsProgressPaused = false;
            }
            else
            {
                // 运行时重新开始
                UpdateStopTime();
                _startTime = DateTime.Now;
                OnTimerElapsed(null, null);
            }
        }

        /// <summary>
        /// 切换全屏
        /// </summary>
        [RelayCommand]
        private void ToggleFullscreen()
        {
            ToggleFullscreenRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 切换紧凑模式
        /// </summary>
        [RelayCommand]
        private void ToggleCompactMode()
        {
            ToggleCompactModeRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 关闭窗口
        /// </summary>
        [RelayCommand]
        private void Close()
        {
            Cleanup();
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 计时器触发事件
        /// </summary>
        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (!IsRunning || IsPaused)
            {
                _timer.Stop();
                return;
            }

            TimeSpan timeSpan = DateTime.Now - _startTime;
            TimeSpan totalTimeSpan = new TimeSpan(Hour, Minute, Second);
            TimeSpan leftTimeSpan = totalTimeSpan - timeSpan;

            if (leftTimeSpan.Milliseconds > 0) leftTimeSpan += new TimeSpan(0, 0, 1);
            double spentTimePercent = timeSpan.TotalMilliseconds / (_totalSeconds * 1000.0);

            Application.Current.Dispatcher.Invoke(() =>
            {
                ProgressValue = 1 - spentTimePercent;
                Hour = leftTimeSpan.Hours;
                Minute = leftTimeSpan.Minutes;
                Second = leftTimeSpan.Seconds;
                CurrentTimeText = leftTimeSpan.ToString(@"hh\:mm\:ss");

                if (spentTimePercent >= 1)
                {
                    OnTimerCompleted();
                }
            });

            if (spentTimePercent >= 1)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        _player.Stream = Properties.Resources.TimerDownNotice;
                        _player.Play();
                    }
                    catch { }
                });
            }
        }

        /// <summary>
        /// 计时完成处理
        /// </summary>
        private void OnTimerCompleted()
        {
            ProgressValue = 0;
            Hour = 0;
            Minute = 0;
            Second = 0;
            _timer.Stop();
            IsRunning = false;
            StartButtonIcon = "\uE768";
            IsStartButtonEnabled = true;
            TimeForeground = new SolidColorBrush(Color.FromArgb(0xFF, 0x5B, 0x5D, 0x5F));
            IsStopTimeVisible = false;
            TimerCompleted?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 更新显示时间
        /// </summary>
        private void UpdateDisplayTime()
        {
            CurrentTimeText = $"{HourText}:{MinuteText}:{SecondText}";
        }

        /// <summary>
        /// 更新结束时间
        /// </summary>
        private void UpdateStopTime()
        {
            TimeSpan totalTimeSpan = new TimeSpan(Hour, Minute, Second);
            StopTimeText = (_startTime + totalTimeSpan).ToString("t");
        }

        #endregion

        #region 事件

        /// <summary>
        /// 请求关闭窗口事件
        /// </summary>
        public event EventHandler RequestClose;

        /// <summary>
        /// 计时完成事件
        /// </summary>
        public event EventHandler TimerCompleted;

        /// <summary>
        /// 请求切换全屏事件
        /// </summary>
        public event EventHandler ToggleFullscreenRequested;

        /// <summary>
        /// 请求切换紧凑模式事件
        /// </summary>
        public event EventHandler ToggleCompactModeRequested;

        #endregion

        #region 清理

        /// <summary>
        /// 清理资源
        /// </summary>
        public override void Cleanup()
        {
            IsRunning = false;
            _timer?.Stop();
            _timer?.Dispose();
            _player?.Dispose();
            base.Cleanup();
        }

        #endregion
    }
}
