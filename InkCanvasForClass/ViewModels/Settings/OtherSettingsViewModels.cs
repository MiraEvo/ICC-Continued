using CommunityToolkit.Mvvm.ComponentModel;
using System;
using Ink_Canvas.Models.Settings;

namespace Ink_Canvas.ViewModels
{
    /// <summary>
    /// Gesture 设置 ViewModel
    /// </summary>
    public partial class GestureSettingsViewModel : ObservableObject
    {
        private readonly GestureSettings _gesture;
        private readonly Action _saveAction;

        public GestureSettingsViewModel(GestureSettings gesture, Action saveAction)
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

        public int HideCursorMode
        {
            get => _gesture.HideCursorMode;
            set { if (SetProperty(_gesture.HideCursorMode, value, _gesture, (g, v) => g.HideCursorMode = v)) _saveAction?.Invoke(); }
        }

        public int MouseWheelAction
        {
            get => _gesture.MouseWheelAction;
            set { if (SetProperty(_gesture.MouseWheelAction, value, _gesture, (g, v) => g.MouseWheelAction = v)) _saveAction?.Invoke(); }
        }

        public int MouseWheelDirection
        {
            get => _gesture.MouseWheelDirection;
            set { if (SetProperty(_gesture.MouseWheelDirection, value, _gesture, (g, v) => g.MouseWheelDirection = v)) _saveAction?.Invoke(); }
        }

        public int PalmEraserDetectionThreshold
        {
            get => _gesture.PalmEraserDetectionThreshold;
            set { if (SetProperty(_gesture.PalmEraserDetectionThreshold, value, _gesture, (g, v) => g.PalmEraserDetectionThreshold = v)) _saveAction?.Invoke(); }
        }

        public int PalmEraserMinIntervalMs
        {
            get => _gesture.PalmEraserMinIntervalMs;
            set { if (SetProperty(_gesture.PalmEraserMinIntervalMs, value, _gesture, (g, v) => g.PalmEraserMinIntervalMs = v)) _saveAction?.Invoke(); }
        }

        public bool PalmEraserDetectOnMove
        {
            get => _gesture.PalmEraserDetectOnMove;
            set { if (SetProperty(_gesture.PalmEraserDetectOnMove, value, _gesture, (g, v) => g.PalmEraserDetectOnMove = v)) _saveAction?.Invoke(); }
        }

        // 现代化手掌橡皮擦设置
        public bool UseAdaptiveThreshold
        {
            get => _gesture.UseAdaptiveThreshold;
            set { if (SetProperty(_gesture.UseAdaptiveThreshold, value, _gesture, (g, v) => g.UseAdaptiveThreshold = v)) _saveAction?.Invoke(); }
        }

        public bool UsePredictiveErasing
        {
            get => _gesture.UsePredictiveErasing;
            set { if (SetProperty(_gesture.UsePredictiveErasing, value, _gesture, (g, v) => g.UsePredictiveErasing = v)) _saveAction?.Invoke(); }
        }

        public double PalmProbabilityThreshold
        {
            get => _gesture.PalmProbabilityThreshold;
            set { if (SetProperty(_gesture.PalmProbabilityThreshold, value, _gesture, (g, v) => g.PalmProbabilityThreshold = v)) _saveAction?.Invoke(); }
        }

        public int TouchHistorySize
        {
            get => _gesture.TouchHistorySize;
            set { if (SetProperty(_gesture.TouchHistorySize, value, _gesture, (g, v) => g.TouchHistorySize = v)) _saveAction?.Invoke(); }
        }

        public bool EnableHapticFeedback
        {
            get => _gesture.EnableHapticFeedback;
            set { if (SetProperty(_gesture.EnableHapticFeedback, value, _gesture, (g, v) => g.EnableHapticFeedback = v)) _saveAction?.Invoke(); }
        }

        public double PalmEraserVelocityThreshold
        {
            get => _gesture.PalmEraserVelocityThreshold;
            set { if (SetProperty(_gesture.PalmEraserVelocityThreshold, value, _gesture, (g, v) => g.PalmEraserVelocityThreshold = v)) _saveAction?.Invoke(); }
        }
    }

    /// <summary>
    /// Startup 设置 ViewModel
    /// </summary>
    public partial class StartupSettingsViewModel : ObservableObject
    {
        private readonly StartupSettings _startup;
        private readonly Action _saveAction;

        public StartupSettingsViewModel(StartupSettings startup, Action saveAction)
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
    /// Snapshot 设置 ViewModel
    /// </summary>
    public partial class SnapshotSettingsViewModel : ObservableObject
    {
        private readonly SnapshotSettings _snapshot;
        private readonly Action _saveAction;

        public SnapshotSettingsViewModel(SnapshotSettings snapshot, Action saveAction)
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

        public void ResetScreenshotFileName()
        {
            _snapshot.ResetToDefaultFileName();
            OnPropertyChanged(nameof(ScreenshotFileName));
            _saveAction?.Invoke();
        }

        public string FormattedFileName => _snapshot.FormattedFileName;
        public bool HasValidPlaceholders => _snapshot.HasValidPlaceholders;
        public string FileExtension => _snapshot.FileExtension;
    }

    /// <summary>
    /// InkToShape 设置 ViewModel
    /// </summary>
    public partial class InkToShapeSettingsViewModel : ObservableObject
    {
        private readonly InkToShapeSettings _inkToShape;
        private readonly Action _saveAction;

        public InkToShapeSettingsViewModel(InkToShapeSettings inkToShape, Action saveAction)
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

        public bool TreatRecognizedInkAsShape
        {
            get => _inkToShape.TreatRecognizedInkAsShape;
            set { if (SetProperty(_inkToShape.TreatRecognizedInkAsShape, value, _inkToShape, (i, v) => i.TreatRecognizedInkAsShape = v)) _saveAction?.Invoke(); }
        }

        public bool EnableDrawingToolbar
        {
            get => _inkToShape.EnableDrawingToolbar;
            set { if (SetProperty(_inkToShape.EnableDrawingToolbar, value, _inkToShape, (i, v) => i.EnableDrawingToolbar = v)) _saveAction?.Invoke(); }
        }

        public bool ExpandShapeVariantsByDefault
        {
            get => _inkToShape.ExpandShapeVariantsByDefault;
            set { if (SetProperty(_inkToShape.ExpandShapeVariantsByDefault, value, _inkToShape, (i, v) => i.ExpandShapeVariantsByDefault = v)) _saveAction?.Invoke(); }
        }

        public double ConfidenceThreshold
        {
            get => _inkToShape.ConfidenceThreshold;
            set { if (SetProperty(_inkToShape.ConfidenceThreshold, value, _inkToShape, (i, v) => i.ConfidenceThreshold = v)) _saveAction?.Invoke(); }
        }

        public double MinimumShapeSize
        {
            get => _inkToShape.MinimumShapeSize;
            set { if (SetProperty(_inkToShape.MinimumShapeSize, value, _inkToShape, (i, v) => i.MinimumShapeSize = v)) _saveAction?.Invoke(); }
        }

        public bool EnablePolygonRecognition
        {
            get => _inkToShape.EnablePolygonRecognition;
            set { if (SetProperty(_inkToShape.EnablePolygonRecognition, value, _inkToShape, (i, v) => i.EnablePolygonRecognition = v)) _saveAction?.Invoke(); }
        }

        public bool EnableShapeSmoothing
        {
            get => _inkToShape.EnableShapeSmoothing;
            set { if (SetProperty(_inkToShape.EnableShapeSmoothing, value, _inkToShape, (i, v) => i.EnableShapeSmoothing = v)) _saveAction?.Invoke(); }
        }

        public int ResamplePointCount
        {
            get => _inkToShape.ResamplePointCount;
            set { if (SetProperty(_inkToShape.ResamplePointCount, value, _inkToShape, (i, v) => i.ResamplePointCount = v)) _saveAction?.Invoke(); }
        }

        public bool EnableAdaptiveResampling
        {
            get => _inkToShape.EnableAdaptiveResampling;
            set { if (SetProperty(_inkToShape.EnableAdaptiveResampling, value, _inkToShape, (i, v) => i.EnableAdaptiveResampling = v)) _saveAction?.Invoke(); }
        }

        public double GeometryValidationStrength
        {
            get => _inkToShape.GeometryValidationStrength;
            set { if (SetProperty(_inkToShape.GeometryValidationStrength, value, _inkToShape, (i, v) => i.GeometryValidationStrength = v)) _saveAction?.Invoke(); }
        }

        public bool IsAnyShapeRecognitionEnabled => _inkToShape.IsAnyShapeRecognitionEnabled;
        public bool IsAdvancedRecognitionEnabled => _inkToShape.IsAdvancedRecognitionEnabled;
        public int EnabledShapeTypeCount => _inkToShape.EnabledShapeTypeCount;
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

        public bool RegistryDisableSideToolbar
        {
            get => _ppt.RegistryDisableSideToolbar;
            set { if (SetProperty(_ppt.RegistryDisableSideToolbar, value, _ppt, (p, v) => p.RegistryDisableSideToolbar = v)) _saveAction?.Invoke(); }
        }

        public bool IsAutoEnterAnnotationMode
        {
            get => _ppt.IsAutoEnterAnnotationMode;
            set { if (SetProperty(_ppt.IsAutoEnterAnnotationMode, value, _ppt, (p, v) => p.IsAutoEnterAnnotationMode = v)) _saveAction?.Invoke(); }
        }

        public bool IsRememberLastPlaybackPosition
        {
            get => _ppt.IsRememberLastPlaybackPosition;
            set { if (SetProperty(_ppt.IsRememberLastPlaybackPosition, value, _ppt, (p, v) => p.IsRememberLastPlaybackPosition = v)) _saveAction?.Invoke(); }
        }

        // Helper methods for bitmask options

        private void UpdateOption(int optionValue, int index, bool value, Action<int> setter)
        {
            var str = optionValue.ToString();
            // Ensure string is long enough (pad with '1's if needed, though default values should prevent this)
            if (str.Length <= index) str = str.PadRight(index + 1, '1');

            char[] c = str.ToCharArray();
            c[index] = value ? '2' : '1';
            int newValue = int.Parse(new string(c));

            if (optionValue != newValue)
            {
                setter(newValue);
                _saveAction?.Invoke();
                OnPropertyChanged(string.Empty); // Refresh all properties as they might depend on this
            }
        }

        private bool GetOption(int optionValue, int index)
        {
            var str = optionValue.ToString();
            if (str.Length <= index) return false;
            return str[index] == '2';
        }

        // PPTButtonsDisplayOption Helpers (LeftBottom, RightBottom, LeftSide, RightSide)

        public bool ShowPPTButtonLeftBottom
        {
            get => GetOption(PPTButtonsDisplayOption, 0);
            set => UpdateOption(_ppt.PPTButtonsDisplayOption, 0, value, v => PPTButtonsDisplayOption = v);
        }

        public bool ShowPPTButtonRightBottom
        {
            get => GetOption(PPTButtonsDisplayOption, 1);
            set => UpdateOption(_ppt.PPTButtonsDisplayOption, 1, value, v => PPTButtonsDisplayOption = v);
        }

        public bool ShowPPTButtonLeftSide
        {
            get => GetOption(PPTButtonsDisplayOption, 2);
            set => UpdateOption(_ppt.PPTButtonsDisplayOption, 2, value, v => PPTButtonsDisplayOption = v);
        }

        public bool ShowPPTButtonRightSide
        {
            get => GetOption(PPTButtonsDisplayOption, 3);
            set => UpdateOption(_ppt.PPTButtonsDisplayOption, 3, value, v => PPTButtonsDisplayOption = v);
        }

        // PPTSButtonsOption Helpers (Side Buttons: ShowPage, HalfOpacity, BlackBackground)

        public bool SideButtonShowPage
        {
            get => GetOption(PPTSButtonsOption, 0);
            set => UpdateOption(_ppt.PPTSButtonsOption, 0, value, v => PPTSButtonsOption = v);
        }

        public bool SideButtonHalfOpacity
        {
            get => GetOption(PPTSButtonsOption, 1);
            set => UpdateOption(_ppt.PPTSButtonsOption, 1, value, v => PPTSButtonsOption = v);
        }

        public bool SideButtonBlackBackground
        {
            get => GetOption(PPTSButtonsOption, 2);
            set => UpdateOption(_ppt.PPTSButtonsOption, 2, value, v => PPTSButtonsOption = v);
        }

        // PPTBButtonsOption Helpers (Bottom Buttons: ShowPage, HalfOpacity, BlackBackground)

        public bool BottomButtonShowPage
        {
            get => GetOption(PPTBButtonsOption, 0);
            set => UpdateOption(_ppt.PPTBButtonsOption, 0, value, v => PPTBButtonsOption = v);
        }

        public bool BottomButtonHalfOpacity
        {
            get => GetOption(PPTBButtonsOption, 1);
            set => UpdateOption(_ppt.PPTBButtonsOption, 1, value, v => PPTBButtonsOption = v);
        }

        public bool BottomButtonBlackBackground
        {
            get => GetOption(PPTBButtonsOption, 2);
            set => UpdateOption(_ppt.PPTBButtonsOption, 2, value, v => PPTBButtonsOption = v);
        }
    }

    /// <summary>
    /// Automation 设置 ViewModel
    /// </summary>
    public partial class AutomationSettingsViewModel : ObservableObject
    {
        private readonly AutomationSettings _automation;
        private readonly Action _saveAction;

        public AutomationSettingsViewModel(AutomationSettings automation, Action saveAction)
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

        public bool IsAutoFoldInHiteLightBoard
        {
            get => _automation.IsAutoFoldInHiteLightBoard;
            set { if (SetProperty(_automation.IsAutoFoldInHiteLightBoard, value, _automation, (a, v) => a.IsAutoFoldInHiteLightBoard = v)) { _saveAction?.Invoke(); OnPropertyChanged(nameof(IsEnableAutoFold)); } }
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

        public bool IsAutoFoldInAdmoxWhiteboard
        {
            get => _automation.IsAutoFoldInAdmoxWhiteboard;
            set { if (SetProperty(_automation.IsAutoFoldInAdmoxWhiteboard, value, _automation, (a, v) => a.IsAutoFoldInAdmoxWhiteboard = v)) { _saveAction?.Invoke(); OnPropertyChanged(nameof(IsEnableAutoFold)); } }
        }

        public bool IsAutoFoldInAdmoxBooth
        {
            get => _automation.IsAutoFoldInAdmoxBooth;
            set { if (SetProperty(_automation.IsAutoFoldInAdmoxBooth, value, _automation, (a, v) => a.IsAutoFoldInAdmoxBooth = v)) { _saveAction?.Invoke(); OnPropertyChanged(nameof(IsEnableAutoFold)); } }
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

        public bool IsAutoKillHiteAnnotation
        {
            get => _automation.IsAutoKillHiteAnnotation;
            set { if (SetProperty(_automation.IsAutoKillHiteAnnotation, value, _automation, (a, v) => a.IsAutoKillHiteAnnotation = v)) _saveAction?.Invoke(); }
        }

        public bool IsAutoKillVComYouJiao
        {
            get => _automation.IsAutoKillVComYouJiao;
            set { if (SetProperty(_automation.IsAutoKillVComYouJiao, value, _automation, (a, v) => a.IsAutoKillVComYouJiao = v)) _saveAction?.Invoke(); }
        }

        public bool IsAutoKillSeewoLauncher2DesktopAnnotation
        {
            get => _automation.IsAutoKillSeewoLauncher2DesktopAnnotation;
            set { if (SetProperty(_automation.IsAutoKillSeewoLauncher2DesktopAnnotation, value, _automation, (a, v) => a.IsAutoKillSeewoLauncher2DesktopAnnotation = v)) _saveAction?.Invoke(); }
        }

        public bool IsAutoKillInkCanvas
        {
            get => _automation.IsAutoKillInkCanvas;
            set { if (SetProperty(_automation.IsAutoKillInkCanvas, value, _automation, (a, v) => a.IsAutoKillInkCanvas = v)) _saveAction?.Invoke(); }
        }

        public bool IsAutoKillICA
        {
            get => _automation.IsAutoKillICA;
            set { if (SetProperty(_automation.IsAutoKillICA, value, _automation, (a, v) => a.IsAutoKillICA = v)) _saveAction?.Invoke(); }
        }

        public bool IsAutoKillIDT
        {
            get => _automation.IsAutoKillIDT;
            set { if (SetProperty(_automation.IsAutoKillIDT, value, _automation, (a, v) => a.IsAutoKillIDT = v)) _saveAction?.Invoke(); }
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

        public bool IsAutoFoldInQPoint
        {
            get => _automation.IsAutoFoldInQPoint;
            set { if (SetProperty(_automation.IsAutoFoldInQPoint, value, _automation, (a, v) => a.IsAutoFoldInQPoint = v)) { _saveAction?.Invoke(); OnPropertyChanged(nameof(IsEnableAutoFold)); } }
        }

        public bool IsAutoFoldInYiYunWhiteboard
        {
            get => _automation.IsAutoFoldInYiYunWhiteboard;
            set { if (SetProperty(_automation.IsAutoFoldInYiYunWhiteboard, value, _automation, (a, v) => a.IsAutoFoldInYiYunWhiteboard = v)) { _saveAction?.Invoke(); OnPropertyChanged(nameof(IsEnableAutoFold)); } }
        }

        public bool IsAutoFoldInYiYunVisualPresenter
        {
            get => _automation.IsAutoFoldInYiYunVisualPresenter;
            set { if (SetProperty(_automation.IsAutoFoldInYiYunVisualPresenter, value, _automation, (a, v) => a.IsAutoFoldInYiYunVisualPresenter = v)) { _saveAction?.Invoke(); OnPropertyChanged(nameof(IsEnableAutoFold)); } }
        }

        public bool IsAutoFoldInMaxHubWhiteboard
        {
            get => _automation.IsAutoFoldInMaxHubWhiteboard;
            set { if (SetProperty(_automation.IsAutoFoldInMaxHubWhiteboard, value, _automation, (a, v) => a.IsAutoFoldInMaxHubWhiteboard = v)) { _saveAction?.Invoke(); OnPropertyChanged(nameof(IsEnableAutoFold)); } }
        }
    }
}
