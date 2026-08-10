namespace BLL.ViewModels.Notifications;

public class NotificationsViewModel
{
    public List<NotificationViewModel> Notifications { get; set; } = new();
    public int UnreadCount { get; set; }
}
