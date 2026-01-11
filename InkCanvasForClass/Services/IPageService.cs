using Ink_Canvas.Helpers;
using Ink_Canvas.Models;
using Ink_Canvas.Services.Events;
using System;
using System.Collections.Generic;
using System.Windows.Ink;

namespace Ink_Canvas.Services
{
    /// <summary>
    /// 页面信息（保留用于向后兼容）
    /// </summary>
    public class PageInfo
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
        /// 页面历史记录
        /// </summary>
        public TimeMachineHistory[] History { get; set; }

        /// <summary>
        /// 页面创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 页面修改时间
        /// </summary>
        public DateTime ModifiedAt { get; set; }

        /// <summary>
        /// 是否为黑板模式页面
        /// </summary>
        public bool IsBlackboardMode { get; set; }

        /// <summary>
        /// 页面缩略图数据（Base64）
        /// </summary>
        public string ThumbnailData { get; set; }

        /// <summary>
        /// 背景颜色
        /// </summary>
        public Ink_Canvas.BlackboardBackgroundColorEnum BackgroundColor { get; set; }

        /// <summary>
        /// 背景图案
        /// </summary>
        public Ink_Canvas.BlackboardBackgroundPatternEnum BackgroundPattern { get; set; }

        public PageInfo()
        {
            Strokes = new StrokeCollection();
            CreatedAt = DateTime.Now;
            ModifiedAt = DateTime.Now;
            BackgroundColor = Ink_Canvas.BlackboardBackgroundColorEnum.White;
            BackgroundPattern = Ink_Canvas.BlackboardBackgroundPatternEnum.None;
        }

        /// <summary>
        /// 从 PageState 创建 PageInfo
        /// </summary>
        public static PageInfo FromPageState(PageState state)
        {
            if (state == null) return null;
            
            return new PageInfo
            {
                Index = state.Index,
                Strokes = state.Strokes,
                History = state.History,
                CreatedAt = state.CreatedAt,
                ModifiedAt = state.ModifiedAt,
                IsBlackboardMode = state.IsBlackboardMode,
                ThumbnailData = state.ThumbnailData != null ? Convert.ToBase64String(state.ThumbnailData) : null,
                BackgroundColor = state.BackgroundColor,
                BackgroundPattern = state.BackgroundPattern
            };
        }

        /// <summary>
        /// 转换为 PageState
        /// </summary>
        public PageState ToPageState()
        {
            return new PageState
            {
                Index = this.Index,
                Strokes = this.Strokes,
                History = this.History,
                CreatedAt = this.CreatedAt,
                ModifiedAt = this.ModifiedAt,
                IsBlackboardMode = this.IsBlackboardMode,
                ThumbnailData = !string.IsNullOrEmpty(this.ThumbnailData) 
                    ? Convert.FromBase64String(this.ThumbnailData) 
                    : null,
                BackgroundColor = this.BackgroundColor,
                BackgroundPattern = this.BackgroundPattern
            };
        }
    }

    /// <summary>
    /// 页面服务接口 - 管理画布页面
    /// </summary>
    public interface IPageService
    {
        #region 事件

        /// <summary>
        /// 页面变化事件（使用新的事件参数类型）
        /// </summary>
        event EventHandler<PageChangedEventArgs> PageChanged;

        /// <summary>
        /// 页面添加事件（使用新的事件参数类型）
        /// </summary>
        event EventHandler<PageAddedEventArgs> PageAdded;

        /// <summary>
        /// 页面删除事件（使用新的事件参数类型）
        /// </summary>
        event EventHandler<PageDeletedEventArgs> PageDeleted;

        /// <summary>
        /// 页面清除事件
        /// </summary>
        event EventHandler<PagesClearedEventArgs> PagesCleared;

        #endregion

        #region 属性

        /// <summary>
        /// 当前页面索引
        /// </summary>
        int CurrentPageIndex { get; }

        /// <summary>
        /// 页面总数
        /// </summary>
        int PageCount { get; }

        /// <summary>
        /// 获取当前页面状态（新增）
        /// </summary>
        PageState CurrentPageState { get; }

        /// <summary>
        /// 获取当前页面信息（保留用于向后兼容）
        /// </summary>
        PageInfo CurrentPage { get; }

        /// <summary>
        /// 获取所有页面状态集合（新增）
        /// </summary>
        IReadOnlyList<PageState> Pages { get; }

        /// <summary>
        /// 是否可以向前翻页
        /// </summary>
        bool CanGoPrevious { get; }

        /// <summary>
        /// 是否可以向后翻页
        /// </summary>
        bool CanGoNext { get; }

        /// <summary>
        /// 是否为黑板模式
        /// </summary>
        bool IsBlackboardMode { get; set; }

        #endregion

        #region 页面导航

        /// <summary>
        /// 跳转到指定页面
        /// </summary>
        /// <param name="pageIndex">页面索引</param>
        /// <returns>是否成功</returns>
        bool GoToPage(int pageIndex);

        /// <summary>
        /// 跳转到上一页
        /// </summary>
        /// <returns>是否成功</returns>
        bool GoPrevious();

        /// <summary>
        /// 跳转到下一页
        /// </summary>
        /// <returns>是否成功</returns>
        bool GoNext();

        /// <summary>
        /// 跳转到第一页
        /// </summary>
        /// <returns>是否成功</returns>
        bool GoFirst();

        /// <summary>
        /// 跳转到最后一页
        /// </summary>
        /// <returns>是否成功</returns>
        bool GoLast();

        #endregion

        #region 页面管理

        /// <summary>
        /// 添加新页面
        /// </summary>
        /// <returns>新页面索引</returns>
        int AddPage();

        /// <summary>
        /// 在指定位置插入新页面
        /// </summary>
        /// <param name="index">插入位置</param>
        /// <returns>新页面索引</returns>
        int InsertPage(int index);

        /// <summary>
        /// 删除指定页面
        /// </summary>
        /// <param name="pageIndex">页面索引</param>
        /// <returns>是否成功</returns>
        bool DeletePage(int pageIndex);

        /// <summary>
        /// 删除当前页面
        /// </summary>
        /// <returns>是否成功</returns>
        bool DeleteCurrentPage();

        /// <summary>
        /// 清除所有页面
        /// </summary>
        void ClearAllPages();

        /// <summary>
        /// 复制页面
        /// </summary>
        /// <param name="sourceIndex">源页面索引</param>
        /// <returns>新页面索引</returns>
        int DuplicatePage(int sourceIndex);

        /// <summary>
        /// 移动页面
        /// </summary>
        /// <param name="fromIndex">源位置</param>
        /// <param name="toIndex">目标位置</param>
        /// <returns>是否成功</returns>
        bool MovePage(int fromIndex, int toIndex);

        #endregion

        #region 页面数据

        /// <summary>
        /// 获取页面信息（保留用于向后兼容）
        /// </summary>
        /// <param name="pageIndex">页面索引</param>
        /// <returns>页面信息</returns>
        PageInfo GetPage(int pageIndex);

        /// <summary>
        /// 获取页面状态（新增）
        /// </summary>
        /// <param name="pageIndex">页面索引</param>
        /// <returns>页面状态</returns>
        PageState GetPageState(int pageIndex);

        /// <summary>
        /// 获取所有页面信息（保留用于向后兼容）
        /// </summary>
        /// <returns>页面信息列表</returns>
        IReadOnlyList<PageInfo> GetAllPages();

        /// <summary>
        /// 保存当前页面状态
        /// </summary>
        void SaveCurrentPageState();

        /// <summary>
        /// 保存当前页面状态（带笔画参数）
        /// </summary>
        /// <param name="strokes">要保存的笔画集合</param>
        void SaveCurrentPageState(StrokeCollection strokes);

        /// <summary>
        /// 恢复当前页面状态
        /// </summary>
        void RestoreCurrentPageState();

        /// <summary>
        /// 加载指定页面的笔画（新增）
        /// </summary>
        /// <param name="pageIndex">页面索引</param>
        /// <returns>笔画集合</returns>
        StrokeCollection LoadPageStrokes(int pageIndex);

        /// <summary>
        /// 更新页面笔画
        /// </summary>
        /// <param name="pageIndex">页面索引</param>
        /// <param name="strokes">笔画集合</param>
        void UpdatePageStrokes(int pageIndex, StrokeCollection strokes);

        /// <summary>
        /// 更新页面历史记录
        /// </summary>
        /// <param name="pageIndex">页面索引</param>
        /// <param name="history">历史记录</param>
        void UpdatePageHistory(int pageIndex, TimeMachineHistory[] history);

        /// <summary>
        /// 更新页面背景颜色（新增）
        /// </summary>
        /// <param name="pageIndex">页面索引</param>
        /// <param name="color">背景颜色</param>
        void UpdatePageBackgroundColor(int pageIndex, Ink_Canvas.BlackboardBackgroundColorEnum color);

        /// <summary>
        /// 更新页面背景图案（新增）
        /// </summary>
        /// <param name="pageIndex">页面索引</param>
        /// <param name="pattern">背景图案</param>
        void UpdatePageBackgroundPattern(int pageIndex, Ink_Canvas.BlackboardBackgroundPatternEnum pattern);

        /// <summary>
        /// 生成页面缩略图
        /// </summary>
        /// <param name="pageIndex">页面索引</param>
        void GenerateThumbnail(int pageIndex);

        /// <summary>
        /// 更新页面缩略图数据（新增）
        /// </summary>
        /// <param name="pageIndex">页面索引</param>
        /// <param name="thumbnailData">缩略图数据</param>
        void UpdatePageThumbnail(int pageIndex, byte[] thumbnailData);

        #endregion

        #region 导入导出

        /// <summary>
        /// 导出所有页面数据
        /// </summary>
        /// <returns>序列化的页面数据</returns>
        string ExportPages();

        /// <summary>
        /// 导入页面数据
        /// </summary>
        /// <param name="data">序列化的页面数据</param>
        /// <returns>是否成功</returns>
        bool ImportPages(string data);

        #endregion
    }
}
