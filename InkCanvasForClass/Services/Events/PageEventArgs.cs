using Ink_Canvas.Models;
using System;

namespace Ink_Canvas.Services.Events
{
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
        /// 旧页面状态
        /// </summary>
        public PageState OldPage { get; }

        /// <summary>
        /// 新页面状态
        /// </summary>
        public PageState NewPage { get; }

        /// <summary>
        /// 页面总数
        /// </summary>
        public int TotalPages { get; }

        /// <summary>
        /// 变化时间
        /// </summary>
        public DateTime ChangedAt { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="oldIndex">旧页面索引</param>
        /// <param name="newIndex">新页面索引</param>
        /// <param name="totalPages">页面总数</param>
        public PageChangedEventArgs(int oldIndex, int newIndex, int totalPages)
        {
            OldIndex = oldIndex;
            NewIndex = newIndex;
            TotalPages = totalPages;
            ChangedAt = DateTime.Now;
        }

        /// <summary>
        /// 构造函数（包含页面状态）
        /// </summary>
        /// <param name="oldIndex">旧页面索引</param>
        /// <param name="newIndex">新页面索引</param>
        /// <param name="oldPage">旧页面状态</param>
        /// <param name="newPage">新页面状态</param>
        /// <param name="totalPages">页面总数</param>
        public PageChangedEventArgs(int oldIndex, int newIndex, PageState oldPage, PageState newPage, int totalPages)
            : this(oldIndex, newIndex, totalPages)
        {
            OldPage = oldPage;
            NewPage = newPage;
        }
    }

    /// <summary>
    /// 页面添加事件参数
    /// </summary>
    public class PageAddedEventArgs : EventArgs
    {
        /// <summary>
        /// 新页面索引
        /// </summary>
        public int PageIndex { get; }

        /// <summary>
        /// 新页面状态
        /// </summary>
        public PageState Page { get; }

        /// <summary>
        /// 页面总数
        /// </summary>
        public int TotalPages { get; }

        /// <summary>
        /// 添加时间
        /// </summary>
        public DateTime AddedAt { get; }

        /// <summary>
        /// 是否为插入操作（而非追加）
        /// </summary>
        public bool IsInsert { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="pageIndex">新页面索引</param>
        /// <param name="totalPages">页面总数</param>
        public PageAddedEventArgs(int pageIndex, int totalPages)
        {
            PageIndex = pageIndex;
            TotalPages = totalPages;
            AddedAt = DateTime.Now;
            IsInsert = false;
        }

        /// <summary>
        /// 构造函数（包含页面状态）
        /// </summary>
        /// <param name="pageIndex">新页面索引</param>
        /// <param name="page">新页面状态</param>
        /// <param name="totalPages">页面总数</param>
        /// <param name="isInsert">是否为插入操作</param>
        public PageAddedEventArgs(int pageIndex, PageState page, int totalPages, bool isInsert = false)
            : this(pageIndex, totalPages)
        {
            Page = page;
            IsInsert = isInsert;
        }
    }

    /// <summary>
    /// 页面删除事件参数
    /// </summary>
    public class PageDeletedEventArgs : EventArgs
    {
        /// <summary>
        /// 被删除的页面索引
        /// </summary>
        public int DeletedIndex { get; }

        /// <summary>
        /// 被删除的页面状态
        /// </summary>
        public PageState DeletedPage { get; }

        /// <summary>
        /// 删除后的当前页面索引
        /// </summary>
        public int NewCurrentIndex { get; }

        /// <summary>
        /// 删除后的页面总数
        /// </summary>
        public int TotalPages { get; }

        /// <summary>
        /// 删除时间
        /// </summary>
        public DateTime DeletedAt { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="deletedIndex">被删除的页面索引</param>
        /// <param name="newCurrentIndex">删除后的当前页面索引</param>
        /// <param name="totalPages">删除后的页面总数</param>
        public PageDeletedEventArgs(int deletedIndex, int newCurrentIndex, int totalPages)
        {
            DeletedIndex = deletedIndex;
            NewCurrentIndex = newCurrentIndex;
            TotalPages = totalPages;
            DeletedAt = DateTime.Now;
        }

        /// <summary>
        /// 构造函数（包含页面状态）
        /// </summary>
        /// <param name="deletedIndex">被删除的页面索引</param>
        /// <param name="deletedPage">被删除的页面状态</param>
        /// <param name="newCurrentIndex">删除后的当前页面索引</param>
        /// <param name="totalPages">删除后的页面总数</param>
        public PageDeletedEventArgs(int deletedIndex, PageState deletedPage, int newCurrentIndex, int totalPages)
            : this(deletedIndex, newCurrentIndex, totalPages)
        {
            DeletedPage = deletedPage;
        }
    }

    /// <summary>
    /// 页面清除事件参数
    /// </summary>
    public class PagesClearedEventArgs : EventArgs
    {
        /// <summary>
        /// 清除前的页面数量
        /// </summary>
        public int PreviousPageCount { get; }

        /// <summary>
        /// 清除时间
        /// </summary>
        public DateTime ClearedAt { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="previousPageCount">清除前的页面数量</param>
        public PagesClearedEventArgs(int previousPageCount)
        {
            PreviousPageCount = previousPageCount;
            ClearedAt = DateTime.Now;
        }
    }
}
