using System.ComponentModel.DataAnnotations;

namespace BLL.ViewModels.Bookings;

public class BookingRequestViewModel
{
    [Required]
    public int ListingId { get; set; }

    [Required, DataType(DataType.Date)]
    public DateTime CheckInDate { get; set; }

    [Required, DataType(DataType.Date)]
    public DateTime CheckOutDate { get; set; }

    [Required, Range(1, 50)]
    public int NumberOfGuests { get; set; }
}
