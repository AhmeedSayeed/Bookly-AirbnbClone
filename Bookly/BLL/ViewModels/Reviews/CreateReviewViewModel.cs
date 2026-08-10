using System.ComponentModel.DataAnnotations;

namespace BLL.ViewModels.Reviews;

public class CreateReviewViewModel
{
    [Required]
    public int BookingId { get; set; }

    public string ListingTitle { get; set; } = string.Empty;

    [Required, Range(1, 5)]
    public int Rating { get; set; }

    [MaxLength(1000)]
    public string? Comment { get; set; }

    [Range(1, 5)]
    public int? CleanlinessRating { get; set; }

    [Range(1, 5)]
    public int? CommunicationRating { get; set; }

    [Range(1, 5)]
    public int? LocationRating { get; set; }

    [Range(1, 5)]
    public int? ValueRating { get; set; }
}
