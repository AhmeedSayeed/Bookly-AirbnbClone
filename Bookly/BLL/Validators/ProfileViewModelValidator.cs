using FluentValidation;
using BLL.ViewModels.Account;

namespace BLL.Validators;

public class ProfileViewModelValidator : AbstractValidator<ProfileViewModel>
{
    public ProfileViewModelValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("FirstNameRequired")
            .MaximumLength(50)
            .WithMessage("FirstNameMaximumLength");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("LastNameRequired")
            .MaximumLength(50)
            .WithMessage("LastNameMaximumLength");

        RuleFor(x => x.Bio)
            .MaximumLength(500)
            .WithMessage("BioMaximumLength");
    }
}