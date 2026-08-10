using BLL.ViewModels.Common;

namespace BLL.ViewModels.Messages;

public class ConversationViewModel
{
    public List<MessageViewModel> Messages { get; set; } = new();
    public UserSummaryViewModel OtherUser { get; set; } = new();
    public MessageFormViewModel NewMessage { get; set; } = new();
}
