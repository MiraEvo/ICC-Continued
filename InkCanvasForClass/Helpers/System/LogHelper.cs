using Ink_Canvas.Services.Logging;
using Sentry;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Ink_Canvas.Helpers
{
    /// <summary>
    /// 日志帮助类 - 兼容层，内部使用现代化的 LoggerService
    /// </summary>
    public class LogHelper
    {
        /// <summary>
        /// 是否启用 Sentry 面包屑集成
        /// </summary>
        public static bool EnableSentryBreadcrumbs { get; set; } = true;

        /// <summary>
        /// 日志文件名（向后兼容）
        /// </summary>
        public static string LogFile = "Log.txt";

        /// <summary>
        /// 是否使用新的日志服务（默认启用）
        /// </summary>
        public static bool UseModernLogger { get; set; } = true;

        static LogHelper()
        {
            try
            {
                Console.OutputEncoding = Encoding.Default;
            }
            catch (PlatformNotSupportedException)
            {
                // Console not available on this platform
            }
            catch (IOException)
            {
                // Console encoding setup failed
            }
        }

        /// <summary>
        /// 记录信息日志（兼容旧版）
        /// </summary>
        public static void NewLog(string str,
            [CallerFilePath] string sourceFilePath = "",
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (UseModernLogger)
            {
                LoggerService.Instance.Information(str, sourceFilePath, memberName, sourceLineNumber);
            }
            else
            {
                WriteLogToFile(str, LogType.Info, sourceFilePath, memberName, sourceLineNumber);
            }
        }

        /// <summary>
        /// 记录异常日志（兼容旧版）
        /// </summary>
        public static void NewLog(Exception ex,
            [CallerFilePath] string sourceFilePath = "",
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (ex == null) return;

            if (UseModernLogger)
            {
                LoggerService.Instance.Exception(ex, null, sourceFilePath, memberName, sourceLineNumber);
            }
            else
            {
                var errorMessage = $"Exception: {ex.Message}\nStackTrace: {ex.StackTrace}";
                if (ex.InnerException != null)
                {
                    errorMessage += $"\nInner Exception: {ex.InnerException.Message}";
                }
                WriteLogToFile(errorMessage, LogType.Error, sourceFilePath, memberName, sourceLineNumber);
            }
        }

        /// <summary>
        /// 写入日志到文件（兼容旧版，现在使用新的日志服务）
        /// </summary>
        public static void WriteLogToFile(string str, LogType logType = LogType.Info,
            [CallerFilePath] string sourceFilePath = "",
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (UseModernLogger)
            {
                // 映射到新的日志级别
                switch (logType)
                {
                    case LogType.Trace:
                        LoggerService.Instance.Debug(str, sourceFilePath, memberName, sourceLineNumber);
                        break;
                    case LogType.Error:
                        LoggerService.Instance.Error(str, sourceFilePath, memberName, sourceLineNumber);
                        break;
                    case LogType.Warning:
                        LoggerService.Instance.Warning(str, sourceFilePath, memberName, sourceLineNumber);
                        break;
                    case LogType.Fatal:
                        LoggerService.Instance.Fatal(str, sourceFilePath, memberName, sourceLineNumber);
                        break;
                    case LogType.Info:
                    case LogType.Event:
                    default:
                        LoggerService.Instance.Information(str, sourceFilePath, memberName, sourceLineNumber);
                        break;
                }
                return;
            }

            // 旧版实现（作为回退）
            LegacyWriteLogToFile(str, logType, sourceFilePath, memberName, sourceLineNumber);
        }

        /// <summary>
        /// 初始化日志服务（新功能）
        /// </summary>
        public static void Initialize(string? sentryDsn = null, bool enableConsole = true, bool enableFile = true)
        {
            LoggerService.Instance.Initialize(sentryDsn, enableConsole, enableFile);
        }

        /// <summary>
        /// 关闭日志服务
        /// </summary>
        public static void Close()
        {
            LoggerService.Instance.Close();
        }

        /// <summary>
        /// 刷新日志缓冲区
        /// </summary>
        public static void Flush()
        {
            LoggerService.Instance.Flush();
        }

        #region Legacy Implementation (Fallback)

        private static void LegacyWriteLogToFile(string str, LogType logType,
            string sourceFilePath, string memberName, int sourceLineNumber)
        {
            string strLogType = "信息";
            ConsoleColor consoleColor = ConsoleColor.White;
            BreadcrumbLevel sentryLevel = BreadcrumbLevel.Info;

            switch (logType)
            {
                case LogType.Event:
                    strLogType = "事件";
                    consoleColor = ConsoleColor.Cyan;
                    sentryLevel = BreadcrumbLevel.Info;
                    break;
                case LogType.Trace:
                    strLogType = "追踪";
                    consoleColor = ConsoleColor.Gray;
                    sentryLevel = BreadcrumbLevel.Debug;
                    break;
                case LogType.Error:
                    strLogType = "错误";
                    consoleColor = ConsoleColor.Red;
                    sentryLevel = BreadcrumbLevel.Error;
                    break;
                case LogType.Warning:
                    strLogType = "警告";
                    consoleColor = ConsoleColor.Yellow;
                    sentryLevel = BreadcrumbLevel.Warning;
                    break;
                case LogType.Fatal:
                    strLogType = "致命";
                    consoleColor = ConsoleColor.DarkRed;
                    sentryLevel = BreadcrumbLevel.Error;
                    break;
                case LogType.Info:
                    strLogType = "信息";
                    consoleColor = ConsoleColor.White;
                    sentryLevel = BreadcrumbLevel.Info;
                    break;
            }

            string fileName = Path.GetFileName(sourceFilePath);
            string logMessage = $"[{strLogType}] [{fileName}:{memberName}:{sourceLineNumber}] {str}";
            string timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string fullLogMessage = $"{timeStamp} {logMessage}";

            // Console Output with Color
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = consoleColor;
            Console.WriteLine(fullLogMessage);
            Console.ForegroundColor = originalColor;

            // Debug Output
            Debug.WriteLine(fullLogMessage);

            // Sentry Breadcrumb Integration
            if (EnableSentryBreadcrumbs)
            {
                try
                {
                    SentryHelper.AddBreadcrumb(
                        message: str,
                        category: "log",
                        level: sentryLevel,
                        data: new System.Collections.Generic.Dictionary<string, string>
                        {
                            { "file", fileName },
                            { "method", memberName },
                            { "line", sourceLineNumber.ToString() },
                            { "log_type", logType.ToString() }
                        }
                    );
                }
                catch (InvalidOperationException)
                {
                    // Ignore Sentry errors to prevent recursive logging
                }
                catch (System.Net.Http.HttpRequestException)
                {
                    // Ignore Sentry network errors to prevent recursive logging
                }
            }

            try
            {
                var file = App.RootPath + LogFile;
                if (!Directory.Exists(App.RootPath))
                {
                    Directory.CreateDirectory(App.RootPath);
                }

                // Use 'using' statement to ensure proper disposal
                using (StreamWriter sw = new StreamWriter(file, true, Encoding.UTF8))
                {
                    sw.WriteLine(fullLogMessage);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"[LogHelper] Failed to write to log file (Access denied): {ex.Message}");
                Debug.WriteLine($"[LogHelper] Original message: {fullLogMessage}");
            }
            catch (DirectoryNotFoundException ex)
            {
                Debug.WriteLine($"[LogHelper] Failed to write to log file (Directory not found): {ex.Message}");
                Debug.WriteLine($"[LogHelper] Original message: {fullLogMessage}");
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"[LogHelper] Failed to write to log file (IO error): {ex.Message}");
                Debug.WriteLine($"[LogHelper] Original message: {fullLogMessage}");
            }
        }

        #endregion

        /// <summary>
        /// 日志类型枚举（向后兼容）
        /// </summary>
        public enum LogType
        {
            Info,
            Trace,
            Error,
            Event,
            Warning,
            Fatal
        }
    }
}
