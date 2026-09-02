using FluentValidation;
using BLL.ViewModels.Account;

namespace BLL.Validators;

public class RegisterViewModelValidator : AbstractValidator<RegisterViewModel>
{
    public RegisterViewModelValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("EmailRequired")
            .EmailAddress()
            .WithMessage("InvalidEmailAddress");

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("FirstNameRequired")
            .MinimumLength(2)
            .WithMessage("FirstNameMinimumLength")
            .MaximumLength(50)
            .WithMessage("FirstNameMaximumLength");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("LastNameRequired")
            .MinimumLength(2)
            .WithMessage("LastNameMinimumLength")
            .MaximumLength(50)
            .WithMessage("LastNameMaximumLength");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("PasswordRequired")
            .MinimumLength(8)
            .WithMessage("PasswordMinimumLength")
            .Matches("[A-Z]")
            .WithMessage("PasswordMustContainUppercase")
            .Matches("[a-z]")
            .WithMessage("PasswordMustContainLowercase")
            .Matches("[0-9]")
            .WithMessage("PasswordMustContainNumber");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password)
            .WithMessage("PasswordsDoNotMatch");
    }
}