using System;
using System.IO;
using System.Linq;
using Ink_Canvas.Services;
using Xunit;

namespace InkCanvasForClass.Tests.Unit.Services
{
    /// <summary>
    /// CodeAnalyzer 单元测试
    /// </summary>
    public class CodeAnalyzerTests : IDisposable
    {
        private readonly CodeAnalyzer _analyzer;
        private readonly string _testDirectory;

        public CodeAnalyzerTests()
        {
            _analyzer = new CodeAnalyzer();
            _testDirectory = Path.Combine(Path.GetTempPath(), "CodeAnalyzerTests_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }

        [Fact]
        public void FindLongMethods_WithLongMethod_FindsMethod()
        {
            // Arrange
            var testFile = Path.Combine(_testDirectory, "TestClass.cs");
            var code = @"
using System;

public class TestClass
{
    public void LongMethod()
    {
        var x = 1;
        var y = 2;
        var z = 3;
        var a = 4;
        var b = 5;
        var c = 6;
        var d = 7;
        var e = 8;
        var f = 9;
        var g = 10;
        var h = 11;
        var i = 12;
        var j = 13;
        var k = 14;
        var l = 15;
        var m = 16;
        var n = 17;
        var o = 18;
        var p = 19;
        var q = 20;
        var r = 21;
        var s = 22;
        var t = 23;
        var u = 24;
        var v = 25;
        var w = 26;
        var x2 = 27;
        var y2 = 28;
        var z2 = 29;
        var a2 = 30;
        var b2 = 31;
        var c2 = 32;
        var d2 = 33;
        var e2 = 34;
        var f2 = 35;
        var g2 = 36;
        var h2 = 37;
        var i2 = 38;
        var j2 = 39;
        var k2 = 40;
        var l2 = 41;
        var m2 = 42;
        var n2 = 43;
        var o2 = 44;
        var p2 = 45;
        var q2 = 46;
        var r2 = 47;
        var s2 = 48;
        var t2 = 49;
        var u2 = 50;
        var v2 = 51;
    }
}";
            File.WriteAllText(testFile, code);

            // Act
            var result = _analyzer.FindLongMethods(testFile, 50).ToList();

            // Assert
            Assert.NotEmpty(result);
            Assert.Contains(result, m => m.MethodName == "LongMethod");
        }

        [Fact]
        public void FindLongMethods_WithShortMethod_ReturnsEmpty()
        {
            // Arrange
            var testFile = Path.Combine(_testDirectory, "TestClass.cs");
            var code = @"
using System;

public class TestClass
{
    public void ShortMethod()
    {
        var x = 1;
        var y = 2;
        Console.WriteLine(x + y);
    }
}";
            File.WriteAllText(testFile, code);

            // Act
            var result = _analyzer.FindLongMethods(testFile, 50).ToList();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void FindMagicNumbers_WithMagicNumbers_FindsThem()
        {
            // Arrange
            var testFile = Path.Combine(_testDirectory, "TestClass.cs");
            var code = @"
using System;

public class TestClass
{
    public void MethodWithMagicNumbers()
    {
        var width = 1920;
        var height = 1080;
        var timeout = 5000;
    }
}";
            File.WriteAllText(testFile, code);

            // Act
            var result = _analyzer.FindMagicNumbers(testFile).ToList();

            // Assert
            Assert.NotEmpty(result);
            Assert.Contains(result, m => m.Value == "1920");
            Assert.Contains(result, m => m.Value == "1080");
            Assert.Contains(result, m => m.Value == "5000");
        }

        [Fact]
        public void FindMagicNumbers_WithCommonNumbers_IgnoresThem()
        {
            // Arrange
            var testFile = Path.Combine(_testDirectory, "TestClass.cs");
            var code = @"
using System;

public class TestClass
{
    public void MethodWithCommonNumbers()
    {
        var x = 0;
        var y = 1;
        var z = -1;
        var a = 2;
    }
}";
            File.WriteAllText(testFile, code);

            // Act
            var result = _analyzer.FindMagicNumbers(testFile).ToList();

            // Assert
            // Should not find 0, 1, -1, 2 as they are common non-magic numbers
            Assert.Empty(result);
        }

        [Fact]
        public void CheckNamingConventions_WithPascalCaseClass_NoViolations()
        {
            // Arrange
            var testFile = Path.Combine(_testDirectory, "TestClass.cs");
            var code = @"
using System;

public class TestClass
{
    public void TestMethod()
    {
    }
}";
            File.WriteAllText(testFile, code);

            // Act
            var result = _analyzer.CheckNamingConventions(testFile).ToList();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void CheckNamingConventions_WithBadClassName_FindsViolation()
        {
            // Arrange
            var testFile = Path.Combine(_testDirectory, "testClass.cs");
            var code = @"
using System;

public class testClass
{
}";
            File.WriteAllText(testFile, code);

            // Act
            var result = _analyzer.CheckNamingConventions(testFile).ToList();

            // Assert
            Assert.NotEmpty(result);
            Assert.Contains(result, v => v.IdentifierName == "testClass" && v.IdentifierType == "Class");
        }

        [Fact]
        public void FindDeadCode_WithUnusedUsing_FindsIt()
        {
            // Arrange
            var testFile = Path.Combine(_testDirectory, "TestClass.cs");
            var code = @"
using System;
using System.Collections.Generic;
using System.Linq;

public class TestClass
{
    public void TestMethod()
    {
        Console.WriteLine(""Hello"");
    }
}";
            File.WriteAllText(testFile, code);

            // Act
            var result = _analyzer.FindDeadCode(_testDirectory).ToList();

            // Assert
            // Should find unused using statements
            Assert.NotEmpty(result);
        }

        [Fact]
        public void FindLongMethods_WithNullPath_ReturnsEmpty()
        {
            // Act
            var result = _analyzer.FindLongMethods(null).ToList();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void FindLongMethods_WithNonExistentFile_ReturnsEmpty()
        {
            // Act
            var result = _analyzer.FindLongMethods("nonexistent.cs").ToList();

            // Assert
            Assert.Empty(result);
        }
    }
}
