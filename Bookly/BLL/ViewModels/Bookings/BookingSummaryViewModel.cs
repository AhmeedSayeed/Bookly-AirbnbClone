namespace BLL.ViewModels.Bookings;

public class BookingSummaryViewModel
{
    public int Id { get; set; }
    public string ListingTitle { get; set; } = string.Empty;
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public decimal TotalPrice { get; set; }
}
