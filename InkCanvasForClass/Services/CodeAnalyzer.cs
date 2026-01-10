using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Ink_Canvas.Helpers;

namespace Ink_Canvas.Services
{
    /// <summary>
    /// 代码分析器实现 - 静态代码分析工具
    /// </summary>
    public class CodeAnalyzer : ICodeAnalyzer
    {
        // 常见的非魔法数字
        private static readonly HashSet<string> CommonNonMagicNumbers = new HashSet<string>
        {
            "0", "1", "-1", "2", "10", "100", "1000"
        };

        /// <summary>
        /// 查找超过指定行数的方法
        /// </summary>
        /// <param name="filePath">C# 源代码文件路径</param>
        /// <param name="lineThreshold">行数阈值（默认 50）</param>
        /// <returns>超过阈值的方法列表</returns>
        public IEnumerable<MethodInfo> FindLongMethods(string filePath, int lineThreshold = 50)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                LogHelper.WriteLogToFile("FindLongMethods: File path is null or empty", LogHelper.LogType.Error);
                return Enumerable.Empty<MethodInfo>();
            }

            if (!File.Exists(filePath))
            {
                LogHelper.WriteLogToFile($"FindLongMethods: File does not exist: {filePath}", LogHelper.LogType.Warning);
                return Enumerable.Empty<MethodInfo>();
            }

            try
            {
                var lines = File.ReadAllLines(filePath);
                var methods = new List<MethodInfo>();
                var fileName = Path.GetFileName(filePath);

                // 方法声明的正则表达式
                // 匹配: [访问修饰符] [修饰符] 返回类型 方法名(参数)
                var methodPattern = @"^\s*(public|private|protected|internal|static|virtual|override|async|sealed)*\s+[\w<>\[\],\s]+\s+(\w+)\s*\(([^)]*)\)";
                var methodRegex = new Regex(methodPattern, RegexOptions.Multiline);

                // 状态机变量用于跟踪方法解析
                int currentMethodStart = -1;  // 当前方法的起始行号
                string currentMethodName = null;  // 当前方法名
                int currentMethodParams = 0;  // 当前方法的参数数量
                int braceDepth = 0;  // 大括号嵌套深度，用于确定方法边界
                bool inMethod = false;  // 是否正在解析方法体内部

                // 逐行扫描源代码文件
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    var trimmedLine = line.Trim();

                    // 跳过空行和注释行，这些不计入方法行数
                    if (string.IsNullOrWhiteSpace(trimmedLine) || 
                        trimmedLine.StartsWith("//") || 
                        trimmedLine.StartsWith("/*") ||
                        trimmedLine.StartsWith("*"))
                    {
                        continue;
                    }

                    // 检测方法声明的开始
                    // 使用正则表达式匹配方法签名（访问修饰符 + 返回类型 + 方法名 + 参数列表）
                    var match = methodRegex.Match(line);
                    if (match.Success && !inMethod)
                    {
                        currentMethodName = match.Groups[2].Value;
                        currentMethodStart = i + 1; // 行号从 1 开始
                        
                        // 计算参数数量：通过逗号分隔参数列表
                        var parameters = match.Groups[3].Value;
                        currentMethodParams = string.IsNullOrWhiteSpace(parameters) ? 0 : 
                            parameters.Split(',').Count(p => !string.IsNullOrWhiteSpace(p));
                        
                        inMethod = true;
                        braceDepth = 0;
                    }

                    if (inMethod)
                    {
                        // 跟踪大括号嵌套深度
                        // 每个 '{' 增加深度，每个 '}' 减少深度
                        // 当深度回到 0 时，表示方法结束
                        braceDepth += line.Count(c => c == '{');
                        braceDepth -= line.Count(c => c == '}');

                        // 方法结束：大括号深度回到 0 且当前行包含 '}'
                        if (braceDepth == 0 && line.Contains('}'))
                        {
                            int methodEndLine = i + 1;
                            int lineCount = methodEndLine - currentMethodStart + 1;

                            // 计算实际代码行数（排除空行、注释和单独的大括号）
                            // 这样可以更准确地衡量方法的复杂度
                            int actualLineCount = 0;
                            for (int j = currentMethodStart - 1; j <= i; j++)
                            {
                                var codeLine = lines[j].Trim();
                                if (!string.IsNullOrWhiteSpace(codeLine) && 
                                    !codeLine.StartsWith("//") &&
                                    !codeLine.StartsWith("/*") &&
                                    !codeLine.StartsWith("*") &&
                                    codeLine != "{" &&
                                    codeLine != "}")
                                {
                                    actualLineCount++;
                                }
                            }

                            // 如果方法行数超过阈值，记录为"长方法"
                            if (actualLineCount > lineThreshold)
                            {
                                methods.Add(new MethodInfo
                                {
                                    FileName = fileName,
                                    MethodName = currentMethodName,
                                    LineCount = actualLineCount,
                                    StartLine = currentMethodStart,
                                    ParameterCount = currentMethodParams
                                });

                                LogHelper.WriteLogToFile(
                                    $"FindLongMethods: Found long method '{currentMethodName}' in {fileName} " +
                                    $"({actualLineCount} lines, starts at line {currentMethodStart})",
                                    LogHelper.LogType.Info);
                            }

                            // 重置状态机，准备解析下一个方法
                            inMethod = false;
                            currentMethodName = null;
                            currentMethodStart = -1;
                        }
                    }
                }

                LogHelper.WriteLogToFile($"FindLongMethods: Found {methods.Count} long methods in {filePath}", LogHelper.LogType.Info);
                return methods;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"FindLongMethods: Error analyzing file {filePath}: {ex.Message}\n{ex.StackTrace}", LogHelper.LogType.Error);
                return Enumerable.Empty<MethodInfo>();
            }
        }

        /// <summary>
        /// 查找代码中的魔法数字
        /// </summary>
        /// <param name="filePath">C# 源代码文件路径</param>
        /// <returns>魔法数字出现位置列表</returns>
        public IEnumerable<MagicNumberOccurrence> FindMagicNumbers(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                LogHelper.WriteLogToFile("FindMagicNumbers: File path is null or empty", LogHelper.LogType.Error);
                return Enumerable.Empty<MagicNumberOccurrence>();
            }

            if (!File.Exists(filePath))
            {
                LogHelper.WriteLogToFile($"FindMagicNumbers: File does not exist: {filePath}", LogHelper.LogType.Warning);
                return Enumerable.Empty<MagicNumberOccurrence>();
            }

            try
            {
                var lines = File.ReadAllLines(filePath);
                var magicNumbers = new List<MagicNumberOccurrence>();
                var fileName = Path.GetFileName(filePath);

                // 匹配数字字面量（整数和浮点数）
                // 排除: 字符串内的数字、注释中的数字、十六进制颜色值
                var numberPattern = @"\b(\d+\.?\d*|\d*\.\d+)\b";
                var numberRegex = new Regex(numberPattern);

                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    var trimmedLine = line.Trim();

                    // 跳过注释行
                    if (trimmedLine.StartsWith("//") || trimmedLine.StartsWith("/*") || trimmedLine.StartsWith("*"))
                    {
                        continue;
                    }

                    // 移除行内注释
                    int commentIndex = line.IndexOf("//");
                    var codeOnly = commentIndex >= 0 ? line.Substring(0, commentIndex) : line;

                    // 跳过字符串字面量中的数字
                    var stringPattern = @"""[^""]*""";
                    codeOnly = Regex.Replace(codeOnly, stringPattern, "");

                    // 查找数字
                    var matches = numberRegex.Matches(codeOnly);
                    foreach (Match match in matches)
                    {
                        var value = match.Value;

                        // 跳过常见的非魔法数字
                        if (CommonNonMagicNumbers.Contains(value))
                        {
                            continue;
                        }

                        // 跳过数组索引和循环变量
                        if (IsArrayIndexOrLoopVariable(codeOnly, match.Index))
                        {
                            continue;
                        }

                        magicNumbers.Add(new MagicNumberOccurrence
                        {
                            FileName = fileName,
                            LineNumber = i + 1,
                            Value = value,
                            Context = trimmedLine
                        });
                    }
                }

                LogHelper.WriteLogToFile($"FindMagicNumbers: Found {magicNumbers.Count} magic numbers in {filePath}", LogHelper.LogType.Info);
                return magicNumbers;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"FindMagicNumbers: Error analyzing file {filePath}: {ex.Message}\n{ex.StackTrace}", LogHelper.LogType.Error);
                return Enumerable.Empty<MagicNumberOccurrence>();
            }
        }

        /// <summary>
        /// 检查命名约定
        /// </summary>
        /// <param name="filePath">C# 源代码文件路径</param>
        /// <returns>命名约定违规列表</returns>
        public IEnumerable<NamingViolation> CheckNamingConventions(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                LogHelper.WriteLogToFile("CheckNamingConventions: File path is null or empty", LogHelper.LogType.Error);
                return Enumerable.Empty<NamingViolation>();
            }

            if (!File.Exists(filePath))
            {
                LogHelper.WriteLogToFile($"CheckNamingConventions: File does not exist: {filePath}", LogHelper.LogType.Warning);
                return Enumerable.Empty<NamingViolation>();
            }

            try
            {
                var lines = File.ReadAllLines(filePath);
                var violations = new List<NamingViolation>();
                var fileName = Path.GetFileName(filePath);

                // 类名模式: public/internal class ClassName
                var classPattern = @"^\s*(public|internal|private|protected)?\s*(static|sealed|abstract)?\s*class\s+(\w+)";
                var classRegex = new Regex(classPattern);

                // 方法名模式: public/private 返回类型 MethodName(
                var methodPattern = @"^\s*(public|private|protected|internal)?\s*(static|virtual|override|async)?\s*[\w<>\[\]]+\s+(\w+)\s*\(";
                var methodRegex = new Regex(methodPattern);

                // 属性名模式: public/private 类型 PropertyName { get
                var propertyPattern = @"^\s*(public|private|protected|internal)?\s*(static|virtual|override)?\s*[\w<>\[\]]+\s+(\w+)\s*\{";
                var propertyRegex = new Regex(propertyPattern);

                // 字段名模式: private/public 类型 fieldName;
                var fieldPattern = @"^\s*(public|private|protected|internal)?\s*(static|readonly|const)?\s*[\w<>\[\]]+\s+(\w+)\s*[;=]";
                var fieldRegex = new Regex(fieldPattern);

                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    var trimmedLine = line.Trim();

                    // 跳过注释
                    if (trimmedLine.StartsWith("//") || trimmedLine.StartsWith("/*") || trimmedLine.StartsWith("*"))
                    {
                        continue;
                    }

                    // 检查类名
                    var classMatch = classRegex.Match(line);
                    if (classMatch.Success)
                    {
                        var className = classMatch.Groups[3].Value;
                        if (!IsPascalCase(className))
                        {
                            violations.Add(new NamingViolation
                            {
                                FileName = fileName,
                                LineNumber = i + 1,
                                IdentifierName = className,
                                IdentifierType = "Class",
                                Violation = "Class names should use PascalCase",
                                SuggestedName = ToPascalCase(className)
                            });
                        }
                    }

                    // 检查方法名
                    var methodMatch = methodRegex.Match(line);
                    if (methodMatch.Success)
                    {
                        var methodName = methodMatch.Groups[3].Value;
                        // 跳过构造函数和特殊方法
                        if (!methodName.StartsWith("_") && !IsPascalCase(methodName))
                        {
                            violations.Add(new NamingViolation
                            {
                                FileName = fileName,
                                LineNumber = i + 1,
                                IdentifierName = methodName,
                                IdentifierType = "Method",
                                Violation = "Method names should use PascalCase",
                                SuggestedName = ToPascalCase(methodName)
                            });
                        }
                    }

                    // 检查属性名
                    var propertyMatch = propertyRegex.Match(line);
                    if (propertyMatch.Success && !line.Contains("(")) // 排除方法
                    {
                        var propertyName = propertyMatch.Groups[3].Value;
                        if (!IsPascalCase(propertyName))
                        {
                            violations.Add(new NamingViolation
                            {
                                FileName = fileName,
                                LineNumber = i + 1,
                                IdentifierName = propertyName,
                                IdentifierType = "Property",
                                Violation = "Property names should use PascalCase",
                                SuggestedName = ToPascalCase(propertyName)
                            });
                        }
                    }

                    // 检查字段名
                    var fieldMatch = fieldRegex.Match(line);
                    if (fieldMatch.Success && line.Contains("private"))
                    {
                        var fieldName = fieldMatch.Groups[3].Value;
                        // 私有字段应该使用 _camelCase 或 camelCase
                        if (!fieldName.StartsWith("_") && !IsCamelCase(fieldName))
                        {
                            violations.Add(new NamingViolation
                            {
                                FileName = fileName,
                                LineNumber = i + 1,
                                IdentifierName = fieldName,
                                IdentifierType = "Field",
                                Violation = "Private field names should use camelCase or _camelCase",
                                SuggestedName = "_" + ToCamelCase(fieldName)
                            });
                        }
                    }
                }

                LogHelper.WriteLogToFile($"CheckNamingConventions: Found {violations.Count} naming violations in {filePath}", LogHelper.LogType.Info);
                return violations;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"CheckNamingConventions: Error analyzing file {filePath}: {ex.Message}\n{ex.StackTrace}", LogHelper.LogType.Error);
                return Enumerable.Empty<NamingViolation>();
            }
        }

        /// <summary>
        /// 查找未使用的代码（死代码）
        /// </summary>
        /// <param name="projectPath">项目路径或解决方案路径</param>
        /// <returns>未使用的代码元素列表</returns>
        public IEnumerable<UnusedCodeElement> FindDeadCode(string projectPath)
        {
            if (string.IsNullOrWhiteSpace(projectPath))
            {
                LogHelper.WriteLogToFile("FindDeadCode: Project path is null or empty", LogHelper.LogType.Error);
                return Enumerable.Empty<UnusedCodeElement>();
            }

            try
            {
                var unusedElements = new List<UnusedCodeElement>();

                // 简化实现：查找未使用的 using 语句
                // 完整的死代码检测需要 Roslyn 进行语义分析
                var csFiles = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories)
                    .Where(f => !f.Contains("\\obj\\") && !f.Contains("\\bin\\"))
                    .ToList();

                foreach (var file in csFiles)
                {
                    var lines = File.ReadAllLines(file);
                    var fileName = Path.GetFileName(file);
                    var fileContent = File.ReadAllText(file);

                    for (int i = 0; i < lines.Length; i++)
                    {
                        var line = lines[i].Trim();

                        // 检查未使用的 using 语句
                        if (line.StartsWith("using ") && !line.Contains("="))
                        {
                            var usingMatch = Regex.Match(line, @"using\s+([\w\.]+);");
                            if (usingMatch.Success)
                            {
                                var namespaceName = usingMatch.Groups[1].Value;
                                var lastPart = namespaceName.Split('.').Last();

                                // 简单检查：如果命名空间的最后部分在文件中没有其他出现，可能未使用
                                var occurrences = Regex.Matches(fileContent, @"\b" + Regex.Escape(lastPart) + @"\b").Count;
                                if (occurrences == 1) // 只在 using 语句中出现
                                {
                                    unusedElements.Add(new UnusedCodeElement
                                    {
                                        FileName = fileName,
                                        LineNumber = i + 1,
                                        ElementName = namespaceName,
                                        ElementType = "Using"
                                    });
                                }
                            }
                        }
                    }
                }

                LogHelper.WriteLogToFile($"FindDeadCode: Found {unusedElements.Count} potentially unused elements in {projectPath}", LogHelper.LogType.Info);
                return unusedElements;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"FindDeadCode: Error analyzing project {projectPath}: {ex.Message}\n{ex.StackTrace}", LogHelper.LogType.Error);
                return Enumerable.Empty<UnusedCodeElement>();
            }
        }

        #region Helper Methods

        /// <summary>
        /// 检查是否为数组索引或循环变量
        /// </summary>
        private bool IsArrayIndexOrLoopVariable(string code, int position)
        {
            // 检查前后字符
            if (position > 0 && code[position - 1] == '[')
                return true;

            // 检查是否在 for 循环中
            var beforeNumber = code.Substring(0, position);
            if (beforeNumber.Contains("for") && beforeNumber.Contains("<") || beforeNumber.Contains("<="))
                return true;

            return false;
        }

        /// <summary>
        /// 检查是否为 PascalCase
        /// </summary>
        private bool IsPascalCase(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            return char.IsUpper(name[0]) && !name.Contains("_");
        }

        /// <summary>
        /// 检查是否为 camelCase
        /// </summary>
        private bool IsCamelCase(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            return char.IsLower(name[0]) && !name.Contains("_");
        }

        /// <summary>
        /// 转换为 PascalCase
        /// </summary>
        private string ToPascalCase(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            // 移除下划线并大写每个单词的首字母
            var parts = name.Split('_');
            var result = string.Join("", parts.Select(p => 
                string.IsNullOrEmpty(p) ? "" : char.ToUpper(p[0]) + p.Substring(1)));

            return result;
        }

        /// <summary>
        /// 转换为 camelCase
        /// </summary>
        private string ToCamelCase(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            var pascal = ToPascalCase(name);
            return char.ToLower(pascal[0]) + pascal.Substring(1);
        }

        #endregion
    }
}
