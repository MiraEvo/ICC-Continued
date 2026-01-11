using Ink_Canvas.Helpers;
using System;
using System.Windows.Ink;

namespace Ink_Canvas.Models
{
    /// <summary>
    /// 页面状态模型 - 包含页面的完整状态信息
    /// </summary>
    public class PageState
    {
        /// <summary>
        /// 页面索引
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// 页面笔画集合
        /// </summary>
        public StrokeCollection Strokes { get; set; }

        /// <summary>
        /// 背景颜色
        /// </summary>
        public Ink_Canvas.BlackboardBackgroundColorEnum BackgroundColor { get; set; }

        /// <summary>
        /// 背景图案
        /// </summary>
        public Ink_Canvas.BlackboardBackgroundPatternEnum BackgroundPattern { get; set; }

        /// <summary>
        /// 页面创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 页面修改时间
        /// </summary>
        public DateTime ModifiedAt { get; set; }

        /// <summary>
        /// 页面缩略图数据（Base64 编码）
        /// </summary>
        public byte[] ThumbnailData { get; set; }

        /// <summary>
        /// 页面历史记录（用于撤销/重做）
        /// </summary>
        public TimeMachineHistory[] History { get; set; }

        /// <summary>
        /// 是否为黑板模式页面
        /// </summary>
        public bool IsBlackboardMode { get; set; }

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public PageState()
        {
            Strokes = new StrokeCollection();
            BackgroundColor = Ink_Canvas.BlackboardBackgroundColorEnum.White;
            BackgroundPattern = Ink_Canvas.BlackboardBackgroundPatternEnum.None;
            CreatedAt = DateTime.Now;
            ModifiedAt = DateTime.Now;
        }

        /// <summary>
        /// 带索引的构造函数
        /// </summary>
        /// <param name="index">页面索引</param>
        public PageState(int index) : this()
        {
            Index = index;
        }

        /// <summary>
        /// 克隆页面状态
        /// </summary>
        /// <returns>克隆的页面状态</returns>
        public PageState Clone()
        {
            var clone = new PageState
            {
                Index = this.Index,
                Strokes = this.Strokes?.Clone() ?? new StrokeCollection(),
                BackgroundColor = this.BackgroundColor,
                BackgroundPattern = this.BackgroundPattern,
                CreatedAt = this.CreatedAt,
                ModifiedAt = DateTime.Now,
                IsBlackboardMode = this.IsBlackboardMode
            };

            // 复制缩略图数据
            if (this.ThumbnailData != null)
            {
                clone.ThumbnailData = new byte[this.ThumbnailData.Length];
                Array.Copy(this.ThumbnailData, clone.ThumbnailData, this.ThumbnailData.Length);
            }

            // 复制历史记录
            if (this.History != null)
            {
                clone.History = new TimeMachineHistory[this.History.Length];
                Array.Copy(this.History, clone.History, this.History.Length);
            }

            return clone;
        }

        /// <summary>
        /// 更新修改时间
        /// </summary>
        public void Touch()
        {
            ModifiedAt = DateTime.Now;
        }
    }
}
