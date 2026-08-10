namespace BLL.ViewModels.Bookings;

public class BookingRequestCardViewModel
{
    public int Id { get; set; }
    public string ListingTitle { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public string? GuestPhotoUrl { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public int NumberOfGuests { get; set; }
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = string.Empty;
}
