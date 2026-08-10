using System.ComponentModel.DataAnnotations;

namespace BLL.ViewModels.Reviews;

public class HostResponseViewModel
{
    [Required]
    public int ReviewId { get; set; }

    public ReviewViewModel Review { get; set; } = new();

    [Required, MaxLength(1000)]
    public string ResponseText { get; set; } = string.Empty;
}
