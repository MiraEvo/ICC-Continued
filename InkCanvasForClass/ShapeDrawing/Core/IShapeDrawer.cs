using System.Windows.Ink;

namespace Ink_Canvas.ShapeDrawing.Core {
    /// <summary>
    /// 形状绘制器接口
    /// </summary>
    public interface IShapeDrawer {
        /// <summary>
        /// 形状类型
        /// </summary>
        ShapeDrawingType ShapeType { get; }

        /// <summary>
        /// 形状名称（用于显示）
        /// </summary>
        string ShapeName { get; }

        /// <summary>
        /// 是否支持多步绘制
        /// </summary>
        bool SupportsMultiStep { get; }

        /// <summary>
        /// 总绘制步骤数
        /// </summary>
        int TotalSteps { get; }

        /// <summary>
        /// 执行形状绘制
        /// </summary>
        /// <param name="context">绘制上下文</param>
        /// <returns>生成的笔画集合</returns>
        StrokeCollection Draw(ShapeDrawingContext context);

        /// <summary>
        /// 重置绘制器状态（用于多步绘制）
        /// </summary>
        void Reset();

        /// <summary>
        /// 验证绘制参数是否有效
        /// </summary>
        /// <param name="context">绘制上下文</param>
        /// <returns>是否有效</returns>
        bool ValidateContext(ShapeDrawingContext context);
    }
}