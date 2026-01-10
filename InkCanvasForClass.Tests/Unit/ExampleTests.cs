using System;
using Xunit;

namespace InkCanvasForClass.Tests.Unit
{
    /// <summary>
    /// 示例单元测试类
    /// 演示如何编写单元测试
    /// </summary>
    public class ExampleTests
    {
        [Fact]
        public void Example_SimpleTest_Passes()
        {
            // Arrange
            var expected = 4;

            // Act
            var actual = 2 + 2;

            // Assert
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(1, 1, 2)]
        [InlineData(2, 3, 5)]
        [InlineData(-1, 1, 0)]
        public void Example_ParameterizedTest_CalculatesCorrectly(int a, int b, int expected)
        {
            // Act
            var actual = a + b;

            // Assert
            Assert.Equal(expected, actual);
        }
    }
}
