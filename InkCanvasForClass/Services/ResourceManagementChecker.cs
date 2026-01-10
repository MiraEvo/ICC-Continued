using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Ink_Canvas.Helpers;

namespace Ink_Canvas.Services
{
    /// <summary>
    /// 资源管理检查器实现 - 检查资源管理和内存泄漏问题
    /// </summary>
    public class ResourceManagementChecker : IResourceManagementChecker
    {
        /// <summary>
        /// 检查 COM 对象释放
        /// </summary>
        /// <param name="filePath">C# 源代码文件路径</param>
        /// <returns>COM 对象释放问题列表</returns>
        public IEnumerable<ComReleaseIssue> CheckComObjectRelease(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                LogHelper.WriteLogToFile("CheckComObjectRelease: File path is null or empty", LogHelper.LogType.Error);
                return Enumerable.Empty<ComReleaseIssue>();
            }

            if (!File.Exists(filePath))
            {
                LogHelper.WriteLogToFile($"CheckComObjectRelease: File does not exist: {filePath}", LogHelper.LogType.Warning);
                return Enumerable.Empty<ComReleaseIssue>();
            }

            try
            {
                var lines = File.ReadAllLines(filePath);
                var issues = new List<ComReleaseIssue>();
                var fileName = Path.GetFileName(filePath);
                var fileContent = File.ReadAllText(filePath);

                // 查找可能的 COM 对象变量
                // 常见的 COM 对象类型: PowerPoint.Application, Excel.Application, Word.Application, dynamic (用于 COM)
                var comObjectPattern = @"(PowerPoint\.Application|Excel\.Application|Word\.Application|Microsoft\.Office\.Interop\.\w+\.\w+)\s+(\w+)\s*=";
                var comObjectRegex = new Regex(comObjectPattern, RegexOptions.Multiline);

                var comObjects = new Dictionary<string, int>(); // 变量名 -> 行号

                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    var trimmedLine = line.Trim();

                    // 跳过注释
                    if (trimmedLine.StartsWith("//") || trimmedLine.StartsWith("/*") || trimmedLine.StartsWith("*"))
                    {
                        continue;
                    }

                    // 查找 COM 对象声明
                    var match = comObjectRegex.Match(line);
                    if (match.Success)
                    {
                        var variableName = match.Groups[2].Value;
                        comObjects[variableName] = i + 1;
                    }
                }

                // 检查每个 COM 对象是否有 ReleaseComObject 调用
                foreach (var comObject in comObjects)
                {
                    var variableName = comObject.Key;
                    var lineNumber = comObject.Value;

                    // 查找 Marshal.ReleaseComObject 或 Marshal.FinalReleaseComObject 调用
                    var releasePattern = $@"Marshal\.(Final)?ReleaseComObject\s*\(\s*{Regex.Escape(variableName)}\s*\)";
                    var hasRelease = Regex.IsMatch(fileContent, releasePattern);

                    if (!hasRelease)
                    {
                        issues.Add(new ComReleaseIssue
                        {
                            FileName = fileName,
                            VariableName = variableName,
                            HasReleaseCall = false,
                            LineNumber = lineNumber,
                            Description = $"COM object '{variableName}' is not released with Marshal.ReleaseComObject"
                        });

                        LogHelper.WriteLogToFile(
                            $"CheckComObjectRelease: Found unreleased COM object '{variableName}' at line {lineNumber} in {fileName}",
                            LogHelper.LogType.Warning);
                    }
                }

                LogHelper.WriteLogToFile($"CheckComObjectRelease: Found {issues.Count} COM release issues in {filePath}", LogHelper.LogType.Info);
                return issues;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"CheckComObjectRelease: Error analyzing file {filePath}: {ex.Message}\n{ex.StackTrace}", LogHelper.LogType.Error);
                return Enumerable.Empty<ComReleaseIssue>();
            }
        }

        /// <summary>
        /// 检查事件订阅/取消订阅
        /// </summary>
        /// <param name="filePath">C# 源代码文件路径</param>
        /// <returns>事件订阅问题列表</returns>
        public IEnumerable<EventSubscriptionIssue> CheckEventSubscriptions(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                LogHelper.WriteLogToFile("CheckEventSubscriptions: File path is null or empty", LogHelper.LogType.Error);
                return Enumerable.Empty<EventSubscriptionIssue>();
            }

            if (!File.Exists(filePath))
            {
                LogHelper.WriteLogToFile($"CheckEventSubscriptions: File does not exist: {filePath}", LogHelper.LogType.Warning);
                return Enumerable.Empty<EventSubscriptionIssue>();
            }

            try
            {
                var lines = File.ReadAllLines(filePath);
                var issues = new List<EventSubscriptionIssue>();
                var fileName = Path.GetFileName(filePath);

                // 跟踪事件订阅和取消订阅
                var subscriptions = new Dictionary<string, List<int>>(); // 事件名 -> 订阅行号列表
                var unsubscriptions = new Dictionary<string, List<int>>(); // 事件名 -> 取消订阅行号列表

                // 事件订阅模式: something += handler
                var subscriptionPattern = @"(\w+(?:\.\w+)*)\s*\+=\s*(\w+)";
                var subscriptionRegex = new Regex(subscriptionPattern);

                // 事件取消订阅模式: something -= handler
                var unsubscriptionPattern = @"(\w+(?:\.\w+)*)\s*-=\s*(\w+)";
                var unsubscriptionRegex = new Regex(unsubscriptionPattern);

                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    var trimmedLine = line.Trim();

                    // 跳过注释
                    if (trimmedLine.StartsWith("//") || trimmedLine.StartsWith("/*") || trimmedLine.StartsWith("*"))
                    {
                        continue;
                    }

                    // 查找事件订阅
                    var subMatch = subscriptionRegex.Match(line);
                    if (subMatch.Success)
                    {
                        var eventName = subMatch.Groups[1].Value;
                        if (!subscriptions.ContainsKey(eventName))
                        {
                            subscriptions[eventName] = new List<int>();
                        }
                        subscriptions[eventName].Add(i + 1);
                    }

                    // 查找事件取消订阅
                    var unsubMatch = unsubscriptionRegex.Match(line);
                    if (unsubMatch.Success)
                    {
                        var eventName = unsubMatch.Groups[1].Value;
                        if (!unsubscriptions.ContainsKey(eventName))
                        {
                            unsubscriptions[eventName] = new List<int>();
                        }
                        unsubscriptions[eventName].Add(i + 1);
                    }
                }

                // 检查每个订阅是否有对应的取消订阅
                foreach (var subscription in subscriptions)
                {
                    var eventName = subscription.Key;
                    var subLines = subscription.Value;

                    if (!unsubscriptions.ContainsKey(eventName))
                    {
                        // 没有任何取消订阅
                        foreach (var lineNumber in subLines)
                        {
                            issues.Add(new EventSubscriptionIssue
                            {
                                FileName = fileName,
                                EventName = eventName,
                                SubscriptionLineNumber = lineNumber,
                                HasUnsubscribe = false,
                                UnsubscribeLineNumber = null,
                                Description = $"Event '{eventName}' is subscribed but never unsubscribed"
                            });

                            LogHelper.WriteLogToFile(
                                $"CheckEventSubscriptions: Found unmatched subscription for '{eventName}' at line {lineNumber} in {fileName}",
                                LogHelper.LogType.Warning);
                        }
                    }
                    else
                    {
                        var unsubLines = unsubscriptions[eventName];
                        
                        // 检查订阅和取消订阅的数量是否匹配
                        if (subLines.Count > unsubLines.Count)
                        {
                            // 有些订阅没有对应的取消订阅
                            var unmatchedCount = subLines.Count - unsubLines.Count;
                            issues.Add(new EventSubscriptionIssue
                            {
                                FileName = fileName,
                                EventName = eventName,
                                SubscriptionLineNumber = subLines.First(),
                                HasUnsubscribe = true,
                                UnsubscribeLineNumber = unsubLines.FirstOrDefault(),
                                Description = $"Event '{eventName}' has {subLines.Count} subscriptions but only {unsubLines.Count} unsubscriptions"
                            });

                            LogHelper.WriteLogToFile(
                                $"CheckEventSubscriptions: Subscription/unsubscription count mismatch for '{eventName}' in {fileName}",
                                LogHelper.LogType.Warning);
                        }
                    }
                }

                LogHelper.WriteLogToFile($"CheckEventSubscriptions: Found {issues.Count} event subscription issues in {filePath}", LogHelper.LogType.Info);
                return issues;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"CheckEventSubscriptions: Error analyzing file {filePath}: {ex.Message}\n{ex.StackTrace}", LogHelper.LogType.Error);
                return Enumerable.Empty<EventSubscriptionIssue>();
            }
        }

        /// <summary>
        /// 检查 Timer 释放
        /// </summary>
        /// <param name="filePath">C# 源代码文件路径</param>
        /// <returns>Timer 释放问题列表</returns>
        public IEnumerable<TimerDisposeIssue> CheckTimerDisposal(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                LogHelper.WriteLogToFile("CheckTimerDisposal: File path is null or empty", LogHelper.LogType.Error);
                return Enumerable.Empty<TimerDisposeIssue>();
            }

            if (!File.Exists(filePath))
            {
                LogHelper.WriteLogToFile($"CheckTimerDisposal: File does not exist: {filePath}", LogHelper.LogType.Warning);
                return Enumerable.Empty<TimerDisposeIssue>();
            }

            try
            {
                var lines = File.ReadAllLines(filePath);
                var issues = new List<TimerDisposeIssue>();
                var fileName = Path.GetFileName(filePath);
                var fileContent = File.ReadAllText(filePath);

                // 查找 Timer 变量声明
                // 支持: DispatcherTimer, System.Timers.Timer, System.Threading.Timer
                var timerPattern = @"(DispatcherTimer|System\.Timers\.Timer|System\.Threading\.Timer|Timer)\s+(\w+)\s*=";
                var timerRegex = new Regex(timerPattern, RegexOptions.Multiline);

                var timers = new Dictionary<string, int>(); // Timer 变量名 -> 行号

                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    var trimmedLine = line.Trim();

                    // 跳过注释
                    if (trimmedLine.StartsWith("//") || trimmedLine.StartsWith("/*") || trimmedLine.StartsWith("*"))
                    {
                        continue;
                    }

                    // 查找 Timer 声明
                    var match = timerRegex.Match(line);
                    if (match.Success)
                    {
                        var timerName = match.Groups[2].Value;
                        timers[timerName] = i + 1;
                    }
                }

                // 检查每个 Timer 是否有 Stop 和 Dispose 调用
                foreach (var timer in timers)
                {
                    var timerName = timer.Key;
                    var lineNumber = timer.Value;

                    // 查找 Stop 调用
                    var stopPattern = $@"{Regex.Escape(timerName)}\.Stop\s*\(\s*\)";
                    var hasStop = Regex.IsMatch(fileContent, stopPattern);

                    // 查找 Dispose 调用
                    var disposePattern = $@"{Regex.Escape(timerName)}\.Dispose\s*\(\s*\)";
                    var hasDispose = Regex.IsMatch(fileContent, disposePattern);

                    // 如果缺少 Stop 或 Dispose，报告问题
                    if (!hasStop || !hasDispose)
                    {
                        var description = "";
                        if (!hasStop && !hasDispose)
                        {
                            description = $"Timer '{timerName}' is not stopped or disposed";
                        }
                        else if (!hasStop)
                        {
                            description = $"Timer '{timerName}' is not stopped before disposal";
                        }
                        else if (!hasDispose)
                        {
                            description = $"Timer '{timerName}' is not disposed";
                        }

                        issues.Add(new TimerDisposeIssue
                        {
                            FileName = fileName,
                            TimerName = timerName,
                            DeclarationLineNumber = lineNumber,
                            HasStopCall = hasStop,
                            HasDisposeCall = hasDispose,
                            Description = description
                        });

                        LogHelper.WriteLogToFile(
                            $"CheckTimerDisposal: {description} at line {lineNumber} in {fileName}",
                            LogHelper.LogType.Warning);
                    }
                }

                LogHelper.WriteLogToFile($"CheckTimerDisposal: Found {issues.Count} timer disposal issues in {filePath}", LogHelper.LogType.Info);
                return issues;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"CheckTimerDisposal: Error analyzing file {filePath}: {ex.Message}\n{ex.StackTrace}", LogHelper.LogType.Error);
                return Enumerable.Empty<TimerDisposeIssue>();
            }
        }
    }
}
