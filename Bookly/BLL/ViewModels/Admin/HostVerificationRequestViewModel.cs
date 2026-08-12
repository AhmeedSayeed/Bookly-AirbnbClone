using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.ViewModels.Admin;

public class HostVerificationRequestViewModel
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string HostName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DocumentUrl { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
}