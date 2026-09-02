namespace BLL.ViewModels.Admin;

public enum RecentActivityType
{
    NewUserRegistered,
    NewListingCreated,
    NewBookingMade
}

public class RecentActivityViewModel
{
    public RecentActivityType Type { get; set; }
    public object[] Args { get; set; } = Array.Empty<object>();
    public DateTime CreatedAt { get; set; }
}