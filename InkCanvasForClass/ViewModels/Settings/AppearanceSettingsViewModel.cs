using CommunityToolkit.Mvvm.ComponentModel;
using System;
using Ink_Canvas.Models.Settings;

namespace Ink_Canvas.ViewModels
{
    /// <summary>
    /// Appearance 设置 ViewModel
    /// </summary>
    public partial class AppearanceSettingsViewModel : ObservableObject
    {
        private readonly AppearanceSettings _appearance;
        private readonly Action _saveAction;

        public AppearanceSettingsViewModel(AppearanceSettings appearance, Action saveAction)
        {
            _appearance = appearance ?? throw new ArgumentNullException(nameof(appearance));
            _saveAction = saveAction;
        }

        public bool IsEnableDisPlayNibModeToggler
        {
            get => _appearance.IsEnableDisPlayNibModeToggler;
            set { if (SetProperty(_appearance.IsEnableDisPlayNibModeToggler, value, _appearance, (a, v) => a.IsEnableDisPlayNibModeToggler = v)) _saveAction?.Invoke(); }
        }

        public bool IsColorfulViewboxFloatingBar
        {
            get => _appearance.IsColorfulViewboxFloatingBar;
            set { if (SetProperty(_appearance.IsColorfulViewboxFloatingBar, value, _appearance, (a, v) => a.IsColorfulViewboxFloatingBar = v)) _saveAction?.Invoke(); }
        }

        public double ViewboxFloatingBarScaleTransformValue
        {
            get => _appearance.ViewboxFloatingBarScaleTransformValue;
            set { if (SetProperty(_appearance.ViewboxFloatingBarScaleTransformValue, value, _appearance, (a, v) => a.ViewboxFloatingBarScaleTransformValue = v)) _saveAction?.Invoke(); }
        }

        public int FloatingBarImg
        {
            get => _appearance.FloatingBarImg;
            set { if (SetProperty(_appearance.FloatingBarImg, value, _appearance, (a, v) => a.FloatingBarImg = v)) _saveAction?.Invoke(); }
        }

        public double ViewboxFloatingBarOpacityValue
        {
            get => _appearance.ViewboxFloatingBarOpacityValue;
            set { if (SetProperty(_appearance.ViewboxFloatingBarOpacityValue, value, _appearance, (a, v) => a.ViewboxFloatingBarOpacityValue = v)) _saveAction?.Invoke(); }
        }

        public bool EnableTrayIcon
        {
            get => _appearance.EnableTrayIcon;
            set { if (SetProperty(_appearance.EnableTrayIcon, value, _appearance, (a, v) => a.EnableTrayIcon = v)) _saveAction?.Invoke(); }
        }

        public double ViewboxFloatingBarOpacityInPPTValue
        {
            get => _appearance.ViewboxFloatingBarOpacityInPPTValue;
            set { if (SetProperty(_appearance.ViewboxFloatingBarOpacityInPPTValue, value, _appearance, (a, v) => a.ViewboxFloatingBarOpacityInPPTValue = v)) _saveAction?.Invoke(); }
        }

        public bool EnableViewboxBlackBoardScaleTransform
        {
            get => _appearance.EnableViewboxBlackBoardScaleTransform;
            set { if (SetProperty(_appearance.EnableViewboxBlackBoardScaleTransform, value, _appearance, (a, v) => a.EnableViewboxBlackBoardScaleTransform = v)) _saveAction?.Invoke(); }
        }

        public bool IsTransparentButtonBackground
        {
            get => _appearance.IsTransparentButtonBackground;
            set { if (SetProperty(_appearance.IsTransparentButtonBackground, value, _appearance, (a, v) => a.IsTransparentButtonBackground = v)) _saveAction?.Invoke(); }
        }

        public bool IsShowExitButton
        {
            get => _appearance.IsShowExitButton;
            set { if (SetProperty(_appearance.IsShowExitButton, value, _appearance, (a, v) => a.IsShowExitButton = v)) _saveAction?.Invoke(); }
        }

        public bool IsShowEraserButton
        {
            get => _appearance.IsShowEraserButton;
            set { if (SetProperty(_appearance.IsShowEraserButton, value, _appearance, (a, v) => a.IsShowEraserButton = v)) _saveAction?.Invoke(); }
        }

        public bool EnableTimeDisplayInWhiteboardMode
        {
            get => _appearance.EnableTimeDisplayInWhiteboardMode;
            set { if (SetProperty(_appearance.EnableTimeDisplayInWhiteboardMode, value, _appearance, (a, v) => a.EnableTimeDisplayInWhiteboardMode = v)) _saveAction?.Invoke(); }
        }

        public bool EnableChickenSoupInWhiteboardMode
        {
            get => _appearance.EnableChickenSoupInWhiteboardMode;
            set { if (SetProperty(_appearance.EnableChickenSoupInWhiteboardMode, value, _appearance, (a, v) => a.EnableChickenSoupInWhiteboardMode = v)) _saveAction?.Invoke(); }
        }

        public bool IsShowHideControlButton
        {
            get => _appearance.IsShowHideControlButton;
            set { if (SetProperty(_appearance.IsShowHideControlButton, value, _appearance, (a, v) => a.IsShowHideControlButton = v)) _saveAction?.Invoke(); }
        }

        public int UnFoldButtonImageType
        {
            get => _appearance.UnFoldButtonImageType;
            set { if (SetProperty(_appearance.UnFoldButtonImageType, value, _appearance, (a, v) => a.UnFoldButtonImageType = v)) _saveAction?.Invoke(); }
        }

        public bool IsShowLRSwitchButton
        {
            get => _appearance.IsShowLRSwitchButton;
            set { if (SetProperty(_appearance.IsShowLRSwitchButton, value, _appearance, (a, v) => a.IsShowLRSwitchButton = v)) _saveAction?.Invoke(); }
        }

        public bool IsShowQuickPanel
        {
            get => _appearance.IsShowQuickPanel;
            set { if (SetProperty(_appearance.IsShowQuickPanel, value, _appearance, (a, v) => a.IsShowQuickPanel = v)) _saveAction?.Invoke(); }
        }

        public int ChickenSoupSource
        {
            get => _appearance.ChickenSoupSource;
            set { if (SetProperty(_appearance.ChickenSoupSource, value, _appearance, (a, v) => a.ChickenSoupSource = v)) _saveAction?.Invoke(); }
        }

        public bool IsShowModeFingerToggleSwitch
        {
            get => _appearance.IsShowModeFingerToggleSwitch;
            set { if (SetProperty(_appearance.IsShowModeFingerToggleSwitch, value, _appearance, (a, v) => a.IsShowModeFingerToggleSwitch = v)) _saveAction?.Invoke(); }
        }

        public int Theme
        {
            get => _appearance.Theme;
            set { if (SetProperty(_appearance.Theme, value, _appearance, (a, v) => a.Theme = v)) _saveAction?.Invoke(); }
        }

        public bool FloatingBarButtonLabelVisibility
        {
            get => _appearance.FloatingBarButtonLabelVisibility;
            set { if (SetProperty(_appearance.FloatingBarButtonLabelVisibility, value, _appearance, (a, v) => a.FloatingBarButtonLabelVisibility = v)) _saveAction?.Invoke(); }
        }

        public string FloatingBarIconsVisibility
        {
            get => _appearance.FloatingBarIconsVisibility;
            set { if (SetProperty(_appearance.FloatingBarIconsVisibility, value, _appearance, (a, v) => a.FloatingBarIconsVisibility = v)) _saveAction?.Invoke(); }
        }

        public int EraserButtonsVisibility
        {
            get => _appearance.EraserButtonsVisibility;
            set { if (SetProperty(_appearance.EraserButtonsVisibility, value, _appearance, (a, v) => a.EraserButtonsVisibility = v)) _saveAction?.Invoke(); }
        }

        public bool OnlyDisplayEraserBtn
        {
            get => _appearance.OnlyDisplayEraserBtn;
            set { if (SetProperty(_appearance.OnlyDisplayEraserBtn, value, _appearance, (a, v) => a.OnlyDisplayEraserBtn = v)) _saveAction?.Invoke(); }
        }
    }
}