using System;
using System.Diagnostics;
using System.IO;

namespace Ink_Canvas.Helpers
{
    class LogHelper
    {
        public static string LogFile = "Log.txt";

        public static void NewLog(string str)
        {
            WriteLogToFile(str, LogType.Info);
        }

        public static void NewLog(Exception ex)
        {
            if (ex == null) return;
            
            var errorMessage = $"Exception: {ex.Message}\nStackTrace: {ex.StackTrace}";
            if (ex.InnerException != null)
            {
                errorMessage += $"\nInner Exception: {ex.InnerException.Message}";
            }
            WriteLogToFile(errorMessage, LogType.Error);
        }

        public static void WriteLogToFile(string str, LogType logType = LogType.Info)
        {
            string strLogType = "Info";
            switch (logType)
            {
                case LogType.Event:
                    strLogType = "Event";
                    break;
                case LogType.Trace:
                    strLogType = "Trace";
                    break;
                case LogType.Error:
                    strLogType = "Error";
                    break;
                case LogType.Warning:
                    strLogType = "Warning";
                    break;
            }
            
            try
            {
                var file = App.RootPath + LogFile;
                if (!Directory.Exists(App.RootPath))
                {
                    Directory.CreateDirectory(App.RootPath);
                }
                
                // Use 'using' statement to ensure proper disposal
                using (StreamWriter sw = new StreamWriter(file, true))
                {
                    sw.WriteLine(string.Format("{0} [{1}] {2}", DateTime.Now.ToString("O"), strLogType, str));
                }
            }
            catch (Exception ex)
            {
                // Fallback: write to debug output if file logging fails
                Debug.WriteLine($"[LogHelper] Failed to write to log file: {ex.Message}");
                Debug.WriteLine($"[LogHelper] Original message [{strLogType}]: {str}");
            }
        }

        public enum LogType
        {
            Info,
            Trace,
            Error,
            Event,
            Warning
        }
    }
}
