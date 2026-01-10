using System;
using System.Linq;
using Xunit;

namespace InkCanvasForClass.Tests.Properties
{
    /// <summary>
    /// 示例属性测试类
    /// 演示如何编写属性测试（Property-Based Tests）
    /// 
    /// 注意：实际的属性测试将使用 FsCheck 库
    /// 这里提供基本示例，实际实现时需要使用 FsCheck.Xunit 的 [Property] 特性
    /// </summary>
    public class ExampleProperties
    {
        /// <summary>
        /// 示例：基本的参数化测试
        /// 实际的属性测试会使用 FsCheck 生成随机输入
        /// </summary>
        [Theory]
        [InlineData(1, 2)]
        [InlineData(5, 10)]
        [InlineData(-3, 7)]
        public void Addition_IsCommutative_Example(int a, int b)
        {
            // 加法交换律：a + b = b + a
            var result1 = a + b;
            var result2 = b + a;
            Assert.Equal(result1, result2);
        }

        /// <summary>
        /// 示例：字符串反转测试
        /// </summary>
        [Theory]
        [InlineData("hello")]
        [InlineData("test")]
        [InlineData("")]
        public void StringReverse_TwiceIsIdentity_Example(string str)
        {
            // 反转两次应该返回原值
            var reversed = new string(str.ToCharArray().Reverse().ToArray());
            var reversedTwice = new string(reversed.ToCharArray().Reverse().ToArray());
            
            Assert.Equal(str, reversedTwice);
        }

        /// <summary>
        /// 示例：列表映射保持长度
        /// </summary>
        [Fact]
        public void ListMap_PreservesLength_Example()
        {
            // 映射操作不改变列表长度
            var list = new[] { 1, 2, 3, 4, 5 };
            var originalLength = list.Length;
            var mapped = list.Select(x => x * 2).ToArray();
            var mappedLength = mapped.Length;
            
            Assert.Equal(originalLength, mappedLength);
        }
    }
}
