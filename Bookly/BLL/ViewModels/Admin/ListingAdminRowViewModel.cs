namespace BLL.ViewModels.Admin;

public class ListingAdminRowViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string HostName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
