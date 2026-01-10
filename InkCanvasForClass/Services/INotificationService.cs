using System;

namespace Ink_Canvas.Services
{
    /// <summary>
    /// 通知服务接口
    /// 负责显示各种类型的通知消息
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Toast 通知类型
        /// </summary>
        enum ToastType
        {
            Informative,
            Success,
            Warning,
            Error
        }

        /// <summary>
        /// 显示通知消息（默认类型为 Informative）
        /// </summary>
        /// <param name="message">通知消息内容</param>
        /// <returns>Toast 对象</returns>
        object ShowNotification(string message);

        /// <summary>
        /// 显示指定类型的 Toast 通知
        /// </summary>
        /// <param name="message">通知消息内容</param>
        /// <param name="type">通知类型</param>
        /// <param name="autoCloseMs">自动关闭时间（毫秒）</param>
        /// <returns>Toast 对象</returns>
        object ShowToast(string message, ToastType type, int autoCloseMs);

        /// <summary>
        /// 静态方法：显示新消息（用于全局访问）
        /// </summary>
        /// <param name="message">通知消息内容</param>
        void ShowNewMessage(string message);
    }
}
