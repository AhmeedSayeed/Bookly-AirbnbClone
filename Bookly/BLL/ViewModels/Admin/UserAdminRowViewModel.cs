namespace BLL.ViewModels.Admin;

public class UserAdminRowViewModel
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsHost { get; set; }
    public bool IsLockedOut { get; set; }
    public DateTime CreatedAt { get; set; }
}
