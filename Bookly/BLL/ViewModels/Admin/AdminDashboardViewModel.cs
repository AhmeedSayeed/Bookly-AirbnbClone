namespace BLL.ViewModels.Admin;

public class AdminDashboardViewModel
{
    public int TotalUsers { get; set; }

    public int TotalListings { get; set; }
    public int TotalBookings { get; set; }
    public decimal TotalRevenue { get; set; }
    public List<RecentActivityViewModel> RecentActivity { get; set; } = new();
}
