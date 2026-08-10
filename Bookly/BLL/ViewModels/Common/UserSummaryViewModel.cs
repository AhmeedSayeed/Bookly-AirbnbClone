namespace BLL.ViewModels.Common;

public class UserSummaryViewModel
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? ProfilePhotoUrl { get; set; }
    public bool IsVerifiedHost { get; set; }
    public DateTime MemberSince { get; set; }
}
