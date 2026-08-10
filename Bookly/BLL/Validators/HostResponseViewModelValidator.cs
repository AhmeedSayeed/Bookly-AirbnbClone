using FluentValidation;
using BLL.ViewModels.Reviews;

namespace BLL.Validators;

public class HostResponseViewModelValidator : AbstractValidator<HostResponseViewModel>
{
    public HostResponseViewModelValidator()
    {
        RuleFor(x => x.ReviewId).GreaterThan(0);
        RuleFor(x => x.ResponseText).NotEmpty().MaximumLength(1000);
    }
}
