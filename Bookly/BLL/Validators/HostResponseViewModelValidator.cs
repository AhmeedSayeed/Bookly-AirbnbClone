using FluentValidation;
using BLL.ViewModels.Reviews;

namespace BLL.Validators;

public class HostResponseViewModelValidator : AbstractValidator<HostResponseViewModel>
{
    public HostResponseViewModelValidator()
    {
        RuleFor(x => x.ReviewId)
            .GreaterThan(0)
            .WithMessage("ReviewIdMustBeGreaterThanZero");

        RuleFor(x => x.ResponseText)
            .NotEmpty()
            .WithMessage("HostResponseRequired")
            .MaximumLength(1000)
            .WithMessage("HostResponseMaximumLength");
    }
}