namespace BLL.ViewModels.Common;

public class ListingCardViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public decimal PricePerNight { get; set; }
    public string? PrimaryPhotoUrl { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public bool IsWishlisted { get; set; }
}
