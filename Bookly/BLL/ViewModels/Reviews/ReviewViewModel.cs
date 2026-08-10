namespace BLL.ViewModels.Reviews;

public class ReviewViewModel
{
    public int Id { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public string? GuestPhotoUrl { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public int? CleanlinessRating { get; set; }
    public int? CommunicationRating { get; set; }
    public int? LocationRating { get; set; }
    public int? ValueRating { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? HostResponse { get; set; }
}
