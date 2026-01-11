using System;
using System.Windows;
using System.Windows.Ink;
using Ink_Canvas.ShapeDrawing.Core;

namespace Ink_Canvas.Services
{
    /// <summary>
    /// 形状绘制模式变化事件参数
    /// </summary>
    public class ShapeModeChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 旧的形状类型
        /// </summary>
        public ShapeDrawingType? OldShapeType { get; }

        /// <summary>
        /// 新的形状类型
        /// </summary>
        public ShapeDrawingType? NewShapeType { get; }

        /// <summary>
        /// 是否进入绘制模式
        /// </summary>
        public bool IsDrawingMode { get; }

        public ShapeModeChangedEventArgs(ShapeDrawingType? oldType, ShapeDrawingType? newType, bool isDrawingMode)
        {
            OldShapeType = oldType;
            NewShapeType = newType;
            IsDrawingMode = isDrawingMode;
        }
    }

    /// <summary>
    /// 形状绘制完成事件参数
    /// </summary>
    public class ShapeDrawnEventArgs : EventArgs
    {
        /// <summary>
        /// 绘制的形状类型
        /// </summary>
        public ShapeDrawingType ShapeType { get; }

        /// <summary>
        /// 生成的笔画集合
        /// </summary>
        public StrokeCollection Strokes { get; }

        /// <summary>
        /// 起始点
        /// </summary>
        public Point StartPoint { get; }

        /// <summary>
        /// 结束点
        /// </summary>
        public Point EndPoint { get; }

        public ShapeDrawnEventArgs(ShapeDrawingType shapeType, StrokeCollection strokes, Point startPoint, Point endPoint)
        {
            ShapeType = shapeType;
            Strokes = strokes;
            StartPoint = startPoint;
            EndPoint = endPoint;
        }
    }

    /// <summary>
    /// 形状绘制服务接口
    /// 提供形状绘制功能的抽象，支持依赖注入
    /// </summary>
    public interface IShapeDrawingService
    {
        #region 属性

        /// <summary>
        /// 当前形状类型
        /// </summary>
        ShapeDrawingType? CurrentShapeType { get; }

        /// <summary>
        /// 是否处于绘制模式
        /// </summary>
        bool IsDrawingMode { get; }

        /// <summary>
        /// 是否正在绘制（已开始但未完成）
        /// </summary>
        bool IsDrawing { get; }

        /// <summary>
        /// 当前绘制步骤（用于多步绘制）
        /// </summary>
        int CurrentStep { get; }

        /// <summary>
        /// 绘制起点
        /// </summary>
        Point? StartPoint { get; }

        #endregion

        #region 事件

        /// <summary>
        /// 形状绘制模式变化事件
        /// </summary>
        event EventHandler<ShapeModeChangedEventArgs> ShapeModeChanged;

        /// <summary>
        /// 形状绘制完成事件
        /// </summary>
        event EventHandler<ShapeDrawnEventArgs> ShapeDrawn;

        #endregion

        #region 绘制模式控制

        /// <summary>
        /// 开始绘制模式
        /// </summary>
        /// <param name="shapeType">形状类型</param>
        void StartDrawing(ShapeDrawingType shapeType);

        /// <summary>
        /// 结束绘制模式
        /// </summary>
        void EndDrawing();

        /// <summary>
        /// 设置当前形状类型（不进入绘制模式）
        /// </summary>
        /// <param name="shapeType">形状类型</param>
        void SetShapeType(ShapeDrawingType shapeType);

        #endregion

        #region 形状绘制

        /// <summary>
        /// 开始绘制形状
        /// </summary>
        /// <param name="startPoint">起始点</param>
        void BeginShape(Point startPoint);

        /// <summary>
        /// 绘制形状预览（鼠标移动时）
        /// </summary>
        /// <param name="endPoint">当前点</param>
        /// <param name="drawingAttributes">绘制属性</param>
        /// <returns>预览笔画集合</returns>
        StrokeCollection DrawPreview(Point endPoint, DrawingAttributes drawingAttributes);

        /// <summary>
        /// 完成形状绘制
        /// </summary>
        /// <param name="endPoint">结束点</param>
        /// <param name="drawingAttributes">绘制属性</param>
        /// <returns>最终笔画集合</returns>
        StrokeCollection FinishShape(Point endPoint, DrawingAttributes drawingAttributes);

        /// <summary>
        /// 取消当前绘制
        /// </summary>
        void CancelShape();

        /// <summary>
        /// 创建形状（一次性绘制，不需要开始/结束流程）
        /// </summary>
        /// <param name="startPoint">起始点</param>
        /// <param name="endPoint">结束点</param>
        /// <param name="shapeType">形状类型</param>
        /// <param name="drawingAttributes">绘制属性</param>
        /// <returns>生成的笔画集合</returns>
        StrokeCollection CreateShape(Point startPoint, Point endPoint, ShapeDrawingType shapeType, DrawingAttributes drawingAttributes);

        #endregion

        #region 绘制器访问

        /// <summary>
        /// 获取指定形状类型的绘制器
        /// </summary>
        /// <param name="shapeType">形状类型</param>
        /// <returns>形状绘制器</returns>
        IShapeDrawer GetDrawer(ShapeDrawingType shapeType);

        /// <summary>
        /// 检查是否支持指定的形状类型
        /// </summary>
        /// <param name="shapeType">形状类型</param>
        /// <returns>是否支持</returns>
        bool IsShapeTypeSupported(ShapeDrawingType shapeType);

        #endregion

        #region 旧版兼容

        /// <summary>
        /// 通过旧版模式数字开始绘制
        /// </summary>
        /// <param name="legacyMode">旧版形状模式数字</param>
        void StartDrawingLegacy(int legacyMode);

        /// <summary>
        /// 将旧版模式数字转换为形状类型
        /// </summary>
        /// <param name="legacyMode">旧版形状模式数字</param>
        /// <returns>形状类型，如果不支持则返回 null</returns>
        ShapeDrawingType? ConvertFromLegacyMode(int legacyMode);

        #endregion
    }
}
