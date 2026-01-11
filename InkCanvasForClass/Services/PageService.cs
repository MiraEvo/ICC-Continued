using Ink_Canvas;
using Ink_Canvas.Helpers;
using Ink_Canvas.Models;
using Ink_Canvas.Services.Events;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Ink;

namespace Ink_Canvas.Services
{
    /// <summary>
    /// 页面服务实现
    /// </summary>
    public class PageService : IPageService
    {
        private readonly List<PageState> _pages;
        private int _currentPageIndex;
        private bool _isBlackboardMode;
        private readonly ITimeMachineService _timeMachineService;

        public PageService() : this(null) { }

        public PageService(ITimeMachineService timeMachineService)
        {
            _timeMachineService = timeMachineService;
            _pages = new List<PageState>();
            _currentPageIndex = -1;
            AddPage();
        }

        #region Events
        public event EventHandler<PageChangedEventArgs> PageChanged;
        public event EventHandler<PageAddedEventArgs> PageAdded;
        public event EventHandler<PageDeletedEventArgs> PageDeleted;
        public event EventHandler<PagesClearedEventArgs> PagesCleared;
        #endregion

        #region Properties
        public int CurrentPageIndex => _currentPageIndex;
        public int PageCount => _pages.Count;

        public PageState CurrentPageState => 
            _currentPageIndex >= 0 && _currentPageIndex < _pages.Count 
                ? _pages[_currentPageIndex] 
                : null;

        public PageInfo CurrentPage => PageInfo.FromPageState(CurrentPageState);
        
        // 修改：将返回类型更改为 IReadOnlyList 以匹配接口定义
        public IReadOnlyList<PageState> Pages => _pages;
        
        public bool CanGoPrevious => _currentPageIndex > 0;
        public bool CanGoNext => _currentPageIndex < _pages.Count - 1;

        public bool IsBlackboardMode
        {
            get => _isBlackboardMode;
            set => _isBlackboardMode = value;
        }
        #endregion

        #region Navigation
        public bool GoToPage(int pageIndex)
        {
            if (pageIndex < 0 || pageIndex >= _pages.Count) return false;
            if (pageIndex == _currentPageIndex) return true;

            SaveCurrentPageState();
            int oldIndex = _currentPageIndex;
            var oldPage = CurrentPageState;
            _currentPageIndex = pageIndex;
            var newPage = CurrentPageState;

            PageChanged?.Invoke(this, new PageChangedEventArgs(oldIndex, pageIndex, oldPage, newPage, _pages.Count));
            return true;
        }

        public bool GoPrevious() => CanGoPrevious && GoToPage(_currentPageIndex - 1);
        public bool GoNext() => CanGoNext && GoToPage(_currentPageIndex + 1);
        public bool GoFirst() => _pages.Count > 0 && GoToPage(0);
        public bool GoLast() => _pages.Count > 0 && GoToPage(_pages.Count - 1);
        #endregion


        #region Page Management
        public int AddPage()
        {
            var newPage = new PageState(_pages.Count) { IsBlackboardMode = _isBlackboardMode };
            _pages.Add(newPage);
            int newIndex = _pages.Count - 1;
            PageAdded?.Invoke(this, new PageAddedEventArgs(newIndex, newPage, _pages.Count, false));
            if (_pages.Count == 1) _currentPageIndex = 0;
            return newIndex;
        }

        public int InsertPage(int index)
        {
            if (index < 0) index = 0;
            if (index > _pages.Count) index = _pages.Count;

            var newPage = new PageState(index) { IsBlackboardMode = _isBlackboardMode };
            _pages.Insert(index, newPage);

            for (int i = index + 1; i < _pages.Count; i++)
                _pages[i].Index = i;

            if (index <= _currentPageIndex && _currentPageIndex < _pages.Count - 1)
                _currentPageIndex++;

            PageAdded?.Invoke(this, new PageAddedEventArgs(index, newPage, _pages.Count, true));
            return index;
        }

        public bool DeletePage(int pageIndex)
        {
            if (pageIndex < 0 || pageIndex >= _pages.Count) return false;

            if (_pages.Count == 1)
            {
                var page = _pages[0];
                page.Strokes = new StrokeCollection();
                page.History = null;
                page.Touch();
                return true;
            }

            var deletedPage = _pages[pageIndex];
            _pages.RemoveAt(pageIndex);

            for (int i = pageIndex; i < _pages.Count; i++)
                _pages[i].Index = i;

            int newCurrentIndex = _currentPageIndex;
            if (_currentPageIndex >= _pages.Count)
                newCurrentIndex = _pages.Count - 1;
            else if (pageIndex < _currentPageIndex)
                newCurrentIndex = _currentPageIndex - 1;
            else if (pageIndex == _currentPageIndex)
                newCurrentIndex = Math.Min(pageIndex, _pages.Count - 1);

            if (pageIndex == _currentPageIndex)
            {
                var newPage = _pages[newCurrentIndex];
                PageChanged?.Invoke(this, new PageChangedEventArgs(_currentPageIndex, newCurrentIndex, deletedPage, newPage, _pages.Count));
            }

            _currentPageIndex = newCurrentIndex;
            PageDeleted?.Invoke(this, new PageDeletedEventArgs(pageIndex, deletedPage, newCurrentIndex, _pages.Count));
            return true;
        }

        public bool DeleteCurrentPage() => DeletePage(_currentPageIndex);

        public void ClearAllPages()
        {
            int previousCount = _pages.Count;
            _pages.Clear();
            _currentPageIndex = -1;
            PagesCleared?.Invoke(this, new PagesClearedEventArgs(previousCount));
            AddPage();
        }


        public int DuplicatePage(int sourceIndex)
        {
            if (sourceIndex < 0 || sourceIndex >= _pages.Count) return -1;

            var sourcePage = _pages[sourceIndex];
            var newPage = sourcePage.Clone();
            newPage.Index = _pages.Count;
            _pages.Add(newPage);

            int newIndex = _pages.Count - 1;
            PageAdded?.Invoke(this, new PageAddedEventArgs(newIndex, newPage, _pages.Count, false));
            return newIndex;
        }

        public bool MovePage(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= _pages.Count ||
                toIndex < 0 || toIndex >= _pages.Count ||
                fromIndex == toIndex)
                return false;

            var page = _pages[fromIndex];
            _pages.RemoveAt(fromIndex);
            _pages.Insert(toIndex, page);

            for (int i = 0; i < _pages.Count; i++)
                _pages[i].Index = i;

            if (_currentPageIndex == fromIndex)
                _currentPageIndex = toIndex;
            else if (fromIndex < _currentPageIndex && toIndex >= _currentPageIndex)
                _currentPageIndex--;
            else if (fromIndex > _currentPageIndex && toIndex <= _currentPageIndex)
                _currentPageIndex++;

            return true;
        }
        #endregion


        #region Page Data
        public PageInfo GetPage(int pageIndex) => PageInfo.FromPageState(GetPageState(pageIndex));

        public PageState GetPageState(int pageIndex)
        {
            if (pageIndex < 0 || pageIndex >= _pages.Count) return null;
            return _pages[pageIndex];
        }

        public IReadOnlyList<PageInfo> GetAllPages() => 
            _pages.Select(p => PageInfo.FromPageState(p)).ToList().AsReadOnly();

        public void SaveCurrentPageState()
        {
            if (CurrentPageState == null) return;
            if (_timeMachineService != null)
                CurrentPageState.History = _timeMachineService.ExportHistory();
            CurrentPageState.Touch();
        }

        public void SaveCurrentPageState(StrokeCollection strokes)
        {
            if (CurrentPageState == null) return;
            CurrentPageState.Strokes = strokes?.Clone() ?? new StrokeCollection();
            if (_timeMachineService != null)
                CurrentPageState.History = _timeMachineService.ExportHistory();
            CurrentPageState.Touch();
        }

        public void RestoreCurrentPageState()
        {
            if (CurrentPageState == null) return;
            if (_timeMachineService != null && CurrentPageState.History != null)
                _timeMachineService.ImportHistory(CurrentPageState.History);
        }

        public StrokeCollection LoadPageStrokes(int pageIndex)
        {
            var page = GetPageState(pageIndex);
            return page?.Strokes?.Clone() ?? new StrokeCollection();
        }

        public void UpdatePageStrokes(int pageIndex, StrokeCollection strokes)
        {
            var page = GetPageState(pageIndex);
            if (page != null)
            {
                page.Strokes = strokes?.Clone() ?? new StrokeCollection();
                page.Touch();
            }
        }

        public void UpdatePageHistory(int pageIndex, TimeMachineHistory[] history)
        {
            var page = GetPageState(pageIndex);
            if (page != null)
            {
                page.History = history;
                page.Touch();
            }
        }

        public void UpdatePageBackgroundColor(int pageIndex, BlackboardBackgroundColorEnum color)
        {
            var page = GetPageState(pageIndex);
            if (page != null)
            {
                page.BackgroundColor = color;
                page.Touch();
            }
        }

        public void UpdatePageBackgroundPattern(int pageIndex, BlackboardBackgroundPatternEnum pattern)
        {
            var page = GetPageState(pageIndex);
            if (page != null)
            {
                page.BackgroundPattern = pattern;
                page.Touch();
            }
        }

        public void GenerateThumbnail(int pageIndex) { /* UI thread implementation required */ }

        public void UpdatePageThumbnail(int pageIndex, byte[] thumbnailData)
        {
            var page = GetPageState(pageIndex);
            if (page != null)
            {
                page.ThumbnailData = thumbnailData;
                page.Touch();
            }
        }
        #endregion


        #region Import/Export
        public string ExportPages()
        {
            try
            {
                SaveCurrentPageState();
                var exportData = new PageExportData
                {
                    Pages = new List<PageExportInfo>(),
                    CurrentPageIndex = _currentPageIndex,
                    IsBlackboardMode = _isBlackboardMode
                };

                foreach (var page in _pages)
                {
                    exportData.Pages.Add(new PageExportInfo
                    {
                        Index = page.Index,
                        CreatedAt = page.CreatedAt,
                        ModifiedAt = page.ModifiedAt,
                        IsBlackboardMode = page.IsBlackboardMode,
                        StrokesData = SerializeStrokes(page.Strokes),
                        BackgroundColor = page.BackgroundColor,
                        BackgroundPattern = page.BackgroundPattern,
                        ThumbnailData = page.ThumbnailData != null ? Convert.ToBase64String(page.ThumbnailData) : null
                    });
                }
                return JsonConvert.SerializeObject(exportData, Formatting.Indented);
            }
            catch { return null; }
        }

        public bool ImportPages(string data)
        {
            if (string.IsNullOrEmpty(data)) return false;
            try
            {
                var exportData = JsonConvert.DeserializeObject<PageExportData>(data);
                if (exportData?.Pages == null || exportData.Pages.Count == 0) return false;

                _pages.Clear();
                foreach (var pageExport in exportData.Pages)
                {
                    _pages.Add(new PageState
                    {
                        Index = pageExport.Index,
                        CreatedAt = pageExport.CreatedAt,
                        ModifiedAt = pageExport.ModifiedAt,
                        IsBlackboardMode = pageExport.IsBlackboardMode,
                        Strokes = DeserializeStrokes(pageExport.StrokesData),
                        BackgroundColor = pageExport.BackgroundColor,
                        BackgroundPattern = pageExport.BackgroundPattern,
                        ThumbnailData = !string.IsNullOrEmpty(pageExport.ThumbnailData) 
                            ? Convert.FromBase64String(pageExport.ThumbnailData) : null
                    });
                }

                _isBlackboardMode = exportData.IsBlackboardMode;
                _currentPageIndex = Math.Min(exportData.CurrentPageIndex, _pages.Count - 1);
                if (_currentPageIndex < 0 && _pages.Count > 0) _currentPageIndex = 0;
                return true;
            }
            catch { return false; }
        }
        #endregion


        #region Helpers
        private string SerializeStrokes(StrokeCollection strokes)
        {
            if (strokes == null || strokes.Count == 0) return null;
            try
            {
                using (var ms = new MemoryStream())
                {
                    strokes.Save(ms);
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
            catch { return null; }
        }

        private StrokeCollection DeserializeStrokes(string data)
        {
            if (string.IsNullOrEmpty(data)) return new StrokeCollection();
            try
            {
                var bytes = Convert.FromBase64String(data);
                using (var ms = new MemoryStream(bytes))
                    return new StrokeCollection(ms);
            }
            catch { return new StrokeCollection(); }
        }
        #endregion

        #region Export Data Classes
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
            public BlackboardBackgroundColorEnum BackgroundColor { get; set; }
            public BlackboardBackgroundPatternEnum BackgroundPattern { get; set; }
            public string ThumbnailData { get; set; }
        }
        #endregion
    }
}