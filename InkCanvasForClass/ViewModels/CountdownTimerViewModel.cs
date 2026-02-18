using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ink_Canvas.Core;
using System;
using System.Media;
using System.Timers;
using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace Ink_Canvas.ViewModels
{
    public partial class CountdownTimerViewModel : ViewModelBase
    {
        private readonly System.Timers.Timer _timer = new();
        private DateTime _startTime;
        private DateTime _pauseTime;
        private int _totalSeconds;
        private SoundPlayer _player = new();

        // 缓存的画笔
        private static readonly SolidColorBrush DefaultHourForegroundBrush = CreateFrozenBrush("#FF5B5D5F");
        private static readonly SolidColorBrush BlackBrush = Brushes.Black;

        private static SolidColorBrush CreateFrozenBrush(string colorHex)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex));
            brush.Freeze();
            return brush;
        }

        public CountdownTimerViewModel()
        {
            _timer.Elapsed += Timer_Elapsed;
            _timer.Interval = 50;

            // 初始化显示
            UpdateDisplayTime();
            UpdateVisibility();
        }

        [ObservableProperty]
        private int _hour = 0;

        [ObservableProperty]
        private int _minute = 1;

        [ObservableProperty]
        private int _second = 0;

        [ObservableProperty]
        private string _hourText = "00";

        [ObservableProperty]
        private string _minuteText = "01";

        [ObservableProperty]
        private string _secondText = "00";

        [ObservableProperty]
        private string _currentTimeText = "";

        [ObservableProperty]
        private string _stopTimeText = "";

        [ObservableProperty]
        private double _processBarValue = 0;

        [ObservableProperty]
        private bool _isProcessBarPaused;

        [ObservableProperty]
        private bool _isTimerRunning;

        [ObservableProperty]
        private bool _isPaused;

        [ObservableProperty]
        private Visibility _processBarVisibility = Visibility.Visible;

        [ObservableProperty]
        private Visibility _adjustHourVisibility = Visibility.Collapsed;

        [ObservableProperty]
        private Visibility _borderStopTimeVisibility = Visibility.Collapsed;

        [ObservableProperty]
        private Visibility _btnStartCoverVisibility = Visibility.Collapsed;

        [ObservableProperty]
        private Visibility _btnResetCoverVisibility = Visibility.Visible;

        [ObservableProperty]
        private Visibility _bigViewControllerVisibility = Visibility.Visible;

        [ObservableProperty]
        private Visibility _tbCurrentTimeVisibility = Visibility.Collapsed;

        [ObservableProperty]
        private Brush _hourForeground = DefaultHourForegroundBrush;

        [ObservableProperty]
        private SymbolRegular _startIcon = SymbolRegular.Play24;

        [ObservableProperty]
        private SymbolRegular _fullscreenIcon = SymbolRegular.FullScreenMaximize24;

        [ObservableProperty]
        private bool _isInCompactMode;

        [ObservableProperty]
        private double _windowWidth = 1100;

        [ObservableProperty]
        private double _windowHeight = 700;

        private WindowState _currentWindowState = WindowState.Normal;
        public WindowState CurrentWindowState
        {
            get => _currentWindowState;
            set
            {
                if (SetProperty(ref _currentWindowState, value))
                {
                     FullscreenIcon = value == WindowState.Maximized 
                        ? SymbolRegular.FullScreenMinimize24 
                        : SymbolRegular.FullScreenMaximize24;
                }
            }
        }

        // Action needed for window operations that can't be bound directly easily
        public Action? CenterWindowAction { get; set; }
        public Action? CloseWindowAction { get; set; }

        partial void OnHourChanged(int value) => HourText = value.ToString("00");
        partial void OnMinuteChanged(int value) => MinuteText = value.ToString("00");
        partial void OnSecondChanged(int value) => SecondText = value.ToString("00");

        private void Timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            if (!IsTimerRunning || IsPaused)
            {
                _timer.Stop();
                return;
            }

            TimeSpan timeSpan = DateTime.Now - _startTime;
            TimeSpan totalTimeSpan = new TimeSpan(Hour, Minute, Second);
            TimeSpan leftTimeSpan = totalTimeSpan - timeSpan;
            
            if (leftTimeSpan.Milliseconds > 0) 
                leftTimeSpan += new TimeSpan(0, 0, 1);
            
            double spentTimePercent = timeSpan.TotalMilliseconds / (_totalSeconds * 1000.0);

            Application.Current.Dispatcher.Invoke(() =>
            {
                ProcessBarValue = 1 - spentTimePercent;
                
                // Don't update Hour/Minute/Second properties directly to avoid loop or reset logic issues during run
                // Just update text for display
                HourText = leftTimeSpan.Hours.ToString("00");
                MinuteText = leftTimeSpan.Minutes.ToString("00");
                SecondText = leftTimeSpan.Seconds.ToString("00");
                CurrentTimeText = leftTimeSpan.ToString(@"hh\:mm\:ss");

                if (spentTimePercent >= 1)
                {
                    TimerFinished();
                }
            });
        }

        private void TimerFinished()
        {
            ProcessBarValue = 0;
            HourText = "00";
            MinuteText = "00";
            SecondText = "00";
            _timer.Stop();
            IsTimerRunning = false;
            StartIcon = SymbolRegular.Play24;
            BtnStartCoverVisibility = Visibility.Visible;
            HourForeground = DefaultHourForegroundBrush;
            BorderStopTimeVisibility = Visibility.Collapsed;

            // Play sound
            try 
            {
                _player.Stream = Ink_Canvas.Properties.Resources.TimerDownNotice;
                _player.Play();
            }
            catch (Exception ex)
            {
                // Handle or log error
                System.Diagnostics.Debug.WriteLine($"Error playing sound: {ex.Message}");
            }
        }

        [RelayCommand]
        private void Start()
        {
            if (IsPaused && IsTimerRunning)
            {
                // Resume
                _startTime += DateTime.Now - _pauseTime;
                IsProcessBarPaused = false;
                HourForeground = BlackBrush;
                StartIcon = SymbolRegular.Pause24;
                IsPaused = false;
                _timer.Start();
                UpdateStopTime();
                BorderStopTimeVisibility = Visibility.Visible;
            }
            else if (IsTimerRunning)
            {
                // Pause
                _pauseTime = DateTime.Now;
                IsProcessBarPaused = true;
                HourForeground = DefaultHourForegroundBrush;
                StartIcon = SymbolRegular.Play24;
                BorderStopTimeVisibility = Visibility.Collapsed;
                IsPaused = true;
                _timer.Stop();
            }
            else
            {
                // Start from beginning
                _startTime = DateTime.Now;
                _totalSeconds = ((Hour * 60) + Minute) * 60 + Second;
                
                if (_totalSeconds == 0) return; // Prevent division by zero

                IsProcessBarPaused = false;
                HourForeground = BlackBrush;
                StartIcon = SymbolRegular.Pause24;
                BtnResetCoverVisibility = Visibility.Collapsed;

                if (_totalSeconds <= 10) _timer.Interval = 20;
                else if (_totalSeconds <= 60) _timer.Interval = 30;
                else if (_totalSeconds <= 120) _timer.Interval = 50;
                else _timer.Interval = 100;

                IsPaused = false;
                IsTimerRunning = true;
                _timer.Start();
                UpdateStopTime();
                BorderStopTimeVisibility = Visibility.Visible;
            }
        }

        [RelayCommand]
        private void Reset()
        {
            if (!IsTimerRunning)
            {
                UpdateDisplayTime();
                BtnResetCoverVisibility = Visibility.Visible;
                BtnStartCoverVisibility = Visibility.Collapsed;
                BorderStopTimeVisibility = Visibility.Collapsed;
                HourForeground = DefaultHourForegroundBrush;
            }
            else if (IsPaused)
            {
                UpdateDisplayTime();
                BtnResetCoverVisibility = Visibility.Visible;
                BtnStartCoverVisibility = Visibility.Collapsed;
                BorderStopTimeVisibility = Visibility.Collapsed;
                HourForeground = DefaultHourForegroundBrush;
                StartIcon = SymbolRegular.Play24;
                IsTimerRunning = false;
                _timer.Stop();
                IsPaused = false;
                ProcessBarValue = 0;
                IsProcessBarPaused = false;
            }
            else
            {
                UpdateStopTime();
                _startTime = DateTime.Now;
                // Manually trigger one update
                Timer_Elapsed(_timer, null);
            }
        }

        private void UpdateDisplayTime()
        {
            HourText = Hour.ToString("00");
            MinuteText = Minute.ToString("00");
            SecondText = Second.ToString("00");
        }

        private void UpdateStopTime()
        {
            TimeSpan totalTimeSpan = new TimeSpan(Hour, Minute, Second);
            StopTimeText = (_startTime + totalTimeSpan).ToString("t");
        }

        [RelayCommand]
        private void AdjustTime(string parameter)
        {
            if (IsTimerRunning) return;

            // parameter format: "type:amount", e.g., "hour:1", "hour:-5", "minute:5"
            var parts = parameter.Split(':');
            if (parts.Length != 2) return;

            string type = parts[0];
            if (!int.TryParse(parts[1], out int amount)) return;

            switch (type)
            {
                case "hour":
                    Hour += amount;
                    if (Hour >= 100) Hour = 0;
                    else if (Hour < 0) Hour = 99;
                    break;
                case "minute":
                    Minute += amount;
                    if (Minute >= 60) Minute = 0;
                    else if (Minute < 0) Minute = 59;
                    break;
                case "second":
                    Second += amount;
                    if (Second >= 60) Second = 0;
                    else if (Second < 0) Second = 59;
                    break;
            }
        }

        [RelayCommand]
        private void ToggleAdjustMode()
        {
            if (IsTimerRunning) return;

            if (ProcessBarVisibility == Visibility.Visible)
            {
                ProcessBarVisibility = Visibility.Collapsed;
                AdjustHourVisibility = Visibility.Visible;
                HourForeground = BlackBrush;
            }
            else
            {
                ProcessBarVisibility = Visibility.Visible;
                AdjustHourVisibility = Visibility.Collapsed;
                HourForeground = DefaultHourForegroundBrush;

                if (Hour == 0 && Minute == 0 && Second == 0)
                {
                    Second = 1;
                }
            }
        }

        [RelayCommand]
        private void ToggleCompactMode()
        {
            if (IsInCompactMode)
            {
                // Restore to normal
                WindowWidth = 1100;
                WindowHeight = 700;
                BigViewControllerVisibility = Visibility.Visible;
                TbCurrentTimeVisibility = Visibility.Collapsed;
                CenterWindowAction?.Invoke();
            }
            else
            {
                // Go to compact
                if (CurrentWindowState == WindowState.Maximized)
                {
                    CurrentWindowState = WindowState.Normal;
                }
                WindowWidth = 400;
                WindowHeight = 250;
                BigViewControllerVisibility = Visibility.Collapsed;
                TbCurrentTimeVisibility = Visibility.Visible;
            }
            IsInCompactMode = !IsInCompactMode;
        }

        [RelayCommand]
        private void ToggleFullscreen()
        {
            if (CurrentWindowState == WindowState.Normal)
            {
                CurrentWindowState = WindowState.Maximized;
            }
            else
            {
                CurrentWindowState = WindowState.Normal;
            }
        }

        [RelayCommand]
        private void Close()
        {
            IsTimerRunning = false;
            _timer.Stop();
            _timer.Dispose();
            CloseWindowAction?.Invoke();
        }

        public override void Cleanup()
        {
            IsTimerRunning = false;
            _timer.Stop();
            _timer.Dispose();
            base.Cleanup();
        }

        private void UpdateVisibility()
        {
            // Initial visibility setup if needed
        }
    }
}
