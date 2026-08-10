using System.ComponentModel.DataAnnotations;

namespace BLL.ViewModels.Messages;

public class MessageFormViewModel
{
    [Required]
    public int ReceiverId { get; set; }

    public int? BookingId { get; set; }

    [Required, MaxLength(2000)]
    public string Content { get; set; } = string.Empty;
}
