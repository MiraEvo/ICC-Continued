## 优化计划

基于对 [SnapshotSettings.cs](file:///c:/Users/SakuraStar/Desktop/ICC-Continued/InkCanvasForClass/Models/Settings/SnapshotSettings.cs) 的分析，我提出以下优化方案：

### 1. 添加数据验证
参考 [CanvasSettings.cs](file:///c:/Users/SakuraStar/Desktop/ICC-Continued/InkCanvasForClass/Models/Settings/CanvasSettings.cs) 的模式，为 `ScreenshotFileName` 属性添加非空验证，确保文件名模式不会设置为空字符串。

### 2. 添加辅助属性
参考 [AppearanceSettings.cs](file:///c:/Users/SakuraStar/Desktop/ICC-Continued/InkCanvasForClass/Models/Settings/AppearanceSettings.cs) 的模式，为截图文件名模式添加辅助属性，方便解析和操作文件名模板中的占位符（如 [YYYY]、[MM] 等）。

### 3. 添加默认值常量
将默认文件名模式提取为常量，便于维护和在代码其他地方引用。

### 4. 代码组织优化
- 将字段和属性分组，提高可读性
- 添加更多详细的 XML 文档注释

### 具体修改内容：

1. **添加常量定义**：
   ```csharp
   public const string DefaultScreenshotFileName = "Screenshot-[YYYY]-[MM]-[DD]-[HH]-[mm]-[ss].png";
   ```

2. **为 ScreenshotFileName 添加验证**：
   ```csharp
   set => SetProperty(ref _screenshotFileName, ValidateNotEmpty(value));
   ```

3. **添加辅助方法/属性**：
   - 添加 `GetFormattedFileName()` 方法，自动替换占位符为实际日期时间
   - 添加 `IsValidFileNamePattern` 属性，验证文件名模式是否有效

4. **代码分组**：将布尔类型设置和字符串类型设置分开，提高可读性

请确认这个优化方案后，我将开始实施具体的代码修改。