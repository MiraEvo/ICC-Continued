## 优化设置存储为 YAML 格式

### 1. 添加 YAML 库依赖
在 `InkCanvasForClass.csproj` 中添加 `YamlDotNet` NuGet 包：
```xml
<PackageReference Include="YamlDotNet" Version="16.3.0" />
```

### 2. 修改 SettingsService.cs
- 将文件扩展名从 `.json` 改为 `.yml`
- 添加 YAML 序列化/反序列化方法
- 保持向后兼容（自动迁移旧 JSON 设置）
- 添加注释支持（生成带说明的 YAML 文件）

### 3. 添加 YAML 注释特性
创建自定义特性用于为设置属性添加中文注释，使生成的 YAML 文件自带说明。

### 4. 生成的 YAML 文件示例
```yaml
# InkCanvasForClass 设置文件
# 生成时间: 2026-02-05

# 画布设置
canvas:
  inkWidth: 2.0                    # 墨迹宽度
  highlighterWidth: 20.0           # 荧光笔宽度
  inkAlpha: 255.0                  # 墨迹透明度
  isShowCursor: false              # 是否显示光标

# 外观设置
appearance:
  theme: 0                         # 主题 (0=浅色, 1=深色)
  isEnableDisPlayNibModeToggler: true

# 启动设置
startup:
  enableWindowChromeRendering: true
```

### 5. 文件变更清单
- **修改**: `InkCanvasForClass.csproj` - 添加 YamlDotNet 依赖
- **修改**: `Services/SettingsService.cs` - 实现 YAML 存储逻辑
- **新增**: `Helpers/YamlSettingsHelper.cs` - YAML 序列化辅助类（可选）

### 6. 向后兼容策略
- 启动时优先读取 `Settings.yml`
- 如果 YAML 不存在但 JSON 存在，自动迁移并删除旧文件
- 保留 JSON 读取能力作为降级方案

请确认此方案后，我将开始实施具体的代码修改。