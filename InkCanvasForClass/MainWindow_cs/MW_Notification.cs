// ============================================================================
// MW_Notification.cs - 通知显示
// ============================================================================
// 
// 功能说明:
//   - 显示应用内通知消息
//   - 静态方法 ShowNewMessage 供全局调用
//
// 迁移状态 (渐进式迁移):
//   - NotificationService 已创建，提供通知队列管理
//   - 此文件中的简单通知逻辑仍在使用
//
// 相关文件:
//   - Services/NotificationService.cs
//   - Services/INotificationService.cs
//   - MW_Toast.xaml.cs (Toast 通知 UI)
//
// ============================================================================

using Ink_Canvas.Helpers;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {



        public static void ShowNewMessage(string notice) {
            (Application.Current?.Windows.Cast<Window>().FirstOrDefault(window => window is MainWindow) as MainWindow)?.ShowNotification(notice);
        }

        public MW_Toast ShowNotification(string notice) {
            var notification = new MW_Toast(MW_Toast.ToastType.Informative, notice, (self) => {
                GridNotifications.Children.Remove(self);
            });
            GridNotifications.Children.Add(notification);
            notification.ShowAnimatedWithAutoDispose(3000 + notice.Length * 10);
            return notification;
        }

        public MW_Toast ShowNewToast(string notice, MW_Toast.ToastType type, int autoCloseMs) {
            var notification = new MW_Toast(type, notice, (self) => {
                GridNotifications.Children.Remove(self);
            });
            GridNotifications.Children.Add(notification);
            notification.ShowAnimatedWithAutoDispose(autoCloseMs + notice.Length * 10);
            return notification;
        }
    }
}