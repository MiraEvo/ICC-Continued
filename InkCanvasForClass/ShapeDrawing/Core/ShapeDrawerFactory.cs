using System;
using System.Collections.Generic;
using Ink_Canvas.ShapeDrawing.Bindables;

namespace Ink_Canvas.ShapeDrawing.Core {
    /// <summary>
    /// 形状绘制器工厂类，负责创建和管理形状绘制器实例
    /// </summary>
    public class ShapeDrawerFactory {
        private static readonly Lazy<ShapeDrawerFactory> _instance =
            new Lazy<ShapeDrawerFactory>(() => new ShapeDrawerFactory());

        /// <summary>
        /// 获取工厂单例
        /// </summary>
        public static ShapeDrawerFactory Instance => _instance.Value;

        /// <summary>
        /// 形状绘制器注册表
        /// </summary>
        private readonly Dictionary<ShapeDrawingType, Func<IShapeDrawer>> _drawerFactories =
            new Dictionary<ShapeDrawingType, Func<IShapeDrawer>>();

        /// <summary>
        /// 绘制器实例缓存（单例模式的绘制器）
        /// </summary>
        private readonly Dictionary<ShapeDrawingType, IShapeDrawer> _drawerCache =
            new Dictionary<ShapeDrawingType, IShapeDrawer>();

        /// <summary>
        /// 魔法数字到枚举的映射（兼容旧系统 MW_ShapeDrawing.cs 中的 drawingShapeMode）
        /// </summary>
        private readonly Dictionary<int, ShapeDrawingType> _legacyModeMapping =
            new Dictionary<int, ShapeDrawingType> {
                // 线条类
                { 1, ShapeDrawingType.Line },           // 直线
                { 2, ShapeDrawingType.ArrowOneSide },   // 箭头线
                { 8, ShapeDrawingType.DashedLine },     // 虚线
                { 15, ShapeDrawingType.ParallelLine },  // 平行线（4条）
                { 18, ShapeDrawingType.DottedLine },    // 点线

                // 基础几何形状
                { 3, ShapeDrawingType.Rectangle },      // 矩形
                { 4, ShapeDrawingType.Ellipse },        // 椭圆
                { 5, ShapeDrawingType.Circle },         // 圆形（从中心）
                { 10, ShapeDrawingType.DashedCircle },  // 虚线圆
                { 16, ShapeDrawingType.CenterEllipse }, // 中心椭圆
                { 19, ShapeDrawingType.RectangleCenter }, // 中心矩形
                { 23, ShapeDrawingType.CenterEllipseWithFocalPoint }, // 中心椭圆带焦点

                // 坐标轴类
                { 11, ShapeDrawingType.Coordinate1 },   // 坐标轴1
                { 12, ShapeDrawingType.Coordinate2 },   // 坐标轴2
                { 13, ShapeDrawingType.Coordinate3 },   // 坐标轴3
                { 14, ShapeDrawingType.Coordinate4 },   // 坐标轴4
                { 17, ShapeDrawingType.Coordinate5 },   // 3D坐标轴

                // 曲线类
                { 20, ShapeDrawingType.Parabola1 },     // 抛物线1（y=ax²）
                { 21, ShapeDrawingType.Parabola2 },     // 抛物线2（x=ay²）
                { 22, ShapeDrawingType.ParabolaWithFocalPoint }, // 带焦点抛物线
                { 24, ShapeDrawingType.Hyperbola },     // 双曲线
                { 25, ShapeDrawingType.HyperbolaWithFocalPoint }, // 带焦点双曲线

                // 3D形状类
                { 6, ShapeDrawingType.Cylinder },       // 圆柱体
                { 7, ShapeDrawingType.Cone },           // 圆锥体
                { 9, ShapeDrawingType.Cuboid },         // 长方体/立方体
            };

        /// <summary>
        /// 私有构造函数（单例模式）
        /// </summary>
        private ShapeDrawerFactory() {
            RegisterBuiltInDrawers();
        }

        /// <summary>
        /// 注册内置绘制器
        /// </summary>
        private void RegisterBuiltInDrawers() {
            // 线条类
            Register<LineShapeDrawer>(ShapeDrawingType.Line);
            Register<DashedLineShapeDrawer>(ShapeDrawingType.DashedLine);
            Register<DottedLineShapeDrawer>(ShapeDrawingType.DottedLine);
            Register<ArrowOneSideShapeDrawer>(ShapeDrawingType.ArrowOneSide);
            Register<ArrowTwoSideShapeDrawer>(ShapeDrawingType.ArrowTwoSide);
            Register<ParallelLineShapeDrawer>(ShapeDrawingType.ParallelLine);

            // 基础几何形状
            Register<RectangleShapeDrawer>(ShapeDrawingType.Rectangle);
            Register<RectangleCenterShapeDrawer>(ShapeDrawingType.RectangleCenter);
            Register<EllipseShapeDrawer>(ShapeDrawingType.Ellipse);
            Register<CircleShapeDrawer>(ShapeDrawingType.Circle);
            Register<CenterCircleShapeDrawer>(ShapeDrawingType.CenterCircle);
            Register<CenterCircleWithRadiusShapeDrawer>(ShapeDrawingType.CenterCircleWithRadius);
            Register<DashedCircleShapeDrawer>(ShapeDrawingType.DashedCircle);
            Register<CenterEllipseShapeDrawer>(ShapeDrawingType.CenterEllipse);
            Register<CenterEllipseWithFocalPointShapeDrawer>(ShapeDrawingType.CenterEllipseWithFocalPoint);

            // 坐标轴类
            Register<Coordinate1ShapeDrawer>(ShapeDrawingType.Coordinate1);
            Register<Coordinate2ShapeDrawer>(ShapeDrawingType.Coordinate2);
            Register<Coordinate3ShapeDrawer>(ShapeDrawingType.Coordinate3);
            Register<Coordinate4ShapeDrawer>(ShapeDrawingType.Coordinate4);
            Register<Coordinate5ShapeDrawer>(ShapeDrawingType.Coordinate5);

            // 曲线类
            Register<Parabola1ShapeDrawer>(ShapeDrawingType.Parabola1);
            Register<Parabola2ShapeDrawer>(ShapeDrawingType.Parabola2);
            Register<ParabolaWithFocalPointShapeDrawer>(ShapeDrawingType.ParabolaWithFocalPoint);
            Register<HyperbolaShapeDrawer>(ShapeDrawingType.Hyperbola);
            Register<HyperbolaWithFocalPointShapeDrawer>(ShapeDrawingType.HyperbolaWithFocalPoint);

            // 3D形状类
            Register<CylinderShapeDrawer>(ShapeDrawingType.Cylinder);
            Register<ConeShapeDrawer>(ShapeDrawingType.Cone);
            Register<CuboidShapeDrawer>(ShapeDrawingType.Cuboid);

            // 坐标轴类
            Register<Coordinate1ShapeDrawer>(ShapeDrawingType.Coordinate1);
            Register<Coordinate2ShapeDrawer>(ShapeDrawingType.Coordinate2);
            Register<Coordinate3ShapeDrawer>(ShapeDrawingType.Coordinate3);
            Register<Coordinate4ShapeDrawer>(ShapeDrawingType.Coordinate4);
            Register<Coordinate5ShapeDrawer>(ShapeDrawingType.Coordinate5);
            Register<CoordinateGridShapeDrawer>(ShapeDrawingType.CoordinateGrid);
        }

        /// <summary>
        /// 注册形状绘制器类型
        /// </summary>
        /// <typeparam name="T">绘制器类型</typeparam>
        /// <param name="shapeType">形状类型</param>
        public void Register<T>(ShapeDrawingType shapeType) where T : IShapeDrawer, new() {
            _drawerFactories[shapeType] = () => new T();
        }

        /// <summary>
        /// 注册形状绘制器工厂方法
        /// </summary>
        /// <param name="shapeType">形状类型</param>
        /// <param name="factory">工厂方法</param>
        public void Register(ShapeDrawingType shapeType, Func<IShapeDrawer> factory) {
            _drawerFactories[shapeType] = factory;
        }

        /// <summary>
        /// 获取形状绘制器（每次返回新实例）
        /// </summary>
        /// <param name="shapeType">形状类型</param>
        /// <returns>形状绘制器实例</returns>
        public IShapeDrawer Create(ShapeDrawingType shapeType) {
            if (_drawerFactories.TryGetValue(shapeType, out var factory)) {
                return factory();
            }
            throw new ArgumentException($"未注册的形状类型: {shapeType}", nameof(shapeType));
        }

        /// <summary>
        /// 获取形状绘制器（缓存单例）
        /// </summary>
        /// <param name="shapeType">形状类型</param>
        /// <returns>形状绘制器实例（单例）</returns>
        public IShapeDrawer GetOrCreate(ShapeDrawingType shapeType) {
            if (!_drawerCache.TryGetValue(shapeType, out var drawer)) {
                drawer = Create(shapeType);
                _drawerCache[shapeType] = drawer;
            }
            return drawer;
        }

        /// <summary>
        /// 通过旧版魔法数字获取形状绘制器
        /// </summary>
        /// <param name="legacyMode">旧版形状模式数字</param>
        /// <returns>形状绘制器实例</returns>
        public IShapeDrawer GetByLegacyMode(int legacyMode) {
            if (_legacyModeMapping.TryGetValue(legacyMode, out var shapeType)) {
                return GetOrCreate(shapeType);
            }
            throw new ArgumentException($"未知的旧版形状模式: {legacyMode}", nameof(legacyMode));
        }

        /// <summary>
        /// 将旧版魔法数字转换为形状类型枚举
        /// </summary>
        /// <param name="legacyMode">旧版形状模式数字</param>
        /// <returns>形状类型枚举</returns>
        public ShapeDrawingType ConvertFromLegacyMode(int legacyMode) {
            if (_legacyModeMapping.TryGetValue(legacyMode, out var shapeType)) {
                return shapeType;
            }
            throw new ArgumentException($"未知的旧版形状模式: {legacyMode}", nameof(legacyMode));
        }

        /// <summary>
        /// 尝试将旧版魔法数字转换为形状类型枚举
        /// </summary>
        /// <param name="legacyMode">旧版形状模式数字</param>
        /// <param name="shapeType">输出的形状类型枚举</param>
        /// <returns>是否转换成功</returns>
        public bool TryConvertFromLegacyMode(int legacyMode, out ShapeDrawingType shapeType) {
            return _legacyModeMapping.TryGetValue(legacyMode, out shapeType);
        }

        /// <summary>
        /// 检查是否支持指定的形状类型
        /// </summary>
        /// <param name="shapeType">形状类型</param>
        /// <returns>是否支持</returns>
        public bool IsSupported(ShapeDrawingType shapeType) {
            return _drawerFactories.ContainsKey(shapeType);
        }

        /// <summary>
        /// 获取所有已注册的形状类型
        /// </summary>
        /// <returns>形状类型列表</returns>
        public IEnumerable<ShapeDrawingType> GetRegisteredTypes() {
            return _drawerFactories.Keys;
        }

        /// <summary>
        /// 清除绘制器缓存
        /// </summary>
        public void ClearCache() {
            foreach (var drawer in _drawerCache.Values) {
                drawer.Reset();
            }
            _drawerCache.Clear();
        }
    }
}
