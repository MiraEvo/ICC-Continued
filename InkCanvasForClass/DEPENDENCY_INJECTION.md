# 依赖注入配置文档

## 概述

本应用使用 Microsoft.Extensions.DependencyInjection 进行依赖注入管理。所有服务在 `App.xaml.cs` 的 `ConfigureServices()` 方法中集中注册。

## 服务生命周期

### Singleton（单例）
- **定义**: 应用程序生命周期内只创建一次，所有请求共享同一实例
- **使用场景**: 
  - 无状态服务
  - 需要在整个应用中共享状态的服务
  - 性能敏感的服务（避免重复创建）

### Transient（瞬态）
- **定义**: 每次请求都创建新实例
- **使用场景**: 
  - 轻量级、无状态的服务
  - 每次使用都需要全新状态的服务
- **注意**: 本应用暂未使用此生命周期

### Scoped（作用域）
- **定义**: 在同一作用域内共享实例
- **使用场景**: Web 应用中的请求作用域
- **注意**: WPF 应用通常不使用此生命周期

## 已注册服务列表

### 核心服务

#### ISettingsService
- **实现**: `SettingsService`
- **生命周期**: Singleton
- **依赖**: 无
- **功能**: 管理应用程序配置的加载、保存和访问
- **注册位置**: `App.xaml.cs` Line ~70

#### ITimeMachineService
- **实现**: `TimeMachineService`
- **生命周期**: Singleton
- **依赖**: 无
- **功能**: 管理撤销/重做历史记录
- **注册位置**: `App.xaml.cs` Line ~73

#### IPageService
- **实现**: `PageService`
- **生命周期**: Singleton
- **依赖**: `ITimeMachineService` (可选，通过构造函数注入)
- **功能**: 管理画布页面的创建、导航和状态
- **注册位置**: `App.xaml.cs` Line ~76
- **注意**: 使用工厂方法注册以注入可选依赖

#### IPPTService
- **实现**: `PPTService`
- **生命周期**: Singleton
- **依赖**: 无
- **功能**: 管理 PowerPoint 集成和 COM 互操作
- **注册位置**: `App.xaml.cs` Line ~83

### 工具服务

#### IFileCleanupService
- **实现**: `FileCleanupService`
- **生命周期**: Singleton
- **依赖**: 无
- **功能**: 清理临时文件和冗余资源
- **注册位置**: `App.xaml.cs` Line ~90

#### ICodeAnalyzer
- **实现**: `CodeAnalyzer`
- **生命周期**: Singleton
- **依赖**: 无
- **功能**: 静态代码分析，识别代码质量问题
- **注册位置**: `App.xaml.cs` Line ~93

#### IResourceManagementChecker
- **实现**: `ResourceManagementChecker`
- **生命周期**: Singleton
- **依赖**: 无
- **功能**: 检查资源管理问题和内存泄漏
- **注册位置**: `App.xaml.cs` Line ~96

### ViewModels

#### SettingsViewModel
- **生命周期**: Singleton
- **依赖**: `ISettingsService` (通过构造函数注入)
- **功能**: 设置界面的视图模型
- **注册位置**: `App.xaml.cs` Line ~104

### SettingsPageViewModel
- **生命周期**: Singleton
- **依赖**: 
  - `ISettingsService` (通过构造函数注入)
  - `SettingsViewModel` (通过构造函数注入)
- **功能**: 设置页面的视图模型
- **注册位置**: `App.xaml.cs` Line ~107
- **注意**: 使用工厂方法注册以注入 SettingsViewModel

#### MainWindowViewModel
- **生命周期**: Singleton
- **依赖**: 
  - `ISettingsService` (通过构造函数注入)
  - `IPageService` (通过构造函数注入)
  - `ITimeMachineService` (通过构造函数注入)
- **功能**: 主窗口的视图模型
- **注册位置**: `App.xaml.cs` Line ~108

#### ToolbarViewModel
- **生命周期**: Singleton
- **依赖**: `ISettingsService` (通过构造函数注入)
- **功能**: 工具栏的视图模型
- **注册位置**: `App.xaml.cs` Line ~112

### 延迟注册的服务

以下服务需要在 `MainWindow` 初始化后手动注册，因为它们依赖于 UI 元素：

#### IInkCanvasService
- **实现**: `InkCanvasService`
- **依赖**: `IccInkCanvas` 实例（UI 元素）
- **功能**: 提供对 InkCanvas 的抽象操作
- **注册方式**: 在 `MainWindow` 构造函数中手动注册到 ServiceLocator

#### INotificationService
- **实现**: `NotificationService`
- **依赖**: `MainWindow` 实例（UI 元素）
- **功能**: 显示通知和消息
- **注册方式**: 在 `MainWindow` 构造函数中手动注册到 ServiceLocator

#### IScreenshotService
- **实现**: `ScreenshotService`
- **依赖**: `MainWindow` 实例（UI 元素）
- **功能**: 截图功能
- **注册方式**: 在 `MainWindow` 构造函数中手动注册到 ServiceLocator

## 服务依赖关系图

```
App.xaml.cs (ConfigureServices)
│
├─ ISettingsService (无依赖)
│  └─ SettingsViewModel
│  └─ MainWindowViewModel
│  └─ ToolbarViewModel
│
├─ ITimeMachineService (无依赖)
│  └─ IPageService
│  └─ MainWindowViewModel
│
├─ IPageService (依赖 ITimeMachineService)
│  └─ MainWindowViewModel
│
├─ IPPTService (无依赖)
│
├─ IFileCleanupService (无依赖)
│
├─ ICodeAnalyzer (无依赖)
│
└─ IResourceManagementChecker (无依赖)

MainWindow (手动注册)
│
├─ IInkCanvasService (依赖 IccInkCanvas)
│
├─ INotificationService (依赖 MainWindow)
│
└─ IScreenshotService (依赖 MainWindow)
```

## 使用指南

### 推荐方式：构造函数注入

```csharp
public class MyViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly IPageService _pageService;

    // 推荐：通过构造函数注入依赖
    public MyViewModel(ISettingsService settingsService, IPageService pageService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _pageService = pageService ?? throw new ArgumentNullException(nameof(pageService));
    }
}
```

### 不推荐方式：ServiceLocator（仅用于无法注入的场景）

```csharp
public class LegacyClass
{
    public void SomeMethod()
    {
        // 不推荐：仅在无法使用构造函数注入时使用
        var settingsService = ServiceLocator.GetRequiredService<ISettingsService>();
    }
}
```

### ServiceLocator 的合理使用场景

1. **XAML 中实例化的类**: 如果类在 XAML 中直接实例化，无法通过构造函数注入
2. **静态方法**: 静态方法无法使用实例依赖
3. **第三方库回调**: 某些第三方库的回调方法中无法注入依赖
4. **向后兼容**: 重构过程中临时使用，最终应该替换为构造函数注入

## 添加新服务的步骤

1. **定义接口**: 在 `Services` 文件夹中创建 `IMyService.cs`
2. **实现接口**: 创建 `MyService.cs` 实现接口
3. **注册服务**: 在 `App.xaml.cs` 的 `ConfigureServices()` 方法中注册
   ```csharp
   services.AddSingleton<IMyService, MyService>();
   ```
4. **更新文档**: 在本文档中添加服务说明
5. **使用服务**: 通过构造函数注入使用服务

## 测试指南

### 单元测试中的依赖注入

```csharp
[Fact]
public void TestMyViewModel()
{
    // 创建模拟服务
    var mockSettingsService = new Mock<ISettingsService>();
    mockSettingsService.Setup(s => s.Settings).Returns(new Settings());
    
    // 通过构造函数注入模拟服务
    var viewModel = new MyViewModel(mockSettingsService.Object);
    
    // 执行测试
    Assert.NotNull(viewModel);
}
```

## 常见问题

### Q: 为什么某些服务使用 ServiceLocator 而不是构造函数注入？
A: 这些服务依赖于 UI 元素（如 InkCanvas、MainWindow），必须在 UI 初始化后才能创建。我们正在逐步重构以减少 ServiceLocator 的使用。

### Q: 如何决定服务的生命周期？
A: 
- 如果服务需要在整个应用中共享状态，使用 Singleton
- 如果服务是无状态的且创建成本低，可以考虑 Transient（但本应用暂未使用）
- 大多数情况下，Singleton 是 WPF 应用的最佳选择

### Q: 可以在服务中注入 ViewModel 吗？
A: 不推荐。服务应该是底层的业务逻辑，不应该依赖于 ViewModel。如果需要通信，使用事件或消息机制。

### Q: 如何处理循环依赖？
A: 
1. 重新设计服务边界，消除循环依赖
2. 使用事件或消息机制解耦
3. 将共享逻辑提取到新的服务中

## 重构计划

### 短期目标
- ✅ 集中注册所有服务
- ✅ 文档化服务依赖关系
- ✅ 将 ViewModels 改为构造函数注入
- ✅ 移除 ViewModels 中的 ServiceLocator 使用（保留合理场景）

### 长期目标
- 减少 ServiceLocator 的使用（仅保留 XAML 实例化等合理场景）
- 将 UI 依赖的服务改为工厂模式
- 实现服务的自动注册（通过反射或源代码生成）

## 已完成的重构

### 2025-01 重构
1. **集中服务注册**: 所有服务在 `App.xaml.cs` 的 `ConfigureServices()` 方法中注册
2. **文档化依赖关系**: 创建 `DEPENDENCY_INJECTION.md` 文档
3. **ViewModels 构造函数注入**: 
   - 移除了 `MainWindowViewModel`、`SettingsViewModel`、`ToolbarViewModel` 的无参构造函数
   - 移除了 `SettingsPageViewModel` 的无参构造函数
   - 所有 ViewModels 现在都通过构造函数注入依赖
4. **ServiceLocator 使用优化**:
   - ViewModels 不再使用 ServiceLocator（除了 XAML 实例化的场景）
   - Services 完全不使用 ServiceLocator
   - 保留 ServiceLocator 仅用于合理场景（XAML 实例化、静态方法等）

## 参考资料

- [Microsoft.Extensions.DependencyInjection 文档](https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)
- [依赖注入最佳实践](https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines)
- [WPF 中的依赖注入](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/advanced/dependency-injection)
