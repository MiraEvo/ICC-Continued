using System;
using System.ComponentModel;
using Newtonsoft.Json;

// 导入新的 Settings 模型命名空间
using Ink_Canvas.Models.Settings;

namespace Ink_Canvas
{
    /// <summary>
    /// 应用程序设置根类
    /// 包含所有配置分类的设置项
    /// </summary>
    public class Settings : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private AdvancedSettings _advanced = new AdvancedSettings();
        private AppearanceSettings _appearance = new AppearanceSettings();
        private AutomationSettings _automation = new AutomationSettings();
        private PowerPointSettings _powerPointSettings = new PowerPointSettings();
        private CanvasSettings _canvas = new CanvasSettings();
        private GestureSettings _gesture = new GestureSettings();
        private InkToShapeSettings _inkToShape = new InkToShapeSettings();
        private StartupSettings _startup = new StartupSettings();
        private RandSettings _randSettings = new RandSettings();
        private SnapshotSettings _snapshot = new SnapshotSettings();
        private StorageSettings _storage = new StorageSettings();

        /// <summary>
        /// 高级设置
        /// </summary>
        [JsonProperty("advanced")]
        public AdvancedSettings Advanced
        {
            get => _advanced;
            set
            {
                if (_advanced != value)
                {
                    _advanced = value ?? new AdvancedSettings();
                    OnPropertyChanged(nameof(Advanced));
                }
            }
        }

        /// <summary>
        /// 外观设置
        /// </summary>
        [JsonProperty("appearance")]
        public AppearanceSettings Appearance
        {
            get => _appearance;
            set
            {
                if (_appearance != value)
                {
                    _appearance = value ?? new AppearanceSettings();
                    OnPropertyChanged(nameof(Appearance));
                }
            }
        }

        /// <summary>
        /// 自动化设置
        /// </summary>
        [JsonProperty("automation")]
        public AutomationSettings Automation
        {
            get => _automation;
            set
            {
                if (_automation != value)
                {
                    _automation = value ?? new AutomationSettings();
                    OnPropertyChanged(nameof(Automation));
                }
            }
        }

        /// <summary>
        /// PowerPoint 行为设置
        /// </summary>
        [JsonProperty("behavior")]
        public PowerPointSettings PowerPointSettings
        {
            get => _powerPointSettings;
            set
            {
                if (_powerPointSettings != value)
                {
                    _powerPointSettings = value ?? new PowerPointSettings();
                    OnPropertyChanged(nameof(PowerPointSettings));
                }
            }
        }

        /// <summary>
        /// 画布设置
        /// </summary>
        [JsonProperty("canvas")]
        public CanvasSettings Canvas
        {
            get => _canvas;
            set
            {
                if (_canvas != value)
                {
                    _canvas = value ?? new CanvasSettings();
                    OnPropertyChanged(nameof(Canvas));
                }
            }
        }

        /// <summary>
        /// 手势设置
        /// </summary>
        [JsonProperty("gesture")]
        public GestureSettings Gesture
        {
            get => _gesture;
            set
            {
                if (_gesture != value)
                {
                    _gesture = value ?? new GestureSettings();
                    OnPropertyChanged(nameof(Gesture));
                }
            }
        }

        /// <summary>
        /// 墨迹转形状设置
        /// </summary>
        [JsonProperty("inkToShape")]
        public InkToShapeSettings InkToShape
        {
            get => _inkToShape;
            set
            {
                if (_inkToShape != value)
                {
                    _inkToShape = value ?? new InkToShapeSettings();
                    OnPropertyChanged(nameof(InkToShape));
                }
            }
        }

        /// <summary>
        /// 启动设置
        /// </summary>
        [JsonProperty("startup")]
        public StartupSettings Startup
        {
            get => _startup;
            set
            {
                if (_startup != value)
                {
                    _startup = value ?? new StartupSettings();
                    OnPropertyChanged(nameof(Startup));
                }
            }
        }

        /// <summary>
        /// 随机点名设置
        /// </summary>
        [JsonProperty("randSettings")]
        public RandSettings RandSettings
        {
            get => _randSettings;
            set
            {
                if (_randSettings != value)
                {
                    _randSettings = value ?? new RandSettings();
                    OnPropertyChanged(nameof(RandSettings));
                }
            }
        }

        /// <summary>
        /// 截图设置
        /// </summary>
        [JsonProperty("snapshot")]
        public SnapshotSettings Snapshot
        {
            get => _snapshot;
            set
            {
                if (_snapshot != value)
                {
                    _snapshot = value ?? new SnapshotSettings();
                    OnPropertyChanged(nameof(Snapshot));
                }
            }
        }

        /// <summary>
        /// 存储设置
        /// </summary>
        [JsonProperty("storage")]
        public StorageSettings Storage
        {
            get => _storage;
            set
            {
                if (_storage != value)
                {
                    _storage = value ?? new StorageSettings();
                    OnPropertyChanged(nameof(Storage));
                }
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // 为了向后兼容，保留原有类名的类型别名
    // 这些别名允许现有代码继续使用旧的类名
    
    /// <summary>
    /// 截图设置类（向后兼容别名）
    /// </summary>
    public class Snapshot : SnapshotSettings { }

    /// <summary>
    /// 存储设置类（向后兼容别名）
    /// </summary>
    public class Storage : StorageSettings { }

    /// <summary>
    /// 画布设置类（向后兼容别名）
    /// </summary>
    public class Canvas : CanvasSettings { }

    /// <summary>
    /// 手势设置类（向后兼容别名）
    /// </summary>
    public class Gesture : GestureSettings { }

    /// <summary>
    /// 启动设置类（向后兼容别名）
    /// </summary>
    public class Startup : StartupSettings { }

    /// <summary>
    /// 外观设置类（向后兼容别名）
    /// </summary>
    public class Appearance : AppearanceSettings { }

    /// <summary>
    /// 自动化设置类（向后兼容别名）
    /// </summary>
    public class Automation : AutomationSettings { }

    /// <summary>
    /// 高级设置类（向后兼容别名）
    /// </summary>
    public class Advanced : AdvancedSettings { }

    /// <summary>
    /// 墨迹转形状设置类（向后兼容别名）
    /// </summary>
    public class InkToShape : InkToShapeSettings { }

    // 枚举类型需要在 Ink_Canvas 命名空间中定义，以便现有代码可以使用
    // 这些枚举与 Ink_Canvas.Models.Settings 中的枚举值相同
    
    /// <summary>
    /// 可选操作枚举
    /// </summary>
    public enum OptionalOperation
    {
        Yes,
        No,
        Ask
    }

    /// <summary>
    /// 黑板背景颜色枚举
    /// </summary>
    public enum BlackboardBackgroundColorEnum
    {
        GrayBlack,
        BlackBoardGreen,
        White,
        BlueBlack,
        EyeProtectionGreen,
        RealBlack
    }

    /// <summary>
    /// 黑板背景图案枚举
    /// </summary>
    public enum BlackboardBackgroundPatternEnum
    {
        None,
        Dots,
        Grid
    }
}
