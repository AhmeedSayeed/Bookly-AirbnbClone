namespace BLL.ViewModels.Bookings;

public class BookingConfirmationViewModel
{
    public int BookingId { get; set; }
    public string ListingTitle { get; set; } = string.Empty;
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public int NumberOfGuests { get; set; }
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = string.Empty;
}
