using Sentry;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Ink_Canvas.Helpers
{
    class LogHelper
    {
        /// <summary>
        /// 是否启用 Sentry 面包屑集成
        /// </summary>
        public static bool EnableSentryBreadcrumbs { get; set; } = true;

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

        public static string LogFile = "Log.txt";

        public static void NewLog(string str,
            [CallerFilePath] string sourceFilePath = "",
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            WriteLogToFile(str, LogType.Info, sourceFilePath, memberName, sourceLineNumber);
        }

        public static void NewLog(Exception ex,
            [CallerFilePath] string sourceFilePath = "",
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (ex == null) return;

            var errorMessage = $"Exception: {ex.Message}\nStackTrace: {ex.StackTrace}";
            if (ex.InnerException != null)
            {
                errorMessage += $"\nInner Exception: {ex.InnerException.Message}";
            }
            WriteLogToFile(errorMessage, LogType.Error, sourceFilePath, memberName, sourceLineNumber);
        }

        public static void WriteLogToFile(string str, LogType logType = LogType.Info,
            [CallerFilePath] string sourceFilePath = "",
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int sourceLineNumber = 0)
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
                    sentryLevel = BreadcrumbLevel.Error; // Sentry 6.0.0 没有 Critical 级别，使用 Error
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
                // Fallback: write to debug output if file logging fails due to access permissions
                Debug.WriteLine($"[LogHelper] Failed to write to log file (Access denied): {ex.Message}");
                Debug.WriteLine($"[LogHelper] Original message: {fullLogMessage}");
            }
            catch (DirectoryNotFoundException ex)
            {
                // Fallback: write to debug output if file logging fails due to missing directory
                Debug.WriteLine($"[LogHelper] Failed to write to log file (Directory not found): {ex.Message}");
                Debug.WriteLine($"[LogHelper] Original message: {fullLogMessage}");
            }
            catch (IOException ex)
            {
                // Fallback: write to debug output if file logging fails due to IO error
                Debug.WriteLine($"[LogHelper] Failed to write to log file (IO error): {ex.Message}");
                Debug.WriteLine($"[LogHelper] Original message: {fullLogMessage}");
            }
        }

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
