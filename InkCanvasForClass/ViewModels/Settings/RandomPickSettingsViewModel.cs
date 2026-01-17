using CommunityToolkit.Mvvm.ComponentModel;
using Ink_Canvas.Models.Settings;
using System;

namespace Ink_Canvas.ViewModels.Settings
{
    public partial class RandomPickSettingsViewModel : ObservableObject
    {
        private readonly RandSettings _settings;
        private readonly Action _saveAction;

        public RandomPickSettingsViewModel(RandSettings settings, Action saveAction)
        {
            _settings = settings;
            _saveAction = saveAction;
        }

        public bool EnableMachineLearning
        {
            get => _settings.EnableMachineLearning;
            set
            {
                if (_settings.EnableMachineLearning != value)
                {
                    _settings.EnableMachineLearning = value;
                    OnPropertyChanged();
                    _saveAction?.Invoke();
                }
            }
        }

        public bool DisplaySwitchRandomPickListBtn
        {
            get => _settings.DisplaySwitchRandomPickListBtn;
            set
            {
                if (_settings.DisplaySwitchRandomPickListBtn != value)
                {
                    _settings.DisplaySwitchRandomPickListBtn = value;
                    OnPropertyChanged();
                    _saveAction?.Invoke();
                }
            }
        }

        public bool DisplayPickHistory
        {
            get => _settings.DisplayPickHistory;
            set
            {
                if (_settings.DisplayPickHistory != value)
                {
                    _settings.DisplayPickHistory = value;
                    OnPropertyChanged();
                    _saveAction?.Invoke();
                }
            }
        }

        public bool DisplayRandWindowNamesInputBtn
        {
            get => _settings.DisplayRandWindowNamesInputBtn;
            set
            {
                if (_settings.DisplayRandWindowNamesInputBtn != value)
                {
                    _settings.DisplayRandWindowNamesInputBtn = value;
                    OnPropertyChanged();
                    _saveAction?.Invoke();
                }
            }
        }

        public double RandWindowOnceCloseLatency
        {
            get => _settings.RandWindowOnceCloseLatency;
            set
            {
                if (Math.Abs(_settings.RandWindowOnceCloseLatency - value) > 0.001)
                {
                    _settings.RandWindowOnceCloseLatency = value;
                    OnPropertyChanged();
                    _saveAction?.Invoke();
                }
            }
        }

        public int RandWindowOnceMaxStudents
        {
            get => _settings.RandWindowOnceMaxStudents;
            set
            {
                if (_settings.RandWindowOnceMaxStudents != value)
                {
                    _settings.RandWindowOnceMaxStudents = value;
                    OnPropertyChanged();
                    _saveAction?.Invoke();
                }
            }
        }
    }
}
