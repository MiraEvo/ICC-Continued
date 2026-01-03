using System;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace Ink_Canvas.Services
{
    /// <summary>
    /// InkCanvas 编辑模式
    /// </summary>
    public enum InkCanvasMode
    {
        /// <summary>
        /// 无操作
        /// </summary>
        None,

        /// <summary>
        /// 墨迹绘制
        /// </summary>
        Ink,

        /// <summary>
        /// 手势识别
        /// </summary>
        GestureOnly,

        /// <summary>
        /// 墨迹和手势
        /// </summary>
        InkAndGesture,

        /// <summary>
        /// 选择模式
        /// </summary>
        Select,

        /// <summary>
        /// 按点擦除
        /// </summary>
        EraseByPoint,

        /// <summary>
        /// 按笔画擦除
        /// </summary>
        EraseByStroke
    }

    /// <summary>
    /// 笔画集合变化事件参数
    /// </summary>
    public class StrokesChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 添加的笔画
        /// </summary>
        public StrokeCollection AddedStrokes { get; }

        /// <summary>
        /// 移除的笔画
        /// </summary>
        public StrokeCollection RemovedStrokes { get; }

        public StrokesChangedEventArgs(StrokeCollection added, StrokeCollection removed)
        {
            AddedStrokes = added ?? new StrokeCollection();
            RemovedStrokes = removed ?? new StrokeCollection();
        }
    }

    /// <summary>
    /// 选择变化事件参数
    /// </summary>
    public class SelectionChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 选中的笔画
        /// </summary>
        public StrokeCollection SelectedStrokes { get; }

        public SelectionChangedEventArgs(StrokeCollection selectedStrokes)
        {
            SelectedStrokes = selectedStrokes ?? new StrokeCollection();
        }
    }

    /// <summary>
    /// InkCanvas 服务接口 - 提供对 InkCanvas 的抽象操作
    /// </summary>
    public interface IInkCanvasService
    {
        #region 事件

        /// <summary>
        /// 笔画集合变化事件
        /// </summary>
        event EventHandler<StrokesChangedEventArgs> StrokesChanged;

        /// <summary>
        /// 选择变化事件
        /// </summary>
        event EventHandler<SelectionChangedEventArgs> SelectionChanged;

        /// <summary>
        /// 编辑模式变化事件
        /// </summary>
        event EventHandler<InkCanvasMode> EditingModeChanged;

        /// <summary>
        /// 绘图属性变化事件
        /// </summary>
        event EventHandler<DrawingAttributes> DrawingAttributesChanged;

        #endregion

        #region 属性

        /// <summary>
        /// 获取或设置笔画集合
        /// </summary>
        StrokeCollection Strokes { get; set; }

        /// <summary>
        /// 获取选中的笔画
        /// </summary>
        StrokeCollection SelectedStrokes { get; }

        /// <summary>
        /// 获取或设置当前编辑模式
        /// </summary>
        InkCanvasMode CurrentMode { get; set; }

        /// <summary>
        /// 获取或设置默认绘图属性
        /// </summary>
        DrawingAttributes DefaultDrawingAttributes { get; set; }

        /// <summary>
        /// 获取 InkCanvas 的实际宽度
        /// </summary>
        double ActualWidth { get; }

        /// <summary>
        /// 获取 InkCanvas 的实际高度
        /// </summary>
        double ActualHeight { get; }

        /// <summary>
        /// 是否有选中的笔画
        /// </summary>
        bool HasSelection { get; }

        /// <summary>
        /// 笔画数量
        /// </summary>
        int StrokeCount { get; }

        #endregion

        #region 笔画操作

        /// <summary>
        /// 添加笔画
        /// </summary>
        /// <param name="strokes">要添加的笔画</param>
        void AddStrokes(StrokeCollection strokes);

        /// <summary>
        /// 添加单个笔画
        /// </summary>
        /// <param name="stroke">要添加的笔画</param>
        void AddStroke(Stroke stroke);

        /// <summary>
        /// 移除笔画
        /// </summary>
        /// <param name="strokes">要移除的笔画</param>
        void RemoveStrokes(StrokeCollection strokes);

        /// <summary>
        /// 移除单个笔画
        /// </summary>
        /// <param name="stroke">要移除的笔画</param>
        void RemoveStroke(Stroke stroke);

        /// <summary>
        /// 清除所有笔画
        /// </summary>
        void ClearStrokes();

        /// <summary>
        /// 替换笔画
        /// </summary>
        /// <param name="oldStrokes">要替换的旧笔画</param>
        /// <param name="newStrokes">新笔画</param>
        void ReplaceStrokes(StrokeCollection oldStrokes, StrokeCollection newStrokes);

        /// <summary>
        /// 检查是否包含指定笔画
        /// </summary>
        /// <param name="stroke">要检查的笔画</param>
        /// <returns>是否包含</returns>
        bool ContainsStroke(Stroke stroke);

        #endregion

        #region 选择操作

        /// <summary>
        /// 选择笔画
        /// </summary>
        /// <param name="strokes">要选择的笔画</param>
        void Select(StrokeCollection strokes);

        /// <summary>
        /// 全选
        /// </summary>
        void SelectAll();

        /// <summary>
        /// 取消选择
        /// </summary>
        void ClearSelection();

        /// <summary>
        /// 删除选中的笔画
        /// </summary>
        void DeleteSelection();

        /// <summary>
        /// 复制选中的笔画
        /// </summary>
        /// <returns>复制的笔画</returns>
        StrokeCollection CopySelection();

        #endregion

        #region 绘图属性操作

        /// <summary>
        /// 设置笔画颜色
        /// </summary>
        /// <param name="color">颜色</param>
        void SetStrokeColor(Color color);

        /// <summary>
        /// 设置笔画宽度
        /// </summary>
        /// <param name="width">宽度</param>
        void SetStrokeWidth(double width);

        /// <summary>
        /// 设置笔画高度
        /// </summary>
        /// <param name="height">高度</param>
        void SetStrokeHeight(double height);

        /// <summary>
        /// 设置是否为高亮笔
        /// </summary>
        /// <param name="isHighlighter">是否为高亮笔</param>
        void SetHighlighter(bool isHighlighter);

        /// <summary>
        /// 应用绘图属性到选中的笔画
        /// </summary>
        /// <param name="attributes">绘图属性</param>
        void ApplyDrawingAttributesToSelection(DrawingAttributes attributes);

        #endregion

        #region 视图操作

        /// <summary>
        /// 滚动到指定位置
        /// </summary>
        /// <param name="point">位置</param>
        void ScrollTo(Point point);

        /// <summary>
        /// 获取笔画边界
        /// </summary>
        /// <param name="strokes">笔画</param>
        /// <returns>边界矩形</returns>
        Rect GetStrokesBounds(StrokeCollection strokes);

        /// <summary>
        /// 获取所有笔画的边界
        /// </summary>
        /// <returns>边界矩形</returns>
        Rect GetAllStrokesBounds();

        #endregion

        #region 命中测试

        /// <summary>
        /// 获取指定点的笔画
        /// </summary>
        /// <param name="point">点</param>
        /// <returns>命中的笔画</returns>
        StrokeCollection HitTest(Point point);

        /// <summary>
        /// 获取指定矩形内的笔画
        /// </summary>
        /// <param name="rect">矩形</param>
        /// <returns>命中的笔画</returns>
        StrokeCollection HitTest(Rect rect);

        /// <summary>
        /// 获取指定路径上的笔画
        /// </summary>
        /// <param name="points">路径点</param>
        /// <returns>命中的笔画</returns>
        StrokeCollection HitTest(StylusPointCollection points);

        #endregion
    }
}