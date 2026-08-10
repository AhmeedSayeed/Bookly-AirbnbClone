namespace BLL.ViewModels.Availability;

public class AvailabilityCalendarViewModel
{
    public int ListingId { get; set; }
    public string ListingTitle { get; set; } = string.Empty;
    public List<DateTime> BookedDates { get; set; } = new();
    public List<DateTime> BlockedDates { get; set; } = new();
}
