using BLL.ViewModels.Common;

namespace BLL.ViewModels.Messages;

public class ConversationSummaryViewModel
{
    public int OtherUserId { get; set; }
    public UserSummaryViewModel OtherUser { get; set; } = new();
    public string LastMessagePreview { get; set; } = string.Empty;
    public DateTime LastMessageAt { get; set; }
    public int UnreadCount { get; set; }
}
