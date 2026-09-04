using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace BLL.ViewModels.Account;

public class ProfileViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "FirstNameRequired")]
    [StringLength(100)]
    [Display(Name = "FirstName")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "LastNameRequired")]
    [StringLength(100)]
    [Display(Name = "LastName")]
    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "BioMaxLength")]
    public string? Bio { get; set; }

    public string? ProfilePhotoUrl { get; set; }

    public IFormFile? ProfilePhoto { get; set; }

    public bool IsHost { get; set; }
    public DateTime CreatedAt { get; set; }
}