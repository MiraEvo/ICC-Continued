using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Controls;

namespace Ink_Canvas.ShapeDrawing.Core {
    /// <summary>
    /// 形状绘制上下文，封装绘制所需的所有参数
    /// </summary>
    public class ShapeDrawingContext {
        /// <summary>
        /// 起始点
        /// </summary>
        public Point StartPoint { get; set; }

        /// <summary>
        /// 结束点
        /// </summary>
        public Point EndPoint { get; set; }

        /// <summary>
        /// 绘制属性（笔触颜色、宽度等）
        /// </summary>
        public DrawingAttributes DrawingAttributes { get; set; }

        /// <summary>
        /// InkCanvas 引用
        /// </summary>
        public InkCanvas InkCanvas { get; set; }

        /// <summary>
        /// 当前绘制步骤（用于多步绘制）
        /// </summary>
        public int CurrentStep { get; set; }

        /// <summary>
        /// 是否为预览模式（鼠标移动时绘制临时形状）
        /// </summary>
        public bool IsPreview { get; set; }

        /// <summary>
        /// 是否在直线形状上分布点（用于墨迹框选）
        /// </summary>
        public bool DistributePointsOnLine { get; set; } = true;

        /// <summary>
        /// 扩展数据（用于存储多步绘制的中间状态等）
        /// </summary>
        public Dictionary<string, object> ExtraData { get; }

        public ShapeDrawingContext() {
            ExtraData = new Dictionary<string, object>();
        }

        /// <summary>
        /// 获取扩展数据
        /// </summary>
        public T GetExtraData<T>(string key, T defaultValue = default) {
            if (ExtraData.TryGetValue(key, out var value) && value is T typedValue) {
                return typedValue;
            }
            return defaultValue;
        }

        /// <summary>
        /// 设置扩展数据
        /// </summary>
        public void SetExtraData<T>(string key, T value) {
            ExtraData[key] = value;
        }

        /// <summary>
        /// 计算起点和终点的距离
        /// </summary>
        public double GetDistance() {
            double dx = EndPoint.X - StartPoint.X;
            double dy = EndPoint.Y - StartPoint.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// 计算起点到终点的角度（弧度）
        /// </summary>
        public double GetAngle() {
            return Math.Atan2(EndPoint.Y - StartPoint.Y, EndPoint.X - StartPoint.X);
        }

        /// <summary>
        /// 获取绘制区域的宽度（绝对值）
        /// </summary>
        public double GetWidth() {
            return Math.Abs(EndPoint.X - StartPoint.X);
        }

        /// <summary>
        /// 获取绘制区域的高度（绝对值）
        /// </summary>
        public double GetHeight() {
            return Math.Abs(EndPoint.Y - StartPoint.Y);
        }

        /// <summary>
        /// 获取绘制区域的中心点
        /// </summary>
        public Point GetCenter() {
            return new Point(
                (StartPoint.X + EndPoint.X) / 2,
                (StartPoint.Y + EndPoint.Y) / 2
            );
        }

        /// <summary>
        /// 克隆一个新的绘制属性
        /// </summary>
        public DrawingAttributes CloneDrawingAttributes() {
            return DrawingAttributes?.Clone() ?? new DrawingAttributes();
        }
    }
}