using Ink_Canvas.Helpers;
using System;
using System.Collections.Generic;
using System.Windows.Ink;

namespace Ink_Canvas.Services
{
    /// <summary>
    /// 页面信息
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

        public PageInfo()
        {
            Strokes = new StrokeCollection();
            CreatedAt = DateTime.Now;
            ModifiedAt = DateTime.Now;
        }
    }

    /// <summary>
    /// 页面变化事件参数
    /// </summary>
    public class PageChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 旧页面索引
        /// </summary>
        public int OldIndex { get; }

        /// <summary>
        /// 新页面索引
        /// </summary>
        public int NewIndex { get; }

        /// <summary>
        /// 页面总数
        /// </summary>
        public int TotalPages { get; }

        public PageChangedEventArgs(int oldIndex, int newIndex, int totalPages)
        {
            OldIndex = oldIndex;
            NewIndex = newIndex;
            TotalPages = totalPages;
        }
    }

    /// <summary>
    /// 页面服务接口 - 管理画布页面
    /// </summary>
    public interface IPageService
    {
        #region 事件

        /// <summary>
        /// 页面变化事件
        /// </summary>
        event EventHandler<PageChangedEventArgs> PageChanged;

        /// <summary>
        /// 页面添加事件
        /// </summary>
        event EventHandler<int> PageAdded;

        /// <summary>
        /// 页面删除事件
        /// </summary>
        event EventHandler<int> PageDeleted;

        /// <summary>
        /// 页面清除事件
        /// </summary>
        event EventHandler PagesCleared;

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
        /// 获取当前页面信息
        /// </summary>
        PageInfo CurrentPage { get; }

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
        /// 获取页面信息
        /// </summary>
        /// <param name="pageIndex">页面索引</param>
        /// <returns>页面信息</returns>
        PageInfo GetPage(int pageIndex);

        /// <summary>
        /// 获取所有页面信息
        /// </summary>
        /// <returns>页面信息列表</returns>
        IReadOnlyList<PageInfo> GetAllPages();

        /// <summary>
        /// 保存当前页面状态
        /// </summary>
        void SaveCurrentPageState();

        /// <summary>
        /// 恢复当前页面状态
        /// </summary>
        void RestoreCurrentPageState();

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
        /// 生成页面缩略图
        /// </summary>
        /// <param name="pageIndex">页面索引</param>
        void GenerateThumbnail(int pageIndex);

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