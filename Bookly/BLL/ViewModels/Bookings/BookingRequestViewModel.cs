using System.ComponentModel.DataAnnotations;

namespace BLL.ViewModels.Bookings;

public class BookingRequestViewModel
{
    [Required(ErrorMessage = "ListingIdRequired")]
    public int ListingId { get; set; }

    [Required(ErrorMessage = "CheckInDateRequired")]
    [DataType(DataType.Date)]
    public DateTime CheckInDate { get; set; }

    [Required(ErrorMessage = "CheckOutDateRequired")]
    [DataType(DataType.Date)]
    public DateTime CheckOutDate { get; set; }

    [Required(ErrorMessage = "NumberOfGuestsRequired")]
    [Range(1, 50, ErrorMessage = "NumberOfGuestsRange")]
    public int NumberOfGuests { get; set; }
}