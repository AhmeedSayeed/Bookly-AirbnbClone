using FluentValidation;
using BLL.ViewModels.Account;

namespace BLL.Validators;

public class LoginViewModelValidator : AbstractValidator<LoginViewModel>
{
    public LoginViewModelValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("EmailRequired")
            .EmailAddress()
            .WithMessage("InvalidEmailAddress");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("PasswordRequired");
    }
}