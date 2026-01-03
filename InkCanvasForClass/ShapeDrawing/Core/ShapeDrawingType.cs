using System;

namespace Ink_Canvas.ShapeDrawing.Core {
    /// <summary>
    /// 形状绘制类型枚举
    /// 枚举值与原始 MW_ShapeDrawing.cs 中的 drawingShapeMode 魔法数字完全对应
    /// </summary>
    public enum ShapeDrawingType {
        /// <summary>
        /// 无形状模式
        /// </summary>
        None = 0,

        // ==================== 线条类 ====================
        /// <summary>
        /// 直线 (drawingShapeMode = 1)
        /// </summary>
        Line = 1,

        /// <summary>
        /// 单向箭头线 (drawingShapeMode = 2)
        /// </summary>
        ArrowOneSide = 2,

        /// <summary>
        /// 虚线 (drawingShapeMode = 8)
        /// </summary>
        DashedLine = 8,

        /// <summary>
        /// 平行线（4条，间距25）(drawingShapeMode = 15)
        /// </summary>
        ParallelLine = 15,

        /// <summary>
        /// 点线 (drawingShapeMode = 18)
        /// </summary>
        DottedLine = 18,

        /// <summary>
        /// 双向箭头线（扩展）
        /// </summary>
        ArrowTwoSide = 26,

        // ==================== 基础几何形状 ====================
        /// <summary>
        /// 矩形（对角点）(drawingShapeMode = 3)
        /// </summary>
        Rectangle = 3,

        /// <summary>
        /// 椭圆（对角点）(drawingShapeMode = 4)
        /// </summary>
        Ellipse = 4,

        /// <summary>
        /// 圆形（从中心向外）(drawingShapeMode = 5)
        /// </summary>
        Circle = 5,

        /// <summary>
        /// 中心圆形（从中心向外绘制）
        /// </summary>
        CenterCircle = 27,

        /// <summary>
        /// 中心圆形带半径
        /// </summary>
        CenterCircleWithRadius = 28,

        /// <summary>
        /// 虚线圆 (drawingShapeMode = 10)
        /// </summary>
        DashedCircle = 10,

        /// <summary>
        /// 中心椭圆（从中心向外）(drawingShapeMode = 16)
        /// </summary>
        CenterEllipse = 16,

        /// <summary>
        /// 中心矩形（从中心向外）(drawingShapeMode = 19)
        /// </summary>
        RectangleCenter = 19,

        /// <summary>
        /// 中心椭圆带焦点 (drawingShapeMode = 23)
        /// </summary>
        CenterEllipseWithFocalPoint = 23,

        // ==================== 坐标轴类 ====================
        /// <summary>
        /// 坐标轴1：第一象限坐标系，双向延伸 (drawingShapeMode = 11)
        /// </summary>
        Coordinate1 = 11,

        /// <summary>
        /// 坐标轴2：第一象限坐标系，X轴单向 (drawingShapeMode = 12)
        /// </summary>
        Coordinate2 = 12,

        /// <summary>
        /// 坐标轴3：第一象限坐标系，Y轴单向 (drawingShapeMode = 13)
        /// </summary>
        Coordinate3 = 13,

        /// <summary>
        /// 坐标轴4：第一象限坐标系，双单向 (drawingShapeMode = 14)
        /// </summary>
        Coordinate4 = 14,

        /// <summary>
        /// 坐标轴5：3D坐标轴 (drawingShapeMode = 17)
        /// </summary>
        Coordinate5 = 17,

        // ==================== 曲线类 ====================
        /// <summary>
        /// 抛物线1：y = ax² 形式 (drawingShapeMode = 20)
        /// </summary>
        Parabola1 = 20,

        /// <summary>
        /// 抛物线2：x = ay² 形式 (drawingShapeMode = 21)
        /// </summary>
        Parabola2 = 21,

        /// <summary>
        /// 带焦点的抛物线 (drawingShapeMode = 22)
        /// </summary>
        ParabolaWithFocalPoint = 22,

        /// <summary>
        /// 双曲线（多步绘制）(drawingShapeMode = 24)
        /// </summary>
        Hyperbola = 24,

        /// <summary>
        /// 带焦点的双曲线（多步绘制）(drawingShapeMode = 25)
        /// </summary>
        HyperbolaWithFocalPoint = 25,

        // ==================== 3D形状类 ====================
        /// <summary>
        /// 圆柱体 (drawingShapeMode = 6)
        /// </summary>
        Cylinder = 6,

        /// <summary>
        /// 圆锥体 (drawingShapeMode = 7)
        /// </summary>
        Cone = 7,

        /// <summary>
        /// 长方体/立方体（多步绘制）(drawingShapeMode = 9)
        /// </summary>
        Cuboid = 9,

        // ==================== V2 系统扩展形状 ====================
        /// <summary>
        /// 三角形
        /// </summary>
        Triangle = 100,

        /// <summary>
        /// 直角三角形
        /// </summary>
        RightTriangle = 101,

        /// <summary>
        /// 菱形
        /// </summary>
        Diamond = 102,

        /// <summary>
        /// 平行四边形
        /// </summary>
        Parallelogram = 103,

        /// <summary>
        /// 四线谱
        /// </summary>
        FourLine = 104,

        /// <summary>
        /// 五线谱
        /// </summary>
        Staff = 105,

        /// <summary>
        /// 饼状椭圆
        /// </summary>
        PieEllipse = 106,

        /// <summary>
        /// 坐标网格
        /// </summary>
        CoordinateGrid = 107
    }

    /// <summary>
    /// 形状类型分类
    /// </summary>
    public enum ShapeCategory {
        /// <summary>
        /// 线条类
        /// </summary>
        Line,

        /// <summary>
        /// 基础几何形状
        /// </summary>
        BasicShape,

        /// <summary>
        /// 坐标轴类
        /// </summary>
        Axis,

        /// <summary>
        /// 曲线类
        /// </summary>
        Curve,

        /// <summary>
        /// 3D立体形状
        /// </summary>
        Solid3D,

        /// <summary>
        /// 特殊形状
        /// </summary>
        Special
    }

    /// <summary>
    /// 形状类型扩展方法
    /// </summary>
    public static class ShapeDrawingTypeExtensions {
        /// <summary>
        /// 获取形状类型的分类
        /// </summary>
        public static ShapeCategory GetCategory(this ShapeDrawingType type) {
            switch (type) {
                case ShapeDrawingType.Line:
                case ShapeDrawingType.DashedLine:
                case ShapeDrawingType.DottedLine:
                case ShapeDrawingType.ArrowOneSide:
                case ShapeDrawingType.ArrowTwoSide:
                case ShapeDrawingType.ParallelLine:
                    return ShapeCategory.Line;

                case ShapeDrawingType.Rectangle:
                case ShapeDrawingType.RectangleCenter:
                case ShapeDrawingType.Ellipse:
                case ShapeDrawingType.Circle:
                case ShapeDrawingType.CenterEllipse:
                case ShapeDrawingType.CenterEllipseWithFocalPoint:
                case ShapeDrawingType.DashedCircle:
                case ShapeDrawingType.Triangle:
                case ShapeDrawingType.RightTriangle:
                case ShapeDrawingType.Diamond:
                case ShapeDrawingType.Parallelogram:
                case ShapeDrawingType.PieEllipse:
                    return ShapeCategory.BasicShape;

                case ShapeDrawingType.Coordinate1:
                case ShapeDrawingType.Coordinate2:
                case ShapeDrawingType.Coordinate3:
                case ShapeDrawingType.Coordinate4:
                case ShapeDrawingType.Coordinate5:
                case ShapeDrawingType.CoordinateGrid:
                    return ShapeCategory.Axis;

                case ShapeDrawingType.Parabola1:
                case ShapeDrawingType.Parabola2:
                case ShapeDrawingType.ParabolaWithFocalPoint:
                case ShapeDrawingType.Hyperbola:
                case ShapeDrawingType.HyperbolaWithFocalPoint:
                    return ShapeCategory.Curve;

                case ShapeDrawingType.Cylinder:
                case ShapeDrawingType.Cone:
                case ShapeDrawingType.Cuboid:
                    return ShapeCategory.Solid3D;

                case ShapeDrawingType.FourLine:
                case ShapeDrawingType.Staff:
                default:
                    return ShapeCategory.Special;
            }
        }

        /// <summary>
        /// 判断是否为线条类形状
        /// </summary>
        public static bool IsLineShape(this ShapeDrawingType type) {
            return type.GetCategory() == ShapeCategory.Line;
        }

        /// <summary>
        /// 判断是否需要多步绘制
        /// </summary>
        public static bool RequiresMultiStep(this ShapeDrawingType type) {
            return type == ShapeDrawingType.Cuboid ||
                   type == ShapeDrawingType.Hyperbola ||
                   type == ShapeDrawingType.HyperbolaWithFocalPoint;
        }

        /// <summary>
        /// 获取形状的显示名称
        /// </summary>
        public static string GetDisplayName(this ShapeDrawingType type) {
            switch (type) {
                case ShapeDrawingType.Line: return "直线";
                case ShapeDrawingType.ArrowOneSide: return "箭头线";
                case ShapeDrawingType.ArrowTwoSide: return "双向箭头";
                case ShapeDrawingType.DashedLine: return "虚线";
                case ShapeDrawingType.DottedLine: return "点线";
                case ShapeDrawingType.ParallelLine: return "平行线";
                case ShapeDrawingType.Rectangle: return "矩形";
                case ShapeDrawingType.RectangleCenter: return "中心矩形";
                case ShapeDrawingType.Ellipse: return "椭圆";
                case ShapeDrawingType.Circle: return "圆形";
                case ShapeDrawingType.CenterEllipse: return "中心椭圆";
                case ShapeDrawingType.CenterEllipseWithFocalPoint: return "中心椭圆（带焦点）";
                case ShapeDrawingType.DashedCircle: return "虚线圆";
                case ShapeDrawingType.Coordinate1: return "坐标轴1";
                case ShapeDrawingType.Coordinate2: return "坐标轴2";
                case ShapeDrawingType.Coordinate3: return "坐标轴3";
                case ShapeDrawingType.Coordinate4: return "坐标轴4";
                case ShapeDrawingType.Coordinate5: return "3D坐标轴";
                case ShapeDrawingType.Parabola1: return "抛物线1";
                case ShapeDrawingType.Parabola2: return "抛物线2";
                case ShapeDrawingType.ParabolaWithFocalPoint: return "抛物线（带焦点）";
                case ShapeDrawingType.Hyperbola: return "双曲线";
                case ShapeDrawingType.HyperbolaWithFocalPoint: return "双曲线（带焦点）";
                case ShapeDrawingType.Cylinder: return "圆柱体";
                case ShapeDrawingType.Cone: return "圆锥体";
                case ShapeDrawingType.Cuboid: return "长方体";
                case ShapeDrawingType.Triangle: return "三角形";
                case ShapeDrawingType.RightTriangle: return "直角三角形";
                case ShapeDrawingType.Diamond: return "菱形";
                case ShapeDrawingType.Parallelogram: return "平行四边形";
                case ShapeDrawingType.FourLine: return "四线谱";
                case ShapeDrawingType.Staff: return "五线谱";
                case ShapeDrawingType.PieEllipse: return "饼状图";
                case ShapeDrawingType.CoordinateGrid: return "坐标网格";
                default: return type.ToString();
            }
        }
    }
}