using System.ComponentModel.DataAnnotations;

namespace BLL.ViewModels.Reviews;

public class CreateReviewViewModel
{
    [Required(ErrorMessage = "BookingIdRequired")]
    public int BookingId { get; set; }

    public string ListingTitle { get; set; } = string.Empty;

    [Required(ErrorMessage = "RatingRequired")]
    [Range(1, 5, ErrorMessage = "RatingRange")]
    public int Rating { get; set; }

    [MaxLength(1000, ErrorMessage = "CommentMaxLength")]
    public string? Comment { get; set; }

    [Range(1, 5, ErrorMessage = "RatingRange")]
    public int? CleanlinessRating { get; set; }

    [Range(1, 5, ErrorMessage = "RatingRange")]
    public int? CommunicationRating { get; set; }

    [Range(1, 5, ErrorMessage = "RatingRange")]
    public int? LocationRating { get; set; }

    [Range(1, 5, ErrorMessage = "RatingRange")]
    public int? ValueRating { get; set; }
}