using BLL.ViewModels.Common;

namespace BLL.ViewModels.Bookings;

public class BookingDetailsViewModel
{
    public int Id { get; set; }
    public string ListingTitle { get; set; } = string.Empty;
    public string ListingAddress { get; set; } = string.Empty;
    public string? ListingPhotoUrl { get; set; }
    public UserSummaryViewModel Guest { get; set; } = new();
    public UserSummaryViewModel Host { get; set; } = new();
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public int NumberOfGuests { get; set; }
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
