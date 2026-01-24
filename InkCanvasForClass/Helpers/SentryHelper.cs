using Sentry;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Ink_Canvas.Helpers
{
    /// <summary>
    /// Sentry 辅助类，提供统一的 Sentry 功能封装
    /// 包含用户识别、面包屑跟踪、性能监控、上下文管理等功能
    /// </summary>
    public static class SentryHelper
    {
        #region 配置常量

        /// <summary>
        /// Sentry DSN
        /// </summary>
        private const string SentryDsn = "https://b86a792323dcbb06a78bd4e28e521630@o4510690045001728.ingest.us.sentry.io/4510690051620864";

        /// <summary>
        /// 是否已初始化
        /// </summary>
        private static bool _isInitialized = false;

        /// <summary>
        /// 当前活动的事务
        /// </summary>
        private static ITransactionTracer _currentTransaction = null;

        /// <summary>
        /// 当前活动的 Span
        /// </summary>
        private static ISpan _currentSpan = null;

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化 Sentry SDK
        /// </summary>
        /// <param name="enablePerformanceMonitoring">是否启用性能监控</param>
        /// <param name="tracesSampleRate">采样率 (0.0 - 1.0)</param>
        public static void Initialize(bool enablePerformanceMonitoring = true, double tracesSampleRate = 1.0)
        {
            if (_isInitialized)
            {
                LogHelper.WriteLogToFile("Sentry SDK 已初始化", LogHelper.LogType.Warning);
                return;
            }

            try
            {
                SentrySdk.Init(options =>
                {
                    options.Dsn = SentryDsn;

                    // 设置发布版本
                    options.Release = $"InkCanvasForClass@{Assembly.GetExecutingAssembly().GetName().Version}";

                    // 设置环境
#if DEBUG
                    options.Environment = "development";
                    options.Debug = true;
#else
                    options.Environment = "production";
#endif

                    // 性能监控设置
                    if (enablePerformanceMonitoring)
                    {
                        options.TracesSampleRate = tracesSampleRate;
                    }

                    // 自动会话跟踪
                    options.AutoSessionTracking = true;

                    // 附加堆栈跟踪到所有消息
                    options.AttachStacktrace = true;

                    // 设置最大面包屑数量
                    options.MaxBreadcrumbs = 100;

                    // 发送默认 PII (个人身份信息) - 根据需要调整
                    options.SendDefaultPii = false;

                    // 设置 BeforeSend 回调，可以在发送前修改或过滤事件
                    options.SetBeforeSend((sentryEvent, hint) =>
                    {
                        // 添加自定义上下文
                        sentryEvent.SetExtra("app_start_time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        return sentryEvent;
                    });
                });

                _isInitialized = true;

                // 设置默认标签
                SetDefaultTags();

                LogHelper.WriteLogToFile("Sentry SDK 初始化成功（增强功能已启用）", LogHelper.LogType.Info);
            }
            catch (InvalidOperationException ex) {
                LogHelper.WriteLogToFile($"初始化 Sentry SDK 失败（无效操作）：{ex.Message}", LogHelper.LogType.Error);
            }
            catch (ArgumentException ex) {
                LogHelper.WriteLogToFile($"初始化 Sentry SDK 失败（参数错误）：{ex.Message}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 设置默认标签
        /// </summary>
        private static void SetDefaultTags()
        {
            try
            {
                SentrySdk.ConfigureScope(scope =>
                {
                    scope.SetTag("os.version", Environment.OSVersion.ToString());
                    scope.SetTag("runtime.version", Environment.Version.ToString());
                    scope.SetTag("machine.name", Environment.MachineName);
                    scope.SetTag("processor.count", Environment.ProcessorCount.ToString());
                    scope.SetTag("is_64bit_os", Environment.Is64BitOperatingSystem.ToString());
                    scope.SetTag("is_64bit_process", Environment.Is64BitProcess.ToString());
                });
            }
            catch (InvalidOperationException ex) {
                LogHelper.WriteLogToFile($"设置 Sentry 默认标签失败（无效操作）：{ex.Message}", LogHelper.LogType.Warning);
            }
            catch (ArgumentException ex) {
                LogHelper.WriteLogToFile($"设置 Sentry 默认标签失败（参数错误）：{ex.Message}", LogHelper.LogType.Warning);
            }
        }

        /// <summary>
        /// 关闭 Sentry SDK
        /// </summary>
        /// <param name="timeoutMs">超时时间（毫秒）</param>
        public static void Close(int timeoutMs = 2000)
        {
            try
            {
                // 结束当前事务
                EndTransaction();

                // 刷新并关闭
                SentrySdk.Flush(TimeSpan.FromMilliseconds(timeoutMs));
                SentrySdk.Close();
                _isInitialized = false;
            }
            catch (InvalidOperationException ex) {
                LogHelper.WriteLogToFile($"关闭 Sentry SDK 失败（无效操作）：{ex.Message}", LogHelper.LogType.Warning);
            }
            catch (System.TimeoutException ex) {
                LogHelper.WriteLogToFile($"关闭 Sentry SDK 失败（超时）：{ex.Message}", LogHelper.LogType.Warning);
            }
        }

        #endregion

        #region 用户识别

        /// <summary>
        /// 设置用户信息
        /// </summary>
        /// <param name="userId">用户 ID</param>
        /// <param name="username">用户名</param>
        /// <param name="email">电子邮件</param>
        /// <param name="ipAddress">IP 地址（可选）</param>
        public static void SetUser(string userId, string username = null, string email = null, string ipAddress = null)
        {
            try
            {
                SentrySdk.ConfigureScope(scope =>
                {
                    scope.User = new SentryUser
                    {
                        Id = userId,
                        Username = username,
                        Email = email,
                        IpAddress = ipAddress ?? "{{auto}}"
                    };
                });

                LogHelper.WriteLogToFile($"Sentry 用户已设置：{userId}", LogHelper.LogType.Trace);
            }
            catch (InvalidOperationException ex) {
                LogHelper.WriteLogToFile($"设置 Sentry 用户失败（无效操作）：{ex.Message}", LogHelper.LogType.Warning);
            }
            catch (ArgumentException ex) {
                LogHelper.WriteLogToFile($"设置 Sentry 用户失败（参数错误）：{ex.Message}", LogHelper.LogType.Warning);
            }
        }

        /// <summary>
        /// 使用机器信息自动生成用户标识
        /// </summary>
        public static void SetAnonymousUser()
        {
            try
            {
                // 使用机器名和用户名组合生成匿名ID
                var machineId = $"{Environment.MachineName}_{Environment.UserName}".GetHashCode().ToString("X8");
                SetUser(machineId, Environment.UserName);
            }
            catch (InvalidOperationException ex) {
                LogHelper.WriteLogToFile($"设置匿名用户失败（无效操作）：{ex.Message}", LogHelper.LogType.Warning);
            }
            catch (ArgumentException ex) {
                LogHelper.WriteLogToFile($"设置匿名用户失败（参数错误）：{ex.Message}", LogHelper.LogType.Warning);
            }
        }

        /// <summary>
        /// 清除用户信息
        /// </summary>
        public static void ClearUser()
        {
            try
            {
                SentrySdk.ConfigureScope(scope =>
                {
                    scope.User = null;
                });
            }
            catch (InvalidOperationException ex) {
                LogHelper.WriteLogToFile($"清除 Sentry 用户失败（无效操作）：{ex.Message}", LogHelper.LogType.Warning);
            }
            catch (ArgumentException ex) {
                LogHelper.WriteLogToFile($"清除 Sentry 用户失败（参数错误）：{ex.Message}", LogHelper.LogType.Warning);
            }
        }

        #endregion

        #region 面包屑 (Breadcrumbs)

        /// <summary>
        /// 面包屑类别
        /// </summary>
        public static class BreadcrumbCategory
        {
            public const string Navigation = "navigation";
            public const string UI = "ui";
            public const string User = "user";
            public const string System = "system";
            public const string Http = "http";
            public const string Debug = "debug";
            public const string Error = "error";
            public const string Query = "query";
            public const string Info = "info";
            public const string Drawing = "drawing";
            public const string Settings = "settings";
            public const string File = "file";
        }

        /// <summary>
        /// 添加面包屑
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="category">类别</param>
        /// <param name="level">级别</param>
        /// <param name="data">附加数据</param>
        public static void AddBreadcrumb(
            string message,
            string category = null,
            BreadcrumbLevel level = BreadcrumbLevel.Info,
            IDictionary<string, string> data = null)
        {
            try
            {
                SentrySdk.AddBreadcrumb(
                    message: message,
                    category: category ?? BreadcrumbCategory.Info,
                    level: level,
                    data: data
                );
            }
            catch (InvalidOperationException ex) {
                LogHelper.WriteLogToFile($"添加面包屑失败（无效操作）：{ex.Message}", LogHelper.LogType.Warning);
            }
            catch (ArgumentException ex) {
                LogHelper.WriteLogToFile($"添加面包屑失败（参数错误）：{ex.Message}", LogHelper.LogType.Warning);
            }
        }

        /// <summary>
        /// 添加导航面包屑
        /// </summary>
        /// <param name="from">来源</param>
        /// <param name="to">目标</param>
        public static void AddNavigationBreadcrumb(string from, string to)
        {
            AddBreadcrumb(
                message: $"导航：{from} -> {to}",
                category: BreadcrumbCategory.Navigation,
                level: BreadcrumbLevel.Info,
                data: new Dictionary<string, string>
                {
                    { "from", from },
                    { "to", to }
                }
            );
        }

        /// <summary>
        /// 添加 UI 操作面包屑
        /// </summary>
        /// <param name="action">操作类型（如 click, hover）</param>
        /// <param name="element">元素名称</param>
        /// <param name="additionalData">附加数据</param>
        public static void AddUIBreadcrumb(string action, string element, IDictionary<string, string> additionalData = null)
        {
            var data = new Dictionary<string, string>
            {
                { "action", action },
                { "element", element }
            };

            if (additionalData != null)
            {
                foreach (var kvp in additionalData)
                {
                    data[kvp.Key] = kvp.Value;
                }
            }

            AddBreadcrumb(
                message: $"UI {action}：{element}",
                category: BreadcrumbCategory.UI,
                level: BreadcrumbLevel.Info,
                data: data
            );
        }

        /// <summary>
        /// 添加绘图操作面包屑
        /// </summary>
        /// <param name="action">操作类型</param>
        /// <param name="details">详细信息</param>
        public static void AddDrawingBreadcrumb(string action, string details = null)
        {
            AddBreadcrumb(
                message: details != null ? $"绘图：{action} - {details}" : $"绘图：{action}",
                category: BreadcrumbCategory.Drawing,
                level: BreadcrumbLevel.Info,
                data: new Dictionary<string, string>
                {
                    { "action", action },
                    { "details", details ?? "" }
                }
            );
        }

        /// <summary>
        /// 添加设置变更面包屑
        /// </summary>
        /// <param name="settingName">设置名称</param>
        /// <param name="oldValue">旧值</param>
        /// <param name="newValue">新值</param>
        public static void AddSettingChangeBreadcrumb(string settingName, string oldValue, string newValue)
        {
            AddBreadcrumb(
                message: $"设置已变更：{settingName}",
                category: BreadcrumbCategory.Settings,
                level: BreadcrumbLevel.Info,
                data: new Dictionary<string, string>
                {
                    { "setting", settingName },
                    { "old_value", oldValue ?? "null" },
                    { "new_value", newValue ?? "null" }
                }
            );
        }

        /// <summary>
        /// 添加文件操作面包屑
        /// </summary>
        /// <param name="operation">操作类型（如 save, load, delete）</param>
        /// <param name="filePath">文件路径</param>
        /// <param name="success">是否成功</param>
        public static void AddFileBreadcrumb(string operation, string filePath, bool success = true)
        {
            AddBreadcrumb(
                message: $"文件{operation}：{filePath}",
                category: BreadcrumbCategory.File,
                level: success ? BreadcrumbLevel.Info : BreadcrumbLevel.Warning,
                data: new Dictionary<string, string>
                {
                    { "operation", operation },
                    { "path", filePath },
                    { "success", success.ToString() }
                }
            );
        }

        #endregion

        #region 异常和消息捕获

        /// <summary>
        /// 捕获异常
        /// </summary>
        /// <param name="exception">异常</param>
        /// <param name="tags">标签</param>
        /// <param name="extras">额外数据</param>
        /// <returns>事件 ID</returns>
        public static SentryId CaptureException(
            Exception exception,
            IDictionary<string, string> tags = null,
            IDictionary<string, object> extras = null,
            [CallerFilePath] string sourceFilePath = "",
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            try
            {
                return SentrySdk.CaptureException(exception, scope =>
                {
                    // 添加调用位置信息
                    scope.SetExtra("caller_file", sourceFilePath);
                    scope.SetExtra("caller_method", memberName);
                    scope.SetExtra("caller_line", sourceLineNumber);

                    // 添加标签
                    if (tags != null)
                    {
                        foreach (var tag in tags)
                        {
                            scope.SetTag(tag.Key, tag.Value);
                        }
                    }

                    // 添加额外数据
                    if (extras != null)
                    {
                        foreach (var extra in extras)
                        {
                            scope.SetExtra(extra.Key, extra.Value);
                        }
                    }
                });
            }
            catch (InvalidOperationException ex) {
                LogHelper.WriteLogToFile($"Sentry 捕获异常失败（无效操作）：{ex.Message}", LogHelper.LogType.Warning);
                return SentryId.Empty;
            }
            catch (ArgumentException ex) {
                LogHelper.WriteLogToFile($"Sentry 捕获异常失败（参数错误）：{ex.Message}", LogHelper.LogType.Warning);
                return SentryId.Empty;
            }
        }

        /// <summary>
        /// 捕获消息
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="level">级别</param>
        /// <param name="tags">标签</param>
        /// <returns>事件 ID</returns>
        public static SentryId CaptureMessage(
            string message,
            SentryLevel level = SentryLevel.Info,
            IDictionary<string, string> tags = null,
            [CallerFilePath] string sourceFilePath = "",
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            try
            {
                return SentrySdk.CaptureMessage(message, scope =>
                {
                    scope.Level = level;

                    // 添加调用位置信息
                    scope.SetExtra("caller_file", sourceFilePath);
                    scope.SetExtra("caller_method", memberName);
                    scope.SetExtra("caller_line", sourceLineNumber);

                    // 添加标签
                    if (tags != null)
                    {
                        foreach (var tag in tags)
                        {
                            scope.SetTag(tag.Key, tag.Value);
                        }
                    }
                });
            }
            catch (InvalidOperationException ex) {
                LogHelper.WriteLogToFile($"Sentry 捕获消息失败（无效操作）：{ex.Message}", LogHelper.LogType.Warning);
                return SentryId.Empty;
            }
            catch (ArgumentException ex) {
                LogHelper.WriteLogToFile($"Sentry 捕获消息失败（参数错误）：{ex.Message}", LogHelper.LogType.Warning);
                return SentryId.Empty;
            }
        }

        #endregion

        #region 性能监控 (Transactions & Spans)

        /// <summary>
        /// 开始一个事务
        /// </summary>
        /// <param name="name">事务名称</param>
        /// <param name="operation">操作类型</param>
        /// <returns>事务跟踪器</returns>
        public static ITransactionTracer StartTransaction(string name, string operation)
        {
            try
            {
                // 结束之前的事务
                EndTransaction();

                _currentTransaction = SentrySdk.StartTransaction(name, operation);
                LogHelper.WriteLogToFile($"Sentry 事务已开始：{name}（{operation}）", LogHelper.LogType.Trace);
                return _currentTransaction;
            }
            catch (InvalidOperationException ex) {
                LogHelper.WriteLogToFile($"启动 Sentry 事务失败（无效操作）：{ex.Message}", LogHelper.LogType.Warning);
                return null;
            }
            catch (ArgumentException ex) {
                LogHelper.WriteLogToFile($"启动 Sentry 事务失败（参数错误）：{ex.Message}", LogHelper.LogType.Warning);
                return null;
            }
        }

        /// <summary>
        /// 获取当前事务
        /// </summary>
        public static ITransactionTracer CurrentTransaction => _currentTransaction;

        /// <summary>
        /// 结束当前事务
        /// </summary>
        /// <param name="status">事务状态</param>
        public static void EndTransaction(SpanStatus status = SpanStatus.Ok)
        {
            try
            {
                if (_currentSpan != null)
                {
                    _currentSpan.Finish(status);
                    _currentSpan = null;
                }

                if (_currentTransaction != null)
                {
                    _currentTransaction.Finish(status);
                    _currentTransaction = null;
                    LogHelper.WriteLogToFile("Sentry 事务已结束", LogHelper.LogType.Trace);
                }
            }
            catch (InvalidOperationException ex) {
                LogHelper.WriteLogToFile($"结束 Sentry 事务失败（无效操作）：{ex.Message}", LogHelper.LogType.Warning);
            }
            catch (ArgumentException ex) {
                LogHelper.WriteLogToFile($"结束 Sentry 事务失败（参数错误）：{ex.Message}", LogHelper.LogType.Warning);
            }
        }

        /// <summary>
        /// 在当前事务中开始一个 Span
        /// </summary>
        /// <param name="operation">操作类型</param>
        /// <param name="description">描述</param>
        /// <returns>Span</returns>
        public static ISpan StartSpan(string operation, string description = null)
        {
            try
            {
                if (_currentTransaction == null)
                {
                    LogHelper.WriteLogToFile("无法开始 Span：当前没有活动事务", LogHelper.LogType.Warning);
                    return null;
                }

                // 结束之前的 Span
                EndSpan();

                _currentSpan = _currentTransaction.StartChild(operation, description);
                return _currentSpan;
            }
            catch (InvalidOperationException ex) {
                LogHelper.WriteLogToFile($"启动 Sentry Span 失败（无效操作）：{ex.Message}", LogHelper.LogType.Warning);
                return null;
            }
            catch (ArgumentException ex) {
                LogHelper.WriteLogToFile($"启动 Sentry Span 失败（参数错误）：{ex.Message}", LogHelper.LogType.Warning);
                return null;
            }
        }

        /// <summary>
        /// 结束当前 Span
        /// </summary>
        /// <param name="status">状态</param>
        public static void EndSpan(SpanStatus status = SpanStatus.Ok)
        {
            try
            {
                if (_currentSpan != null)
                {
                    _currentSpan.Finish(status);
                    _currentSpan = null;
                }
            }
            catch (InvalidOperationException ex) {
                LogHelper.WriteLogToFile($"结束 Sentry Span 失败（无效操作）：{ex.Message}", LogHelper.LogType.Warning);
            }
            catch (ArgumentException ex) {
                LogHelper.WriteLogToFile($"结束 Sentry Span 失败（参数错误）：{ex.Message}", LogHelper.LogType.Warning);
            }
        }

        /// <summary>
        /// 测量代码块的执行时间
        /// </summary>
        /// <param name="operation">操作名称</param>
        /// <param name="description">描述</param>
        /// <returns>可释放的 Span 包装器</returns>
        public static SpanScope MeasureSpan(string operation, string description = null)
        {
            return new SpanScope(operation, description);
        }

        /// <summary>
        /// Span 作用域包装器，用于 using 语句
        /// </summary>
        public class SpanScope : IDisposable
        {
            private readonly ISpan _span;
            private SpanStatus _status = SpanStatus.Ok;

            public SpanScope(string operation, string description = null)
            {
                try
                {
                    if (_currentTransaction != null)
                    {
                        _span = _currentTransaction.StartChild(operation, description);
                    }
                }
                catch (InvalidOperationException ex) {
                    LogHelper.WriteLogToFile($"创建 SpanScope 失败（无效操作）：{ex.Message}", LogHelper.LogType.Warning);
                }
                catch (ArgumentException ex) {
                    LogHelper.WriteLogToFile($"创建 SpanScope 失败（参数错误）：{ex.Message}", LogHelper.LogType.Warning);
                }
            }

            /// <summary>
            /// 设置 Span 状态
            /// </summary>
            public void SetStatus(SpanStatus status)
            {
                _status = status;
            }

            /// <summary>
            /// 标记为失败
            /// </summary>
            public void SetFailed()
            {
                _status = SpanStatus.InternalError;
            }

            public void Dispose()
            {
                try
                {
                    _span?.Finish(_status);
                }
                catch (InvalidOperationException)
                {
                    // Ignore dispose errors
                }
            }
        }

        #endregion

        #region 上下文和标签

        /// <summary>
        /// 设置标签
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        public static void SetTag(string key, string value)
        {
            try
            {
                SentrySdk.ConfigureScope(scope =>
                {
                    scope.SetTag(key, value);
                });
            }
            catch (InvalidOperationException ex) {
                LogHelper.WriteLogToFile($"设置 Sentry 标签失败（无效操作）：{ex.Message}", LogHelper.LogType.Warning);
            }
            catch (ArgumentException ex) {
                LogHelper.WriteLogToFile($"设置 Sentry 标签失败（参数错误）：{ex.Message}", LogHelper.LogType.Warning);
            }
        }

        /// <summary>
        /// 设置多个标签
        /// </summary>
        /// <param name="tags">标签字典</param>
        public static void SetTags(IDictionary<string, string> tags)
        {
            try
            {
                SentrySdk.ConfigureScope(scope =>
                {
                    foreach (var tag in tags)
                    {
                        scope.SetTag(tag.Key, tag.Value);
                    }
                });
            }
            catch (InvalidOperationException ex) {
                LogHelper.WriteLogToFile($"设置 Sentry 标签集合失败（无效操作）：{ex.Message}", LogHelper.LogType.Warning);
            }
            catch (ArgumentException ex) {
                LogHelper.WriteLogToFile($"设置 Sentry 标签集合失败（参数错误）：{ex.Message}", LogHelper.LogType.Warning);
            }
        }

        /// <summary>
        /// 设置额外数据
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        public static void SetExtra(string key, object value)
        {
            try
            {
                SentrySdk.ConfigureScope(scope =>
                {
                    scope.SetExtra(key, value);
                });
            }
            catch (InvalidOperationException ex) {
                LogHelper.WriteLogToFile($"设置 Sentry 额外数据失败（无效操作）：{ex.Message}", LogHelper.LogType.Warning);
            }
            catch (ArgumentException ex) {
                LogHelper.WriteLogToFile($"设置 Sentry 额外数据失败（参数错误）：{ex.Message}", LogHelper.LogType.Warning);
            }
        }

        /// <summary>
        /// 设置上下文
        /// </summary>
        /// <param name="key">上下文键</param>
        /// <param name="data">上下文数据</param>
        public static void SetContext(string key, object data)
        {
            try
            {
                SentrySdk.ConfigureScope(scope =>
                {
                    scope.Contexts[key] = data;
                });
            }
            catch (InvalidOperationException ex) {
                LogHelper.WriteLogToFile($"设置 Sentry 上下文失败（无效操作）：{ex.Message}", LogHelper.LogType.Warning);
            }
            catch (ArgumentException ex) {
                LogHelper.WriteLogToFile($"设置 Sentry 上下文失败（参数错误）：{ex.Message}", LogHelper.LogType.Warning);
            }
        }

        /// <summary>
        /// 设置应用程序上下文
        /// </summary>
        /// <param name="currentMode">当前模式</param>
        /// <param name="isInPPTMode">是否在 PPT 模式</param>
        /// <param name="currentPage">当前页面</param>
        /// <param name="totalPages">总页数</param>
        public static void SetAppContext(string currentMode, bool isInPPTMode, int currentPage, int totalPages)
        {
            SetContext("app_state", new
            {
                current_mode = currentMode,
                is_in_ppt_mode = isInPPTMode,
                current_page = currentPage,
                total_pages = totalPages,
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }

        /// <summary>
        /// 设置绘图上下文
        /// </summary>
        /// <param name="penColor">画笔颜色</param>
        /// <param name="penSize">画笔大小</param>
        /// <param name="isEraser">是否为橡皮擦</param>
        /// <param name="strokeCount">笔画数量</param>
        public static void SetDrawingContext(string penColor, double penSize, bool isEraser, int strokeCount)
        {
            SetContext("drawing_state", new
            {
                pen_color = penColor,
                pen_size = penSize,
                is_eraser = isEraser,
                stroke_count = strokeCount,
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }

        #endregion

        #region 便捷方法

        /// <summary>
        /// 记录信息级别消息
        /// </summary>
        public static void LogInfo(string message)
        {
            CaptureMessage(message, SentryLevel.Info);
        }

        /// <summary>
        /// 记录警告级别消息
        /// </summary>
        public static void LogWarning(string message)
        {
            CaptureMessage(message, SentryLevel.Warning);
        }

        /// <summary>
        /// 记录错误级别消息
        /// </summary>
        public static void LogError(string message)
        {
            CaptureMessage(message, SentryLevel.Error);
        }

        /// <summary>
        /// 记录致命级别消息
        /// </summary>
        public static void LogFatal(string message)
        {
            CaptureMessage(message, SentryLevel.Fatal);
        }

        /// <summary>
        /// 刷新 Sentry 事件队列
        /// </summary>
        /// <param name="timeoutMs">超时时间（毫秒）</param>
        public static void Flush(int timeoutMs = 2000)
        {
            try
            {
                SentrySdk.Flush(TimeSpan.FromMilliseconds(timeoutMs));
            }
            catch (InvalidOperationException ex) {
                LogHelper.WriteLogToFile($"刷新 Sentry 失败（无效操作）：{ex.Message}", LogHelper.LogType.Warning);
            }
            catch (System.TimeoutException ex) {
                LogHelper.WriteLogToFile($"刷新 Sentry 失败（超时）：{ex.Message}", LogHelper.LogType.Warning);
            }
        }

        #endregion
    }
}
