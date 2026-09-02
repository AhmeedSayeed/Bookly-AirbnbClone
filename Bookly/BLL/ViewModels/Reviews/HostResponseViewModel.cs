using System.ComponentModel.DataAnnotations;

namespace BLL.ViewModels.Reviews;

public class HostResponseViewModel
{
    [Required(ErrorMessage = "ReviewIdRequired")]
    public int ReviewId { get; set; }

    public ReviewViewModel Review { get; set; } = new();
    public int ListingId { get; set; }

    [Required(ErrorMessage = "ResponseTextRequired")]
    [MaxLength(1000, ErrorMessage = "ResponseTextMaxLength")]
    public string ResponseText { get; set; } = string.Empty;
}