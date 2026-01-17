using System;
using System.IO;
using Newtonsoft.Json;

namespace Ink_Canvas.Models.Settings
{
    /// <summary>
    /// 自动化设置类
    /// </summary>
    public class AutomationSettings : SettingsBase
    {
        private bool _isAutoFoldInEasiNote = false;
        private bool _isAutoFoldInEasiNoteIgnoreDesktopAnno = false;
        private bool _isAutoFoldInEasiCamera = false;
        private bool _isAutoFoldInEasiNote3 = false;
        private bool _isAutoFoldInEasiNote3C = false;
        private bool _isAutoFoldInEasiNote5C = false;
        private bool _isAutoFoldInSeewoPincoTeacher = false;
        private bool _isAutoFoldInHiteTouchPro = false;
        private bool _isAutoFoldInHiteLightBoard = false;
        private bool _isAutoFoldInHiteCamera = false;
        private bool _isAutoFoldInWxBoardMain = false;
        private bool _isAutoFoldInOldZyBoard = false;
        private bool _isAutoFoldInMSWhiteboard = false;
        private bool _isAutoFoldInAdmoxWhiteboard = false;
        private bool _isAutoFoldInAdmoxBooth = false;
        private bool _isAutoFoldInQPoint = false;
        private bool _isAutoFoldInYiYunWhiteboard = false;
        private bool _isAutoFoldInYiYunVisualPresenter = false;
        private bool _isAutoFoldInMaxHubWhiteboard = false;
        private bool _isAutoFoldInPPTSlideShow = false;
        private bool _isAutoKillPptService = false;
        private bool _isAutoKillEasiNote = false;
        private bool _isAutoKillHiteAnnotation = false;
        private bool _isAutoKillVComYouJiao = false;
        private bool _isAutoKillSeewoLauncher2DesktopAnnotation = false;
        private bool _isAutoKillInkCanvas = false;
        private bool _isAutoKillICA = false;
        private bool _isAutoKillIDT = true;
        private bool _isSaveScreenshotsInDateFolders = false;
        private bool _isAutoSaveStrokesAtScreenshot = false;
        private bool _isAutoSaveStrokesAtClear = false;
        private bool _isAutoClearWhenExitingWritingMode = false;
        private int _minimumAutomationStrokeNumber = 0;
        private bool _isEnableLimitAutoSaveAmount = false;
        private int _limitAutoSaveAmount = 3;

        /// <summary>
        /// 是否启用自动折叠
        /// </summary>
        [JsonIgnore]
        public bool IsEnableAutoFold =>
            IsAutoFoldInEasiNote
            || IsAutoFoldInEasiCamera
            || IsAutoFoldInEasiNote3C
            || IsAutoFoldInEasiNote5C
            || IsAutoFoldInSeewoPincoTeacher
            || IsAutoFoldInHiteTouchPro
            || IsAutoFoldInHiteCamera
            || IsAutoFoldInWxBoardMain
            || IsAutoFoldInOldZyBoard
            || IsAutoFoldInPPTSlideShow
            || IsAutoFoldInMSWhiteboard
            || IsAutoFoldInAdmoxWhiteboard
            || IsAutoFoldInAdmoxBooth
            || IsAutoFoldInQPoint
            || IsAutoFoldInYiYunWhiteboard
            || IsAutoFoldInYiYunVisualPresenter
            || IsAutoFoldInMaxHubWhiteboard;

        [JsonProperty("isAutoFoldInEasiNote")]
        public bool IsAutoFoldInEasiNote
        {
            get => _isAutoFoldInEasiNote;
            set => SetProperty(ref _isAutoFoldInEasiNote, value);
        }

        [JsonProperty("isAutoFoldInEasiNoteIgnoreDesktopAnno")]
        public bool IsAutoFoldInEasiNoteIgnoreDesktopAnno
        {
            get => _isAutoFoldInEasiNoteIgnoreDesktopAnno;
            set => SetProperty(ref _isAutoFoldInEasiNoteIgnoreDesktopAnno, value);
        }

        [JsonProperty("isAutoFoldInEasiCamera")]
        public bool IsAutoFoldInEasiCamera
        {
            get => _isAutoFoldInEasiCamera;
            set => SetProperty(ref _isAutoFoldInEasiCamera, value);
        }

        [JsonProperty("isAutoFoldInEasiNote3")]
        public bool IsAutoFoldInEasiNote3
        {
            get => _isAutoFoldInEasiNote3;
            set => SetProperty(ref _isAutoFoldInEasiNote3, value);
        }

        [JsonProperty("isAutoFoldInEasiNote3C")]
        public bool IsAutoFoldInEasiNote3C
        {
            get => _isAutoFoldInEasiNote3C;
            set => SetProperty(ref _isAutoFoldInEasiNote3C, value);
        }

        [JsonProperty("isAutoFoldInEasiNote5C")]
        public bool IsAutoFoldInEasiNote5C
        {
            get => _isAutoFoldInEasiNote5C;
            set => SetProperty(ref _isAutoFoldInEasiNote5C, value);
        }

        [JsonProperty("isAutoFoldInSeewoPincoTeacher")]
        public bool IsAutoFoldInSeewoPincoTeacher
        {
            get => _isAutoFoldInSeewoPincoTeacher;
            set => SetProperty(ref _isAutoFoldInSeewoPincoTeacher, value);
        }

        [JsonProperty("isAutoFoldInHiteTouchPro")]
        public bool IsAutoFoldInHiteTouchPro
        {
            get => _isAutoFoldInHiteTouchPro;
            set => SetProperty(ref _isAutoFoldInHiteTouchPro, value);
        }

        [JsonProperty("isAutoFoldInHiteLightBoard")]
        public bool IsAutoFoldInHiteLightBoard
        {
            get => _isAutoFoldInHiteLightBoard;
            set => SetProperty(ref _isAutoFoldInHiteLightBoard, value);
        }

        [JsonProperty("isAutoFoldInHiteCamera")]
        public bool IsAutoFoldInHiteCamera
        {
            get => _isAutoFoldInHiteCamera;
            set => SetProperty(ref _isAutoFoldInHiteCamera, value);
        }

        [JsonProperty("isAutoFoldInWxBoardMain")]
        public bool IsAutoFoldInWxBoardMain
        {
            get => _isAutoFoldInWxBoardMain;
            set => SetProperty(ref _isAutoFoldInWxBoardMain, value);
        }

        [JsonProperty("isAutoFoldInOldZyBoard")]
        public bool IsAutoFoldInOldZyBoard
        {
            get => _isAutoFoldInOldZyBoard;
            set => SetProperty(ref _isAutoFoldInOldZyBoard, value);
        }

        [JsonProperty("isAutoFoldInMSWhiteboard")]
        public bool IsAutoFoldInMSWhiteboard
        {
            get => _isAutoFoldInMSWhiteboard;
            set => SetProperty(ref _isAutoFoldInMSWhiteboard, value);
        }

        [JsonProperty("isAutoFoldInAdmoxWhiteboard")]
        public bool IsAutoFoldInAdmoxWhiteboard
        {
            get => _isAutoFoldInAdmoxWhiteboard;
            set => SetProperty(ref _isAutoFoldInAdmoxWhiteboard, value);
        }

        [JsonProperty("isAutoFoldInAdmoxBooth")]
        public bool IsAutoFoldInAdmoxBooth
        {
            get => _isAutoFoldInAdmoxBooth;
            set => SetProperty(ref _isAutoFoldInAdmoxBooth, value);
        }

        [JsonProperty("isAutoFoldInQPoint")]
        public bool IsAutoFoldInQPoint
        {
            get => _isAutoFoldInQPoint;
            set => SetProperty(ref _isAutoFoldInQPoint, value);
        }

        [JsonProperty("isAutoFoldInYiYunWhiteboard")]
        public bool IsAutoFoldInYiYunWhiteboard
        {
            get => _isAutoFoldInYiYunWhiteboard;
            set => SetProperty(ref _isAutoFoldInYiYunWhiteboard, value);
        }

        [JsonProperty("isAutoFoldInYiYunVisualPresenter")]
        public bool IsAutoFoldInYiYunVisualPresenter
        {
            get => _isAutoFoldInYiYunVisualPresenter;
            set => SetProperty(ref _isAutoFoldInYiYunVisualPresenter, value);
        }

        [JsonProperty("isAutoFoldInMaxHubWhiteboard")]
        public bool IsAutoFoldInMaxHubWhiteboard
        {
            get => _isAutoFoldInMaxHubWhiteboard;
            set => SetProperty(ref _isAutoFoldInMaxHubWhiteboard, value);
        }

        [JsonProperty("isAutoFoldInPPTSlideShow")]
        public bool IsAutoFoldInPPTSlideShow
        {
            get => _isAutoFoldInPPTSlideShow;
            set => SetProperty(ref _isAutoFoldInPPTSlideShow, value);
        }

        [JsonProperty("isAutoKillPptService")]
        public bool IsAutoKillPptService
        {
            get => _isAutoKillPptService;
            set => SetProperty(ref _isAutoKillPptService, value);
        }

        [JsonProperty("isAutoKillEasiNote")]
        public bool IsAutoKillEasiNote
        {
            get => _isAutoKillEasiNote;
            set => SetProperty(ref _isAutoKillEasiNote, value);
        }

        [JsonProperty("isAutoKillHiteAnnotation")]
        public bool IsAutoKillHiteAnnotation
        {
            get => _isAutoKillHiteAnnotation;
            set => SetProperty(ref _isAutoKillHiteAnnotation, value);
        }

        [JsonProperty("isAutoKillVComYouJiao")]
        public bool IsAutoKillVComYouJiao
        {
            get => _isAutoKillVComYouJiao;
            set => SetProperty(ref _isAutoKillVComYouJiao, value);
        }

        [JsonProperty("isAutoKillSeewoLauncher2DesktopAnnotation")]
        public bool IsAutoKillSeewoLauncher2DesktopAnnotation
        {
            get => _isAutoKillSeewoLauncher2DesktopAnnotation;
            set => SetProperty(ref _isAutoKillSeewoLauncher2DesktopAnnotation, value);
        }

        [JsonProperty("isAutoKillInkCanvas")]
        public bool IsAutoKillInkCanvas
        {
            get => _isAutoKillInkCanvas;
            set => SetProperty(ref _isAutoKillInkCanvas, value);
        }

        [JsonProperty("isAutoKillICA")]
        public bool IsAutoKillICA
        {
            get => _isAutoKillICA;
            set => SetProperty(ref _isAutoKillICA, value);
        }

        [JsonProperty("isAutoKillIDT")]
        public bool IsAutoKillIDT
        {
            get => _isAutoKillIDT;
            set => SetProperty(ref _isAutoKillIDT, value);
        }

        [JsonProperty("isSaveScreenshotsInDateFolders")]
        public bool IsSaveScreenshotsInDateFolders
        {
            get => _isSaveScreenshotsInDateFolders;
            set => SetProperty(ref _isSaveScreenshotsInDateFolders, value);
        }

        [JsonProperty("isAutoSaveStrokesAtScreenshot")]
        public bool IsAutoSaveStrokesAtScreenshot
        {
            get => _isAutoSaveStrokesAtScreenshot;
            set => SetProperty(ref _isAutoSaveStrokesAtScreenshot, value);
        }

        [JsonProperty("isAutoSaveStrokesAtClear")]
        public bool IsAutoSaveStrokesAtClear
        {
            get => _isAutoSaveStrokesAtClear;
            set => SetProperty(ref _isAutoSaveStrokesAtClear, value);
        }

        [JsonProperty("isAutoClearWhenExitingWritingMode")]
        public bool IsAutoClearWhenExitingWritingMode
        {
            get => _isAutoClearWhenExitingWritingMode;
            set => SetProperty(ref _isAutoClearWhenExitingWritingMode, value);
        }

        [JsonProperty("minimumAutomationStrokeNumber")]
        public int MinimumAutomationStrokeNumber
        {
            get => _minimumAutomationStrokeNumber;
            set => SetProperty(ref _minimumAutomationStrokeNumber, value);
        }

        /// <summary>
        /// 自动保存笔画位置
        /// </summary>
        [JsonProperty("autoSavedStrokesLocation")]
        public string AutoSavedStrokesLocation { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

        /// <summary>
        /// 是否自动删除保存的文件
        /// </summary>
        [JsonProperty("autoDelSavedFiles")]
        public bool AutoDelSavedFiles { get; set; } = false;

        /// <summary>
        /// 自动删除保存文件的天数阈值
        /// </summary>
        [JsonProperty("autoDelSavedFilesDaysThreshold")]
        public int AutoDelSavedFilesDaysThreshold { get; set; } = 15;

        [JsonProperty("isEnableLimitAutoSaveAmount")]
        public bool IsEnableLimitAutoSaveAmount
        {
            get => _isEnableLimitAutoSaveAmount;
            set => SetProperty(ref _isEnableLimitAutoSaveAmount, value);
        }

        [JsonProperty("limitAutoSaveAmount")]
        public int LimitAutoSaveAmount
        {
            get => _limitAutoSaveAmount;
            set => SetProperty(ref _limitAutoSaveAmount, value);
        }
    }
}
