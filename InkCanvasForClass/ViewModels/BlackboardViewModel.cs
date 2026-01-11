using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ink_Canvas.Core;
using Ink_Canvas.Models;
using Ink_Canvas.Services;
using Ink_Canvas.Services.Events;
using System;
using System.Collections.ObjectModel;
using System.Windows.Ink;

namespace Ink_Canvas.ViewModels
{
    /// <summary>
    /// 页面缩略图模型 - 用于页面列表显示
    /// </summary>
    public class PageThumbnail : ObservableObject
    {
        private int _index;
        private StrokeCollection _strokes;
        private bool _isSelected;
        private byte[] _thumbnailData;

        /// <summary>
        /// 页面索引（从1开始显示）
        /// </summary>
        public int Index
        {
            get => _index;
            set => SetProperty(ref _index, value);
        }

        /// <summary>
        /// 页面笔画集合
        /// </summary>
        public StrokeCollection Strokes
        {
            get => _strokes;
            set => SetProperty(ref _strokes, value);
        }

        /// <summary>
        /// 是否选中
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        /// <summary>
        /// 缩略图数据
        /// </summary>
        public byte[] ThumbnailData
        {
            get => _thumbnailData;
            set => SetProperty(ref _thumbnailData, value);
        }
    }

    /// <summary>
    /// Blackboard ViewModel - 管理白板界面状态和命令
    /// </summary>
    public partial class BlackboardViewModel : ViewModelBase
    {
        private readonly IPageService _pageService;
        private readonly ISettingsService _settingsService;

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="pageService">页面服务</param>
        /// <param name="settingsService">设置服务</param>
        public BlackboardViewModel(
            IPageService pageService,
            ISettingsService settingsService)
        {
            _pageService = pageService ?? throw new ArgumentNullException(nameof(pageService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

            PageThumbnails = new ObservableCollection<PageThumbnail>();

            // 订阅页面服务事件
            _pageService.PageChanged += OnPageChanged;
            _pageService.PageAdded += OnPageAdded;
            _pageService.PageDeleted += OnPageDeleted;
            _pageService.PagesCleared += OnPagesCleared;

            // 初始化状态
            UpdatePageInfo();
            RefreshPageThumbnails();
        }

        #endregion

        #region 页面导航属性

        /// <summary>
        /// 当前页面索引（从0开始）
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CurrentPageDisplay))]
        [NotifyPropertyChangedFor(nameof(PageCountText))]
        [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand))]
        [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
        private int _currentPageIndex;

        /// <summary>
        /// 页面总数
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CurrentPageDisplay))]
        [NotifyPropertyChangedFor(nameof(PageCountText))]
        [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand))]
        [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeletePageCommand))]
        private int _totalPages;

        /// <summary>
        /// 当前页面显示文本（如 "1/5"）
        /// </summary>
        public string CurrentPageDisplay => $"{CurrentPageIndex + 1}/{TotalPages}";

        /// <summary>
        /// 页面计数文本
        /// </summary>
        public string PageCountText => $"{TotalPages} 页";

        /// <summary>
        /// 页面缩略图集合
        /// </summary>
        public ObservableCollection<PageThumbnail> PageThumbnails { get; }

        /// <summary>
        /// 是否可以向前翻页
        /// </summary>
        public bool CanGoPrevious => CurrentPageIndex > 0;

        /// <summary>
        /// 是否可以向后翻页
        /// </summary>
        public bool CanGoNext => CurrentPageIndex < TotalPages - 1;

        #endregion

        #region 显示状态属性

        /// <summary>
        /// 页面列表是否可见
        /// </summary>
        [ObservableProperty]
        private bool _isPageListVisible;

        /// <summary>
        /// 背景设置面板是否可见
        /// </summary>
        [ObservableProperty]
        private bool _isBackgroundPanelVisible;

        /// <summary>
        /// 手势设置面板是否可见
        /// </summary>
        [ObservableProperty]
        private bool _isGesturePanelVisible;

        /// <summary>
        /// 白板界面是否可见
        /// </summary>
        [ObservableProperty]
        private bool _isBlackboardVisible;

        #endregion

        #region 背景设置属性

        /// <summary>
        /// 当前背景颜色
        /// </summary>
        [ObservableProperty]
        private BlackboardBackgroundColorEnum _backgroundColor = BlackboardBackgroundColorEnum.White;

        /// <summary>
        /// 当前背景图案
        /// </summary>
        [ObservableProperty]
        private BlackboardBackgroundPatternEnum _backgroundPattern = BlackboardBackgroundPatternEnum.None;

        #endregion

        #region 页面导航命令

        /// <summary>
        /// 页面变更事件
        /// </summary>
        public event EventHandler<PageNavigationEventArgs> PageNavigationRequested;

        /// <summary>
        /// 上一页命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanGoPrevious))]
        private void PreviousPage()
        {
            if (_pageService.GoPrevious())
            {
                UpdatePageInfo();
                PageNavigationRequested?.Invoke(this, new PageNavigationEventArgs(CurrentPageIndex, NavigationDirection.Previous));
            }
        }

        /// <summary>
        /// 下一页命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanGoNext))]
        private void NextPage()
        {
            if (_pageService.GoNext())
            {
                UpdatePageInfo();
                PageNavigationRequested?.Invoke(this, new PageNavigationEventArgs(CurrentPageIndex, NavigationDirection.Next));
            }
        }

        /// <summary>
        /// 添加新页面命令
        /// </summary>
        [RelayCommand]
        private void AddPage()
        {
            int newIndex = _pageService.AddPage();
            if (newIndex >= 0)
            {
                _pageService.GoToPage(newIndex);
                UpdatePageInfo();
                RefreshPageThumbnails();
                PageNavigationRequested?.Invoke(this, new PageNavigationEventArgs(newIndex, NavigationDirection.New));
            }
        }

        /// <summary>
        /// 删除当前页面命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanDeletePage))]
        private void DeletePage()
        {
            if (_pageService.DeleteCurrentPage())
            {
                UpdatePageInfo();
                RefreshPageThumbnails();
            }
        }

        private bool CanDeletePage() => TotalPages > 1;

        /// <summary>
        /// 导航到指定页面命令
        /// </summary>
        [RelayCommand]
        private void NavigateToPage(int pageIndex)
        {
            if (_pageService.GoToPage(pageIndex))
            {
                UpdatePageInfo();
                PageNavigationRequested?.Invoke(this, new PageNavigationEventArgs(pageIndex, NavigationDirection.Direct));
            }
        }

        #endregion

        #region 页面列表命令

        /// <summary>
        /// 切换页面列表可见性命令
        /// </summary>
        [RelayCommand]
        private void TogglePageList()
        {
            IsPageListVisible = !IsPageListVisible;
            // 关闭其他面板
            if (IsPageListVisible)
            {
                IsBackgroundPanelVisible = false;
                IsGesturePanelVisible = false;
            }
        }

        /// <summary>
        /// 关闭页面列表命令
        /// </summary>
        [RelayCommand]
        private void ClosePageList()
        {
            IsPageListVisible = false;
        }

        #endregion

        #region 背景设置命令

        /// <summary>
        /// 背景颜色变更事件
        /// </summary>
        public event EventHandler<BackgroundChangedEventArgs> BackgroundChanged;

        /// <summary>
        /// 设置背景颜色命令
        /// </summary>
        [RelayCommand]
        private void SetBackgroundColor(BlackboardBackgroundColorEnum color)
        {
            BackgroundColor = color;
            _pageService.UpdatePageBackgroundColor(CurrentPageIndex, color);
            BackgroundChanged?.Invoke(this, new BackgroundChangedEventArgs(color, BackgroundPattern));
        }

        /// <summary>
        /// 设置背景图案命令
        /// </summary>
        [RelayCommand]
        private void SetBackgroundPattern(BlackboardBackgroundPatternEnum pattern)
        {
            BackgroundPattern = pattern;
            _pageService.UpdatePageBackgroundPattern(CurrentPageIndex, pattern);
            BackgroundChanged?.Invoke(this, new BackgroundChangedEventArgs(BackgroundColor, pattern));
        }

        /// <summary>
        /// 切换背景设置面板可见性命令
        /// </summary>
        [RelayCommand]
        private void ToggleBackgroundPanel()
        {
            IsBackgroundPanelVisible = !IsBackgroundPanelVisible;
            // 关闭其他面板
            if (IsBackgroundPanelVisible)
            {
                IsPageListVisible = false;
                IsGesturePanelVisible = false;
            }
        }

        /// <summary>
        /// 关闭背景设置面板命令
        /// </summary>
        [RelayCommand]
        private void CloseBackgroundPanel()
        {
            IsBackgroundPanelVisible = false;
        }

        #endregion

        #region 手势设置命令

        /// <summary>
        /// 手势设置变更事件
        /// </summary>
        public event EventHandler<GestureSettingsChangedEventArgs> GestureSettingsChanged;

        /// <summary>
        /// 切换手势设置面板可见性命令
        /// </summary>
        [RelayCommand]
        private void ToggleGesturePanel()
        {
            IsGesturePanelVisible = !IsGesturePanelVisible;
            // 关闭其他面板
            if (IsGesturePanelVisible)
            {
                IsPageListVisible = false;
                IsBackgroundPanelVisible = false;
            }
        }

        /// <summary>
        /// 关闭手势设置面板命令
        /// </summary>
        [RelayCommand]
        private void CloseGesturePanel()
        {
            IsGesturePanelVisible = false;
        }

        #endregion

        #region 事件处理

        private void OnPageChanged(object sender, PageChangedEventArgs e)
        {
            CurrentPageIndex = e.NewIndex;
            TotalPages = e.TotalPages;
            
            // 更新背景设置
            if (e.NewPage != null)
            {
                BackgroundColor = e.NewPage.BackgroundColor;
                BackgroundPattern = e.NewPage.BackgroundPattern;
            }
            
            UpdateSelectedThumbnail();
        }

        private void OnPageAdded(object sender, PageAddedEventArgs e)
        {
            TotalPages = e.TotalPages;
            RefreshPageThumbnails();
        }

        private void OnPageDeleted(object sender, PageDeletedEventArgs e)
        {
            CurrentPageIndex = e.NewCurrentIndex;
            TotalPages = e.TotalPages;
            RefreshPageThumbnails();
        }

        private void OnPagesCleared(object sender, PagesClearedEventArgs e)
        {
            CurrentPageIndex = 0;
            TotalPages = 1;
            RefreshPageThumbnails();
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 更新页面信息
        /// </summary>
        private void UpdatePageInfo()
        {
            CurrentPageIndex = _pageService.CurrentPageIndex;
            TotalPages = _pageService.PageCount;
            
            // 更新背景设置
            var currentPage = _pageService.CurrentPageState;
            if (currentPage != null)
            {
                BackgroundColor = currentPage.BackgroundColor;
                BackgroundPattern = currentPage.BackgroundPattern;
            }
        }

        /// <summary>
        /// 刷新页面缩略图列表
        /// </summary>
        public void RefreshPageThumbnails()
        {
            PageThumbnails.Clear();
            var pages = _pageService.Pages;
            
            for (int i = 0; i < pages.Count; i++)
            {
                var page = pages[i];
                PageThumbnails.Add(new PageThumbnail
                {
                    Index = i + 1, // 显示从1开始
                    Strokes = page.Strokes?.Clone() ?? new StrokeCollection(),
                    IsSelected = i == CurrentPageIndex,
                    ThumbnailData = page.ThumbnailData
                });
            }
        }

        /// <summary>
        /// 更新选中的缩略图
        /// </summary>
        private void UpdateSelectedThumbnail()
        {
            for (int i = 0; i < PageThumbnails.Count; i++)
            {
                PageThumbnails[i].IsSelected = i == CurrentPageIndex;
            }
        }

        /// <summary>
        /// 更新指定页面的缩略图
        /// </summary>
        /// <param name="pageIndex">页面索引</param>
        /// <param name="strokes">笔画集合</param>
        public void UpdatePageThumbnail(int pageIndex, StrokeCollection strokes)
        {
            if (pageIndex >= 0 && pageIndex < PageThumbnails.Count)
            {
                PageThumbnails[pageIndex].Strokes = strokes?.Clone() ?? new StrokeCollection();
            }
        }

        /// <summary>
        /// 关闭所有弹出面板
        /// </summary>
        public void CloseAllPanels()
        {
            IsPageListVisible = false;
            IsBackgroundPanelVisible = false;
            IsGesturePanelVisible = false;
        }

        #endregion

        #region 清理

        /// <summary>
        /// 清理资源
        /// </summary>
        public override void Cleanup()
        {
            _pageService.PageChanged -= OnPageChanged;
            _pageService.PageAdded -= OnPageAdded;
            _pageService.PageDeleted -= OnPageDeleted;
            _pageService.PagesCleared -= OnPagesCleared;
            base.Cleanup();
        }

        #endregion
    }

    #region 事件参数类

    /// <summary>
    /// 页面导航方向
    /// </summary>
    public enum NavigationDirection
    {
        Previous,
        Next,
        Direct,
        New
    }

    /// <summary>
    /// 页面导航事件参数
    /// </summary>
    public class PageNavigationEventArgs : EventArgs
    {
        /// <summary>
        /// 目标页面索引
        /// </summary>
        public int PageIndex { get; }

        /// <summary>
        /// 导航方向
        /// </summary>
        public NavigationDirection Direction { get; }

        public PageNavigationEventArgs(int pageIndex, NavigationDirection direction)
        {
            PageIndex = pageIndex;
            Direction = direction;
        }
    }

    /// <summary>
    /// 背景变更事件参数
    /// </summary>
    public class BackgroundChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 背景颜色
        /// </summary>
        public BlackboardBackgroundColorEnum BackgroundColor { get; }

        /// <summary>
        /// 背景图案
        /// </summary>
        public BlackboardBackgroundPatternEnum BackgroundPattern { get; }

        public BackgroundChangedEventArgs(
            BlackboardBackgroundColorEnum color, 
            BlackboardBackgroundPatternEnum pattern)
        {
            BackgroundColor = color;
            BackgroundPattern = pattern;
        }
    }

    /// <summary>
    /// 手势设置变更事件参数
    /// </summary>
    public class GestureSettingsChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 是否启用手势
        /// </summary>
        public bool IsGestureEnabled { get; }

        public GestureSettingsChangedEventArgs(bool isGestureEnabled)
        {
            IsGestureEnabled = isGestureEnabled;
        }
    }

    #endregion
}
