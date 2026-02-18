using Ink_Canvas.Core;
using Ink_Canvas.Helpers;
using Ink_Canvas.Services;
using Ink_Canvas.ViewModels;
using Ink_Canvas.Services.Events;
using Ink_Canvas.Models.Settings;
using OSVersionExtension;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Threading;
using System.Windows.Threading;
using System.Windows.Interop;
using Ink_Canvas.Popups;

namespace Ink_Canvas {
    public partial class MainWindow {
        
        /// <summary>
        /// 处理设置变更事件
        /// </summary>
        private void OnSettingChanged(object sender, SettingChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    switch (e.CategoryName)
                    {
                        case "Canvas":
                            HandleCanvasSettingChanged(e.PropertyName);
                            break;
                        case "PowerPoint":
                            HandlePowerPointSettingChanged(e.PropertyName);
                            break;
                        case "Automation":
                            HandleAutomationSettingChanged(e.PropertyName);
                            break;
                        case "Gesture":
                            HandleGestureSettingChanged(e.PropertyName);
                            break;
                        case "Advanced":
                            HandleAdvancedSettingChanged(e.PropertyName);
                            break;
                        case "Startup":
                            HandleStartupSettingChanged(e.PropertyName);
                            break;
                        case "InkToShape":
                            HandleInkToShapeSettingChanged(e.PropertyName);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"Error handling setting change: {ex.Message}", LogHelper.LogType.Error);
                }
            });
        }

        private void HandleCanvasSettingChanged(string propertyName)
        {
            switch (propertyName)
            {
                case nameof(CanvasSettings.IsShowCursor):
                    inkCanvas_EditingModeChanged(inkCanvas, null);
                    break;
                case nameof(CanvasSettings.EraserSize):
                    // Update eraser size logic
                    double width = 24;
                    switch (Settings.Canvas.EraserSize)
                    {
                        case 0: width = 24; break;
                        case 1: width = 38; break;
                        case 2: width = 46; break;
                        case 3: width = 62; break;
                        case 4: width = 78; break;
                    }
                    eraserWidth = width;
                    isEraserCircleShape = Settings.Canvas.EraserShapeType == 0;
                    break;
                case nameof(CanvasSettings.EraserShapeType):
                    CheckEraserTypeTab();
                    isEraserCircleShape = Settings.Canvas.EraserShapeType == 0;
                    break;
                case nameof(CanvasSettings.InkWidth):
                    if (drawingAttributes != null)
                    {
                        drawingAttributes.Height = Settings.Canvas.InkWidth;
                        drawingAttributes.Width = Settings.Canvas.InkWidth;
                    }
                    break;
                case nameof(CanvasSettings.HighlighterWidth):
                    if (drawingAttributes != null)
                    {
                        // Note: Logic from HighlighterWidthSlider_ValueChanged
                        // drawingAttributes.Height = Settings.Canvas.HighlighterWidth;
                        // drawingAttributes.Width = Settings.Canvas.HighlighterWidth / 2;
                        // But we need to check current tool mode.
                        // For now, just update if it's highlighter mode or generic update.
                    }
                    break;
                case nameof(CanvasSettings.FitToCurve):
                    if (drawingAttributes != null)
                        drawingAttributes.FitToCurve = Settings.Canvas.FitToCurve;
                    break;
                case nameof(CanvasSettings.ApplyScaleToStylusTip):
                    SelectionV2.ApplyScaleToStylusTip = Settings.Canvas.ApplyScaleToStylusTip;
                    break;
                case nameof(CanvasSettings.OnlyHitTestFullyContainedStrokes):
                    SelectionV2.OnlyHitTestFullyContainedStrokes = Settings.Canvas.OnlyHitTestFullyContainedStrokes;
                    break;
                case nameof(CanvasSettings.AllowClickToSelectLockedStroke):
                    SelectionV2.AllowClickToSelectLockedStroke = Settings.Canvas.AllowClickToSelectLockedStroke;
                    break;
                case nameof(CanvasSettings.SelectionMethod):
                    SelectionV2.SelectionModeSelected = (SelectionPopup.SelectionMode)Settings.Canvas.SelectionMethod;
                    break;
            }
        }

        private void HandlePowerPointSettingChanged(string propertyName)
        {
            switch (propertyName)
            {
                case nameof(PowerPointSettings.PowerPointSupport):
                    if (Settings.PowerPointSettings.PowerPointSupport)
                        timerCheckPPT.Start();
                    else
                        timerCheckPPT.Stop();
                    break;
                case nameof(PowerPointSettings.ShowPPTButton):
                case nameof(PowerPointSettings.PPTButtonsDisplayOption):
                case nameof(PowerPointSettings.PPTLSButtonPosition):
                case nameof(PowerPointSettings.PPTRSButtonPosition):
                    if (BorderFloatingBarExitPPTBtn.Visibility == Visibility.Visible) UpdatePPTBtnDisplaySettingsStatus();
                    break;
                case nameof(PowerPointSettings.PPTSButtonsOption):
                case nameof(PowerPointSettings.PPTBButtonsOption):
                    if (BorderFloatingBarExitPPTBtn.Visibility == Visibility.Visible) UpdatePPTBtnStyleSettingsStatus();
                    break;
            }
        }

        private void HandleAutomationSettingChanged(string propertyName)
        {
            // Check if it's an auto-fold property
            if (propertyName.StartsWith("IsAutoFold"))
            {
                // StartOrStoptimerCheckAutoFold(); // Removed
            }
            // Check if it's an auto-kill property
            else if (propertyName.StartsWith("IsAutoKill"))
            {
                if (Settings.Automation.IsAutoKillEasiNote || Settings.Automation.IsAutoKillPptService ||
                    Settings.Automation.IsAutoKillHiteAnnotation || Settings.Automation.IsAutoKillInkCanvas
                    || Settings.Automation.IsAutoKillICA || Settings.Automation.IsAutoKillIDT || Settings.Automation.IsAutoKillVComYouJiao
                    || Settings.Automation.IsAutoKillSeewoLauncher2DesktopAnnotation)
                    timerKillProcess.Start();
                else
                    timerKillProcess.Stop();
            }
        }

        private void HandleGestureSettingChanged(string propertyName)
        {
            switch (propertyName)
            {
                case nameof(GestureSettings.IsEnableTwoFingerZoom):
                case nameof(GestureSettings.IsEnableTwoFingerTranslate):
                case nameof(GestureSettings.IsEnableTwoFingerRotation):
                    CheckEnableTwoFingerGestureBtnColorPrompt();
                    break;
                case nameof(GestureSettings.IsEnableMultiTouchMode):
                    if (Settings.Gesture.IsEnableMultiTouchMode) {
                        if (!isInMultiTouchMode) {
                            inkCanvas.StylusDown += MainWindow_StylusDown;
                            inkCanvas.StylusMove += MainWindow_StylusMove;
                            inkCanvas.StylusUp += MainWindow_StylusUp;
                            inkCanvas.TouchDown += MainWindow_TouchDown;
                            inkCanvas.TouchDown -= Main_Grid_TouchDown;
                            inkCanvas.EditingMode = InkCanvasEditingMode.None;
                            inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
                            inkCanvas.Children.Clear();
                            isInMultiTouchMode = true;
                        }
                    } else {
                        if (isInMultiTouchMode) {
                            inkCanvas.StylusDown -= MainWindow_StylusDown;
                            inkCanvas.StylusMove -= MainWindow_StylusMove;
                            inkCanvas.StylusUp -= MainWindow_StylusUp;
                            inkCanvas.TouchDown -= MainWindow_TouchDown;
                            inkCanvas.TouchDown += Main_Grid_TouchDown;
                            inkCanvas.EditingMode = InkCanvasEditingMode.None;
                            inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
                            inkCanvas.Children.Clear();
                            isInMultiTouchMode = false;
                        }
                    }
                    CheckEnableTwoFingerGestureBtnColorPrompt();
                    break;
                case nameof(GestureSettings.DisableGestureEraser):
                    break;
            }
        }

        private void HandleAdvancedSettingChanged(string propertyName)
        {
            switch (propertyName)
            {
                case nameof(AdvancedSettings.IsSpecialScreen):
                    break;
                case nameof(AdvancedSettings.IsEnableEdgeGestureUtil):
                    if (OSVersion.GetOperatingSystem() >= OSVersionExtension.OperatingSystem.Windows10)
                        EdgeGestureUtil.DisableEdgeGestures(new WindowInteropHelper(this).Handle, Settings.Advanced.IsEnableEdgeGestureUtil);
                    break;
                case nameof(AdvancedSettings.IsEnableForceFullScreen):
                    if (Settings.Advanced.IsEnableForceFullScreen)
                    {
                        MainWindow_OnSizeChanged(this, null);
                    }
                    break;
            }
        }

        private void HandleStartupSettingChanged(string propertyName)
        {
            switch (propertyName)
            {
                case nameof(StartupSettings.IsEnableNibMode):
                    if (Settings.Startup.IsEnableNibMode)
                        BoundsWidth = Settings.Advanced.NibModeBoundsWidth;
                    else
                        BoundsWidth = Settings.Advanced.FingerModeBoundsWidth;
                    break;
                case nameof(StartupSettings.IsAutoUpdate):
                    break;
                case nameof(StartupSettings.IsAutoUpdateWithSilence):
                    break;
            }
        }

        private void HandleInkToShapeSettingChanged(string propertyName)
        {
            switch (propertyName)
            {
                case nameof(InkToShapeSettings.IsInkToShapeEnabled):
                    PenPaletteV2.InkRecognition = Settings.InkToShape.IsInkToShapeEnabled;
                    break;
            }
        }
    }
}
