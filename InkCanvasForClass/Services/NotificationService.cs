using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Ink_Canvas.Services
{
    /// <summary>
    /// 通知服务实现
    /// 负责显示各种类型的通知消息
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly Func<Panel> _getNotificationContainer;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="getNotificationContainer">获取通知容器的委托</param>
        public NotificationService(Func<Panel> getNotificationContainer)
        {
            _getNotificationContainer = getNotificationContainer ?? throw new ArgumentNullException(nameof(getNotificationContainer));
        }

        /// <summary>
        /// 显示通知消息（默认类型为 Informative）
        /// </summary>
        public object ShowNotification(string message)
        {
            var container = _getNotificationContainer();
            if (container == null)
            {
                throw new InvalidOperationException("Notification container is not available");
            }

            var notification = new MW_Toast(MW_Toast.ToastType.Informative, message, (self) =>
            {
                container.Children.Remove(self);
            });

            container.Children.Add(notification);
            notification.ShowAnimatedWithAutoDispose(3000 + message.Length * 10);
            return notification;
        }

        /// <summary>
        /// 显示指定类型的 Toast 通知
        /// </summary>
        public object ShowToast(string message, INotificationService.ToastType type, int autoCloseMs)
        {
            var container = _getNotificationContainer();
            if (container == null)
            {
                throw new InvalidOperationException("Notification container is not available");
            }

            // 将 INotificationService.ToastType 转换为 MW_Toast.ToastType
            MW_Toast.ToastType toastType = type switch
            {
                INotificationService.ToastType.Informative => MW_Toast.ToastType.Informative,
                INotificationService.ToastType.Success => MW_Toast.ToastType.Success,
                INotificationService.ToastType.Warning => MW_Toast.ToastType.Warning,
                INotificationService.ToastType.Error => MW_Toast.ToastType.Error,
                _ => MW_Toast.ToastType.Informative
            };

            var notification = new MW_Toast(toastType, message, (self) =>
            {
                container.Children.Remove(self);
            });

            container.Children.Add(notification);
            notification.ShowAnimatedWithAutoDispose(autoCloseMs + message.Length * 10);
            return notification;
        }

        /// <summary>
        /// 静态方法：显示新消息（用于全局访问）
        /// </summary>
        public void ShowNewMessage(string message)
        {
            var mainWindow = Application.Current?.Windows.Cast<Window>().FirstOrDefault(window => window is MainWindow) as MainWindow;
            if (mainWindow != null)
            {
                // 通过 MainWindow 的 ShowNotification 方法显示通知
                // 这是为了保持与现有代码的兼容性
                mainWindow.ShowNotification(message);
            }
        }
    }
}
