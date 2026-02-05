using Serilog;
using Serilog.Context;
using Serilog.Core;
using Serilog.Events;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using LogLevel = Ink_Canvas.Services.Logging.LogLevel;

namespace Ink_Canvas.Services.Logging
{
    /// <summary>
    /// 现代化日志服务实现 - 基于 Serilog
    /// </summary>
    public class LoggerService : ILoggerService, IDisposable
    {
        private Serilog.ILogger? _logger;
        private readonly LoggingLevelSwitch _levelSwitch;
        private readonly string _logDirectory;
        private bool _isInitialized;
        private readonly object _initLock = new();

        /// <summary>
        /// 单例实例
        /// </summary>
        public static LoggerService Instance { get; } = new();

        private LoggerService()
        {
            _levelSwitch = new LoggingLevelSwitch(LogEventLevel.Debug);
            _logDirectory = Path.Combine(App.RootPath, "Logs");
        }

        /// <summary>
        /// 初始化日志服务
        /// </summary>
        public void Initialize(string? sentryDsn = null, bool enableConsole = true, bool enableFile = true)
        {
            if (_isInitialized) return;

            lock (_initLock)
            {
                if (_isInitialized) return;

                try
                {
                    // 确保日志目录存在
                    if (!Directory.Exists(_logDirectory))
                    {
                        Directory.CreateDirectory(_logDirectory);
                    }

                    var config = new LoggerConfiguration()
                        .MinimumLevel.ControlledBy(_levelSwitch)
                        .Enrich.FromLogContext();

                    // 控制台输出
                    if (enableConsole)
                    {
                        config = config.WriteTo.Console(
                            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                            theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Code);
                    }

                    // 文件输出 - 支持滚动
                    if (enableFile)
                    {
                        var logFilePath = Path.Combine(_logDirectory, "log-.txt");
                        config = config.WriteTo.File(
                            path: logFilePath,
                            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                            rollingInterval: RollingInterval.Day,
                            retainedFileCountLimit: 30, // 保留30天日志
                            fileSizeLimitBytes: 10 * 1024 * 1024, // 10MB
                            rollOnFileSizeLimit: true,
                            shared: true,
                            flushToDiskInterval: TimeSpan.FromSeconds(1));
                    }

                    // Sentry 集成
                    if (!string.IsNullOrEmpty(sentryDsn))
                    {
                        config = config.WriteTo.Sentry(o =>
                        {
                            o.Dsn = sentryDsn;
                            o.MinimumBreadcrumbLevel = LogEventLevel.Debug;
                            o.MinimumEventLevel = LogEventLevel.Error;
                            o.AttachStacktrace = true;
                            o.SendDefaultPii = false;
                        });
                    }

                    _logger = config.CreateLogger();
                    _isInitialized = true;

                    Information("日志服务初始化完成");
                }
                catch (Exception ex)
                {
                    // 初始化失败时回退到简单控制台输出
                    _logger = new LoggerConfiguration()
                        .WriteTo.Console()
                        .CreateLogger();

                    _logger.Error(ex, "日志服务初始化失败，使用回退配置");
                    _isInitialized = true;
                }
            }
        }

        /// <summary>
        /// 设置日志级别
        /// </summary>
        public void SetMinimumLevel(LogLevel level)
        {
            _levelSwitch.MinimumLevel = ConvertLogLevel(level);
        }

        #region ILoggerService Implementation

        public void Verbose(string message,
            [CallerFilePath] string sourceFilePath = "",
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            Log(LogLevel.Verbose, message, sourceFilePath, memberName, sourceLineNumber);
        }

        public void Debug(string message,
            [CallerFilePath] string sourceFilePath = "",
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            Log(LogLevel.Debug, message, sourceFilePath, memberName, sourceLineNumber);
        }

        public void Information(string message,
            [CallerFilePath] string sourceFilePath = "",
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            Log(LogLevel.Information, message, sourceFilePath, memberName, sourceLineNumber);
        }

        public void Warning(string message,
            [CallerFilePath] string sourceFilePath = "",
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            Log(LogLevel.Warning, message, sourceFilePath, memberName, sourceLineNumber);
        }

        public void Error(string message,
            [CallerFilePath] string sourceFilePath = "",
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            Log(LogLevel.Error, message, sourceFilePath, memberName, sourceLineNumber);
        }

        public void Fatal(string message,
            [CallerFilePath] string sourceFilePath = "",
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            Log(LogLevel.Fatal, message, sourceFilePath, memberName, sourceLineNumber);
        }

        public void Exception(Exception exception, string? message = null,
            [CallerFilePath] string sourceFilePath = "",
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            EnsureInitialized();

            var context = BuildSourceContext(sourceFilePath, memberName, sourceLineNumber);
            var logMessage = string.IsNullOrEmpty(message)
                ? $"Exception: {exception.Message}"
                : message;

            using (LogContext.PushProperty("SourceContext", context))
            {
                _logger!.Error(exception, logMessage);
            }
        }

        public void Log(LogLevel level, string messageTemplate, object[] propertyValues,
            [CallerFilePath] string sourceFilePath = "",
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            EnsureInitialized();

            var context = BuildSourceContext(sourceFilePath, memberName, sourceLineNumber);
            var serilogLevel = ConvertLogLevel(level);

            using (LogContext.PushProperty("SourceContext", context))
            {
                _logger!.Write(serilogLevel, messageTemplate, propertyValues);
            }
        }

        public IDisposable BeginScope(string messageTemplate, params object[] args)
        {
            EnsureInitialized();
            return LogContext.PushProperty("Scope", string.Format(messageTemplate, args));
        }

        public void AddGlobalProperty(string key, object value)
        {
            EnsureInitialized();
            LogContext.PushProperty(key, value);
        }

        public void RemoveGlobalProperty(string key)
        {
            // Serilog 的 LogContext 不支持直接移除，这里只是接口实现
            // 实际使用中可以通过 PushProperty 的 IDisposable 返回值来管理生命周期
        }

        public void Flush()
        {
            (_logger as Logger)?.Dispose();
        }

        public void Close()
        {
            (_logger as Logger)?.Dispose();
            _isInitialized = false;
        }

        #endregion

        #region Helper Methods

        private void Log(LogLevel level, string message,
            string sourceFilePath, string memberName, int sourceLineNumber)
        {
            EnsureInitialized();

            var context = BuildSourceContext(sourceFilePath, memberName, sourceLineNumber);
            var serilogLevel = ConvertLogLevel(level);

            using (LogContext.PushProperty("SourceContext", context))
            {
                _logger!.Write(serilogLevel, message);
            }
        }

        private string BuildSourceContext(string sourceFilePath, string memberName, int sourceLineNumber)
        {
            var fileName = Path.GetFileName(sourceFilePath);
            return $"{fileName}:{memberName}:{sourceLineNumber}";
        }

        private LogEventLevel ConvertLogLevel(LogLevel level)
        {
            return level switch
            {
                LogLevel.Verbose => LogEventLevel.Verbose,
                LogLevel.Debug => LogEventLevel.Debug,
                LogLevel.Information => LogEventLevel.Information,
                LogLevel.Warning => LogEventLevel.Warning,
                LogLevel.Error => LogEventLevel.Error,
                LogLevel.Fatal => LogEventLevel.Fatal,
                _ => LogEventLevel.Information
            };
        }

        private void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                Initialize();
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Close();
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
