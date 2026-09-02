using BLL.ViewModels.Bookings;
using BLL.ViewModels.Common;
using BLL.ViewModels.Reviews;
using System.Collections.Generic;

namespace BLL.ViewModels.Listings;

public class ListingDetailsViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PropertyType { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public decimal PricePerNight { get; set; }
    public int MaxGuests { get; set; }
    public int Bedrooms { get; set; }
    public int Bathrooms { get; set; }
    public int Beds { get; set; }
    public string? CancellationPolicy { get; set; }

    public List<string> PhotoUrls { get; set; } = new();
    public List<string> Amenities { get; set; } = new();
    public UserSummaryViewModel Host { get; set; } = new();
    public List<ReviewViewModel> Reviews { get; set; } = new();
    public double AverageRating { get; set; }

    // Whether the currently logged-in user has this listing saved to their wishlist
    public bool IsWishlisted { get; set; }

    public BookingFormViewModel Booking { get; set; } = new();
}