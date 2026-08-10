using FluentValidation;
using BLL.ViewModels.Account;

namespace BLL.Validators;

public class ProfileViewModelValidator : AbstractValidator<ProfileViewModel>
{
    public ProfileViewModelValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Bio).MaximumLength(500);
    }
}
