using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace Ink_Canvas.ViewModels
{
    /// <summary>
    /// Gesture 设置 ViewModel
    /// </summary>
    public partial class GestureSettingsViewModel : ObservableObject
    {
        private readonly Gesture _gesture;
        private readonly Action _saveAction;

        public GestureSettingsViewModel(Gesture gesture, Action saveAction)
        {
            _gesture = gesture ?? throw new ArgumentNullException(nameof(gesture));
            _saveAction = saveAction;
        }

        public bool IsEnableTwoFingerGesture => _gesture.IsEnableTwoFingerGesture;
        public bool IsEnableTwoFingerGestureTranslateOrRotation => _gesture.IsEnableTwoFingerGestureTranslateOrRotation;

        public bool IsEnableMultiTouchMode
        {
            get => _gesture.IsEnableMultiTouchMode;
            set { if (SetProperty(_gesture.IsEnableMultiTouchMode, value, _gesture, (g, v) => g.IsEnableMultiTouchMode = v)) _saveAction?.Invoke(); }
        }

        public bool IsEnableTwoFingerZoom
        {
            get => _gesture.IsEnableTwoFingerZoom;
            set { if (SetProperty(_gesture.IsEnableTwoFingerZoom, value, _gesture, (g, v) => g.IsEnableTwoFingerZoom = v)) { _saveAction?.Invoke(); OnPropertyChanged(nameof(IsEnableTwoFingerGesture)); } }
        }

        public bool IsEnableTwoFingerTranslate
        {
            get => _gesture.IsEnableTwoFingerTranslate;
            set { if (SetProperty(_gesture.IsEnableTwoFingerTranslate, value, _gesture, (g, v) => g.IsEnableTwoFingerTranslate = v)) { _saveAction?.Invoke(); OnPropertyChanged(nameof(IsEnableTwoFingerGesture)); OnPropertyChanged(nameof(IsEnableTwoFingerGestureTranslateOrRotation)); } }
        }

        public bool AutoSwitchTwoFingerGesture
        {
            get => _gesture.AutoSwitchTwoFingerGesture;
            set { if (SetProperty(_gesture.AutoSwitchTwoFingerGesture, value, _gesture, (g, v) => g.AutoSwitchTwoFingerGesture = v)) _saveAction?.Invoke(); }
        }

        public bool IsEnableTwoFingerRotation
        {
            get => _gesture.IsEnableTwoFingerRotation;
            set { if (SetProperty(_gesture.IsEnableTwoFingerRotation, value, _gesture, (g, v) => g.IsEnableTwoFingerRotation = v)) { _saveAction?.Invoke(); OnPropertyChanged(nameof(IsEnableTwoFingerGesture)); OnPropertyChanged(nameof(IsEnableTwoFingerGestureTranslateOrRotation)); } }
        }

        public bool IsEnableTwoFingerRotationOnSelection
        {
            get => _gesture.IsEnableTwoFingerRotationOnSelection;
            set { if (SetProperty(_gesture.IsEnableTwoFingerRotationOnSelection, value, _gesture, (g, v) => g.IsEnableTwoFingerRotationOnSelection = v)) _saveAction?.Invoke(); }
        }

        public bool DisableGestureEraser
        {
            get => _gesture.DisableGestureEraser;
            set { if (SetProperty(_gesture.DisableGestureEraser, value, _gesture, (g, v) => g.DisableGestureEraser = v)) _saveAction?.Invoke(); }
        }

        public int DefaultMultiPointHandWritingMode
        {
            get => _gesture.DefaultMultiPointHandWritingMode;
            set { if (SetProperty(_gesture.DefaultMultiPointHandWritingMode, value, _gesture, (g, v) => g.DefaultMultiPointHandWritingMode = v)) _saveAction?.Invoke(); }
        }

        public bool HideCursorWhenUsingTouchDevice
        {
            get => _gesture.HideCursorWhenUsingTouchDevice;
            set { if (SetProperty(_gesture.HideCursorWhenUsingTouchDevice, value, _gesture, (g, v) => g.HideCursorWhenUsingTouchDevice = v)) _saveAction?.Invoke(); }
        }

        public bool EnableMouseGesture
        {
            get => _gesture.EnableMouseGesture;
            set { if (SetProperty(_gesture.EnableMouseGesture, value, _gesture, (g, v) => g.EnableMouseGesture = v)) _saveAction?.Invoke(); }
        }

        public bool EnableMouseRightBtnGesture
        {
            get => _gesture.EnableMouseRightBtnGesture;
            set { if (SetProperty(_gesture.EnableMouseRightBtnGesture, value, _gesture, (g, v) => g.EnableMouseRightBtnGesture = v)) _saveAction?.Invoke(); }
        }

        public bool EnableMouseWheelGesture
        {
            get => _gesture.EnableMouseWheelGesture;
            set { if (SetProperty(_gesture.EnableMouseWheelGesture, value, _gesture, (g, v) => g.EnableMouseWheelGesture = v)) _saveAction?.Invoke(); }
        }
    }

    /// <summary>
    /// Startup 设置 ViewModel
    /// </summary>
    public partial class StartupSettingsViewModel : ObservableObject
    {
        private readonly Startup _startup;
        private readonly Action _saveAction;

        public StartupSettingsViewModel(Startup startup, Action saveAction)
        {
            _startup = startup ?? throw new ArgumentNullException(nameof(startup));
            _saveAction = saveAction;
        }

        public bool IsAutoUpdate
        {
            get => _startup.IsAutoUpdate;
            set { if (SetProperty(_startup.IsAutoUpdate, value, _startup, (s, v) => s.IsAutoUpdate = v)) _saveAction?.Invoke(); }
        }

        public bool IsAutoUpdateWithSilence
        {
            get => _startup.IsAutoUpdateWithSilence;
            set { if (SetProperty(_startup.IsAutoUpdateWithSilence, value, _startup, (s, v) => s.IsAutoUpdateWithSilence = v)) _saveAction?.Invoke(); }
        }

        public string AutoUpdateWithSilenceStartTime
        {
            get => _startup.AutoUpdateWithSilenceStartTime;
            set { if (SetProperty(_startup.AutoUpdateWithSilenceStartTime, value, _startup, (s, v) => s.AutoUpdateWithSilenceStartTime = v)) _saveAction?.Invoke(); }
        }

        public string AutoUpdateWithSilenceEndTime
        {
            get => _startup.AutoUpdateWithSilenceEndTime;
            set { if (SetProperty(_startup.AutoUpdateWithSilenceEndTime, value, _startup, (s, v) => s.AutoUpdateWithSilenceEndTime = v)) _saveAction?.Invoke(); }
        }

        public bool IsEnableNibMode
        {
            get => _startup.IsEnableNibMode;
            set { if (SetProperty(_startup.IsEnableNibMode, value, _startup, (s, v) => s.IsEnableNibMode = v)) _saveAction?.Invoke(); }
        }

        public bool IsFoldAtStartup
        {
            get => _startup.IsFoldAtStartup;
            set { if (SetProperty(_startup.IsFoldAtStartup, value, _startup, (s, v) => s.IsFoldAtStartup = v)) _saveAction?.Invoke(); }
        }

        public bool EnableWindowChromeRendering
        {
            get => _startup.EnableWindowChromeRendering;
            set { if (SetProperty(_startup.EnableWindowChromeRendering, value, _startup, (s, v) => s.EnableWindowChromeRendering = v)) _saveAction?.Invoke(); }
        }
    }

    /// <summary>
    /// Advanced 设置 ViewModel
    /// </summary>
    public partial class AdvancedSettingsViewModel : ObservableObject
    {
        private readonly Advanced _advanced;
        private readonly Action _saveAction;

        public AdvancedSettingsViewModel(Advanced advanced, Action saveAction)
        {
            _advanced = advanced ?? throw new ArgumentNullException(nameof(advanced));
            _saveAction = saveAction;
        }

        public bool IsSpecialScreen
        {
            get => _advanced.IsSpecialScreen;
            set { if (SetProperty(_advanced.IsSpecialScreen, value, _advanced, (a, v) => a.IsSpecialScreen = v)) _saveAction?.Invoke(); }
        }

        public bool IsQuadIR
        {
            get => _advanced.IsQuadIR;
            set { if (SetProperty(_advanced.IsQuadIR, value, _advanced, (a, v) => a.IsQuadIR = v)) _saveAction?.Invoke(); }
        }

        public double TouchMultiplier
        {
            get => _advanced.TouchMultiplier;
            set { if (SetProperty(_advanced.TouchMultiplier, value, _advanced, (a, v) => a.TouchMultiplier = v)) _saveAction?.Invoke(); }
        }

        public int NibModeBoundsWidth
        {
            get => _advanced.NibModeBoundsWidth;
            set { if (SetProperty(_advanced.NibModeBoundsWidth, value, _advanced, (a, v) => a.NibModeBoundsWidth = v)) _saveAction?.Invoke(); }
        }

        public int FingerModeBoundsWidth
        {
            get => _advanced.FingerModeBoundsWidth;
            set { if (SetProperty(_advanced.FingerModeBoundsWidth, value, _advanced, (a, v) => a.FingerModeBoundsWidth = v)) _saveAction?.Invoke(); }
        }

        public bool EraserBindTouchMultiplier
        {
            get => _advanced.EraserBindTouchMultiplier;
            set { if (SetProperty(_advanced.EraserBindTouchMultiplier, value, _advanced, (a, v) => a.EraserBindTouchMultiplier = v)) _saveAction?.Invoke(); }
        }

        public double NibModeBoundsWidthThresholdValue
        {
            get => _advanced.NibModeBoundsWidthThresholdValue;
            set { if (SetProperty(_advanced.NibModeBoundsWidthThresholdValue, value, _advanced, (a, v) => a.NibModeBoundsWidthThresholdValue = v)) _saveAction?.Invoke(); }
        }

        public double FingerModeBoundsWidthThresholdValue
        {
            get => _advanced.FingerModeBoundsWidthThresholdValue;
            set { if (SetProperty(_advanced.FingerModeBoundsWidthThresholdValue, value, _advanced, (a, v) => a.FingerModeBoundsWidthThresholdValue = v)) _saveAction?.Invoke(); }
        }

        public double NibModeBoundsWidthEraserSize
        {
            get => _advanced.NibModeBoundsWidthEraserSize;
            set { if (SetProperty(_advanced.NibModeBoundsWidthEraserSize, value, _advanced, (a, v) => a.NibModeBoundsWidthEraserSize = v)) _saveAction?.Invoke(); }
        }

        public double FingerModeBoundsWidthEraserSize
        {
            get => _advanced.FingerModeBoundsWidthEraserSize;
            set { if (SetProperty(_advanced.FingerModeBoundsWidthEraserSize, value, _advanced, (a, v) => a.FingerModeBoundsWidthEraserSize = v)) _saveAction?.Invoke(); }
        }

        public bool IsLogEnabled
        {
            get => _advanced.IsLogEnabled;
            set { if (SetProperty(_advanced.IsLogEnabled, value, _advanced, (a, v) => a.IsLogEnabled = v)) _saveAction?.Invoke(); }
        }

        public bool IsEnableFullScreenHelper
        {
            get => _advanced.IsEnableFullScreenHelper;
            set { if (SetProperty(_advanced.IsEnableFullScreenHelper, value, _advanced, (a, v) => a.IsEnableFullScreenHelper = v)) _saveAction?.Invoke(); }
        }

        public bool IsEnableEdgeGestureUtil
        {
            get => _advanced.IsEnableEdgeGestureUtil;
            set { if (SetProperty(_advanced.IsEnableEdgeGestureUtil, value, _advanced, (a, v) => a.IsEnableEdgeGestureUtil = v)) _saveAction?.Invoke(); }
        }

        public bool EdgeGestureUtilOnlyAffectBlackboardMode
        {
            get => _advanced.EdgeGestureUtilOnlyAffectBlackboardMode;
            set { if (SetProperty(_advanced.EdgeGestureUtilOnlyAffectBlackboardMode, value, _advanced, (a, v) => a.EdgeGestureUtilOnlyAffectBlackboardMode = v)) _saveAction?.Invoke(); }
        }

        public bool IsEnableForceFullScreen
        {
            get => _advanced.IsEnableForceFullScreen;
            set { if (SetProperty(_advanced.IsEnableForceFullScreen, value, _advanced, (a, v) => a.IsEnableForceFullScreen = v)) _saveAction?.Invoke(); }
        }

        public bool IsEnableResolutionChangeDetection
        {
            get => _advanced.IsEnableResolutionChangeDetection;
            set { if (SetProperty(_advanced.IsEnableResolutionChangeDetection, value, _advanced, (a, v) => a.IsEnableResolutionChangeDetection = v)) _saveAction?.Invoke(); }
        }

        public bool IsEnableDPIChangeDetection
        {
            get => _advanced.IsEnableDPIChangeDetection;
            set { if (SetProperty(_advanced.IsEnableDPIChangeDetection, value, _advanced, (a, v) => a.IsEnableDPIChangeDetection = v)) _saveAction?.Invoke(); }
        }

        public bool IsDisableCloseWindow
        {
            get => _advanced.IsDisableCloseWindow;
            set { if (SetProperty(_advanced.IsDisableCloseWindow, value, _advanced, (a, v) => a.IsDisableCloseWindow = v)) _saveAction?.Invoke(); }
        }

        public bool EnableForceTopMost
        {
            get => _advanced.EnableForceTopMost;
            set { if (SetProperty(_advanced.EnableForceTopMost, value, _advanced, (a, v) => a.EnableForceTopMost = v)) _saveAction?.Invoke(); }
        }
    }

    /// <summary>
    /// Snapshot 设置 ViewModel
    /// </summary>
    public partial class SnapshotSettingsViewModel : ObservableObject
    {
        private readonly Snapshot _snapshot;
        private readonly Action _saveAction;

        public SnapshotSettingsViewModel(Snapshot snapshot, Action saveAction)
        {
            _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            _saveAction = saveAction;
        }

        public bool ScreenshotUsingMagnificationAPI
        {
            get => _snapshot.ScreenshotUsingMagnificationAPI;
            set { if (SetProperty(_snapshot.ScreenshotUsingMagnificationAPI, value, _snapshot, (s, v) => s.ScreenshotUsingMagnificationAPI = v)) _saveAction?.Invoke(); }
        }

        public bool CopyScreenshotToClipboard
        {
            get => _snapshot.CopyScreenshotToClipboard;
            set { if (SetProperty(_snapshot.CopyScreenshotToClipboard, value, _snapshot, (s, v) => s.CopyScreenshotToClipboard = v)) _saveAction?.Invoke(); }
        }

        public bool HideMainWinWhenScreenshot
        {
            get => _snapshot.HideMainWinWhenScreenshot;
            set { if (SetProperty(_snapshot.HideMainWinWhenScreenshot, value, _snapshot, (s, v) => s.HideMainWinWhenScreenshot = v)) _saveAction?.Invoke(); }
        }

        public bool AttachInkWhenScreenshot
        {
            get => _snapshot.AttachInkWhenScreenshot;
            set { if (SetProperty(_snapshot.AttachInkWhenScreenshot, value, _snapshot, (s, v) => s.AttachInkWhenScreenshot = v)) _saveAction?.Invoke(); }
        }

        public bool OnlySnapshotMaximizeWindow
        {
            get => _snapshot.OnlySnapshotMaximizeWindow;
            set { if (SetProperty(_snapshot.OnlySnapshotMaximizeWindow, value, _snapshot, (s, v) => s.OnlySnapshotMaximizeWindow = v)) _saveAction?.Invoke(); }
        }

        public string ScreenshotFileName
        {
            get => _snapshot.ScreenshotFileName;
            set { if (SetProperty(_snapshot.ScreenshotFileName, value, _snapshot, (s, v) => s.ScreenshotFileName = v)) _saveAction?.Invoke(); }
        }
    }

    /// <summary>
    /// InkToShape 设置 ViewModel
    /// </summary>
    public partial class InkToShapeSettingsViewModel : ObservableObject
    {
        private readonly InkToShape _inkToShape;
        private readonly Action _saveAction;

        public InkToShapeSettingsViewModel(InkToShape inkToShape, Action saveAction)
        {
            _inkToShape = inkToShape ?? throw new ArgumentNullException(nameof(inkToShape));
            _saveAction = saveAction;
        }

        public bool IsInkToShapeEnabled
        {
            get => _inkToShape.IsInkToShapeEnabled;
            set { if (SetProperty(_inkToShape.IsInkToShapeEnabled, value, _inkToShape, (i, v) => i.IsInkToShapeEnabled = v)) _saveAction?.Invoke(); }
        }

        public bool IsInkToShapeNoFakePressureRectangle
        {
            get => _inkToShape.IsInkToShapeNoFakePressureRectangle;
            set { if (SetProperty(_inkToShape.IsInkToShapeNoFakePressureRectangle, value, _inkToShape, (i, v) => i.IsInkToShapeNoFakePressureRectangle = v)) _saveAction?.Invoke(); }
        }

        public bool IsInkToShapeNoFakePressureTriangle
        {
            get => _inkToShape.IsInkToShapeNoFakePressureTriangle;
            set { if (SetProperty(_inkToShape.IsInkToShapeNoFakePressureTriangle, value, _inkToShape, (i, v) => i.IsInkToShapeNoFakePressureTriangle = v)) _saveAction?.Invoke(); }
        }

        public bool IsInkToShapeTriangle
        {
            get => _inkToShape.IsInkToShapeTriangle;
            set { if (SetProperty(_inkToShape.IsInkToShapeTriangle, value, _inkToShape, (i, v) => i.IsInkToShapeTriangle = v)) _saveAction?.Invoke(); }
        }

        public bool IsInkToShapeRectangle
        {
            get => _inkToShape.IsInkToShapeRectangle;
            set { if (SetProperty(_inkToShape.IsInkToShapeRectangle, value, _inkToShape, (i, v) => i.IsInkToShapeRectangle = v)) _saveAction?.Invoke(); }
        }

        public bool IsInkToShapeRounded
        {
            get => _inkToShape.IsInkToShapeRounded;
            set { if (SetProperty(_inkToShape.IsInkToShapeRounded, value, _inkToShape, (i, v) => i.IsInkToShapeRounded = v)) _saveAction?.Invoke(); }
        }
    }

    /// <summary>
    /// PowerPoint 设置 ViewModel
    /// </summary>
    public partial class PowerPointSettingsViewModel : ObservableObject
    {
        private readonly PowerPointSettings _ppt;
        private readonly Action _saveAction;

        public PowerPointSettingsViewModel(PowerPointSettings ppt, Action saveAction)
        {
            _ppt = ppt ?? throw new ArgumentNullException(nameof(ppt));
            _saveAction = saveAction;
        }

        public bool ShowPPTButton
        {
            get => _ppt.ShowPPTButton;
            set { if (SetProperty(_ppt.ShowPPTButton, value, _ppt, (p, v) => p.ShowPPTButton = v)) _saveAction?.Invoke(); }
        }

        public int PPTButtonsDisplayOption
        {
            get => _ppt.PPTButtonsDisplayOption;
            set { if (SetProperty(_ppt.PPTButtonsDisplayOption, value, _ppt, (p, v) => p.PPTButtonsDisplayOption = v)) _saveAction?.Invoke(); }
        }

        public int PPTLSButtonPosition
        {
            get => _ppt.PPTLSButtonPosition;
            set { if (SetProperty(_ppt.PPTLSButtonPosition, value, _ppt, (p, v) => p.PPTLSButtonPosition = v)) _saveAction?.Invoke(); }
        }

        public int PPTRSButtonPosition
        {
            get => _ppt.PPTRSButtonPosition;
            set { if (SetProperty(_ppt.PPTRSButtonPosition, value, _ppt, (p, v) => p.PPTRSButtonPosition = v)) _saveAction?.Invoke(); }
        }

        public int PPTSButtonsOption
        {
            get => _ppt.PPTSButtonsOption;
            set { if (SetProperty(_ppt.PPTSButtonsOption, value, _ppt, (p, v) => p.PPTSButtonsOption = v)) _saveAction?.Invoke(); }
        }

        public int PPTBButtonsOption
        {
            get => _ppt.PPTBButtonsOption;
            set { if (SetProperty(_ppt.PPTBButtonsOption, value, _ppt, (p, v) => p.PPTBButtonsOption = v)) _saveAction?.Invoke(); }
        }

        public bool EnablePPTButtonPageClickable
        {
            get => _ppt.EnablePPTButtonPageClickable;
            set { if (SetProperty(_ppt.EnablePPTButtonPageClickable, value, _ppt, (p, v) => p.EnablePPTButtonPageClickable = v)) _saveAction?.Invoke(); }
        }

        public bool PowerPointSupport
        {
            get => _ppt.PowerPointSupport;
            set { if (SetProperty(_ppt.PowerPointSupport, value, _ppt, (p, v) => p.PowerPointSupport = v)) _saveAction?.Invoke(); }
        }

        public bool IsShowCanvasAtNewSlideShow
        {
            get => _ppt.IsShowCanvasAtNewSlideShow;
            set { if (SetProperty(_ppt.IsShowCanvasAtNewSlideShow, value, _ppt, (p, v) => p.IsShowCanvasAtNewSlideShow = v)) _saveAction?.Invoke(); }
        }

        public bool IsNoClearStrokeOnSelectWhenInPowerPoint
        {
            get => _ppt.IsNoClearStrokeOnSelectWhenInPowerPoint;
            set { if (SetProperty(_ppt.IsNoClearStrokeOnSelectWhenInPowerPoint, value, _ppt, (p, v) => p.IsNoClearStrokeOnSelectWhenInPowerPoint = v)) _saveAction?.Invoke(); }
        }

        public bool IsShowStrokeOnSelectInPowerPoint
        {
            get => _ppt.IsShowStrokeOnSelectInPowerPoint;
            set { if (SetProperty(_ppt.IsShowStrokeOnSelectInPowerPoint, value, _ppt, (p, v) => p.IsShowStrokeOnSelectInPowerPoint = v)) _saveAction?.Invoke(); }
        }

        public bool IsAutoSaveStrokesInPowerPoint
        {
            get => _ppt.IsAutoSaveStrokesInPowerPoint;
            set { if (SetProperty(_ppt.IsAutoSaveStrokesInPowerPoint, value, _ppt, (p, v) => p.IsAutoSaveStrokesInPowerPoint = v)) _saveAction?.Invoke(); }
        }

        public bool IsAutoSaveScreenShotInPowerPoint
        {
            get => _ppt.IsAutoSaveScreenShotInPowerPoint;
            set { if (SetProperty(_ppt.IsAutoSaveScreenShotInPowerPoint, value, _ppt, (p, v) => p.IsAutoSaveScreenShotInPowerPoint = v)) _saveAction?.Invoke(); }
        }

        public bool IsNotifyPreviousPage
        {
            get => _ppt.IsNotifyPreviousPage;
            set { if (SetProperty(_ppt.IsNotifyPreviousPage, value, _ppt, (p, v) => p.IsNotifyPreviousPage = v)) _saveAction?.Invoke(); }
        }

        public bool IsNotifyHiddenPage
        {
            get => _ppt.IsNotifyHiddenPage;
            set { if (SetProperty(_ppt.IsNotifyHiddenPage, value, _ppt, (p, v) => p.IsNotifyHiddenPage = v)) _saveAction?.Invoke(); }
        }

        public bool IsNotifyAutoPlayPresentation
        {
            get => _ppt.IsNotifyAutoPlayPresentation;
            set { if (SetProperty(_ppt.IsNotifyAutoPlayPresentation, value, _ppt, (p, v) => p.IsNotifyAutoPlayPresentation = v)) _saveAction?.Invoke(); }
        }

        public bool IsEnableTwoFingerGestureInPresentationMode
        {
            get => _ppt.IsEnableTwoFingerGestureInPresentationMode;
            set { if (SetProperty(_ppt.IsEnableTwoFingerGestureInPresentationMode, value, _ppt, (p, v) => p.IsEnableTwoFingerGestureInPresentationMode = v)) _saveAction?.Invoke(); }
        }

        public bool IsSupportWPS
        {
            get => _ppt.IsSupportWPS;
            set { if (SetProperty(_ppt.IsSupportWPS, value, _ppt, (p, v) => p.IsSupportWPS = v)) _saveAction?.Invoke(); }
        }

        public bool RegistryShowSlideShowToolbar
        {
            get => _ppt.RegistryShowSlideShowToolbar;
            set { if (SetProperty(_ppt.RegistryShowSlideShowToolbar, value, _ppt, (p, v) => p.RegistryShowSlideShowToolbar = v)) _saveAction?.Invoke(); }
        }

        public bool RegistryShowBlackScreenLastSlideShow
        {
            get => _ppt.RegistryShowBlackScreenLastSlideShow;
            set { if (SetProperty(_ppt.RegistryShowBlackScreenLastSlideShow, value, _ppt, (p, v) => p.RegistryShowBlackScreenLastSlideShow = v)) _saveAction?.Invoke(); }
        }
    }

    /// <summary>
    /// Automation 设置 ViewModel
    /// </summary>
    public partial class AutomationSettingsViewModel : ObservableObject
    {
        private readonly Automation _automation;
        private readonly Action _saveAction;

        public AutomationSettingsViewModel(Automation automation, Action saveAction)
        {
            _automation = automation ?? throw new ArgumentNullException(nameof(automation));
            _saveAction = saveAction;
        }

        public bool IsEnableAutoFold => _automation.IsEnableAutoFold;

        public bool IsAutoFoldInEasiNote
        {
            get => _automation.IsAutoFoldInEasiNote;
            set { if (SetProperty(_automation.IsAutoFoldInEasiNote, value, _automation, (a, v) => a.IsAutoFoldInEasiNote = v)) { _saveAction?.Invoke(); OnPropertyChanged(nameof(IsEnableAutoFold)); } }
        }

        public bool IsAutoFoldInEasiNoteIgnoreDesktopAnno
        {
            get => _automation.IsAutoFoldInEasiNoteIgnoreDesktopAnno;
            set { if (SetProperty(_automation.IsAutoFoldInEasiNoteIgnoreDesktopAnno, value, _automation, (a, v) => a.IsAutoFoldInEasiNoteIgnoreDesktopAnno = v)) _saveAction?.Invoke(); }
        }

        public bool IsAutoFoldInEasiCamera
        {
            get => _automation.IsAutoFoldInEasiCamera;
            set { if (SetProperty(_automation.IsAutoFoldInEasiCamera, value, _automation, (a, v) => a.IsAutoFoldInEasiCamera = v)) { _saveAction?.Invoke(); OnPropertyChanged(nameof(IsEnableAutoFold)); } }
        }

        public bool IsAutoFoldInEasiNote3
        {
            get => _automation.IsAutoFoldInEasiNote3;
            set { if (SetProperty(_automation.IsAutoFoldInEasiNote3, value, _automation, (a, v) => a.IsAutoFoldInEasiNote3 = v)) _saveAction?.Invoke(); }
        }

        public bool IsAutoFoldInEasiNote3C
        {
            get => _automation.IsAutoFoldInEasiNote3C;
            set { if (SetProperty(_automation.IsAutoFoldInEasiNote3C, value, _automation, (a, v) => a.IsAutoFoldInEasiNote3C = v)) { _saveAction?.Invoke(); OnPropertyChanged(nameof(IsEnableAutoFold)); } }
        }

        public bool IsAutoFoldInEasiNote5C
        {
            get => _automation.IsAutoFoldInEasiNote5C;
            set { if (SetProperty(_automation.IsAutoFoldInEasiNote5C, value, _automation, (a, v) => a.IsAutoFoldInEasiNote5C = v)) { _saveAction?.Invoke(); OnPropertyChanged(nameof(IsEnableAutoFold)); } }
        }

        public bool IsAutoFoldInSeewoPincoTeacher
        {
            get => _automation.IsAutoFoldInSeewoPincoTeacher;
            set { if (SetProperty(_automation.IsAutoFoldInSeewoPincoTeacher, value, _automation, (a, v) => a.IsAutoFoldInSeewoPincoTeacher = v)) { _saveAction?.Invoke(); OnPropertyChanged(nameof(IsEnableAutoFold)); } }
        }

        public bool IsAutoFoldInHiteTouchPro
        {
            get => _automation.IsAutoFoldInHiteTouchPro;
            set { if (SetProperty(_automation.IsAutoFoldInHiteTouchPro, value, _automation, (a, v) => a.IsAutoFoldInHiteTouchPro = v)) { _saveAction?.Invoke(); OnPropertyChanged(nameof(IsEnableAutoFold)); } }
        }

        public bool IsAutoFoldInHiteCamera
        {
            get => _automation.IsAutoFoldInHiteCamera;
            set { if (SetProperty(_automation.IsAutoFoldInHiteCamera, value, _automation, (a, v) => a.IsAutoFoldInHiteCamera = v)) { _saveAction?.Invoke(); OnPropertyChanged(nameof(IsEnableAutoFold)); } }
        }

        public bool IsAutoFoldInWxBoardMain
        {
            get => _automation.IsAutoFoldInWxBoardMain;
            set { if (SetProperty(_automation.IsAutoFoldInWxBoardMain, value, _automation, (a, v) => a.IsAutoFoldInWxBoardMain = v)) { _saveAction?.Invoke(); OnPropertyChanged(nameof(IsEnableAutoFold)); } }
        }

        public bool IsAutoFoldInOldZyBoard
        {
            get => _automation.IsAutoFoldInOldZyBoard;
            set { if (SetProperty(_automation.IsAutoFoldInOldZyBoard, value, _automation, (a, v) => a.IsAutoFoldInOldZyBoard = v)) { _saveAction?.Invoke(); OnPropertyChanged(nameof(IsEnableAutoFold)); } }
        }

        public bool IsAutoFoldInMSWhiteboard
        {
            get => _automation.IsAutoFoldInMSWhiteboard;
            set { if (SetProperty(_automation.IsAutoFoldInMSWhiteboard, value, _automation, (a, v) => a.IsAutoFoldInMSWhiteboard = v)) { _saveAction?.Invoke(); OnPropertyChanged(nameof(IsEnableAutoFold)); } }
        }

        public bool IsAutoFoldInPPTSlideShow
        {
            get => _automation.IsAutoFoldInPPTSlideShow;
            set { if (SetProperty(_automation.IsAutoFoldInPPTSlideShow, value, _automation, (a, v) => a.IsAutoFoldInPPTSlideShow = v)) { _saveAction?.Invoke(); OnPropertyChanged(nameof(IsEnableAutoFold)); } }
        }

        public bool IsAutoKillPptService
        {
            get => _automation.IsAutoKillPptService;
            set { if (SetProperty(_automation.IsAutoKillPptService, value, _automation, (a, v) => a.IsAutoKillPptService = v)) _saveAction?.Invoke(); }
        }

        public bool IsAutoKillEasiNote
        {
            get => _automation.IsAutoKillEasiNote;
            set { if (SetProperty(_automation.IsAutoKillEasiNote, value, _automation, (a, v) => a.IsAutoKillEasiNote = v)) _saveAction?.Invoke(); }
        }

        public bool IsSaveScreenshotsInDateFolders
        {
            get => _automation.IsSaveScreenshotsInDateFolders;
            set { if (SetProperty(_automation.IsSaveScreenshotsInDateFolders, value, _automation, (a, v) => a.IsSaveScreenshotsInDateFolders = v)) _saveAction?.Invoke(); }
        }

        public bool IsAutoSaveStrokesAtScreenshot
        {
            get => _automation.IsAutoSaveStrokesAtScreenshot;
            set { if (SetProperty(_automation.IsAutoSaveStrokesAtScreenshot, value, _automation, (a, v) => a.IsAutoSaveStrokesAtScreenshot = v)) _saveAction?.Invoke(); }
        }

        public bool IsAutoSaveStrokesAtClear
        {
            get => _automation.IsAutoSaveStrokesAtClear;
            set { if (SetProperty(_automation.IsAutoSaveStrokesAtClear, value, _automation, (a, v) => a.IsAutoSaveStrokesAtClear = v)) _saveAction?.Invoke(); }
        }

        public bool IsAutoClearWhenExitingWritingMode
        {
            get => _automation.IsAutoClearWhenExitingWritingMode;
            set { if (SetProperty(_automation.IsAutoClearWhenExitingWritingMode, value, _automation, (a, v) => a.IsAutoClearWhenExitingWritingMode = v)) _saveAction?.Invoke(); }
        }

        public int MinimumAutomationStrokeNumber
        {
            get => _automation.MinimumAutomationStrokeNumber;
            set { if (SetProperty(_automation.MinimumAutomationStrokeNumber, value, _automation, (a, v) => a.MinimumAutomationStrokeNumber = v)) _saveAction?.Invoke(); }
        }

        public string AutoSavedStrokesLocation
        {
            get => _automation.AutoSavedStrokesLocation;
            set { if (SetProperty(_automation.AutoSavedStrokesLocation, value, _automation, (a, v) => a.AutoSavedStrokesLocation = v)) _saveAction?.Invoke(); }
        }

        public bool AutoDelSavedFiles
        {
            get => _automation.AutoDelSavedFiles;
            set { if (SetProperty(_automation.AutoDelSavedFiles, value, _automation, (a, v) => a.AutoDelSavedFiles = v)) _saveAction?.Invoke(); }
        }

        public int AutoDelSavedFilesDaysThreshold
        {
            get => _automation.AutoDelSavedFilesDaysThreshold;
            set { if (SetProperty(_automation.AutoDelSavedFilesDaysThreshold, value, _automation, (a, v) => a.AutoDelSavedFilesDaysThreshold = v)) _saveAction?.Invoke(); }
        }

        public bool IsEnableLimitAutoSaveAmount
        {
            get => _automation.IsEnableLimitAutoSaveAmount;
            set { if (SetProperty(_automation.IsEnableLimitAutoSaveAmount, value, _automation, (a, v) => a.IsEnableLimitAutoSaveAmount = v)) _saveAction?.Invoke(); }
        }

        public int LimitAutoSaveAmount
        {
            get => _automation.LimitAutoSaveAmount;
            set { if (SetProperty(_automation.LimitAutoSaveAmount, value, _automation, (a, v) => a.LimitAutoSaveAmount = v)) _saveAction?.Invoke(); }
        }
    }
}