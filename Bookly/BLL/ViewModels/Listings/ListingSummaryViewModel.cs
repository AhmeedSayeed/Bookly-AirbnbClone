namespace BLL.ViewModels.Listings;

public class ListingSummaryViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public decimal PricePerNight { get; set; }
    public bool IsActive { get; set; }
    public int TotalBookings { get; set; }
    public string? PrimaryPhotoUrl { get; set; }
}
