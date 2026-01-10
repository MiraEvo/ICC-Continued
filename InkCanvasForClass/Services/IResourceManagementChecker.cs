using System.Collections.Generic;

namespace Ink_Canvas.Services
{
    /// <summary>
    /// COM 对象释放问题
    /// </summary>
    public class ComReleaseIssue
    {
        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 变量名
        /// </summary>
        public string VariableName { get; set; }

        /// <summary>
        /// 是否有释放调用
        /// </summary>
        public bool HasReleaseCall { get; set; }

        /// <summary>
        /// 行号
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// 问题描述
        /// </summary>
        public string Description { get; set; }
    }

    /// <summary>
    /// 事件订阅问题
    /// </summary>
    public class EventSubscriptionIssue
    {
        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 事件名称
        /// </summary>
        public string EventName { get; set; }

        /// <summary>
        /// 订阅行号
        /// </summary>
        public int SubscriptionLineNumber { get; set; }

        /// <summary>
        /// 是否有取消订阅
        /// </summary>
        public bool HasUnsubscribe { get; set; }

        /// <summary>
        /// 取消订阅行号（如果存在）
        /// </summary>
        public int? UnsubscribeLineNumber { get; set; }

        /// <summary>
        /// 问题描述
        /// </summary>
        public string Description { get; set; }
    }

    /// <summary>
    /// Timer 释放问题
    /// </summary>
    public class TimerDisposeIssue
    {
        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Timer 变量名
        /// </summary>
        public string TimerName { get; set; }

        /// <summary>
        /// 声明行号
        /// </summary>
        public int DeclarationLineNumber { get; set; }

        /// <summary>
        /// 是否有 Stop 调用
        /// </summary>
        public bool HasStopCall { get; set; }

        /// <summary>
        /// 是否有 Dispose 调用
        /// </summary>
        public bool HasDisposeCall { get; set; }

        /// <summary>
        /// 问题描述
        /// </summary>
        public string Description { get; set; }
    }

    /// <summary>
    /// 资源管理检查器接口 - 检查资源管理和内存泄漏问题
    /// </summary>
    public interface IResourceManagementChecker
    {
        /// <summary>
        /// 检查 COM 对象释放
        /// </summary>
        /// <param name="filePath">C# 源代码文件路径</param>
        /// <returns>COM 对象释放问题列表</returns>
        IEnumerable<ComReleaseIssue> CheckComObjectRelease(string filePath);

        /// <summary>
        /// 检查事件订阅/取消订阅
        /// </summary>
        /// <param name="filePath">C# 源代码文件路径</param>
        /// <returns>事件订阅问题列表</returns>
        IEnumerable<EventSubscriptionIssue> CheckEventSubscriptions(string filePath);

        /// <summary>
        /// 检查 Timer 释放
        /// </summary>
        /// <param name="filePath">C# 源代码文件路径</param>
        /// <returns>Timer 释放问题列表</returns>
        IEnumerable<TimerDisposeIssue> CheckTimerDisposal(string filePath);
    }
}
