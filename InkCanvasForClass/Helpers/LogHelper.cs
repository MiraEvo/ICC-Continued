using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Ink_Canvas.Helpers
{
    class LogHelper
    {
        static LogHelper()
        {
            try
            {
                Console.OutputEncoding = Encoding.Default;
            }
            catch
            {
                // Ignored
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

            switch (logType)
            {
                case LogType.Event:
                    strLogType = "事件";
                    consoleColor = ConsoleColor.Cyan;
                    break;
                case LogType.Trace:
                    strLogType = "追踪";
                    consoleColor = ConsoleColor.Gray;
                    break;
                case LogType.Error:
                    strLogType = "错误";
                    consoleColor = ConsoleColor.Red;
                    break;
                case LogType.Warning:
                    strLogType = "警告";
                    consoleColor = ConsoleColor.Yellow;
                    break;
                case LogType.Fatal:
                    strLogType = "致命";
                    consoleColor = ConsoleColor.DarkRed;
                    break;
                case LogType.Info:
                    strLogType = "信息";
                    consoleColor = ConsoleColor.White;
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
            catch (Exception ex)
            {
                // Fallback: write to debug output if file logging fails
                Debug.WriteLine($"[LogHelper] Failed to write to log file: {ex.Message}");
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
