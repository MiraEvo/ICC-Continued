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
        #region 常量

        /// <summary>
        /// 默认自动保存数量限制
        /// </summary>
        public const int DefaultLimitAutoSaveAmount = 3;

        /// <summary>
        /// 最小自动保存数量限制
        /// </summary>
        public const int MinLimitAutoSaveAmount = 1;

        /// <summary>
        /// 最大自动保存数量限制
        /// </summary>
        public const int MaxLimitAutoSaveAmount = 100;

        /// <summary>
        /// 最小自动删除天数阈值
        /// </summary>
        public const int MinAutoDelDaysThreshold = 1;

        /// <summary>
        /// 最大自动删除天数阈值
        /// </summary>
        public const int MaxAutoDelDaysThreshold = 365;

        /// <summary>
        /// 默认自动保存笔画位置
        /// </summary>
        public static readonly string DefaultAutoSavedStrokesLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

        #endregion

        #region 字段

        // 自动折叠设置
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

        // 自动结束进程设置
        private bool _isAutoKillPptService = false;
        private bool _isAutoKillEasiNote = false;
        private bool _isAutoKillHiteAnnotation = false;
        private bool _isAutoKillVComYouJiao = false;
        private bool _isAutoKillSeewoLauncher2DesktopAnnotation = false;
        private bool _isAutoKillInkCanvas = false;
        private bool _isAutoKillICA = false;
        private bool _isAutoKillIDT = true;

        // 自动保存设置
        private bool _isSaveScreenshotsInDateFolders = false;
        private bool _isAutoSaveStrokesAtScreenshot = false;
        private bool _isAutoSaveStrokesAtClear = false;
        private bool _isAutoClearWhenExitingWritingMode = false;
        private int _minimumAutomationStrokeNumber = 0;
        private bool _isEnableLimitAutoSaveAmount = false;
        private int _limitAutoSaveAmount = DefaultLimitAutoSaveAmount;
        private string _autoSavedStrokesLocation = DefaultAutoSavedStrokesLocation;
        private bool _autoDelSavedFiles = false;
        private int _autoDelSavedFilesDaysThreshold = 15;

        #endregion

        #region 辅助属性

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

        /// <summary>
        /// 是否启用了任何自动结束进程功能
        /// </summary>
        [JsonIgnore]
        public bool IsAnyAutoKillEnabled =>
            IsAutoKillPptService
            || IsAutoKillEasiNote
            || IsAutoKillHiteAnnotation
            || IsAutoKillVComYouJiao
            || IsAutoKillSeewoLauncher2DesktopAnnotation
            || IsAutoKillInkCanvas
            || IsAutoKillICA
            || IsAutoKillIDT;

        /// <summary>
        /// 自动保存路径是否存在
        /// </summary>
        [JsonIgnore]
        public bool AutoSavedStrokesLocationExists => Directory.Exists(AutoSavedStrokesLocation);

        #endregion

        #region 自动折叠属性

        /// <summary>
        /// 是否在希沃白板中自动折叠
        /// </summary>
        [JsonProperty("isAutoFoldInEasiNote")]
        public bool IsAutoFoldInEasiNote
        {
            get => _isAutoFoldInEasiNote;
            set => SetProperty(ref _isAutoFoldInEasiNote, value);
        }

        /// <summary>
        /// 是否在希沃白板中自动折叠时忽略桌面批注
        /// </summary>
        [JsonProperty("isAutoFoldInEasiNoteIgnoreDesktopAnno")]
        public bool IsAutoFoldInEasiNoteIgnoreDesktopAnno
        {
            get => _isAutoFoldInEasiNoteIgnoreDesktopAnno;
            set => SetProperty(ref _isAutoFoldInEasiNoteIgnoreDesktopAnno, value);
        }

        /// <summary>
        /// 是否在希沃视频展台中自动折叠
        /// </summary>
        [JsonProperty("isAutoFoldInEasiCamera")]
        public bool IsAutoFoldInEasiCamera
        {
            get => _isAutoFoldInEasiCamera;
            set => SetProperty(ref _isAutoFoldInEasiCamera, value);
        }

        /// <summary>
        /// 是否在希沃白板 3 中自动折叠
        /// </summary>
        [JsonProperty("isAutoFoldInEasiNote3")]
        public bool IsAutoFoldInEasiNote3
        {
            get => _isAutoFoldInEasiNote3;
            set => SetProperty(ref _isAutoFoldInEasiNote3, value);
        }

        /// <summary>
        /// 是否在希沃白板 3C 中自动折叠
        /// </summary>
        [JsonProperty("isAutoFoldInEasiNote3C")]
        public bool IsAutoFoldInEasiNote3C
        {
            get => _isAutoFoldInEasiNote3C;
            set => SetProperty(ref _isAutoFoldInEasiNote3C, value);
        }

        /// <summary>
        /// 是否在希沃白板 5C 中自动折叠
        /// </summary>
        [JsonProperty("isAutoFoldInEasiNote5C")]
        public bool IsAutoFoldInEasiNote5C
        {
            get => _isAutoFoldInEasiNote5C;
            set => SetProperty(ref _isAutoFoldInEasiNote5C, value);
        }

        /// <summary>
        /// 是否在希沃品课中自动折叠
        /// </summary>
        [JsonProperty("isAutoFoldInSeewoPincoTeacher")]
        public bool IsAutoFoldInSeewoPincoTeacher
        {
            get => _isAutoFoldInSeewoPincoTeacher;
            set => SetProperty(ref _isAutoFoldInSeewoPincoTeacher, value);
        }

        /// <summary>
        /// 是否在鸿合触控屏中自动折叠
        /// </summary>
        [JsonProperty("isAutoFoldInHiteTouchPro")]
        public bool IsAutoFoldInHiteTouchPro
        {
            get => _isAutoFoldInHiteTouchPro;
            set => SetProperty(ref _isAutoFoldInHiteTouchPro, value);
        }

        /// <summary>
        /// 是否在鸿合光能板中自动折叠
        /// </summary>
        [JsonProperty("isAutoFoldInHiteLightBoard")]
        public bool IsAutoFoldInHiteLightBoard
        {
            get => _isAutoFoldInHiteLightBoard;
            set => SetProperty(ref _isAutoFoldInHiteLightBoard, value);
        }

        /// <summary>
        /// 是否在鸿合展台中自动折叠
        /// </summary>
        [JsonProperty("isAutoFoldInHiteCamera")]
        public bool IsAutoFoldInHiteCamera
        {
            get => _isAutoFoldInHiteCamera;
            set => SetProperty(ref _isAutoFoldInHiteCamera, value);
        }

        /// <summary>
        /// 是否在万校云白板中自动折叠
        /// </summary>
        [JsonProperty("isAutoFoldInWxBoardMain")]
        public bool IsAutoFoldInWxBoardMain
        {
            get => _isAutoFoldInWxBoardMain;
            set => SetProperty(ref _isAutoFoldInWxBoardMain, value);
        }

        /// <summary>
        /// 是否在旧版智育白板中自动折叠
        /// </summary>
        [JsonProperty("isAutoFoldInOldZyBoard")]
        public bool IsAutoFoldInOldZyBoard
        {
            get => _isAutoFoldInOldZyBoard;
            set => SetProperty(ref _isAutoFoldInOldZyBoard, value);
        }

        /// <summary>
        /// 是否在 Microsoft Whiteboard 中自动折叠
        /// </summary>
        [JsonProperty("isAutoFoldInMSWhiteboard")]
        public bool IsAutoFoldInMSWhiteboard
        {
            get => _isAutoFoldInMSWhiteboard;
            set => SetProperty(ref _isAutoFoldInMSWhiteboard, value);
        }

        /// <summary>
        /// 是否在 Admox 白板中自动折叠
        /// </summary>
        [JsonProperty("isAutoFoldInAdmoxWhiteboard")]
        public bool IsAutoFoldInAdmoxWhiteboard
        {
            get => _isAutoFoldInAdmoxWhiteboard;
            set => SetProperty(ref _isAutoFoldInAdmoxWhiteboard, value);
        }

        /// <summary>
        /// 是否在 Admox 展台中自动折叠
        /// </summary>
        [JsonProperty("isAutoFoldInAdmoxBooth")]
        public bool IsAutoFoldInAdmoxBooth
        {
            get => _isAutoFoldInAdmoxBooth;
            set => SetProperty(ref _isAutoFoldInAdmoxBooth, value);
        }

        /// <summary>
        /// 是否在 QPoint 中自动折叠
        /// </summary>
        [JsonProperty("isAutoFoldInQPoint")]
        public bool IsAutoFoldInQPoint
        {
            get => _isAutoFoldInQPoint;
            set => SetProperty(ref _isAutoFoldInQPoint, value);
        }

        /// <summary>
        /// 是否在艺云白板中自动折叠
        /// </summary>
        [JsonProperty("isAutoFoldInYiYunWhiteboard")]
        public bool IsAutoFoldInYiYunWhiteboard
        {
            get => _isAutoFoldInYiYunWhiteboard;
            set => SetProperty(ref _isAutoFoldInYiYunWhiteboard, value);
        }

        /// <summary>
        /// 是否在艺云视频展台中自动折叠
        /// </summary>
        [JsonProperty("isAutoFoldInYiYunVisualPresenter")]
        public bool IsAutoFoldInYiYunVisualPresenter
        {
            get => _isAutoFoldInYiYunVisualPresenter;
            set => SetProperty(ref _isAutoFoldInYiYunVisualPresenter, value);
        }

        /// <summary>
        /// 是否在 MaxHub 白板中自动折叠
        /// </summary>
        [JsonProperty("isAutoFoldInMaxHubWhiteboard")]
        public bool IsAutoFoldInMaxHubWhiteboard
        {
            get => _isAutoFoldInMaxHubWhiteboard;
            set => SetProperty(ref _isAutoFoldInMaxHubWhiteboard, value);
        }

        /// <summary>
        /// 是否在 PPT 幻灯片放映中自动折叠
        /// </summary>
        [JsonProperty("isAutoFoldInPPTSlideShow")]
        public bool IsAutoFoldInPPTSlideShow
        {
            get => _isAutoFoldInPPTSlideShow;
            set => SetProperty(ref _isAutoFoldInPPTSlideShow, value);
        }

        #endregion

        #region 自动结束进程属性

        /// <summary>
        /// 是否自动结束 PPT 服务
        /// </summary>
        [JsonProperty("isAutoKillPptService")]
        public bool IsAutoKillPptService
        {
            get => _isAutoKillPptService;
            set => SetProperty(ref _isAutoKillPptService, value);
        }

        /// <summary>
        /// 是否自动结束希沃白板
        /// </summary>
        [JsonProperty("isAutoKillEasiNote")]
        public bool IsAutoKillEasiNote
        {
            get => _isAutoKillEasiNote;
            set => SetProperty(ref _isAutoKillEasiNote, value);
        }

        /// <summary>
        /// 是否自动结束鸿合批注
        /// </summary>
        [JsonProperty("isAutoKillHiteAnnotation")]
        public bool IsAutoKillHiteAnnotation
        {
            get => _isAutoKillHiteAnnotation;
            set => SetProperty(ref _isAutoKillHiteAnnotation, value);
        }

        /// <summary>
        /// 是否自动结束 VCom 幼教
        /// </summary>
        [JsonProperty("isAutoKillVComYouJiao")]
        public bool IsAutoKillVComYouJiao
        {
            get => _isAutoKillVComYouJiao;
            set => SetProperty(ref _isAutoKillVComYouJiao, value);
        }

        /// <summary>
        /// 是否自动结束希沃 Launcher2 桌面批注
        /// </summary>
        [JsonProperty("isAutoKillSeewoLauncher2DesktopAnnotation")]
        public bool IsAutoKillSeewoLauncher2DesktopAnnotation
        {
            get => _isAutoKillSeewoLauncher2DesktopAnnotation;
            set => SetProperty(ref _isAutoKillSeewoLauncher2DesktopAnnotation, value);
        }

        /// <summary>
        /// 是否自动结束 InkCanvas
        /// </summary>
        [JsonProperty("isAutoKillInkCanvas")]
        public bool IsAutoKillInkCanvas
        {
            get => _isAutoKillInkCanvas;
            set => SetProperty(ref _isAutoKillInkCanvas, value);
        }

        /// <summary>
        /// 是否自动结束 ICA
        /// </summary>
        [JsonProperty("isAutoKillICA")]
        public bool IsAutoKillICA
        {
            get => _isAutoKillICA;
            set => SetProperty(ref _isAutoKillICA, value);
        }

        /// <summary>
        /// 是否自动结束 IDT
        /// </summary>
        [JsonProperty("isAutoKillIDT")]
        public bool IsAutoKillIDT
        {
            get => _isAutoKillIDT;
            set => SetProperty(ref _isAutoKillIDT, value);
        }

        #endregion

        #region 自动保存属性

        /// <summary>
        /// 是否将截图保存在日期文件夹中
        /// </summary>
        [JsonProperty("isSaveScreenshotsInDateFolders")]
        public bool IsSaveScreenshotsInDateFolders
        {
            get => _isSaveScreenshotsInDateFolders;
            set => SetProperty(ref _isSaveScreenshotsInDateFolders, value);
        }

        /// <summary>
        /// 截图时是否自动保存笔画
        /// </summary>
        [JsonProperty("isAutoSaveStrokesAtScreenshot")]
        public bool IsAutoSaveStrokesAtScreenshot
        {
            get => _isAutoSaveStrokesAtScreenshot;
            set => SetProperty(ref _isAutoSaveStrokesAtScreenshot, value);
        }

        /// <summary>
        /// 清除时是否自动保存笔画
        /// </summary>
        [JsonProperty("isAutoSaveStrokesAtClear")]
        public bool IsAutoSaveStrokesAtClear
        {
            get => _isAutoSaveStrokesAtClear;
            set => SetProperty(ref _isAutoSaveStrokesAtClear, value);
        }

        /// <summary>
        /// 退出书写模式时是否自动清除
        /// </summary>
        [JsonProperty("isAutoClearWhenExitingWritingMode")]
        public bool IsAutoClearWhenExitingWritingMode
        {
            get => _isAutoClearWhenExitingWritingMode;
            set => SetProperty(ref _isAutoClearWhenExitingWritingMode, value);
        }

        /// <summary>
        /// 最小自动保存笔画数量（0-1000）
        /// </summary>
        [JsonProperty("minimumAutomationStrokeNumber")]
        public int MinimumAutomationStrokeNumber
        {
            get => _minimumAutomationStrokeNumber;
            set => SetProperty(ref _minimumAutomationStrokeNumber, ClampRange(value, 0, 1000));
        }

        /// <summary>
        /// 是否启用限制自动保存数量
        /// </summary>
        [JsonProperty("isEnableLimitAutoSaveAmount")]
        public bool IsEnableLimitAutoSaveAmount
        {
            get => _isEnableLimitAutoSaveAmount;
            set => SetProperty(ref _isEnableLimitAutoSaveAmount, value);
        }

        /// <summary>
        /// 自动保存数量限制（1-100）
        /// </summary>
        [JsonProperty("limitAutoSaveAmount")]
        public int LimitAutoSaveAmount
        {
            get => _limitAutoSaveAmount;
            set => SetProperty(ref _limitAutoSaveAmount, ClampRange(value, MinLimitAutoSaveAmount, MaxLimitAutoSaveAmount));
        }

        /// <summary>
        /// 自动保存笔画位置
        /// </summary>
        [JsonProperty("autoSavedStrokesLocation")]
        public string AutoSavedStrokesLocation
        {
            get => _autoSavedStrokesLocation;
            set => SetProperty(ref _autoSavedStrokesLocation, ValidateNotEmpty(value));
        }

        /// <summary>
        /// 是否自动删除保存的文件
        /// </summary>
        [JsonProperty("autoDelSavedFiles")]
        public bool AutoDelSavedFiles
        {
            get => _autoDelSavedFiles;
            set => SetProperty(ref _autoDelSavedFiles, value);
        }

        /// <summary>
        /// 自动删除保存文件的天数阈值（1-365）
        /// </summary>
        [JsonProperty("autoDelSavedFilesDaysThreshold")]
        public int AutoDelSavedFilesDaysThreshold
        {
            get => _autoDelSavedFilesDaysThreshold;
            set => SetProperty(ref _autoDelSavedFilesDaysThreshold, ClampRange(value, MinAutoDelDaysThreshold, MaxAutoDelDaysThreshold));
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 获取自动保存笔画的完整路径
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>完整路径</returns>
        public string GetAutoSavedStrokePath(string fileName)
        {
            return Path.Combine(AutoSavedStrokesLocation, fileName);
        }

        /// <summary>
        /// 确保自动保存目录存在
        /// </summary>
        /// <returns>如果目录存在或创建成功返回 true</returns>
        public bool EnsureAutoSavedStrokesLocationExists()
        {
            try
            {
                if (!Directory.Exists(AutoSavedStrokesLocation))
                {
                    Directory.CreateDirectory(AutoSavedStrokesLocation);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 重置自动保存位置为默认值
        /// </summary>
        public void ResetAutoSavedStrokesLocation()
        {
            AutoSavedStrokesLocation = DefaultAutoSavedStrokesLocation;
        }

        #endregion
    }
}
