using System.Collections.Generic;

namespace Ink_Canvas.Services
{
    /// <summary>
    /// 方法信息 - 包含方法的元数据
    /// </summary>
    public class MethodInfo
    {
        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 方法名
        /// </summary>
        public string MethodName { get; set; }

        /// <summary>
        /// 方法行数（不包括空行和注释）
        /// </summary>
        public int LineCount { get; set; }

        /// <summary>
        /// 方法起始行号
        /// </summary>
        public int StartLine { get; set; }

        /// <summary>
        /// 方法参数数量
        /// </summary>
        public int ParameterCount { get; set; }
    }

    /// <summary>
    /// 魔法数字出现位置
    /// </summary>
    public class MagicNumberOccurrence
    {
        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 行号
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// 数字值
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// 上下文代码
        /// </summary>
        public string Context { get; set; }
    }

    /// <summary>
    /// 命名约定违规
    /// </summary>
    public class NamingViolation
    {
        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 行号
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// 标识符名称
        /// </summary>
        public string IdentifierName { get; set; }

        /// <summary>
        /// 标识符类型（Class, Method, Property, Field 等）
        /// </summary>
        public string IdentifierType { get; set; }

        /// <summary>
        /// 违规描述
        /// </summary>
        public string Violation { get; set; }

        /// <summary>
        /// 建议的名称
        /// </summary>
        public string SuggestedName { get; set; }
    }

    /// <summary>
    /// 未使用的代码元素
    /// </summary>
    public class UnusedCodeElement
    {
        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 行号
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// 元素名称
        /// </summary>
        public string ElementName { get; set; }

        /// <summary>
        /// 元素类型（Method, Class, Field, Property 等）
        /// </summary>
        public string ElementType { get; set; }
    }

    /// <summary>
    /// 代码分析器接口 - 静态代码分析工具
    /// </summary>
    public interface ICodeAnalyzer
    {
        /// <summary>
        /// 查找超过指定行数的方法
        /// </summary>
        /// <param name="filePath">C# 源代码文件路径</param>
        /// <param name="lineThreshold">行数阈值（默认 50）</param>
        /// <returns>超过阈值的方法列表</returns>
        IEnumerable<MethodInfo> FindLongMethods(string filePath, int lineThreshold = 50);

        /// <summary>
        /// 查找代码中的魔法数字
        /// </summary>
        /// <param name="filePath">C# 源代码文件路径</param>
        /// <returns>魔法数字出现位置列表</returns>
        IEnumerable<MagicNumberOccurrence> FindMagicNumbers(string filePath);

        /// <summary>
        /// 检查命名约定
        /// </summary>
        /// <param name="filePath">C# 源代码文件路径</param>
        /// <returns>命名约定违规列表</returns>
        IEnumerable<NamingViolation> CheckNamingConventions(string filePath);

        /// <summary>
        /// 查找未使用的代码（死代码）
        /// </summary>
        /// <param name="projectPath">项目路径或解决方案路径</param>
        /// <returns>未使用的代码元素列表</returns>
        IEnumerable<UnusedCodeElement> FindDeadCode(string projectPath);
    }
}
