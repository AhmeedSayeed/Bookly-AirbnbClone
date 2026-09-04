using System.ComponentModel.DataAnnotations;

namespace BLL.ViewModels.Account;

public class RegisterViewModel
{
    [Required(ErrorMessage = "EmailRequired")]
    [EmailAddress(ErrorMessage = "EmailInvalid")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "FirstNameRequired")]
    [MinLength(2)]
    [Display(Name = "FirstName")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "LastNameRequired")]
    [MinLength(2)]
    [Display(Name = "LastName")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "PasswordRequired")]
    [DataType(DataType.Password)]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "ConfirmPasswordRequired")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "PasswordMismatch")]
    [Display(Name = "ConfirmPassword")]
    public string ConfirmPassword { get; set; } = string.Empty;
}