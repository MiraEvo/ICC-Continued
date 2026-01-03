using Ink_Canvas.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Ink;

namespace Ink_Canvas.Services
{
    /// <summary>
    /// 页面服务实现
    /// </summary>
    public class PageService : IPageService
    {
        private readonly List<PageInfo> _pages;
        private int _currentPageIndex;
        private bool _isBlackboardMode;
        private readonly ITimeMachineService _timeMachineService;

        /// <summary>
        /// 构造函数
        /// </summary>
        public PageService() : this(null)
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="timeMachineService">时光机服务（可选）</param>
        public PageService(ITimeMachineService timeMachineService)
        {
            _timeMachineService = timeMachineService;
            _pages = new List<PageInfo>();
            _currentPageIndex = -1;
            
            // 初始化时添加一个空白页面
            AddPage();
        }

        #region 事件

        /// <inheritdoc />
        public event EventHandler<PageChangedEventArgs> PageChanged;

        /// <inheritdoc />
        public event EventHandler<int> PageAdded;

        /// <inheritdoc />
        public event EventHandler<int> PageDeleted;

        /// <inheritdoc />
        public event EventHandler PagesCleared;

        #endregion

        #region 属性

        /// <inheritdoc />
        public int CurrentPageIndex => _currentPageIndex;

        /// <inheritdoc />
        public int PageCount => _pages.Count;

        /// <inheritdoc />
        public PageInfo CurrentPage => _currentPageIndex >= 0 && _currentPageIndex < _pages.Count 
            ? _pages[_currentPageIndex] 
            : null;

        /// <inheritdoc />
        public bool CanGoPrevious => _currentPageIndex > 0;

        /// <inheritdoc />
        public bool CanGoNext => _currentPageIndex < _pages.Count - 1;

        /// <inheritdoc />
        public bool IsBlackboardMode
        {
            get => _isBlackboardMode;
            set => _isBlackboardMode = value;
        }

        #endregion

        #region 页面导航

        /// <inheritdoc />
        public bool GoToPage(int pageIndex)
        {
            if (pageIndex < 0 || pageIndex >= _pages.Count)
            {
                return false;
            }

            if (pageIndex == _currentPageIndex)
            {
                return true;
            }

            // 保存当前页面状态
            SaveCurrentPageState();

            int oldIndex = _currentPageIndex;
            _currentPageIndex = pageIndex;

            // 触发页面变化事件
            PageChanged?.Invoke(this, new PageChangedEventArgs(oldIndex, pageIndex, _pages.Count));

            return true;
        }

        /// <inheritdoc />
        public bool GoPrevious()
        {
            return CanGoPrevious && GoToPage(_currentPageIndex - 1);
        }

        /// <inheritdoc />
        public bool GoNext()
        {
            return CanGoNext && GoToPage(_currentPageIndex + 1);
        }

        /// <inheritdoc />
        public bool GoFirst()
        {
            return _pages.Count > 0 && GoToPage(0);
        }

        /// <inheritdoc />
        public bool GoLast()
        {
            return _pages.Count > 0 && GoToPage(_pages.Count - 1);
        }

        #endregion

        #region 页面管理

        /// <inheritdoc />
        public int AddPage()
        {
            var newPage = new PageInfo
            {
                Index = _pages.Count,
                IsBlackboardMode = _isBlackboardMode
            };

            _pages.Add(newPage);
            
            int newIndex = _pages.Count - 1;
            PageAdded?.Invoke(this, newIndex);

            // 如果是第一个页面，自动切换到该页面
            if (_pages.Count == 1)
            {
                _currentPageIndex = 0;
            }

            return newIndex;
        }

        /// <inheritdoc />
        public int InsertPage(int index)
        {
            if (index < 0)
            {
                index = 0;
            }
            if (index > _pages.Count)
            {
                index = _pages.Count;
            }

            var newPage = new PageInfo
            {
                Index = index,
                IsBlackboardMode = _isBlackboardMode
            };

            _pages.Insert(index, newPage);

            // 更新后续页面的索引
            for (int i = index + 1; i < _pages.Count; i++)
            {
                _pages[i].Index = i;
            }

            // 如果插入位置在当前页面之前或等于当前页面，需要调整当前索引
            if (index <= _currentPageIndex && _currentPageIndex < _pages.Count - 1)
            {
                _currentPageIndex++;
            }

            PageAdded?.Invoke(this, index);

            return index;
        }

        /// <inheritdoc />
        public bool DeletePage(int pageIndex)
        {
            if (pageIndex < 0 || pageIndex >= _pages.Count)
            {
                return false;
            }

            // 如果只有一个页面，不允许删除
            if (_pages.Count == 1)
            {
                // 清空页面内容而不是删除
                _pages[0].Strokes = new StrokeCollection();
                _pages[0].History = null;
                _pages[0].ModifiedAt = DateTime.Now;
                return true;
            }

            _pages.RemoveAt(pageIndex);

            // 更新后续页面的索引
            for (int i = pageIndex; i < _pages.Count; i++)
            {
                _pages[i].Index = i;
            }

            // 调整当前页面索引
            if (_currentPageIndex >= _pages.Count)
            {
                _currentPageIndex = _pages.Count - 1;
            }
            else if (pageIndex < _currentPageIndex)
            {
                _currentPageIndex--;
            }
            else if (pageIndex == _currentPageIndex)
            {
                // 当前页面被删除，需要触发页面变化事件
                int newIndex = Math.Min(pageIndex, _pages.Count - 1);
                PageChanged?.Invoke(this, new PageChangedEventArgs(_currentPageIndex, newIndex, _pages.Count));
                _currentPageIndex = newIndex;
            }

            PageDeleted?.Invoke(this, pageIndex);

            return true;
        }

        /// <inheritdoc />
        public bool DeleteCurrentPage()
        {
            return DeletePage(_currentPageIndex);
        }

        /// <inheritdoc />
        public void ClearAllPages()
        {
            _pages.Clear();
            _currentPageIndex = -1;
            
            PagesCleared?.Invoke(this, EventArgs.Empty);
            
            // 添加一个空白页面
            AddPage();
        }

        /// <inheritdoc />
        public int DuplicatePage(int sourceIndex)
        {
            if (sourceIndex < 0 || sourceIndex >= _pages.Count)
            {
                return -1;
            }

            var sourcePage = _pages[sourceIndex];
            var newPage = new PageInfo
            {
                Index = _pages.Count,
                Strokes = sourcePage.Strokes?.Clone() ?? new StrokeCollection(),
                IsBlackboardMode = sourcePage.IsBlackboardMode,
                CreatedAt = DateTime.Now,
                ModifiedAt = DateTime.Now
            };

            // 复制历史记录（深拷贝）
            if (sourcePage.History != null)
            {
                newPage.History = new TimeMachineHistory[sourcePage.History.Length];
                Array.Copy(sourcePage.History, newPage.History, sourcePage.History.Length);
            }

            _pages.Add(newPage);

            int newIndex = _pages.Count - 1;
            PageAdded?.Invoke(this, newIndex);

            return newIndex;
        }

        /// <inheritdoc />
        public bool MovePage(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= _pages.Count ||
                toIndex < 0 || toIndex >= _pages.Count ||
                fromIndex == toIndex)
            {
                return false;
            }

            var page = _pages[fromIndex];
            _pages.RemoveAt(fromIndex);
            _pages.Insert(toIndex, page);

            // 更新所有页面的索引
            for (int i = 0; i < _pages.Count; i++)
            {
                _pages[i].Index = i;
            }

            // 调整当前页面索引
            if (_currentPageIndex == fromIndex)
            {
                _currentPageIndex = toIndex;
            }
            else if (fromIndex < _currentPageIndex && toIndex >= _currentPageIndex)
            {
                _currentPageIndex--;
            }
            else if (fromIndex > _currentPageIndex && toIndex <= _currentPageIndex)
            {
                _currentPageIndex++;
            }

            return true;
        }

        #endregion

        #region 页面数据

        /// <inheritdoc />
        public PageInfo GetPage(int pageIndex)
        {
            if (pageIndex < 0 || pageIndex >= _pages.Count)
            {
                return null;
            }
            return _pages[pageIndex];
        }

        /// <inheritdoc />
        public IReadOnlyList<PageInfo> GetAllPages()
        {
            return _pages.AsReadOnly();
        }

        /// <inheritdoc />
        public void SaveCurrentPageState()
        {
            if (CurrentPage == null) return;

            // 保存历史记录
            if (_timeMachineService != null)
            {
                CurrentPage.History = _timeMachineService.ExportHistory();
            }

            CurrentPage.ModifiedAt = DateTime.Now;
        }

        /// <inheritdoc />
        public void RestoreCurrentPageState()
        {
            if (CurrentPage == null) return;

            // 恢复历史记录
            if (_timeMachineService != null && CurrentPage.History != null)
            {
                _timeMachineService.ImportHistory(CurrentPage.History);
            }
        }

        /// <inheritdoc />
        public void UpdatePageStrokes(int pageIndex, StrokeCollection strokes)
        {
            var page = GetPage(pageIndex);
            if (page != null)
            {
                page.Strokes = strokes?.Clone() ?? new StrokeCollection();
                page.ModifiedAt = DateTime.Now;
            }
        }

        /// <inheritdoc />
        public void UpdatePageHistory(int pageIndex, TimeMachineHistory[] history)
        {
            var page = GetPage(pageIndex);
            if (page != null)
            {
                page.History = history;
                page.ModifiedAt = DateTime.Now;
            }
        }

        /// <inheritdoc />
        public void GenerateThumbnail(int pageIndex)
        {
            // 缩略图生成需要在 UI 线程执行
            // 这里只是标记接口，具体实现需要在调用方处理
            var page = GetPage(pageIndex);
            if (page != null)
            {
                // 实际实现需要使用 RenderTargetBitmap 来生成缩略图
                // 这里暂时留空
            }
        }

        #endregion

        #region 导入导出

        /// <inheritdoc />
        public string ExportPages()
        {
            try
            {
                // 保存当前页面状态
                SaveCurrentPageState();

                var exportData = new PageExportData
                {
                    Pages = new List<PageExportInfo>(),
                    CurrentPageIndex = _currentPageIndex,
                    IsBlackboardMode = _isBlackboardMode
                };

                foreach (var page in _pages)
                {
                    var pageExport = new PageExportInfo
                    {
                        Index = page.Index,
                        CreatedAt = page.CreatedAt,
                        ModifiedAt = page.ModifiedAt,
                        IsBlackboardMode = page.IsBlackboardMode,
                        StrokesData = SerializeStrokes(page.Strokes)
                    };
                    exportData.Pages.Add(pageExport);
                }

                return JsonConvert.SerializeObject(exportData, Formatting.Indented);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <inheritdoc />
        public bool ImportPages(string data)
        {
            if (string.IsNullOrEmpty(data)) return false;

            try
            {
                var exportData = JsonConvert.DeserializeObject<PageExportData>(data);
                if (exportData?.Pages == null || exportData.Pages.Count == 0)
                {
                    return false;
                }

                // 清除现有页面
                _pages.Clear();

                foreach (var pageExport in exportData.Pages)
                {
                    var page = new PageInfo
                    {
                        Index = pageExport.Index,
                        CreatedAt = pageExport.CreatedAt,
                        ModifiedAt = pageExport.ModifiedAt,
                        IsBlackboardMode = pageExport.IsBlackboardMode,
                        Strokes = DeserializeStrokes(pageExport.StrokesData)
                    };
                    _pages.Add(page);
                }

                _isBlackboardMode = exportData.IsBlackboardMode;
                _currentPageIndex = Math.Min(exportData.CurrentPageIndex, _pages.Count - 1);

                if (_currentPageIndex < 0 && _pages.Count > 0)
                {
                    _currentPageIndex = 0;
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 序列化笔画集合
        /// </summary>
        private string SerializeStrokes(StrokeCollection strokes)
        {
            if (strokes == null || strokes.Count == 0)
            {
                return null;
            }

            try
            {
                using (var ms = new MemoryStream())
                {
                    strokes.Save(ms);
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 反序列化笔画集合
        /// </summary>
        private StrokeCollection DeserializeStrokes(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return new StrokeCollection();
            }

            try
            {
                var bytes = Convert.FromBase64String(data);
                using (var ms = new MemoryStream(bytes))
                {
                    return new StrokeCollection(ms);
                }
            }
            catch
            {
                return new StrokeCollection();
            }
        }

        #endregion

        #region 导出数据类

        private class PageExportData
        {
            public List<PageExportInfo> Pages { get; set; }
            public int CurrentPageIndex { get; set; }
            public bool IsBlackboardMode { get; set; }
        }

        private class PageExportInfo
        {
            public int Index { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime ModifiedAt { get; set; }
            public bool IsBlackboardMode { get; set; }
            public string StrokesData { get; set; }
        }

        #endregion
    }
}