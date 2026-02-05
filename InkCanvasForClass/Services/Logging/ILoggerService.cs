using System;
using System.Runtime.CompilerServices;

namespace Ink_Canvas.Services.Logging
{
    /// <summary>
    /// 日志级别
    /// </summary>
    public enum LogLevel
    {
        Verbose,
        Debug,
        Information,
        Warning,
        Error,
        Fatal
    }

    /// <summary>
    /// 日志服务接口 - 提供现代化的日志记录功能
    /// </summary>
    public interface ILoggerService
    {
        /// <summary>
        /// 记录详细日志
        /// </summary>
        void Verbose(string message,
            [CallerFilePath] string sourceFilePath = "",
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int sourceLineNumber = 0);

        /// <summary>
        /// 记录调试日志
        /// </summary>
        void Debug(string message,
            [CallerFilePath] string sourceFilePath = "",
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int sourceLineNumber = 0);

        /// <summary>
        /// 记录信息日志
        /// </summary>
        void Information(string message,
            [CallerFilePath] string sourceFilePath = "",
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int sourceLineNumber = 0);

        /// <summary>
        /// 记录警告日志
        /// </summary>
        void Warning(string message,
            [CallerFilePath] string sourceFilePath = "",
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int sourceLineNumber = 0);

        /// <summary>
        /// 记录错误日志
        /// </summary>
        void Error(string message,
            [CallerFilePath] string sourceFilePath = "",
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int sourceLineNumber = 0);

        /// <summary>
        /// 记录致命错误日志
        /// </summary>
        void Fatal(string message,
            [CallerFilePath] string sourceFilePath = "",
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int sourceLineNumber = 0);

        /// <summary>
        /// 记录异常
        /// </summary>
        void Exception(Exception exception, string? message = null,
            [CallerFilePath] string sourceFilePath = "",
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int sourceLineNumber = 0);

        /// <summary>
        /// 记录结构化日志（使用模板和属性）
        /// </summary>
        void Log(LogLevel level, string messageTemplate, object[] propertyValues,
            [CallerFilePath] string sourceFilePath = "",
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int sourceLineNumber = 0);

        /// <summary>
        /// 开始一个逻辑操作上下文
        /// </summary>
        IDisposable BeginScope(string messageTemplate, params object[] args);

        /// <summary>
        /// 添加全局属性
        /// </summary>
        void AddGlobalProperty(string key, object value);

        /// <summary>
        /// 移除全局属性
        /// </summary>
        void RemoveGlobalProperty(string key);

        /// <summary>
        /// 刷新日志缓冲区
        /// </summary>
        void Flush();

        /// <summary>
        /// 关闭日志服务
        /// </summary>
        void Close();
    }
}
