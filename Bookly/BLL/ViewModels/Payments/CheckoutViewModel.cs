using BLL.ViewModels.Bookings;

namespace BLL.ViewModels.Payments;

public class CheckoutViewModel
{
    public BookingSummaryViewModel Booking { get; set; } = new();
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EGP";
    public string? PaymobIframeUrl { get; set; }
}
