#[ MW_ShapeDrawing.cs]() 重构计划

## 重构状态: ✅ 集成已完成

**最后更新**: 2026-01-03

### 已完成的工作

1. **核心架构** (`InkCanvasForClass/ShapeDrawing/Core/`)
   - `ShapeDrawingType.cs` - 形状类型枚举（与原始 drawingShapeMode 完全对应）
   - `ShapeDrawingContext.cs` - 绘制上下文类
   - `IShapeDrawer.cs` - 形状绘制器接口
   - `BaseShapeDrawer.cs` - 抽象基类（含通用绘制方法）
   - `ShapeDrawerFactory.cs` - 工厂类（含正确的魔法数字映射）
   - `ShapeDrawingService.cs` - 服务类（统一入口）

2. **形状绘制器实现** (`InkCanvasForClass/ShapeDrawing/Bindables/`)
   - `LineShapeDrawer.cs` - 线条类
     - `LineShapeDrawer` - 直线 (mode=1)
     - `DashedLineShapeDrawer` - 虚线 (mode=8)
     - `DottedLineShapeDrawer` - 点线 (mode=18)
     - `ArrowOneSideShapeDrawer` - 单向箭头 (mode=2)
     - `ArrowTwoSideShapeDrawer` - 双向箭头
     - `ParallelLineShapeDrawer` - 4条平行线 (mode=15)
   
   - `GeometryShapeDrawer.cs` - 几何形状类
     - `RectangleShapeDrawer` - 矩形 (mode=3)
     - `RectangleCenterShapeDrawer` - 中心矩形 (mode=19)
     - `EllipseShapeDrawer` - 椭圆 (mode=4)
     - `CircleShapeDrawer` - 圆形 (mode=5)
     - `CenterCircleShapeDrawer` - 中心圆形 (新增)
     - `CenterCircleWithRadiusShapeDrawer` - 中心圆形带半径 (新增)
     - `CenterEllipseShapeDrawer` - 中心椭圆 (mode=16)
     - `CenterEllipseWithFocalPointShapeDrawer` - 中心椭圆带焦点 (mode=23)
     - `DashedCircleShapeDrawer` - 虚线圆 (mode=10)
   
   - `CoordinateShapeDrawer.cs` - 坐标轴类（与原始代码完全一致）
     - `Coordinate1ShapeDrawer` - 双向延伸坐标系 (mode=11)
     - `Coordinate2ShapeDrawer` - X轴单向 (mode=12)
     - `Coordinate3ShapeDrawer` - Y轴单向 (mode=13)
     - `Coordinate4ShapeDrawer` - 双单向 (mode=14)
     - `Coordinate5ShapeDrawer` - 3D坐标轴 (mode=17)
   
   - `CurveShapeDrawer.cs` - 曲线类
     - `Parabola1ShapeDrawer` - 抛物线 y=ax² (mode=20)
     - `Parabola2ShapeDrawer` - 抛物线 x=ay² (mode=21)
     - `ParabolaWithFocalPointShapeDrawer` - 带焦点抛物线 (mode=22)
     - `HyperbolaShapeDrawer` - 双曲线 (mode=24)
     - `HyperbolaWithFocalPointShapeDrawer` - 带焦点双曲线 (mode=25)
   
   - `Shape3DDrawer.cs` - 3D形状类
     - `CylinderShapeDrawer` - 圆柱体 (mode=6)
     - `ConeShapeDrawer` - 圆锥体 (mode=7)
     - `CuboidShapeDrawer` - 长方体 (mode=9)

3. **集成文件** (`InkCanvasForClass/MainWindow_cs/`)
   - `MW_ShapeDrawingRefactored.cs` - 重构后的 partial class（使用类型别名解决冲突）
   - `MW_ShapeDrawing.cs` - 已集成渐进式切换逻辑

4. **渐进式切换机制** ✅ 已完成
   - 在 `MouseTouchMove()` 方法中添加了 `TryDrawShapeWithRefactoredSystem()` 方法
   - 新系统优先处理，如果失败则自动回退到旧的 switch 语句
   - 多步绘制形状（立方体、双曲线）暂时使用旧系统处理
   - 添加了 `_useRefactoredShapeDrawing` 开关，可以控制是否启用新系统

### 待完成的工作

1. ⏳ 编写单元测试
2. ⏳ 验证所有形状绘制的正确性
3. ⏳ 完善多步绘制形状的新系统支持（立方体、双曲线）
4. ⏳ 移除旧的重复代码（在充分测试后）

---

## 1. 问题分析

### 1.1 当前代码问题

#### 旧系统 (MW_ShapeDrawing.cs)
- **巨大的 switch 语句**：[`MouseTouchMove()`](InkCanvasForClass/MainWindow_cs/MW_ShapeDrawing.cs:441) 方法超过 700 行，包含 25+ 个 case 分支
- **魔法数字**：使用 `drawingShapeMode = 1, 2, 3...` 而不是枚举
- **代码重复**：每个 case 分支有相似的模式（设置提交类型、生成笔画、移除/添加临时笔画）
- **异常处理不当**：多处空 catch 块

#### 新系统 V2 (MW_ShapeDrawingCore.cs)
- 已使用 [`ShapeDrawingType`](InkCanvasForClass/MainWindow_cs/MW_ShapeDrawing.cs:1580) 枚举
- [`DrawShapeCore()`](InkCanvasForClass/MainWindow_cs/MW_ShapeDrawingCore.cs:12) 方法结构较好，但仍使用 if-else 链
- 与旧系统存在功能重叠

### 1.2 魔法数字与枚举映射

| 魔法数字 | 形状类型 | 对应枚举值 | 状态 |
|---------|---------|-----------|------|
| 1 | 直线 | Line | ✅ |
| 2 | 箭头线 | ArrowOneSide | ✅ |
| 3 | 矩形 | Rectangle | ✅ |
| 4 | 椭圆 | Ellipse | ✅ |
| 5 | 圆形 | Circle | ✅ |
| 6 | 圆柱体 | Cylinder | ✅ |
| 7 | 圆锥体 | Cone | ✅ |
| 8 | 虚线 | DashedLine | ✅ |
| 9 | 立方体 | Cuboid | ✅ |
| 10 | 虚线圆 | DashedCircle | ✅ |
| 11 | 坐标轴1 | Coordinate1 | ✅ |
| 12 | 坐标轴2 | Coordinate2 | ✅ |
| 13 | 坐标轴3 | Coordinate3 | ✅ |
| 14 | 坐标轴4 | Coordinate4 | ✅ |
| 15 | 平行线（4条）| ParallelLine | ✅ |
| 16 | 中心椭圆 | CenterEllipse | ✅ |
| 17 | 3D坐标轴 | Coordinate5 | ✅ |
| 18 | 点线 | DottedLine | ✅ |
| 19 | 中心矩形 | RectangleCenter | ✅ |
| 20 | 抛物线1 | Parabola1 | ✅ |
| 21 | 抛物线2 | Parabola2 | ✅ |
| 22 | 带焦点抛物线 | ParabolaWithFocalPoint | ✅ |
| 23 | 带焦点椭圆 | CenterEllipseWithFocalPoint | ✅ |
| 24 | 双曲线 | Hyperbola | ✅ |
| 25 | 双曲线带焦点 | HyperbolaWithFocalPoint | ✅ |

## 2. 重构架构设计

### 2.1 策略模式架构

```
┌─────────────────────────────────────────────────────────────────┐
│                         MainWindow                               │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │                   ShapeDrawingManager                        ││
│  │  ┌─────────────────┐  ┌──────────────────────────────────┐  ││
│  │  │ ShapeDrawerFactory│  │ Dictionary<ShapeType, IShapeDrawer>│ ││
│  │  └─────────────────┘  └──────────────────────────────────┘  ││
│  └─────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│                      IShapeDrawer (接口)                         │
│  + Draw(context: ShapeDrawingContext): StrokeCollection          │
│  + ShapeType: ShapeDrawingType                                   │
│  + SupportsMultiStep: bool                                       │
└─────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┼───────────────┐
                    ▼               ▼               ▼
        ┌───────────────┐  ┌───────────────┐  ┌───────────────┐
        │ BaseShapeDrawer│  │LineShapeDrawer│  │AxisShapeDrawer│
        │   (抽象基类)    │  │   (线条类)     │  │  (坐标轴类)   │
        └───────────────┘  └───────────────┘  └───────────────┘
```

### 2.2 核心接口定义

```csharp
// 形状绘制上下文，封装绘制所需的所有参数
public class ShapeDrawingContext
{
    public Point StartPoint { get; set; }
    public Point EndPoint { get; set; }
    public DrawingAttributes DrawingAttributes { get; set; }
    public InkCanvas InkCanvas { get; set; }
    public int CurrentStep { get; set; }  // 用于多步绘制
    public Dictionary<string, object> ExtraData { get; set; }  // 扩展数据
}

// 形状绘制器接口
public interface IShapeDrawer
{
    ShapeDrawingType ShapeType { get; }
    bool SupportsMultiStep { get; }
    int TotalSteps { get; }
    StrokeCollection Draw(ShapeDrawingContext context);
    void Reset();  // 重置多步状态
}

// 抽象基类，提供通用实现
public abstract class BaseShapeDrawer : IShapeDrawer
{
    public abstract ShapeDrawingType ShapeType { get; }
    public virtual bool SupportsMultiStep => false;
    public virtual int TotalSteps => 1;
    
    public abstract StrokeCollection Draw(ShapeDrawingContext context);
    
    public virtual void Reset() { }
    
    // 通用辅助方法
    protected Stroke CreateStroke(StylusPointCollection points, DrawingAttributes attrs)
    {
        return new Stroke(points) { DrawingAttributes = attrs.Clone() };
    }
    
    protected Stroke CreateLineStroke(Point start, Point end, DrawingAttributes attrs)
    {
        return ShapeDrawingHelper.GenerateLineStroke(start, end, attrs);
    }
}
```

### 2.3 形状绘制器分类

#### 2.3.1 线条类绘制器

```csharp
// 基础线条绘制器
public class LineShapeDrawer : BaseShapeDrawer
{
    public override ShapeDrawingType ShapeType => ShapeDrawingType.Line;
    
    public override StrokeCollection Draw(ShapeDrawingContext context)
    {
        var stk = new IccStroke(new StylusPointCollection {
            new StylusPoint(context.StartPoint.X, context.StartPoint.Y),
            new StylusPoint(context.EndPoint.X, context.EndPoint.Y),
        }, context.DrawingAttributes);
        stk.AddPropertyData(IccStroke.StrokeIsShapeGuid, true);
        stk.AddPropertyData(IccStroke.StrokeShapeTypeGuid, (int)ShapeType);
        return new StrokeCollection { stk };
    }
}

// 虚线绘制器
public class DashedLineShapeDrawer : LineShapeDrawer
{
    public override ShapeDrawingType ShapeType => ShapeDrawingType.DashedLine;
}

// 箭头线绘制器
public class ArrowLineShapeDrawer : LineShapeDrawer
{
    public override ShapeDrawingType ShapeType => ShapeDrawingType.ArrowOneSide;
}
```

#### 2.3.2 几何形状绘制器

```csharp
// 矩形绘制器
public class RectangleShapeDrawer : BaseShapeDrawer
{
    public override ShapeDrawingType ShapeType => ShapeDrawingType.Rectangle;
    
    public override StrokeCollection Draw(ShapeDrawingContext context)
    {
        var iniP = context.StartPoint;
        var endP = context.EndPoint;
        var pointList = new List<Point> {
            new Point(iniP.X, iniP.Y),
            new Point(iniP.X, endP.Y),
            new Point(endP.X, endP.Y),
            new Point(endP.X, iniP.Y),
            new Point(iniP.X, iniP.Y)
        };
        var stroke = CreateStroke(new StylusPointCollection(pointList), context.DrawingAttributes);
        return new StrokeCollection { stroke };
    }
}

// 椭圆绘制器
public class EllipseShapeDrawer : BaseShapeDrawer
{
    public override ShapeDrawingType ShapeType => ShapeDrawingType.Ellipse;
    
    public override StrokeCollection Draw(ShapeDrawingContext context)
    {
        var pointList = ShapeDrawingHelper.GenerateEllipseGeometry(context.StartPoint, context.EndPoint);
        var stroke = CreateStroke(new StylusPointCollection(pointList), context.DrawingAttributes);
        return new StrokeCollection { stroke };
    }
}
```

#### 2.3.3 多步绘制器（如双曲线、立方体）

```csharp
// 立方体绘制器 - 需要两步绘制
public class CuboidShapeDrawer : BaseShapeDrawer
{
    private Point _frontRectIniP;
    private Point _frontRectEndP;
    private StrokeCollection _firstStepStrokes;
    
    public override ShapeDrawingType ShapeType => ShapeDrawingType.Cube;
    public override bool SupportsMultiStep => true;
    public override int TotalSteps => 2;
    
    public override StrokeCollection Draw(ShapeDrawingContext context)
    {
        if (context.CurrentStep == 0)
        {
            return DrawFirstStep(context);
        }
        else
        {
            return DrawSecondStep(context);
        }
    }
    
    private StrokeCollection DrawFirstStep(ShapeDrawingContext context)
    {
        // 绘制前面的矩形
        _frontRectIniP = context.StartPoint;
        _frontRectEndP = context.EndPoint;
        // ... 实现细节
    }
    
    private StrokeCollection DrawSecondStep(ShapeDrawingContext context)
    {
        // 根据深度绘制立方体的其他边
        // ... 实现细节
    }
    
    public override void Reset()
    {
        _frontRectIniP = default;
        _frontRectEndP = default;
        _firstStepStrokes = null;
    }
}
```

### 2.4 工厂和管理器

```csharp
// 形状绘制器工厂
public static class ShapeDrawerFactory
{
    private static readonly Dictionary<ShapeDrawingType, IShapeDrawer> _drawers;
    
    static ShapeDrawerFactory()
    {
        _drawers = new Dictionary<ShapeDrawingType, IShapeDrawer>();
        RegisterDefaultDrawers();
    }
    
    private static void RegisterDefaultDrawers()
    {
        // 线条类
        Register(new LineShapeDrawer());
        Register(new DashedLineShapeDrawer());
        Register(new DottedLineShapeDrawer());
        Register(new ArrowLineShapeDrawer());
        
        // 几何形状
        Register(new RectangleShapeDrawer());
        Register(new EllipseShapeDrawer());
        Register(new CircleShapeDrawer());
        
        // 坐标轴
        Register(new Axis2DShapeDrawer());
        Register(new Axis3DShapeDrawer());
        
        // 曲线
        Register(new HyperbolaShapeDrawer());
        Register(new ParabolaShapeDrawer());
        
        // 3D形状
        Register(new CylinderShapeDrawer());
        Register(new ConeShapeDrawer());
        Register(new CuboidShapeDrawer());
    }
    
    public static void Register(IShapeDrawer drawer)
    {
        _drawers[drawer.ShapeType] = drawer;
    }
    
    public static IShapeDrawer GetDrawer(ShapeDrawingType type)
    {
        return _drawers.TryGetValue(type, out var drawer) ? drawer : null;
    }
}

// 形状绘制管理器 - 集成到 MainWindow
public class ShapeDrawingManager
{
    private readonly InkCanvas _inkCanvas;
    private IShapeDrawer _currentDrawer;
    private Stroke _lastTempStroke;
    private StrokeCollection _lastTempStrokeCollection;
    private int _currentStep;
    
    public ShapeDrawingType? CurrentShapeType { get; private set; }
    
    public void StartDrawing(ShapeDrawingType type)
    {
        _currentDrawer = ShapeDrawerFactory.GetDrawer(type);
        _currentDrawer?.Reset();
        CurrentShapeType = type;
        _currentStep = 0;
    }
    
    public void OnMouseMove(Point startPoint, Point endPoint, DrawingAttributes attrs)
    {
        if (_currentDrawer == null) return;
        
        var context = new ShapeDrawingContext
        {
            StartPoint = startPoint,
            EndPoint = endPoint,
            DrawingAttributes = attrs,
            InkCanvas = _inkCanvas,
            CurrentStep = _currentStep
        };
        
        // 移除之前的临时笔画
        RemoveTempStrokes();
        
        // 绘制新的临时笔画
        var strokes = _currentDrawer.Draw(context);
        AddTempStrokes(strokes);
    }
    
    public void OnMouseUp()
    {
        if (_currentDrawer == null) return;
        
        if (_currentDrawer.SupportsMultiStep && _currentStep < _currentDrawer.TotalSteps - 1)
        {
            _currentStep++;
        }
        else
        {
            // 完成绘制，提交到历史记录
            CommitStrokes();
            EndDrawing();
        }
    }
    
    public void EndDrawing()
    {
        _currentDrawer?.Reset();
        _currentDrawer = null;
        CurrentShapeType = null;
        _currentStep = 0;
    }
    
    private void RemoveTempStrokes() { /* ... */ }
    private void AddTempStrokes(StrokeCollection strokes) { /* ... */ }
    private void CommitStrokes() { /* ... */ }
}
```

## 3. 文件结构规划

```
InkCanvasForClass/
├── ShapeDrawing/                          # 新建目录
│   ├── Core/
│   │   ├── IShapeDrawer.cs               # 接口定义
│   │   ├── BaseShapeDrawer.cs            # 抽象基类
│   │   ├── ShapeDrawingContext.cs        # 绘制上下文
│   │   ├── ShapeDrawerFactory.cs         # 工厂类
│   │   └── ShapeDrawingManager.cs        # 管理器
│   ├── Drawers/
│   │   ├── Lines/
│   │   │   ├── LineShapeDrawer.cs
│   │   │   ├── DashedLineShapeDrawer.cs
│   │   │   ├── DottedLineShapeDrawer.cs
│   │   │   └── ArrowLineShapeDrawer.cs
│   │   ├── Shapes/
│   │   │   ├── RectangleShapeDrawer.cs
│   │   │   ├── EllipseShapeDrawer.cs
│   │   │   └── CircleShapeDrawer.cs
│   │   ├── Axes/
│   │   │   ├── Axis2DShapeDrawer.cs
│   │   │   └── Axis3DShapeDrawer.cs
│   │   ├── Curves/
│   │   │   ├── HyperbolaShapeDrawer.cs
│   │   │   └── ParabolaShapeDrawer.cs
│   │   └── Solids/
│   │       ├── CylinderShapeDrawer.cs
│   │       ├── ConeShapeDrawer.cs
│   │       └── CuboidShapeDrawer.cs
│   └── Helpers/
│       └── ShapeDrawingHelper.cs          # 从现有代码迁移
├── MainWindow_cs/
│   ├── MW_ShapeDrawing.cs                 # 重构后仅保留入口方法
│   └── MW_ShapeDrawingCore.cs             # 保留兼容层或删除
```

## 4. 实施步骤

### 阶段一：创建基础架构 (1-3天)

1. **创建目录结构和核心接口**
   - 创建 `ShapeDrawing` 目录
   - 定义 `IShapeDrawer` 接口
   - 定义 `ShapeDrawingContext` 类
   - 创建 `BaseShapeDrawer` 抽象基类

2. **创建工厂和管理器**
   - 实现 `ShapeDrawerFactory`
   - 实现 `ShapeDrawingManager`

### 阶段二：迁移形状绘制器 (3-5天)

按照依赖顺序逐步迁移：

1. **线条类**（最简单，用于验证架构）
   - `LineShapeDrawer`
   - `DashedLineShapeDrawer`
   - `DottedLineShapeDrawer`
   - `ArrowLineShapeDrawer`

2. **基础几何形状**
   - `RectangleShapeDrawer`
   - `EllipseShapeDrawer`
   - `CircleShapeDrawer`

3. **坐标轴**
   - `Axis2DShapeDrawer` (及其变体)
   - `Axis3DShapeDrawer`

4. **曲线**
   - `ParabolaShapeDrawer`
   - `HyperbolaShapeDrawer`

5. **3D形状**（最复杂）
   - `CylinderShapeDrawer`
   - `ConeShapeDrawer`
   - `CuboidShapeDrawer`

### 阶段三：重构 MainWindow 集成 (2-3天)

1. **替换 MouseTouchMove**
   - 使用 `ShapeDrawingManager` 替代 switch 语句
   - 保持向后兼容

2. **统一枚举使用**
   - 将 `drawingShapeMode` 改为 `ShapeDrawingType?`
   - 更新所有按钮点击事件

3. **清理旧代码**
   - 删除 switch 语句中的旧实现
   - 合并或删除 `MW_ShapeDrawingCore.cs` 中的重复代码

### 阶段四：测试和优化 (1-2天)

1. 编写单元测试
2. 进行集成测试
3. 性能优化
4. 代码审查

## 5. 重构后的 MouseTouchMove 方法

```csharp
private void MouseTouchMove(Point endP) {
    if (Settings.Canvas.FitToCurve == true) 
        drawingAttributes.FitToCurve = false;
    
    ViewboxFloatingBar.IsHitTestVisible = false;
    BlackboardUIGridForInkReplay.IsHitTestVisible = false;
    
    if (_shapeDrawingManager.CurrentShapeType == null)
        return;
    
    _currentCommitType = CommitReason.ShapeDrawing;
    _shapeDrawingManager.OnMouseMove(iniP, endP, inkCanvas.DefaultDrawingAttributes);
}
```

## 6. 风险和缓解措施

| 风险 | 影响 | 缓解措施 |
|-----|------|---------|
| 功能回归 | 高 | 逐步迁移，每步都进行测试 |
| 性能下降 | 中 | 使用对象池、缓存绘制器实例 |
| 多步绘制状态管理 | 中 | 仔细设计状态机，单元测试覆盖 |
| 兼容性问题 | 低 | 保留向后兼容层 |

## 7. 预期收益

1. **可维护性**：每个形状绘制器独立，便于修改和扩展
2. **可测试性**：可以单独测试每个绘制器
3. **可读性**：消除 700+ 行的 switch 语句
4. **可扩展性**：添加新形状只需创建新绘制器并注册
5. **代码复用**：通过基类和辅助方法复用公共逻辑