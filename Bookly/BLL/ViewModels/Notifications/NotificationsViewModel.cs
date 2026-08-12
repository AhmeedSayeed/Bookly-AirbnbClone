using BLL.ViewModels.Common;

namespace BLL.ViewModels.Notifications;

public class NotificationsViewModel
{
    public List<NotificationViewModel> Notifications { get; set; } = new();
    public int UnreadCount { get; set; }
    public PageInfoViewModel PageInfo { get; set; } = new();
}