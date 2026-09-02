using System.ComponentModel.DataAnnotations;

namespace BLL.ViewModels.Messages;

public class MessageFormViewModel
{
    [Required(ErrorMessage = "ReceiverIdRequired")]
    public int ReceiverId { get; set; }

    public int? BookingId { get; set; }

    [Required(ErrorMessage = "MessageContentRequired")]
    [MaxLength(2000, ErrorMessage = "MessageContentMaxLength")]
    public string Content { get; set; } = string.Empty;
}