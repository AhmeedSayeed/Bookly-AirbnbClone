namespace BLL.ViewModels.Bookings;

public class MyTripsViewModel
{
    public List<BookingCardViewModel> Upcoming { get; set; } = new();
    public List<BookingCardViewModel> Past { get; set; } = new();
}
