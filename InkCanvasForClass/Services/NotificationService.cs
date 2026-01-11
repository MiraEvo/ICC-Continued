using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Ink_Canvas.Services
{
    /// <summary>
    /// 通知项
    /// </summary>
    internal class NotificationItem
    {
        public string Message { get; set; }
        public NotificationType Type { get; set; }
        public int DurationMs { get; set; }
    }

    /// <summary>
    /// 通知服务实现
    /// 负责显示各种类型的通知消息
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly Func<Panel> _getNotificationContainer;
        private readonly Queue<NotificationItem> _notificationQueue = new Queue<NotificationItem>();
        private readonly object _queueLock = new object();
        private bool _isProcessingQueue = false;
        private bool _isQueueEnabled = true;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="getNotificationContainer">获取通知容器的委托</param>
        public NotificationService(Func<Panel> getNotificationContainer)
        {
            _getNotificationContainer = getNotificationContainer ?? throw new ArgumentNullException(nameof(getNotificationContainer));
        }

        /// <summary>
        /// 获取或设置是否启用通知队列
        /// </summary>
        public bool IsQueueEnabled
        {
            get => _isQueueEnabled;
            set => _isQueueEnabled = value;
        }

        /// <summary>
        /// 获取队列中待处理的通知数量
        /// </summary>
        public int QueuedNotificationCount
        {
            get
            {
                lock (_queueLock)
                {
                    return _notificationQueue.Count;
                }
            }
        }

        /// <summary>
        /// 显示通知消息（默认类型为 Informative）
        /// </summary>
        public object ShowNotification(string message)
        {
            return ShowNotification(message, NotificationType.Info);
        }

        /// <summary>
        /// 显示指定类型的通知消息
        /// </summary>
        public object ShowNotification(string message, NotificationType type)
        {
            // 默认持续时间：3000ms + 消息长度 * 10ms
            int durationMs = 3000 + message.Length * 10;
            return ShowNotification(message, type, durationMs);
        }

        /// <summary>
        /// 显示指定类型的通知消息，带自定义持续时间
        /// </summary>
        public object ShowNotification(string message, NotificationType type, int durationMs)
        {
            if (_isQueueEnabled)
            {
                // 将通知加入队列
                lock (_queueLock)
                {
                    _notificationQueue.Enqueue(new NotificationItem
                    {
                        Message = message,
                        Type = type,
                        DurationMs = durationMs
                    });
                }

                // 启动队列处理
                ProcessQueueAsync();
                return null; // 队列模式下不返回具体的 Toast 对象
            }
            else
            {
                // 直接显示通知
                return ShowNotificationInternal(message, type, durationMs);
            }
        }

        /// <summary>
        /// 显示指定类型的 Toast 通知（保留用于向后兼容）
        /// </summary>
        public object ShowToast(string message, INotificationService.ToastType type, int autoCloseMs)
        {
            // 将 ToastType 转换为 NotificationType
            NotificationType notificationType = type switch
            {
                INotificationService.ToastType.Informative => NotificationType.Info,
                INotificationService.ToastType.Success => NotificationType.Success,
                INotificationService.ToastType.Warning => NotificationType.Warning,
                INotificationService.ToastType.Error => NotificationType.Error,
                _ => NotificationType.Info
            };

            // 直接使用传入的 autoCloseMs，不再额外添加消息长度
            return ShowNotification(message, notificationType, autoCloseMs);
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

        /// <summary>
        /// 清除所有待处理的通知
        /// </summary>
        public void ClearQueue()
        {
            lock (_queueLock)
            {
                _notificationQueue.Clear();
            }
        }

        /// <summary>
        /// 内部方法：实际显示通知
        /// </summary>
        private object ShowNotificationInternal(string message, NotificationType type, int durationMs)
        {
            var container = _getNotificationContainer();
            if (container == null)
            {
                throw new InvalidOperationException("Notification container is not available");
            }

            // 将 NotificationType 转换为 MW_Toast.ToastType
            MW_Toast.ToastType toastType = type switch
            {
                NotificationType.Info => MW_Toast.ToastType.Informative,
                NotificationType.Success => MW_Toast.ToastType.Success,
                NotificationType.Warning => MW_Toast.ToastType.Warning,
                NotificationType.Error => MW_Toast.ToastType.Error,
                _ => MW_Toast.ToastType.Informative
            };

            MW_Toast notification = null;

            // 确保在 UI 线程上创建和添加通知
            Application.Current.Dispatcher.Invoke(() =>
            {
                notification = new MW_Toast(toastType, message, (self) =>
                {
                    container.Children.Remove(self);
                });

                container.Children.Add(notification);
                notification.ShowAnimatedWithAutoDispose(durationMs);
            });

            return notification;
        }

        /// <summary>
        /// 异步处理通知队列
        /// </summary>
        private void ProcessQueueAsync()
        {
            lock (_queueLock)
            {
                if (_isProcessingQueue)
                {
                    // 已经在处理队列，不需要重复启动
                    return;
                }
                _isProcessingQueue = true;
            }

            Task.Run(async () =>
            {
                while (true)
                {
                    NotificationItem item = null;

                    lock (_queueLock)
                    {
                        if (_notificationQueue.Count == 0)
                        {
                            _isProcessingQueue = false;
                            break;
                        }

                        item = _notificationQueue.Dequeue();
                    }

                    if (item != null)
                    {
                        // 显示通知
                        ShowNotificationInternal(item.Message, item.Type, item.DurationMs);

                        // 等待通知显示完成（持续时间 + 200ms 动画时间）
                        await Task.Delay(item.DurationMs + 200);
                    }
                }
            });
        }
    }
}
