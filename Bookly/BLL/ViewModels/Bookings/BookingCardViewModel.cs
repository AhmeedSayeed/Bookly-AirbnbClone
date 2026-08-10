namespace BLL.ViewModels.Bookings;

public class BookingCardViewModel
{
    public int Id { get; set; }
    public string ListingTitle { get; set; } = string.Empty;
    public string ListingCity { get; set; } = string.Empty;
    public string? ListingPhotoUrl { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool CanReview { get; set; }
}
