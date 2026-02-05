## 修复设置属性 YAML 序列化兼容性

### 发现的问题

YamlDotNet 使用属性名（Property Name）进行序列化，而当前代码中部分属性使用了 `[JsonProperty]` 特性指定了与属性名不同的名称，这会导致：

1. **StartupSettings.cs** - JsonProperty 名称多了 "is" 前缀：
   - `AutoUpdateWithSilenceStartTime` → `isAutoUpdateWithSilenceStartTime`
   - `AutoUpdateWithSilenceEndTime` → `isAutoUpdateWithSilenceEndTime`

2. **SnapshotSettings.cs** - JsonProperty 名称与属性名不一致：
   - `ScreenshotUsingMagnificationAPI` → `usingMagnificationAPI`

### 修复方案

**方案一：统一使用 CamelCase 命名（推荐）**
移除不一致的 `[JsonProperty]` 特性，让 YamlDotNet 自动使用属性名的 CamelCase 形式。

**方案二：添加 `[YamlMember]` 特性**
为需要特定名称的属性添加 YamlDotNet 的特性。

### 具体修改计划

1. **StartupSettings.cs** - 修改第 41-42 行和第 51-52 行：
   ```csharp
   // 修改前
   [JsonProperty("isAutoUpdateWithSilenceStartTime")]
   
   // 修改后（移除特性，使用属性名 camelCase）
   // 或修改为
   [JsonProperty("autoUpdateWithSilenceStartTime")]
   ```

2. **SnapshotSettings.cs** - 修改第 20-21 行：
   ```csharp
   // 修改前
   [JsonProperty("usingMagnificationAPI")]
   
   // 修改后（统一为属性名 camelCase）
   [JsonProperty("screenshotUsingMagnificationAPI")]
   ```

3. **SettingsService.cs** - 确保序列化器配置正确：
   - 使用 `CamelCaseNamingConvention` 确保属性名首字母小写
   - 添加 `IgnoreUnmatchedProperties()` 防止未知属性导致错误

### 向后兼容性考虑

- YAML 文件是新生成的，不存在旧版 YAML 兼容问题
- JSON 迁移时，由于 SettingsService 使用 Newtonsoft.Json 读取旧文件，JsonProperty 仍然有效
- 迁移到 YAML 后，YamlDotNet 不使用 JsonProperty，所以命名必须一致

请确认此方案后，我将实施具体的代码修改。