using BLL.ViewModels.Bookings;

namespace BLL.ViewModels.Payments;

public class PaymentResultViewModel
{
    public bool Success { get; set; }
    public BookingSummaryViewModel Booking { get; set; } = new();
    public string? TransactionId { get; set; }
    public string? FailureReason { get; set; }
}
