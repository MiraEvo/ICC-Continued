using Microsoft.Office.Interop.PowerPoint;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace Ink_Canvas.Services
{
    /// <summary>
    /// PowerPoint 集成服务接口
    /// 负责处理与 PowerPoint 的 COM 互操作和幻灯片放映管理
    /// </summary>
    public interface IPPTService
    {
        /// <summary>
        /// PPT 应用程序实例
        /// </summary>
        Microsoft.Office.Interop.PowerPoint.Application PptApplication { get; }

        /// <summary>
        /// 当前演示文稿
        /// </summary>
        Presentation Presentation { get; }

        /// <summary>
        /// 幻灯片集合
        /// </summary>
        Slides Slides { get; }

        /// <summary>
        /// 当前幻灯片
        /// </summary>
        Slide Slide { get; }

        /// <summary>
        /// 幻灯片总数
        /// </summary>
        int SlidesCount { get; }

        /// <summary>
        /// 当前放映位置
        /// </summary>
        int CurrentShowPosition { get; set; }

        /// <summary>
        /// 墨迹内存流数组
        /// </summary>
        MemoryStream[] MemoryStreams { get; set; }

        /// <summary>
        /// 检查并连接到 PowerPoint 应用程序
        /// </summary>
        /// <param name="progId">程序 ID（默认为 "PowerPoint.Application"，WPS 为 "kwpp.Application"）</param>
        /// <returns>是否成功连接</returns>
        bool CheckAndConnectPPT(string progId = "PowerPoint.Application");

        /// <summary>
        /// 启动幻灯片放映
        /// </summary>
        void StartSlideShow();

        /// <summary>
        /// 结束幻灯片放映
        /// </summary>
        void EndSlideShow();

        /// <summary>
        /// 切换到上一张幻灯片
        /// </summary>
        void PreviousSlide();

        /// <summary>
        /// 切换到下一张幻灯片
        /// </summary>
        void NextSlide();

        /// <summary>
        /// 跳转到指定幻灯片
        /// </summary>
        /// <param name="slideIndex">幻灯片索引</param>
        void GoToSlide(int slideIndex);

        /// <summary>
        /// 显示幻灯片导航
        /// </summary>
        void ShowSlideNavigation();

        /// <summary>
        /// 判断幻灯片放映是否正在运行
        /// </summary>
        /// <returns>是否正在放映</returns>
        bool IsSlideShowRunning();

        /// <summary>
        /// 保存墨迹到内存流
        /// </summary>
        /// <param name="slideIndex">幻灯片索引</param>
        /// <param name="stream">墨迹数据流</param>
        void SaveStrokesToMemory(int slideIndex, MemoryStream stream);

        /// <summary>
        /// 从内存流加载墨迹
        /// </summary>
        /// <param name="slideIndex">幻灯片索引</param>
        /// <returns>墨迹数据流</returns>
        MemoryStream LoadStrokesFromMemory(int slideIndex);

        /// <summary>
        /// 异步保存墨迹到文件
        /// </summary>
        /// <param name="parameters">保存参数对象</param>
        /// <returns>异步任务</returns>
        Task SaveStrokesToFileAsync(SaveStrokesParameters parameters);

        /// <summary>
        /// 从文件加载墨迹（异步）
        /// </summary>
        /// <param name="presentationName">演示文稿名称</param>
        /// <param name="slidesCount">幻灯片总数</param>
        /// <param name="rootPath">根路径</param>
        /// <returns>加载的墨迹数量</returns>
        Task<int> LoadStrokesFromFileAsync(string presentationName, int slidesCount, string rootPath);

        /// <summary>
        /// <summary>
        /// 检查是否有隐藏的幻灯片
        /// </summary>
        /// <returns>是否有隐藏幻灯片</returns>
        bool HasHiddenSlides();

        /// <summary>
        /// 取消隐藏所有幻灯片
        /// </summary>
        void UnhideAllSlides();

        /// <summary>
        /// 检查是否启用了自动播放
        /// </summary>
        /// <returns>是否启用自动播放</returns>
        bool HasAutoPlayEnabled();

        /// <summary>
        /// 禁用自动播放
        /// </summary>
        void DisableAutoPlay();

        /// <summary>
        /// 释放 COM 对象资源
        /// </summary>
        void ReleaseComObjects();

        /// <summary>
        /// 演示文稿打开事件
        /// </summary>
        event Action<Presentation> PresentationOpened;

        /// <summary>
        /// 演示文稿关闭事件
        /// </summary>
        event Action<Presentation> PresentationClosed;

        /// <summary>
        /// 幻灯片放映开始事件
        /// </summary>
        event Action<SlideShowWindow> SlideShowBegin;

        /// <summary>
        /// 幻灯片切换事件
        /// </summary>
        event Action<SlideShowWindow> SlideShowNextSlide;

        /// <summary>
        /// 幻灯片放映结束事件
        /// </summary>
        event Action<Presentation> SlideShowEnd;
    }
}
