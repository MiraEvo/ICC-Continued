# InkCanvasForClass.Tests

测试项目，用于 InkCanvasForClass 应用程序的单元测试和属性测试。

## 测试框架

- **xUnit**: 主要测试框架
- **FsCheck**: 属性测试（Property-Based Testing）
- **Moq**: 模拟框架，用于创建测试替身

## 项目结构

```
InkCanvasForClass.Tests/
├── Unit/                    # 单元测试
│   ├── Services/           # 服务层测试
│   └── Helpers/            # 辅助类测试
└── Properties/             # 属性测试（Property-Based Tests）
```

## 测试策略

本项目采用**双重测试策略**：

### 1. 单元测试 (Unit Tests)
- 验证特定示例和边缘情况
- 测试错误条件和异常处理
- 位于 `Unit/` 目录

### 2. 属性测试 (Property-Based Tests)
- 验证跨所有输入的通用属性
- 每个测试至少运行 100 次迭代
- 位于 `Properties/` 目录

## 运行测试

```bash
# 运行所有测试
dotnet test

# 运行特定测试项目
dotnet test InkCanvasForClass.Tests/InkCanvasForClass.Tests.csproj

# 运行测试并生成覆盖率报告
dotnet test --collect:"XPlat Code Coverage"
```

## 测试命名约定

### 单元测试
```csharp
[Fact]
public void MethodName_Scenario_ExpectedBehavior()
{
    // Arrange
    // Act
    // Assert
}
```

### 属性测试
```csharp
[Property]
public Property PropertyName_Description()
{
    // Feature: code-optimization-cleanup, Property N: 属性描述
    return Prop.ForAll(...);
}
```

## 代码覆盖率目标

- 目标覆盖率：至少 60%
- 重点覆盖：服务层、业务逻辑、资源管理

## 依赖项

测试项目引用主项目 `InkCanvasForClass.csproj`，可以访问所有公共和内部类型。
